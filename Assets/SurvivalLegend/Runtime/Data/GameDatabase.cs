using System;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalLegend.Data
{
    [CreateAssetMenu(fileName = "SurvivalLegendGameDatabase", menuName = "Survival Legend/Game Database")]
    public sealed class GameDatabase : ScriptableObject
    {
        [Header("Registries")]
        public List<CharacterDefinition> Characters = new List<CharacterDefinition>();
        public List<SkillDefinition> Skills = new List<SkillDefinition>();
        public List<SpecialDefinition> Specials = new List<SpecialDefinition>();
        public List<EnemyDefinition> Enemies = new List<EnemyDefinition>();
        public List<NormalAugmentDefinition> NormalAugments = new List<NormalAugmentDefinition>();
        public List<SpecialAugmentDefinition> SpecialAugments = new List<SpecialAugmentDefinition>();
        [Header("Simulation rules")]
        public List<SpawnStageDefinition> SpawnStages = new List<SpawnStageDefinition>();
        public List<SpawnEntryDefinition> NormalSpawnPool = new List<SpawnEntryDefinition>();
        public List<SpawnEntryDefinition> EliteSpawnPool = new List<SpawnEntryDefinition>();
        public BalanceDefinition Balance = new BalanceDefinition();
        public CombatTuningDefinition Combat = new CombatTuningDefinition();
        public RainTuningDefinition Rain = new RainTuningDefinition();
        public RewardTuningDefinition Rewards = new RewardTuningDefinition();
        [Header("Run defaults")]
        public CharacterDefinition DefaultCharacter;
        public SpecialDefinition DefaultSpecialD;
        public SpecialDefinition DefaultSpecialF;
        public float EliteOffsetX = 380, EliteOffsetY = -200, EliteInset = 60;

        public GameContentSnapshot BuildSnapshot()
        {
            var errors = Validate();
            if (errors.Count > 0) throw new InvalidOperationException("Survival Legend GameDatabase is invalid:\n - " + string.Join("\n - ", errors));
            var characters = new Dictionary<string, CharacterData>(StringComparer.Ordinal); foreach (var item in Characters) characters.Add(item.Id, item.ToData());
            var skills = new Dictionary<string, SkillData>(StringComparer.Ordinal); foreach (var item in Skills) skills.Add(item.Id, item.ToData());
            var specials = new Dictionary<string, SpecialData>(StringComparer.Ordinal); foreach (var item in Specials) specials.Add(item.Id, item.ToData());
            var enemies = new Dictionary<string, EnemyData>(StringComparer.Ordinal); foreach (var item in Enemies) enemies.Add(item.Id, item.ToData());
            return new GameContentSnapshot(characters, skills, specials, enemies, ToStages(), ToNormalAugments(), ToSpecialAugments(),
                Balance.ToData(), Combat.ToData(), Rain.ToData(), Rewards.ToData(), ToSpawnEntries(NormalSpawnPool), ToSpawnEntries(EliteSpawnPool),
                EliteOffsetX, EliteOffsetY, EliteInset, DefaultCharacter.Id, DefaultSpecialD.Id, DefaultSpecialF.Id);
        }

        public List<string> Validate()
        {
            var errors = new List<string>();
            if (Characters == null || Skills == null || Specials == null || Enemies == null || NormalAugments == null || SpecialAugments == null || SpawnStages == null || NormalSpawnPool == null || EliteSpawnPool == null) { errors.Add("Every content registry and spawn list must be assigned."); return errors; }
            if (Balance == null || Combat == null || Rain == null || Rewards == null) { errors.Add("Balance, Combat, Rain, and Rewards tuning are all required."); return errors; }
            ValidateRegistry("character", Characters, x => x == null ? null : x.Id, errors); ValidateRegistry("skill", Skills, x => x == null ? null : x.Id, errors);
            ValidateRegistry("special", Specials, x => x == null ? null : x.Id, errors); ValidateRegistry("enemy", Enemies, x => x == null ? null : x.Id, errors);
            ValidateRegistry("normal augment", NormalAugments, x => x == null ? null : x.Id, errors); ValidateRegistry("special augment", SpecialAugments, x => x == null ? null : x.Id, errors);
            if (DefaultCharacter == null) errors.Add("Default Character is required."); else if (!Characters.Contains(DefaultCharacter)) errors.Add("Default Character is not in Characters.");
            if (DefaultSpecialD == null || !Specials.Contains(DefaultSpecialD)) errors.Add("Default Special D must reference an entry in Specials.");
            if (DefaultSpecialF == null || !Specials.Contains(DefaultSpecialF)) errors.Add("Default Special F must reference an entry in Specials.");
            if (DefaultSpecialD != null && DefaultSpecialD == DefaultSpecialF) errors.Add("Default Special D and F must be different assets.");
            foreach (var character in Characters) { if (character == null) continue; character.Validate(Skills, errors); }
            foreach (var character in Characters) { if (character == null || character.Stats == null) continue; if (character.BasicKind != "melee" && character.BasicKind != "projectile" && character.BasicKind != "chain") errors.Add("Character '"+character.Id+"' Basic Kind must be melee, projectile, or chain."); if (!Positive(character.Stats.Hp) || !NonNegative(character.Stats.Speed) || !Positive(character.Stats.AttackSpeed) || !NonNegative(character.Stats.Damage) || !NonNegative(character.Stats.Range)) errors.Add("Character '"+character.Id+"' needs finite HP/Attack Speed > 0 and finite non-negative base combat stats."); if(character.BasicKind=="chain" && (character.ChainRatios==null||character.ChainRatios.Length==0||!AllPositive(character.ChainRatios)))errors.Add("Chain character '"+character.Id+"' needs one or more finite positive Chain Ratios."); }
            foreach (var skill in Skills) { if (skill == null) continue; ValidateSkill(skill, errors); }
            foreach (var special in Specials) { if (special == null) continue; if (special.Kind != "heal" && special.Kind != "haste" && special.Kind != "shield" && special.Kind != "pull") errors.Add("Special '"+special.Id+"' Kind must be heal, haste, shield, or pull."); if (!Positive(special.Cooldown)) errors.Add("Special '"+special.Id+"' needs a finite cooldown > 0."); }
            foreach (var enemy in Enemies) { if (enemy == null) continue; enemy.Validate(Skills, errors); if (enemy.Category != "melee" && enemy.Category != "ranged") errors.Add("Enemy '"+enemy.Id+"' Category must be melee or ranged."); if (!Positive(enemy.Hp) || !Positive(enemy.Radius) || !NonNegative(enemy.Speed) || !NonNegative(enemy.Damage) || !NonNegative(enemy.AttackRange) || !NonNegative(enemy.Windup)) errors.Add("Enemy '"+enemy.Id+"' needs finite HP/radius > 0 and non-negative combat values."); }
            foreach (var augment in NormalAugments) if(augment!=null && augment.Operation != AugmentOperation.Add && augment.Operation != AugmentOperation.Percent) errors.Add("Normal augment '"+augment.Id+"' Operation must be Add or Percent.");
            foreach (var augment in SpecialAugments) { if (augment == null) continue; augment.Validate(Characters, Skills, errors); ValidateAugment(augment, errors); }
            ValidateSpawnPool("normal", NormalSpawnPool, Enemies, errors); ValidateSpawnPool("elite", EliteSpawnPool, Enemies, errors);
            ValidatePoolDisjointness(errors);
            var normalEligible=0; foreach(var item in NormalAugments) if(item!=null&&Positive(item.Weight)) normalEligible++; if(normalEligible==0) errors.Add("At least one Normal Augment needs Weight > 0 so level cards can resolve.");
            foreach(var character in Characters) if(character!=null) { var eligible=0; foreach(var item in SpecialAugments) if(item!=null&&Positive(item.Weight)&&(item.Character==null||item.Character==character))eligible++; if(eligible==0) errors.Add("Character '"+character.Id+"' needs one eligible Special Augment with Weight > 0 so elite cards can resolve."); }
            if (SpawnStages.Count == 0) errors.Add("At least one Spawn Stage is required.");
            SpawnStageDefinition previous=null; for (var i = 0; i < SpawnStages.Count; i++) { var stage=SpawnStages[i]; if (stage == null) { errors.Add("Spawn Stages has a missing entry at " + i + "."); continue; } if (!NonNegative(stage.Time) || !Positive(stage.Interval) || !NonNegative(stage.Batch) || stage.Cap < 1) errors.Add("Spawn Stage "+i+" needs finite Time/Batch >= 0, Interval > 0, and Cap >= 1."); if (previous != null && stage.Time <= previous.Time) errors.Add("Spawn Stages must have strictly increasing Time."); previous=stage; }
            if (!Positive(Balance.FixedStep) || Balance.FixedStep > .05f) errors.Add("Balance Fixed Step must be finite, > 0, and <= 0.05.");
            if (!Positive(Balance.ArenaWidth) || !Positive(Balance.ArenaHeight) || !NonNegative(Balance.ArenaInset) || Balance.ArenaWidth <= Balance.ArenaInset*2 || Balance.ArenaHeight <= Balance.ArenaInset*2) errors.Add("Arena Width/Height must exceed twice Arena Inset.");
            if (!Positive(Balance.DodgeDuration) || !Positive(Balance.DodgeRecharge)) errors.Add("Dodge Duration and Recharge must be finite and > 0.");
            if (!Positive(Balance.HastePointsPerDoubleRate) || !Positive(Balance.ArmorConstant) || Balance.EliteEveryLevels < 1) errors.Add("Haste Points Per Double Rate, Armor Constant, and Elite Every Levels must be positive.");
            if (!Positive(Combat.PlayerShotSpeed) || !Positive(Combat.PlayerShotLifetime) || !Positive(Combat.RecastDelay)) errors.Add("Combat player projectile speed/lifetime and recast delay must be finite and > 0.");
            if (Rain.Columns < 1 || !Positive(Rain.RowDelay) || !Positive(Rain.FallTime)) errors.Add("Rain Columns, Row Delay, and Fall Time must be positive.");
            if (!NonNegative(Rewards.SilverChance) || !NonNegative(Rewards.GoldChance) || Rewards.SilverChance+Rewards.GoldChance > 1 || Rewards.Choices < 1) errors.Add("Reward chances must be finite, non-negative, sum to at most one, and Choices must be at least one.");
            return errors;
        }

        static bool Finite(float value) { return !float.IsNaN(value) && !float.IsInfinity(value); }
        static bool Positive(float value) { return Finite(value) && value > 0; }
        static bool NonNegative(float value) { return Finite(value) && value >= 0; }
        static bool AllPositive(float[] values) { foreach(var value in values) if(!Positive(value)) return false; return true; }
        static void ValidateSkill(SkillDefinition skill, List<string> errors)
        { if (skill.Targeting == null) errors.Add("Skill '"+skill.Id+"' needs Targeting."); if (skill.Effects == null || skill.Effects.Length == 0) { errors.Add("Skill '"+skill.Id+"' needs at least one effect."); return; } if (!NonNegative(skill.Cooldown)) errors.Add("Skill '"+skill.Id+"' needs a finite non-negative cooldown."); foreach(var effect in skill.Effects) { if(effect==null) {errors.Add("Skill '"+skill.Id+"' has a missing effect.");continue;} if(effect.Kind==DeliveryKind.Projectile && !Positive(effect.Speed)) errors.Add("Projectile skill '"+skill.Id+"' needs Speed > 0."); if((effect.Kind==DeliveryKind.Zone||effect.Kind==DeliveryKind.Rain)&&!Positive(effect.Interval)) errors.Add("Zone/rain skill '"+skill.Id+"' needs Interval > 0."); if(effect.Kind==DeliveryKind.Zone&&!Positive(effect.Duration)) errors.Add("Zone skill '"+skill.Id+"' needs Duration > 0."); } }

        static void ValidateRegistry<T>(string label, IList<T> entries, Func<T, string> id, List<string> errors) where T : UnityEngine.Object
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < entries.Count; i++) { var entry = entries[i]; if (entry == null) { errors.Add(label + " registry contains a missing reference at " + i + "."); continue; } var value = id(entry); if (string.IsNullOrWhiteSpace(value)) errors.Add(label + " '" + entry.name + "' needs a stable Id."); else if (!seen.Add(value)) errors.Add("Duplicate " + label + " Id '" + value + "'."); }
        }
        static void ValidateSpawnPool(string label, IList<SpawnEntryDefinition> pool, IList<EnemyDefinition> enemies, List<string> errors)
        {
            if (pool.Count == 0) { errors.Add("The " + label + " spawn pool is empty."); return; }
            var fill = 0;
            foreach (var entry in pool) { if (entry == null) { errors.Add("The " + label + " spawn pool has a missing entry."); continue; } if (entry.Enemy == null || !enemies.Contains(entry.Enemy)) errors.Add("The " + label + " spawn entry needs an enemy from the Enemies registry."); if (entry.FillRemainder) fill++; if (!NonNegative(entry.Weight) || !NonNegative(entry.StartTime) || !NonNegative(entry.RampSeconds)) errors.Add("Spawn entry Weight, Start Time, and Ramp Seconds must be finite and non-negative."); }
            if (fill != 1) errors.Add("The " + label + " spawn pool needs exactly one Fill Remainder entry.");
        }
        void ValidatePoolDisjointness(List<string> errors)
        {
            var normal = new HashSet<EnemyDefinition>(); foreach(var entry in NormalSpawnPool) if(entry!=null&&entry.Enemy!=null) normal.Add(entry.Enemy);
            foreach(var entry in EliteSpawnPool) if(entry!=null&&entry.Enemy!=null&&normal.Contains(entry.Enemy)) errors.Add("Enemy '"+entry.Enemy.Id+"' is in both Normal and Elite Spawn Pools. Pools must be disjoint.");
        }
        static readonly HashSet<string> StatScopes = new HashSet<string>(StringComparer.Ordinal) { "damage", "armor", "speed", "attackSpeed", "maxHp", "range", "healthRegen", "lifesteal", "critChance", "critMultiplier", "eliteDamage", "chainTargets" };
        static readonly HashSet<string> HasteScopes = new HashSet<string>(StringComparer.Ordinal) { "all", "q", "w", "e" };
        static readonly HashSet<string> PatchPaths = new HashSet<string>(StringComparer.Ordinal) { "effects.0.nextAttack.charges", "effects.1.interval", "targeting.radius", "targeting.coreRadius", "effects.0.shape.radius", "effects.0.shape.coreRadius", "effects.0.modifiers.attackRange", "effects.0.radius", "targeting.width", "effects.0.waves", "effects.0.penetrations", "effects.0.modifiers.incomingDamage", "targeting.range" };
        static void ValidateAugment(SpecialAugmentDefinition augment, List<string> errors)
        {
            if(augment.Effects==null) return;
            foreach(var effect in augment.Effects) { if(effect==null) continue; var label="Special augment '"+augment.Id+"'";
                if(effect.Kind==AugmentEffectKind.Stat && (effect.Operation!=AugmentOperation.Add&&effect.Operation!=AugmentOperation.Percent || !StatScopes.Contains(effect.StatOrScope))) errors.Add(label+" Stat effect needs Add/Percent and a supported stat scope.");
                if(effect.Kind==AugmentEffectKind.Haste && (effect.Operation!=AugmentOperation.Add&&effect.Operation!=AugmentOperation.Percent || !HasteScopes.Contains(effect.StatOrScope))) errors.Add(label+" Haste effect needs Add/Percent and scope all, q, w, or e.");
                if(effect.Kind==AugmentEffectKind.Patch && (effect.Operation!=AugmentOperation.Add&&effect.Operation!=AugmentOperation.Multiply || !PatchPaths.Contains(effect.Path))) errors.Add(label+" Patch must use Add/Multiply and an interpreter-supported Path.");
                if(effect.Kind==AugmentEffectKind.Repeat && (effect.Operation!=AugmentOperation.Add || !Positive(effect.Value) || Math.Abs(effect.Value-Mathf.Round(effect.Value))>.0001f)) errors.Add(label+" Repeat needs an Add positive whole-number Value.");
                if(effect.Kind==AugmentEffectKind.AddEffect && effect.InjectedEffect==null) errors.Add(label+" Add Effect needs an injected effect.");
            }
        }
        SpawnStageData[] ToStages() { var output = new SpawnStageData[SpawnStages.Count]; for (var i = 0; i < output.Length; i++) output[i] = SpawnStages[i].ToData(); return output; }
        NormalAugmentData[] ToNormalAugments() { var output = new NormalAugmentData[NormalAugments.Count]; for (var i = 0; i < output.Length; i++) output[i] = NormalAugments[i].ToData(); return output; }
        AugmentData[] ToSpecialAugments() { var output = new AugmentData[SpecialAugments.Count]; for (var i = 0; i < output.Length; i++) output[i] = SpecialAugments[i].ToData(); return output; }
        static SpawnEntryData[] ToSpawnEntries(IList<SpawnEntryDefinition> source) { var output = new SpawnEntryData[source.Count]; for (var i = 0; i < output.Length; i++) output[i] = source[i].ToData(); return output; }
    }
}
