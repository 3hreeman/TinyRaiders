using System;
using System.Collections.Generic;

namespace SurvivalLegend.Data
{
    // Source of truth for the first playable slice. All values map to the current
    // web Survivor runtime (not the older design workbook). It intentionally has
    // no MonoBehaviour, ScriptableObject, or presentation dependencies.
    public enum TargetKind { Self, Direction, Point }
    public enum DeliveryKind { Buff, Projectile, Rain, Area, Zone, Shield, Teleport }
    public enum ShapeKind { None, Circle, Rectangle }
    public enum AugmentEffectKind { Stat, Haste, Patch, Repeat, AddEffect }
    public enum AugmentOperation { Add, Percent, Multiply }

    [Serializable]
    public readonly struct StatBlock
    {
        public readonly float Hp, Speed, AttackSpeed, Damage, Range, HealthRegen, Lifesteal, Armor;
        public StatBlock(float hp, float speed, float attackSpeed, float damage, float range, float healthRegen, float lifesteal, float armor)
        { Hp = hp; Speed = speed; AttackSpeed = attackSpeed; Damage = damage; Range = range; HealthRegen = healthRegen; Lifesteal = lifesteal; Armor = armor; }
    }

    [Serializable]
    public readonly struct CharacterData
    {
        public readonly string Id, Name, Subtitle, Traits, Color, BasicKind, BasicWeapon;
        public readonly StatBlock Stats;
        public readonly string[] Skills;
        public readonly float AnimationStride, AttackDuration, ChainRange;
        public readonly float[] ChainRatios;
        public CharacterData(string id, string name, string subtitle, string traits, string color, string basicKind, StatBlock stats, string[] skills, float stride, float attackDuration, string basicWeapon = "", float[] chainRatios = null, float chainRange = 0)
        { Id = id; Name = name; Subtitle = subtitle; Traits = traits; Color = color; BasicKind = basicKind; Stats = stats; Skills = skills; AnimationStride = stride; AttackDuration = attackDuration; BasicWeapon = basicWeapon; ChainRatios = chainRatios ?? Array.Empty<float>(); ChainRange = chainRange; }
    }

    [Serializable]
    public readonly struct TargetingData
    {
        public readonly TargetKind Kind;
        public readonly float Range, Radius, Width, Spread, CoreRadius;
        public TargetingData(TargetKind kind, float range = 0, float radius = 0, float width = 0, float spread = 0, float coreRadius = 0)
        { Kind = kind; Range = range; Radius = radius; Width = width; Spread = spread; CoreRadius = coreRadius; }
    }

    [Serializable]
    public readonly struct EffectData
    {
        public readonly DeliveryKind Kind;
        public readonly float DamageMultiplier, Duration, Interval, Delay, Speed, Range, Radius, Count, Waves, Spread, Length, Width;
        public readonly bool Pierce, FollowCaster;
        public readonly float AttackSpeedMultiplier, AttackRangeMultiplier, ExtraTargets;
        public readonly ShapeKind Shape;
        public readonly float CoreRadius, CoreMultiplier, RootSeconds, ExplosionRadius, Penetrations, MaxHealthRatio, Reflect;
        public readonly bool UntilHit;
        public readonly float NextAttackMultiplier, NextAttackSplash, NextAttackRadius, NextAttackCharges;
        public readonly float BuffDamageMultiplier, MoveSpeedMultiplier, CooldownRateMultiplier, IncomingDamageMultiplier;
        public EffectData(DeliveryKind kind, float damageMultiplier = 0, float duration = 0, float interval = 0, float delay = 0,
            float speed = 0, float range = 0, float radius = 0, float count = 0, float waves = 0, float spread = 0,
            float length = 0, float width = 0, bool pierce = false, bool followCaster = false,
            float attackSpeedMultiplier = 0, float attackRangeMultiplier = 0, float extraTargets = 0, ShapeKind shape = ShapeKind.None,
            float coreRadius = 0, float coreMultiplier = 0, float rootSeconds = 0, float explosionRadius = 0, float penetrations = 0,
            float maxHealthRatio = 0, float reflect = 0, bool untilHit = false, float nextAttackMultiplier = 0, float nextAttackSplash = 0,
            float nextAttackRadius = 0, float nextAttackCharges = 0, float buffDamageMultiplier = 0, float moveSpeedMultiplier = 0,
            float cooldownRateMultiplier = 0, float incomingDamageMultiplier = 0)
        { Kind = kind; DamageMultiplier = damageMultiplier; Duration = duration; Interval = interval; Delay = delay; Speed = speed; Range = range; Radius = radius; Count = count; Waves = waves; Spread = spread; Length = length; Width = width; Pierce = pierce; FollowCaster = followCaster; AttackSpeedMultiplier = attackSpeedMultiplier; AttackRangeMultiplier = attackRangeMultiplier; ExtraTargets = extraTargets; Shape = shape; CoreRadius = coreRadius; CoreMultiplier = coreMultiplier; RootSeconds = rootSeconds; ExplosionRadius = explosionRadius; Penetrations = penetrations; MaxHealthRatio = maxHealthRatio; Reflect = reflect; UntilHit = untilHit; NextAttackMultiplier = nextAttackMultiplier; NextAttackSplash = nextAttackSplash; NextAttackRadius = nextAttackRadius; NextAttackCharges = nextAttackCharges; BuffDamageMultiplier = buffDamageMultiplier; MoveSpeedMultiplier = moveSpeedMultiplier; CooldownRateMultiplier = cooldownRateMultiplier; IncomingDamageMultiplier = incomingDamageMultiplier; }
    }

