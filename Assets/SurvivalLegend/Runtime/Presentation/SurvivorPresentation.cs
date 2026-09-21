using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using SurvivalLegend;
using SurvivalLegend.Data;
using Catalog = SurvivalLegend.Data.SurvivalLegendCatalog;

namespace SurvivalLegend.Presentation
{
    /// <summary>
    /// Complete first-slice presentation.  It intentionally uses IMGUI plus baked textures:
    /// the same code path works in the Editor, desktop player and WebGL without scene wiring.
    /// </summary>
    [DefaultExecutionOrder(50)]
    public sealed class SurvivorPresentation : MonoBehaviour
    {
        private const float W = SurvivalLegendArt.ViewWidth;
        private const float H = SurvivalLegendArt.ViewHeight;
        private readonly Dictionary<string, Texture2D> _icons = new Dictionary<string, Texture2D>();
        private SurvivorGame _game;
        private Font _font, _boldFont;
        private GUIStyle _label, _small, _tiny, _heading, _title, _button, _panelTitle, _center;
        private bool _stylesReady, _showLoadout;
        private Vector2 _resultScroll;
        private int _specialD = 1, _specialF, _selectedCharacter = 1;
        private readonly string[] _characterIds = { "swordsman", "archer", "mage" };
        private readonly string[] _characterNames = { "검사", "궁수", "마법사" };
        private readonly string[] _characterSubtitles = { "대검으로 버티는 전선", "거리를 지배하는 화살", "번개와 원소의 연쇄" };
        private readonly string[] _characterTraits = { "체력 높음 · 이동 느림 · 공격 보통", "체력 보통 · 이동 빠름 · 공격 빠름", "체력 낮음 · 이동 보통 · 공격 느림" };
        private readonly string[][] _characterSkills =
        {
            new[] { "강화 일격", "반격의 방벽", "회전 공격", "심판의 대검" },
            new[] { "집중 사격", "다발 화살", "관통 화살", "화살비" },
            new[] { "폭발 화염구", "서리 결계", "비전 도약", "비전 과부하" }
        };
        private readonly string[] _specialIds = { "heal", "haste", "barrier", "blackhole" };
        private readonly string[] _specialNames = { "회복", "가속", "보호막", "블랙홀" };
        private readonly string[] _specialDescriptions =
        {
            "잃은 체력의 20% 즉시 회복 · 90초", "5초간 이동 속도 +50% · 60초",
            "최대 체력 25% 보호막 · 5초 · 45초", "지정 위치로 적을 3초간 흡입 · 90초"
        };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsurePresentation()
        {
            if (FindAnyObjectByType<SurvivalLegend.UI.SurvivorCanvasUI>() != null) return;
            if (FindAnyObjectByType<SurvivorPresentation>() != null) return;
            SurvivorGame game = SurvivorGame.Instance ?? FindAnyObjectByType<SurvivorGame>();
            if (game != null) game.gameObject.AddComponent<SurvivorPresentation>();
        }

        private void Awake()
        {
            _game = SurvivorGame.Instance ?? FindAnyObjectByType<SurvivorGame>();
            _font = Resources.Load<Font>("Art/Fonts/NotoSansKR-Regular");
            _boldFont = Resources.Load<Font>("Art/Fonts/NotoSansKR-Bold");
            LoadIcons();
        }

        private void LoadIcons()
        {
            string[] ids = { "armor", "attack", "barrier", "blackhole", "bow-e", "bow-q", "bow-r", "bow-w", "chain", "cooldown", "critical", "elite", "haste", "heal", "magic-e", "magic-q", "magic-r", "magic-w", "mastery", "speed", "sword-e", "sword-q", "sword-r", "sword-w" };
            foreach (string id in ids)
            {
                Texture2D icon = Resources.Load<Texture2D>("Art/Icons/" + id);
                if (icon == null) continue;
                icon.filterMode = FilterMode.Point;
                _icons[id] = icon;
            }
        }

        private void Update()
        {
            if (_game == null) _game = SurvivorGame.Instance ?? FindAnyObjectByType<SurvivorGame>();
            if (_game == null) return;
            Vector2 view = MouseToView();
            _game.CursorWorld = SurvivalLegendArt.ViewToWorld(view);
            _game.PointerOverUi = IsPointerOverUi(view, _game.State.Phase);
        }

        private Vector2 MouseToView()
        {
            Vector2 mouse = Mouse.current != null ? Mouse.current.position.ReadValue() : new Vector2(Screen.width * .5f, Screen.height * .5f);
            float scale = Mathf.Min(Screen.width / W, Screen.height / H);
            float left = (Screen.width - W * scale) * .5f, top = (Screen.height - H * scale) * .5f;
            return new Vector2((mouse.x - left) / scale, (Screen.height - mouse.y - top) / scale);
        }

        private static bool IsPointerOverUi(Vector2 p, RunPhase phase)
        {
            if (phase == RunPhase.Title || phase == RunPhase.Paused || phase == RunPhase.Augment || phase == RunPhase.AugmentEnter || phase == RunPhase.Results) return true;
            return p.y < 66 || p.y > 538;
        }

        private void OnGUI()
        {
            if (_game == null) return;
            EnsureStyles();
            float scale = Mathf.Min(Screen.width / W, Screen.height / H);
            Vector2 offset = new Vector2((Screen.width - W * scale) * .5f, (Screen.height - H * scale) * .5f);
            Matrix4x4 before = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(offset, Quaternion.identity, new Vector3(scale, scale, 1));
            Color oldColor = GUI.color;
            GUI.color = Color.white;
            DrawBackdrop();
            if (_game.State.Phase == RunPhase.Title) DrawTitle();
            else DrawRun(_game.State);
            GUI.color = oldColor;
            GUI.matrix = before;
        }

        private void EnsureStyles()
        {
            if (_stylesReady) return;
            _stylesReady = true;
            _label = Style(14, SurvivalLegendArt.Text, FontStyle.Normal, TextAnchor.MiddleLeft);
            _small = Style(12, SurvivalLegendArt.Muted, FontStyle.Normal, TextAnchor.MiddleLeft);
            _tiny = Style(10, SurvivalLegendArt.Muted, FontStyle.Bold, TextAnchor.MiddleLeft);
            _heading = Style(28, SurvivalLegendArt.Text, FontStyle.Bold, TextAnchor.MiddleCenter);
            _title = Style(70, SurvivalLegendArt.Text, FontStyle.Normal, TextAnchor.MiddleCenter);
            _panelTitle = Style(18, SurvivalLegendArt.Text, FontStyle.Bold, TextAnchor.MiddleLeft);
            _center = Style(13, SurvivalLegendArt.Text, FontStyle.Bold, TextAnchor.MiddleCenter);
            _button = new GUIStyle(GUI.skin.button)
            {
                font = _boldFont != null ? _boldFont : _font, fontSize = 13, fontStyle = FontStyle.Normal, alignment = TextAnchor.MiddleCenter,
                normal = { textColor = SurvivalLegendArt.Text }, hover = { textColor = SurvivalLegendArt.PowerLight },
                active = { textColor = Color.white }, padding = new RectOffset(12, 12, 8, 8)
            };
        }

        private GUIStyle Style(int size, Color color, FontStyle fontStyle, TextAnchor anchor)
        {
            Font selectedFont = fontStyle == FontStyle.Bold && _boldFont != null ? _boldFont : _font;
            return new GUIStyle(GUI.skin.label) { font = selectedFont, fontSize = size, fontStyle = FontStyle.Normal, alignment = anchor, normal = { textColor = color }, richText = true, clipping = TextClipping.Clip };
        }

