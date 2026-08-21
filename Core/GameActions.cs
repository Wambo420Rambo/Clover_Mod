using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Reflection;
using BepInEx.Logging;
using CloverMod.Patches;
using HarmonyLib;
using UnityEngine;

namespace CloverMod.Core
{
    internal sealed class GameActions
    {
        private static readonly PowerupScript.Identifier[] CorpsePieces =
        {
            PowerupScript.Identifier.Skeleton_Arm1,
            PowerupScript.Identifier.Skeleton_Arm2,
            PowerupScript.Identifier.Skeleton_Leg1,
            PowerupScript.Identifier.Skeleton_Leg2,
            PowerupScript.Identifier.Skeleton_Head,
        };

        private readonly ManualLogSource log;
        private readonly UndoManager undo;
        private readonly FieldInfo sixChanceField = AccessTools.Field(typeof(GameplayData), "_666Chance");
        private readonly FieldInfo sixChanceMaxField = AccessTools.Field(typeof(GameplayData), "_666ChanceMaxAbsolute");
        private readonly FieldInfo redButtonMultiplierField = AccessTools.Field(typeof(GameplayData), "_redButtonActivationsMultiplier");
        private readonly FieldInfo activationLuckField = AccessTools.Field(typeof(GameplayData), "activationLuck");
        private readonly FieldInfo powerupLuckField = AccessTools.Field(typeof(GameplayData), "powerupLuck");
        private readonly FieldInfo storeLuckField = AccessTools.Field(typeof(GameplayData), "storeLuck");
        private readonly MethodInfo definePhoneStuffMethod = AccessTools.Method(typeof(PhoneUiScript), "DefinePhoneStuff");

        public GameActions(ManualLogSource log)
        {
            this.log = log;
            undo = new UndoManager(log);
        }

        public bool CanUndo => undo.CanUndo;

        public string UndoDescription => undo.Description;

        public ActionResult UndoLastChange() => undo.Undo();

        public string ReadStatus()
        {
            try
            {
                return string.Format(
                    CultureInfo.CurrentCulture,
                    "Coins: {0}   Tickets: {1:N0}   Spins: {2}",
                    ClassExtensions.ToStringSmart(GameplayData.CoinsGet()),
                    GameplayData.CloverTicketsGet(),
                    GameplayData.SpinsLeftGet());
            }
            catch
            {
                return "Start or continue a run to use gameplay actions.";
            }
        }

        public string ReadCharmSlots()
        {
            try
            {
                return GameplayData.MaxEquippablePowerupsGet(false).ToString(CultureInfo.CurrentCulture);
            }
            catch
            {
                return "unavailable";
            }
        }

        public ActionResult AddCoins(BigInteger value)
        {
            if (value.IsZero)
            {
                return ActionResult.Failure("Coin amount must not be zero.");
            }

            BigInteger previous = BigInteger.Zero;
            return Execute("Add coins", () =>
            {
                previous = GameplayData.CoinsGet();
                GameplayData.CoinsAdd(value, true);
                return $"Added {value.ToString("N0", CultureInfo.CurrentCulture)} coins.";
            }, () => GameplayData.CoinsSet(previous));
        }

        public ActionResult AddCoinPower(int exponent)
        {
            if (exponent < 0 || exponent > 1000)
            {
                return ActionResult.Failure("Coin exponent must be between 0 and 1000.");
            }

            return AddCoins(BigInteger.Pow(10, exponent));
        }

        public ActionResult AddCloverTickets(long value)
        {
            if (value == 0)
            {
                return ActionResult.Failure("Ticket amount must not be zero.");
            }

            long previous = 0;
            return Execute("Add clover tickets", () =>
            {
                previous = GameplayData.CloverTicketsGet();
                GameplayData.CloverTicketsAdd(value, true);
                return $"Added {value.ToString("N0", CultureInfo.CurrentCulture)} clover tickets.";
            }, () => GameplayData.CloverTicketsSet(previous));
        }

        public ActionResult AddPatternMultiplier(BigInteger value)
        {
            if (value.IsZero)
            {
                return ActionResult.Failure("Pattern multiplier amount must not be zero.");
            }

            BigInteger previous = BigInteger.Zero;
            return Execute("Add pattern multiplier", () =>
            {
                previous = GameplayData.AllPatternsMultiplierGet(false);
                GameplayData.AllPatternsMultiplierAdd(value);
                return $"Added {value.ToString("N0", CultureInfo.CurrentCulture)} to the pattern multiplier.";
            }, () => GameplayData.AllPatternsMultiplierSet(previous));
        }