    [Serializable]
    public readonly struct SkillData
    {
        public readonly string Id, Name, Icon, Color, Description, Animation;
        public readonly float Cooldown;
        public readonly bool Ultimate;
        public readonly TargetingData Targeting;
        public readonly EffectData[] Effects;
        public SkillData(string id, string name, string icon, string color, float cooldown, bool ultimate, string description, TargetingData targeting, string animation, params EffectData[] effects)
        { Id = id; Name = name; Icon = icon; Color = color; Cooldown = cooldown; Ultimate = ultimate; Description = description; Targeting = targeting; Animation = animation; Effects = effects; }
    }

    [Serializable]
    public readonly struct SpecialData
    {
        public readonly string Id, Name, Icon, Color, Description, Kind;
        public readonly float Cooldown, Duration, MissingHealthRatio, SpeedMultiplier, MaxHealthRatio, Range, Radius, PullSpeed;
        public SpecialData(string id, string name, string icon, string color, float cooldown, string description, string kind,
            float duration = 0, float missingHealthRatio = 0, float speedMultiplier = 0, float maxHealthRatio = 0, float range = 0, float radius = 0, float pullSpeed = 0)
        { Id = id; Name = name; Icon = icon; Color = color; Cooldown = cooldown; Description = description; Kind = kind; Duration = duration; MissingHealthRatio = missingHealthRatio; SpeedMultiplier = speedMultiplier; MaxHealthRatio = maxHealthRatio; Range = range; Radius = radius; PullSpeed = pullSpeed; }
    }

    [Serializable]
    public readonly struct EnemyData
    {
        public readonly string Id, Name, Category;
        public readonly float Hp, Speed, Radius, Damage, AttackRange, Windup;
        public readonly string[] Patterns;
        public EnemyData(string id, string name, string category, float hp, float speed, float radius, float damage, float attackRange, float windup, params string[] patterns)
        { Id = id; Name = name; Category = category; Hp = hp; Speed = speed; Radius = radius; Damage = damage; AttackRange = attackRange; Windup = windup; Patterns = patterns; }
    }

    [Serializable]
    public readonly struct SpawnStageData
    {
        public readonly float Time, Interval, Batch;
        public readonly int Cap;
        public SpawnStageData(float time, float interval, float batch, int cap) { Time = time; Interval = interval; Batch = batch; Cap = cap; }
    }

    [Serializable]
    public readonly struct AugmentEffectData
    {
        public readonly AugmentEffectKind Kind;
        public readonly string StatOrScope, SkillId, Path;
        public readonly AugmentOperation Operation;
        public readonly float Value;
        public readonly EffectData InjectedEffect;
        public AugmentEffectData(AugmentEffectKind kind, string statOrScope, AugmentOperation operation, float value, string skillId = "", string path = "", EffectData injectedEffect = default)
        { Kind = kind; StatOrScope = statOrScope; Operation = operation; Value = value; SkillId = skillId; Path = path; InjectedEffect = injectedEffect; }
    }

    [Serializable]
    public readonly struct AugmentData
    {
        public readonly string Id, Title, Description, Icon, CharacterId;
        public readonly float Weight;
        public readonly AugmentEffectData[] Effects;
        public AugmentData(string id, string title, string description, string icon, float weight, string characterId, params AugmentEffectData[] effects)
        { Id = id; Title = title; Description = description; Icon = icon; Weight = weight; CharacterId = characterId; Effects = effects; }
    }

    [Serializable]
    public readonly struct NormalAugmentData
    {
        public readonly string Id, Title, Icon, StatOrScope;
        public readonly AugmentOperation Operation;
        public readonly float Silver, Gold, Prism, Weight;
        public NormalAugmentData(string id, string title, string icon, string statOrScope, AugmentOperation operation, float silver, float gold, float prism, float weight = 1)
        { Id = id; Title = title; Icon = icon; StatOrScope = statOrScope; Operation = operation; Silver = silver; Gold = gold; Prism = prism; Weight = weight; }
    }