        private static void DrawBackdrop()
        {
            Fill(new Rect(0, 0, W, H), SurvivalLegendArt.Page);
            GUI.DrawTexture(new Rect(0, 0, W, H), SurvivalLegendArt.Arena(), ScaleMode.StretchToFill, true);
            Fill(new Rect(0, 0, W, 720), new Color(0.01f, .035f, .04f, .12f));
        }

        private void DrawTitle()
        {
            Fill(new Rect(0, 0, W, H), new Color(.01f, .03f, .035f, .56f));
            Stroke(new Rect(14, 14, W - 28, H - 28), SurvivalLegendArt.Metal, 1);
            Stroke(new Rect(19, 19, W - 38, H - 38), new Color32(48, 78, 76, 255), 1);
            GUI.Label(new Rect(55, 28, 250, 24), "‹  ARCADE", Style(10, SurvivalLegendArt.Edge, FontStyle.Normal, TextAnchor.MiddleLeft));
            GUI.Label(new Rect(W - 360, 28, 305, 24), "CONTROL. EVOLVE. SURVIVE.", Style(9, SurvivalLegendArt.Muted, FontStyle.Normal, TextAnchor.MiddleRight));
            if (!_showLoadout)
            {
                GUI.Label(new Rect(0, 94, W, 24), "T H E   A R E N A   A W A I T S", Style(10, SurvivalLegendArt.Edge, FontStyle.Normal, TextAnchor.MiddleCenter));
                GUI.Label(new Rect(0, 125, W, 86), "SURVIVAL", _title);
                GUI.Label(new Rect(0, 195, W, 76), "L E G E N D", Style(53, SurvivalLegendArt.Edge, FontStyle.Normal, TextAnchor.MiddleCenter));
                DrawRule(500, 283, 280);
                GUI.Label(new Rect(0, 291, W, 26), "끝까지 살아남아라.", Style(16, SurvivalLegendArt.Muted, FontStyle.Normal, TextAnchor.MiddleCenter));
                DrawTitleHeroes();
                if (FrameButton(new Rect(510, 567, 260, 58), "전장으로    ↗", true)) _showLoadout = true;
                GUI.Label(new Rect(0, 634, W, 20), "마우스 + 키보드", Style(10, SurvivalLegendArt.Muted, FontStyle.Normal, TextAnchor.MiddleCenter));
            }
            else DrawLoadout();
            GUI.Label(new Rect(54, H - 42, 300, 20), "SINGLE PLAYER SURVIVAL", Style(9, SurvivalLegendArt.Muted, FontStyle.Normal, TextAnchor.MiddleLeft));
            GUI.Label(new Rect(W - 330, H - 42, 276, 20), "THREE HEROES · ONE ARENA", Style(9, SurvivalLegendArt.Muted, FontStyle.Normal, TextAnchor.MiddleRight));
        }

        private void DrawTitleHeroes()
        {
            DrawShadow(new Vector2(435, 492), 68, 17, new Color(.96f, .8f, .5f, .16f));
            DrawShadow(new Vector2(640, 510), 74, 19, new Color(.7f, .97f, .51f, .18f));
            DrawShadow(new Vector2(845, 492), 68, 17, new Color(.77f, .63f, 1f, .16f));
            DrawPlayerActor("swordsman", .2f, false, false, 0, new Vector2(435, 500), 1.3f, Color.white);
            DrawPlayerActor("archer", Mathf.PI / 4f, false, false, 0, new Vector2(640, 518), 1.4f, Color.white);
            DrawPlayerActor("mage", 1.35f, false, false, 0, new Vector2(845, 500), 1.3f, Color.white);
        }

        private void DrawLoadout()
        {
            GUI.Label(new Rect(0, 67, W, 18), "당신의 생존 방식은?", Style(10, SurvivalLegendArt.Accent, FontStyle.Bold, TextAnchor.MiddleCenter));
            GUI.Label(new Rect(0, 88, W, 38), "캐릭터를 선택하세요", _heading);
            GUI.Label(new Rect(0, 119, W, 20), "적 우클릭 공격 · A 후 좌클릭 공격 이동 · 모든 스킬을 보유하고 시작", Style(10, SurvivalLegendArt.Muted, FontStyle.Normal, TextAnchor.MiddleCenter));
            for (int i = 0; i < _characterIds.Length; i++)
            {
                Rect card = new Rect(209 + i * 288, 145, 270, 180);
                bool selected = i == _selectedCharacter;
                Panel(card, selected);
                Color heroColor = CharacterColor(_characterIds[i]);
                DrawShadow(new Vector2(card.x + 55, card.y + 126), 34, 8, new Color(heroColor.r, heroColor.g, heroColor.b, selected ? .25f : .12f));
                DrawPlayerActor(_characterIds[i], Mathf.PI / 4f, false, false, 0, new Vector2(card.x + 55, card.y + 129), .78f, selected ? Color.white : new Color(.72f, .76f, .76f));
                GUI.Label(new Rect(card.x + 105, card.y + 17, 145, 29), _characterNames[i], Style(21, heroColor, FontStyle.Bold, TextAnchor.MiddleLeft));
                GUI.Label(new Rect(card.x + 105, card.y + 48, 150, 20), _characterSubtitles[i], Style(11, SurvivalLegendArt.Text, FontStyle.Bold, TextAnchor.MiddleLeft));
                GUI.Label(new Rect(card.x + 105, card.y + 73, 150, 42), _characterTraits[i], Style(10, SurvivalLegendArt.Muted, FontStyle.Normal, TextAnchor.UpperLeft));
                GUI.Label(new Rect(card.x + 105, card.y + 127, 145, 23), selected ? "선택됨  ✓" : "선택", Style(10, selected ? heroColor : SurvivalLegendArt.Edge, FontStyle.Bold, TextAnchor.MiddleLeft));
                if (GUI.Button(card, GUIContent.none, GUIStyle.none)) _selectedCharacter = i;
            }
            Rect kit = new Rect(300, 338, 680, 44); Panel(kit, false);
            for (int i = 0; i < 4; i++)
            {
                string id = SkillId(_selectedCharacter, i); Texture2D icon = Icon(id);
                Rect cell = new Rect(kit.x + 8 + i * 167, kit.y + 5, 160, 34);
                GUI.Label(new Rect(cell.x, cell.y, 20, 34), "QWER"[i].ToString(), Style(11, SurvivalLegendArt.Edge, FontStyle.Bold, TextAnchor.MiddleCenter));
                if (icon != null) GUI.DrawTexture(new Rect(cell.x + 23, cell.y + 2, 30, 30), icon, ScaleMode.ScaleToFit, true);
                GUI.Label(new Rect(cell.x + 57, cell.y, 101, 34), _characterSkills[_selectedCharacter][i], Style(10, SurvivalLegendArt.Text, FontStyle.Bold, TextAnchor.MiddleLeft));
            }
            Rect specials = new Rect(330, 395, 620, 108);
            Panel(specials, false);
            GUI.Label(new Rect(specials.x + 17, specials.y + 10, 240, 20), "특수능력 · 서로 다른 능력 2개", Style(10, SurvivalLegendArt.Edge, FontStyle.Bold, TextAnchor.MiddleLeft));
            DrawSpecialPicker(new Rect(specials.x + 17, specials.y + 39, 285, 58), "D", ref _specialD, _specialF);
            DrawSpecialPicker(new Rect(specials.x + 318, specials.y + 39, 285, 58), "F", ref _specialF, _specialD);
            if (FrameButton(new Rect(410, 522, 460, 54), "생존 시작    ↗", true))
            {
                _game.StartRun((uint)DateTime.UtcNow.Ticks, _specialIds[_specialD], _specialIds[_specialF], _characterIds[_selectedCharacter]);
                _showLoadout = false;
            }
            if (GUI.Button(new Rect(565, 590, 150, 30), "‹ 타이틀로", _button)) _showLoadout = false;
        }