        public ActionResult AddSymbolMultiplier(BigInteger value)
        {
            if (value.IsZero)
            {
                return ActionResult.Failure("Symbol multiplier amount must not be zero.");
            }

            BigInteger previous = BigInteger.Zero;
            return Execute("Add symbol multiplier", () =>
            {
                previous = GameplayData.AllSymbolsMultiplierGet(false);
                GameplayData.AllSymbolsMultiplierAdd(value);
                return $"Added {value.ToString("N0", CultureInfo.CurrentCulture)} to the symbol multiplier.";
            }, () => GameplayData.AllSymbolsMultiplierSet(previous));
        }

        public ActionResult AddPatternMultiplierPower(int exponent)
        {
            return AddBigIntegerPower(exponent, AddPatternMultiplier, "Pattern");
        }

        public ActionResult AddSymbolMultiplierPower(int exponent)
        {
            return AddBigIntegerPower(exponent, AddSymbolMultiplier, "Symbol");
        }

        public ActionResult SetInterestRate(float value)
        {
            if (value < 0f || value > 100f)
            {
                return ActionResult.Failure("Interest rate must be between 0 and 100.");
            }

            float previous = 0f;
            return Execute("Set interest rate", () =>
            {
                previous = GameplayData.InterestRateGet();
                GameplayData.InterestRateSet(value);
                return $"Interest rate set to {value:0.###}.";
            }, () => GameplayData.InterestRateSet(previous));
        }

        public ActionResult SetSixMaxPercent(float percent)
        {
            if (!IsPercent(percent))
            {
                return ActionResult.Failure("666 maximum must be between 0% and 100%.");
            }

            return SetPrivateFloat(sixChanceMaxField, percent / 100f, "666 maximum");
        }

        public ActionResult SetSixChancePercent(float percent)
        {
            if (!IsPercent(percent))
            {
                return ActionResult.Failure("666 chance must be between 0% and 100%.");
            }

            try
            {
                GameplayData data = GameplayData.Instance;
                if (data == null)
                {
                    return ActionResult.Failure("Gameplay data is not ready.");
                }

                if (sixChanceField == null || sixChanceMaxField == null)
                {
                    return ActionResult.Failure("This game version does not expose the 666 chance fields.");
                }

                float maximum = (float)sixChanceMaxField.GetValue(data);
                float value = percent / 100f;
                if (value > maximum)
                {
                    return ActionResult.Failure($"Raise the 666 maximum first (current maximum: {maximum:P1}).");
                }

                float previous = (float)sixChanceField.GetValue(data);
                sixChanceField.SetValue(data, value);
                undo.Record("Set 666 chance", () => sixChanceField.SetValue(GameplayData.Instance, previous));
                return LoggedSuccess($"666 chance set to {percent:0.###}%.");
            }
            catch (Exception exception)
            {
                return Failed("Set 666 chance", exception);
            }
        }

        public ActionResult SetRedButtonMultiplier(int value)
        {
            if (value < 0)
            {
                return ActionResult.Failure("Red-button multiplier must be zero or greater.");
            }

            return SetPrivateInt(redButtonMultiplierField, value, "Red-button multiplier");
        }

        public ActionResult SetAscensionCounter(int value)
        {
            if (value < 0)
            {
                return ActionResult.Failure("Ascension counter must be zero or greater.");
            }

            int previous = 0;
            return Execute("Set ascension counter", () =>
            {
                Panik.Data.GameData data = Panik.Data.GameData.inst;
                if (data == null)
                {
                    throw new InvalidOperationException("Save data is not ready.");
                }

                previous = data.goodEndingCounter;
                data.goodEndingCounter = value;
                return $"Ascension counter set to {value}. Restart the run to refresh dependent UI.";
            }, () => RequireSaveData().goodEndingCounter = previous);
        }

        public ActionResult AddCharmSlots(int value)
        {
            if (value < 1 || value > 200)
            {
                return ActionResult.Failure("Charm slots to add must be between 1 and 200.");
            }

            int previous = 0;
            return Execute("Add charm slots", () =>
            {
                previous = GameplayData.MaxEquippablePowerupsGet(false);
                GameplayData.MaxEquippablePowerupsAdd(value);
                return $"Added {value} charm slot{(value == 1 ? string.Empty : "s")}.";
            }, () => GameplayData.MaxEquippablePowerupsSet(previous));
        }