    [Serializable]
    public readonly struct BalanceData
    {
        public readonly float FixedStep, ArenaWidth, ArenaHeight, ArenaInset, ProjectileMargin;
        public readonly float PlayerRadius, HitInvulnerability, CritChance, CritMultiplier, DamageGrowth, SpeedGrowth, AttackSpeedGrowth, HpGrowth;
        public readonly float EnemyHpGrowth, EnemyDamageGrowth, EnemySpeedGrowth, EnemyInitialDelay, EnemyDelayJitter, EnemyEdgeInset, EnemyCornerInset, EnemySlowMultiplier;
        public readonly float SpawnInitialDelay, SpawnJitterMin, SpawnJitterWidth, ShooterChance, ShooterStart, ShooterRamp, CasterChance, CasterStart, CasterRamp;
        public readonly float DodgeDistance, DodgeDuration, DodgeRecharge; public readonly int DodgeCapacity;
        public readonly float UltimateMaximum, UltimateChargePerHit, HastePointsPerDoubleRate, LevelUpSeconds, CardDealSeconds, EliteDeathSeconds, DeathSeconds;
        public readonly float EliteInitialDelay, EliteAttackRange, EliteWindup, EliteCleaveWindup, EliteMotion; public readonly int EliteEveryLevels;
        public readonly float KillQuadratic, KillLinear, KillConstant, ArmorConstant;
        public BalanceData(float fixedStep, float arenaWidth, float arenaHeight, float arenaInset, float projectileMargin,
            float playerRadius, float hitInvulnerability, float critChance, float critMultiplier, float damageGrowth, float speedGrowth, float attackSpeedGrowth, float hpGrowth,
            float enemyHpGrowth, float enemyDamageGrowth, float enemySpeedGrowth, float enemyInitialDelay, float enemyDelayJitter, float enemyEdgeInset, float enemyCornerInset, float enemySlowMultiplier,
            float spawnInitialDelay, float spawnJitterMin, float spawnJitterWidth, float shooterChance, float shooterStart, float shooterRamp, float casterChance, float casterStart, float casterRamp,
            float dodgeDistance, float dodgeDuration, float dodgeRecharge, int dodgeCapacity, float ultimateMaximum, float ultimateChargePerHit, float hastePointsPerDoubleRate,
            float levelUpSeconds, float cardDealSeconds, float eliteDeathSeconds, float deathSeconds, float eliteInitialDelay, float eliteAttackRange, float eliteWindup, float eliteCleaveWindup, float eliteMotion, int eliteEveryLevels,
            float killQuadratic, float killLinear, float killConstant, float armorConstant)
        { FixedStep = fixedStep; ArenaWidth = arenaWidth; ArenaHeight = arenaHeight; ArenaInset = arenaInset; ProjectileMargin = projectileMargin; PlayerRadius = playerRadius; HitInvulnerability = hitInvulnerability; CritChance = critChance; CritMultiplier = critMultiplier; DamageGrowth = damageGrowth; SpeedGrowth = speedGrowth; AttackSpeedGrowth = attackSpeedGrowth; HpGrowth = hpGrowth; EnemyHpGrowth = enemyHpGrowth; EnemyDamageGrowth = enemyDamageGrowth; EnemySpeedGrowth = enemySpeedGrowth; EnemyInitialDelay = enemyInitialDelay; EnemyDelayJitter = enemyDelayJitter; EnemyEdgeInset = enemyEdgeInset; EnemyCornerInset = enemyCornerInset; EnemySlowMultiplier = enemySlowMultiplier; SpawnInitialDelay = spawnInitialDelay; SpawnJitterMin = spawnJitterMin; SpawnJitterWidth = spawnJitterWidth; ShooterChance = shooterChance; ShooterStart = shooterStart; ShooterRamp = shooterRamp; CasterChance = casterChance; CasterStart = casterStart; CasterRamp = casterRamp; DodgeDistance = dodgeDistance; DodgeDuration = dodgeDuration; DodgeRecharge = dodgeRecharge; DodgeCapacity = dodgeCapacity; UltimateMaximum = ultimateMaximum; UltimateChargePerHit = ultimateChargePerHit; HastePointsPerDoubleRate = hastePointsPerDoubleRate; LevelUpSeconds = levelUpSeconds; CardDealSeconds = cardDealSeconds; EliteDeathSeconds = eliteDeathSeconds; DeathSeconds = deathSeconds; EliteInitialDelay = eliteInitialDelay; EliteAttackRange = eliteAttackRange; EliteWindup = eliteWindup; EliteCleaveWindup = eliteCleaveWindup; EliteMotion = eliteMotion; EliteEveryLevels = eliteEveryLevels; KillQuadratic = killQuadratic; KillLinear = killLinear; KillConstant = killConstant; ArmorConstant = armorConstant; }
    }

