# Six-Sided Swordsmen

A turn-based tactical combat game built in Unity, played on a hexagonal grid. Recruit and position a squad of units, then outmaneuver your opponent using terrain, unit classes, and ability commands until one side is wiped out.

> This repository is a personal copy of a team project I contributed to, kept here for portfolio purposes. The original was built by a 7-person team as the final project for **COMP 361 (Software Engineering Project)** at McGill University. All commit history and authorship from the original team are preserved.

![Gameplay](screenshots/gameplay.gif)

## Gameplay

- **Setup phase** — each player recruits units from a roster (Swordsman, Spearman, Archer, Holy Knight, Ghost) and places them on their side of the map before battle begins.

  ![Unit setup phase](screenshots/unit-setup.png)

- **Hex-grid combat** — the map is a procedurally generated hex grid using axial coordinates, with tiles carrying attributes like terrain type and altitude that affect movement and combat.
- **Turn-based commands** — players select units and issue move/attack commands through a console-style UI, with input validation to prevent illegal actions.

  ![Turn-based combat](screenshots/combat.png)

- **Save & load** — full game state serializes to JSON so a match can be saved and resumed later.

  ![Save / load menu](screenshots/save-load.png)

- **Replay** — every match logs a turn-by-turn event history that can be played back afterward, complete with per-turn summaries.

  ![Match replay viewer](screenshots/replay.png)

## My contributions

I worked primarily on the **replay system** — the turn-based event log, match playback/viewer, and per-turn summary UI — along with the grid's tile-click selection handling, several combat commands, and the data persistence adapters for grid/tile state.

## Tech stack

- **Engine:** Unity 2022.3
- **Language:** C#
- **Data:** JSON save files via Unity's `JsonUtility`
- **Platform:** standalone desktop build (Windows/macOS)

## Project structure

```
Assets/
  Scripts/
    Grid/              # hex grid data structure, tile logic, event handling
    Combat/             # units, commands (move/attack), combat resolution
    UI/                 # unit console, menus, in-game windows
    Data Persistence/   # JSON save/load, DTOs, adapters
    Replay/              # turn/event logging, replay viewer, turn summaries
    Setup/               # unit recruitment & placement phase
    System/              # game manager, turn flow
  Scriptable Objects/
    UnitSO/              # per-unit stats (Swordsman, Spearman, Archer, Holy Knight, Ghost)
    UnitCommandSO/       # command definitions
  Prefabs/
  Art/ Sounds/ Sprites/
```

## Running it

Open the project in Unity 2022.3.x via Unity Hub, then open `Assets/Scenes/Title.unity` and press Play.

## Team

Built with Michael Xie, Enoch Chan, Russ Morta, Manshen He, Hoi Kin Chiu, and Maxim Miladinov-Genov.
