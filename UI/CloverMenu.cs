using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using BepInEx.Configuration;
using BepInEx.Logging;
using CloverMod.Configuration;
using CloverMod.Core;
using CloverMod.Patches;
using UnityEngine;
using BigInteger = System.Numerics.BigInteger;

namespace CloverMod.UI
{
    internal sealed class CloverMenu : IDisposable
    {
        private const string CursorDisableReason = "CloverMod.Menu";

        private static readonly Regex TextMeshProSpriteTag = new Regex(
            "<sprite[^>]*name=\"([^\"]+)\"[^>]*>",
            RegexOptions.IgnoreCase);

        private static readonly string[] Tabs =
        {
            "Currency",
            "Multipliers",
            "Rates & 666",
            "Charms",
            "Memory Cards",
            "Symbols",
            "Patterns",
            "Run",
            "Extras",
        };

        private static readonly SymbolScript.Kind[] SymbolKinds =
        {
            SymbolScript.Kind.lemon,
            SymbolScript.Kind.cherry,
            SymbolScript.Kind.clover,
            SymbolScript.Kind.bell,
            SymbolScript.Kind.diamond,
            SymbolScript.Kind.coins,
            SymbolScript.Kind.seven,
        };

        private static readonly PatternScript.Kind[] PatternKinds =
        {
            PatternScript.Kind.horizontal3,
            PatternScript.Kind.horizontal4,
            PatternScript.Kind.horizontal5,
            PatternScript.Kind.vertical3,
            PatternScript.Kind.diagonal3,
            PatternScript.Kind.pyramid,
            PatternScript.Kind.pyramidInverted,
            PatternScript.Kind.triangle,
            PatternScript.Kind.triangleInverted,
            PatternScript.Kind.eye,
            PatternScript.Kind.jackpot,
        };

        private static readonly PowerupScript.Modifier[] CharmModifiers =
        {
            PowerupScript.Modifier.none,
            PowerupScript.Modifier.symbolMultiplier,
            PowerupScript.Modifier.patternMultiplier,
            PowerupScript.Modifier.cloverTicket,
            PowerupScript.Modifier.obsessive,
            PowerupScript.Modifier.gambler,
            PowerupScript.Modifier.speculative,
            PowerupScript.Modifier.devious,
        };

        private readonly GameActions actions;
        private readonly ModConfig config;
        private readonly ManualLogSource log;
        private readonly Dictionary<SymbolScript.Kind, string> symbolChanceInputs;
        private readonly Dictionary<SymbolScript.Kind, bool> symbolChanceLocks;
        private readonly Dictionary<SymbolScript.Kind, string> symbolValueInputs;
        private readonly Dictionary<PatternScript.Kind, string> patternValueInputs;
        private readonly List<CharmInfo> charmInfos = new List<CharmInfo>();
        private readonly List<Texture2D> textures = new List<Texture2D>();

        private GUIStyle panelStyle;
        private GUIStyle headerStyle;
        private GUIStyle sectionStyle;
        private GUIStyle labelStyle;
        private GUIStyle mutedStyle;
        private GUIStyle warningStyle;
        private GUIStyle dangerStyle;
        private GUIStyle buttonStyle;
        private GUIStyle primaryButtonStyle;
        private GUIStyle tabStyle;
        private GUIStyle textFieldStyle;
        private GUIStyle statusStyle;
        private GUIStyle sliderStyle;
        private GUIStyle sliderThumbStyle;
        private Texture2D overlayTexture;
        private bool stylesReady;

        private bool isOpen;
        private RebindTarget rebindTarget;
        private int activeTab;
        private Vector2 scrollPosition;
        private CursorLockMode previousCursorLock;
        private bool previousCursorVisible;
        private float previousTimeScale = 1f;
        private float targetGameSpeed = 1f;
        private bool gameSpeedChanged;
        private int transitionSpeed = 1;

        private string statusMessage = "Ready.";
        private bool statusSucceeded = true;
        private bool achievementConfirmationArmed;
        private float achievementConfirmationUntil;
        private bool discardConfirmationArmed;
        private float discardConfirmationUntil;
        private RunEditTarget runEditConfirmation;
        private float runEditConfirmationUntil;
        private PowerupScript.Identifier? selectedCharm;
        private string charmSearch = string.Empty;
        private bool charmOwnedOnly = true;
        private RunModifierScript.Identifier? selectedMemoryCard;
        private string memoryCardSearch = string.Empty;

        private string coinsInput = "1000";
        private string coinExponentInput = "10";
        private string ticketsInput = "100";
        private string patternMultiplierInput = "1";
        private string patternMultiplierExponentInput = "10";
        private string symbolMultiplierInput = "1";
        private string symbolMultiplierExponentInput = "10";
        private string interestInput = "1";
        private string sixChanceInput = "30";
        private string redButtonInput = "1";
        private string ascensionInput = "10";
        private string charmSlotsInput = "1";
        private string restocksInput = "25";
        private string memoryCardsInput = "999";
        private string memoryWinsInput = "10";
        private string extraRoundsInput = "2";
        private string extraSpinsInput = "2";
        private string activationLuckInput = "0";
        private string powerupLuckInput = "0";
        private string storeLuckInput = "0";
        private string debtIndexInput = "0";
        private string depositInput = "0";

        public CloverMenu(GameActions actions, ModConfig config, ManualLogSource log)
        {
            this.actions = actions;
            this.config = config;
            this.log = log;
            symbolChanceInputs = new Dictionary<SymbolScript.Kind, string>
            {
                [SymbolScript.Kind.lemon] = "20",
                [SymbolScript.Kind.cherry] = "20",
                [SymbolScript.Kind.clover] = "20",
                [SymbolScript.Kind.bell] = "10",
                [SymbolScript.Kind.diamond] = "10",
                [SymbolScript.Kind.coins] = "10",
                [SymbolScript.Kind.seven] = "10",
            };

            symbolChanceLocks = new Dictionary<SymbolScript.Kind, bool>();
            symbolValueInputs = new Dictionary<SymbolScript.Kind, string>();
            foreach (SymbolScript.Kind kind in SymbolKinds)
            {
                symbolChanceLocks[kind] = false;
                symbolValueInputs[kind] = "0";
            }

            patternValueInputs = new Dictionary<PatternScript.Kind, string>();
            foreach (PatternScript.Kind kind in PatternKinds)
            {
                patternValueInputs[kind] = "1";
            }
        }

        public bool IsOpen => isOpen;

        public bool IsRebinding => rebindTarget != RebindTarget.None;

        public void Toggle()
        {
            if (isOpen)
            {
                Close();
            }
            else
            {
                Open();
            }
        }

        public void Open()
        {
            if (isOpen)
            {
                return;
            }

            previousCursorLock = Cursor.lockState;
            previousCursorVisible = Cursor.visible;
            previousTimeScale = Time.timeScale;
            targetGameSpeed = previousTimeScale > 0f ? previousTimeScale : 1f;
            gameSpeedChanged = false;

            if (Panik.Data.SettingsData.inst != null)
            {
                transitionSpeed = Mathf.Clamp(AnimationSpeedSafetyPatch.GetRequestedSpeed(), 1, 20);
            }

            LoadCurrentSymbolChances(showStatus: false);
            LoadCurrentSymbolValues(showStatus: false);
            LoadRunEditorValues(showStatus: false);
            RefreshCharmBrowser(showStatus: false);

            isOpen = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            if (config.PauseWhileOpen.Value)
            {
                Time.timeScale = 0f;
            }

            try
            {
                CameraController.DisableReason_Add(CursorDisableReason);
            }
            catch (Exception exception)
            {
                log.LogWarning($"Could not disable camera input: {exception.Message}");
            }

            log.LogInfo("CloverMod menu opened.");
        }

        public void Close()
        {
            if (!isOpen)
            {
                rebindTarget = RebindTarget.None;
                return;
            }

            rebindTarget = RebindTarget.None;
            isOpen = false;
            Cursor.lockState = previousCursorLock;
            Cursor.visible = previousCursorVisible;
            Time.timeScale = gameSpeedChanged ? targetGameSpeed : previousTimeScale;

            try
            {
                CameraController.DisableReason_Remove(CursorDisableReason);
            }
            catch (Exception exception)
            {
                log.LogWarning($"Could not restore camera input: {exception.Message}");
            }

            log.LogInfo("CloverMod menu closed.");
        }

        public void CapturePressedKey()
        {
            if (!Input.anyKeyDown)
            {
                return;
            }

            foreach (KeyCode key in Enum.GetValues(typeof(KeyCode)))
            {
                if (key == KeyCode.None || !Input.GetKeyDown(key))
                {
                    continue;
                }

                if (key == KeyCode.Escape)
                {
                    rebindTarget = RebindTarget.None;
                    SetStatus(ActionResult.Failure("Key rebinding cancelled."));
                    return;
                }

                switch (rebindTarget)
                {
                    case RebindTarget.Menu:
                        config.MenuKey.Value = key;
                        break;
                    case RebindTarget.FallbackMenu:
                        config.FallbackMenuKey.Value = key;
                        break;
                }

                rebindTarget = RebindTarget.None;
                SetStatus(ActionResult.Success($"Hotkey changed to {key}."));
                return;
            }
        }