    [Serializable]
    public readonly struct CombatTuningData
    {
        public readonly float Sight, PlayerShotSpeed, PlayerShotRadius, PlayerShotLifetime, HostileShotSpeed, HostileSpeedCap, HostileSpeedGrowth, HostileShotLifetime;
        public readonly float ChainDelay, ChainRange, ExtraChainRatio, RecastDelay, ZoneInterval;
        public CombatTuningData(float sight, float playerShotSpeed, float playerShotRadius, float playerShotLifetime, float hostileShotSpeed, float hostileSpeedCap, float hostileSpeedGrowth, float hostileShotLifetime, float chainDelay, float chainRange, float extraChainRatio, float recastDelay, float zoneInterval)
        { Sight = sight; PlayerShotSpeed = playerShotSpeed; PlayerShotRadius = playerShotRadius; PlayerShotLifetime = playerShotLifetime; HostileShotSpeed = hostileShotSpeed; HostileSpeedCap = hostileSpeedCap; HostileSpeedGrowth = hostileSpeedGrowth; HostileShotLifetime = hostileShotLifetime; ChainDelay = chainDelay; ChainRange = chainRange; ExtraChainRatio = extraChainRatio; RecastDelay = recastDelay; ZoneInterval = zoneInterval; }
    }

    [Serializable]
    public readonly struct RainTuningData
    {
        public readonly int Columns;
        public readonly float JitterStart, JitterWidth, RowDelay, FallTime, HitRadius;
        public RainTuningData(int columns, float jitterStart, float jitterWidth, float rowDelay, float fallTime, float hitRadius)
        { Columns = columns; JitterStart = jitterStart; JitterWidth = jitterWidth; RowDelay = rowDelay; FallTime = fallTime; HitRadius = hitRadius; }
    }

    [Serializable]
    public readonly struct RewardTuningData
    {
        public readonly float SilverChance, GoldChance, RerollPromotion;
        public readonly int Choices;
        public RewardTuningData(float silverChance, float goldChance, float rerollPromotion, int choices) { SilverChance = silverChance; GoldChance = goldChance; RerollPromotion = rerollPromotion; Choices = choices; }
    }

    public static class SurvivalLegendCatalog
    {
        public static readonly BalanceData Config = new BalanceData(
            1f / 60f, 1440, 1440, 24, 30, 15, .55f, .05f, 1.5f, .08f, .018f, .025f, .06f,
            .045f, .035f, .009f, 1.5f, 1.5f, 18, 28, .35f,
            1.2f, .8f, .4f, .3f, 20, 100, .2f, 60, 120,
            180, .2f, 10, 2, 100, 5, 100, .85f, .65f, 2.4f, 1.2f, 2, 520, 1.7f, 1.4f, .65f, 5, .5f, 1.5f, 1, 100);

        public static readonly CombatTuningData Combat = new CombatTuningData(480, 620, 5, 1.6f, 205, 100, .3f, 7, .14f, 160, .5f, .18f, .5f);
        public static readonly RainTuningData Rain = new RainTuningData(5, .25f, .5f, .075f, .45f, 18);
        public static readonly RewardTuningData Rewards = new RewardTuningData(.55f, .35f, .15f, 3);
        public const float EliteOffsetX = 380;
        public const float EliteOffsetY = -200;
        public const float EliteInset = 60;

        public static readonly CharacterData Swordsman = new CharacterData(
            "swordsman", "검사", "대검으로 버티는 전선", "체력 높음 · 이동 느림 · 공격 보통", "#f5ce87", "melee",
            new StatBlock(180, 164, 1.3f, 25.6f, 112, 5, 0, 15), new[] { "sword-q", "sword-w", "sword-e", "sword-r" }, 10, .38f, "greatsword");
        public static readonly CharacterData Archer = new CharacterData(
            "archer", "궁수", "거리를 지배하는 화살", "체력 보통 · 이동 빠름 · 공격 빠름", "#b3f783", "projectile",
            new StatBlock(120, 216, 1.7f, 17.6f, 300, 3, 0, 8), new[] { "bow-q", "bow-w", "bow-e", "bow-r" }, 17, .24f, "bow");
        public static readonly CharacterData Mage = new CharacterData(
            "mage", "마법사", "번개와 원소의 연쇄", "체력 낮음 · 이동 보통 · 공격 느림", "#c5a0ff", "chain",
            new StatBlock(90, 188, .72f, 22.4f, 330, 3, 0, 0), new[] { "magic-q", "magic-w", "magic-e", "magic-r" }, 12, .45f, "staff", new[] { 1f, .75f, .5f }, 160);
        public static readonly IReadOnlyDictionary<string, CharacterData> Characters = new Dictionary<string, CharacterData>(StringComparer.Ordinal)
        {
            [Swordsman.Id] = Swordsman, [Archer.Id] = Archer, [Mage.Id] = Mage
        };

