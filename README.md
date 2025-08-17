TESTIEC – Gameplay Conversion Notes

A professional summary of the migration from a match-3 (Candy Crush–style) project to a tap-to-collect with bottom buffer game, including dual modes (Classic & Time Attack), AI helpers, and sound.

1) Overview

Old: swap/match with gravity and cascades.

New: player taps tiles on the board to collect them into a 5-slot bottom area. When exactly 3 of the same type are present in the bottom, they clear.

Gravity: disabled (tiles above do not fall after a collect).

Bottom behavior: newly collected items insert next to their same-type group, shifting others to the right to cluster matches.

2) Game Modes
Classic

Lose if the bottom area is full and no clear occurs.

Win when the board is empty.

Time Attack

Bottom full does not cause a loss.

Player can tap an item in the bottom to return it to its original board cell.

Lose when the countdown timer reaches zero.

Win when the board is empty.

3) Core Mechanics

Tap-to-Collect: tap a board cell → item animates to bottom.

Exact-3 Clear: only exactly 3 of the same type clears; after clearing, remaining bottom items compact left.

Bottom Insert Rule: upon collection, find the last position of the same type in bottom; insert right after it, shift items to the right as needed.

Initialization (divisible by 3): after board fill, the item distribution is balanced so each type count ≡ 0 (mod 3) (requires total board cells % 3 == 0).

4) Systems & Architecture
BoardController (gameplay hub)

Builds the bottom area as 5 runtime Cells (no Inspector wiring).

Handles input:

Board cell tap: collect to bottom.

Bottom cell tap (Time Attack only): return item to its InitialCell on the board.

Tracks remaining board items, checks win/lose conditions per mode.

Provides AI helpers (auto-win / auto-lose).

Item state

Each Item stores InitialCell (the original board cell it spawned in) to enable Time Attack returns.

No gravity

Board compaction/fall is disabled for this gameplay (we do not shift items down after collection).

5) UI

Panels

UIPanelGameWin (win) and UIPanelGameOver (lose) are split.

UIPanelGame includes two buttons:

AI Win → autoplay to finish quickly.

AI Lose → autoplay to intentionally fail.

UIPanelMain (Home) has separate buttons for Classic / Time Attack entry.

Moves counter: converted to count-up (increments per action), purely informational.

6) AI Helpers

AutoPlayWin(delay): chooses cells that complete a triple ASAP, then sets up 1→2→3 sequences while respecting bottom capacity.

AutoPlayLose(delay): avoids creating triples (prefers new types or ones at count 1) to fill bottom and lose.

These call into the same input pipeline as the player (no special privileges).

7) Audio

SoundManager (singleton, DontDestroyOnLoad) with BGM/SFX:

BGM plays when a level starts; pauses on game pause; stops on main menu or before Win/Lose SFX.

SFX Collect when an item lands in the bottom.

SFX Win / SFX Lose on results.

Resource paths are taken from Constants (under Assets/Resources/…).
You can also assign clips directly in the SoundManager inspector.

8) Configuration

GameSettings

BottomAreaMax = 5

BottomAreaPosition (e.g., -4)

TimeAttackSeconds (optional, if you prefer not to hardcode 60s)

Constraints

For init divisible by 3 to be feasible, ensure BoardSizeX * BoardSizeY % 3 == 0.

Constants
Define audio resource paths, prefab names, etc. SoundManager loads them via Resources.Load using these paths.

9) Key Script Changes

Gameplay

BoardController.cs

Dual modes: Classic and TimeAttack (EnableTimeAttack(true/false)).

Bottom built at runtime (5 Cells with BoxCollider2D).

Insert-next-to-same-type behavior in bottom, compact left after clears.

Exact-3 clear logic.

No gravity after collect.

Return to original cell (Time Attack) using Item.InitialCell.

AI: AutoPlayWin, AutoPlayLose, StopAutoPlay.

SFX: calls SoundManager.PlayCollect() on successful collect.

Item.cs

Added public Cell InitialCell { get; set; }.

LevelMoves.cs

Count increments per action (no decrements/game over).

LevelTime.cs

Used for Time Attack countdown.

State/UI

GameManager.cs

Added GAME_WIN state, GameWin().

LoadLevelTimeAttack() sets up BoardController in TimeAttack mode + LevelTime (e.g., 60s).

Starts BGM on game start; plays Win/Lose SFX on results; pauses/resumes BGM with state.

UIMainManager.cs

Routes GAME_WIN panel.

Adds LoadLevelTimeAttack() that calls GameManager.LoadLevelTimeAttack().

UIPanelMain.cs

Separate buttons for Classic / Time Attack; auto-binds by child names if not set.

UIPanelGame.cs

Buttons for AI Win / AI Lose; auto-binds by child names if not set.

UIPanelGameWin.cs / UIPanelGameOver.cs

Independent scripts per result.

Audio

SoundManager.cs

Singleton with BGM/SFX and resource loading via Constants.

10) How to Play

Classic

Tap board items to collect to bottom.

When exactly 3 of the same type are in bottom → they clear.

Lose if bottom becomes full without a clear; win when the board is empty.

Time Attack

Same collections to bottom.

Tap an item in bottom to return it to its original board cell.

Lose on timer expiry; win when the board is empty.

AI Buttons

AI Win: autoplay to victory (greedy triple completion).

AI Lose: autoplay to failure (avoid triples).

11) Dev Setup

Unity Project Settings

Version Control: Visible Meta Files

Asset Serialization: Force Text

Resources

Place audio files to match Constants paths (e.g., Assets/Resources/Sound/...).

Git

Use the provided .gitignore (Unity).

Consider Git LFS for large assets (.wav, .mp3, .psd, .fbx, etc.).

12) Known Limitations & Next Steps

Divisible-by-3 balancing assumes total board cells % 3 == 0.

UX polish: optional HUD for Left: X | Bottom: y/5, subtle shakes/highlights, configurable Time Attack duration in UI.

AI: heuristics can be tuned (e.g., lookahead for capacity management).

13) Quick API Reference

Mode toggle

GameManager.LoadLevelTimeAttack() → Time Attack

GameManager.LoadLevel(GameManager.eLevelMode.MOVES) → Classic (moves counter)

BoardController

EnableTimeAttack(bool)

AutoPlayWin(float stepDelay = 0.5f)

AutoPlayLose(float stepDelay = 0.5f)

StopAutoPlay()

SoundManager

PlayBGM(), PauseBGM(bool), StopBGM()

PlayCollect(), PlayWin(), PlayLose()
