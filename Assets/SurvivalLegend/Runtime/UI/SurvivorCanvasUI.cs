using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SurvivalLegend.Data;

namespace SurvivalLegend.UI
{
    /// <summary>Binds saved Canvas controls to a run. Only variable content rows are instantiated.</summary>
    [DefaultExecutionOrder(40)]
    public sealed class SurvivorCanvasUI : MonoBehaviour
    {
        public SurvivorGame Game;
        public GameObject TitlePanel, LoadoutPanel, HudPanel, PausePanel, AugmentPanel, ResultsPanel, TransitionPanel;
        public Button Enter, Start, Back, Pause, Resume, Quit, Retry, ResultsHome;
        public Button SpecialD, SpecialF;
        public Text SpecialDLabel, SpecialFLabel, SelectedKit, ErrorLabel;
        public Text Status, HealthLabel, XpLabel, Notice, DodgeLabel, UltimateLabel, TransitionLabel, ResultSummary;
        public Image HealthFill, XpFill, UltimateFill;
        [Header("Gothic HUD")]
        public bool OrbHud;
        public Text SectorLabel, KitLabel, CombatStatsLabel, ShieldLabel, EliteLabel, HealthMaximumLabel;
        public GameObject ElitePanel;
        public Image EliteFill;
        public RectTransform RosterContent, AugmentContent, HistoryContent;
        public CharacterCardUI CharacterTemplate;
        public AugmentCardUI AugmentTemplate;
        public Text HistoryTemplate;
        public SkillSlotUI[] Slots;
        public ScrollRect HistoryScroll;
        readonly List<CharacterCardUI> cards = new List<CharacterCardUI>();
        readonly List<AugmentCardUI> choices = new List<AugmentCardUI>();
        readonly List<Text> history = new List<Text>();
        readonly List<string> characters = new List<string>(), specials = new List<string>();
        readonly Dictionary<string, Texture2D> icons = new Dictionary<string, Texture2D>();
        GameContentSnapshot shownContent;
        RunState shownRun;
        RunPhase previousPhase = (RunPhase)(-1);
        string selected;
        int d, f = 1;
        bool loadout;

