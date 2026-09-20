"""Bake Survival Legend's source-faithful SD character atlases.

Layout: 6 columns (idle, walk-a, walk-b, attack-a, attack-b, attack-c)
        x 8 rows (world-facing angles 0,45,...315 degrees).
Each frame is 128x128 RGBA with the foot origin at (64, 112). The body uses the
source body.ts 2x pixel grid while weapon rig coordinates remain unscaled, exactly
as draw.ts composes them in the same context.

The drawing coordinates, palettes, front/back test and quarter-view rig math are
ported from survivor/visual-versions/sd/{body,draw,defaults}.ts.  Pillow is used
only as a deterministic PNG encoder; no interpolation or generated art is used.
"""

from __future__ import annotations

import math
from pathlib import Path
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "Assets" / "SurvivalLegend" / "Resources" / "Art" / "Characters"
OVERVIEW = ROOT / "Docs" / "Porting" / "CharacterAtlasOverview.png"
CELL_W, CELL_H, COLUMNS, ROWS = 128, 128, 6, 8
ORIGIN_X, ORIGIN_Y, PIXEL = 64, 112, 2
OUTLINE = "#20232b"

PALETTES = {
    "swordsman": dict(primary="#8b9dac", trim="#e3ba71", shadow="#283440", skin="#dcb492", hair="#483423", cloth="#9c3540", leather="#51372a", eye="#c8f0ff"),
    "archer": dict(primary="#376856", trim="#e8cd83", shadow="#193d36", skin="#f1c8ac", hair="#f0ce68", cloth="#588876", leather="#664830", eye="#65dcbb"),
    "mage": dict(primary="#635391", trim="#c5b3f4", shadow="#201d3b", skin="#c4bdd8", hair="#cfcced", cloth="#383060", leather="#77648e", eye="#80eaff"),
}
GUIDS = {
    "swordsman": "eebaf35cb60e4ad99e3018501dffe88f",
    "archer": "492d9f7c53b747edb3a9ff0e230d0df3",
    "mage": "b58cae745f3b481193f61e292272334d",
}


def rig(angle: float, forward: float, side: float, height: float = 0.0) -> tuple[float, float, float]:
    """Source rigPoint. drawSdBody applies its own 2x scale; the weapon does not."""
    x = math.cos(angle) * forward - math.sin(angle) * side
    y = math.sin(angle) * forward + math.cos(angle) * side
    return ((x - y) * .39, (x + y) * .155 - height, (x + y) * .155)


class Canvas:
    def __init__(self) -> None:
        self.image = Image.new("RGBA", (CELL_W, CELL_H), (0, 0, 0, 0))
        self.draw = ImageDraw.Draw(self.image)

    def rect(self, x: float, y: float, w: float, h: float, color: str) -> None:
        x0, y0 = round(ORIGIN_X + x * PIXEL), round(ORIGIN_Y + y * PIXEL)
        self.draw.rectangle((x0, y0, x0 + round(w * PIXEL) - 1, y0 + round(h * PIXEL) - 1), fill=color)

    def box(self, x: float, y: float, w: float, h: float, color: str) -> None:
        self.rect(x, y, w, h, OUTLINE)
        self.rect(x + 1, y + 1, w - 2, h - 2, color)

    def point(self, p: tuple[float, float, float]) -> tuple[int, int]:
        return round(ORIGIN_X + p[0]), round(ORIGIN_Y + p[1])

    def line(self, a: tuple[float, float, float], b: tuple[float, float, float], color: str, width: int) -> None:
        self.draw.line((self.point(a), self.point(b)), fill=color, width=width, joint="curve")

    def disc(self, p: tuple[float, float, float], radius: float, color: str) -> None:
        x, y = self.point(p)
        r = round(radius)
        self.draw.ellipse((x-r, y-r, x+r, y+r), fill=color)

    def polygon(self, points: list[tuple[float, float, float]], fill: str, outline: str | None = None) -> None:
        xy = [self.point(p) for p in points]
        self.draw.polygon(xy, fill=fill)
        if outline:
            self.draw.line(xy + [xy[0]], fill=outline, width=2, joint="curve")