        private void DrawSpecialPicker(Rect rect, string key, ref int selected, int excluded)
        {
            Panel(rect, false);
            GUI.Label(new Rect(rect.x + 8, rect.y + 8, 25, 22), key, Style(13, SurvivalLegendArt.Edge, FontStyle.Bold, TextAnchor.MiddleCenter));
            Texture2D icon = Icon(_specialIds[selected]);
            if (icon != null) GUI.DrawTexture(new Rect(rect.x + 39, rect.y + 8, 36, 36), icon, ScaleMode.ScaleToFit, true);
            GUI.Label(new Rect(rect.x + 82, rect.y + 5, 120, 23), _specialNames[selected], _label);
            GUI.Label(new Rect(rect.x + 82, rect.y + 27, 180, 20), _specialDescriptions[selected], _tiny);
            if (GUI.Button(new Rect(rect.x + rect.width - 30, rect.y + 14, 22, 28), "›", _button))
            {
                do selected = (selected + 1) % _specialIds.Length; while (selected == excluded);
            }
        }

        private void DrawRun(RunState run)
        {
            DrawWorld(run);
            DrawTopBar(run);
            DrawXp(run);
            DrawCombatHud(run);
            if (run.NoticeTime > 0 && !string.IsNullOrEmpty(run.Notice))
            {
                Rect notice = new Rect(390, 486, 500, 30); Panel(notice, false);
                GUI.Label(notice, LocalizeNotice(run.Notice), Style(10, SurvivalLegendArt.Text, FontStyle.Normal, TextAnchor.MiddleCenter));
            }
            if (run.Phase == RunPhase.Paused) DrawPause();
            else if (run.Phase == RunPhase.AugmentEnter || run.Phase == RunPhase.Augment) DrawAugments(run);
            else if (run.Phase == RunPhase.EliteDeath) DrawEliteReward(run);
            else if (run.Phase == RunPhase.Dying) DrawDying(run);
            else if (run.Phase == RunPhase.Results) DrawResults(run);
        }

        private void DrawWorld(RunState run)
        {
            foreach (ZoneState zone in run.Zones) DrawZone(zone);
            List<EnemyState> enemies = new List<EnemyState>(run.Enemies);
            enemies.Sort((a, b) => SurvivalLegendArt.WorldToView(a.Position).y.CompareTo(SurvivalLegendArt.WorldToView(b.Position).y));
            Vector2 playerPoint = SurvivalLegendArt.WorldToView(run.Player.Position);
            bool playerDrawn = false;
            foreach (EnemyState enemy in enemies)
            {
                Vector2 point = SurvivalLegendArt.WorldToView(enemy.Position);
                if (!playerDrawn && point.y > playerPoint.y) { DrawPlayer(run, playerPoint); playerDrawn = true; }
                DrawEnemy(enemy, point);
            }
            if (!playerDrawn) DrawPlayer(run, playerPoint);
            foreach (ProjectileState projectile in run.Projectiles) DrawProjectile(projectile);
            foreach (EffectState effect in run.Effects) DrawEffect(effect);
            DrawAimPreview(run);
            if (run.Destination.HasValue)
            {
                Vector2 p = SurvivalLegendArt.WorldToView(run.Destination.Value);
                GUI.color = run.Order == "attack-move" ? new Color(1f, .72f, .42f, .8f) : new Color(.7f, .97f, .51f, .65f);
                GUI.DrawTexture(new Rect(p.x - 11, p.y - 5, 22, 10), SurvivalLegendArt.Ring(GUI.color), ScaleMode.StretchToFill, true);
                GUI.color = Color.white;
            }
        }

        private void DrawPlayer(RunState run, Vector2 point)
        {
            Color heroColor = CharacterColor(run.Character);
            DrawShadow(point, 11, 5, new Color(heroColor.r, heroColor.g, heroColor.b, .28f));
            bool shield = run.Player.Invulnerable > 0 || run.Player.Shield > 0 || run.Player.SkillShield > 0 || run.DodgeRemaining > 0;
            if (shield)
            {
                GUI.color = new Color(.84f, 1f, .71f, .8f);
                GUI.DrawTexture(new Rect(point.x - 15, point.y - 38, 30, 40), SurvivalLegendArt.Ring(GUI.color), ScaleMode.StretchToFill, true);
                GUI.color = Color.white;
            }
            Color tint = run.Phase == RunPhase.Dying ? new Color(1, 1, 1, Mathf.Clamp01(1f - run.PhaseElapsed / 1.2f)) : Color.white;
            if (run.Player.Motion == "blink") tint.a *= .55f + .45f * Mathf.Abs(Mathf.Sin(Time.time * 12));
            float progress = run.Player.AttackAnimation > 0 && run.Player.MotionDuration > 0
                ? 1f - run.Player.AttackAnimation / run.Player.MotionDuration : 0;
            bool spin = run.Zones.Exists(z => z.FollowCaster && z.Visual == "spin" && z.Life > 0);
            float facing = run.Player.AttackAnimation > 0 ? run.Player.MotionAngle : run.Player.Facing;
            if (spin) { facing += Time.time * 18f; progress = Mathf.Repeat(Time.time * 3f, 1f); }
            DrawPlayerActor(run.Character, facing, run.Player.Moving, run.Player.AttackAnimation > 0 || spin, progress, point, .55f, tint);
            bool empowered = run.Buffs.Exists(b => b.NextAttackCharges > 0);
            bool overloaded = run.Buffs.Exists(b => b.Id == "magic-r");
            if (empowered) DrawGroundRing(run.Player.Position, 42, new Color(1f, .88f, .42f, .75f));
            if (overloaded)
            {
                for (int i = 0; i < 3; i++)
                {
                    float a = Time.time * 3f + i * Mathf.PI * 2f / 3f;
                    Vector2 orb = point + new Vector2(Mathf.Cos(a) * 17, Mathf.Sin(a) * 7 - 25);
                    Fill(new Rect(orb.x - 2, orb.y - 2, 4, 4), new Color(.91f, .78f, 1f, .9f));
                }
            }
            DrawHealthBar(new Vector2(point.x, point.y - 43), 42, run.Player.Hp / Mathf.Max(1, run.Player.MaxHp), run.Player.Hp / Mathf.Max(1, run.Player.MaxHp) > .3f ? new Color32(179, 247, 131, 255) : SurvivalLegendArt.Danger);
            GUI.Label(new Rect(point.x - 30, point.y + 5, 60, 18), "YOU", Style(10, SurvivalLegendArt.Text, FontStyle.Bold, TextAnchor.MiddleCenter));
        }

