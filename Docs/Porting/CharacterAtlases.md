# SD character atlas contract

`Tools/Art/bake_sd_atlases.py` bakes the three playable character textures in
`Assets/SurvivalLegend/Resources/Art/Characters`. The baker ports the rectangle
coordinates, palettes and rig math from the original `visual-versions/sd` code;
it does not synthesize or reinterpret the characters.

Each PNG is a **768 × 1024** atlas made of **128 × 128** RGBA cells:

- rows `0..7`: source-world facing angles `0°, 45°, 90°, 135°, 180°, 225°, 270°, 315°`;
- columns `0..5`: `idle`, `walk-a`, `walk-b`, `attack-a`, `attack-b`, `attack-c`;
- frame foot origin: `(64, 112)` measured from the cell's top-left;
- every frame has at least 14 transparent pixels before the atlas cell boundary.

Facing stays in source-world radians. A direction is selected with the same
nearest-octant rule used by the web version. The body applies the source SD
two-pixel grid internally. Weapon points remain at unscaled `rigPoint` coordinates,
then rotate in the world plane and project with `(x-y)*.39`, `(x+y)*.155-height`.
This is why side and diagonal weapons are unique drawings and are not mirrored
front frames. Weapon depth decides whether the complete arm/hand/weapon rig is
painted before or after the body.

The committed `.meta` files set point filtering, clamp wrapping, alpha transparency,
no mipmaps and no texture compression. `SurvivalLegendArt.PlayerFrameUv` reads the
grid directly, so no Unity Sprite Editor slicing is required. Re-run the baker after
changing its source-port coordinates; it also regenerates the labeled review sheet
`Docs/Porting/CharacterAtlasOverview.png` and preserves fixed Unity GUIDs.

![Eight-direction player overview](CharacterAtlasOverview.png)