        public ActionResult SetCharmSlots(int value)
        {
            if (value < 1 || value > 200)
            {
                return ActionResult.Failure("Total charm slots must be between 1 and 200.");
            }

            int previous = 0;
            return Execute("Set charm slots", () =>
            {
                previous = GameplayData.MaxEquippablePowerupsGet(false);
                GameplayData.MaxEquippablePowerupsSet(value);
                return $"Total charm slots set to {value}.";
            }, () => GameplayData.MaxEquippablePowerupsSet(previous));
        }

        public ActionResult UnlockAllCharms()
        {
            return Execute("Unlock all charms", () =>
            {
                int unlocked = 0;
                int alreadyUnlocked = 0;
                foreach (PowerupScript.Identifier identifier in Enum.GetValues(typeof(PowerupScript.Identifier)))
                {
                    if (!IsRealCharm(identifier))
                    {
                        continue;
                    }

                    if (PowerupScript.IsUnlocked(identifier))
                    {
                        alreadyUnlocked++;
                    }
                    else if (PowerupScript.Unlock(identifier))
                    {
                        unlocked++;
                    }
                }

                return $"Unlocked {unlocked} charms; {alreadyUnlocked} were already unlocked.";
            });
        }

        public IReadOnlyList<CharmInfo> ReadCharmInfos()
        {
            List<CharmInfo> result = new List<CharmInfo>();
            foreach (PowerupScript.Identifier identifier in Enum.GetValues(typeof(PowerupScript.Identifier)))
            {
                if (!IsRealCharm(identifier))
                {
                    continue;
                }

                bool equipped = PowerupScript.IsEquipped_Quick(identifier);
                bool inDrawer = PowerupScript.IsInDrawer_Quick(identifier);
                int chargesUsed = 0;
                int chargesMaximum = 0;
                try
                {
                    chargesUsed = GameplayData.Powerup_ButtonChargesUsed_GetAbsolute(identifier);
                    chargesMaximum = GameplayData.Powerup_ButtonChargesMax_Get(identifier);
                }
                catch
                {
                    // Many passive charms do not expose charge data.
                }

                result.Add(new CharmInfo
                {
                    Identifier = identifier,
                    Unlocked = PowerupScript.IsUnlocked(identifier),
                    Equipped = equipped,
                    InDrawer = inDrawer,
                    Modifier = GameplayData.Powerup_Modifier_Get(identifier),
                    ChargesUsed = chargesUsed,
                    ChargesMaximum = chargesMaximum,
                });
            }

            return result;
        }

        public ActionResult UnlockCharm(PowerupScript.Identifier identifier)
        {
            if (!IsRealCharm(identifier))
            {
                return ActionResult.Failure("Select a valid charm first.");
            }

            return Execute("Unlock charm", () =>
            {
                if (PowerupScript.IsUnlocked(identifier))
                {
                    return $"{identifier} is already unlocked.";
                }

                if (!PowerupScript.Unlock(identifier))
                {
                    throw new InvalidOperationException("The game rejected the unlock request.");
                }

                return $"Unlocked {identifier}.";
            });
        }

        public ActionResult EquipCharm(PowerupScript.Identifier identifier)
        {
            if (!IsRealCharm(identifier))
            {
                return ActionResult.Failure("Select a valid charm first.");
            }

            return Execute("Equip charm", () =>
            {
                if (PowerupScript.IsEquipped_Quick(identifier))
                {
                    return $"{identifier} is already equipped.";
                }

                if (!PowerupScript.IsUnlocked(identifier))
                {
                    PowerupScript.Unlock(identifier);
                }

                if (!PowerupScript.Equip(identifier, false, false))
                {
                    throw new InvalidOperationException("No free charm slot or the charm cannot be equipped here.");
                }

                PowerupScript.RefreshPlacementAll();
                return $"Equipped {identifier}.";
            });
        }

        public ActionResult RechargeCharm(PowerupScript.Identifier identifier)
        {
            if (!IsRealCharm(identifier))
            {
                return ActionResult.Failure("Select a valid charm first.");
            }

            return Execute("Recharge charm", () =>
            {
                bool changed = GameplayData.Powerup_ButtonChargesUsed_Reset(identifier, true);
                return changed ? $"Recharged {identifier}." : $"{identifier} did not need a recharge.";
            });
        }

