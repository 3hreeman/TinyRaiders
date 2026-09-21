# Game Database authoring

`GameDatabase` is editable source content. `BuildSnapshot()` validates it and creates an independent `GameContentSnapshot` for one run. The simulation only reads that snapshot; it never retains or mutates a `ScriptableObject`.

Use **Survival Legend > Data > Ensure Game Database** to create `Assets/Resources/Data/SurvivalLegendGameDatabase.asset` and its linked source-parity definition assets. `GameDatabaseSeed.EnsureDatabase()` is one-shot: if the database asset already exists it returns immediately, preserving all edits and deliberate deletions.

If an early initial seed is detected with every expected definition ID blank, use **Survival Legend > Data > Repair Blank Initial Seed**. The repair requires that exact all-blank baseline shape, restores source values into the existing individual assets, and preserves their GUIDs and the database GUID. It never runs automatically and refuses any database with authored IDs.

Use **Survival Legend > Data > Database Manager** to search, create, duplicate, unregister, validate, and save content. Registry removal removes an offering from subsequent snapshots but deliberately leaves its asset on disk, allowing it to be re-added. The manager exposes missing references so they can be removed, uses unique IDs for created/duplicated content, and records registry changes for Undo.

Characters reference exactly four skills; enemies reference pattern skills; class augments reference their character and patched skill. The normal/elite spawn pools define spawn roles without gameplay ID switches. New definitions become playable through the generic interpreters once their required references and pool entries are registered.

`BuildSnapshot()` reports blank/duplicate IDs, missing references, invalid defaults, unsafe fixed-step or arena values, invalid spawn stages, unsupported role strings, and unsafe delivery timings. Fix errors before starting a run.

Map bounds and spawn points must be passed into simulation by calling `snapshot.WithArena(width, height, inset, spawnPoints)` before constructing a run. Decorative map colliders do not control gameplay.