        public static readonly IReadOnlyDictionary<string, SkillData> Skills = new Dictionary<string, SkillData>(StringComparer.Ordinal)
        {
            ["sword-q"] = new SkillData("sword-q", "강화 일격", "✦", "#f5ce87", 6, false, "다음 기본 공격 200%. 대상 주변에 공격력 75% 피해.", new TargetingData(TargetKind.Self, radius: 112), "empower", new EffectData(DeliveryKind.Buff, untilHit: true, nextAttackMultiplier: 2, nextAttackSplash: .75f, nextAttackRadius: 110, nextAttackCharges: 1)),
            ["sword-w"] = new SkillData("sword-w", "반격의 방벽", "⬡", "#8cceff", 12, false, "3초간 최대 체력 20% 보호막. 받은 공격 피해의 30%를 공격자에게 반사.", new TargetingData(TargetKind.Self, radius: 65), "guard", new EffectData(DeliveryKind.Shield, duration: 3, maxHealthRatio: .2f, reflect: .3f)),
            ["sword-e"] = new SkillData("sword-e", "회전 공격", "⟳", "#f7dfac", 12, false, "5초간 0.25초마다 주변에 공격력 75% 피해. 이동 속도 +15%.", new TargetingData(TargetKind.Self, radius: 145), "spin",
                new EffectData(DeliveryKind.Buff, duration: 5, moveSpeedMultiplier: 1.15f),
                new EffectData(DeliveryKind.Zone, .75f, duration: 5, interval: .25f, radius: 145, followCaster: true, shape: ShapeKind.Circle)),
            ["sword-r"] = new SkillData("sword-r", "심판의 대검", "⚔", "#ffe1a0", 0, true, "지정 위치에 거대한 검을 떨어뜨려 공격력 1000%, 중심에는 2000% 피해.", new TargetingData(TargetKind.Point, range: 600, radius: 180, coreRadius: 60), "slam", new EffectData(DeliveryKind.Zone, 10, duration: .05f, interval: .5f, delay: .45f, radius: 180, shape: ShapeKind.Circle, coreRadius: 60, coreMultiplier: 2)),

            ["bow-q"] = new SkillData("bow-q", "집중 사격", "»", "#b3f783", 11, false, "5초간 공격 속도 +50%. 주 대상 외 주변 두 대상에도 화살 발사.", new TargetingData(TargetKind.Self, radius: 300), "focus", new EffectData(DeliveryKind.Buff, duration: 5, attackSpeedMultiplier: 1.5f, attackRangeMultiplier: 1, extraTargets: 2)),
            ["bow-w"] = new SkillData("bow-w", "다발 화살", "⋔", "#d5ed9b", 7, false, "45도 부채꼴로 화살 7발 발사. 화살마다 공격력 75% 피해.", new TargetingData(TargetKind.Direction, range: 600, width: 6, spread: 45), "volley", new EffectData(DeliveryKind.Projectile, .75f, speed: 700, range: 600, radius: 6, count: 7, spread: 45)),
            ["bow-e"] = new SkillData("bow-e", "관통 화살", "➶", "#73ddff", 8, false, "일직선의 모든 적을 관통하며 공격력 150% 피해.", new TargetingData(TargetKind.Direction, range: 850, width: 12), "pierce", new EffectData(DeliveryKind.Projectile, 1.5f, speed: 760, range: 850, radius: 12, pierce: true)),
            ["bow-r"] = new SkillData("bow-r", "화살비", "⇣", "#d8faaa", 0, true, "가까운 곳부터 먼 곳까지 화살비 50발을 2회 발사. 화살마다 적중 시 공격력 50% 피해.", new TargetingData(TargetKind.Direction, range: 580, width: 180), "rain", new EffectData(DeliveryKind.Rain, .5f, interval: 1.1f, count: 50, waves: 2, length: 580, width: 360)),

            ["magic-q"] = new SkillData("magic-q", "폭발 화염구", "☄", "#ffac79", 6, false, "처음 맞은 적과 주변 적에게 공격력 150% 피해.", new TargetingData(TargetKind.Direction, range: 700, width: 13), "fireball", new EffectData(DeliveryKind.Projectile, 1.5f, speed: 480, range: 700, radius: 13, penetrations: 0, explosionRadius: 110)),
            ["magic-w"] = new SkillData("magic-w", "서리 결계", "❋", "#9adfff", 10, false, "주변 적에게 공격력 100% 피해를 주고 1초간 이동 불가.", new TargetingData(TargetKind.Self, radius: 190), "frost", new EffectData(DeliveryKind.Area, 1, radius: 190, shape: ShapeKind.Circle, rootSeconds: 1)),
            ["magic-e"] = new SkillData("magic-e", "비전 도약", "◇", "#c5a0ff", 9, false, "커서 방향 최대 280 거리로 순간이동. 도착 지점 주변에 공격력 100% 피해.", new TargetingData(TargetKind.Point, range: 280, radius: 120), "blink", new EffectData(DeliveryKind.Teleport), new EffectData(DeliveryKind.Area, 1, radius: 120, shape: ShapeKind.Circle)),
            ["magic-r"] = new SkillData("magic-r", "비전 과부하", "✺", "#e6c6ff", 0, true, "10초간 모든 스킬 쿨타임 회복 속도 6배, 주는 피해 2배.", new TargetingData(TargetKind.Self, radius: 90), "overload", new EffectData(DeliveryKind.Buff, duration: 10, buffDamageMultiplier: 2, cooldownRateMultiplier: 6, incomingDamageMultiplier: 1)),

            ["strike"] = new SkillData("strike", "근접 타격", "", "#ff7180", 1, false, "근거리 공격", new TargetingData(TargetKind.Self, radius: 30), "swing", new EffectData(DeliveryKind.Area, 1, radius: 30)),
            ["shot"] = new SkillData("shot", "조준탄", "", "#ff7180", 2.8f, false, "사거리 안에서 조준 후 투사체 발사", new TargetingData(TargetKind.Direction, range: 650, width: 7), "cast", new EffectData(DeliveryKind.Projectile, 1, speed: 230, range: 650, radius: 7)),
            ["hazard"] = new SkillData("hazard", "위험 지대", "", "#ff7180", 4.6f, false, "예고 후 폭발하는 장판", new TargetingData(TargetKind.Point, range: 420, radius: 74), "cast", new EffectData(DeliveryKind.Zone, 1, duration: 1.6f, interval: .5f, delay: 1.3f, radius: 74)),
            ["elite-cleave"] = new SkillData("elite-cleave", "파멸의 내려찍기", "⚔", "#ff8b52", 2.5f, false, "긴 준비 후 주변 강타", new TargetingData(TargetKind.Self, radius: 185), "slam", new EffectData(DeliveryKind.Area, 1.8f, radius: 185)),
            ["elite-barrage"] = new SkillData("elite-barrage", "분열의 포효", "✹", "#ff668d", 3, false, "예고 후 부채꼴 탄막", new TargetingData(TargetKind.Direction, range: 850, width: 12, spread: 110), "cast", new EffectData(DeliveryKind.Projectile, 1.2f, speed: 260, range: 850, radius: 12, count: 9, spread: 110)),
            ["elite-rupture"] = new SkillData("elite-rupture", "대지 붕괴", "◎", "#ffb25c", 3.5f, false, "지정 위치 붕괴", new TargetingData(TargetKind.Point, range: 600, radius: 170), "slam", new EffectData(DeliveryKind.Zone, 2.2f, duration: .05f, interval: 1, delay: 1.2f, radius: 170))
        };