        public ActionResult RechargeAllCharms()
        {
            return Execute("Recharge all charms", () =>
            {
                bool changed = GameplayData.Powerup_ButtonChargesUsed_ResetAll(true);
                return changed ? "Recharged all charm abilities." : "No charm ability needed a recharge.";
            });
        }

        public ActionResult SetCharmModifier(PowerupScript.Identifier identifier, PowerupScript.Modifier modifier)
        {
            if (!IsRealCharm(identifier) || modifier < PowerupScript.Modifier.none || modifier >= PowerupScript.Modifier.count)
            {
                return ActionResult.Failure("Select a valid charm and modifier first.");
            }

            PowerupScript.Modifier previous = PowerupScript.Modifier.none;
            return Execute("Set charm modifier", () =>
            {
                previous = GameplayData.Powerup_Modifier_Get(identifier);
                GameplayData.Powerup_Modifier_Set(identifier, modifier, true);
                return $"{identifier} modifier set to {modifier}.";
            }, () => GameplayData.Powerup_Modifier_Set(identifier, previous, true));
        }

        public ActionResult DiscardCharm(PowerupScript.Identifier identifier)
        {
            if (!IsRealCharm(identifier))
            {
                return ActionResult.Failure("Select a valid charm first.");
            }

            return Execute("Discard charm", () =>
            {
                if (!PowerupScript.IsEquipped_Quick(identifier) && !PowerupScript.IsInDrawer_Quick(identifier))
                {
                    throw new InvalidOperationException("The selected charm is not owned in this run.");
                }

                if (!PowerupScript.ThrowAway(identifier, false))
                {
                    throw new InvalidOperationException("The game rejected the discard request.");
                }

                return $"Discarded {identifier} back to the unbought pool.";
            });
        }

        public ActionResult SetFreeRestocks(long value)
        {
            if (value < 0)
            {
                return ActionResult.Failure("Free restocks must be zero or greater.");
            }

            long previous = 0;
            return Execute("Set free restocks", () =>
            {
                previous = GameplayData.StoreFreeRestocksGet();
                GameplayData.StoreFreeRestocksSet(value);
                return $"Free restocks set to {value:N0}.";
            }, () => GameplayData.StoreFreeRestocksSet(previous));
        }

        public ActionResult SetSymbolChances(IReadOnlyDictionary<SymbolScript.Kind, float> values)
        {
            float total = 0f;
            foreach (KeyValuePair<SymbolScript.Kind, float> pair in values)
            {
                if (float.IsNaN(pair.Value) || float.IsInfinity(pair.Value) ||
                    pair.Value < 0f || pair.Value > 100f)
                {
                    return ActionResult.Failure($"{pair.Key} chance must be between 0 and 100.");
                }

                total += pair.Value;
            }

            if (Math.Abs(total - 100f) > 0.05f)
            {
                return ActionResult.Failure($"Symbol chances total {total:0.##}; they must total 100.");
            }

            Dictionary<SymbolScript.Kind, float> previous = new Dictionary<SymbolScript.Kind, float>();
            return Execute("Set symbol chances", () =>
            {
                foreach (KeyValuePair<SymbolScript.Kind, float> pair in values)
                {
                    previous[pair.Key] = GameplayData.Symbol_Chance_Get(pair.Key, false, false);
                    GameplayData.Symbol_Chance_Set(pair.Key, pair.Value);
                }

                return "Symbol chances applied (total: 100).";
            }, () =>
            {
                foreach (KeyValuePair<SymbolScript.Kind, float> pair in previous)
                {
                    GameplayData.Symbol_Chance_Set(pair.Key, pair.Value);
                }
            });
        }

        public BigInteger ReadSymbolCoinValue(SymbolScript.Kind kind)
        {
            return new BigInteger(GameplayData.Symbol_CoinsValue_GetBasic(kind)) +
                   GameplayData.Symbol_CoinsValueExtra_Get(kind);
        }

