# Skill FX art resources

Run `Survival Legend > Build FX Textures` or call `SurvivalLegend.Editor.FX.FxTextureBuilder.EnsureAssets()` from the editor authoring pipeline. It creates four transparent, white RGB / alpha-shaped PNGs and two shared material assets only when missing. Regeneration does not overwrite artist edits to existing PNGs or materials.

| Asset | Native size | Intended use |
| --- | ---: | --- |
| `soft-glow.png` | 128×128 | Small light blooms, hit glints, healing particles |
| `spark-streak.png` | 128×128 | Thin directional sparks, arrow trails, lightning flecks |
| `crescent-slash.png` | 256×256 | Sword arcs and slash sweeps |
| `vortex-soft.png` | 256×256 | Pull/black-hole swirls with alpha blending |

`FXAdditive.mat` uses `SurvivalLegend/FXAdditive` (`Blend SrcAlpha One`); `FXAlpha.mat` uses `SurvivalLegend/FXAlpha` (`Blend SrcAlpha OneMinusSrcAlpha`). Both shaders have Built-in and URP unlit passes, use `_MainTex` × material `_Color` × particle vertex color, disable depth writes and culling, and keep transparent backgrounds. Use shared material assets on ParticleSystem renderers. Do not instantiate materials at runtime. Additional per-texture material variants can use the same saved shaders and assign a different main texture.

Tint at the particle renderer/profile level: sword gold and ivory, archer leaf green and cyan, mage ember orange, ice blue and violet, heal emerald, haste gold, pull purple, shield teal. Keep particle sprites small around the SD actors; the glow texture's center alpha is intentionally limited to avoid washing out pixel art. The dark vortex uses the alpha material so a muted violet or charcoal tint retains its interior structure.
