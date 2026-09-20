# Survival Legend presentation direction

## Visual source of truth

The first playable keeps the web game's `relic` UI theme and its current SD visual version.  Unity uses the exact source projection:

```text
screenX = 640 + (worldX - worldY) * 0.39
screenY = 260 + (worldX + worldY - 1440) * 0.155
```

The arena is a 1440 × 1440 world shown on a 1280 × 720 presentation canvas.  The 80-unit alternating stone tiles, shallow south/east plinth, ritual rings, corner lamps, actor shadows and muted blue-green backdrop come directly from `render.ts` and `camera.ts`.

Player textures are committed finite atlases baked from the rectangle coordinates, restricted palettes and world-space weapon rig in `visual-versions/sd/body.ts`, `draw.ts`, and `defaults.ts`. They preserve eight source-world directions, two walk frames and three attack frames with point filtering. The swordsman keeps plate, red cape and greatsword; the archer keeps blond hair, long ears, diagonal leather strap, quiver and large bow; the mage keeps the violet hood and projected staff gem. Enemy textures use the same finite eight-direction runtime cache based on `enemies.ts`. Enemy roles remain readable by silhouette: horned chaser, skull crossbow shooter, hooded caster and oversized crowned executioner.

All 24 `v2-pixel` PNG icons are copied unchanged into `Assets/SurvivalLegend/Resources/Art/Icons`.  The older screenshots in `Docs/Porting/Reference` are retained for composition and spacing; some predate the latest icon replacement, so the current PNG files take precedence for icon appearance.

## Interface

The title uses the original arena as a dim scene, three SD hero figures, wide serif-like title spacing, brass double frame, mist veil and the same top/bottom microcopy.  The product name is **SURVIVAL LEGEND** for this Unity port.

The start flow exposes the swordsman, archer and mage plus the D/F loadout selection. It keeps the source three-card choice, dark inset surfaces, brass frame, per-class accent, Korean traits, four-skill preview and control summary.

The combat HUD keeps the source layout:

- left gold ultimate orb with R and charge percentage;
- center Q/W/E + D/F console with current v2 pixel icons, cooldown veil, key labels, dodge status and stats;
- right red vitality orb with current/max health and shield state;
- framed level/time/kills bar above the field and the thin XP strip along the bottom.

Zones use the exact ground projection, hostile warnings are red/orange, friendly areas are pale lime, arrows retain the ivory-green source color, enemy fire is coral, and lightning uses the same three-layer blue-white jagged path idea.  Damage numbers, shield ellipse, move marker, health bars, elite aura and elite reward flash are presentation-only.

Augment, pause, death and result overlays use the same dark panel, inner inset border, brass outer edge, teal focus and tier colors as the source UI.  Results label total kills and elite kills separately; normal kills can be derived as `Kills - EliteKills` where needed.

## Font and platform behavior

Korean labels use the bundled static `NotoSansKR-Regular.otf` and `NotoSansKR-Bold.otf`, licensed under SIL Open Font License 1.1.  The license is included beside the fonts.  Static weights avoid the ultra-thin fallback Unity can produce from a variable font.  The UI never requests an OS font at runtime, so the same Hangul glyphs and weights are available in desktop and WebGL players.

The presentation uses Unity IMGUI and runtime-baked `Texture2D` art.  It scales the 1280 × 720 reference canvas uniformly with letterboxing, which keeps the original arena and actor proportions across desktop window sizes and WebGL embeds.  Pointer coordinates are inverted through the exact source camera transform before being assigned to `SurvivorGame.CursorWorld`.

## Reference captures

- `Reference/original-title.png`: title hierarchy, frame, hero staging and action placement.
- `Reference/original-character-select.png`: selection card, loadout panel and copy hierarchy.
- `Reference/original-sd-live-game.png`: arena scale, SD actor scale and orb console placement.  This capture uses an older icon revision.
- `Reference/original-augments.png`: three-card overlay, veil, typography and tier treatment.