        public ActionResult SetSymbolCoinValues(IReadOnlyDictionary<SymbolScript.Kind, BigInteger> values)
        {
            foreach (KeyValuePair<SymbolScript.Kind, BigInteger> pair in values)
            {
                if (pair.Value < BigInteger.Zero)
                {
                    return ActionResult.Failure($"{pair.Key} coin value must be zero or greater.");
                }
            }

            Dictionary<SymbolScript.Kind, BigInteger> previous = new Dictionary<SymbolScript.Kind, BigInteger>();
            return Execute("Set symbol coin values", () =>
            {
                foreach (KeyValuePair<SymbolScript.Kind, BigInteger> pair in values)
                {
                    previous[pair.Key] = GameplayData.Symbol_CoinsValueExtra_Get(pair.Key);
                    BigInteger basic = new BigInteger(GameplayData.Symbol_CoinsValue_GetBasic(pair.Key));
                    GameplayData.Symbol_CoinsValueExtra_Set(pair.Key, pair.Value - basic);
                }

                return "Symbol coin values applied.";
            }, () =>
            {
                foreach (KeyValuePair<SymbolScript.Kind, BigInteger> pair in previous)
                {
                    GameplayData.Symbol_CoinsValueExtra_Set(pair.Key, pair.Value);
                }
            });
        }

        public ActionResult ResetSymbolCoinBonuses(IEnumerable<SymbolScript.Kind> kinds)
        {
            Dictionary<SymbolScript.Kind, BigInteger> previous = new Dictionary<SymbolScript.Kind, BigInteger>();
            return Execute("Reset symbol coin bonuses", () =>
            {
                foreach (SymbolScript.Kind kind in kinds)
                {
                    previous[kind] = GameplayData.Symbol_CoinsValueExtra_Get(kind);
                    GameplayData.Symbol_CoinsValueExtra_Reset(kind);
                }

                return "Reset all editable symbol coin bonuses to their vanilla base values.";
            }, () =>
            {
                foreach (KeyValuePair<SymbolScript.Kind, BigInteger> pair in previous)
                {
                    GameplayData.Symbol_CoinsValueExtra_Set(pair.Key, pair.Value);
                }
            });
        }

        public ActionResult AddPatternValues(IReadOnlyDictionary<PatternScript.Kind, double> values, bool exponentMode)
        {
            foreach (KeyValuePair<PatternScript.Kind, double> pair in values)
            {
                if (double.IsNaN(pair.Value) || double.IsInfinity(pair.Value))
                {
                    return ActionResult.Failure($"{pair.Key} contains an invalid number.");
                }

                if (exponentMode && (pair.Value < 0 || pair.Value > 300 || pair.Value % 1 != 0))
                {
                    return ActionResult.Failure("Pattern exponents must be whole numbers between 0 and 300.");
                }
            }

            Dictionary<PatternScript.Kind, double> previous = new Dictionary<PatternScript.Kind, double>();
            return Execute("Add pattern values", () =>
            {
                foreach (KeyValuePair<PatternScript.Kind, double> pair in values)
                {
                    previous[pair.Key] = GameplayData.Pattern_ValueExtra_Get(pair.Key);
                    double value = exponentMode ? Math.Pow(10d, pair.Value) : pair.Value;
                    GameplayData.Pattern_ValueExtra_Add(pair.Key, value);
                }

                return exponentMode ? "Exponential pattern values added." : "Pattern values added.";
            }, () =>
            {
                foreach (KeyValuePair<PatternScript.Kind, double> pair in previous)
                {
                    GameplayData.Pattern_ValueExtra_Set(pair.Key, pair.Value);
                }
            });
        }

        public string ReadRunDetails()
        {
            try
            {
                return string.Format(
                    CultureInfo.CurrentCulture,
                    "Seed: {0}\nDebt: {1}   Deadline level: {2}   Deposit: {3}\nRounds left: {4}   Current deadline round: {5}   Spins left: {6}\nSlot luck: {7:0.###}   Activation: {8:0.###}   Charm mult.: {9:0.###}   Store: {10:0.###}   Temporary: {11:0.###}\nPattern multiplier: {12}   Symbol multiplier: {13}   Interest: {14:0.###}",
                    GameplayData.SeedGetAsString(),
                    ClassExtensions.ToStringSmart(GameplayData.DebtGet()),
                    ClassExtensions.ToStringSmart(GameplayData.DebtIndexGet()),
                    ClassExtensions.ToStringSmart(GameplayData.DepositGet()),
                    GameplayData.RoundsLeftToDeadline(),
                    GameplayData.RoundOfDeadlineGet(),
                    GameplayData.SpinsLeftGet(),
                    GameplayData.LuckGet(),
                    GameplayData.ActivationLuckGet(),
                    GameplayData.PowerupLuckGet(),
                    GameplayData.StoreLuckGet(),
                    GameplayData.ExtraLuck_GetTotal(),
                    ClassExtensions.ToStringSmart(GameplayData.AllPatternsMultiplierGet(false)),
                    ClassExtensions.ToStringSmart(GameplayData.AllSymbolsMultiplierGet(false)),
                    GameplayData.InterestRateGet());
            }
            catch
            {
                return "Run information is unavailable. Start or continue a run first.";
            }
        }

