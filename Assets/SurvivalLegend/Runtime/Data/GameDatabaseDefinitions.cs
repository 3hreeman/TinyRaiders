using System;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalLegend.Data
{
    [Serializable] public class StatDefinition { public float Hp, Speed, AttackSpeed, Damage, Range, HealthRegen, Lifesteal, Armor; public StatBlock ToData() { return new StatBlock(Hp, Speed, AttackSpeed, Damage, Range, HealthRegen, Lifesteal, Armor); } }
    [Serializable] public class TargetingDefinition { public TargetKind Kind; public float Range, Radius, Width, Spread, CoreRadius; public TargetingData ToData() { return new TargetingData(Kind, Range, Radius, Width, Spread, CoreRadius); } }
    [Serializable] public class EffectDefinition
    {
        public DeliveryKind Kind; public float DamageMultiplier, Duration, Interval, Delay, Speed, Range, Radius, Count, Waves, Spread, Length, Width;
        public bool Pierce, FollowCaster; public float AttackSpeedMultiplier, AttackRangeMultiplier, ExtraTargets; public ShapeKind Shape;
        public float CoreRadius, CoreMultiplier, RootSeconds, ExplosionRadius, Penetrations, MaxHealthRatio, Reflect; public bool UntilHit;
        public float NextAttackMultiplier, NextAttackSplash, NextAttackRadius, NextAttackCharges;
        public float BuffDamageMultiplier, MoveSpeedMultiplier, CooldownRateMultiplier, IncomingDamageMultiplier;
        public EffectData ToData() { return new EffectData(Kind, DamageMultiplier, Duration, Interval, Delay, Speed, Range, Radius, Count, Waves, Spread, Length, Width, Pierce, FollowCaster, AttackSpeedMultiplier, AttackRangeMultiplier, ExtraTargets, Shape, CoreRadius, CoreMultiplier, RootSeconds, ExplosionRadius, Penetrations, MaxHealthRatio, Reflect, UntilHit, NextAttackMultiplier, NextAttackSplash, NextAttackRadius, NextAttackCharges, BuffDamageMultiplier, MoveSpeedMultiplier, CooldownRateMultiplier, IncomingDamageMultiplier); }
        public static EffectDefinition From(EffectData d) { return new EffectDefinition { Kind=d.Kind, DamageMultiplier=d.DamageMultiplier, Duration=d.Duration, Interval=d.Interval, Delay=d.Delay, Speed=d.Speed, Range=d.Range, Radius=d.Radius, Count=d.Count, Waves=d.Waves, Spread=d.Spread, Length=d.Length, Width=d.Width, Pierce=d.Pierce, FollowCaster=d.FollowCaster, AttackSpeedMultiplier=d.AttackSpeedMultiplier, AttackRangeMultiplier=d.AttackRangeMultiplier, ExtraTargets=d.ExtraTargets, Shape=d.Shape, CoreRadius=d.CoreRadius, CoreMultiplier=d.CoreMultiplier, RootSeconds=d.RootSeconds, ExplosionRadius=d.ExplosionRadius, Penetrations=d.Penetrations, MaxHealthRatio=d.MaxHealthRatio, Reflect=d.Reflect, UntilHit=d.UntilHit, NextAttackMultiplier=d.NextAttackMultiplier, NextAttackSplash=d.NextAttackSplash, NextAttackRadius=d.NextAttackRadius, NextAttackCharges=d.NextAttackCharges, BuffDamageMultiplier=d.BuffDamageMultiplier, MoveSpeedMultiplier=d.MoveSpeedMultiplier, CooldownRateMultiplier=d.CooldownRateMultiplier, IncomingDamageMultiplier=d.IncomingDamageMultiplier }; }
    }

    public abstract class ContentDefinition : ScriptableObject { [Tooltip("Stable key used by saves, data links, and presentation mappings.")] public string Id; }
    [Serializable] public class AugmentEffectDefinition
    { public AugmentEffectKind Kind; public string StatOrScope, Path; public SkillDefinition Skill; public AugmentOperation Operation; public float Value; public EffectDefinition InjectedEffect = new EffectDefinition(); public AugmentEffectData ToData() { return new AugmentEffectData(Kind,StatOrScope,Operation,Value,Skill==null?string.Empty:Skill.Id,Path,InjectedEffect==null?default:InjectedEffect.ToData()); } }
    [Serializable] public class SpawnStageDefinition { public float Time, Interval, Batch; public int Cap; public SpawnStageData ToData(){return new SpawnStageData(Time,Interval,Batch,Cap);} }
    [Serializable] public class SpawnEntryDefinition { public EnemyDefinition Enemy; public float Weight=1, StartTime, RampSeconds; public bool FillRemainder; public SpawnEntryData ToData(){return new SpawnEntryData(Enemy.Id,Weight,StartTime,RampSeconds,FillRemainder);} }

    [Serializable] public class BalanceDefinition
    {
        public float FixedStep=1f/60f, ArenaWidth=1440, ArenaHeight=1440, ArenaInset=24, ProjectileMargin=30, PlayerRadius=15, HitInvulnerability=.55f, CritChance=.05f, CritMultiplier=1.5f, DamageGrowth=.08f, SpeedGrowth=.018f, AttackSpeedGrowth=.025f, HpGrowth=.06f;
        public float EnemyHpGrowth=.045f, EnemyDamageGrowth=.035f, EnemySpeedGrowth=.009f, EnemyInitialDelay=1.5f, EnemyDelayJitter=1.5f, EnemyEdgeInset=18, EnemyCornerInset=28, EnemySlowMultiplier=.35f;
        public float SpawnInitialDelay=1.2f, SpawnJitterMin=.8f, SpawnJitterWidth=.4f, DodgeDistance=180, DodgeDuration=.2f, DodgeRecharge=10, UltimateMaximum=100, UltimateChargePerHit=5, HastePointsPerDoubleRate=100, LevelUpSeconds=.85f, CardDealSeconds=.65f, EliteDeathSeconds=2.4f, DeathSeconds=1.2f, EliteInitialDelay=2, EliteAttackRange=520, EliteWindup=1.7f, EliteCleaveWindup=1.4f, EliteMotion=.65f, KillQuadratic=.5f, KillLinear=1.5f, KillConstant=1, ArmorConstant=100;
        public int DodgeCapacity=2, EliteEveryLevels=5;
        public BalanceData ToData(){return new BalanceData(FixedStep,ArenaWidth,ArenaHeight,ArenaInset,ProjectileMargin,PlayerRadius,HitInvulnerability,CritChance,CritMultiplier,DamageGrowth,SpeedGrowth,AttackSpeedGrowth,HpGrowth,EnemyHpGrowth,EnemyDamageGrowth,EnemySpeedGrowth,EnemyInitialDelay,EnemyDelayJitter,EnemyEdgeInset,EnemyCornerInset,EnemySlowMultiplier,SpawnInitialDelay,SpawnJitterMin,SpawnJitterWidth,.3f,20,100,.2f,60,120,DodgeDistance,DodgeDuration,DodgeRecharge,DodgeCapacity,UltimateMaximum,UltimateChargePerHit,HastePointsPerDoubleRate,LevelUpSeconds,CardDealSeconds,EliteDeathSeconds,DeathSeconds,EliteInitialDelay,EliteAttackRange,EliteWindup,EliteCleaveWindup,EliteMotion,EliteEveryLevels,KillQuadratic,KillLinear,KillConstant,ArmorConstant);}
    }
    [Serializable] public class CombatTuningDefinition { public float Sight=480, PlayerShotSpeed=620, PlayerShotRadius=5, PlayerShotLifetime=1.6f, HostileShotSpeed=205, HostileSpeedCap=100, HostileSpeedGrowth=.3f, HostileShotLifetime=7, ChainDelay=.14f, ChainRange=160, ExtraChainRatio=.5f, RecastDelay=.18f, ZoneInterval=.5f; public CombatTuningData ToData(){return new CombatTuningData(Sight,PlayerShotSpeed,PlayerShotRadius,PlayerShotLifetime,HostileShotSpeed,HostileSpeedCap,HostileSpeedGrowth,HostileShotLifetime,ChainDelay,ChainRange,ExtraChainRatio,RecastDelay,ZoneInterval);} }
    [Serializable] public class RainTuningDefinition { public int Columns=5; public float JitterStart=.25f,JitterWidth=.5f,RowDelay=.075f,FallTime=.45f,HitRadius=18; public RainTuningData ToData(){return new RainTuningData(Columns,JitterStart,JitterWidth,RowDelay,FallTime,HitRadius);} }
    [Serializable] public class RewardTuningDefinition { public float SilverChance=.55f,GoldChance=.35f,RerollPromotion=.15f; public int Choices=3; public RewardTuningData ToData(){return new RewardTuningData(SilverChance,GoldChance,RerollPromotion,Choices);} }
}
