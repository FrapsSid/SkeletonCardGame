# Bet To The Bone

---

## Table of Contents

- [About](#about)
- [Download & Play](#download--play)
- [Features](#features)
- [How To Play](#how-to-play)
- [Tech Stack](#tech-stack)
- [Build From Source](#build-from-source)
- [CI/CD](#cicd)
- [Testing](#testing)
- [Project Structure](#project-structure)
- [Design Documents](#design-documents)
- [Team](#team)
- [Track](#track)

---

## About

**Bet To The Bone** is a stylized low-poly multiplayer game built in Unity.
Players take the role of skeletons competing in a card game where the stakes
are literal — you bet your arms, legs, skull, and even your soul. The game
combines card combinations, strategic betting, physical world interaction,
and a cheating system where accusations and punishments are part of the
gameplay.

The project was developed as part of the Practicum Project course at
Innopolis University.

---

## Download & Play

> **The desktop build is the recommended and stable way to play.**
> The WebGL browser build exists for convenience but is currently unstable
> and may not represent the full experience reliably.

| Build | Link | Status |
|---|---|---|
| Desktop (Windows) — Recommended | https://disk.yandex.ru/d/qtBfVkB_fKfMmg | Stable |
| WebGL (browser) | https://necr0manth.dev/SkeletonCardGame/index.html | Unstable |

Download the desktop build, extract the archive, and run the executable.
No installation is required.

---

## Features

### Core Gameplay

- Phase-based match loop: combination reveal, card deal, discussion,
  active turns, round end
- Three active iterations per round with table card reveals between them
- Individual player declarations (Easy / Medium / Hard combination targets)
- Shared participation price with raise / match / fold / pass mechanics
- GDD-aligned combination system with Easy, Medium, Hard, and
  Anti-combinations
- Card combinations checked against personal hand and shared table cards
- Team-based scoring with pot distribution

### Physical World

- Cards physically dealt to players and placed on the table
- Fan-style card hand with a card holding animation
- Cards can be held, placed on the table, and revealed at round end
- Body parts detachable from skeleton models with gameplay consequences
- Phylactery model and other key game objects present in the world

### Skeleton Body Part Stakes

- Players wager body parts and team assets
- Losing a skull affects vision
- Losing arms restricts card interaction
- Losing legs reduces movement speed
- Losing a soul incapacitates the player

### Card Visibility

- Personal and teammate cards visible within a defined cone of vision
- Opponent cards require a deliberate "Peek" action
- Card concealment shader prevents cards from being readable from the
  third-person camera

### Multiplayer & Authorization

- Account registration and login with username and password
- Discord OAuth login as an alternative authentication path
- Dedicated login and registration UI in-game
- User data stored securely in a server-side database
- Online multiplayer with Discord identity integration
- Lobby creation and joining flow
- Synchronized match phases, card states, and table information
- Full multiplayer match flow from lobby to round resolution

### AI

- AI opponents playable in solo lobby
- AI uses limited information to make decisions
- Improved bot behavior for more realistic solo practice

### Presentation

- Low-poly skeleton character models with rigging and animations
- Skeleton animations and card holding animation connected to gameplay
- Full bar environment with lighting
- Main menu background
- Sound effects across key interactions and state changes

### UI & Onboarding

- Tutorial slideshow integrated into the main menu
- Round and match-end result screens
- Participant display panel
- Settings menu
- 2D combination cards displayed for each active round combination

---

## How To Play

### Quick Start

1. Download and run the desktop build (link above).
2. Log in with your username and password, or use Discord login.
3. Read through the tutorial in the main menu to learn the rules.
4. Start a solo match against AI or create / join an online lobby.

### Round Flow

1. **Combination Reveal** — four combinations are announced for the round:
   Easy, Medium, Hard, and Anti-combination.
2. **Card Deal** — each player receives 2 cards; table cards are placed
   into the shared play area.
3. **Discussion Phase** — players can move, communicate, and plan before
   the active phase begins.
4. **Active Phase** — players take turns. On your turn you can:
   - Draw a card from the deck
   - Declare or upgrade your combination target
   - Raise the participation price
   - Match the current participation price
   - Pass
   - Fold
5. **Round End** — cards are revealed, combinations are evaluated, scores
   are calculated, and the pot is distributed to the winning team.

### Betting Rules

- Players stake team assets, including body parts and other owned items.
- When a player raises the price, other active players must respond:
  match, raise again, or fold.
- Folded players are excluded from scoring and cannot win the pot.
- At round end, the winning team receives the pot.
- If multiple teams tie, the pot is split.

### Combination Scoring

- Each player declares a target combination.
- At round end, the declared combination is checked against the player's
  cards and the shared table cards.
- If a player collects an Anti-combination, their contribution is cancelled.
- If a player's declared combination is not satisfied, they do not
  contribute to the team score.

---

## Tech Stack

| Area | Technology |
|---|---|
| Engine | Unity |
| Language | C# |
| Multiplayer | Unity networking stack |
| Authentication | Username/password + Discord OAuth |
| Backend / Database | Server-side user data storage |
| Build / CI | GitHub Actions |
| Deployment | Desktop (Windows) + Unity WebGL (unstable) |
| Design | Figma |
| Version Control | Git / GitHub |

---

## Build From Source

### Requirements

- Unity (check ProjectSettings/ProjectVersion.txt for the exact version)
- Git

### Steps

```bash
git clone https://github.com/FrapsSid/SkeletonCardGame.git
```

Then:

1. Open Unity Hub.
2. Add the cloned project folder.
3. Open the project.
4. Open the main scene.
5. Press Play to run in the editor.

To build for Windows:

1. Open File > Build Settings.
2. Select Windows.
3. Press Build.

Note: the desktop build is the stable and recommended target.
The WebGL build target currently produces an unstable build and is not
recommended for evaluation.

The CI pipeline also produces builds automatically on every push.
Download the latest artifact from the GitHub Actions page:

https://github.com/FrapsSid/SkeletonCardGame/actions/workflows/ci.yml

---

## CI/CD

The project uses GitHub Actions for continuous integration and build
automation.

On repository updates, the pipeline:

1. Checks out the repository.
2. Sets up the Unity environment.
3. Runs the automated test suite.
4. Produces a build artifact.

| Resource | Link |
|---|---|
| CI Workflow File | https://github.com/FrapsSid/SkeletonCardGame/blob/main/.github/workflows/ci.yml |
| Build Runs | https://github.com/FrapsSid/SkeletonCardGame/actions/workflows/ci.yml |

---

## Testing

Current automated test coverage:

| Type | Scope |
|---|---|
| Unit tests | Card combination rules and logic |
| Integration tests | Match flow phase transitions |

To run tests in Unity:

1. Open the project in Unity.
2. Go to Window > General > Test Runner.
3. Press Run All.

---

## Project Structure

```text
Assets/
├── Scenes/           Game scenes: menu, bar, multiplayer lobby
├── Scripts/
│   ├── GameLoop/     Round phases, match management, GameManager
│   ├── Cards/        Card data, combinations, hand management,
│   │                 visibility rules
│   ├── Betting/      Stakes, declarations, pot resolution
│   ├── Player/       Player controller, AI controller, skeleton body
│   ├── Inventory/    Pickup, inventory, item transfer
│   ├── Multiplayer/  Networking, lobby, Discord login, synchronization
│   ├── Auth/         Login, registration, database client
│   └── UI/           UI panels, tutorial, settings, results screens
├── Prefabs/          Reusable game objects
├── Art/
│   ├── Models/       Skeleton, phylactery, bar environment, props
│   ├── Textures/     Textures and materials
│   └── UI/           2D assets, combination cards, backgrounds
├── Audio/            Sound effects
└── Tests/            Unit and integration tests
```

---

## Design Documents

| Document | Link |
|---|---|
| Figma UI & Design | https://www.figma.com/design/nSY2k25bwaS6qqZ3E67lHs/ |

---

## Team

| Name | Role |
|---|---|
| Максим Батуев | Team Lead |
| Васильев Никита | 2D Artist / Programmer |
| Егор Парубчишин | 3D Artist |
| Багаутдинов Тамерлан | Game Designer / Programmer |
| Езовских Дмитрий | Programmer |
| Ярослав Воронин | Programmer |
| Нурбек Хабибуллин | Programmer |
| Илья Казачков | Programmer |

---

## Track

This project was developed under the **Startup Track** of the Practicum
Project course at **Innopolis University**.

Target customer segment: players interested in social bluffing, party
strategy, and stylized tabletop-inspired multiplayer experiences.