        public static readonly IReadOnlyDictionary<string, SpecialData> Specials = new Dictionary<string, SpecialData>(StringComparer.Ordinal)
        {
            ["heal"] = new SpecialData("heal", "회복", "♡", "#b3f783", 90, "즉시 잃은 체력의 20% 회복 · 90초", "heal", missingHealthRatio: .2f),
            ["haste"] = new SpecialData("haste", "가속", "»", "#ffe49d", 60, "5초간 이동 속도 +50% · 60초", "haste", duration: 5, speedMultiplier: 1.5f),
            ["barrier"] = new SpecialData("barrier", "보호막", "⬡", "#8cceff", 45, "최대 체력 25% 보호막 · 5초 유지 · 45초", "shield", duration: 5, maxHealthRatio: .25f),
            ["blackhole"] = new SpecialData("blackhole", "블랙홀", "◉", "#c5a0ff", 90, "지정 위치 주변의 적을 중심으로 3초간 흡입 · 90초", "pull", duration: 3, range: 450, radius: 180, pullSpeed: 240)
        };

        public static readonly IReadOnlyDictionary<string, EnemyData> Enemies = new Dictionary<string, EnemyData>(StringComparer.Ordinal)
        {
            ["chaser"] = new EnemyData("chaser", "추격자", "melee", 34, 49, 15, 10, 28, .1f, "strike"),
            ["shooter"] = new EnemyData("shooter", "사수", "ranged", 48, 32, 18, 13, 340, .65f, "shot"),
            ["caster"] = new EnemyData("caster", "주술사", "ranged", 48, 32, 18, 18, 420, .3f, "hazard"),
            ["elite"] = new EnemyData("elite", "파멸의 집행자", "melee", 1400, 90, 38, 28, 520, 1.7f, "elite-cleave", "elite-barrage", "elite-rupture")
        };

        public static readonly SpawnStageData[] SpawnStages =
        {
            new SpawnStageData(0, 2.4f, 1, 16), new SpawnStageData(30, 1.9f, 1.25f, 28), new SpawnStageData(60, 1.5f, 1.75f, 45),
            new SpawnStageData(120, 1.15f, 2.5f, 75), new SpawnStageData(180, .95f, 3.25f, 110), new SpawnStageData(300, .8f, 4, 160)
        };

