# Clover Mod

**Press F2 to open/close the mod menu in-game.**

## Overview

Clover Mod is a BepInEx plugin designed to enhance gameplay for CloverPit by providing a customizable in-game menu with various cheats and modifications. This mod allows players to tweak game mechanics such as currency, multipliers, symbol chances, and more, offering a tailored experience for testing or casual play.

> ⚠️ **Warning:** Using high values in the mod may cause game instability or crashes. Use at your own risk!

## Features

### Core Cheats
- **Currency Cheats:** Add coins and clover tickets, including exponential coin additions for rapid wealth accumulation.
- **Multiplier Cheats:** Adjust pattern and symbol multipliers for enhanced rewards and bigger payouts.
- **Interest and 666 Cheats:** Modify interest rates, 666 chances, and red button multipliers to customize risk/reward mechanics.
- **Charms & Store Cheats:** Unlock all charms instantly, add charm slots, and enable free restocks for unlimited resources.
- **Symbol Chance Cheats:** Fine-tune spawn probabilities for game symbols to create your ideal gameplay experience.
- **Pattern Value Cheats:** Adjust values for various patterns with exponential options for dramatic effects.
- **Animation Speed Cheats:** Adjust the speed in whitch the Animation plays
- **Add Memotry Card Win Cheats:** Set all of your Memory Card wins.

### Quality of Life Features
- **Extra Cheats:** Trigger phone transformations, unlock achievements instantly, and add extra rounds or spins.
- **Game Speed Control:** Adjust game speed dynamically via a slider or use convenient preset buttons (1x, 2x, 4x) for faster gameplay.
- **User-Friendly UI:** Toggleable menu with clear, bold fonts and a semi-transparent dark background for optimal visibility during gameplay.

## Requirements

- **CloverPit** (base game)
- **[BepInEx](https://github.com/BepInEx/BepInEx/releases)** (version 5.x or later)

## Installation

### Step 1: Install BepInEx

1. Download the latest version of **BepInEx 5.x** from the [official GitHub releases page](https://github.com/BepInEx/BepInEx/releases).
2. Extract the BepInEx files into your CloverPit installation directory (where `CloverPit.exe` is located).
3. Run the game once to allow BepInEx to generate its folder structure (`BepInEx/plugins`, `BepInEx/config`, etc.).
4. Close the game after BepInEx initializes.

### Step 2: Install Clover Mod

1. Download the latest release of **Clover Mod** from the [Releases page](../../releases).
2. Extract the `CloverMod.dll` file into the `BepInEx/plugins` folder in your CloverPit directory.
   - If the `plugins` folder doesn't exist, create it manually.
3. Your directory structure should look like this:
   ```
   CloverPit/
   ├── BepInEx/
   │   ├── plugins/
   │   │   └── CloverMod.dll
   │   └── ...
   └── CloverPit.exe
   ```

### Step 3: Verify Installation

1. Launch the game.
2. Check the BepInEx console window or the log file (`BepInEx/LogOutput.log`) for the message:
   ```
   Clover Mod v1.0.1 successfully loaded!
   ```
3. If you see this message, the mod is installed correctly.

## Usage

### Opening the Mod Menu

- **Press F2** at any time during gameplay to toggle the Clover Mod menu.
- The menu pauses the game automatically and displays various cheat sections.

### Navigating the Menu

- Use the **foldout sections** (e.g., Currency Cheats, Multiplier Cheats, Symbol Chance Cheats) to access specific options.
- Enter values in text fields and click the corresponding buttons to apply cheats.
- Adjust game speed using the **slider** or convenient **preset buttons** (1x, 2x, 4x).
- Scroll through the menu if needed to access all available options.

### Applying Cheats

- Input your desired values for coins, multipliers, chances, pattern values, etc.
- Click the corresponding button to apply the changes instantly.
- **Be cautious with high values**, as they may cause lag, instability, or crashes.
- Some changes (e.g., Ascension Counter) require restarting your run to take effect.

### Closing the Menu

- Press **F2** again or click the **"Close Menu (F2)"** button to close the menu and resume gameplay.

## Examples

### Adding Coins
1. Open the menu with **F2**.
2. Navigate to the **"Currency Cheats"** section.
3. Enter a value in the **"Add Normal coins"** field (e.g., `1000`).
4. Click **"Add Coins"** to instantly add the coins to your balance.

### Unlocking All Charms
1. Open the menu with **F2**.
2. Navigate to the **"Charms & Store Cheats"** section.
3. Click **"Unlock All Charms"** to instantly unlock every charm in the game.

### Adjusting Game Speed
1. Open the menu with **F2**.
2. Scroll to the **"Game Speed Control"** section.
3. Use the slider to set a custom speed or click a preset button (**1x**, **2x**, or **4x**).

## Troubleshooting

| Issue | Solution |
|-------|----------|
| **Mod not loading** | Ensure BepInEx is correctly installed and the `CloverMod.dll` file is in the `BepInEx/plugins` folder. Check the log file for errors. |
| **Menu not appearing** | Verify that **F2** is not bound to another action in the game. Check the BepInEx console or log file for errors. |
| **Game crashes or freezes** | Reduce the values entered in the mod menu. High values (especially exponential additions) can cause instability. Try restarting the game. |
| **Changes not taking effect** | Some modifications (like Ascension Counter) require restarting your run. Exit to the main menu and start a new game. |
| **BepInEx console not appearing** | Enable the console in `BepInEx/config/BepInEx.cfg` by setting `Enabled = true` under `[Logging.Console]`. |

## Contributing

We welcome contributions from the community! To contribute:

1. **Fork the repository** on GitHub.
2. **Create a new branch** for your changes (`git checkout -b feature/your-feature-name`).
3. **Make your modifications** and test thoroughly.
4. **Submit a pull request** with a clear description of your changes and why they're beneficial.

Please ensure your code:
- Follows the existing code style and conventions
- Includes appropriate logging for debugging purposes
- Does not introduce breaking changes without discussion
- Is well-commented for future maintainers

## License

This project is licensed under the **MIT License**. See the [LICENSE](LICENSE) file for full details.

## Disclaimer

This mod is provided **as-is**, with **no warranty of any kind**, express or implied. Use it responsibly and at your own risk.

⚠️ **Important Notes:**
- This mod may violate the terms of service of CloverPit or related platforms.
- The author is **not responsible** for any consequences, including but not limited to:
  - Game bans or account suspensions
  - Data loss or corruption
  - Game instability or crashes
  - Any other technical issues

By using this mod, you acknowledge and accept these risks.

---

P.S. This is my first time Modding, so the codebase is a bit messy and inconsistent (thanks to Me and AI). I hope to clean it up and make a more organized version when I have the time.