        void Awake()
        {
            Enter.onClick.AddListener(() => { loadout = true; RefreshPanels(); });
            Back.onClick.AddListener(() => { loadout = false; RefreshPanels(); });
            Start.onClick.AddListener(BeginRun);
            Pause.onClick.AddListener(() => Game.TogglePause());
            Resume.onClick.AddListener(() => Game.TogglePause());
            Quit.onClick.AddListener(Home);
            Retry.onClick.AddListener(BeginRun);
            ResultsHome.onClick.AddListener(Home);
            SpecialD.onClick.AddListener(() => CycleSpecial(true));
            SpecialF.onClick.AddListener(() => CycleSpecial(false));
            for (int i = 0; i < Slots.Length; i++)
            {
                int slot = i;
                Slots[i].Cast.onClick.AddListener(() => Game.RequestCast(slot));
                Slots[i].QuickCast.onValueChanged.AddListener(value => Game.QuickCastSlots[slot] = value);
            }
        }
        void Home() { Game.ReturnToTitle(); loadout = false; RefreshPanels(); }
        void BeginRun()
        {
            if (!Game.CanStart || characters.Count == 0 || specials.Count < 2) return;
            Game.StartRun(unchecked((uint)DateTime.UtcNow.Ticks), specials[d], specials[f], selected);
            loadout = false;
            RefreshPanels();
        }
        void CycleSpecial(bool first)
        {
            if (specials.Count < 2) return;
            if (first) { do d = (d + 1) % specials.Count; while (d == f); }
            else { do f = (f + 1) % specials.Count; while (f == d); }
            RefreshLoadout();
        }
        void Update()
        {
            if (Game == null) return;
            if (shownContent != Game.Content) RebuildRoster();
            ErrorLabel.text = Game.InitializationError ?? "";
            Start.interactable = Game.CanStart && characters.Count > 0 && specials.Count >= 2;
            var run = Game.State;
            if (run == null) return;
            if (shownRun != run || previousPhase != run.Phase)
            {
                shownRun = run; previousPhase = run.Phase;
                RefreshPanels();
                if (run.Phase == RunPhase.Results) RefreshResults(run);
            }
            if (run.Phase == RunPhase.Title) return;
            var p = run.Player;
            string className = Game.Content.Characters.TryGetValue(run.Character, out var c) ? c.Name : run.Character;
            Status.text = OrbHud ? $"LV. {run.Level}   ·   {Mathf.FloorToInt(run.Time / 60):00}:{Mathf.FloorToInt(run.Time % 60):00}   ·   처치 {run.Kills}" : $"{className}   LV. {run.Level}                         {Mathf.FloorToInt(run.Time / 60):00}:{Mathf.FloorToInt(run.Time % 60):00}                         처치 {run.Kills}  ·  정예 {run.EliteKills}";
            HealthFill.fillAmount = p.Hp / Mathf.Max(1, p.MaxHp);
            float shield = (p.ShieldRemaining > 0 ? p.Shield : 0) + (p.SkillShieldRemaining > 0 ? p.SkillShield : 0);
            HealthLabel.text = OrbHud ? Mathf.CeilToInt(p.Hp).ToString() : $"{Mathf.CeilToInt(p.Hp)} / {Mathf.CeilToInt(p.MaxHp)}" + (shield > 0 ? $"  +{Mathf.CeilToInt(shield)} 보호막" : "");
            if (HealthMaximumLabel != null) HealthMaximumLabel.text = $"/ {Mathf.CeilToInt(p.MaxHp)}";
            XpFill.fillAmount = (float)run.Xp / Mathf.Max(1, run.NextXp);
            XpLabel.text = $"경험치  {run.Xp} / {run.NextXp}";
            float maximum = Game.Content.Config.UltimateMaximum;
            UltimateFill.fillAmount = run.Ultimate / Mathf.Max(1, maximum);
            UltimateLabel.text = $"궁극기  {Mathf.FloorToInt(100 * run.Ultimate / Mathf.Max(1, maximum))}%";
            DodgeLabel.text = $"SHIFT  회피  {run.DodgeCharges}/{Game.Content.Config.DodgeCapacity}" + (run.DodgeRecharge > 0 ? $"  {run.DodgeRecharge:0.0}s" : "  준비");
            if (KitLabel != null) KitLabel.text = $"{className}  ·  LV. {run.Level}";
            if (CombatStatsLabel != null) CombatStatsLabel.text = $"공격 {p.Damage:0.#}  ·  방어 {p.Armor:0.#}";
            if (ShieldLabel != null) ShieldLabel.text = shield > 0 ? $"보호막 +{Mathf.CeilToInt(shield)}" : $"생명력 {100 * p.Hp / Mathf.Max(1, p.MaxHp):0}%";
            UpdateElite(run);
            Notice.text = run.AimSlot >= 0 ? "좌클릭으로 시전 위치 선택 · 우클릭 / ESC 취소" : run.NoticeTime > 0 ? LocalizeNotice(run.Notice) : "우클릭 이동 / 공격  ·  A 공격 이동  ·  S 정지";
            if (OrbHud && run.AimSlot < 0 && (run.NoticeTime <= 0 || string.IsNullOrEmpty(run.Notice) || run.Notice.StartsWith("Right click", StringComparison.Ordinal))) Notice.text = "";
            for (int i = 0; i < Slots.Length; i++)
            {
                var slot = Slots[i];
                string id = i < 4 ? run.Skills[i].Id : run.Specials[i - 4].Id;
                float remain = i < 4 ? run.Skills[i].Remaining : run.Specials[i - 4].Remaining;
                float total = i < 4 ? run.Skills[i].Cooldown : run.Specials[i - 4].Cooldown;
                slot.Name.text = i < 4 ? run.Skills[i].Name : run.Specials[i - 4].Name;
                if (slot.Level != null) slot.Level.text = i < 4 ? $"Lv.{run.Skills[i].Level}" : "";
                slot.Icon.texture = Icon(id);
                slot.Icon.enabled = slot.Icon.texture != null;
                bool ultimate = i < 4 && Game.Content.Skills[id].Ultimate;
                bool active = ultimate && run.Buffs.Exists(b => b.Id == id);
                float seconds = i < 4 ? remain / Mathf.Max(.01f, Game.Simulation.CooldownRecovery(i)) : remain;
                slot.Cooldown.text = active ? "활성" : remain > 0 ? seconds.ToString("0.0") : ultimate && run.Ultimate < maximum ? "충전 중" : "";
                slot.CooldownFill.fillAmount = remain / Mathf.Max(.01f, total);
                slot.Cast.interactable = Game.Simulation.CombatActive && remain <= 0 && !active && (!ultimate || run.Ultimate >= maximum);
                if (OrbHud && i == 3)
                {
                    UltimateLabel.text = active ? "효과 활성" : remain > 0 ? $"재사용 {seconds:0.0}s" : ultimate && run.Ultimate < maximum ? $"충전 {100 * run.Ultimate / Mathf.Max(1, maximum):0}%" : "사용 가능";
                    if (!ultimate) UltimateFill.fillAmount = remain <= 0 ? 1 : 1 - Mathf.Clamp01(remain / Mathf.Max(.01f, total));
                    slot.Cooldown.text = "";
                }
            }
            if (AugmentPanel.activeSelf) RefreshChoices(run);
            if (TransitionPanel.activeSelf) TransitionLabel.text = run.Phase == RunPhase.EliteDeath ? "정예 처치\n특별 보상을 준비합니다" : "전투 종료";
        }
        void UpdateElite(RunState run)
        {
            if (ElitePanel == null) return;
            EnemyState elite = null;
            foreach (var enemy in run.Enemies)
                if (enemy.Hp > 0 && Game.Simulation.IsElite(enemy.Kind)) { elite = enemy; break; }
            ElitePanel.SetActive(elite != null);
            if (elite == null) return;
            if (EliteFill != null) EliteFill.fillAmount = Mathf.Clamp01(elite.Hp / Mathf.Max(1, elite.MaxHp));
            if (EliteLabel != null) EliteLabel.text = $"{Game.Content.Enemies[elite.Kind].Name}    ·    정예 LV. {elite.Level}";
        }
        Texture2D Icon(string id)
        {
            if (!icons.TryGetValue(id, out var icon)) { icon = Resources.Load<Texture2D>("Art/Icons/" + id); icons[id] = icon; }
            return icon;
        }
        Texture2D AugmentIcon(AugmentChoice choice, RunState run)
        {
            if (choice.SkillSlot >= 0 && choice.SkillSlot < run.Skills.Length) return Icon(run.Skills[choice.SkillSlot].Id);
            if (choice.Tier == "special")
                foreach (var definition in Game.Content.SpecialAugments)
                    if (definition.Id == choice.Id)
                        foreach (var effect in definition.Effects)
                            if (!string.IsNullOrEmpty(effect.SkillId)) return Icon(effect.SkillId);
            switch (choice.Stat)
            {
                case "damage": return Icon("attack");
                case "armor": return Icon("armor");
                case "maxHp": case "healthRegen": case "lifesteal": return Icon("heal");
                case "speed": return Icon("speed");
                case "attackSpeed": return Icon("haste");
                case "critChance": case "critMultiplier": return Icon("critical");
                case "range": return Icon("bow-e");
                case "eliteDamage": return Icon("elite");
                case "chainTargets": return Icon("chain");
                case "all": case "q": case "w": case "e": case "r": return Icon("cooldown");
                default: return Icon("mastery");
            }
        }
        static string LocalizeNotice(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            if (value.StartsWith("Right click", StringComparison.Ordinal)) return "적 우클릭: 공격 · 빈 지면 우클릭: 이동 · A 후 좌클릭: 공격 이동";
            return value;
        }
        void RebuildRoster()
        {
            string oldD = d < specials.Count ? specials[d] : Game.Content?.DefaultSpecialDId, oldF = f < specials.Count ? specials[f] : Game.Content?.DefaultSpecialFId;
            shownContent = Game.Content;
            characters.Clear(); specials.Clear();
            if (shownContent == null) return;
            characters.AddRange(shownContent.Characters.Keys); specials.AddRange(shownContent.Specials.Keys);
            if (!characters.Contains(selected)) selected = shownContent.DefaultCharacterId;
            d = Mathf.Max(0, specials.IndexOf(oldD)); f = specials.IndexOf(oldF);
            if (f < 0 || f == d) f = specials.Count > 1 ? (d + 1) % specials.Count : 0;
            while (cards.Count < characters.Count) cards.Add(Instantiate(CharacterTemplate, RosterContent));
            for (int i = 0; i < cards.Count; i++)
            {
                var card = cards[i]; card.gameObject.SetActive(i < characters.Count);
                if (i >= characters.Count) continue;
                string id = characters[i]; var data = shownContent.Characters[id];
                card.Name.text = data.Name; card.Subtitle.text = data.Subtitle;
                card.Stats.text = $"체력 {data.Stats.Hp:0}   공격력 {data.Stats.Damage:0.#}\n이동 {data.Stats.Speed:0}   공격 속도 {data.Stats.AttackSpeed:0.##}\n{data.Traits}";
                var atlas = Resources.Load<Texture2D>("Art/Characters/" + id + "-sd-atlas");
                card.Portrait.texture = atlas; card.Portrait.enabled = atlas != null;
                card.Portrait.uvRect = new Rect(0, .75f, 1f / 6, 1f / 8);
                card.Select.onClick.RemoveAllListeners();
                card.Select.onClick.AddListener(() => { selected = id; RefreshLoadout(); });
            }
            RefreshLoadout();
        }
        void RefreshLoadout()
        {
            for (int i = 0; i < characters.Count; i++) cards[i].Selection.color = OrbHud ? (characters[i] == selected ? new Color(1f,.93f,.72f) : new Color(.68f,.72f,.75f)) : (characters[i] == selected ? new Color(.22f, .28f, .22f) : new Color(.07f, .12f, .13f));
            if (shownContent != null && shownContent.Characters.TryGetValue(selected, out var data))
            {
                var kit = new List<string>();
                for (int i = 0; i < data.Skills.Length; i++) if (shownContent.Skills.TryGetValue(data.Skills[i], out var skill)) kit.Add($"{"QWER"[i]}  {skill.Name}");
                SelectedKit.text = string.Join("    ·    ", kit);
            }
            if (specials.Count < 2) return;
            var sd = shownContent.Specials[specials[d]]; var sf = shownContent.Specials[specials[f]];
            SpecialDLabel.text = $"D  {sd.Name}  ›\n{sd.Description}";
            SpecialFLabel.text = $"F  {sf.Name}  ›\n{sf.Description}";
        }
        void RefreshPanels()
        {
            var phase = Game != null && Game.State != null ? Game.State.Phase : RunPhase.Title;
            TitlePanel.SetActive(phase == RunPhase.Title && !loadout);
            LoadoutPanel.SetActive(phase == RunPhase.Title && loadout);
            HudPanel.SetActive(phase != RunPhase.Title);
            PausePanel.SetActive(phase == RunPhase.Paused);
            AugmentPanel.SetActive(phase == RunPhase.Augment || phase == RunPhase.AugmentEnter);
            ResultsPanel.SetActive(phase == RunPhase.Results);
            // Level-up feedback belongs to the player in the world, so combat stays visible.
            TransitionPanel.SetActive(phase == RunPhase.EliteDeath || phase == RunPhase.Dying);
        }
        void RefreshChoices(RunState run)
        {
            while (choices.Count < run.AugmentChoices.Count)
            {
                int index = choices.Count;
                var card = Instantiate(AugmentTemplate, AugmentContent);
                card.Choose.onClick.AddListener(() => Game.ChooseAugment(index));
                card.Reroll.onClick.AddListener(() => Game.RerollAugment(index));
                choices.Add(card);
            }
            for (int i = 0; i < choices.Count; i++)
            {
                var card = choices[i]; card.gameObject.SetActive(i < run.AugmentChoices.Count);
                if (i >= run.AugmentChoices.Count) continue;
                var data = run.AugmentChoices[i];
                bool ready = run.Phase == RunPhase.Augment;
                card.BindPresentation(data, AugmentIcon(data, run), i, run.AugmentChoices.Count, ready);
                card.Reroll.gameObject.SetActive(!run.SpecialReward);
                card.Choose.interactable = ready && card.RevealComplete;
                card.Reroll.interactable = ready && card.RevealComplete && !data.Rerolled && !run.SpecialReward;
            }
        }
        void RefreshResults(RunState run)
        {
            ResultSummary.text = $"{Mathf.FloorToInt(run.Time / 60):00}:{Mathf.FloorToInt(run.Time % 60):00} 생존   ·   LV. {run.Level}   ·   처치 {run.Kills - run.EliteKills} / 정예 {run.EliteKills}\n가한 피해 {run.DamageDealt:0}   받은 피해 {run.DamageTaken:0}   회복 {run.Healing:0}";
            while (history.Count < run.History.Count) history.Add(Instantiate(HistoryTemplate, HistoryContent));
            for (int i = 0; i < history.Count; i++) { history[i].gameObject.SetActive(i < run.History.Count); if (i < run.History.Count) history[i].text = run.History[i]; }
            HistoryScroll.verticalNormalizedPosition = 1;
        }
    }
}