        private void DrawAimPreview(RunState run)
        {
            if (run.AimSlot < 0 || run.Phase != RunPhase.Playing) return;
            Vector2 originWorld = run.Player.Position, cursorWorld = _game.CursorWorld;
            Vector2 delta = cursorWorld - originWorld;
            float angle = delta.sqrMagnitude > .001f ? Mathf.Atan2(delta.y, delta.x) : run.Player.Facing;
            Color color = new Color(CharacterColor(run.Character).r, CharacterColor(run.Character).g, CharacterColor(run.Character).b, .8f);
            if (run.AimSlot == 6)
            {
                DrawAimLine(SurvivalLegendArt.WorldToView(originWorld), SurvivalLegendArt.WorldToView(cursorWorld), color, 1.5f);
                return;
            }
            if (run.AimSlot >= 4)
            {
                int specialSlot = run.AimSlot - 4;
                if (specialSlot < 0 || specialSlot >= run.Specials.Length || run.Specials[specialSlot].Id != "blackhole") return;
                SpecialData special = Catalog.Specials["blackhole"];
                Vector2 center = originWorld + Vector2.ClampMagnitude(delta, special.Range);
                DrawGroundRing(originWorld, special.Range, new Color(color.r, color.g, color.b, .2f));
                DrawGroundRing(center, special.Radius, color);
                return;
            }
            if (_game.Simulation == null || run.AimSlot >= run.Skills.Length) return;
            TargetingData target = _game.Simulation.GetSkillTargeting(run.AimSlot);
            if (target.Kind == TargetKind.Self)
            {
                DrawGroundRing(originWorld, Mathf.Max(35, target.Radius), color);
                return;
            }
            if (target.Kind == TargetKind.Point)
            {
                Vector2 center = originWorld + Vector2.ClampMagnitude(delta, target.Range);
                DrawGroundRing(originWorld, target.Range, new Color(color.r, color.g, color.b, .2f));
                DrawGroundRing(center, Mathf.Max(18, target.Radius), color);
                if (target.CoreRadius > 0) DrawGroundRing(center, target.CoreRadius, new Color(1f, .95f, .72f, .9f));
                DrawAimLine(SurvivalLegendArt.WorldToView(originWorld), SurvivalLegendArt.WorldToView(center), new Color(color.r, color.g, color.b, .32f), 1);
                return;
            }
            Vector2 forward = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            Vector2 side = new Vector2(-forward.y, forward.x);
            float halfWidth = Mathf.Max(2, target.Width * .5f);
            Vector2 endWorld = originWorld + forward * target.Range;
            Vector2 a = SurvivalLegendArt.WorldToView(originWorld - side * halfWidth);
            Vector2 b = SurvivalLegendArt.WorldToView(originWorld + side * halfWidth);
            Vector2 c = SurvivalLegendArt.WorldToView(endWorld + side * halfWidth);
            Vector2 d = SurvivalLegendArt.WorldToView(endWorld - side * halfWidth);
            DrawAimLine(a, b, color, 1); DrawAimLine(b, c, color, 1); DrawAimLine(c, d, color, 1); DrawAimLine(d, a, color, 1);
            DrawAimLine(SurvivalLegendArt.WorldToView(originWorld), SurvivalLegendArt.WorldToView(endWorld), new Color(color.r, color.g, color.b, .48f), 1);
            if (target.Spread > 0)
            {
                float half = target.Spread * Mathf.Deg2Rad * .5f;
                foreach (float spread in new[] { -half, half })
                {
                    Vector2 ray = new Vector2(Mathf.Cos(angle + spread), Mathf.Sin(angle + spread));
                    DrawAimLine(SurvivalLegendArt.WorldToView(originWorld), SurvivalLegendArt.WorldToView(originWorld + ray * target.Range), color, 1.5f);
                }
            }
        }

        private static void DrawGroundRing(Vector2 world, float radius, Color color)
        {
            if (radius <= 0) return;
            Vector2 p = SurvivalLegendArt.WorldToView(world);
            float rx = radius * .5515f, ry = radius * .2192f;
            GUI.color = color;
            GUI.DrawTexture(new Rect(p.x - rx, p.y - ry, rx * 2, ry * 2), SurvivalLegendArt.Ring(color), ScaleMode.StretchToFill, true);
            GUI.color = Color.white;
        }

        private void DrawEnemy(EnemyState enemy, Vector2 point)
        {
            bool elite = enemy.Kind == "elite";
            Color aura = elite ? new Color(1f, .4f, .33f, .2f) : EnemyColor(enemy.Kind, .2f);
            DrawShadow(point, elite ? 29 : 10, elite ? 11 : 4, aura);
            if (enemy.Windup > 0) DrawAimLine(point, SurvivalLegendArt.WorldToView(enemy.Aim), new Color(1f, .62f, .45f, .75f), 2);
            Color tint = enemy.Flash > 0 ? new Color(1f, .94f, .84f) : enemy.Root > 0 ? new Color(.57f, .69f, 1f) : Color.white;
            DrawActor(SurvivalLegendArt.Enemy(enemy.Kind, enemy.Facing, enemy.Moving, enemy.Aiming), point, .58f, tint);
            DrawHealthBar(new Vector2(point.x, point.y - (elite ? 72 : 43)), elite ? 62 : 28, enemy.Hp / Mathf.Max(1, enemy.MaxHp), EnemyColor(enemy.Kind, 1));
            if (elite) GUI.Label(new Rect(point.x - 64, point.y - 91, 128, 17), "파멸의 집행자", Style(10, SurvivalLegendArt.Edge, FontStyle.Bold, TextAnchor.MiddleCenter));
        }

        private void DrawProjectile(ProjectileState shot)
        {
            Vector2 p = SurvivalLegendArt.WorldToView(shot.Position, shot.Elevation);
            Vector2 d = new Vector2(shot.Velocity.x * World.WorldProjection.HorizontalScale, shot.Velocity.y * World.WorldProjection.VerticalScale).normalized;
            Color color = shot.Hostile ? new Color(1f, .45f, .34f) : Parse(shot.Color, shot.Visual == "fireball" ? new Color(1f, .59f, .35f) : new Color(.88f, 1f, .79f));
            if (shot.Visual == "fireball")
            {
                GUI.color = new Color(color.r, color.g, color.b, .42f);
                GUI.DrawTexture(new Rect(p.x - 13, p.y - 13, 26, 26), SurvivalLegendArt.Disc(GUI.color), ScaleMode.StretchToFill, true);
                GUI.color = Color.white; DrawLine(p - d * 18, p, new Color(color.r, color.g, color.b, .55f), 7);
                Fill(new Rect(p.x - 5, p.y - 5, 10, 10), Color.Lerp(color, Color.white, .48f));
            }
            else
            {
                DrawLine(p - d * 11, p + d * 9, color, shot.Hostile ? 4 : shot.Pierce ? 4 : 2);
                Fill(new Rect(p.x - 2, p.y - 2, 4, 4), Color.white);
            }
        }

        private void DrawZone(ZoneState zone)
        {
            Vector2 p = SurvivalLegendArt.WorldToView(zone.Position);
            Color color = zone.Hostile ? new Color(1f, .42f, .38f, zone.Warning > 0 ? .72f : .5f) : new Color(.72f, .9f, .58f, .6f);
            if (zone.Visual == "rain") color = new Color(.91f, 1f, .76f, .64f);
            else if (zone.Visual == "spin") color = new Color(1f, .82f, .48f, .7f);
            else if (zone.Visual == "sword") color = new Color(1f, .89f, .63f, .85f);
            else if (zone.Visual == "blackhole") color = new Color(.68f, .44f, 1f, .72f);
            if (zone.Shape == "circle")
            {
                float rx = zone.Radius * .5515f, ry = zone.Radius * .2192f;
                GUI.color = color;
                GUI.DrawTexture(new Rect(p.x - rx, p.y - ry, rx * 2, ry * 2), SurvivalLegendArt.Ring(color), ScaleMode.StretchToFill, true);
                if (zone.Warning <= 0) { GUI.color = new Color(color.r, color.g, color.b, .12f); GUI.DrawTexture(new Rect(p.x - rx, p.y - ry, rx * 2, ry * 2), SurvivalLegendArt.Ring(GUI.color, 128, 56), ScaleMode.StretchToFill, true); }
                if (zone.Visual == "sword")
                {
                    DrawLine(new Vector2(p.x, p.y - ry * .22f), new Vector2(p.x, p.y - 92), new Color(1f, .94f, .76f, .95f), 11);
                    DrawLine(new Vector2(p.x - 18, p.y - 55), new Vector2(p.x + 18, p.y - 55), SurvivalLegendArt.Edge, 5);
                }
                GUI.color = Color.white;
            }
            else
            {
                Vector2 axisWorld = new Vector2(Mathf.Cos(zone.Angle), Mathf.Sin(zone.Angle));
                Vector2 end = SurvivalLegendArt.WorldToView(zone.Position + axisWorld * zone.Length);
                DrawAimLine(p, end, color, Mathf.Max(3, zone.Width * .155f));
            }
        }