def body(c: Canvas, hero: str, angle: float, step: int) -> None:
    p = PALETTES[hero]
    f = rig(angle, 1, 0)
    front = f[2] >= 0
    side = -1 if f[0] < -.12 * PIXEL else 1 if f[0] > .12 * PIXEL else 0
    face = side * 2

    def cape() -> None:
        if hero == "archer": return
        c.box(-6-side, -14, 12, 13, p["cloth"])
        c.rect(-4-side, -12, 2, 10, p["primary"])
        c.rect(2-side, -10, 2, 9, p["shadow"])

    def hair() -> None:
        if hero != "archer": return
        c.box(-7, -21, 14, 15, p["hair"])
        c.rect(-5, -17, 2, 10, p["trim"])
        c.rect(3, -17, 2, 11, p["leather"])

    def quiver() -> None:
        if hero != "archer": return
        c.box(-9, -17, 4, 10, p["leather"])
        c.rect(-8, -22, 1, 9, p["trim"]); c.rect(-6, -21, 1, 7, p["trim"])
        c.rect(-9, -23, 2, 3, "#e5e0cf"); c.rect(-7, -22, 2, 3, "#e5e0cf")

    if front: cape(); hair(); quiver()
    c.box(-5, -4-step, 5, 4, p["leather"]); c.box(1, -4+step, 5, 4, p["leather"])
    c.box(-5, -14, 11, 11, p["cloth"] if hero == "mage" else p["primary"])
    c.rect(-3, -12, 3, 6, p["primary"]); c.rect(2, -12, 2, 7, p["shadow"])
    c.box(-8, -13, 4, 7, p["primary"] if hero == "swordsman" else p["cloth"])
    c.box(5, -13, 4, 7, p["primary"] if hero == "swordsman" else p["cloth"])
    c.rect(-7, -7, 2, 2, p["trim"] if hero == "swordsman" else p["skin"])
    c.rect(6, -7, 2, 2, p["trim"] if hero == "swordsman" else p["skin"])
    if front:
        c.rect(-4, -6, 9, 2, p["leather"]); c.rect(0, -6, 2, 2, p["trim"])
        if hero == "swordsman": c.rect(-4, -13, 9, 2, p["trim"]); c.rect(-2, -10, 5, 2, "#cad6d8")
        elif hero == "archer":
            for i in range(6): c.rect(-3+i, -13+i, 2, 2, p["leather"])
        else: c.rect(-4, -5, 9, 3, p["cloth"]); c.rect(0, -12, 1, 8, p["trim"]); c.rect(-1, -11, 3, 2, p["eye"])
    if not front: cape(); hair(); quiver()
    c.rect(-7, -30, 14, 1, OUTLINE); c.rect(-8, -29, 16, 15, OUTLINE); c.rect(-6, -14, 12, 1, OUTLINE)
    head = p["hair"] if hero == "archer" else p["cloth"] if hero == "mage" else p["primary"]
    c.rect(-7, -28, 14, 13, head); c.rect(-5, -29, 10, 2, p["hair"] if hero == "archer" else p["trim"])
    if front:
        c.rect(-5+face, -25, 10 if side == 0 else 9, 9, p["shadow"] if hero == "mage" else p["skin"])
        c.rect(-3+face, -22, 1, 3, p["eye"] if hero == "mage" else OUTLINE)
        c.rect(2+face, -22, 1, 3, p["eye"] if hero == "mage" else OUTLINE)
    else:
        c.rect(-5, -25, 2, 8, p["trim"] if hero == "archer" else p["primary"]); c.rect(4, -24, 2, 9, p["shadow"])
    if hero == "swordsman":
        c.rect(-7, -28, 14, 4, p["primary"]); c.rect(-5, -29, 10, 2, "#ccd6d9")
        if front:
            c.rect(-6+face, -24, 11, 5, OUTLINE); c.rect(-4+face, -22, 7, 1, p["eye"]); c.rect(-1+face, -25, 2, 11, p["trim"])
        c.rect(-1, -32, 2, 3, p["cloth"])
    elif hero == "archer":
        c.rect(-7, -28, 14, 4, p["hair"]); c.rect(-7, -24, 3, 5, p["hair"]); c.rect(5, -24, 2, 8, p["hair"])
        c.rect(-3+face, -25, 3, 2, p["hair"]); c.rect(face, -27, 1, 2, p["eye"] if front else p["hair"])
        c.rect(-5, -28, 9, 1, p["trim"])
        c.rect(-11, -23, 4, 2, OUTLINE); c.rect(-10, -22, 4, 2, p["skin"]); c.rect(8, -23, 3, 2, OUTLINE); c.rect(7, -22, 3, 2, p["skin"])
    else:
        c.rect(-3, -32, 6, 2, OUTLINE); c.rect(-2, -31, 4, 2, p["primary"])
        c.rect(-7, -27, 2, 11, p["primary"]); c.rect(5, -27, 2, 11, p["shadow"])


