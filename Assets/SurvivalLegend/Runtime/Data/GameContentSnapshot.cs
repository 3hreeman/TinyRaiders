using System;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalLegend.Data
{
    /// <summary>One immutable run-content copy. Simulation may freely retain this object, but never authoring assets.</summary>
    public sealed class GameContentSnapshot
    {
        public readonly IReadOnlyDictionary<string, CharacterData> Characters;
        public readonly IReadOnlyDictionary<string, SkillData> Skills;
        public readonly IReadOnlyDictionary<string, SpecialData> Specials;
        public readonly IReadOnlyDictionary<string, EnemyData> Enemies;
        public readonly SpawnStageData[] SpawnStages;
        public readonly NormalAugmentData[] NormalAugments;
        public readonly AugmentData[] SpecialAugments;
        public readonly BalanceData Config;
        public readonly CombatTuningData Combat;
        public readonly RainTuningData Rain;
        public readonly RewardTuningData Rewards;
        public readonly SpawnEntryData[] NormalSpawnPool;
        public readonly SpawnEntryData[] EliteSpawnPool;
        public readonly float EliteOffsetX, EliteOffsetY, EliteInset;
        public readonly string DefaultCharacterId, DefaultSpecialDId, DefaultSpecialFId;
        public readonly Vector2[] SpawnPoints;
        public readonly IReadOnlyList<ObstacleData> Obstacles;

        public GameContentSnapshot(
            IDictionary<string, CharacterData> characters, IDictionary<string, SkillData> skills,
            IDictionary<string, SpecialData> specials, IDictionary<string, EnemyData> enemies,
            SpawnStageData[] spawnStages, NormalAugmentData[] normalAugments, AugmentData[] specialAugments,
            BalanceData config, CombatTuningData combat, RainTuningData rain, RewardTuningData rewards,
            SpawnEntryData[] normalSpawnPool, SpawnEntryData[] eliteSpawnPool,
            float eliteOffsetX, float eliteOffsetY, float eliteInset,
            string defaultCharacterId, string defaultSpecialDId, string defaultSpecialFId, Vector2[] spawnPoints = null, IEnumerable<ObstacleData> obstacles = null)
        {
            Characters = CopyCharacters(characters); Skills = CopySkills(skills); Specials = CopySpecials(specials); Enemies = CopyEnemies(enemies);
            SpawnStages = Copy(spawnStages); NormalAugments = Copy(normalAugments); SpecialAugments = CopyAugments(specialAugments);
            Config = config; Combat = combat; Rain = rain; Rewards = rewards;
            NormalSpawnPool = Copy(normalSpawnPool); EliteSpawnPool = Copy(eliteSpawnPool);
            EliteOffsetX = eliteOffsetX; EliteOffsetY = eliteOffsetY; EliteInset = eliteInset;
            DefaultCharacterId = defaultCharacterId ?? string.Empty; DefaultSpecialDId = defaultSpecialDId ?? string.Empty; DefaultSpecialFId = defaultSpecialFId ?? string.Empty;
            SpawnPoints = Copy(spawnPoints);
            var solids=obstacles==null?new List<ObstacleData>():new List<ObstacleData>(obstacles);
            solids.Sort((a,b)=>StringComparer.Ordinal.Compare(a.Id,b.Id));
            string previous=null;
            foreach(var solid in solids)
            {
                if(string.IsNullOrWhiteSpace(solid.Id)||solid.Id==previous)throw new ArgumentException("Obstacle IDs must be nonempty and unique.",nameof(obstacles));
                if(float.IsNaN(solid.Position.x)||float.IsInfinity(solid.Position.x)||float.IsNaN(solid.Position.y)||float.IsInfinity(solid.Position.y)||float.IsNaN(solid.Radius)||float.IsInfinity(solid.Radius)||solid.Radius<=0)
                    throw new ArgumentException("Obstacle '"+solid.Id+"' needs a finite position and radius > 0.",nameof(obstacles));
                previous=solid.Id;
            }
            Obstacles=solids.AsReadOnly();
        }

        public GameContentSnapshot Clone()
        {
            return new GameContentSnapshot(new Dictionary<string, CharacterData>(Characters), new Dictionary<string, SkillData>(Skills),
                new Dictionary<string, SpecialData>(Specials), new Dictionary<string, EnemyData>(Enemies), SpawnStages, NormalAugments, SpecialAugments,
                Config, Combat, Rain, Rewards, NormalSpawnPool, EliteSpawnPool, EliteOffsetX, EliteOffsetY, EliteInset,
                DefaultCharacterId, DefaultSpecialDId, DefaultSpecialFId, SpawnPoints, Obstacles);
        }

        public static GameContentSnapshot CreateDefault()
        {
            return new GameContentSnapshot(new Dictionary<string, CharacterData>(SurvivalLegendCatalog.Characters),
                new Dictionary<string, SkillData>(SurvivalLegendCatalog.Skills), new Dictionary<string, SpecialData>(SurvivalLegendCatalog.Specials),
                new Dictionary<string, EnemyData>(SurvivalLegendCatalog.Enemies), SurvivalLegendCatalog.SpawnStages,
                SurvivalLegendCatalog.NormalAugments, SurvivalLegendCatalog.SpecialAugments, SurvivalLegendCatalog.Config,
                SurvivalLegendCatalog.Combat, SurvivalLegendCatalog.Rain, SurvivalLegendCatalog.Rewards,
                new[] { new SpawnEntryData("chaser", 1f, 0, 0, true), new SpawnEntryData("shooter", .3f, 20, 100, false), new SpawnEntryData("caster", .2f, 60, 120, false) },
                new[] { new SpawnEntryData("elite", 1f, 0, 0, true) }, SurvivalLegendCatalog.EliteOffsetX, SurvivalLegendCatalog.EliteOffsetY,
                SurvivalLegendCatalog.EliteInset, "archer", "heal", "haste");
        }

        /// <summary>Returns a separate content instance whose authored arena geometry is consumed by simulation.</summary>
        public GameContentSnapshot WithArena(float width, float height, float inset, Vector2[] spawnPoints)
        {
            var b = Config;
            var arena = new BalanceData(b.FixedStep, width, height, inset, b.ProjectileMargin, b.PlayerRadius, b.HitInvulnerability, b.CritChance, b.CritMultiplier,
                b.DamageGrowth, b.SpeedGrowth, b.AttackSpeedGrowth, b.HpGrowth, b.EnemyHpGrowth, b.EnemyDamageGrowth, b.EnemySpeedGrowth, b.EnemyInitialDelay,
                b.EnemyDelayJitter, b.EnemyEdgeInset, b.EnemyCornerInset, b.EnemySlowMultiplier, b.SpawnInitialDelay, b.SpawnJitterMin, b.SpawnJitterWidth,
                b.ShooterChance, b.ShooterStart, b.ShooterRamp, b.CasterChance, b.CasterStart, b.CasterRamp, b.DodgeDistance, b.DodgeDuration, b.DodgeRecharge,
                b.DodgeCapacity, b.UltimateMaximum, b.UltimateChargePerHit, b.HastePointsPerDoubleRate, b.LevelUpSeconds, b.CardDealSeconds, b.EliteDeathSeconds,
                b.DeathSeconds, b.EliteInitialDelay, b.EliteAttackRange, b.EliteWindup, b.EliteCleaveWindup, b.EliteMotion, b.EliteEveryLevels, b.KillQuadratic,
                b.KillLinear, b.KillConstant, b.ArmorConstant);
            return new GameContentSnapshot(new Dictionary<string, CharacterData>(Characters), new Dictionary<string, SkillData>(Skills), new Dictionary<string, SpecialData>(Specials),
                new Dictionary<string, EnemyData>(Enemies), SpawnStages, NormalAugments, SpecialAugments, arena, Combat, Rain, Rewards, NormalSpawnPool, EliteSpawnPool,
                EliteOffsetX, EliteOffsetY, EliteInset, DefaultCharacterId, DefaultSpecialDId, DefaultSpecialFId, spawnPoints, Obstacles);
        }

        public GameContentSnapshot WithObstacles(IEnumerable<ObstacleData> obstacles)
        {
            return new GameContentSnapshot(new Dictionary<string,CharacterData>(Characters),new Dictionary<string,SkillData>(Skills),
                new Dictionary<string,SpecialData>(Specials),new Dictionary<string,EnemyData>(Enemies),SpawnStages,NormalAugments,SpecialAugments,
                Config,Combat,Rain,Rewards,NormalSpawnPool,EliteSpawnPool,EliteOffsetX,EliteOffsetY,EliteInset,
                DefaultCharacterId,DefaultSpecialDId,DefaultSpecialFId,SpawnPoints,obstacles);
        }

        public float KillsForNextLevel(int level) { var current = Math.Max(1, level); return Config.KillQuadratic * current * current + Config.KillLinear * current + Config.KillConstant; }
        public float CooldownRecovery(float flatHaste, float percentHaste = 0) { return 1 + flatHaste * (1 + percentHaste) / Config.HastePointsPerDoubleRate; }
        public float MitigatedDamage(float damage, float armor) { return Math.Max(0, damage) * Config.ArmorConstant / (Config.ArmorConstant + Math.Max(0, armor)); }

        static T[] Copy<T>(T[] source) { return source == null ? Array.Empty<T>() : (T[])source.Clone(); }
        static IReadOnlyDictionary<string, CharacterData> CopyCharacters(IDictionary<string, CharacterData> source)
        {
            var copy = new Dictionary<string, CharacterData>(StringComparer.Ordinal);
            foreach (var pair in source) { var d = pair.Value; copy[pair.Key] = new CharacterData(d.Id, d.Name, d.Subtitle, d.Traits, d.Color, d.BasicKind, d.Stats, Copy(d.Skills), d.AnimationStride, d.AttackDuration, d.BasicWeapon, Copy(d.ChainRatios), d.ChainRange); }
            return copy;
        }
        static IReadOnlyDictionary<string, SkillData> CopySkills(IDictionary<string, SkillData> source)
        {
            var copy = new Dictionary<string, SkillData>(StringComparer.Ordinal);
            foreach (var pair in source) { var d = pair.Value; copy[pair.Key] = new SkillData(d.Id, d.Name, d.Icon, d.Color, d.Cooldown, d.Ultimate, d.Description, d.Targeting, d.Animation, Copy(d.Effects)); }
            return copy;
        }
        static IReadOnlyDictionary<string, SpecialData> CopySpecials(IDictionary<string, SpecialData> source) { return new Dictionary<string, SpecialData>(source, StringComparer.Ordinal); }
        static IReadOnlyDictionary<string, EnemyData> CopyEnemies(IDictionary<string, EnemyData> source)
        {
            var copy = new Dictionary<string, EnemyData>(StringComparer.Ordinal);
            foreach (var pair in source) { var d = pair.Value; copy[pair.Key] = new EnemyData(d.Id, d.Name, d.Category, d.Hp, d.Speed, d.Radius, d.Damage, d.AttackRange, d.Windup, Copy(d.Patterns)); }
            return copy;
        }
        static AugmentData[] CopyAugments(AugmentData[] source)
        {
            if (source == null) return Array.Empty<AugmentData>();
            var copy = new AugmentData[source.Length];
            for (var i = 0; i < source.Length; i++) { var d = source[i]; copy[i] = new AugmentData(d.Id, d.Title, d.Description, d.Icon, d.Weight, d.CharacterId, Copy(d.Effects)); }
            return copy;
        }
    }

    [Serializable]
    public readonly struct SpawnEntryData
    {
        public readonly string EnemyId;
        public readonly float Weight, StartTime, RampSeconds;
        public readonly bool FillRemainder;
        public SpawnEntryData(string enemyId, float weight, float startTime, float rampSeconds, bool fillRemainder)
        { EnemyId = enemyId; Weight = weight; StartTime = startTime; RampSeconds = rampSeconds; FillRemainder = fillRemainder; }
        public float ChanceAt(float time) { return FillRemainder ? 0 : Weight * (RampSeconds <= 0 ? (time >= StartTime ? 1 : 0) : Math.Min(1, Math.Max(0, (time - StartTime) / RampSeconds))); }
    }
}
