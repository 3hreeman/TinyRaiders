using System;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalLegend
{
    /// <summary>
    /// Runtime-baked versions of the original survivor Canvas artwork.  Shapes, restricted
    /// palettes and two-head proportions are ported from visual-versions/sd/*.ts.
    /// Textures stay point-filtered so the same art is crisp in desktop and WebGL builds.
    /// </summary>
    public static class SurvivalLegendArt
    {
        public const float ViewWidth = 1280f;
        public const float ViewHeight = 720f;
        public const float WorldSize = 1440f;
        public const int PlayerAtlasColumns = 6;
        public const int PlayerAtlasDirections = 8;
        public static readonly Color32 Page = Hex("#090f11");
        public static readonly Color32 Panel = Hex("#101a1d");
        public static readonly Color32 Inset = Hex("#080e11");
        public static readonly Color32 Raised = Hex("#1c2b2c");
        public static readonly Color32 Text = Hex("#e9e4d2");
        public static readonly Color32 Muted = Hex("#a0b4b3");
        public static readonly Color32 Metal = Hex("#87734e");
        public static readonly Color32 Edge = Hex("#d1bb83");
        public static readonly Color32 Accent = Hex("#55d8bc");
        public static readonly Color32 Selection = Hex("#153c37");
        public static readonly Color32 Line = Hex("#304e4c");
        public static readonly Color32 Health = Hex("#c62e40");
        public static readonly Color32 HealthLight = Hex("#ff7d79");
        public static readonly Color32 Power = Hex("#dba84d");
        public static readonly Color32 PowerLight = Hex("#fff0a8");
        public static readonly Color32 Danger = Hex("#ff6868");

        private static readonly Dictionary<string, Texture2D> Cache = new Dictionary<string, Texture2D>();
        private static readonly Color32 Clear = new Color32(0, 0, 0, 0);
        private static readonly Color32 Outline = Hex("#20232b");

        public static Vector2 WorldToView(Vector2 point, float elevation = 0f)
        {
            return World.WorldProjection.WorldToView(point,elevation);
        }

        public static Vector2 ViewToWorld(Vector2 point)
        {
            return World.WorldProjection.ViewToWorld(point);
        }

        public static Texture2D Arena() => BakeArena("arena", true);
        public static Texture2D ArenaGround() => BakeArena("arena-ground", false);

        private static Texture2D BakeArena(string key, bool decorations)
        {
            if (Cache.TryGetValue(key, out Texture2D found)) return found;
            PixelCanvas c = new PixelCanvas(1280, 720, Hex("#080f14"));
            // The shallow plinth and diagonal 80-unit stonework are the original fixed arena.
            Vector2 top = WorldToView(new Vector2(0, 0));
            Vector2 right = WorldToView(new Vector2(1440, 0));
            Vector2 bottom = WorldToView(new Vector2(1440, 1440));
            Vector2 left = WorldToView(new Vector2(0, 1440));
            c.Polygon(new[] { right, bottom, bottom + Vector2.up * 22, right + Vector2.up * 22 }, Hex("#14262b"));
            c.Polygon(new[] { bottom, left, left + Vector2.up * 22, bottom + Vector2.up * 22 }, Hex("#0d1b20"));
            // The source paints a continuous floor before laying in its subtly inset
            // checker stones.  Keeping this underlay prevents the narrow joints from
            // exposing the near-black page backdrop at screen resolution.
            c.Polygon(new[] { top, right, bottom, left }, Hex("#1b302f"));
            for (int x = 0; x < 1440; x += 80)
            for (int y = 0; y < 1440; y += 80)
            {
                Color32 tile = ((x / 80 + y / 80) & 1) == 0 ? Hex("#203837") : Hex("#1b302f");
                c.Polygon(new[]
                {
                    WorldToView(new Vector2(x + 2, y + 2)),
                    WorldToView(new Vector2(Mathf.Min(x + 78, 1438), y + 2)),
                    WorldToView(new Vector2(Mathf.Min(x + 78, 1438), Mathf.Min(y + 78, 1438))),
                    WorldToView(new Vector2(x + 2, Mathf.Min(y + 78, 1438)))
                }, tile);
            }
            c.Polyline(new[] { top, right, bottom, left, top }, Hex("#81b39a66"), 2);
            if (decorations)
            {
                Vector2 center = WorldToView(new Vector2(720, 720));
                c.Ellipse(center, 77, 31, Hex("#b3f78328"), 2);
                c.Ellipse(center, 141, 56, Hex("#b3f78320"), 2);
                c.Ellipse(center, 14, 6, Hex("#c2f38b45"), 2);
                foreach (Vector2 corner in new[] { top, right, bottom, left })
                {
                    c.Ellipse(corner + Vector2.up * 2, 11, 5, Hex("#00000065"), 1);
                    c.Line(corner, corner + Vector2.up * -17, Hex("#3f6760"), 5);
                    c.Disc(corner + Vector2.up * -19, 3, Hex("#c5f7b2"));
                }
            }
            return Cache[key] = c.Texture("Survival Legend Arena " + key);
        }

        public static Texture2D ArenaRitual()
        {
            const string key = "arena-ritual";
            if (Cache.TryGetValue(key, out var found)) return found;
            var c = new PixelCanvas(300, 120, Clear);
            var center = new Vector2(150, 60);
            c.Ellipse(center, 77, 31, Hex("#b3f78328"), 2);
            c.Ellipse(center, 141, 56, Hex("#b3f78320"), 2);
            c.Ellipse(center, 14, 6, Hex("#c2f38b45"), 2);
            return Cache[key] = c.Texture("Arena ritual rings");
        }

        public static Texture2D ArenaCornerLamp()
        {
            const string key = "arena-corner-lamp";
            if (Cache.TryGetValue(key, out var found)) return found;
            var c = new PixelCanvas(32, 48, Clear);
            var corner = new Vector2(16, 24);
            c.Ellipse(corner + Vector2.up * 2, 11, 5, Hex("#00000065"), 1);
            c.Line(corner, corner + Vector2.up * -17, Hex("#3f6760"), 5);
            c.Disc(corner + Vector2.up * -19, 3, Hex("#c5f7b2"));
            return Cache[key] = c.Texture("Arena corner lamp");
        }

        public static Texture2D Player(string hero, float facing, bool moving, bool attacking)
        {
            int direction = Direction(facing);
            int step = moving ? Mathf.FloorToInt(Time.time * 9f) & 1 : 0;
            string key = $"player:{hero}:{direction}:{step}:{attacking}";
            if (Cache.TryGetValue(key, out Texture2D found)) return found;
            return Cache[key] = BakePlayer(hero, direction, step, attacking);
        }

        /// <summary>
        /// Loads the committed SD atlas baked by Tools/Art/bake_sd_atlases.py. Rows are
        /// source-world directions (0..7); columns are idle, two walk, then three attack frames.
        /// </summary>
        public static Texture2D PlayerAtlas(string hero)
        {
            string key = "atlas:" + hero;
            if (Cache.TryGetValue(key, out Texture2D found)) return found;
            Texture2D atlas = Resources.Load<Texture2D>("Art/Characters/" + hero + "-sd-atlas");
            if (atlas != null) atlas.filterMode = FilterMode.Point;
            return Cache[key] = atlas;
        }

        public static Rect PlayerFrameUv(float facing, bool moving, bool attacking, float attackProgress)
        {
            int direction = Direction(facing);
            int column;
            if (attacking) column = 3 + Mathf.Clamp(Mathf.FloorToInt(Mathf.Clamp01(attackProgress) * 3f), 0, 2);
            else if (moving) column = 1 + (Mathf.FloorToInt(Time.time * 9f) & 1);
            else column = 0;
            float width = 1f / PlayerAtlasColumns, height = 1f / PlayerAtlasDirections;
            // PNG rows are top-down while GUI texture coordinates start at the lower-left.
            return new Rect(column * width, 1f - (direction + 1) * height, width, height);
        }

        public static Texture2D Enemy(string kind, float facing, bool moving, bool aiming)
        {
            int direction = Direction(facing);
            int step = moving ? Mathf.FloorToInt(Time.time * 9f) & 1 : 0;
            string key = $"enemy:{kind}:{direction}:{step}:{aiming}";
            if (Cache.TryGetValue(key, out Texture2D found)) return found;
            return Cache[key] = BakeEnemy(kind, direction, step, aiming);
        }

        /// <summary>Deterministic frame access for editor atlas export; does not sample Time.time.</summary>
        public static Texture2D EnemyFrame(string kind, int direction, int step, bool aiming)
            => BakeEnemy(kind, direction & 7, step & 1, aiming);

        public static Texture2D Ring(Color color, int size = 128, int thickness = 3)
        {
            Color32 cc = color;
            string key = $"ring:{cc.r}:{cc.g}:{cc.b}:{cc.a}:{size}:{thickness}";
            if (Cache.TryGetValue(key, out Texture2D found)) return found;
            PixelCanvas c = new PixelCanvas(size, size, Clear);
            c.Ellipse(new Vector2(size * .5f, size * .5f), size * .5f - 2, size * .5f - 2, cc, thickness);
            return Cache[key] = c.Texture("Ring");
        }

        public static Texture2D Disc(Color color, int size = 128)
        {
            Color32 cc = color;
            string key = $"disc:{cc.r}:{cc.g}:{cc.b}:{cc.a}:{size}";
            if (Cache.TryGetValue(key, out Texture2D found)) return found;
            PixelCanvas c = new PixelCanvas(size, size, Clear);
            c.Disc(new Vector2(size * .5f, size * .5f), size * .5f - 2, cc);
            return Cache[key] = c.Texture("Disc");
        }

        public static Texture2D Orb(float ratio, bool vitality)
        {
            int level = Mathf.Clamp(Mathf.RoundToInt(ratio * 20f), 0, 20);
            string key = $"orb:{vitality}:{level}";
            if (Cache.TryGetValue(key, out Texture2D found)) return found;
            const int size = 128;
            PixelCanvas c = new PixelCanvas(size, size, Clear);
            Color32 deep = vitality ? Health : Power;
            Color32 light = vitality ? HealthLight : PowerLight;
            Vector2 center = new Vector2(64, 64);
            float fillY = size - 8 - (size - 16) * level / 20f;
            for (int y = 4; y < size - 4; y++)
            for (int x = 4; x < size - 4; x++)
            {
                float dx = x - center.x, dy = y - center.y;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                if (d > 57f) continue;
                Color32 col = Hex("#11191a");
                if (y >= fillY)
                {
                    float t = Mathf.Clamp01((y - fillY) / 80f + d / 150f);
                    col = Lerp(light, deep, t);
                }
                if (d < 54f && x < 58 && y < 48)
                    col = Lerp(col, new Color32(255, 255, 255, 255), Mathf.Clamp01((54f - d) / 100f));
                c.Set(x, y, col);
            }
            c.Ellipse(center, 61, 61, Metal, 5);
            c.Ellipse(center, 56, 56, Edge, 1);
            return Cache[key] = c.Texture(vitality ? "Vitality Orb" : "Ultimate Orb");
        }

        private static Texture2D BakePlayer(string hero, int direction, int step, bool attacking)
        {
            PixelCanvas c = new PixelCanvas(72, 92, Clear);
            int ox = 36, oy = 78, px = 2;
            Palette p = hero == "swordsman"
                ? new Palette("#8b9dac", "#e3ba71", "#283440", "#dcb492", "#483423", "#9c3540", "#51372a", "#c8f0ff")
                : hero == "mage"
                    ? new Palette("#635391", "#c5b3f4", "#201d3b", "#c4bdd8", "#cfcced", "#383060", "#77648e", "#80eaff")
                    : new Palette("#376856", "#e8cd83", "#193d36", "#f1c8ac", "#f0ce68", "#588876", "#664830", "#65dcbb");
            float angle = direction * Mathf.PI / 4f;
            float wx = Mathf.Cos(angle), wy = Mathf.Sin(angle);
            bool front = (wx + wy) * .155f >= 0f;
            int side = (wx - wy) < -.12f ? -1 : (wx - wy) > .12f ? 1 : 0;
            int foot = step == 0 ? -1 : 1;
            Action<int, int, int, int, Color32> rect = (x, y, w, h, color) => c.Rect(ox + x * px, oy + y * px, w * px, h * px, color);
            Action<int, int, int, int, Color32> box = (x, y, w, h, color) =>
            {
                rect(x, y, w, h, Outline);
                rect(x + 1, y + 1, w - 2, h - 2, color);
            };
            Action cape = () =>
            {
                if (hero == "archer") return;
                box(-6 - side, -14, 12, 13, p.Cloth);
                rect(-4 - side, -12, 2, 10, p.Primary);
                rect(2 - side, -10, 2, 9, p.Shadow);
            };
            Action hair = () =>
            {
                if (hero != "archer") return;
                box(-7, -21, 14, 15, p.Hair);
                rect(-5, -17, 2, 10, p.Trim);
                rect(3, -17, 2, 11, p.Leather);
            };
            Action quiver = () =>
            {
                if (hero != "archer") return;
                box(-9, -17, 4, 10, p.Leather);
                rect(-8, -22, 1, 9, p.Trim); rect(-6, -21, 1, 7, p.Trim);
                rect(-9, -23, 2, 3, Hex("#e5e0cf")); rect(-7, -22, 2, 3, Hex("#e5e0cf"));
            };
            if (front) { cape(); hair(); quiver(); }
            box(-5, -4 - foot, 5, 4, p.Leather); box(1, -4 + foot, 5, 4, p.Leather);
            box(-5, -14, 11, 11, hero == "mage" ? p.Cloth : p.Primary);
            rect(-3, -12, 3, 6, p.Primary); rect(2, -12, 2, 7, p.Shadow);
            box(-8, -13, 4, 7, hero == "swordsman" ? p.Primary : p.Cloth);
            box(5, -13, 4, 7, hero == "swordsman" ? p.Primary : p.Cloth);
            rect(-7, -7, 2, 2, hero == "swordsman" ? p.Trim : p.Skin);
            rect(6, -7, 2, 2, hero == "swordsman" ? p.Trim : p.Skin);
            if (front)
            {
                rect(-4, -6, 9, 2, p.Leather); rect(0, -6, 2, 2, p.Trim);
                if (hero == "swordsman") { rect(-4, -13, 9, 2, p.Trim); rect(-2, -10, 5, 2, Hex("#cad6d8")); }
                if (hero == "archer") for (int i = 0; i < 6; i++) rect(-3 + i, -13 + i, 2, 2, p.Leather);
                if (hero == "mage") { rect(-4, -5, 9, 3, p.Cloth); rect(0, -12, 1, 8, p.Trim); rect(-1, -11, 3, 2, p.Eye); }
            }
            if (!front) { cape(); hair(); quiver(); }
            rect(-7, -30, 14, 1, Outline); rect(-8, -29, 16, 15, Outline); rect(-6, -14, 12, 1, Outline);
            Color32 head = hero == "archer" ? p.Hair : hero == "mage" ? p.Cloth : p.Primary;
            rect(-7, -28, 14, 13, head); rect(-5, -29, 10, 2, hero == "archer" ? p.Hair : p.Trim);
            int face = side * 2;
            if (front)
            {
                rect(-5 + face, -25, side == 0 ? 10 : 9, 9, hero == "mage" ? p.Shadow : p.Skin);
                rect(-3 + face, -22, 1, 3, hero == "mage" ? p.Eye : Outline);
                rect(2 + face, -22, 1, 3, hero == "mage" ? p.Eye : Outline);
            }
            else { rect(-5, -25, 2, 8, hero == "archer" ? p.Trim : p.Primary); rect(4, -24, 2, 9, p.Shadow); }
            if (hero == "swordsman")
            {
                rect(-7, -28, 14, 4, p.Primary); rect(-5, -29, 10, 2, Hex("#ccd6d9"));
                if (front) { rect(-6 + face, -24, 11, 5, Outline); rect(-4 + face, -22, 7, 1, p.Eye); rect(-1 + face, -25, 2, 11, p.Trim); }
                rect(-1, -32, 2, 3, p.Cloth);
            }
            else if (hero == "archer")
            {
                rect(-7, -28, 14, 4, p.Hair); rect(-7, -24, 3, 5, p.Hair); rect(5, -24, 2, 8, p.Hair);
                rect(-5, -28, 9, 1, p.Trim);
                rect(-11, -23, 4, 2, Outline); rect(-10, -22, 4, 2, p.Skin);
                rect(8, -23, 3, 2, Outline); rect(7, -22, 3, 2, p.Skin);
            }
            else
            {
                rect(-3, -32, 6, 2, Outline); rect(-2, -31, 4, 2, p.Primary);
                rect(-7, -27, 2, 11, p.Primary); rect(5, -27, 2, 11, p.Shadow);
            }
            DrawWeapon(c, hero, ox, oy, side == 0 ? 1 : side, attacking, p);
            return c.Texture("SD " + hero);
        }

        private static void DrawWeapon(PixelCanvas c, string hero, int ox, int oy, int side, bool attacking, Palette p)
        {
            int handX = ox + side * (attacking ? 17 : 13), handY = oy - (attacking ? 34 : 20);
            c.Line(new Vector2(ox + side * 9, oy - 22), new Vector2(handX, handY), p.Primary, 5);
            c.Disc(new Vector2(handX, handY), 3, p.Skin);
            if (hero == "archer")
            {
                int x = handX + side * 7;
                c.Line(new Vector2(x, handY - 18), new Vector2(x + side * 7, handY), p.Trim, 3);
                c.Line(new Vector2(x + side * 7, handY), new Vector2(x, handY + 18), p.Trim, 3);
                c.Line(new Vector2(x, handY - 18), new Vector2(x, handY + 18), Hex("#dfe7d1"), 1);
                c.Line(new Vector2(handX - side * 4, handY), new Vector2(handX + side * (attacking ? 27 : 18), handY - (attacking ? 2 : 0)), Hex("#e5e0cf"), 2);
            }
            else if (hero == "swordsman")
            {
                Vector2 a = new Vector2(handX, handY), b = a + new Vector2(side * 26, -24);
                c.Line(a, b, Outline, 8); c.Line(a, b, Hex("#cad6d8"), 5);
                c.Line(a + Vector2.left * side * 7, a + Vector2.right * side * 7, p.Trim, 4);
            }
            else
            {
                Vector2 tip = new Vector2(handX + side * 8, handY - 28);
                c.Line(new Vector2(handX, handY + 8), tip, p.Leather, 4);
                c.Disc(tip, 6, p.Trim); c.Disc(tip, 3, p.Eye);
            }
        }

        private static Texture2D BakeEnemy(string kind, int direction, int step, bool aiming)
        {
            bool elite = kind == "elite", ranged = kind == "shooter", caster = kind == "caster";
            PixelCanvas c = new PixelCanvas(elite ? 104 : 72, elite ? 122 : 92, Clear);
            int scale = elite ? 3 : 2, ox = c.Width / 2, oy = c.Height - 12;
            Palette p = kind == "chaser" ? new Palette("#b56e73", "#e19a8d", "#633c51", "#b56e73", "#e19a8d", "#735048", "#633c51", "#ffe4ab")
                : ranged ? new Palette("#e0d1a4", "#f4e8c6", "#968565", "#e0d1a4", "#f4e8c6", "#876449", "#968565", "#252633")
                : caster ? new Palette("#6a588d", "#9c81b5", "#312d4f", "#6a588d", "#9c81b5", "#514570", "#312d4f", "#c5f1ff")
                : new Palette("#94717e", "#cab79e", "#342d43", "#94717e", "#cab79e", "#743b50", "#342d43", "#ffe598");
            float angle = direction * Mathf.PI / 4f;
            float wx = Mathf.Cos(angle), wy = Mathf.Sin(angle);
            bool front = (wx + wy) >= 0; int side = wx - wy < 0 ? -1 : 1; int foot = step == 0 ? -1 : 1;
            Action<int, int, int, int, Color32> rect = (x, y, w, h, col) => c.Rect(ox + x * scale, oy + y * scale, w * scale, h * scale, col);
            Action<int, int, int, int, Color32> box = (x, y, w, h, col) => { rect(x, y, w, h, Outline); rect(x + 1, y + 1, w - 2, h - 2, col); };
            box(-5, -4 - foot, 4, 4, p.Shadow); box(1, -4 + foot, 4, 4, p.Shadow);
            box(-6, -13, 12, 10, p.Cloth); rect(-4, -11, 3, 6, p.Skin); rect(1, -6, 4, 2, p.Shadow);
            box(-9, -12, 4, 7, p.Shadow); box(5, -12, 4, 7, p.Shadow);
            if (elite) { rect(-6, -12, 12, 2, p.Trim); rect(-2, -10, 4, 4, p.Trim); }
            rect(-7, -29, 14, 1, Outline); rect(-8, -28, 16, 14, Outline); rect(-6, -14, 12, 1, Outline);
            rect(-7, -27, 14, 12, caster ? p.Cloth : p.Skin); rect(-5, -28, 10, 3, p.Trim); rect(4, -25, 3, 10, p.Shadow);
            if (front)
            {
                if (caster) { rect(-5, -24, 10, 8, p.Shadow); rect(-3, -21, 2, 1, p.Eye); rect(2, -21, 2, 1, p.Eye); }
                else { rect(-5, -24, 4, 4, p.Shadow); rect(1, -24, 4, 4, p.Shadow); rect(-4, -22, 2, 1, p.Eye); rect(2, -22, 2, 1, p.Eye); rect(-3, -17, 6, 1, p.Shadow); }
                if (ranged) { rect(-1, -20, 2, 2, p.Shadow); rect(-4, -16, 1, 2, p.Shadow); rect(-1, -16, 1, 2, p.Shadow); rect(2, -16, 1, 2, p.Shadow); }
            }
            else rect(-4, -25, 3, 9, p.Shadow);
            if (!ranged && !caster)
            {
                box(-11, -32, 4, 8, p.Trim); rect(-10, -35, 2, 5, p.Trim); box(7, -32, 4, 8, p.Trim); rect(8, -35, 2, 5, p.Trim);
                if (elite) { Color32 crown = Hex("#d8ad65"); rect(-5, -31, 10, 2, crown); rect(-4, -34, 2, 4, crown); rect(-1, -35, 2, 5, crown); rect(2, -34, 2, 4, crown); }
            }
            if (caster) { rect(-4, -31, 8, 3, p.Shadow); rect(-2, -33, 4, 3, p.Cloth); }
            int wxp = side * 10, wyp = -8 + (aiming ? -3 : 0);
            if (ranged)
            {
                box(wxp - 4, wyp - 9, 8, 3, p.Shadow); rect(wxp - 6, wyp - 8, 2, 7, p.Trim); rect(wxp + 4, wyp - 8, 2, 7, p.Trim); rect(wxp - 1, wyp - 7, 2, 9, p.Cloth);
            }
            else if (caster) { box(wxp, wyp - 19, 3, 24, p.Shadow); box(wxp - 2, wyp - 24, 7, 7, p.Trim); rect(wxp, wyp - 22, 3, 3, p.Eye); }
            else { box(wxp - 1, wyp - 10, 3, 16, p.Cloth); box(wxp - 3, wyp - (elite ? 24 : 18), 7, elite ? 16 : 12, p.Shadow); rect(wxp - 2, wyp - (elite ? 23 : 17), 3, elite ? 14 : 10, p.Trim); }
            return c.Texture("SD " + kind);
        }

        private static int Direction(float radians)
        {
            int d = Mathf.RoundToInt(Mathf.Repeat(World.WorldProjection.AtlasFacing(radians), Mathf.PI * 2f) / (Mathf.PI / 4f));
            return d & 7;
        }

        public static Color32 Hex(string value)
        {
            if (ColorUtility.TryParseHtmlString(value, out Color parsed)) return parsed;
            return Color.magenta;
        }

        private static Color32 Lerp(Color32 a, Color32 b, float t) => (Color32)Color.Lerp(a, b, t);

        private readonly struct Palette
        {
            public readonly Color32 Primary, Trim, Shadow, Skin, Hair, Cloth, Leather, Eye;
            public Palette(string primary, string trim, string shadow, string skin, string hair, string cloth, string leather, string eye)
            {
                Primary = Hex(primary); Trim = Hex(trim); Shadow = Hex(shadow); Skin = Hex(skin);
                Hair = Hex(hair); Cloth = Hex(cloth); Leather = Hex(leather); Eye = Hex(eye);
            }
        }

        private sealed class PixelCanvas
        {
            private readonly Color32[] _pixels;
            public int Width { get; }
            public int Height { get; }
            public PixelCanvas(int width, int height, Color32 fill)
            {
                Width = width; Height = height; _pixels = new Color32[width * height];
                for (int i = 0; i < _pixels.Length; i++) _pixels[i] = fill;
            }
            public void Set(int x, int y, Color32 color)
            {
                if (x < 0 || x >= Width || y < 0 || y >= Height) return;
                _pixels[(Height - 1 - y) * Width + x] = color;
            }
            public void Rect(int x, int y, int width, int height, Color32 color)
            {
                for (int py = y; py < y + height; py++) for (int px = x; px < x + width; px++) Set(px, py, color);
            }
            public void Disc(Vector2 center, float radius, Color32 color)
            {
                int minX = Mathf.FloorToInt(center.x - radius), maxX = Mathf.CeilToInt(center.x + radius);
                int minY = Mathf.FloorToInt(center.y - radius), maxY = Mathf.CeilToInt(center.y + radius);
                float rr = radius * radius;
                for (int y = minY; y <= maxY; y++) for (int x = minX; x <= maxX; x++)
                    if ((new Vector2(x, y) - center).sqrMagnitude <= rr) Set(x, y, color);
            }
            public void Line(Vector2 a, Vector2 b, Color32 color, int width = 1)
            {
                int steps = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(a, b) * 1.5f));
                for (int i = 0; i <= steps; i++) Disc(Vector2.Lerp(a, b, i / (float)steps), Mathf.Max(.5f, width * .5f), color);
            }
            public void Polyline(Vector2[] points, Color32 color, int width)
            {
                for (int i = 1; i < points.Length; i++) Line(points[i - 1], points[i], color, width);
            }
            public void Ellipse(Vector2 center, float rx, float ry, Color32 color, int thickness)
            {
                const int segments = 180;
                Vector2 previous = center + new Vector2(rx, 0);
                for (int i = 1; i <= segments; i++)
                {
                    float a = i * Mathf.PI * 2f / segments;
                    Vector2 next = center + new Vector2(Mathf.Cos(a) * rx, Mathf.Sin(a) * ry);
                    Line(previous, next, color, thickness); previous = next;
                }
            }
            public void Polygon(Vector2[] vertices, Color32 color)
            {
                int minY = Height - 1, maxY = 0;
                foreach (Vector2 v in vertices) { minY = Mathf.Min(minY, Mathf.FloorToInt(v.y)); maxY = Mathf.Max(maxY, Mathf.CeilToInt(v.y)); }
                List<float> intersections = new List<float>(vertices.Length);
                for (int y = minY; y <= maxY; y++)
                {
                    intersections.Clear(); float scan = y + .5f;
                    for (int i = 0, j = vertices.Length - 1; i < vertices.Length; j = i++)
                    {
                        Vector2 a = vertices[j], b = vertices[i];
                        if ((a.y > scan) == (b.y > scan)) continue;
                        intersections.Add(a.x + (scan - a.y) * (b.x - a.x) / (b.y - a.y));
                    }
                    intersections.Sort();
                    for (int i = 0; i + 1 < intersections.Count; i += 2)
                        Rect(Mathf.FloorToInt(intersections[i]), y, Mathf.CeilToInt(intersections[i + 1] - intersections[i]), 1, color);
                }
            }
            public Texture2D Texture(string name)
            {
                Texture2D texture = new Texture2D(Width, Height, TextureFormat.RGBA32, false)
                {
                    name = name, filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave
                };
                texture.SetPixels32(_pixels); texture.Apply(false, true); return texture;
            }
        }
    }
}