def weapon(c: Canvas, hero: str, angle: float, attack_phase: float | None) -> None:
    p = PALETTES[hero]
    pulse = 0 if attack_phase is None else math.sin(math.pi * attack_phase)
    weapon_angle, weapon_raise, reach = angle, 0.0, 0.0
    if attack_phase is not None:
        if hero == "swordsman":
            cut = max(0.0, min(1.0, (attack_phase - .16) / .52))
            eased = cut * cut * (3 - 2 * cut)
            recovery = max(0.0, (attack_phase - .68) / .32)
            weapon_angle += (-1.15 + eased * 2.3) * (1 - recovery)
            weapon_raise = (44 - eased * 64) * (1 - recovery)
        elif hero == "archer": reach = -pulse * 9
        else: reach = pulse * 17
    point = lambda f,s,h: rig(angle, f, s, h)
    wp = lambda f,s,h: rig(weapon_angle, f, s, h)
    hand = wp(13+reach, 10, 29+weapon_raise*.3)
    shoulder = point(0, 10, 24)
    c.line(shoulder, hand, p["primary"], 5); c.disc(hand, 3, p["skin"])
    if hero == "swordsman":
        base, tip = 13+reach, 66
        blade = [wp(base+6,6,30+weapon_raise*.35), wp(tip-10,6,30+weapon_raise), wp(tip,10,31+weapon_raise), wp(tip-10,14,30+weapon_raise), wp(base+6,14,30+weapon_raise*.35)]
        c.polygon(blade, "#dce6ee", p["trim"])
        c.line(wp(base,1,29+weapon_raise*.3), wp(base,19,29+weapon_raise*.3), p["trim"], 4)
        c.line(wp(base-10,10,29+weapon_raise*.3), wp(base+4,10,29+weapon_raise*.3), p["leather"], 5)
    elif hero == "archer":
        f = 28+reach; top=wp(f,-17,43+weapon_raise); bottom=wp(f,17,17+weapon_raise); middle=wp(f+17,0,30+weapon_raise)
        # Source uses a quadratic bow; a three-segment stepped arc stays crisp in the atlas.
        upper=wp(f+11,-9,34+weapon_raise); lower=wp(f+11,9,26+weapon_raise)
        c.line(top, upper, p["trim"], 3); c.line(upper, middle, p["trim"], 3); c.line(middle, lower, p["trim"], 3); c.line(lower, bottom, p["trim"], 3)
        pull=wp(f-6-pulse*16,0,30+weapon_raise); c.line(top,pull,"#f2e3b9",1); c.line(pull,bottom,"#f2e3b9",1)
        if attack_phase is None or attack_phase > .55:
            tail=wp(f-10,0,30+weapon_raise); tip=wp(f+31,0,30+weapon_raise)
            c.line(tail,tip,"#fff0bd",2); c.polygon([tip,wp(f+24,-4,30+weapon_raise),wp(f+24,4,30+weapon_raise)],"#fff0bd",p["trim"])
        c.line(point(0,-10,24),pull,p["primary"],4)
    else:
        foot=wp(12+reach,13,7+weapon_raise*.2); gem=wp(25+reach,13,57+weapon_raise)
        c.line(foot,gem,p["leather"],5)
        c.polygon([(gem[0],gem[1]-9,gem[2]),(gem[0]+6,gem[1],gem[2]),(gem[0],gem[1]+9,gem[2]),(gem[0]-6,gem[1],gem[2])],p["trim"],OUTLINE)
        c.disc(gem, 3 + pulse*3, "#fff1ff" if attack_phase is None else "#91e5ff")


def render_frame(hero: str, direction: int, column: int) -> Image.Image:
    c = Canvas(); angle = direction * math.pi / 4
    step = -1 if column == 1 else 1 if column == 2 else 0
    attack_phase = (column - 3) / 2 if column >= 3 else None
    weapon_depth = rig(angle if attack_phase is None or hero != "swordsman" else angle + (-1.15 + attack_phase*2.3), 32, 10)[2]
    if weapon_depth < 0: weapon(c, hero, angle, attack_phase)
    body(c, hero, angle, step)
    if weapon_depth >= 0: weapon(c, hero, angle, attack_phase)
    return c.image