        public static readonly NormalAugmentData[] NormalAugments =
        {
            new NormalAugmentData("attack-flat", "공격력", "⚔", "damage", AugmentOperation.Add, 5, 10, 20),
            new NormalAugmentData("armor-flat", "방어력", "⬡", "armor", AugmentOperation.Add, 5, 10, 20),
            new NormalAugmentData("attack-percent", "공격력 증폭", "⚔", "damage", AugmentOperation.Percent, .05f, .075f, .1f),
            new NormalAugmentData("armor-percent", "방어력 증폭", "⬡", "armor", AugmentOperation.Percent, .05f, .075f, .1f),
            new NormalAugmentData("stride", "이동 속도", "➶", "speed", AugmentOperation.Percent, .05f, .075f, .1f),
            new NormalAugmentData("speed", "공격 속도", "»", "attackSpeed", AugmentOperation.Percent, .05f, .075f, .1f),
            new NormalAugmentData("critical", "치명타 확률", "✧", "critChance", AugmentOperation.Add, .02f, .03f, .05f),
            new NormalAugmentData("haste-all", "균형 잡힌 가속", "◷", "all", AugmentOperation.Add, 5, 10, 20),
            new NormalAugmentData("haste-q", "Q 집중 가속", "◷", "q", AugmentOperation.Add, 12, 24, 48),
            new NormalAugmentData("haste-w", "W 집중 가속", "◷", "w", AugmentOperation.Add, 12, 24, 48),
            new NormalAugmentData("haste-e", "E 집중 가속", "◷", "e", AugmentOperation.Add, 12, 24, 48),
            new NormalAugmentData("health-regen", "체력 회복", "♥", "healthRegen", AugmentOperation.Add, 1, 2, 4),
            new NormalAugmentData("lifesteal", "흡혈", "♥", "lifesteal", AugmentOperation.Add, .02f, .03f, .05f)
        };

