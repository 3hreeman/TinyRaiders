# Archer first-playable balance parity

The data in `Assets/SurvivalLegend/Runtime/Data/SurvivalLegendCatalog.cs` is mapped from the live web runtime under `D:/Codex/Arcade/apps/web/src/games/survivor`, rather than from the older arena documents. `Assets/Resources/Data/SurvivalLegendCatalog.json` is a compact review/export copy; simulation must use the typed catalog where detail differs.

## Full roster parity

`Characters` contains the complete source roster. Swordsman is **180 HP, 164 speed, 1.3 attacks/s, 25.6 damage, 112 range, 5 regeneration, 15 armor** and uses the greatsword. Archer remains **120/216/1.7/17.6/300/3/8** with the bow. Mage is **90 HP, 188 speed, 0.72 attacks/s, 22.4 damage, 330 range, 3 regeneration, 0 armor** with the staff; its basic attack chains at **100%, 75%, 50%** over 160 range.

- Swordsman Q is an until-hit next attack: 2× primary damage plus 75% splash in radius 110, one charge. W is a 3 s shield for 20% max HP with 30% reflect. E supplies 1.15× movement for 5 s and a radius-145, 75%-damage following zone every 0.25 s. R is a 10× zone at range 600, radius 180 with a radius-60 2× core, delayed 0.45 s.
- Mage Q is a 1.5× projectile at 480 speed, range 700, radius 13, with zero base penetrations and explosion radius 110. W is radius 190, 1× damage and 1 s root. E teleports up to 280 then deals 1× in radius 120. R lasts 10 s with 2× outgoing damage and 6× cooldown recovery; its base incoming multiplier is 1.
- The special pool has **22** items: five common, four swordsman, four archer, five mage, and four global haste amplifiers. Filter a non-empty `CharacterId` against the selected roster; unscoped common and haste cards remain eligible to all characters.
- Swordsman cards patch Q charges +1; inject a 3 s 1.2× movement buff into W; multiply E's following-zone interval by 0.9; and multiply all four R outer/core targeting/effect radii by 1.5. Mage cards add one basic chain target, Q penetration +1, W targeting/effect radius ×1.25, E range ×1.2, and R incoming multiplier ×0.75.

## Acceptance checklist

- Archer starts at **120 HP, 216 speed, 1.7 attacks/s, 17.6 damage, 300 range, 3 health regeneration/s, 8 armor**, 5% crit chance, and 1.5× crit damage. On every level, rebuild from base: damage +8%, speed +1.8%, attack speed +2.5%, max HP +6%; then apply flat bonuses and percentage bonuses. Level-up restores all currently missing health after that max-HP rebuild.
- Q has 11 s cooldown and for 5 s grants 1.5× attack speed plus two secondary arrow targets. W fires seven 75% arrows at 700 speed across 45° with range 600/radius 6 on 7 s cooldown. E is a 150%, piercing projectile at 760 speed with range 850/radius 12 on 8 s cooldown. R has no cooldown, requires 100 ultimate charge, and fires 50 50%-damage arrows twice over a 580×360 rectangle at 1.1 s intervals.
- Each successful damaging hit grants 5 ultimate, capped at 100. Ultimate hits and each falling-arrow impact from R grant **no** ultimate. Lifesteal is calculated from actual damage dealt after shields and overkill clipping.
- Q/W/E haste recovery rate is `1 + (flatHaste * (1 + percentHaste)) / 100`; apply `all` plus the skill slot. R, D, F, and dodge never receive haste. Buff cooldown-rate multipliers multiply recovery rate, while remaining cooldown work is never rescaled when a haste source changes.
- D/F use real combat seconds: heal restores 20% of missing HP (90 s); haste is 1.5× movement for 5 s (60 s); barrier gives 25% max-HP shield for 5 s (45 s); black hole pulls within radius 180, up to range 450, for 3 s at 240 (90 s).
- Dodge has two sequential charges. It travels 180 over 0.2 s, is invulnerable for the entire motion, and begins a 10 s recharge only when spending from full. Spending the second charge does not reset the first recharge; simulation pauses freeze recharging.
- Normal enemies scale from run level: HP +4.5%/level, damage +3.5%/level, speed +0.9%/level. Spawn pacing interpolates stages rather than stepping at boundaries: `(0,2.4,1,16)`, `(30,1.9,1.25,28)`, `(60,1.5,1.75,45)`, `(120,1.15,2.5,75)`, `(180,.95,3.25,110)`, `(300,.8,4,160)`. Fractional batch counts are randomized and no backlog is accumulated while at cap.
- The level requirement is `0.5 * level² + 1.5 * level + 1`: 3, 6, 10, 15, 21 at levels 1–5. A level transition keeps combat live for 0.85 s, then cards enter for 0.65 s; cards freeze combat only after entering.
- Every fifth reached level queues an elite. The elite stops ordinary spawns. On elite death, remove hostile projectiles/zones, hold the death presentation for 2.4 s, then offer exactly three special cards. If a normal card offer was pending, preserve it untouched and restore it after the special selection. Queue later fifth-level milestones; do not discard them.

## Catalog API for gameplay

Use `SurvivalLegend.Data.SurvivalLegendCatalog`:

- `Config`, `Combat`, `Rain`, and `Rewards` contain timing and global constants.
- `Archer`, `Skills`, `Specials`, `Enemies`, and `SpawnStages` provide typed content.
- `NormalAugments` provides silver/gold/prism values. `SpecialAugments` is the exact 13-item pool eligible to the archer: five common, four archer, and four global haste amplifiers.
- `KillsForNextLevel`, `CooldownRecovery`, and `MitigatedDamage` expose source formulas for deterministic gameplay tests.