        private void DrawEffect(EffectState effect)
        {
            Vector2 p = SurvivalLegendArt.WorldToView(effect.Position, effect.Elevation);
            Color color = Parse(effect.Color, Color.white);
            float t = 1f - effect.Life / Mathf.Max(.001f, effect.MaxLife);
            if (effect.Kind == "text")
            {
                GUI.Label(new Rect(p.x - 45, p.y - 70 - t * 24, 90, 24), effect.Text, Style(effect.Critical ? 18 : 13, color, FontStyle.Bold, TextAnchor.MiddleCenter));
            }
            else if (effect.Kind == "lightning")
            {
                Vector2 end = SurvivalLegendArt.WorldToView(effect.End);
                DrawLightning(p, end, effect.Position.GetHashCode(), t, color);
            }
            else if (effect.Kind == "slash")
            {
                Vector2 end = effect.End == Vector2.zero ? p + new Vector2(effect.Radius, -effect.Radius * .25f) : SurvivalLegendArt.WorldToView(effect.End);
                DrawLine(p, end, new Color(color.r, color.g, color.b, 1f - t), Mathf.Lerp(7, 2, t));
            }
            else
            {
                float radius = Mathf.Max(10, effect.Radius * (.35f + t * .7f));
                GUI.color = new Color(color.r, color.g, color.b, 1f - t);
                GUI.DrawTexture(new Rect(p.x - radius, p.y - radius * .4f, radius * 2, radius * .8f), SurvivalLegendArt.Ring(GUI.color), ScaleMode.StretchToFill, true);
                GUI.color = Color.white;
            }
        }

        private void DrawLightning(Vector2 from, Vector2 to, int seed, float age, Color color)
        {
            const int segments = 10; Vector2 previous = from; Vector2 delta = to - from; Vector2 normal = new Vector2(-delta.y, delta.x).normalized;
            int frame = Mathf.FloorToInt(age * 42f);
            for (int i = 1; i <= segments; i++)
            {
                float t = i / (float)segments; float noise = Mathf.Sin((i + 1) * 127.1f + seed * .0317f + frame * 73.3f) * 43758.5453f;
                float offset = i == segments ? 0 : (noise - Mathf.Floor(noise)) * 18f - 9f;
                Vector2 next = from + delta * t + normal * offset;
                DrawLine(previous, next, new Color(.15f, .54f, 1f, .3f), 6); DrawLine(previous, next, color, 2); previous = next;
            }
        }

        private void DrawTopBar(RunState run)
        {
            Fill(new Rect(0, 0, W, 58), new Color32(8, 14, 17, 246));
            Fill(new Rect(0, 57, W, 1), SurvivalLegendArt.Metal);
            Panel(new Rect(18, 10, 42, 38), false);
            GUI.Label(new Rect(18, 9, 42, 27), run.Level.ToString(), Style(18, SurvivalLegendArt.Edge, FontStyle.Normal, TextAnchor.MiddleCenter));
            GUI.Label(new Rect(18, 31, 42, 14), "LV", Style(9, SurvivalLegendArt.Edge, FontStyle.Bold, TextAnchor.MiddleCenter));
            GUI.Label(new Rect(72, 8, 240, 23), CharacterName(run.Character), _label);
            GUI.Label(new Rect(72, 28, 240, 17), "SURVIVAL LEGEND", Style(10, SurvivalLegendArt.Accent, FontStyle.Bold, TextAnchor.MiddleLeft));
            GUI.Label(new Rect(820, 8, 160, 38), $"생존 시간\n{FormatTime(run.Time)}", Style(12, SurvivalLegendArt.Text, FontStyle.Bold, TextAnchor.MiddleCenter));
            GUI.Label(new Rect(960, 8, 120, 38), $"처치\n{run.Kills:000}", Style(12, SurvivalLegendArt.Text, FontStyle.Bold, TextAnchor.MiddleCenter));
            if (GUI.Button(new Rect(1178, 11, 78, 34), "Ⅱ  ESC", _button)) _game.TogglePause();
        }

        private void DrawXp(RunState run)
        {
            float ratio = run.NextXp > 0 ? Mathf.Clamp01(run.Xp / (float)run.NextXp) : 0;
            Fill(new Rect(0, H - 10, W, 10), new Color32(16, 38, 30, 255));
            Fill(new Rect(0, H - 10, W * ratio, 10), SurvivalLegendArt.Accent);
            GUI.Label(new Rect(15, H - 15, 100, 14), "LV " + run.Level, Style(9, SurvivalLegendArt.Edge, FontStyle.Bold, TextAnchor.MiddleLeft));
            GUI.Label(new Rect(W * .5f - 80, H - 15, 160, 14), $"{run.Xp} / {run.NextXp} XP", Style(9, SurvivalLegendArt.Text, FontStyle.Bold, TextAnchor.MiddleCenter));
        }