        public string ReadSeed()
        {
            try
            {
                return GameplayData.SeedGetAsString();
            }
            catch
            {
                return string.Empty;
            }
        }

        public bool TryReadRunEditorValues(out BigInteger debtIndex, out BigInteger deposit)
        {
            try
            {
                debtIndex = GameplayData.DebtIndexGet();
                deposit = GameplayData.DepositGet();
                return true;
            }
            catch
            {
                debtIndex = BigInteger.Zero;
                deposit = BigInteger.Zero;
                return false;
            }
        }

        public bool TryReadLuckValues(out float activation, out float powerup, out float store)
        {
            try
            {
                GameplayData data = GameplayData.Instance;
                activation = ReadBaseLuck(data, activationLuckField, GameplayData.ActivationLuckGet);
                powerup = ReadBaseLuck(data, powerupLuckField, GameplayData.PowerupLuckGet);
                store = ReadBaseLuck(data, storeLuckField, GameplayData.StoreLuckGet);
                return true;
            }
            catch
            {
                activation = 0f;
                powerup = 0f;
                store = 0f;
                return false;
            }
        }

        public ActionResult SetActivationLuck(float value)
        {
            return SetLuckValue(
                "activation luck",
                value,
                () => ReadBaseLuck(GameplayData.Instance, activationLuckField, GameplayData.ActivationLuckGet),
                GameplayData.ActivationLuckSet);
        }

        public ActionResult SetPowerupLuck(float value)
        {
            return SetLuckValue(
                "charm luck multiplier",
                value,
                () => ReadBaseLuck(GameplayData.Instance, powerupLuckField, GameplayData.PowerupLuckGet),
                GameplayData.PowerupLuckSet);
        }

        public ActionResult SetStoreLuck(float value)
        {
            return SetLuckValue(
                "store luck",
                value,
                () => ReadBaseLuck(GameplayData.Instance, storeLuckField, GameplayData.StoreLuckGet),
                GameplayData.StoreLuckSet);
        }

        public ActionResult ResetTemporaryLuck()
        {
            return Execute("Reset temporary luck", () =>
            {
                GameplayData.ExtraLuck_ResetAll();
                return "Cleared all temporary timed luck entries.";
            });
        }

        public ActionResult SetDeadlineLevel(BigInteger value)
        {
            if (value < BigInteger.Zero)
            {
                return ActionResult.Failure("Deadline level must be zero or greater.");
            }

            BigInteger previous = BigInteger.Zero;
            return Execute("Set deadline level", () =>
            {
                previous = GameplayData.DebtIndexGet();
                GameplayData.DebtIndexSet(value);
                return $"Deadline level set to {value.ToString("N0", CultureInfo.CurrentCulture)}.";
            }, () => GameplayData.DebtIndexSet(previous));
        }

        public ActionResult SetDeposit(BigInteger value)
        {
            if (value < BigInteger.Zero)
            {
                return ActionResult.Failure("Deposit must be zero or greater.");
            }

            BigInteger previous = BigInteger.Zero;
            return Execute("Set deposit", () =>
            {
                previous = GameplayData.DepositGet();
                GameplayData.DepositSet(value);
                return $"Deposit set to {value.ToString("N0", CultureInfo.CurrentCulture)}.";
            }, () => GameplayData.DepositSet(previous));
        }

        public ActionResult TriggerPhoneTransformation()
        {
            return Execute("Trigger phone transformation", () =>
            {
                GameplayData data = GameplayData.Instance;
                if (data == null)
                {
                    throw new InvalidOperationException("Gameplay data is not ready.");
                }

                data._phone_pickedUpOnceLastDeadline = false;
                data._phone_bookSpecialCall = true;
                data._phoneAlreadyTransformed = false;
                data._phone_SpecialCalls_Counter = 3;

                PhoneUiScript phoneUi = UnityEngine.Object.FindFirstObjectByType<PhoneUiScript>();
                if (phoneUi == null || definePhoneStuffMethod == null)
                {
                    throw new InvalidOperationException("Phone UI is not available in the current scene.");
                }

                definePhoneStuffMethod.Invoke(phoneUi, new object[] { true });
                return "Phone transformation triggered.";
            });
        }