def bake(hero: str) -> Path:
    atlas = Image.new("RGBA", (CELL_W*COLUMNS, CELL_H*ROWS), (0,0,0,0))
    for direction in range(ROWS):
        for column in range(COLUMNS):
            atlas.alpha_composite(render_frame(hero, direction, column), (column*CELL_W, direction*CELL_H))
    path = OUT / f"{hero}-sd-atlas.png"
    atlas.save(path, optimize=True)
    path.with_suffix(".png.meta").write_text(f"""fileFormatVersion: 2
guid: {GUIDS[hero]}
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {{}}
  serializedVersion: 13
  mipmaps:
    mipMapMode: 0
    enableMipMap: 0
    sRGBTexture: 1
    linearTexture: 0
    fadeOut: 0
    borderMipMap: 0
    mipMapsPreserveCoverage: 0
    alphaTestReferenceValue: 0.5
    mipMapFadeDistanceStart: 1
    mipMapFadeDistanceEnd: 3
  bumpmap:
    convertToNormalMap: 0
    externalNormalMap: 0
    heightScale: 0.25
    normalMapFilter: 0
    flipGreenChannel: 0
  isReadable: 0
  streamingMipmaps: 0
  streamingMipmapsPriority: 0
  vTOnly: 0
  ignoreMipmapLimit: 0
  grayScaleToAlpha: 0
  generateCubemap: 6
  cubemapConvolution: 0
  seamlessCubemap: 0
  textureFormat: 1
  maxTextureSize: 2048
  textureSettings:
    serializedVersion: 2
    filterMode: 0
    aniso: 1
    mipBias: 0
    wrapU: 1
    wrapV: 1
    wrapW: 1
  nPOTScale: 0
  lightmap: 0
  compressionQuality: 100
  spriteMode: 0
  spriteExtrude: 1
  spriteMeshType: 1
  alignment: 0
  spritePivot: {{x: 0.5, y: 0.5}}
  spritePixelsToUnits: 100
  spriteBorder: {{x: 0, y: 0, z: 0, w: 0}}
  spriteGenerateFallbackPhysicsShape: 0
  alphaUsage: 1
  alphaIsTransparency: 1
  spriteTessellationDetail: -1
  textureType: 0
  textureShape: 1
  singleChannelComponent: 0
  flipbookRows: 1
  flipbookColumns: 1
  maxTextureSizeSet: 0
  compressionQualitySet: 0
  textureFormatSet: 0
  ignorePngGamma: 0
  applyGammaDecoding: 0
  swizzle: 50462976
  cookieLightType: 0
  platformSettings:
  - serializedVersion: 4
    buildTarget: DefaultTexturePlatform
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 0
    compressionQuality: 100
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  spriteSheet:
    serializedVersion: 2
    sprites: []
    outline: []
    customData:
    physicsShape: []
    bones: []
    spriteID:
    internalID: 0
    vertices: []
    indices:
    edges: []
    weights: []
    secondaryTextures: []
    spriteCustomMetadata:
      entries: []
    nameFileIdTable: {{}}
  mipmapLimitGroupName:
  pSDRemoveMatte: 0
  userData: SD atlas: rows=8 world directions, columns=idle walk-a walk-b attack-a attack-b attack-c, cell=128x128, foot=64,112
  assetBundleName:
  assetBundleVariant:
""", encoding="utf-8", newline="\n")
    return path


def main() -> None:
    OUT.mkdir(parents=True, exist_ok=True)
    paths = {hero: bake(hero) for hero in PALETTES}
    for path in paths.values(): print(path.relative_to(ROOT))
    # Compact human-review sheet: one idle frame for every world direction, all classes.
    cell_w, cell_h, top, left = 128, 128, 38, 112
    sheet = Image.new("RGBA", (left + cell_w * 8 + 12, top + cell_h * 3 + 12), "#080f14")
    draw = ImageDraw.Draw(sheet)
    draw.text((12, 10), "SD PLAYER DIRECTIONS · WORLD ANGLE (quarter-view projected)", fill="#d1bb83")
    for d in range(8): draw.text((left + d*cell_w + 38, 20), f"{d*45}°", fill="#a0b4b3")
    for row, (hero, path) in enumerate(paths.items()):
        atlas = Image.open(path)
        draw.text((12, top + row*cell_h + 50), hero.upper(), fill=PALETTES[hero]["trim"])
        for d in range(8):
            frame = atlas.crop((0, d*CELL_H, CELL_W, (d+1)*CELL_H))
            x, y = left + d*cell_w, top + row*cell_h
            draw.rectangle((x, y, x+cell_w-5, y+cell_h-5), fill="#101a1d", outline="#304e4c")
            sheet.alpha_composite(frame, (x + (cell_w-CELL_W)//2, y - 5))
    OVERVIEW.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(OVERVIEW, optimize=True)
    print(OVERVIEW.relative_to(ROOT))


if __name__ == "__main__":
    main()