        private void DrawCombatHud(RunState run)
        {
            float hp = Mathf.Clamp01(run.Player.Hp / Mathf.Max(1, run.Player.MaxHp));
            float ultimate = Mathf.Clamp01(run.Ultimate / 100f);
            string ultimateName = run.Skills != null && run.Skills.Length > 3 ? run.Skills[3].Name : "궁극기";
            GUI.Label(new Rect(151, 538, 158, 24), $"ULTIMATE\n{ultimateName}", Style(10, SurvivalLegendArt.Edge, FontStyle.Bold, TextAnchor.MiddleCenter));
            GUI.DrawTexture(new Rect(164, 558, 124, 124), SurvivalLegendArt.Orb(ultimate, false));
            GUI.Label(new Rect(164, 585, 124, 28), "R", Style(19, Color.white, FontStyle.Bold, TextAnchor.MiddleCenter));
            GUI.Label(new Rect(164, 617, 124, 24), run.Ultimate >= 100 ? "사용 가능" : Mathf.FloorToInt(run.Ultimate) + "%", Style(11, Color.white, FontStyle.Bold, TextAnchor.MiddleCenter));
            if (GUI.Button(new Rect(164, 558, 124, 124), GUIContent.none, GUIStyle.none)) _game.RequestCast(3);
            DrawQuickCastToggle(new Rect(182, 684, 88, 17), 3);
            GUI.Label(new Rect(980, 538, 140, 24), "VITALITY\n체력", Style(10, SurvivalLegendArt.Edge, FontStyle.Bold, TextAnchor.MiddleCenter));
            GUI.DrawTexture(new Rect(988, 558, 124, 124), SurvivalLegendArt.Orb(hp, true));
            GUI.Label(new Rect(988, 586, 124, 31), Mathf.CeilToInt(Mathf.Max(0, run.Player.Hp)).ToString(), Style(25, Color.white, FontStyle.Bold, TextAnchor.MiddleCenter));
            GUI.Label(new Rect(988, 615, 124, 20), "/ " + Mathf.CeilToInt(run.Player.MaxHp), Style(10, Color.white, FontStyle.Normal, TextAnchor.MiddleCenter));
            float totalShield = run.Player.Shield + run.Player.SkillShield;
            GUI.Label(new Rect(988, 681, 124, 19), totalShield > 0 ? "보호막 +" + Mathf.CeilToInt(totalShield) : $"생명력  {Mathf.RoundToInt(hp * 100)}%", Style(10, SurvivalLegendArt.Text, FontStyle.Bold, TextAnchor.MiddleCenter));
            Rect console = new Rect(315, 554, 650, 140); Panel(console, false);
            GUI.Label(new Rect(430, 528, 420, 24), $"SHIFT   회피   {run.DodgeCharges}/2  ·  {(run.DodgeCharges == 2 ? "준비 완료" : run.DodgeRecharge.ToString("0.0") + "초 후 +1")}", Style(11, SurvivalLegendArt.Text, FontStyle.Bold, TextAnchor.MiddleCenter));
            string[] keys = { "Q", "W", "E", "D", "F" };
            for (int i = 0; i < 5; i++)
            {
                int contractSlot = i < 3 ? i : i + 1;
                SkillState skill = i < 3 ? run.Skills[i] : null;
                SpecialState special = i >= 3 ? run.Specials[i - 3] : null;
                string id = skill != null ? skill.Id : special.Id;
                string name = skill != null ? skill.Name : special.Name;
                float remaining = skill != null ? skill.Remaining : special.Remaining;
                float displayRemaining = skill != null ? remaining / Mathf.Max(.01f, _game.Simulation.CooldownRecovery(contractSlot)) : remaining;
                float cooldown = skill != null ? skill.Cooldown : special.Cooldown;
                Rect slot = new Rect(console.x + 14 + i * 124, console.y + 19, 112, 83);
                bool armed = run.AimSlot == contractSlot;
                DrawSkillSlot(slot, keys[i], id, name, remaining, displayRemaining, cooldown, skill != null ? skill.Level : 0, armed);
                if (GUI.Button(slot, GUIContent.none, GUIStyle.none)) _game.RequestCast(contractSlot);
                bool supportsQuickCast = contractSlot == 1 || contractSlot == 2 || id == "blackhole";
                if (supportsQuickCast) DrawQuickCastToggle(new Rect(slot.x + 18, slot.yMax + 2, 76, 15), contractSlot);
            }
            Fill(new Rect(console.x + 15, console.y + 121, console.width - 30, 1), SurvivalLegendArt.Line);
            GUI.Label(new Rect(console.x + 16, console.y + 123, 250, 14), $"{CharacterName(run.Character)} · LV {run.Level}", _tiny);
            GUI.Label(new Rect(console.x + console.width - 260, console.y + 122, 245, 16), $"공격 {run.Player.Damage:0} · 방어 {run.Player.Armor:0}", Style(10, SurvivalLegendArt.Muted, FontStyle.Bold, TextAnchor.MiddleRight));
        }

        private void DrawSkillSlot(Rect rect, string key, string id, string name, float remaining, float displayRemaining, float cooldown, int level, bool armed)
        {
            Fill(rect, SurvivalLegendArt.Inset); Stroke(rect, armed ? SurvivalLegendArt.Accent : SurvivalLegendArt.Metal, armed ? 2 : 1);
            Texture2D icon = Icon(id);
            if (icon != null) GUI.DrawTexture(new Rect(rect.x + 31, rect.y + 8, 49, 49), icon, ScaleMode.ScaleToFit, true);
            Panel(new Rect(rect.x + 4, rect.y + 4, 20, 18), false);
            GUI.Label(new Rect(rect.x + 4, rect.y + 4, 20, 18), key, Style(11, SurvivalLegendArt.Text, FontStyle.Bold, TextAnchor.MiddleCenter));
            if (level > 0) GUI.Label(new Rect(rect.x + rect.width - 38, rect.y + 4, 33, 15), "Lv." + level, Style(9, SurvivalLegendArt.Muted, FontStyle.Bold, TextAnchor.MiddleRight));
            GUI.Label(new Rect(rect.x + 3, rect.y + rect.height - 24, rect.width - 6, 20), name, Style(11, SurvivalLegendArt.Text, FontStyle.Bold, TextAnchor.MiddleCenter));
            if (remaining > 0)
            {
                float ratio = Mathf.Clamp01(remaining / Mathf.Max(.01f, cooldown));
                Fill(new Rect(rect.x, rect.y, rect.width, rect.height * ratio), new Color(0, 0, 0, .68f));
                GUI.Label(rect, displayRemaining.ToString("0.0"), Style(20, Color.white, FontStyle.Bold, TextAnchor.MiddleCenter));
            }
        }

        private void DrawQuickCastToggle(Rect rect, int slot)
        {
            if (_game.QuickCastSlots == null || slot < 0 || slot >= _game.QuickCastSlots.Length) return;
            bool enabled = _game.QuickCastSlots[slot];
            Fill(rect, enabled ? SurvivalLegendArt.Selection : SurvivalLegendArt.Inset);
            Stroke(rect, enabled ? SurvivalLegendArt.Accent : SurvivalLegendArt.Line, 1);
            GUI.Label(rect, enabled ? "✓ 즉시" : "□ 즉시", Style(9, enabled ? SurvivalLegendArt.Accent : SurvivalLegendArt.Muted, FontStyle.Bold, TextAnchor.MiddleCenter));
            if (GUI.Button(rect, GUIContent.none, GUIStyle.none)) _game.QuickCastSlots[slot] = !enabled;
        }

        private void DrawPause()
        {
            Veil(); Rect panel = new Rect(420, 190, 440, 330); Panel(panel, true);
            GUI.Label(new Rect(panel.x, panel.y + 35, panel.width, 24), "전투 일시정지", _heading);
            GUI.Label(new Rect(panel.x + 40, panel.y + 80, panel.width - 80, 65), "ESC 또는 SPACE로 계속합니다.\n우클릭 이동 · A+좌클릭 공격 이동\nQ W E R / D F 스킬 · SHIFT 회피", Style(12, SurvivalLegendArt.Muted, FontStyle.Normal, TextAnchor.MiddleCenter));
            if (FrameButton(new Rect(panel.x + 80, panel.y + 174, panel.width - 160, 48), "계속하기", true)) _game.TogglePause();
            if (GUI.Button(new Rect(panel.x + 80, panel.y + 238, panel.width - 160, 38), "타이틀로 돌아가기", _button)) { _game.ReturnToTitle(); _showLoadout = false; }
        }

