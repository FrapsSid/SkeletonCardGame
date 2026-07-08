# Bet To The Bone

A multiplayer social bluffing card game where skeleton players bet their own body parts in a dark, ironic underground bar.

## About

Bet To The Bone is a stylized tabletop-inspired multiplayer game built in Unity. Players take on the role of skeletons competing in a card game where the stakes are literal — you wager your bones. The game combines card combinations, strategic betting, physical interaction with game objects, and a cheating/judge system that rewards social deception.

## How To Play

### Quick Start
1. Launch the game
2. Go through the mini-tutorial in the main menu to learn the basics
3. Start a match (currently playable against AI opponents)

### Round Flow
1. **Combination Reveal** — Four combinations are announced for the round (Easy, Medium, Hard, Anti)
2. **Card Deal** — Each player receives 2 cards, 2 cards are placed on the table
3. **Discussion Phase** — 30 seconds to move around, talk with teammates, and strategize
4. **Active Phase** — 3 iterations of turns where players can:
   - Draw a card
   - Declare or upgrade a combination target
   - Raise the participation price
   - Match the current price
   - Pass
   - Fold
5. **Round End** — Cards are revealed, combinations are checked, scores are calculated, and the pot is distributed

### Betting & Stakes
- Players bet body parts and other team assets
- Losing a body part applies gameplay debuffs (e.g., losing legs reduces movement speed)
- Losing your soul incapacitates you for the rest of the match

### Card Visibility
- Your own cards and teammate cards are visible when looking at them
- Opponent cards require using the "Peek" action and are only visible while you maintain line of sight
- A special shader hides card information from the third-person camera

## Current State

### What Works
- Full match loop with AI opponents (multiple rounds, scoring, match resolution)
- Card combinations system aligned with the GDD
- Physical cards in hand with fan display
- Inventory system (pick up, store, retrieve, drop items)
- Skeleton animations connected to gameplay events
- Sound effects
- Card visibility rules
- 2D combination cards
- Settings menu
- Mini-tutorial
- Round and match-end UI
- Participant display
- Bar environment with low-poly art style
- Multiplayer foundation (lobby, scene transition, phase sync, table-card sync)
- CI/CD with automatic Unity WebGL builds

### In Progress
- Full multiplayer synchronization
- Judge and violations system
- Gameplay balancing

## Build

### Play in Browser
[Latest WebGL Build](https://necr0manth.dev/SkeletonCardGame/index.html)
