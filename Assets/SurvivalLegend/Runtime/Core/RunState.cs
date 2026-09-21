using System;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalLegend
{
    public enum RunPhase { Title, Playing, Paused, LevelUp, AugmentEnter, Augment, EliteDeath, Dying, Results }
    [Serializable] public sealed class RunState
    {
        public RunPhase Phase = RunPhase.Playing;
        public string Character = "archer";
        public PlayerState Player = new PlayerState();
        public List<EnemyState> Enemies = new List<EnemyState>();
        public List<ProjectileState> Projectiles = new List<ProjectileState>();
        public List<ZoneState> Zones = new List<ZoneState>();
        public List<EffectState> Effects = new List<EffectState>();
        public List<BuffState> Buffs = new List<BuffState>();
        public List<ChainState> Chains = new List<ChainState>();
        public List<AugmentChoice> AugmentChoices = new List<AugmentChoice>();
        public List<string> History = new List<string>();
        public SkillState[] Skills;
        public SpecialState[] Specials;
        public float Time, Ultimate, PhaseElapsed, DamageDealt, DamageTaken, Healing;
        public int Level = 1, Kills, Xp, NextXp = 3, EliteKills;
        public string Notice = "Right click: move / attack · A + click: attack move · QWER: skills";
        public float NoticeTime = 6;
        public uint Seed;
        public bool SpecialReward;
        public int DodgeCharges = 2;
        public float DodgeRecharge, DodgeRemaining;
        public string Order = "idle";
        public Vector2? Destination;
        public int TargetId = -1, AimSlot = -1;
    }
    [Serializable] public sealed class PlayerState
    {
        public Vector2 Position = new Vector2(720,720);
        public float Hp = 120, MaxHp = 120, Speed = 216, Damage = 17.6f, AttackSpeed = 1.7f, Range = 300;
        public float Armor = 8, HealthRegen = 3, Lifesteal, CritChance = .05f, CritMultiplier = 1.5f;
        public float Facing = Mathf.PI / 4, Invulnerable, Shield, ShieldRemaining;
        public bool Moving;
        public float AttackAnimation, FocusRemaining, HasteRemaining;
        public float HasteSpeedMultiplier=1;
        public string Motion = "idle";
        public float MotionDuration, MotionAngle;
        public float EliteDamage;
        public int ChainTargets;
        public float SkillShield, SkillShieldRemaining, SkillShieldReflect;
    }
    [Serializable] public sealed class EnemyState
    {
        public int Id, Level;
        public string Kind;
        public Vector2 Position;
        public float Hp, MaxHp, Radius, Speed, Damage, Facing, AttackTimer, Windup, Root, Flash;
        public bool Moving;
        public string Pattern;
        public Vector2 Aim;
        public float PatternAngle;
        public bool Aiming;
        public int PatternIndex;
    }
    [Serializable] public sealed class ProjectileState
    {
        public int Id, TargetId = -1;
        public int SourceId = -1, Penetrations;
        public Vector2 Position, Velocity;
        public float Radius, Damage, Life;
        public float Explosion, Root;
        public string Color;
        public bool Hostile, Pierce, Charge = true;
        public string Visual = "arrow";
        public bool HasFlight;
        public float FlightStartHeight, FlightDistance, FlightTraveled;
        public float Elevation => HasFlight ? Mathf.Lerp(FlightStartHeight,20,Mathf.Clamp01(FlightTraveled/Mathf.Max(1,FlightDistance))) : 10;
        public HashSet<int> Hits = new HashSet<int>();
    }
    [Serializable] public sealed class ZoneState
    {
        public int Id;
        public Vector2 Position;
        public float Radius, Warning, Life, Interval, Tick, Damage, Angle, Length, Width, Arc;
        public bool Hostile, Charge = true;
        public bool FollowCaster;
        public int TicksLeft = -1, SourceId = -1;
        public float CoreRadius, CoreMultiplier = 1, Root;
        public string Shape = "circle", Visual;
    }
    [Serializable] public sealed class EffectState
    {
        public long Id, Tick;
        public Vector2 Position, End;
        public string Kind, Text, Color;
        public float Life, MaxLife, Radius;
        public bool Critical;
        public float Elevation;
        public bool FromStaff;
        public EffectState Snapshot() => (EffectState)MemberwiseClone();
    }
    [Serializable] public sealed class SkillState
    {
        public string Id, Name, Description;
        public int Level = 1;
        public float Cooldown, Remaining, DamageMultiplier = 1, AreaMultiplier = 1, Haste;
        public int Repeats;
        public List<SkillPatchState> Patches = new List<SkillPatchState>();
        public List<SurvivalLegend.Data.EffectData> AddedEffects = new List<SurvivalLegend.Data.EffectData>();
    }
    [Serializable] public sealed class SpecialState
    {
        public string Id, Name;
        public float Cooldown, Remaining;
    }
    [Serializable] public sealed class AugmentChoice
    {
        public string Id, Title, Description, Tier, Kind, Stat;
        public float Value;
        public int SkillSlot = -1;
        public string Upgrade;
        public bool Rerolled;
        public string Operation = "percent";
    }
    [Serializable] public sealed class SkillPatchState
    {
        public string Path, Operation;
        public float Value;
    }
    [Serializable] public sealed class BuffState
    {
        public string Id;
        public float Remaining, Power=1;
        public float Damage=1,AttackSpeed=1,MoveSpeed=1,CooldownRate=1,AttackRange=1,IncomingDamage=1;
        public int ExtraTargets;
        public float NextAttackMultiplier, NextAttackSplash, NextAttackRadius;
        public int NextAttackCharges;
    }
    [Serializable] public sealed class ChainState
    {
        public Vector2 Origin;
        public int LastId;
        public List<int> Hits = new List<int>();
        public Queue<float> Ratios = new Queue<float>();
        public float Damage, Delay, Range;
    }
}