        private void DrawAugments(RunState run)
        {
            Veil();
            GUI.Label(new Rect(0, 104, W, 20), $"LEVEL {run.Level} REACHED", Style(10, SurvivalLegendArt.Accent, FontStyle.Bold, TextAnchor.MiddleCenter));
            GUI.Label(new Rect(0, 126, W, 43), run.SpecialReward ? "엘리트 보상을 선택하세요" : "다음 진화를 선택하세요", _heading);
            GUI.Label(new Rect(0, 165, W, 24), "증강 하나를 선택하면 전투가 계속됩니다.", Style(11, SurvivalLegendArt.Muted, FontStyle.Normal, TextAnchor.MiddleCenter));
            for (int i = 0; i < run.AugmentChoices.Count && i < 3; i++)
            {
                AugmentChoice choice = run.AugmentChoices[i]; Rect card = new Rect(168 + i * 320, 212, 300, 334);
                Color tier = TierColor(choice.Tier); Panel(card, false); Stroke(card, tier, 1);
                GUI.Label(new Rect(card.x + 20, card.y + 15, 180, 20), TierName(choice.Tier), Style(9, tier, FontStyle.Bold, TextAnchor.MiddleLeft));
                GUI.Label(new Rect(card.x + card.width - 48, card.y + 15, 28, 20), (i + 1).ToString("00"), Style(9, SurvivalLegendArt.Muted, FontStyle.Normal, TextAnchor.MiddleRight));
                Texture2D icon = IconForChoice(choice, run);
                if (icon != null) GUI.DrawTexture(new Rect(card.x + 19, card.y + 54, 62, 62), icon, ScaleMode.ScaleToFit, true);
                GUI.Label(new Rect(card.x + 20, card.y + 126, card.width - 40, 35), choice.Title, Style(18, SurvivalLegendArt.Text, FontStyle.Bold, TextAnchor.MiddleLeft));
                GUI.Label(new Rect(card.x + 20, card.y + 166, card.width - 40, 72), choice.Description, Style(11, SurvivalLegendArt.Muted, FontStyle.Normal, TextAnchor.UpperLeft));
                Fill(new Rect(card.x + 20, card.y + 250, card.width - 40, 1), SurvivalLegendArt.Line);
                GUI.Label(new Rect(card.x + 20, card.y + 264, 130, 25), run.Phase == RunPhase.Augment ? "선택하기" : "카드 펼치는 중", Style(11, tier, FontStyle.Bold, TextAnchor.MiddleLeft));
                GUI.Label(new Rect(card.x + card.width - 50, card.y + 264, 30, 25), run.Phase == RunPhase.Augment ? "↗" : "…", Style(15, tier, FontStyle.Bold, TextAnchor.MiddleRight));
                // Selection deliberately ends above the reroll row so its input can never be consumed by the card.
                Rect chooseArea = new Rect(card.x, card.y, card.width, 296);
                if (run.Phase == RunPhase.Augment && GUI.Button(chooseArea, GUIContent.none, GUIStyle.none)) _game.ChooseAugment(i);
                bool canReroll = run.Phase == RunPhase.Augment && !run.SpecialReward && !choice.Rerolled;
                if (canReroll && GUI.Button(new Rect(card.x + 20, card.y + 300, card.width - 40, 24), "한 번 다시 뽑기", _button)) _game.RerollAugment(i);
            }
        }

        private void DrawEliteReward(RunState run)
        {
            Fill(new Rect(0, 0, W, H), new Color(1f, .86f, .62f, Mathf.Lerp(.28f, 0f, run.PhaseElapsed / 2.4f)));
            GUI.Label(new Rect(0, 210, W, 35), "ELITE DEFEATED", Style(15, SurvivalLegendArt.Edge, FontStyle.Bold, TextAnchor.MiddleCenter));
            GUI.Label(new Rect(0, 244, W, 60), "파멸의 집행자 격파", _heading);
            GUI.Label(new Rect(0, 298, W, 28), "특별 증강이 기다립니다", Style(13, SurvivalLegendArt.PowerLight, FontStyle.Normal, TextAnchor.MiddleCenter));
        }

        private void DrawDying(RunState run)
        {
            Fill(new Rect(0, 0, W, H), new Color(.3f, .02f, .04f, Mathf.Clamp01(run.PhaseElapsed / 1.2f) * .5f));
            GUI.Label(new Rect(0, 260, W, 60), "쓰러졌습니다", _heading);
        }

        private void DrawResults(RunState run)
        {
            Veil(); Rect panel = new Rect(260, 92, 760, 535); Panel(panel, true);
            GUI.Label(new Rect(panel.x + 32, panel.y + 24, 250, 18), "RUN COMPLETE", Style(10, SurvivalLegendArt.Accent, FontStyle.Bold, TextAnchor.MiddleLeft));
            GUI.Label(new Rect(panel.x + 32, panel.y + 48, 450, 40), "생존 기록", Style(28, SurvivalLegendArt.Text, FontStyle.Bold, TextAnchor.MiddleLeft));
            GUI.Label(new Rect(panel.x + 32, panel.y + 89, 500, 20), $"{CharacterName(run.Character)} · LV {run.Level}   |   {FormatTime(run.Time)}", _small);
            Fill(new Rect(panel.x + 30, panel.y + 120, panel.width - 60, 1), SurvivalLegendArt.Line);
            ResultMetric(new Rect(panel.x + 32, panel.y + 145, 158, 92), "일반 처치", Mathf.Max(0, run.Kills - run.EliteKills).ToString());
            ResultMetric(new Rect(panel.x + 205, panel.y + 145, 158, 92), "가한 피해", Mathf.RoundToInt(run.DamageDealt).ToString("N0"));
            ResultMetric(new Rect(panel.x + 378, panel.y + 145, 158, 92), "받은 피해", Mathf.RoundToInt(run.DamageTaken).ToString("N0"));
            ResultMetric(new Rect(panel.x + 551, panel.y + 145, 158, 92), "엘리트", run.EliteKills.ToString());
            GUI.Label(new Rect(panel.x + 32, panel.y + 266, 200, 22), "획득한 증강", _panelTitle);
            Rect historyViewport = new Rect(panel.x + 32, panel.y + 298, panel.width - 64, 119);
            int rows = Mathf.Max(1, Mathf.CeilToInt(run.History.Count / 3f));
            Rect historyContent = new Rect(0, 0, historyViewport.width - 18, rows * 42);
            _resultScroll = GUI.BeginScrollView(historyViewport, _resultScroll, historyContent, false, rows * 42 > historyViewport.height);
            for (int i = 0; i < run.History.Count; i++)
            {
                float cellWidth = (historyContent.width - 16) / 3f;
                Rect item = new Rect((i % 3) * (cellWidth + 8), (i / 3) * 42, cellWidth, 34);
                Fill(item, SurvivalLegendArt.Inset); Stroke(item, SurvivalLegendArt.Line, 1); GUI.Label(new Rect(item.x + 9, item.y, item.width - 18, item.height), run.History[i], _small);
            }
            if (run.History.Count == 0) GUI.Label(new Rect(0, 2, historyContent.width, 40), "이번 원정에서 획득한 증강이 없습니다.", _small);
            GUI.EndScrollView();
            if (FrameButton(new Rect(panel.x + 32, panel.y + 438, 320, 50), "다시 도전", true)) _game.StartRun((uint)DateTime.UtcNow.Ticks, _specialIds[_specialD], _specialIds[_specialF], run.Character);
            if (GUI.Button(new Rect(panel.x + 380, panel.y + 438, 320, 50), "타이틀로", _button)) { _game.ReturnToTitle(); _showLoadout = false; }
        }

        private void ResultMetric(Rect rect, string label, string value)
        {
            Fill(rect, SurvivalLegendArt.Inset); Stroke(rect, SurvivalLegendArt.Line, 1);
            GUI.Label(new Rect(rect.x + 15, rect.y + 9, rect.width - 30, 20), label, _small);
            GUI.Label(new Rect(rect.x + 15, rect.y + 34, rect.width - 30, 42), value, Style(25, SurvivalLegendArt.Edge, FontStyle.Bold, TextAnchor.MiddleLeft));
        }

        private static void Panel(Rect rect, bool highlighted)
        {
            Fill(rect, highlighted ? new Color32(19, 43, 39, 248) : new Color32(16, 26, 29, 248));
            Stroke(rect, highlighted ? SurvivalLegendArt.Edge : SurvivalLegendArt.Metal, highlighted ? 2 : 1);
            Stroke(new Rect(rect.x + 4, rect.y + 4, rect.width - 8, rect.height - 8), highlighted ? SurvivalLegendArt.Metal : SurvivalLegendArt.Line, 1);
        }

