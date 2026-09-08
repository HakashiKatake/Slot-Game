# Classic Retro Slot Machine Game (Unity)

A high-performance, polished, and responsive 3-reel retro slot machine game built in **Unity 6 (6000.4.10f1)** using modern **C# OOP architecture**, **Text Mesh Pro**, and **Universal Render Pipeline (URP 2D)**.

Direct play using : https://slot-game-wheat.vercel.app/

---

## Game Overview & Features

### Core Game Loop
- **3-Reel Classic Mechanism**: Features 3 physical reels with authentic continuous vertical scrolling, symbol wrapping, and smooth deceleration.
- **Winning Combinations**: Matching 3 symbols along the center payline evaluates against a configurable `PayoutTableSO`.
- **Interactive Lever Mechanic**:
  - Pulling the physical animated lever bets the **last betted amount** (defaults to **50G** if no prior bet has been selected).
  - Switches sprites to animate the lever pulling down and springing back up.
  - Can also be triggered via the keyboard shortcut (`Space` or `Enter`).
- **Quick Bet Menu**:
  - Always available on the right-hand side of the machine.
  - Quick-bet buttons: **10G**, **50G**, **100G**.
  - Clicking any option automatically updates the player's last bet, deducts credits, and spins immediately.
  - Features an **Exit** button to toggle the menu overlay.
- **Rules & Paytable Popup**:
  - Appears automatically at game launch with complete rules and multipliers.
  - Includes a pixel-accurate **X close button** with hover/pressed states.
- **Jackpot & Win Celebration Popups**:
  - Full-screen animated celebration modal for **Jackpot (3x Sevens)**, **Bell Bonus (3x Bells)**, or **Big Wins**.
  - Interactive **Collect** button to claim credits and return to the game.

### Bonus Features
1. **Bell Bonus & Free Spins**:
   - Hitting **3 Bells** awards **5 Free Spins**!
   - Free spins require **0 credits** and apply an automatic **2x win multiplier** to all payouts.
   - Automatically auto-spins through remaining free spins until complete.
2. **Wild Sevens**:
   - The **Seven** symbol acts as a Wild substitution, matching any other symbol along the payline.
3. **Partial Cherry Payouts**:
   - 1 Cherry = 2x Bet payout.
   - 2 Cherries = 5x Bet payout.
   - 3 Cherries = 15x Bet payout.
4. **Authentic Audio System**:
   - Procedural / sampled audio for button clicks, lever pulls, reel spinning loop, individual reel stop clunks (with rising pitch per reel), and win fanfare.

---

## Payout Table

| Combination | Multiplier | Feature / Tier |
| :--- | :---: | :--- |
| **7 - 7 - 7** | **100x** | 🏆 **JACKPOT** |
| **Bell - Bell - Bell** | **50x** | 🔔 **BELL BONUS + 5 Free Spins (2x Multiplier)** |
| **Bar - Bar - Bar** | **25x** | 💰 **Big Win** |
| **Cherry - Cherry - Cherry** | **15x** | ✨ **Medium Win** |
| **Any 2 Cherries** | **5x** | 🍒 **Small Win** |
| **Any 1 Cherry** | **2x** | 🍒 **Small Win** |

---

## Project Architecture & Design Principles

The codebase was refactored and streamlined for maximum readability, maintainability, and clean Object-Oriented design:

```
Assets/
├── Prefabs/                # Reusable UI & Slot components (Reels, Popups, Canvas)
├── Scenes/                 # Main game scene (SampleScene.unity)
├── Scripts/
│   ├── Audio/              # SlotAudioService (SFX, loop management, pitch modulation)
│   ├── Controllers/        # SlotMachinePresenter (Main clean coordinator)
│   ├── Core/               # ScriptableObjects & Enums (Config, PayoutTable, SymbolData)
│   ├── Editor/             # SceneSetup & WebGL build automation
│   ├── Reels/              # ReelController, ReelsManager, SymbolCellView
│   └── Services/           # RNG (weighted probability) & ClassicPayoutEvaluator
├── Settings/               # ScriptableObject instances & URP configuration
├── Sounds/                 # High quality audio effects (.wav)
└── Sprites/                # Pixel art assets, sliced spritesheets, retro UI frames
```

### Key Technical Highlights:
- **Direct Inspector Wiring**: All UI buttons, text elements, popups, and sprites are exposed as `[SerializeField]` references in the Unity Inspector. No hardcoded magic strings or brittle reflection.
- **Separation of Concerns**:
  - `SlotRngService`: Pure C# weighted probability generator ensuring fair and unpredictable outcomes based on each symbol's configured weight.
  - `ClassicPayoutEvaluator`: Decoupled evaluation engine testing paylines against wild rules, partials, and multiplier tables.
  - `ReelController`: Handles individual reel column scrolling, circular symbol recycling, and cubic ease-out deceleration to land dead-center on the target symbol.
  - `ReelsManager`: Coordinates staggered reel stops (Reel 0 → Reel 1 → Reel 2) and calculates suspense pauses when reels match high-value symbols.
  - `SlotMachinePresenter`: Concise high-level controller connecting user inputs to reel spin execution and UI updates.

---

## How to Run

### In the Unity Editor:
1. Open the project in **Unity 6 (6000.4.10f1)** or higher.
2. In the Project window, navigate to `Assets/Scenes/` and double-click `SampleScene.unity`.
3. Press the **Play** button at the top of the Unity Editor.
4. Interact with the **Rules Popup (X)**, click the **Lever** (or press `Space`), or pick a bet from the **Quick Bet Menu**.

### WebGL Build:
1. In Unity, select `File > Build Settings...`
2. Switch Platform to **WebGL**.
3. Target output directory: `Build/WebGL`.
4. Click **Build**.
5. Once built, open `Build/WebGL/index.html` via a local web server (e.g. `python3 -m http.server 8000` inside `Build/WebGL/` and browse to `http://localhost:8000`) or play directly online on https://slot-game-wheat.vercel.app

---

## Asset Credits
All visual sprites, fonts, and sound effects are provided per the assignment requirements and properly configured in Unity's 2D Sprite Editor and Audio Importer.