        public ActionResult TriggerPhoneRing()
        {
            return Execute("Trigger phone ring", () =>
            {
                GameplayData data = GameplayData.Instance;
                if (data == null || PhoneScript.instance == null)
                {
                    throw new InvalidOperationException("Phone is not available in the current scene.");
                }

                data._phone_abilityAlreadyPickedUp = false;
                data._phone_pickedUpOnceLastDeadline = false;
                PhoneScript.StateSet(PhoneScript.State.offRinging);
                PhoneScript.PhoneRing();
                return "Phone ring triggered.";
            });
        }

        public ActionResult SetMemoryCardCounts(int value)
        {
            if (value < 0 || value > 1000000)
            {
                return ActionResult.Failure("Memory-card count must be between 0 and 1,000,000.");
            }

            return Execute("Set memory-card counts", () =>
            {
                Panik.Data.GameData data = RequireSaveData();
                int updated = 0;
                UnlimitedMemoryCardsPatch.Bypass = true;
                try
                {
                    foreach (RunModifierScript.Identifier identifier in Enum.GetValues(typeof(RunModifierScript.Identifier)))
                    {
                        if (!IsRealMemoryCard(identifier))
                        {
                            continue;
                        }

                        data.RunModifier_OwnedCount_Set(identifier, value);
                        updated++;
                    }
                }
                finally
                {
                    UnlimitedMemoryCardsPatch.Bypass = false;
                }

                return $"Set {updated} memory-card counts to {value:N0}.";
            });
        }

        public ActionResult SetMemoryCardWins(int value)
        {
            if (value < 0 || value > 1000000)
            {
                return ActionResult.Failure("Memory-card wins must be between 0 and 1,000,000.");
            }

            return Execute("Set memory-card wins", () =>
            {
                Panik.Data.GameData data = RequireSaveData();
                int updated = 0;
                foreach (RunModifierScript.Identifier identifier in Enum.GetValues(typeof(RunModifierScript.Identifier)))
                {
                    if (!IsRealMemoryCard(identifier))
                    {
                        continue;
                    }

                    data.RunModifier_WonTimes_Set(identifier, value);
                    updated++;
                }

                return $"Set wins for {updated} memory cards to {value:N0}.";
            });
        }

        public ActionResult EquipAllCorpsePieces()
        {
            return Execute("Equip corpse pieces", () =>
            {
                int equipped = 0;
                foreach (PowerupScript.Identifier identifier in CorpsePieces)
                {
                    if (PowerupScript.IsEquipped_Quick(identifier) || PowerupScript.Equip(identifier, true, false))
                    {
                        equipped++;
                    }
                }

                return equipped == CorpsePieces.Length
                    ? "All corpse pieces equipped."
                    : $"Equipped {equipped}/{CorpsePieces.Length} corpse pieces. Add charm slots if the layout is full.";
            });
        }

        public ActionResult AddExtraRounds(int value)
        {
            if (value < 1 || value > 1000000)
            {
                return ActionResult.Failure("Extra rounds must be between 1 and 1,000,000.");
            }

            return Execute("Add extra rounds", () =>
            {
                GameplayData.DeadlineRoundsIncrement_Manual(value);
                return $"Added {value:N0} extra rounds.";
            });
        }

        public ActionResult AddExtraSpins(int value)
        {
            if (value < 1 || value > 1000000)
            {
                return ActionResult.Failure("Extra spins must be between 1 and 1,000,000.");
            }

            int previous = 0;
            return Execute("Add extra spins", () =>
            {
                previous = GameplayData.SpinsLeftGet();
                GameplayData.ExtraSpinsAdd(value);
                return $"Added {value:N0} extra spins.";
            }, () => GameplayData.SpinsLeftSet(previous));
        }

        public ActionResult SetTransitionSpeed(int value)
        {
            if (value < 1 || value > 20)
            {
                return ActionResult.Failure("Transition speed must be between 1x and 20x.");
            }

            int previous = AnimationSpeedSafetyPatch.GetRequestedSpeed();
            return Execute("Set transition speed", () =>
            {
                Panik.Data.SettingsData settings = Panik.Data.SettingsData.inst;
                if (settings == null)
                {
                    throw new InvalidOperationException("Settings data is not ready.");
                }

                AnimationSpeedSafetyPatch.SetRequestedSpeed(value);
                return $"Payout/transition animation speed set to {value}x.";
            }, () => AnimationSpeedSafetyPatch.SetRequestedSpeed(previous));
        }