        public void Draw()
        {
            if (!isOpen)
            {
                return;
            }

            EnsureStyles();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), overlayTexture);

            float width = Mathf.Min(680f, Screen.width - 24f);
            float height = Mathf.Min(760f, Screen.height - 24f);
            Rect panel = new Rect((Screen.width - width) / 2f, (Screen.height - height) / 2f, width, height);
            GUI.Box(panel, GUIContent.none, panelStyle);

            GUILayout.BeginArea(new Rect(panel.x + 14f, panel.y + 12f, panel.width - 28f, panel.height - 24f));
            DrawHeader();
            DrawStatusBar();
            DrawTabs();

            scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.ExpandHeight(true));
            switch (activeTab)
            {
                case 0:
                    DrawCurrency();
                    break;
                case 1:
                    DrawMultipliers();
                    break;
                case 2:
                    DrawRates();
                    break;
                case 3:
                    DrawCharms();
                    break;
                case 4:
                    DrawMemoryCards();
                    break;
                case 5:
                    DrawSymbols();
                    break;
                case 6:
                    DrawPatterns();
                    break;
                case 7:
                    DrawRunTools();
                    break;
                default:
                    DrawExtras();
                    break;
            }

            GUILayout.EndScrollView();
            DrawFooter();
            GUILayout.EndArea();
        }

        public void Dispose()
        {
            Close();
            foreach (Texture2D texture in textures)
            {
                if (texture != null)
                {
                    UnityEngine.Object.Destroy(texture);
                }
            }

            textures.Clear();
            stylesReady = false;
        }

        private void DrawHeader()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("CLOVER MOD", headerStyle);
            GUILayout.FlexibleSpace();
            GUILayout.Label("v" + Plugin.PluginVersion, mutedStyle);
            GUILayout.EndHorizontal();
        }

        private void DrawStatusBar()
        {
            Color old = statusStyle.normal.textColor;
            statusStyle.normal.textColor = statusSucceeded ? new Color(0.65f, 1f, 0.72f) : new Color(1f, 0.55f, 0.55f);
            GUILayout.BeginHorizontal();
            GUILayout.Label(actions.ReadStatus() + "\n" + statusMessage, statusStyle, GUILayout.Height(72f));
            if (actions.CanUndo && GUILayout.Button("UNDO\n" + actions.UndoDescription, buttonStyle, GUILayout.Width(165f), GUILayout.Height(72f)))
            {
                Run(actions.UndoLastChange());
                LoadCurrentSymbolChances(showStatus: false);
                LoadCurrentSymbolValues(showStatus: false);
                LoadRunEditorValues(showStatus: false);
                RefreshCharmBrowser(showStatus: false);
            }
            GUILayout.EndHorizontal();
            statusStyle.normal.textColor = old;
        }

        private void DrawTabs()
        {
            int selected = GUILayout.SelectionGrid(activeTab, Tabs, 4, tabStyle, GUILayout.Height(56f));
            if (selected != activeTab)
            {
                activeTab = selected;
                scrollPosition = Vector2.zero;
            }

            GUILayout.Space(8f);
        }

        private void DrawFooter()
        {
            GUILayout.Space(8f);
            if (GUILayout.Button("CLOSE MENU", primaryButtonStyle, GUILayout.Height(34f)))
            {
                Close();
            }

            GUILayout.Label(
                $"Menu: {config.MenuKey.Value}  |  Fallback: {config.FallbackMenuKey.Value}  |  Escape closes",
                mutedStyle);
        }

        private void DrawCurrency()
        {
            Section("Coins");
            TextActionRow("Amount", ref coinsInput, "Add", () =>
            {
                if (TryParseBigInteger(coinsInput, out BigInteger value))
                {
                    Run(actions.AddCoins(value));
                }
                else
                {
                    Invalid("Enter a whole-number coin amount.");
                }
            });

            TextActionRow("10^ exponent", ref coinExponentInput, "Add power", () =>
            {
                if (TryParseInt(coinExponentInput, out int value))
                {
                    Run(actions.AddCoinPower(value));
                }
                else
                {
                    Invalid("Enter a whole-number exponent.");
                }
            });
            GUILayout.Label("Very large values are supported, but extreme payouts can still stall game animations.", warningStyle);

            Section("Clover tickets");
            TextActionRow("Amount", ref ticketsInput, "Add", () =>
            {
                if (TryParseLong(ticketsInput, out long value))
                {
                    Run(actions.AddCloverTickets(value));
                }
                else
                {
                    Invalid("Enter a whole-number ticket amount.");
                }
            });
        }

        private void DrawMultipliers()
        {
            Section("Pattern multiplier");
            TextActionRow("Amount", ref patternMultiplierInput, "Add", () =>
            {
                if (TryParseBigInteger(patternMultiplierInput, out BigInteger value))
                {
                    Run(actions.AddPatternMultiplier(value));
                }
                else
                {
                    Invalid("Enter a whole-number pattern multiplier.");
                }
            });
            TextActionRow("10^ exponent", ref patternMultiplierExponentInput, "Add power", () =>
            {
                if (TryParseInt(patternMultiplierExponentInput, out int value))
                {
                    Run(actions.AddPatternMultiplierPower(value));
                }
                else
                {
                    Invalid("Enter a whole-number exponent.");
                }
            });

            Section("Symbol multiplier");
            TextActionRow("Amount", ref symbolMultiplierInput, "Add", () =>
            {
                if (TryParseBigInteger(symbolMultiplierInput, out BigInteger value))
                {
                    Run(actions.AddSymbolMultiplier(value));
                }
                else
                {
                    Invalid("Enter a whole-number symbol multiplier.");
                }
            });
            TextActionRow("10^ exponent", ref symbolMultiplierExponentInput, "Add power", () =>
            {
                if (TryParseInt(symbolMultiplierExponentInput, out int value))
                {
                    Run(actions.AddSymbolMultiplierPower(value));
                }
                else
                {
                    Invalid("Enter a whole-number exponent.");
                }
            });
        }

        private void DrawRates()
        {
            Section("Interest rate");
            TextActionRow("Value (0-100)", ref interestInput, "Set", () =>
            {
                if (TryParseFloat(interestInput, out float value))
                {
                    Run(actions.SetInterestRate(value));
                }
                else
                {
                    Invalid("Enter a valid interest rate.");
                }
            });

            Section("666 chance");
            TextActionRow("Percent", ref sixChanceInput, "Set maximum", () =>
            {
                if (TryParseFloat(sixChanceInput, out float value))
                {
                    Run(actions.SetSixMaxPercent(value));
                }
                else
                {
                    Invalid("Enter a valid percentage.");
                }
            }, "Set chance", () =>
            {
                if (TryParseFloat(sixChanceInput, out float value))
                {
                    Run(actions.SetSixChancePercent(value));
                }
                else
                {
                    Invalid("Enter a valid percentage.");
                }
            });
            GUILayout.Label("Set the maximum before setting a chance above the game's current cap.", mutedStyle);

            Section("Run values");
            TextActionRow("Red-button mult.", ref redButtonInput, "Set", () =>
            {
                if (TryParseInt(redButtonInput, out int value))
                {
                    Run(actions.SetRedButtonMultiplier(value));
                }
                else
                {
                    Invalid("Enter a whole-number multiplier.");
                }
            });
            TextActionRow("Ascension count", ref ascensionInput, "Set", () =>
            {
                if (TryParseInt(ascensionInput, out int value))
                {
                    Run(actions.SetAscensionCounter(value));
                }
                else
                {
                    Invalid("Enter a whole-number ascension counter.");
                }
            });
        }

        private void DrawCharms()
        {
            Section("Charm slots");
            GUILayout.Label("Current base slots: " + actions.ReadCharmSlots(), mutedStyle);
            TextActionRow("Slots", ref charmSlotsInput, "Add", () =>
            {
                if (TryParseInt(charmSlotsInput, out int value))
                {
                    Run(actions.AddCharmSlots(value));
                }
                else
                {
                    Invalid("Enter a whole-number slot count.");
                }
            }, "Set total", () =>
            {
                if (TryParseInt(charmSlotsInput, out int value))
                {
                    Run(actions.SetCharmSlots(value));
                }
                else
                {
                    Invalid("Enter a whole-number slot count.");
                }
            });
            GUILayout.Label("Counts above 50 can overflow parts of the vanilla charm UI.", warningStyle);

            ButtonRow(
                "Unlock all charms",
                () => Run(actions.UnlockAllCharms()),
                "Equip corpse pieces",
                () => Run(actions.EquipAllCorpsePieces()));

            Section("Store");
            TextActionRow("Free restocks", ref restocksInput, "Set", () =>
            {
                if (TryParseLong(restocksInput, out long value))
                {
                    Run(actions.SetFreeRestocks(value));
                }
                else
                {
                    Invalid("Enter a whole-number restock count.");
                }
            });

            DrawCharmBrowser();
        }

        private void DrawCharmBrowser()
        {
            Section("Charm browser");
            GUILayout.BeginHorizontal();
            GUILayout.Label("Search", labelStyle, GUILayout.Width(90f));
            charmSearch = GUILayout.TextField(charmSearch, textFieldStyle, GUILayout.ExpandWidth(true));
            charmOwnedOnly = GUILayout.Toggle(charmOwnedOnly, "Owned only", GUILayout.Width(110f));
            if (GUILayout.Button("Refresh", buttonStyle, GUILayout.Width(90f)))
            {
                RefreshCharmBrowser(showStatus: true);
            }
            GUILayout.EndHorizontal();

            int shown = 0;
            int matched = 0;
            foreach (CharmInfo info in charmInfos)
            {
                if (charmOwnedOnly && !info.Owned)
                {
                    continue;
                }

                string name = CharmDisplayName(info.Identifier);
                if (!string.IsNullOrWhiteSpace(charmSearch) &&
                    name.IndexOf(charmSearch, StringComparison.OrdinalIgnoreCase) < 0 &&
                    info.Identifier.ToString().IndexOf(charmSearch, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                matched++;
                if (shown >= 60)
                {
                    continue;
                }

                shown++;
                string state = info.Equipped ? "EQUIPPED" : info.InDrawer ? "DRAWER" : info.Unlocked ? "UNLOCKED" : "LOCKED";
                string chargeText = info.ChargesMaximum > 0
                    ? $" | charges {Mathf.Max(0, info.ChargesMaximum - info.ChargesUsed)}/{info.ChargesMaximum}"
                    : string.Empty;
                string label = $"{name}  [{state}]  | {ModifierDisplayName(info.Modifier)}{chargeText}";
                GUIStyle style = selectedCharm.HasValue && selectedCharm.Value.Equals(info.Identifier)
                    ? primaryButtonStyle
                    : buttonStyle;
                if (GUILayout.Button(label, style, GUILayout.Height(29f)))
                {
                    selectedCharm = info.Identifier;
                    discardConfirmationArmed = false;
                }
            }

            if (matched == 0)
            {
                GUILayout.Label(
                    charmOwnedOnly ? "No owned charm matches the search." : "No charm matches the search.",
                    mutedStyle);
            }
            else if (matched > shown)
            {
                GUILayout.Label($"Showing the first {shown} of {matched} matches. Refine the search to narrow the list.", warningStyle);
            }

            CharmInfo selected = FindSelectedCharm();
            if (selected == null)
            {
                GUILayout.Label("Select a charm above to unlock, equip, recharge or modify it.", mutedStyle);
                ButtonRow("Refresh list", () => RefreshCharmBrowser(showStatus: true), "Recharge all", () => RunCharmAction(actions.RechargeAllCharms()));
                return;
            }

            GUILayout.Space(5f);
            GUILayout.Label(
                $"Selected: {CharmDisplayName(selected.Identifier)} | " +
                $"Owned: {(selected.Owned ? "yes" : "no")} | Equipped: {(selected.Equipped ? "yes" : "no")} | Drawer: {(selected.InDrawer ? "yes" : "no")} | Unlocked: {(selected.Unlocked ? "yes" : "no")}",
                labelStyle);

            ButtonRow(
                "Unlock selected",
                () => RunCharmAction(actions.UnlockCharm(selected.Identifier)),
                "Add / equip selected",
                () => RunCharmAction(actions.EquipCharm(selected.Identifier)));
            ButtonRow(
                "Recharge selected",
                () => RunCharmAction(actions.RechargeCharm(selected.Identifier)),
                "Recharge all",
                () => RunCharmAction(actions.RechargeAllCharms()));

            GUILayout.Label("Selected charm modifier", mutedStyle);
            string[] modifierNames = new string[CharmModifiers.Length];
            int selectedModifierIndex = 0;
            for (int index = 0; index < CharmModifiers.Length; index++)
            {
                modifierNames[index] = ModifierDisplayName(CharmModifiers[index]);
                if (CharmModifiers[index] == selected.Modifier)
                {
                    selectedModifierIndex = index;
                }
            }

            int newModifierIndex = GUILayout.SelectionGrid(selectedModifierIndex, modifierNames, 4, tabStyle);
            if (newModifierIndex != selectedModifierIndex)
            {
                RunCharmAction(actions.SetCharmModifier(selected.Identifier, CharmModifiers[newModifierIndex]));
            }

            if (discardConfirmationArmed && Time.realtimeSinceStartup > discardConfirmationUntil)
            {
                discardConfirmationArmed = false;
            }

            string discardLabel = discardConfirmationArmed
                ? "CLICK AGAIN: DISCARD SELECTED CHARM"
                : "Discard selected charm";
            if (GUILayout.Button(discardLabel, dangerStyle, GUILayout.Height(32f)))
            {
                if (discardConfirmationArmed)
                {
                    discardConfirmationArmed = false;
                    RunCharmAction(actions.DiscardCharm(selected.Identifier));
                }
                else
                {
                    discardConfirmationArmed = true;
                    discardConfirmationUntil = Time.realtimeSinceStartup + 6f;
                    Invalid("Discard is armed for 6 seconds. This returns the charm to the unbought pool and can trigger vanilla discard effects.");
                }
            }
        }

        private void DrawMemoryCards()
        {
            Section("Multiple memory cards");
            bool enabled = GUILayout.Toggle(
                config.MultipleMemoryCardsEnabled.Value,
                "Enable additional memory cards");
            if (enabled != config.MultipleMemoryCardsEnabled.Value)
            {
                config.MultipleMemoryCardsEnabled.Value = enabled;
                MultipleMemoryCards.EnabledChanged();
                Run(ActionResult.Success(
                    enabled
                        ? "Additional memory cards enabled."
                        : "Additional memory cards disabled."));
            }

            string primaryName = MemoryCardDisplayName(MultipleMemoryCards.PrimaryCard);
            GUILayout.Label(
                $"Primary: {primaryName}  |  selected: {MultipleMemoryCards.ConfiguredCards.Count}  |  additional active: {MultipleMemoryCards.ActiveRunCards.Count}",
                mutedStyle);
            GUILayout.Label(
                "Click a card once to enable it and again to disable it. The selection is saved. A card marked PRIMARY is the normal card chosen by the game and is not counted twice.",
                mutedStyle);
            GUILayout.Label(
                "Removing a card stops its ongoing rules immediately. Bonuses already granted once (for example tickets, slots or starting coins) cannot be taken back safely and therefore remain until the next run.",
                warningStyle);

            ButtonRow(
                "Select all",
                () =>
                {
                    MultipleMemoryCards.SelectAllConfigured();
                    Run(ActionResult.Success("All memory cards selected."));
                },
                "Clear selection",
                () =>
                {
                    MultipleMemoryCards.ClearConfigured();
                    Run(ActionResult.Success("Additional memory-card selection cleared."));
                });

            Section("Card list");
            GUILayout.BeginHorizontal();
            GUILayout.Label("Search", labelStyle, GUILayout.Width(90f));
            memoryCardSearch = GUILayout.TextField(
                memoryCardSearch,
                textFieldStyle,
                GUILayout.ExpandWidth(true));
            GUILayout.EndHorizontal();

            int shown = 0;
            foreach (RunModifierScript.Identifier identifier in
                     Enum.GetValues(typeof(RunModifierScript.Identifier)))
            {
                if (!MultipleMemoryCards.IsValidCard(identifier))
                {
                    continue;
                }

                string name = MemoryCardDisplayName(identifier);
                if (!string.IsNullOrWhiteSpace(memoryCardSearch) &&
                    name.IndexOf(memoryCardSearch, StringComparison.OrdinalIgnoreCase) < 0 &&
                    identifier.ToString().IndexOf(
                        memoryCardSearch,
                        StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                shown++;
                bool configured = MultipleMemoryCards.IsConfigured(identifier);
                bool primary = MultipleMemoryCards.PrimaryCard == identifier;
                string state = primary
                    ? configured ? "PRIMARY + SAVED" : "PRIMARY"
                    : configured ? "ON" : "OFF";
                string dlc = IsFusionMemoryCard(identifier) ? " | FUSION DLC" : string.Empty;
                GUIStyle style = configured || primary ? primaryButtonStyle : buttonStyle;

                if (GUILayout.Button(
                        $"[{state}]  {name}{dlc}",
                        style,
                        GUILayout.Height(31f)))
                {
                    selectedMemoryCard = identifier;
                    if (configured)
                    {
                        MultipleMemoryCards.RemoveConfigured(identifier);
                        Run(ActionResult.Success($"Disabled additional card: {name}."));
                    }
                    else
                    {
                        MultipleMemoryCards.AddConfigured(identifier);
                        Run(ActionResult.Success($"Enabled additional card: {name}."));
                    }
                }
            }

            if (shown == 0)
            {
                GUILayout.Label("No memory card matches the search.", mutedStyle);
            }

            if (selectedMemoryCard.HasValue)
            {
                GUILayout.Space(5f);
                GUILayout.Label(
                    "Selected: " + MemoryCardDisplayName(selectedMemoryCard.Value),
                    labelStyle);
                GUILayout.Label(
                    MemoryCardDescription(selectedMemoryCard.Value),
                    statusStyle);
            }

            Section("Card inventory");
            bool unlimited = GUILayout.Toggle(
                config.UnlimitedMemoryCards.Value,
                "Unlimited memory cards (prevent spending)");
            if (unlimited != config.UnlimitedMemoryCards.Value)
            {
                config.UnlimitedMemoryCards.Value = unlimited;
                Run(ActionResult.Success(
                    unlimited
                        ? "Unlimited memory cards enabled."
                        : "Unlimited memory cards disabled."));
            }

            TextActionRow("Owned count", ref memoryCardsInput, "Set all", () =>
            {
                if (TryParseInt(memoryCardsInput, out int value))
                {
                    Run(actions.SetMemoryCardCounts(value));
                }
                else
                {
                    Invalid("Enter a whole-number memory-card count.");
                }
            });
            TextActionRow("Win count", ref memoryWinsInput, "Set all", () =>
            {
                if (TryParseInt(memoryWinsInput, out int value))
                {
                    Run(actions.SetMemoryCardWins(value));
                }
                else
                {
                    Invalid("Enter a whole-number memory-card win count.");
                }
            });
        }

        private static string MemoryCardDisplayName(
            RunModifierScript.Identifier identifier)
        {
            if (!MultipleMemoryCards.IsValidCard(identifier))
            {
                return identifier == RunModifierScript.Identifier.defaultModifier
                    ? "None"
                    : identifier.ToString();
            }

            try
            {
                return RunModifierScript.TitleGet(identifier);
            }
            catch
            {
                return identifier.ToString();
            }
        }

        private static string MemoryCardDescription(
            RunModifierScript.Identifier identifier)
        {
            try
            {
                string description = RunModifierScript.DescriptionGet(identifier);
                return TextMeshProSpriteTag.Replace(
                    description,
                    match => "[" + SplitPascalCase(match.Groups[1].Value) + "]");
            }
            catch
            {
                return identifier.ToString();
            }
        }

        private static string SplitPascalCase(string value)
        {
            return Regex.Replace(value, "(?<!^)([A-Z])", " $1");
        }

        private static bool IsFusionMemoryCard(
            RunModifierScript.Identifier identifier)
        {
            return (int)identifier >=
                   (int)RunModifierScript.Identifier.Fusion_ViciousCicle;
        }

        private void DrawSymbols()
        {
            Section("Symbol spawn weights");
            foreach (SymbolScript.Kind kind in SymbolKinds)
            {
                string input = symbolChanceInputs[kind];
                GUILayout.BeginHorizontal();
                GUILayout.Label(DisplayName(kind.ToString()), labelStyle, GUILayout.Width(150f));
                string changedInput = GUILayout.TextField(input, textFieldStyle, GUILayout.Width(120f));
                symbolChanceInputs[kind] = changedInput;
                symbolChanceLocks[kind] = GUILayout.Toggle(symbolChanceLocks[kind], "Lock", GUILayout.Width(75f));
                GUILayout.EndHorizontal();

                if (changedInput != input && TryParseFloat(changedInput, out float changedValue) &&
                    !float.IsNaN(changedValue) && !float.IsInfinity(changedValue) &&
                    changedValue >= 0f && changedValue <= 100f)
                {
                    AdjustSymbolChances(kind, changedValue);
                }
            }

            float total = 0f;
            bool allValid = TryGetSymbolChanceTotal(out total);
            GUILayout.Label(
                allValid ? $"Total: {total:0.####} / 100" : "Total: invalid input",
                allValid && Math.Abs(total - 100f) <= 0.05f ? mutedStyle : warningStyle);
            ButtonRow(
                "Load current chances",
                () => LoadCurrentSymbolChances(showStatus: true),
                "Equalize unlocked",
                EqualizeSymbolChances);
            if (GUILayout.Button("Clear all chance locks", buttonStyle, GUILayout.Height(30f)))
            {
                foreach (SymbolScript.Kind kind in SymbolKinds)
                {
                    symbolChanceLocks[kind] = false;
                }

                Run(ActionResult.Success("Cleared all symbol chance locks."));
            }
            if (GUILayout.Button("APPLY SYMBOL CHANCES", primaryButtonStyle, GUILayout.Height(34f)))
            {
                ApplySymbolChances();
            }

            GUILayout.Label("Editing one value keeps it fixed and proportionally adjusts only unlocked fields to a total of 100. Locked fields never move.", mutedStyle);

            Section("Symbol coin values");
            foreach (SymbolScript.Kind kind in SymbolKinds)
            {
                string input = symbolValueInputs[kind];
                GUILayout.BeginHorizontal();
                GUILayout.Label(DisplayName(kind.ToString()), labelStyle, GUILayout.Width(150f));
                symbolValueInputs[kind] = GUILayout.TextField(input, textFieldStyle, GUILayout.Width(180f));
                GUILayout.EndHorizontal();
            }

            ButtonRow(
                "Load current values",
                () => LoadCurrentSymbolValues(showStatus: true),
                "Reset vanilla bonuses",
                () =>
                {
                    Run(actions.ResetSymbolCoinBonuses(SymbolKinds));
                    LoadCurrentSymbolValues(showStatus: false);
                });
            if (GUILayout.Button("APPLY SYMBOL COIN VALUES", primaryButtonStyle, GUILayout.Height(34f)))
            {
                ApplySymbolValues();
            }
            GUILayout.Label("These are the base+bonus values before global symbol multipliers and charm effects.", mutedStyle);
        }

        private void DrawPatterns()
        {
            Section("Pattern values to add");
            foreach (PatternScript.Kind kind in PatternKinds)
            {
                string input = patternValueInputs[kind];
                GUILayout.BeginHorizontal();
                GUILayout.Label(PatternDisplayName(kind), labelStyle, GUILayout.Width(170f));
                input = GUILayout.TextField(input, textFieldStyle, GUILayout.Width(120f));
                patternValueInputs[kind] = input;
                GUILayout.EndHorizontal();
            }

            ButtonRow("Add values", () => ApplyPatternValues(false), "Add 10^values", () => ApplyPatternValues(true));
            GUILayout.Label("Exponential mode accepts whole-number exponents from 0 to 300.", warningStyle);
        }

        private void DrawRunTools()
        {
            Section("Current run");
            GUILayout.Label(actions.ReadRunDetails(), statusStyle);
            ButtonRow(
                "Refresh run data",
                () => LoadRunEditorValues(showStatus: true),
                "Copy seed",
                () =>
                {
                    string seed = actions.ReadSeed();
                    if (string.IsNullOrEmpty(seed))
                    {
                        Invalid("No active run seed is available.");
                    }
                    else
                    {
                        GUIUtility.systemCopyBuffer = seed;
                        Run(ActionResult.Success($"Copied seed {seed}."));
                    }
                });

            Section("Luck editor");
            TextActionRow("Activation luck", ref activationLuckInput, "Set", () => SetLuckInput(activationLuckInput, actions.SetActivationLuck));
            GUILayout.Label("Activation luck multiplies random charm activation chances. 1.0 = normal; 2.0 = roughly twice the chance.", mutedStyle);
            TextActionRow("Charm luck mult.", ref powerupLuckInput, "Set", () => SetLuckInput(powerupLuckInput, actions.SetPowerupLuck));
            GUILayout.Label("Charm luck multiplies temporary slot luck granted by charm effects. It does not directly increase every charm's activation chance.", mutedStyle);
            TextActionRow("Store luck", ref storeLuckInput, "Set", () => SetLuckInput(storeLuckInput, actions.SetStoreLuck));
            GUILayout.Label("Store luck affects luck checks during store rerolls. Higher values improve the chance of a lucky reroll result.", mutedStyle);
            ButtonRow(
                "Reload luck values",
                () => LoadRunEditorValues(showStatus: true),
                "Clear timed luck",
                () => Run(actions.ResetTemporaryLuck()));
            GUILayout.Label("Valid range for all three fields: 0.5 to 100,000. Vanilla is 1.0. CloverPit has no hard upper cap; CloverMod limits input to 100,000 for safety. Total slot luck shown above is a separate reel-luck value made from temporary and active charm effects.", warningStyle);

            Section("Run value editor");
            if (runEditConfirmation != RunEditTarget.None && Time.realtimeSinceStartup > runEditConfirmationUntil)
            {
                runEditConfirmation = RunEditTarget.None;
            }

            TextActionRow(
                "Deadline level",
                ref debtIndexInput,
                runEditConfirmation == RunEditTarget.DebtIndex ? "CONFIRM" : "Set",
                () => ConfirmRunEdit(RunEditTarget.DebtIndex));
            TextActionRow(
                "Deposit",
                ref depositInput,
                runEditConfirmation == RunEditTarget.Deposit ? "CONFIRM" : "Set",
                () => ConfirmRunEdit(RunEditTarget.Deposit));
            GUILayout.Label("Deadline level selects the debt-progression step used by the game; it does not change the current round inside that deadline. These fields directly rewrite the active run: click Set once to arm the change for 6 seconds, then click CONFIRM to apply it.", warningStyle);
        }

        private void DrawExtras()
        {
            Section("Presets");
            ButtonRow("Normal", ApplyNormalPreset, "Lucky", ApplyLuckyPreset);
            if (GUILayout.Button("Load custom preset", buttonStyle, GUILayout.Height(32f)))
            {
                LoadCustomPreset();
            }
            if (GUILayout.Button("Save current settings as custom preset", buttonStyle, GUILayout.Height(32f)))
            {
                SaveCustomPreset();
            }
            GUILayout.Label(
                "Normal: 1x speed, normal win animation, Auto off.\n" +
                "Lucky: Seven 40%, other symbols 10%, activation luck 10.\n" +
                "Custom: saves or restores your current speed, slot rules, symbol chances and locks.",
                statusStyle,
                GUILayout.Height(88f));
            GUILayout.Label("UNDO always reverses only the latest reversible value change and then becomes empty. Unlocks, achievements, charm discard and other irreversible actions are not stored in Undo.", mutedStyle);

            DrawQualityOfLife();

            Section("Quick actions");
            ButtonRow("Phone transform", () => Run(actions.TriggerPhoneTransformation()), "Ring phone", () => Run(actions.TriggerPhoneRing()));
            if (GUILayout.Button("Equip corpse pieces", buttonStyle, GUILayout.Height(32f)))
            {
                Run(actions.EquipAllCorpsePieces());
            }

            Section("Slot machine");
            bool autoSlotMode = GUILayout.Toggle(
                config.AutoSlotMode.Value,
                "Auto mode (automatically start the next spin)");
            if (autoSlotMode != config.AutoSlotMode.Value)
            {
                config.AutoSlotMode.Value = autoSlotMode;
                if (!autoSlotMode)
                {
                    SlotMachineAutoPatch.StopAutoMode();
                }
                log.LogInfo(autoSlotMode ? "Auto slot mode enabled." : "Auto slot mode disabled.");
                Run(ActionResult.Success(autoSlotMode ? "Auto mode enabled." : "Auto mode disabled."));
            }
            GUILayout.Label("Auto starts the next spin when the machine is ready. It is disabled by default and keeps the game's normal animations.", mutedStyle);

            Section("Rounds and spins");
            TextActionRow("Extra rounds", ref extraRoundsInput, "Add", () =>
            {
                if (TryParseInt(extraRoundsInput, out int value))
                {
                    Run(actions.AddExtraRounds(value));
                }
                else
                {
                    Invalid("Enter a whole-number round count.");
                }
            });
            TextActionRow("Extra spins", ref extraSpinsInput, "Add", () =>
            {
                if (TryParseInt(extraSpinsInput, out int value))
                {
                    Run(actions.AddExtraSpins(value));
                }
                else
                {
                    Invalid("Enter a whole-number spin count.");
                }
            });

            Section("Payout / transition animation speed");
            GUILayout.Label($"Current target: {transitionSpeed}x (this is separate from global game speed)", mutedStyle);
            int sliderSpeed = Mathf.RoundToInt(GUILayout.HorizontalSlider(transitionSpeed, 1f, 20f, sliderStyle, sliderThumbStyle));
            if (sliderSpeed != transitionSpeed)
            {
                ApplyTransitionSpeed(sliderSpeed);
            }
            SpeedButtons(new[] { 1, 2, 5, 10, 20 }, speed => ApplyTransitionSpeed(speed));

            Section("Global game speed");
            GUILayout.Label($"Target after closing menu: {targetGameSpeed:0.0}x", mutedStyle);
            float speedValue = GUILayout.HorizontalSlider(targetGameSpeed, 0.25f, 4f, sliderStyle, sliderThumbStyle);
            speedValue = Mathf.Round(speedValue * 4f) / 4f;
            if (!Mathf.Approximately(speedValue, targetGameSpeed))
            {
                SetGameSpeed(speedValue);
            }
            FloatSpeedButtons(new[] { 0.5f, 1f, 2f, 4f }, SetGameSpeed);

            bool pause = GUILayout.Toggle(config.PauseWhileOpen.Value, "Pause gameplay while this menu is open");
            if (pause != config.PauseWhileOpen.Value)
            {
                config.PauseWhileOpen.Value = pause;
                Time.timeScale = pause ? 0f : targetGameSpeed;
            }

            Section("Hotkeys");
            HotkeyRow("Menu", config.MenuKey.Value, RebindTarget.Menu);
            HotkeyRow("Fallback menu", config.FallbackMenuKey.Value, RebindTarget.FallbackMenu);
            if (IsRebinding)
            {
                GUILayout.Label("Press a key now. Escape cancels.", warningStyle);
            }

            Section("Achievements");
            if (achievementConfirmationArmed && Time.realtimeSinceStartup > achievementConfirmationUntil)
            {
                achievementConfirmationArmed = false;
            }

            string achievementLabel = achievementConfirmationArmed
                ? "CLICK AGAIN TO UNLOCK EVERY ACHIEVEMENT"
                : "Unlock all Steam achievements";
            if (GUILayout.Button(achievementLabel, dangerStyle, GUILayout.Height(34f)))
            {
                if (achievementConfirmationArmed)
                {
                    achievementConfirmationArmed = false;
                    Run(actions.UnlockAllAchievements());
                }
                else
                {
                    achievementConfirmationArmed = true;
                    achievementConfirmationUntil = Time.realtimeSinceStartup + 6f;
                    Invalid("Achievement unlock armed for 6 seconds. Click again to confirm.");
                }
            }
        }

        private void LoadCurrentSymbolChances(bool showStatus)
        {
            try
            {
                Dictionary<SymbolScript.Kind, float> current = new Dictionary<SymbolScript.Kind, float>();
                float total = 0f;
                foreach (SymbolScript.Kind kind in SymbolKinds)
                {
                    float value = Mathf.Max(0f, GameplayData.Symbol_Chance_Get(kind, false, false));
                    current[kind] = value;
                    total += value;
                }

                if (total <= 0f)
                {
                    if (showStatus)
                    {
                        Invalid("Current symbol chances could not be loaded.");
                    }
                    return;
                }

                float allocated = 0f;
                for (int index = 0; index < SymbolKinds.Length; index++)
                {
                    SymbolScript.Kind kind = SymbolKinds[index];
                    float percentage = index == SymbolKinds.Length - 1
                        ? 100f - allocated
                        : current[kind] * 100f / total;
                    symbolChanceInputs[kind] = FormatSymbolChance(percentage);
                    if (TryParseFloat(symbolChanceInputs[kind], out float displayed))
                    {
                        allocated += displayed;
                    }
                }

                if (showStatus)
                {
                    Run(ActionResult.Success("Loaded and normalized the current in-game symbol chances."));
                }
            }
            catch (Exception exception)
            {
                if (showStatus)
                {
                    Invalid($"Could not load symbol chances: {exception.Message}");
                }
            }
        }

        private void LoadCurrentSymbolValues(bool showStatus)
        {
            try
            {
                foreach (SymbolScript.Kind kind in SymbolKinds)
                {
                    symbolValueInputs[kind] = actions.ReadSymbolCoinValue(kind).ToString(CultureInfo.InvariantCulture);
                }

                if (showStatus)
                {
                    Run(ActionResult.Success("Loaded current symbol coin values."));
                }
            }
            catch (Exception exception)
            {
                if (showStatus)
                {
                    Invalid($"Could not load symbol coin values: {exception.Message}");
                }
            }
        }

        private void ApplySymbolValues()
        {
            Dictionary<SymbolScript.Kind, BigInteger> values = new Dictionary<SymbolScript.Kind, BigInteger>();
            foreach (SymbolScript.Kind kind in SymbolKinds)
            {
                if (!TryParseBigInteger(symbolValueInputs[kind], out BigInteger value) || value < BigInteger.Zero)
                {
                    Invalid($"Enter a whole-number coin value of zero or greater for {DisplayName(kind.ToString())}.");
                    return;
                }

                values[kind] = value;
            }

            Run(actions.SetSymbolCoinValues(values));
            if (statusSucceeded)
            {
                LoadCurrentSymbolValues(showStatus: false);
            }
        }

        private void EqualizeSymbolChances()
        {
            float lockedTotal = 0f;
            int unlockedCount = 0;
            foreach (SymbolScript.Kind kind in SymbolKinds)
            {
                if (symbolChanceLocks[kind])
                {
                    if (!TryParseFloat(symbolChanceInputs[kind], out float lockedValue))
                    {
                        Invalid($"Locked chance for {DisplayName(kind.ToString())} is invalid.");
                        return;
                    }

                    lockedTotal += lockedValue;
                }
                else
                {
                    unlockedCount++;
                }
            }

            if (unlockedCount == 0 || lockedTotal > 100f)
            {
                Invalid(unlockedCount == 0 ? "Unlock at least one symbol first." : "Locked symbol chances already exceed 100.");
                return;
            }

            float remaining = 100f - lockedTotal;
            float allocated = 0f;
            int assigned = 0;
            foreach (SymbolScript.Kind kind in SymbolKinds)
            {
                if (symbolChanceLocks[kind])
                {
                    continue;
                }

                assigned++;
                float value = assigned == unlockedCount ? remaining - allocated : remaining / unlockedCount;
                symbolChanceInputs[kind] = FormatSymbolChance(value);
                if (TryParseFloat(symbolChanceInputs[kind], out float displayed))
                {
                    allocated += displayed;
                }
            }

            Run(ActionResult.Success("Unlocked symbol chances distributed equally. Click Apply to save them."));
        }

        private void AdjustSymbolChances(SymbolScript.Kind editedKind, float editedValue)
        {
            float lockedTotal = 0f;
            int adjustableCount = 0;
            foreach (SymbolScript.Kind kind in SymbolKinds)
            {
                if (kind == editedKind)
                {
                    continue;
                }

                if (symbolChanceLocks[kind])
                {
                    if (TryParseFloat(symbolChanceInputs[kind], out float lockedValue))
                    {
                        lockedTotal += Mathf.Max(0f, lockedValue);
                    }
                }
                else
                {
                    adjustableCount++;
                }
            }

            editedValue = Mathf.Clamp(editedValue, 0f, Mathf.Max(0f, 100f - lockedTotal));
            if (adjustableCount == 0)
            {
                editedValue = Mathf.Max(0f, 100f - lockedTotal);
            }

            symbolChanceInputs[editedKind] = FormatSymbolChance(editedValue);

            float remaining = Mathf.Max(0f, 100f - editedValue - lockedTotal);
            float otherTotal = 0f;
            foreach (SymbolScript.Kind kind in SymbolKinds)
            {
                if (kind == editedKind || symbolChanceLocks[kind])
                {
                    continue;
                }

                if (TryParseFloat(symbolChanceInputs[kind], out float current) &&
                    !float.IsNaN(current) && !float.IsInfinity(current))
                {
                    otherTotal += Mathf.Max(0f, current);
                }
            }

            int adjustedCount = 0;
            float allocated = 0f;
            foreach (SymbolScript.Kind kind in SymbolKinds)
            {
                if (kind == editedKind || symbolChanceLocks[kind])
                {
                    continue;
                }

                adjustedCount++;
                float adjusted;
                if (adjustedCount == adjustableCount)
                {
                    adjusted = remaining - allocated;
                }
                else if (otherTotal <= 0f)
                {
                    adjusted = remaining / adjustableCount;
                }
                else
                {
                    TryParseFloat(symbolChanceInputs[kind], out float current);
                    adjusted = Mathf.Max(0f, current) / otherTotal * remaining;
                }

                symbolChanceInputs[kind] = FormatSymbolChance(Mathf.Max(0f, adjusted));
                if (TryParseFloat(symbolChanceInputs[kind], out float displayed))
                {
                    allocated += displayed;
                }
            }
        }

        private bool TryGetSymbolChanceTotal(out float total)
        {
            total = 0f;
            foreach (SymbolScript.Kind kind in SymbolKinds)
            {
                if (!TryParseFloat(symbolChanceInputs[kind], out float value) ||
                    float.IsNaN(value) || float.IsInfinity(value) || value < 0f || value > 100f)
                {
                    return false;
                }

                total += value;
            }

            return true;
        }

        private static string FormatSymbolChance(float value)
        {
            return value.ToString("0.####", CultureInfo.InvariantCulture);
        }

        private void ApplySymbolChances()
        {
            Dictionary<SymbolScript.Kind, float> values = new Dictionary<SymbolScript.Kind, float>();
            foreach (SymbolScript.Kind kind in SymbolKinds)
            {
                if (!TryParseFloat(symbolChanceInputs[kind], out float value))
                {
                    Invalid($"Enter a valid chance for {DisplayName(kind.ToString())}.");
                    return;
                }

                values[kind] = value;
            }

            Run(actions.SetSymbolChances(values));
        }

        private void ApplyPatternValues(bool exponentMode)
        {
            Dictionary<PatternScript.Kind, double> values = new Dictionary<PatternScript.Kind, double>();
            foreach (PatternScript.Kind kind in PatternKinds)
            {
                if (!TryParseDouble(patternValueInputs[kind], out double value))
                {
                    Invalid($"Enter a valid value for {PatternDisplayName(kind)}.");
                    return;
                }

                values[kind] = value;
            }

            Run(actions.AddPatternValues(values, exponentMode));
        }

        private void RefreshCharmBrowser(bool showStatus)
        {
            try
            {
                charmInfos.Clear();
                charmInfos.AddRange(actions.ReadCharmInfos());
                if (selectedCharm.HasValue && FindSelectedCharm() == null)
                {
                    selectedCharm = null;
                }

                if (showStatus)
                {
                    int owned = 0;
                    foreach (CharmInfo info in charmInfos)
                    {
                        if (info.Owned)
                        {
                            owned++;
                        }
                    }

                    Run(ActionResult.Success($"Charm list refreshed: {owned} owned, {charmInfos.Count} known."));
                }
            }
            catch (Exception exception)
            {
                charmInfos.Clear();
                if (showStatus)
                {
                    Invalid($"Could not load charms: {exception.Message}");
                }
            }
        }

        private CharmInfo FindSelectedCharm()
        {
            if (!selectedCharm.HasValue)
            {
                return null;
            }

            foreach (CharmInfo info in charmInfos)
            {
                if (info.Identifier.Equals(selectedCharm.Value))
                {
                    return info;
                }
            }

            return null;
        }

        private void RunCharmAction(ActionResult result)
        {
            Run(result);
            if (result.Succeeded)
            {
                RefreshCharmBrowser(showStatus: false);
            }
        }

        private void LoadRunEditorValues(bool showStatus)
        {
            bool loaded = false;
            if (actions.TryReadLuckValues(out float activation, out float powerup, out float store))
            {
                activationLuckInput = activation.ToString("0.###", CultureInfo.InvariantCulture);
                powerupLuckInput = powerup.ToString("0.###", CultureInfo.InvariantCulture);
                storeLuckInput = store.ToString("0.###", CultureInfo.InvariantCulture);
                loaded = true;
            }

            if (actions.TryReadRunEditorValues(out BigInteger debtIndex, out BigInteger deposit))
            {
                debtIndexInput = debtIndex.ToString(CultureInfo.InvariantCulture);
                depositInput = deposit.ToString(CultureInfo.InvariantCulture);
                loaded = true;
            }

            if (showStatus)
            {
                Run(loaded
                    ? ActionResult.Success("Reloaded the current run and luck values.")
                    : ActionResult.Failure("No active run data could be loaded."));
            }
        }

        private void SetLuckInput(string input, Func<float, ActionResult> setter)
        {
            if (!TryParseFloat(input, out float value))
            {
                Invalid("Enter a valid luck value.");
                return;
            }

            Run(setter(value));
            if (statusSucceeded)
            {
                LoadRunEditorValues(showStatus: false);
            }
        }

        private void ConfirmRunEdit(RunEditTarget target)
        {
            if (runEditConfirmation != target || Time.realtimeSinceStartup > runEditConfirmationUntil)
            {
                runEditConfirmation = target;
                runEditConfirmationUntil = Time.realtimeSinceStartup + 6f;
                Run(ActionResult.Success("Safety confirmation active for 6 seconds. Click CONFIRM again to apply this direct run change."));
                return;
            }

            runEditConfirmation = RunEditTarget.None;
            string input = target == RunEditTarget.DebtIndex ? debtIndexInput : depositInput;
            if (!TryParseBigInteger(input, out BigInteger value))
            {
                Invalid("Enter a whole number of zero or greater.");
                return;
            }

            Run(target == RunEditTarget.DebtIndex ? actions.SetDeadlineLevel(value) : actions.SetDeposit(value));
            if (statusSucceeded)
            {
                LoadRunEditorValues(showStatus: false);
            }
        }

        private void ApplyNormalPreset()
        {
            config.AutoSlotMode.Value = false;
            SlotMachineAutoPatch.StopAutoMode();
            SetGameSpeed(1f);
            ApplyTransitionSpeed(1);
            if (!statusSucceeded)
            {
                return;
            }
            foreach (SymbolScript.Kind kind in SymbolKinds)
            {
                symbolChanceLocks[kind] = false;
            }

            LoadCurrentSymbolChances(showStatus: false);
            Run(ActionResult.Success("Normal preset applied: 1x speed, vanilla win animations and Auto off."));
        }

        private void ApplyLuckyPreset()
        {
            config.AutoSlotMode.Value = false;
            SlotMachineAutoPatch.StopAutoMode();
            SetGameSpeed(1f);
            ApplyTransitionSpeed(1);
            if (!statusSucceeded)
            {
                return;
            }
            foreach (SymbolScript.Kind kind in SymbolKinds)
            {
                symbolChanceLocks[kind] = false;
                symbolChanceInputs[kind] = kind == SymbolScript.Kind.seven ? "40" : "10";
            }

            ApplySymbolChances();
            if (!statusSucceeded)
            {
                return;
            }
            Run(actions.SetActivationLuck(10f));
            if (statusSucceeded)
            {
                LoadRunEditorValues(showStatus: false);
                Run(ActionResult.Success("Lucky preset applied: Seven 40%, every other symbol 10%, activation luck 10."));
            }
        }

        private void SaveCustomPreset()
        {
            string[] chances = new string[SymbolKinds.Length];
            string[] locks = new string[SymbolKinds.Length];
            for (int index = 0; index < SymbolKinds.Length; index++)
            {
                chances[index] = symbolChanceInputs[SymbolKinds[index]];
                locks[index] = symbolChanceLocks[SymbolKinds[index]] ? "1" : "0";
            }

            config.CustomPreset.Value = string.Join(";", new[]
            {
                "v1",
                "game=" + targetGameSpeed.ToString("0.###", CultureInfo.InvariantCulture),
                "transition=" + transitionSpeed.ToString(CultureInfo.InvariantCulture),
                "auto=" + (config.AutoSlotMode.Value ? "1" : "0"),
                "chances=" + string.Join(",", chances),
                "locks=" + string.Join(",", locks),
            });
            Run(ActionResult.Success("Saved the current settings as the custom preset."));
        }

        private void LoadCustomPreset()
        {
            string preset = config.CustomPreset.Value;
            if (string.IsNullOrWhiteSpace(preset))
            {
                Invalid("No custom preset has been saved yet.");
                return;
            }

            try
            {
                Dictionary<string, string> fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                string[] parts = preset.Split(';');
                foreach (string part in parts)
                {
                    int separator = part.IndexOf('=');
                    if (separator > 0)
                    {
                        fields[part.Substring(0, separator)] = part.Substring(separator + 1);
                    }
                }

                if (!fields.TryGetValue("game", out string gameText) || !TryParseFloat(gameText, out float game) ||
                    !fields.TryGetValue("transition", out string transitionText) || !TryParseInt(transitionText, out int animation) ||
                    !fields.TryGetValue("chances", out string chanceText) ||
                    !fields.TryGetValue("locks", out string lockText))
                {
                    throw new FormatException("Required preset values are missing.");
                }

                string[] chances = chanceText.Split(',');
                string[] locks = lockText.Split(',');
                if (chances.Length != SymbolKinds.Length || locks.Length != SymbolKinds.Length)
                {
                    throw new FormatException("The symbol list does not match this game version.");
                }

                SetGameSpeed(game);
                ApplyTransitionSpeed(Mathf.Clamp(animation, 1, 20));
                config.AutoSlotMode.Value = ReadPresetBool(fields, "auto");

                float chanceTotal = 0f;
                for (int index = 0; index < SymbolKinds.Length; index++)
                {
                    if (!TryParseFloat(chances[index], out float chance) || chance < 0f || chance > 100f)
                    {
                        throw new FormatException("A saved symbol chance is invalid.");
                    }

                    symbolChanceInputs[SymbolKinds[index]] = FormatSymbolChance(chance);
                    symbolChanceLocks[SymbolKinds[index]] = locks[index] == "1";
                    chanceTotal += chance;
                }

                if (Math.Abs(chanceTotal - 100f) > 0.05f)
                {
                    throw new FormatException($"Saved symbol chances total {chanceTotal:0.###} instead of 100.");
                }

                ApplySymbolChances();
                if (!statusSucceeded)
                {
                    return;
                }
                if (!config.AutoSlotMode.Value)
                {
                    SlotMachineAutoPatch.StopAutoMode();
                }
                Run(ActionResult.Success("Custom preset loaded and applied."));
            }
            catch (Exception exception)
            {
                Invalid($"Could not load custom preset: {exception.Message}");
            }
        }

        private static bool ReadPresetBool(IReadOnlyDictionary<string, string> fields, string key)
        {
            return fields.TryGetValue(key, out string value) && value == "1";
        }

        private void DrawQualityOfLife()
        {
            Section("Quality of life (opt-in)");
            ConfigToggle(config.AutoSkipIntro, "Auto-skip intro scene");
            ConfigToggle(config.AutoCompleteCorpse, "Auto-fill missing corpse pieces into free drawers");
            ConfigToggle(config.SkipMemoryPackPunch, "Skip memory-pack punch animation");
            ConfigToggle(config.AutoFlipMemoryPackCards, "Auto-flip cards during memory-pack deals");
            ConfigToggle(config.FastMemoryPackFlow, "Fast memory-pack waits and auto-continue");
            GUILayout.Label("Fast pack flow leaves the Yes/No deal dialogue to you. All QoL switches are disabled by default.", mutedStyle);

            ConfigToggle(config.UsePhaseSpeedProfiles, "Use automatic phase speed profiles");
            if (config.UsePhaseSpeedProfiles.Value)
            {
                ConfigSpeedSlider("Normal game + animation", config.NormalPhaseSpeed, 4);
                ConfigSpeedSlider("Gambling animation", config.GamblingAnimationSpeed, 20);
                ConfigSpeedSlider("After first jackpot animation", config.JackpotAnimationSpeed, 20);
                ConfigSpeedSlider("Cutscene game", config.CutsceneGameSpeed, 4);
                ConfigSpeedSlider("Charm-discard burst", config.CharmDiscardSpeed, 4);
                GUILayout.Label("Phase profiles override the two manual speed sliders while enabled. Active payout/charm animations are still temporarily capped at safe 4x.", warningStyle);
            }
        }

        private void ConfigToggle(ConfigEntry<bool> entry, string label)
        {
            bool value = GUILayout.Toggle(entry.Value, label);
            if (value == entry.Value)
            {
                return;
            }

            entry.Value = value;
            Run(ActionResult.Success($"{label}: {(value ? "enabled" : "disabled")}."));
        }

        private void ConfigSpeedSlider(string label, ConfigEntry<int> entry, int maximum)
        {
            GUILayout.Label($"{label}: {entry.Value}x", mutedStyle);
            int value = Mathf.RoundToInt(GUILayout.HorizontalSlider(
                entry.Value,
                1f,
                maximum,
                sliderStyle,
                sliderThumbStyle));
            if (value != entry.Value)
            {
                entry.Value = value;
            }
        }

        private void ApplyTransitionSpeed(int speed)
        {
            ActionResult result = actions.SetTransitionSpeed(speed);
            if (result.Succeeded)
            {
                transitionSpeed = speed;
            }

            Run(result);
        }

        private void SetGameSpeed(float speed)
        {
            targetGameSpeed = Mathf.Clamp(speed, 0.25f, 4f);
            gameSpeedChanged = true;
            if (!config.PauseWhileOpen.Value)
            {
                Time.timeScale = targetGameSpeed;
            }

            Run(ActionResult.Success($"Global game speed target set to {targetGameSpeed:0.00}x."));
        }

        private void HotkeyRow(string label, KeyCode key, RebindTarget target)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, labelStyle, GUILayout.Width(170f));
            GUILayout.Label(key.ToString(), mutedStyle, GUILayout.Width(130f));
            string button = rebindTarget == target ? "Press key..." : "Change";
            if (GUILayout.Button(button, buttonStyle, GUILayout.Width(120f)))
            {
                rebindTarget = rebindTarget == target ? RebindTarget.None : target;
            }
            GUILayout.EndHorizontal();
        }

        private void TextActionRow(
            string label,
            ref string input,
            string actionLabel,
            Action action,
            string secondaryLabel = null,
            Action secondaryAction = null)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, labelStyle, GUILayout.Width(150f));
            input = GUILayout.TextField(input, textFieldStyle, GUILayout.Width(150f));
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(actionLabel, buttonStyle, GUILayout.Width(110f)))
            {
                action();
            }

            if (secondaryLabel != null && GUILayout.Button(secondaryLabel, buttonStyle, GUILayout.Width(110f)))
            {
                secondaryAction?.Invoke();
            }
            GUILayout.EndHorizontal();
        }

        private void ButtonRow(string firstLabel, Action first, string secondLabel, Action second)
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(firstLabel, buttonStyle, GUILayout.Height(32f)))
            {
                first();
            }
            if (GUILayout.Button(secondLabel, buttonStyle, GUILayout.Height(32f)))
            {
                second();
            }
            GUILayout.EndHorizontal();
        }

        private void SpeedButtons(IEnumerable<int> speeds, Action<int> apply)
        {
            GUILayout.BeginHorizontal();
            foreach (int speed in speeds)
            {
                if (GUILayout.Button(speed + "x", buttonStyle))
                {
                    apply(speed);
                }
            }
            GUILayout.EndHorizontal();
        }

        private void FloatSpeedButtons(IEnumerable<float> speeds, Action<float> apply)
        {
            GUILayout.BeginHorizontal();
            foreach (float speed in speeds)
            {
                if (GUILayout.Button(speed.ToString("0.#", CultureInfo.InvariantCulture) + "x", buttonStyle))
                {
                    apply(speed);
                }
            }
            GUILayout.EndHorizontal();
        }

        private void Section(string title)
        {
            GUILayout.Space(7f);
            GUILayout.Label(title.ToUpperInvariant(), sectionStyle);
            GUILayout.Space(3f);
        }

        private void Run(ActionResult result)
        {
            SetStatus(result);
        }

        private void Invalid(string message)
        {
            SetStatus(ActionResult.Failure(message));
        }

        private void SetStatus(ActionResult result)
        {
            statusSucceeded = result.Succeeded;
            statusMessage = result.Message;
        }

        private void EnsureStyles()
        {
            if (stylesReady)
            {
                return;
            }

            overlayTexture = MakeTexture(new Color(0.01f, 0.015f, 0.02f, 0.72f));
            Texture2D panel = MakeTexture(new Color(0.055f, 0.065f, 0.075f, 0.98f));
            Texture2D button = MakeTexture(new Color(0.12f, 0.15f, 0.17f, 1f));
            Texture2D buttonHover = MakeTexture(new Color(0.18f, 0.38f, 0.25f, 1f));
            Texture2D primary = MakeTexture(new Color(0.10f, 0.46f, 0.25f, 1f));
            Texture2D tab = MakeTexture(new Color(0.09f, 0.11f, 0.13f, 1f));
            Texture2D slider = MakeTexture(new Color(0.10f, 0.14f, 0.16f, 1f));
            Texture2D thumb = MakeTexture(new Color(0.18f, 0.72f, 0.36f, 1f));

            panelStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = panel },
                padding = new RectOffset(12, 12, 12, 12),
            };
            headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 27,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.55f, 1f, 0.67f) },
            };
            sectionStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.45f, 0.95f, 0.58f) },
            };
            labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                normal = { textColor = Color.white },
            };
            mutedStyle = new GUIStyle(labelStyle)
            {
                fontSize = 14,
                wordWrap = true,
                normal = { textColor = new Color(0.68f, 0.72f, 0.75f) },
            };
            warningStyle = new GUIStyle(mutedStyle)
            {
                normal = { textColor = new Color(1f, 0.78f, 0.25f) },
            };
            dangerStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1f, 0.55f, 0.55f), background = button },
                hover = { textColor = Color.white, background = MakeTexture(new Color(0.55f, 0.12f, 0.12f, 1f)) },
            };
            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 15,
                normal = { textColor = Color.white, background = button },
                hover = { textColor = Color.white, background = buttonHover },
                active = { textColor = Color.white, background = primary },
            };
            primaryButtonStyle = new GUIStyle(buttonStyle)
            {
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white, background = primary },
            };
            tabStyle = new GUIStyle(buttonStyle)
            {
                fontSize = 14,
                normal = { textColor = new Color(0.78f, 0.82f, 0.84f), background = tab },
                onNormal = { textColor = Color.white, background = primary },
                onHover = { textColor = Color.white, background = buttonHover },
            };
            textFieldStyle = new GUIStyle(GUI.skin.textField)
            {
                fontSize = 16,
                normal = { textColor = Color.white, background = tab },
                focused = { textColor = Color.white, background = button },
            };
            statusStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleLeft,
                wordWrap = true,
                padding = new RectOffset(8, 8, 4, 4),
                normal = { background = tab },
            };
            sliderStyle = new GUIStyle(GUI.skin.horizontalSlider)
            {
                normal = { background = slider },
                fixedHeight = 8f,
            };
            sliderThumbStyle = new GUIStyle(GUI.skin.horizontalSliderThumb)
            {
                normal = { background = thumb },
                fixedWidth = 18f,
                fixedHeight = 18f,
            };

            stylesReady = true;
        }

        private Texture2D MakeTexture(Color color)
        {
            Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            textures.Add(texture);
            return texture;
        }

        private static bool TryParseBigInteger(string input, out BigInteger value)
        {
            return BigInteger.TryParse(input, NumberStyles.Integer | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out value) ||
                   BigInteger.TryParse(input, NumberStyles.Integer | NumberStyles.AllowThousands, CultureInfo.CurrentCulture, out value);
        }

        private static bool TryParseInt(string input, out int value)
        {
            return int.TryParse(input, NumberStyles.Integer | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out value) ||
                   int.TryParse(input, NumberStyles.Integer | NumberStyles.AllowThousands, CultureInfo.CurrentCulture, out value);
        }

        private static bool TryParseLong(string input, out long value)
        {
            return long.TryParse(input, NumberStyles.Integer | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out value) ||
                   long.TryParse(input, NumberStyles.Integer | NumberStyles.AllowThousands, CultureInfo.CurrentCulture, out value);
        }

        private static bool TryParseFloat(string input, out float value)
        {
            return float.TryParse(input, NumberStyles.Float, CultureInfo.InvariantCulture, out value) ||
                   float.TryParse(input, NumberStyles.Float, CultureInfo.CurrentCulture, out value);
        }

        private static bool TryParseDouble(string input, out double value)
        {
            return double.TryParse(input, NumberStyles.Float, CultureInfo.InvariantCulture, out value) ||
                   double.TryParse(input, NumberStyles.Float, CultureInfo.CurrentCulture, out value);
        }

        private static string DisplayName(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            return char.ToUpperInvariant(value[0]) + value.Substring(1);
        }

        private static string CharmDisplayName(PowerupScript.Identifier identifier)
        {
            string value = identifier.ToString().Replace('_', ' ');
            System.Text.StringBuilder result = new System.Text.StringBuilder(value.Length + 8);
            for (int index = 0; index < value.Length; index++)
            {
                char character = value[index];
                if (index > 0 && char.IsUpper(character) && value[index - 1] != ' ' && !char.IsUpper(value[index - 1]))
                {
                    result.Append(' ');
                }

                result.Append(character);
            }

            return result.ToString();
        }

        private static string ModifierDisplayName(PowerupScript.Modifier modifier)
        {
            switch (modifier)
            {
                case PowerupScript.Modifier.symbolMultiplier:
                    return "Symbol mult.";
                case PowerupScript.Modifier.patternMultiplier:
                    return "Pattern mult.";
                case PowerupScript.Modifier.cloverTicket:
                    return "Clover ticket";
                default:
                    return DisplayName(modifier.ToString());
            }
        }

        private static string PatternDisplayName(PatternScript.Kind kind)
        {
            switch (kind)
            {
                case PatternScript.Kind.horizontal3:
                    return "3 horizontal";
                case PatternScript.Kind.horizontal4:
                    return "4 horizontal";
                case PatternScript.Kind.horizontal5:
                    return "5 horizontal";
                case PatternScript.Kind.vertical3:
                    return "3 vertical";
                case PatternScript.Kind.diagonal3:
                    return "3 diagonal";
                case PatternScript.Kind.pyramidInverted:
                    return "Pyramid inverted";
                case PatternScript.Kind.triangleInverted:
                    return "Triangle inverted";
                default:
                    return DisplayName(kind.ToString());
            }
        }

        private enum RebindTarget
        {
            None,
            Menu,
            FallbackMenu,
        }

        private enum RunEditTarget
        {
            None,
            DebtIndex,
            Deposit,
        }
    }
}
