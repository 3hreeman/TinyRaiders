# Survival Legend — first playable

## Approved scope

- Targets: Windows PC and Unity WebGL; keyboard and mouse.
- Source: `D:/Codex/Arcade`, commit `9452bb8`; source game currently titled Survival League.
- Preserve the original SD V3 visual direction, quarter-view projection, commands and balance.
- One playable class: archer. Complete title/loadout → combat → level-up augmentation → elite → special reward → death/results/retry loop.
- Implement QWER, four selectable D/F abilities, attack move, dodge, pause, cooldowns, growth, regeneration and lifesteal.
- Further characters and expanded features require the user's approval after trying this slice.

## Approved follow-up: full original roster and eight directions

The user accepted the archer slice and approved swordsman and mage implementation, plus eight-direction character resources for all three classes. Preserve the source mechanics, class-specific augments and SD visual direction. Include class selection and retry using the selected class. Validate compilation, simulation behavior and Unity Play Mode; do not produce additional Windows or WebGL builds for this follow-up. Existing `Builds/` artifacts remain the earlier archer preview.

## Team

The parent agent coordinates scope, interfaces, integration and validation. User permits model adjustments if useful.

| Role | Initial model | Ownership |
| --- | --- | --- |
| Art director | Sol, high | Original SD graphics, animation, HUD/UI, art resources |
| Designer | Terra, high | Source data, balance catalog, parity criteria |
| Engineer | Astra, high | Simulation, inputs, lifecycle, scene/bootstrap/build helpers |
| Coordinator | Parent | Integration, tests, builds and delivery |

## Acceptance

1. Open the supplied scene and play without manual component wiring.
2. Readable original-style quarter-view field, SD archer/enemies, HUD and augmentation cards.
3. Right-click move/attack, A attack move, S stop, QWER and D/F targeting, left Shift dodge, Escape cancel/pause.
4. Archer's four distinct skills, explicit attack orders, enemy attack warnings and elite patterns.
5. Three kills reach level 2; level-up restores HP; augmentation selection pauses combat/cooldowns.
6. Level 5 triggers an elite; its death and special reward resume normal spawning correctly.
7. Death freezes combat statistics; retry creates a clean run.
8. Automated simulation checks, Unity compilation and a rendered play-mode inspection.
9. Produce local platform builds when their installed Unity modules permit; distinguish unverified targets in delivery.

## Source reference

Authoritative values live in TypeScript catalogs under `apps/web/src/games/survivor`, not the exported spreadsheet/JSON snapshot. Existing source baseline: 26 test files / 213 tests passed. Do not modify the source web project while porting.

The original procedural graphics require recreation/baking; there is no existing complete character sprite sheet to copy. The 24 pixel skill/stat icons can be reused directly.
