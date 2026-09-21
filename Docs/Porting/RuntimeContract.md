# Runtime and authoring API contract

## Authored GameObject scene

`SurvivalLegend.Editor.MigrationBootstrap.EnsureProject()` creates missing database and visual assets only. `CreateScene()` creates `Assets/SurvivalLegend/Scenes/SurvivalLegendGameObjects.unity` with saved Systems/Game Session, Player Input Router, camera, World/map/presentation bindings and Canvas/EventSystem. If that scene exists, it is opened without regeneration. Nothing generates assets or edits scenes on Play or editor reload. `SetAsDefaultScene()` is a separate explicit command for the integration owner after verification. The original `SurvivalLegend.unity` is retained.

`SurvivorGame` is the session host. Serialized `Database : SurvivalLegend.Data.GameDatabase` and `ArenaMap : SurvivalLegend.World.ArenaMapView` supply authored content. `Initialize()` and `StartRun(uint seed=1,string specialD=null,string specialF=null,string character=null)` validate and copy the database; null IDs select database defaults. Missing/invalid database fails visibly through `InitializationError` and `CanStart == false`. Authored scenes never silently use the built-in catalog. `Content : GameContentSnapshot` exposes the current run definitions to Canvas and views. Edits to assets affect the next run; an active simulation retains its independent snapshot.

`GameSimulation(uint seed=1,string specialD=null,string specialF=null,string character=null,GameContentSnapshot content=null)` preserves baseline test callers. Only its omitted-content test path uses `GameContentSnapshot.CreateDefault()`. All live gameplay reads `Content`. The old static `KillsForNextLevel(int)` and `MitigatedDamage(float,float)` are baseline test compatibility helpers; live calculations use the snapshot instance. No static active database exists.

Characters, skill IDs, special IDs, enemy IDs, normal/special augment pools, and normal/elite spawn pools come from instance registries. Built-in character IDs are swordsman/archer/mage; new IDs require no roster switch in gameplay. Basic attack behavior is the definition BasicKind (melee/projectile/chain). Elite identity is membership in EliteSpawnPool, not the literal ID or display category. Spawn pools use time-ramped weights and one remainder entry. Authored map marker positions are copied through ArenaMapView.BuildSnapshot/WithArena; empty marker lists preserve source edge spawning. Config arena dimensions/inset control bounds and spawning.

## Identity, lifetime and visual events

`SurvivorGame.Generation : int` changes on each reset/title/retry and on initialization failure. `event Action<int> RunReset` notifies views to clear bindings/pools. Gameplay object IDs remain ints unique within a run. Views identify entities by (Generation, Id), reconcile against live lists, and never drive damage or movement from transforms, physics or animation callbacks.

`EffectState.Id` and `Tick` are longs. They use a separate visual counter and never consume RNG or gameplay IDs. `Simulation.DrainVisualEvents(List<EffectState> destination)` appends queued independent effect copies and drains once. It preserves transient events even when multiple simulation steps occur before rendering. `State.Effects` also retains live effects for lifetime reconciliation. Existing Kind/Text/Color/Position/End/Life/MaxLife/Radius/Critical/Elevation/FromStaff fields remain available. The caller should clear its list before draining each frame.

## Coordinates and input

`SurvivalLegend.World.WorldProjection` is the only camera/pointer/world mapping: `WorldToView`, `ViewToWorld`, `WorldToScene`, `SceneToWorld`, `ScreenToWorld`, `ViewportPixels`, `ConfigureCamera`, and `SortingOrder`.

The original logical map defaults to 1440 by 1440. Projection is view x=640+(x-y)*.39, y=260+(x+y-1440)*.155. The shared view is 1280 by 720; scene XY uses 100 pixels per unit, camera orthographic size3.6, and centered letterboxing. Elevation subtracts from projected view Y; collision stays on logical ground. Changing arena bounds changes playable geometry, while projection scale/origin remain fixed deliberately.

`PlayerInputRouter` is a saved component (execution order50) with explicit Game/WorldCamera references. It sets CursorWorld and PointerOverUi using the shared projection, camera viewport and EventSystem raycasts. `SurvivorGame` steps simulation at order100; world views update afterward. QWER/DF, left Shift dodge, A then enemy/ground click, S stop, Escape/Space pause, right-click explicit attack/move and 250ms held movement preserve source controls. Right-click during aiming cancels without moving. UI/modal phases block pointer world commands. QuickCast and QuickCastSlots[6] remain on the host. WASD is not movement.

## Combat and state

Namespace `SurvivalLegend`. Positions are Vector2 and facing angles are source-world radians. Simulation reads neither Unity Time/Input nor Unity Random. FixedStep defaults to 1/60. Source transitions are `Title, Playing, Paused, LevelUp, AugmentEnter, Augment, EliteDeath, Dying, Results`. Kills includes elites; ordinary results kills equals Kills-EliteKills.

Commands on host and simulation: TogglePause, ChooseAugment(index), RerollAugment(index), CastSkill(slot,target), UseSpecial(slot,target), Dodge(target), MoveTo(point), AttackMove(point), Attack(enemyId), Stop. Host adds StartRun, ReturnToTitle, RequestCast(slot). Simulation adds Step(delta), SpawnEnemy(kind,position,level), DamageEnemy(enemy,amount,charge=true), DamagePlayer(amount,sourceId=-1), RestoreHealth(amount), IsElite(enemyId), GetSkillTargeting(slot), CooldownRecovery(slot).

RunState exposes Player, Enemies, Projectiles, Zones, Effects, Buffs, Chains, AugmentChoices, History, Skills[4], Specials[2], Character, phase/timers, statistics, dodge charges, destination/target/order and AimSlot (-1 none,0..3 skills,4..5 specials,6 attack). Player motion/facing/animation and projectile flight elevation drive eight-direction view resources. Damage/health/shield stats remain simulation-owned. Buffs contain temporary damage/attack/movement/cooldown/incoming modifiers and empowered melee charges. Player SkillShield and its duration/reflection are independent of D/F Shield.

The player skill interpreter supports buff/shield/teleport/area/projectile/rain/zone definitions, ordered added effects, stat/haste/patch/repeat augments, root, explosion, penetration, delayed/follow-caster zones, and unique delayed mage chains. GetSkillTargeting applies augment patches for previews. Ultimate charge rules use skill metadata, not slot index. Enemy patterns cycle in registry order and execute all area/projectile/zone effects. New delivery algorithms require explicit interpreter support and authoring validation; changing a data ID never adds executable behavior.

## Verification boundaries

Existing archer and full-roster source parity suites remain unchanged. SnapshotIntegrationTests cover authored stat edits affecting next run only, snapshot deep-copy isolation, renamed skill/ultimate metadata, added enemies and pool-defined elites, authored bounds/spawn markers, visual event RNG independence, and reversible projection. Parent runs Unity compilation, all tests, scene generation and visual checks; no platform builds are part of this migration.