        // The source pool has five common, each character's own four/five cards,
        // and four globally applicable haste amplifiers. CharacterId empty means
        // available to every roster member.
        public static readonly AugmentData[] SpecialAugments =
        {
            new AugmentData("common-speed", "끝없는 공세", "공격 속도 +30%", "✦", 1, "", new AugmentEffectData(AugmentEffectKind.Stat, "attackSpeed", AugmentOperation.Percent, .3f)),
            new AugmentData("common-power", "압도적 힘", "공격력 +30%", "✦", 1, "", new AugmentEffectData(AugmentEffectKind.Stat, "damage", AugmentOperation.Percent, .3f)),
            new AugmentData("common-combined", "전투 숙련", "공격력 및 공격 속도 +15%", "✦", 1, "", new AugmentEffectData(AugmentEffectKind.Stat, "damage", AugmentOperation.Percent, .15f), new AugmentEffectData(AugmentEffectKind.Stat, "attackSpeed", AugmentOperation.Percent, .15f)),
            new AugmentData("common-critical", "치명적 파괴", "치명타 배율 +25%p", "✦", 1, "", new AugmentEffectData(AugmentEffectKind.Stat, "critMultiplier", AugmentOperation.Add, .25f)),
            new AugmentData("common-elite", "거인 사냥꾼", "엘리트 대상 피해량 +20%", "✦", 1, "", new AugmentEffectData(AugmentEffectKind.Stat, "eliteDamage", AugmentOperation.Add, .2f)),
            new AugmentData("sword-q-charges", "연속 강화", "Q 적용 횟수 +1", "⚔", 1, "swordsman", new AugmentEffectData(AugmentEffectKind.Patch, "", AugmentOperation.Add, 1, "sword-q", "effects.0.nextAttack.charges")),
            new AugmentData("sword-w-speed", "진격의 방벽", "W 지속 중 이동 속도 +20%", "⚔", 1, "swordsman", new AugmentEffectData(AugmentEffectKind.AddEffect, "", AugmentOperation.Add, 0, "sword-w", injectedEffect: new EffectData(DeliveryKind.Buff, duration: 3, moveSpeedMultiplier: 1.2f))),
            new AugmentData("sword-e-interval", "소용돌이", "E 피해 간격 -10%", "⚔", 1, "swordsman", new AugmentEffectData(AugmentEffectKind.Patch, "", AugmentOperation.Multiply, .9f, "sword-e", "effects.1.interval")),
            new AugmentData("sword-r-area", "거대한 심판", "R 범위 +50%", "⚔", 1, "swordsman",
                new AugmentEffectData(AugmentEffectKind.Patch, "", AugmentOperation.Multiply, 1.5f, "sword-r", "targeting.radius"),
                new AugmentEffectData(AugmentEffectKind.Patch, "", AugmentOperation.Multiply, 1.5f, "sword-r", "targeting.coreRadius"),
                new AugmentEffectData(AugmentEffectKind.Patch, "", AugmentOperation.Multiply, 1.5f, "sword-r", "effects.0.shape.radius"),
                new AugmentEffectData(AugmentEffectKind.Patch, "", AugmentOperation.Multiply, 1.5f, "sword-r", "effects.0.shape.coreRadius")),
            new AugmentData("bow-q-range", "매의 눈", "Q 지속 중 공격 사거리 +25%", "➶", 1, "archer", new AugmentEffectData(AugmentEffectKind.Patch, "", AugmentOperation.Multiply, 1.25f, "bow-q", "effects.0.modifiers.attackRange")),
            new AugmentData("bow-w-repeat", "연속 일제사격", "W 발사 횟수 +1", "➶", 1, "archer", new AugmentEffectData(AugmentEffectKind.Repeat, "", AugmentOperation.Add, 1, "bow-w")),
            new AugmentData("bow-e-width", "대형 관통 화살", "E 적중 폭 +25%", "➶", 1, "archer", new AugmentEffectData(AugmentEffectKind.Patch, "", AugmentOperation.Multiply, 1.25f, "bow-e", "effects.0.radius"), new AugmentEffectData(AugmentEffectKind.Patch, "", AugmentOperation.Multiply, 1.25f, "bow-e", "targeting.width")),
            new AugmentData("bow-r-wave", "끝없는 화살비", "R 발동 횟수 +1 (50발)", "➶", 1, "archer", new AugmentEffectData(AugmentEffectKind.Patch, "", AugmentOperation.Add, 1, "bow-r", "effects.0.waves")),
            new AugmentData("mage-basic-chain", "확장된 연쇄", "일반 공격 적중 대상 +1", "✧", 1, "mage", new AugmentEffectData(AugmentEffectKind.Stat, "chainTargets", AugmentOperation.Add, 1)),
            new AugmentData("magic-q-pierce", "관통 화염구", "Q 추가 관통 횟수 +1", "✧", 1, "mage", new AugmentEffectData(AugmentEffectKind.Patch, "", AugmentOperation.Add, 1, "magic-q", "effects.0.penetrations")),
            new AugmentData("magic-w-area", "빙결 지대", "W 범위 +25%", "✧", 1, "mage", new AugmentEffectData(AugmentEffectKind.Patch, "", AugmentOperation.Multiply, 1.25f, "magic-w", "targeting.radius"), new AugmentEffectData(AugmentEffectKind.Patch, "", AugmentOperation.Multiply, 1.25f, "magic-w", "effects.0.shape.radius")),
            new AugmentData("magic-e-range", "공간 확장", "E 이동 사거리 +20%", "✧", 1, "mage", new AugmentEffectData(AugmentEffectKind.Patch, "", AugmentOperation.Multiply, 1.2f, "magic-e", "targeting.range")),
            new AugmentData("magic-r-defense", "비전 보호", "R 지속 중 받는 피해 25% 감소", "✧", 1, "mage", new AugmentEffectData(AugmentEffectKind.Patch, "", AugmentOperation.Multiply, .75f, "magic-r", "effects.0.modifiers.incomingDamage")),
            new AugmentData("haste-amplify-all", "끊임없는 순환", "전체 QWE 스킬 가속 수치 +20%", "◷", 1, "", new AugmentEffectData(AugmentEffectKind.Haste, "all", AugmentOperation.Percent, .2f)),
            new AugmentData("haste-amplify-q", "Q 초집중", "Q 스킬 가속 수치 +45%", "◷", 1, "", new AugmentEffectData(AugmentEffectKind.Haste, "q", AugmentOperation.Percent, .45f)),
            new AugmentData("haste-amplify-w", "W 초집중", "W 스킬 가속 수치 +45%", "◷", 1, "", new AugmentEffectData(AugmentEffectKind.Haste, "w", AugmentOperation.Percent, .45f)),
            new AugmentData("haste-amplify-e", "E 초집중", "E 스킬 가속 수치 +45%", "◷", 1, "", new AugmentEffectData(AugmentEffectKind.Haste, "e", AugmentOperation.Percent, .45f))
        };

        public static float KillsForNextLevel(int level) { var current = Math.Max(1, level); return Config.KillQuadratic * current * current + Config.KillLinear * current + Config.KillConstant; }
        public static float CooldownRecovery(float flatHaste, float percentHaste) { return 1 + flatHaste * (1 + percentHaste) / Config.HastePointsPerDoubleRate; }
        public static float MitigatedDamage(float amount, float armor) { return Math.Max(0, amount) * Config.ArmorConstant / (Config.ArmorConstant + Math.Max(0, armor)); }
    }
}