        public ActionResult UnlockAllAchievements()
        {
            return Execute("Unlock all achievements", () =>
            {
                int unlocked = 0;
                foreach (Panik.PlatformAPI.AchievementFullGame achievement in Enum.GetValues(typeof(Panik.PlatformAPI.AchievementFullGame)))
                {
                    int numeric = (int)achievement;
                    if (numeric < 0 || achievement == Panik.PlatformAPI.AchievementFullGame.Count || achievement == Panik.PlatformAPI.AchievementFullGame.Undefined)
                    {
                        continue;
                    }

                    Panik.PlatformAPI.AchievementUnlock_FullGame(achievement);
                    unlocked++;
                }

                return $"Requested unlock for {unlocked} achievements.";
            });
        }

        private ActionResult AddBigIntegerPower(int exponent, Func<BigInteger, ActionResult> action, string label)
        {
            if (exponent < 0 || exponent > 1000)
            {
                return ActionResult.Failure($"{label} exponent must be between 0 and 1000.");
            }

            return action(BigInteger.Pow(10, exponent));
        }

        private ActionResult SetLuckValue(string label, float value, Func<float> getter, Action<float> setter)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value < 0.5f || value > 100000f)
            {
                return ActionResult.Failure($"{label} must be between 0.5 and 100,000 (vanilla: 1.0).");
            }

            float previous = 0f;
            return Execute($"Set {label}", () =>
            {
                previous = getter();
                setter(value);
                return $"{label} set to {value:0.###}.";
            }, () => setter(previous));
        }

        private static float ReadBaseLuck(GameplayData data, FieldInfo field, Func<float> fallback)
        {
            if (data != null && field != null)
            {
                return (float)field.GetValue(data);
            }

            return fallback();
        }

        private ActionResult SetPrivateFloat(FieldInfo field, float value, string label)
        {
            if (field == null)
            {
                return ActionResult.Failure($"This game version does not expose the {label} field.");
            }

            float previous = 0f;
            return Execute($"Set {label}", () =>
            {
                GameplayData data = GameplayData.Instance;
                if (data == null)
                {
                    throw new InvalidOperationException("Gameplay data is not ready.");
                }

                previous = (float)field.GetValue(data);
                field.SetValue(data, value);
                return $"{label} set to {value:P1}.";
            }, () => field.SetValue(GameplayData.Instance, previous));
        }

        private ActionResult SetPrivateInt(FieldInfo field, int value, string label)
        {
            if (field == null)
            {
                return ActionResult.Failure($"This game version does not expose the {label} field.");
            }

            int previous = 0;
            return Execute($"Set {label}", () =>
            {
                GameplayData data = GameplayData.Instance;
                if (data == null)
                {
                    throw new InvalidOperationException("Gameplay data is not ready.");
                }

                previous = (int)field.GetValue(data);
                field.SetValue(data, value);
                return $"{label} set to {value:N0}.";
            }, () => field.SetValue(GameplayData.Instance, previous));
        }

        private ActionResult Execute(string operation, Func<string> action, Action undoAction = null)
        {
            try
            {
                string message = action();
                if (undoAction != null)
                {
                    undo.Record(operation, undoAction);
                }

                return LoggedSuccess(message);
            }
            catch (Exception exception)
            {
                return Failed(operation, exception);
            }
        }

        private ActionResult LoggedSuccess(string message)
        {
            log.LogInfo(message);
            return ActionResult.Success(message);
        }

        private ActionResult Failed(string operation, Exception exception)
        {
            Exception cause = exception is TargetInvocationException invocation && invocation.InnerException != null
                ? invocation.InnerException
                : exception;
            string message = $"{operation} failed: {cause.Message}";
            log.LogError(message);
            return ActionResult.Failure(message);
        }

        private static Panik.Data.GameData RequireSaveData()
        {
            return Panik.Data.GameData.inst ?? throw new InvalidOperationException("Save data is not ready.");
        }

        private static bool IsPercent(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value) && value >= 0f && value <= 100f;
        }

        private static bool IsRealCharm(PowerupScript.Identifier identifier)
        {
            int numeric = (int)identifier;
            return numeric >= 0 && numeric < (int)PowerupScript.Identifier.count;
        }

        private static bool IsRealMemoryCard(RunModifierScript.Identifier identifier)
        {
            int numeric = (int)identifier;
            return numeric > (int)RunModifierScript.Identifier.defaultModifier && numeric < (int)RunModifierScript.Identifier.count;
        }
    }
}
