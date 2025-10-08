using BepInEx;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace CloverMod
{
    [BepInPlugin("Clovermod", "Clover Mod", "1.0.2")]
    public class StarterPlugin : BaseUnityPlugin
    {
        #region Constants
        private const float MENU_WIDTH = 400f;
        private const float MENU_HEIGHT = 600f;
        private const float MENU_PADDING = 10f;
        private const float SECTION_INDENT = 20f;
        private const int INT_MAX_SAFE = int.MaxValue - 500;
        #endregion

        #region Reflection Cache
        private Type gameplayDataType;
        private Type cameraDataType;
        private MethodInfo coinsAddMethodInfo;
        private MethodInfo cloverTicketsAddMethodInfo;
        private MethodInfo allPatternMultiplierAddInfo;
        private MethodInfo allSymbolsMultiplierAddInfo;
        private MethodInfo interestRateSetMethodInfo;
        private MethodInfo addCharmSlotInfo;
        private MethodInfo patternValueAdd;
        private MethodInfo symbolChanceSetMethod;
        private MethodInfo addFreeRestockMethod;
        private MethodInfo extraSpinsAddMethod;
        private MethodInfo extraRoundsAddMethod;
        #endregion

        #region UI State
        private bool showUI = false;
        private Vector2 scrollPosition;
        private bool waitingForKey = false;
        private KeyCode openMenu = KeyCode.F2;
        private MethodInfo freezeCursor;
        private MethodInfo unfreezeCursor;
        CameraController cameraInstance = CameraController.instance;

        // Cached styles (created once)
        private GUIStyle warningStyle;
        private GUIStyle cautionStyle;
        private GUIStyle headerStyle;
        private GUIStyle label;
        private GUIStyle credit;

        // Foldout states
        private Dictionary<string, bool> foldoutStates;
        #endregion

        #region Input Fields
        private string coinsStr = "1";
        private string coinsExponentStr = "1";
        private string cloverTicketsStr = "1";
        private string patternMultiplierStr = "1";
        private string patternMultiplierMathStr = "1";
        private string symbolMultiplierStr = "1";
        private string symbolMultiplierMathStr = "1";
        private string interestRateStr = "1.0";
        private string sixChanceStr = "1.0";
        private string redButtonStr = "1";
        private string endingCounterStr = "10";
        private string charmSlotsStr = "20";
        private string freeRestockStr = "25";
        private string extraRoundsStr = "2";
        private string extraSpinsStr = "2";
        private string amountOfWins = "10";

        private Dictionary<string, string> symbolChances;
        private Dictionary<string, string> patternValues;
        #endregion

        #region Cursor State
        private CursorLockMode previousLockState;
        private bool previousCursorVisible;
        private float desiredTimeScale = 1.0f;
        private float desiredAnimationTimeScale = 1.0f;
        private string openMenuKeyPref = "OpenMenuKey";
        #endregion

        private void Awake()
        {
            Logger.LogInfo("Clover Mod v1.0.1 successfully loaded!");

            InitializeReflection();
            InitializeFoldouts();
            InitializeInputDictionaries();

            // Load saved key from PlayerPrefs
            if (PlayerPrefs.HasKey(openMenuKeyPref))
            {
                string savedKey = PlayerPrefs.GetString(openMenuKeyPref);
                if (System.Enum.TryParse(savedKey, out KeyCode key))
                {
                    openMenu = key;
                }
            }

            desiredTimeScale = Time.timeScale;

           credit = new GUIStyle
            {
                fontSize = 14,
                normal = { textColor = Color.white },
                alignment = TextAnchor.LowerRight
            };
        }

        private void InitializeReflection()
        {
            gameplayDataType = typeof(GameplayData);
            cameraDataType = typeof(CameraController);

            const BindingFlags publicStatic = BindingFlags.Public | BindingFlags.Static;
            const BindingFlags privateStatic = BindingFlags.NonPublic | BindingFlags.Static;

            coinsAddMethodInfo = gameplayDataType.GetMethod("CoinsAdd", publicStatic);
            allPatternMultiplierAddInfo = gameplayDataType.GetMethod("AllPatternsMultiplierAdd", publicStatic);
            allSymbolsMultiplierAddInfo = gameplayDataType.GetMethod("AllSymbolsMultiplierAdd", publicStatic);
            cloverTicketsAddMethodInfo = gameplayDataType.GetMethod("CloverTicketsAdd", publicStatic);
            interestRateSetMethodInfo = gameplayDataType.GetMethod("InterestRateSet", publicStatic);
            addCharmSlotInfo = gameplayDataType.GetMethod("MaxEquippablePowerupsAdd", publicStatic);
            patternValueAdd = gameplayDataType.GetMethod("Pattern_ValueExtra_Add", publicStatic);
            symbolChanceSetMethod = gameplayDataType.GetMethod("Symbol_Chance_Set", privateStatic);
            addFreeRestockMethod = gameplayDataType.GetMethod("StoreFreeRestocksSet", publicStatic);
            extraSpinsAddMethod = gameplayDataType.GetMethod("ExtraSpinsAdd", publicStatic);
            extraRoundsAddMethod = gameplayDataType.GetMethod("DeadlineRoundsIncrement_Manual", publicStatic);
            freezeCursor = cameraDataType.GetMethod("DisableReason_Add", publicStatic);
            unfreezeCursor = cameraDataType.GetMethod("DisableReason_Remove", publicStatic);
        }

        private void InitializeFoldouts()
        {
            foldoutStates = new Dictionary<string, bool>
            {
                { "currency", false },
                { "multipliers", false },
                { "rates", false },
                { "charms", false },
                { "symbols", false },
                { "patterns", false },
                { "extras", false }
            };
        }

        private void InitializeInputDictionaries()
        {
            symbolChances = new Dictionary<string, string>
            {
                { "lemon", "20" },
                { "cherry", "20" },
                { "clover", "20" },
                { "bell", "10" },
                { "diamond", "10" },
                { "coins", "10" },
                { "seven", "10" }
            };

            patternValues = new Dictionary<string, string>
            {
                { "horizontal3", "1" },
                { "horizontal4", "1" },
                { "horizontal5", "1" },
                { "vertical3", "1" },
                { "diagonal3", "1" },
                { "pyramid", "1" },
                { "pyramidInverted", "1" },
                { "triangle", "1" },
                { "triangleInverted", "1" },
                { "eye", "1" },
                { "jackpot", "1" }
            };
        }

        private void Update()
        {
            if (waitingForKey)
            {
                foreach (KeyCode key in System.Enum.GetValues(typeof(KeyCode)))
                {
                    if (Input.GetKeyDown(key))
                    {
                        openMenu = key;
                        PlayerPrefs.SetString(openMenuKeyPref, key.ToString());
                        PlayerPrefs.Save();
                        Logger.LogInfo($"Menu open key set to {key}");
                        waitingForKey = false;
                        break;
                    }
                }
            }
            else if (Input.GetKeyDown(openMenu))
            {
                ToggleUI();
                
            }

            if (showUI)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

        }

        private void ToggleUI()
        {
            showUI = !showUI;

            if (showUI)
            {
                previousLockState = Cursor.lockState;
                previousCursorVisible = Cursor.visible;
                desiredTimeScale = Time.timeScale;
                Time.timeScale = 0f;

                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;

                freezeCursor?.Invoke(cameraInstance, new object[] { "reason" });


                Logger.LogInfo("Clover Mod UI opened!");
            }
            else
            {
                Cursor.lockState = previousLockState;
                Cursor.visible = previousCursorVisible;
                Time.timeScale = desiredTimeScale;
                unfreezeCursor?.Invoke(cameraInstance, new object[] { "reason" });
                Logger.LogInfo("Clover Mod UI closed!");
            }
        }

        private void OnGUI()
        {
            if (!showUI) return;

            GUI.Label(new Rect(Screen.width - 150, Screen.height - 30, 140, 20), "Clover Mod v1.0.1", credit);
            InitializeStyles();
            DrawMainMenu();
        }

        private void InitializeStyles()
        {
            if (warningStyle == null)
            {
                warningStyle = new GUIStyle(GUI.skin.label)
                {
                    normal = { textColor = Color.red },
                    fontSize = 22,
                    fontStyle = FontStyle.Bold,
                    richText = true
                };
            }

            if (cautionStyle == null)
            {
                cautionStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 18,
                    richText = true
                };
            }

            if (headerStyle == null)
            {
                headerStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 25,
                    fontStyle = FontStyle.Bold
                };
            }
            if (label == null)
            {
                label = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 20,
                    fontStyle = FontStyle.Bold
                };
            }
        }

        private void DrawMainMenu()
        {
            Rect menuRect = new Rect(MENU_PADDING, Screen.height - MENU_HEIGHT - MENU_PADDING, MENU_WIDTH, MENU_HEIGHT);
            GUI.Box(menuRect, "Clover Mod Menu v1.0.1");

            GUILayout.BeginArea(new Rect(
                menuRect.x + MENU_PADDING,
                menuRect.y + MENU_PADDING * 2,
                menuRect.width - MENU_PADDING * 2,
                menuRect.height - MENU_PADDING * 4
            ));

            scrollPosition = GUILayout.BeginScrollView(scrollPosition);

            GUILayout.Label("<color=red><b>Warning:</b> High values may Lag/Crash the game! Use at your own risk.</color>", warningStyle);
            GUILayout.Label("Clover Mod Controls", headerStyle);
            GUILayout.Space(10);

            DrawCurrencySection();
            DrawMultipliersSection();
            DrawRatesSection();
            DrawCharmsSection();
            DrawSymbolsSection();
            DrawPatternsSection();
            DrawExtrasSection();

            GUILayout.Space(20);
            if (GUILayout.Button("Close Menu"))
            {
                ToggleUI();
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        #region UI Section Helpers
        private bool DrawFoldoutSection(string key, string title)
        {
            string toggleText = foldoutStates[key] ? $"- {title}" : $"+ {title}";
            if (GUILayout.Button(toggleText))
            {
                foldoutStates[key] = !foldoutStates[key];
            }
            return foldoutStates[key];
        }

        private void BeginIndentedSection()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(SECTION_INDENT);
            GUILayout.BeginVertical();
        }

        private void EndIndentedSection()
        {
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
            GUILayout.Space(10);
        }

        private void DrawCautionLabel()
        {
            GUILayout.Label("<color=yellow><b>⚠ Warning:</b> Entering a high number may cause crashes or instability.</color>", cautionStyle);
        }

        private bool ShouldSkipValue(int value, string fieldName)
        {
            if (value == 1)
            {
                Logger.LogWarning($"Skipping {fieldName} - value is 1 (would have no effect)");
                return true;
            }
            return false;
        }

        private bool ShouldSkipValue(float value, string fieldName)
        {
            if (Mathf.Approximately(value, 1.0f))
            {
                Logger.LogWarning($"Skipping {fieldName} - value is 1.0 (would have no effect)");
                return true;
            }
            return false;
        }

        private bool ShouldSkipValue(double value, string fieldName)
        {
            if (Math.Abs(value - 1.0) < 0.0001)
            {
                Logger.LogWarning($"Skipping {fieldName} - value is 1.0 (would have no effect)");
                return true;
            }
            return false;
        }
        #endregion

        #region Currency Section
        private void DrawCurrencySection()
        {
            if (!DrawFoldoutSection("currency", "Currency Cheats")) return;

            BeginIndentedSection();

            GUILayout.Label($"Add Normal coins (Limit: {int.MaxValue:N0})");
            coinsStr = GUILayout.TextField(coinsStr);
            if (GUILayout.Button("Add Coins"))
            {
                ExecuteWithMethodCheck(coinsAddMethodInfo, "CoinsAdd", () =>
                {
                    int value = ParseIntOrDefault(coinsStr, 1);
                    if (!ShouldSkipValue(value, "coins"))
                    {
                        CoinCheat.AddMaxCoins(coinsAddMethodInfo, value);
                        Logger.LogInfo($"Added {value:N0} coins!");
                    }
                });
            }

            GUILayout.Label("Add E+ Coins: Input 10 = E+10");
            coinsExponentStr = GUILayout.TextField(coinsExponentStr);
            DrawCautionLabel();
            if (GUILayout.Button("Add E+ Coins"))
            {
                ExecuteWithMethodCheck(coinsAddMethodInfo, "CoinsAdd", () =>
                {
                    if (int.TryParse(coinsExponentStr, out int value))
                    {
                        if (!ShouldSkipValue(value, "E+ coins"))
                        {
                            CoinCheat.AddMaxCoinsMath(coinsAddMethodInfo, value);
                            Logger.LogInfo($"Added E+{value} coins!");
                        }
                    }
                    else
                    {
                        Logger.LogWarning("Invalid input for E+ coins.");
                    }
                });
            }

            GUILayout.Label("Times to add max clover tickets");
            cloverTicketsStr = GUILayout.TextField(cloverTicketsStr);
            if (GUILayout.Button("Add Clover Tickets"))
            {
                ExecuteWithMethodCheck(cloverTicketsAddMethodInfo, "CloverTicketsAdd", () =>
                {
                    int value = ParseIntOrDefault(cloverTicketsStr, int.MaxValue);
                    if (!ShouldSkipValue(value, "clover tickets"))
                    {
                        CloverTicketCheat.AddMaxCloverTickets(cloverTicketsAddMethodInfo, value);
                        Logger.LogInfo($"Added clover tickets (x{value})!");
                    }
                });
            }

            EndIndentedSection();
        }
        #endregion

        #region Multipliers Section
        private void DrawMultipliersSection()
        {
            if (!DrawFoldoutSection("multipliers", "Multiplier Cheats")) return;

            BeginIndentedSection();

            GUILayout.Label($"Pattern multiplier (Limit: {int.MaxValue:N0})");
            patternMultiplierStr = GUILayout.TextField(patternMultiplierStr);
            if (GUILayout.Button("Add Pattern Multiplier"))
            {
                ExecuteWithMethodCheck(allPatternMultiplierAddInfo, "AllPatternsMultiplierAdd", () =>
                {
                    int value = ParseIntOrDefault(patternMultiplierStr, int.MaxValue);
                    if (!ShouldSkipValue(value, "pattern multiplier"))
                    {
                        MultiplierCheat.AddMaxPatternMultiplier(allPatternMultiplierAddInfo, value);
                        Logger.LogInfo("Pattern multiplier added!");
                    }
                });
            }

            GUILayout.Label("Add E+ Pattern Multiplier: Input 10 = E+10");
            patternMultiplierMathStr = GUILayout.TextField(patternMultiplierMathStr);
            DrawCautionLabel();
            if (GUILayout.Button("Add E+ Pattern Multiplier"))
            {
                ExecuteWithMethodCheck(allPatternMultiplierAddInfo, "AllPatternsMultiplierAdd", () =>
                {
                    if (int.TryParse(patternMultiplierMathStr, out int value))
                    {
                        if (!ShouldSkipValue(value, "E+ pattern multiplier"))
                        {
                            MultiplierCheat.AddMaxPatternMultiplierMath(allPatternMultiplierAddInfo, value);
                            Logger.LogInfo($"Added E+{value} pattern multiplier!");
                        }
                    }
                    else
                    {
                        Logger.LogWarning("Invalid input for E+ pattern multiplier.");
                    }
                });
            }

            GUILayout.Label($"Symbol multiplier (Limit: {int.MaxValue:N0})");
            symbolMultiplierStr = GUILayout.TextField(symbolMultiplierStr);
            if (GUILayout.Button("Add Symbol Multiplier"))
            {
                ExecuteWithMethodCheck(allSymbolsMultiplierAddInfo, "AllSymbolsMultiplierAdd", () =>
                {
                    int value = ParseIntOrDefault(symbolMultiplierStr, int.MaxValue);
                    if (!ShouldSkipValue(value, "symbol multiplier"))
                    {
                        MultiplierCheat.AddMaxSymbolMultiplier(allSymbolsMultiplierAddInfo, value);
                        Logger.LogInfo("Symbol multiplier added!");
                    }
                });
            }

            GUILayout.Label("Add E+ Symbol Multiplier: Input 10 = E+10");
            symbolMultiplierMathStr = GUILayout.TextField(symbolMultiplierMathStr);
            DrawCautionLabel();
            if (GUILayout.Button("Add E+ Symbol Multiplier"))
            {
                ExecuteWithMethodCheck(allSymbolsMultiplierAddInfo, "AllSymbolsMultiplierAdd", () =>
                {
                    if (int.TryParse(symbolMultiplierMathStr, out int value))
                    {
                        if (!ShouldSkipValue(value, "E+ symbol multiplier"))
                        {
                            MultiplierCheat.AddMaxSymbolMultiplierMath(allSymbolsMultiplierAddInfo, value);
                            Logger.LogInfo($"Added E+{value} symbol multiplier!");
                        }
                    }
                    else
                    {
                        Logger.LogWarning("Invalid input for E+ symbol multiplier.");
                    }
                });
            }

            EndIndentedSection();
        }
        #endregion

        #region Rates Section
        private void DrawRatesSection()
        {
            if (!DrawFoldoutSection("rates", "Interest and 666 Cheats")) return;

            BeginIndentedSection();

            GUILayout.Label("Interest rate value (Limit: 100)");
            interestRateStr = GUILayout.TextField(interestRateStr);
            if (GUILayout.Button("Set Interest Rate"))
            {
                ExecuteWithMethodCheck(interestRateSetMethodInfo, "InterestRateSet", () =>
                {
                    if (float.TryParse(interestRateStr, out float value))
                    {
                        if (!ShouldSkipValue(value, "interest rate"))
                        {
                            InterestRateCheat.SetMaxInterestRate(interestRateSetMethodInfo, value);
                            Logger.LogInfo($"Interest rate set to {value}!");
                        }
                    }
                    else
                    {
                        Logger.LogWarning("Invalid input for interest rate.");
                    }
                });
            }

            GUILayout.Label("Set 666 Chance (0-100)");
            GUILayout.Label("Max 666 Chance is Hardcoded to 30 so you need to Change it.");
            sixChanceStr = GUILayout.TextField(sixChanceStr);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Set 666 Max Chance"))
            {
                if (float.TryParse(sixChanceStr, out float value))
                {
                    value /= 100f;
                    TrippleSixCheat.SetMax666Chance(gameplayDataType, value);
                    Logger.LogInfo($"Max 666 chance set to {value}%!");
                }
                else
                {
                    Logger.LogWarning("Invalid input for 666 chance.");
                }
            }

            if (GUILayout.Button("Set 666 Chance"))
            {
                if (float.TryParse(sixChanceStr, out float value))
                {
                    value /= 100f;
                    TrippleSixCheat.Set666Chance(gameplayDataType, value);
                    Logger.LogInfo($"666 chance set to {value}%!");
                }
                else
                {
                    Logger.LogWarning("Invalid input for 666 chance.");
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.Label($"Red button multiplier (Limit: {int.MaxValue:N0})");
            redButtonStr = GUILayout.TextField(redButtonStr);
            if (GUILayout.Button("Set Red Button Multiplier"))
            {
                int value = ParseIntOrDefault(redButtonStr, 1);
                if (!ShouldSkipValue(value, "red button multiplier"))
                {
                    RedButtonCheat.SetRedButtonActivationsMultiplier(gameplayDataType, value);
                    Logger.LogInfo($"Red button multiplier set to {value}!");
                }
            }

            GUILayout.Label("Ascension counter --> Restart your run for it to Update");
            endingCounterStr = GUILayout.TextField(endingCounterStr);
            if (GUILayout.Button("Set Ascension Counter"))
            {
                int value = ParseIntOrDefault(endingCounterStr, INT_MAX_SAFE);
                EndingCounterCheat.SetGoodEndingCounter(value);
                Logger.LogInfo($"Ascension counter set to {value}!");
            }

            EndIndentedSection();
        }
        #endregion

        #region Charms Section
        private void DrawCharmsSection()
        {
            if (!DrawFoldoutSection("charms", "Charms & Store Cheats")) return;

            BeginIndentedSection();

            GUILayout.Label("Charm slots to add:");
            charmSlotsStr = GUILayout.TextField(charmSlotsStr);
            DrawCautionLabel();
            if (GUILayout.Button("Add Charm Slots"))
            {
                ExecuteWithMethodCheck(addCharmSlotInfo, "MaxEquippablePowerupsAdd", () =>
                {
                    int value = ParseIntOrDefault(charmSlotsStr, 1);
                    if (!ShouldSkipValue(value, "charm slots"))
                    {
                        CharmSlotCheat.AddCharmSlots(addCharmSlotInfo, value);
                        Logger.LogInfo($"Added {value} charm slots!");
                    }
                });
            }

            if (GUILayout.Button("Unlock All Charms"))
            {
                var instanceProperty = gameplayDataType?.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
                var instance = instanceProperty?.GetValue(null);

                if (instance != null)
                {
                    CharmUnlockCheat.UnlockAllLockedCharms(instance);
                    Logger.LogInfo("All locked charms have been unlocked!");
                }
                else
                {
                    Logger.LogWarning("Could not get GameplayData instance.");
                }
            }

            GUILayout.Label("Free restocks:");
            freeRestockStr = GUILayout.TextField(freeRestockStr);
            if (GUILayout.Button("Add Free Restocks"))
            {
                ExecuteWithMethodCheck(addFreeRestockMethod, "StoreFreeRestocksSet", () =>
                {
                    int value = ParseIntOrDefault(freeRestockStr, int.MaxValue);
                    FreeRestockCheat.AddFreeRestock(addFreeRestockMethod, value);
                    Logger.LogInfo($"Set {value} free restocks!");
                });
            }

            EndIndentedSection();
        }
        #endregion

        #region Symbols Section
        private void DrawSymbolsSection()
        {
            if (!DrawFoldoutSection("symbols", "Symbol Chance Cheats")) return;

            BeginIndentedSection();

            GUILayout.Label("Symbol Chances (must add to 100 or will be equalized)");

            foreach (var symbol in new[] { "lemon", "cherry", "clover", "bell", "diamond", "coins", "seven" })
            {
                GUILayout.Label($"{char.ToUpper(symbol[0]) + symbol.Substring(1)}:");
                symbolChances[symbol] = GUILayout.TextField(symbolChances[symbol]);
            }

            if (GUILayout.Button("Set Symbol Chances"))
            {
                var parsedChances = new Dictionary<string, float>();
                bool allValid = true;
                float total = 0f;

                foreach (var kvp in symbolChances)
                {
                    if (float.TryParse(kvp.Value, out float chance))
                    {
                        parsedChances[kvp.Key] = chance;
                        total += chance;
                    }
                    else
                    {
                        allValid = false;
                        break;
                    }
                }

                if (allValid)
                {
                    if (total > 100f)
                    {
                        Logger.LogWarning("Total exceeds 100%! Using equal distribution.");
                        float equalShare = 100f / parsedChances.Count;
                        foreach (var key in new List<string>(parsedChances.Keys))
                        {
                            parsedChances[key] = equalShare;
                        }
                    }

                    SymbolChanceCheat.SetSymbolChances(symbolChanceSetMethod, parsedChances);
                    Logger.LogInfo("Symbol spawn chances updated!");
                }
                else
                {
                    Logger.LogWarning("Invalid input for symbol chances.");
                }
            }

            EndIndentedSection();
        }
        #endregion

        #region Patterns Section
        private void DrawPatternsSection()
        {
            if (!DrawFoldoutSection("patterns", "Pattern Value Cheats")) return;

            BeginIndentedSection();

            GUILayout.Label("Pattern Values to Add (doubles):");

            var patternLabels = new Dictionary<string, string>
            {
                { "horizontal3", "hor_Value" },
                { "horizontal4", "hor_L_Value" },
                { "horizontal5", "hor_Xl_Value" },
                { "vertical3", "vert" },
                { "diagonal3", "diag" },
                { "pyramid", "zig" },
                { "pyramidInverted", "zag" },
                { "triangle", "above" },
                { "triangleInverted", "below" },
                { "eye", "Eye" },
                { "jackpot", "Jackpot" }
            };

            foreach (var kvp in patternLabels)
            {
                GUILayout.Label($"{kvp.Value}:");
                patternValues[kvp.Key] = GUILayout.TextField(patternValues[kvp.Key]);
            }

            if (GUILayout.Button("Add Pattern Values"))
            {
                ApplyPatternValues(false);
            }

            DrawCautionLabel();
            if (GUILayout.Button("Add E+ Pattern Values (100 = E+100)"))
            {
                ApplyPatternValues(true);
            }

            EndIndentedSection();
        }

        private void ApplyPatternValues(bool useExponential)
        {
            ExecuteWithMethodCheck(patternValueAdd, "Pattern_ValueExtra_Add", () =>
            {
                var parsedValues = new Dictionary<string, double>();
                bool allValid = true;
                bool allAreOne = true;

                foreach (var kvp in patternValues)
                {
                    if (double.TryParse(kvp.Value, out double value))
                    {
                        parsedValues[kvp.Key] = value;
                        if (Math.Abs(value - 1.0) > 0.0001)
                        {
                            allAreOne = false;
                        }
                    }
                    else
                    {
                        allValid = false;
                        break;
                    }
                }

                if (!allValid)
                {
                    Logger.LogWarning("Invalid input for pattern values.");
                    return;
                }

                if (allAreOne)
                {
                    Logger.LogWarning("All pattern values are 1 - skipping to avoid overwriting with no effect");
                    return;
                }

                if (useExponential)
                {
                    PatternValueCheat.SetPatternMultiplierMath(patternValueAdd, parsedValues);
                    Logger.LogInfo("E+ Pattern values added!");
                }
                else
                {
                    PatternValueCheat.SetPatternMultiplier(patternValueAdd, parsedValues);
                    Logger.LogInfo("Pattern values added!");
                }
            });
        }
        #endregion

        #region Extras Section
        private void DrawExtrasSection()
        {
            if (!DrawFoldoutSection("extras", "Extra Cheats")) return;

            BeginIndentedSection();

            if (GUILayout.Button("Trigger Phone Transformation"))
            {
                PhoneCheat.TriggerPhoneTransformation(gameplayDataType);
                Logger.LogInfo("Phone transformation triggered!");
            }

            if(GUILayout.Button("Trigger Phone Ring"))
            {
                PhoneCheat.TriggerPhone();
                Logger.LogInfo("Phone ring triggered!");
            }

            GUILayout.Label("Set Wins to all Cards:");
            amountOfWins = GUILayout.TextField(amountOfWins);
            if (GUILayout.Button("Add Wins"))
            {
                if (int.TryParse(amountOfWins, out int amount))
                {
                    AddWinsToCardsCheat.addCardWins(amount);
                    Logger.LogInfo($"Set wins to: {amountOfWins}");
                }
            }

            if (GUILayout.Button("Unlock All Achievements"))
            {
                AchievementCheat.UnlockAllAchievements();
                Logger.LogInfo("All Steam achievements unlocked!");
            }

            GUILayout.Label("Extra rounds to add:");
            extraRoundsStr = GUILayout.TextField(extraRoundsStr);
            if (GUILayout.Button("Add Extra Rounds"))
            {
                ExecuteWithMethodCheck(extraRoundsAddMethod, "DeadlineRoundsIncrement_Manual", () =>
                {
                    if (int.TryParse(extraRoundsStr, out int value))
                    {
                        if (!ShouldSkipValue(value, "extra rounds"))
                        {
                            SpinsRoundsCheat.ExtraRoundsAdd(extraRoundsAddMethod, value);
                            Logger.LogInfo($"Added {value} extra rounds!");
                        }
                    }
                    else
                    {
                        Logger.LogWarning("Invalid input for extra rounds.");
                    }
                });
            }

            GUILayout.Label("Extra spins to add:");
            extraSpinsStr = GUILayout.TextField(extraSpinsStr);
            if (GUILayout.Button("Add Extra Spins"))
            {
                ExecuteWithMethodCheck(extraSpinsAddMethod, "ExtraSpinsAdd", () =>
                {
                    if (int.TryParse(extraSpinsStr, out int value))
                    {
                        if (!ShouldSkipValue(value, "extra spins"))
                        {
                            SpinsRoundsCheat.ExtraSpinsAdd(extraSpinsAddMethod, value);
                            Logger.LogInfo($"Added {value} extra spins!");
                        }
                    }
                    else
                    {
                        Logger.LogWarning("Invalid input for extra spins.");
                    }
                });
            }

            GUILayout.Label($"Current Menu Key: {openMenu}");
            if (GUILayout.Button(waitingForKey ? "Press a key..." : "Set Menu Open Key"))
            {
                waitingForKey = !waitingForKey;
                if (waitingForKey)
                {
                    Logger.LogInfo("Waiting for key press to set new menu open key...");
                }
            }

            GUILayout.Label($"Animation Speed: {desiredAnimationTimeScale:F1} --> if on 20x put gamespeed to 2x otherwise it can break.");
            desiredAnimationTimeScale = GUILayout.HorizontalSlider(desiredAnimationTimeScale, 1.0f, 20.0f, GUILayout.Width(300));

            GUILayout.BeginHorizontal();
            if (DrawAnimationButton("1x",   1)) { }
            if (DrawAnimationButton("10x", 10)) { }
            if (DrawAnimationButton("20x", 20)) { }
            GUILayout.EndHorizontal();

            GUILayout.Label($"Game Speed: {desiredTimeScale:F1}");
            desiredTimeScale = GUILayout.HorizontalSlider(desiredTimeScale, 1.0f, 4.0f, GUILayout.Width(300)); 


            GUILayout.BeginHorizontal();
            if (DrawSpeedButton("1x", 1f)) { }
            if (DrawSpeedButton("2x", 2f)) { }
            if (DrawSpeedButton("4x", 4f)) { }
            GUILayout.EndHorizontal();

            EndIndentedSection();
        }
        private bool DrawAnimationButton(string label, int speed)
        {
            if (GUILayout.Button(label, GUILayout.Width(50)))
            {
                desiredAnimationTimeScale = speed;
                AnimationSpeedCheat.AddAnimationSpeed(speed);
                Logger.LogInfo($"Animation speed set to {speed}x!");
                return true;
            }
            return false;
        }

        private bool DrawSpeedButton(string label, float speed)
        {
            if (GUILayout.Button(label, GUILayout.Width(50)))
            {
                desiredTimeScale = speed;
                Logger.LogInfo($"Game speed set to {speed}x!");
                return true;
            }
            return false;
        }

        #endregion

        #region Utility Methods
        private int ParseIntOrDefault(string input, int defaultValue)
        {
            return int.TryParse(input, out int result) ? result : defaultValue;
        }

        private void ExecuteWithMethodCheck(MethodInfo method, string methodName, Action action)
        {
            if (method != null)
            {
                action();
            }
            else
            {
                Logger.LogWarning($"Could not find {methodName} method.");
            }
        }
        #endregion
    }
}