        private bool FrameButton(Rect rect, string text, bool primary)
        {
            Fill(rect, primary ? SurvivalLegendArt.Selection : SurvivalLegendArt.Inset);
            Stroke(rect, primary ? SurvivalLegendArt.Edge : SurvivalLegendArt.Metal, 2);
            Stroke(new Rect(rect.x + 5, rect.y + 5, rect.width - 10, rect.height - 10), primary ? SurvivalLegendArt.Metal : SurvivalLegendArt.Line, 1);
            return GUI.Button(rect, text, _button);
        }

        private static void Veil() => Fill(new Rect(0, 58, W, H - 58), new Color(0.01f, .035f, .045f, .88f));
        private static void DrawRule(float x, float y, float width)
        {
            Fill(new Rect(x, y, width, 1), SurvivalLegendArt.Metal);
            Fill(new Rect(x + width * .5f - 3, y - 2, 6, 5), SurvivalLegendArt.Edge);
        }
        private static void Fill(Rect rect, Color color) { Color before = GUI.color; GUI.color = color; GUI.DrawTexture(rect, Texture2D.whiteTexture); GUI.color = before; }
        private static void Stroke(Rect rect, Color color, int thickness)
        {
            Fill(new Rect(rect.x, rect.y, rect.width, thickness), color); Fill(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color);
            Fill(new Rect(rect.x, rect.y, thickness, rect.height), color); Fill(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), color);
        }
        private static void DrawShadow(Vector2 point, float rx, float ry, Color color)
        {
            Color before = GUI.color; GUI.color = Color.white; GUI.DrawTexture(new Rect(point.x - rx, point.y - ry, rx * 2, ry * 2), SurvivalLegendArt.Disc(color), ScaleMode.StretchToFill, true); GUI.color = before;
        }
        private static void DrawActor(Texture2D texture, Vector2 foot, float scale, Color tint)
        {
            float width = texture.width * scale, height = texture.height * scale; Color before = GUI.color; GUI.color = tint;
            GUI.DrawTexture(new Rect(foot.x - width * .5f, foot.y - height + 10 * scale, width, height), texture, ScaleMode.StretchToFill, true); GUI.color = before;
        }

        private static void DrawPlayerActor(string hero, float facing, bool moving, bool attacking, float attackProgress, Vector2 foot, float scale, Color tint)
        {
            Texture2D atlas = SurvivalLegendArt.PlayerAtlas(hero);
            if (atlas == null)
            {
                DrawActor(SurvivalLegendArt.Player(hero, facing, moving, attacking), foot, scale, tint);
                return;
            }
            const float frameWidth = 128, frameHeight = 128, footX = 64, footY = 112;
            Rect target = new Rect(foot.x - footX * scale, foot.y - footY * scale, frameWidth * scale, frameHeight * scale);
            Color previous = GUI.color; GUI.color = tint;
            GUI.DrawTextureWithTexCoords(target, atlas, SurvivalLegendArt.PlayerFrameUv(facing, moving, attacking, attackProgress), true);
            GUI.color = previous;
        }
        private static void DrawHealthBar(Vector2 point, float width, float ratio, Color color)
        {
            Fill(new Rect(point.x - width * .5f, point.y, width, 5), new Color32(7, 19, 21, 255));
            Fill(new Rect(point.x - width * .5f + 1, point.y + 1, Mathf.Max(0, width - 2) * Mathf.Clamp01(ratio), 3), color);
        }
        private static void DrawAimLine(Vector2 from, Vector2 to, Color color, float width) => DrawLine(from, to, color, width);
        private static void DrawLine(Vector2 from, Vector2 to, Color color, float width)
        {
            Vector2 delta = to - from; float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            Matrix4x4 before = GUI.matrix; Color previous = GUI.color;
            GUI.color = color; GUIUtility.RotateAroundPivot(angle, from); GUI.DrawTexture(new Rect(from.x, from.y - width * .5f, delta.magnitude, width), Texture2D.whiteTexture);
            GUI.matrix = before; GUI.color = previous;
        }
        private Texture2D Icon(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (_icons.TryGetValue(id, out Texture2D icon)) return icon;
            icon = Resources.Load<Texture2D>("Art/Icons/" + id); if (icon != null) { icon.filterMode = FilterMode.Point; _icons[id] = icon; }
            return icon;
        }
        private Texture2D IconForChoice(AugmentChoice choice, RunState run)
        {
            if (choice.SkillSlot >= 0 && choice.SkillSlot < run.Skills.Length) return Icon(run.Skills[choice.SkillSlot].Id);
            if (!string.IsNullOrEmpty(choice.Id) && choice.Id.Contains("armor")) return Icon("armor");
            if (!string.IsNullOrEmpty(choice.Id) && choice.Id.Contains("critical")) return Icon("critical");
            if (!string.IsNullOrEmpty(choice.Id) && choice.Id.Contains("haste")) return Icon("haste");
            if (!string.IsNullOrEmpty(choice.Id) && (choice.Id.Contains("health") || choice.Id.Contains("life"))) return Icon("heal");
            if (!string.IsNullOrEmpty(choice.Id) && choice.Id.Contains("speed")) return Icon("speed");
            return Icon("attack");
        }
        private static string CharacterName(string id) => id == "swordsman" ? "검사" : id == "mage" ? "마법사" : "궁수";
        private static Color CharacterColor(string id) => id == "swordsman" ? SurvivalLegendArt.Hex("#f5ce87") : id == "mage" ? SurvivalLegendArt.Hex("#c5a0ff") : SurvivalLegendArt.Hex("#b3f783");
        private static string SkillId(int character, int slot)
        {
            string prefix = character == 0 ? "sword" : character == 2 ? "magic" : "bow";
            return prefix + "-" + "qwer"[slot];
        }
        private static Color EnemyColor(string kind, float alpha)
        {
            Color c = kind == "chaser" ? SurvivalLegendArt.Hex("#dd90a0") : kind == "shooter" ? SurvivalLegendArt.Hex("#ffba87") : kind == "elite" ? SurvivalLegendArt.Hex("#ff647a") : SurvivalLegendArt.Hex("#cda2ff"); c.a = alpha; return c;
        }
        private static Color TierColor(string tier)
        {
            if (tier == "legendary" || tier == "special") return new Color32(255, 185, 99, 255);
            if (tier == "rare") return new Color32(125, 210, 255, 255);
            if (tier == "epic") return new Color32(205, 146, 255, 255);
            return new Color32(187, 240, 126, 255);
        }
        private static string TierName(string tier)
        {
            if (tier == "legendary") return "전설 증강"; if (tier == "special") return "특별 증강"; if (tier == "rare") return "희귀 증강"; if (tier == "epic") return "영웅 증강"; return "일반 증강";
        }
        private static string FormatTime(float seconds) => $"{Mathf.FloorToInt(seconds / 60):00}:{Mathf.FloorToInt(seconds % 60):00}";
        private static Color Parse(string value, Color fallback) { return !string.IsNullOrEmpty(value) && ColorUtility.TryParseHtmlString(value, out Color parsed) ? parsed : fallback; }
        private static string LocalizeNotice(string text)
        {
            if (text.StartsWith("Right click")) return "적 우클릭: 공격 · 빈 지면 우클릭: 이동 · A 후 좌클릭: 공격 이동";
            return text;
        }
    }
}
