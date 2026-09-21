using System;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalLegend
{
    public sealed partial class GameSimulation
    {
        readonly List<PullField> pulls=new List<PullField>();
        sealed class PullField { public Vector2 Position; public float Life,Radius,Speed; }
        public bool CastSkill(int slot,Vector2 target)
        {
            if(!CombatActive||State.DodgeRemaining>0||slot<0||slot>=4) return false;
            var s=State.Skills[slot]; if(s.Remaining>.00001f) {Notice("Skill cooling down");return false;}
            bool ultimate=Content.Skills[s.Id].Ultimate;
            if(ultimate&&(State.Ultimate<Content.Config.UltimateMaximum||State.Buffs.Exists(b=>b.Id==s.Id))) {Notice("Ultimate requires full charge and no active ultimate buff");return false;}
            if(ultimate) State.Ultimate=0; else s.Remaining=s.Cooldown;
            State.AimSlot=-1; ExecuteSkill(slot,target);
            for(int i=0;i<s.Repeats;i++) pendingCasts.Add(new PendingCast{Slot=slot,Target=target,Delay=Content.Combat.RecastDelay*(i+1)});
            return true;
        }
        public bool UseSpecial(int slot,Vector2 target)
        {
            if(!CombatActive||State.DodgeRemaining>0||slot<0||slot>=2) return false;
            var special=State.Specials[slot];if(special.Remaining>.00001f){Notice("Special cooling down");return false;}
            var p=State.Player;var definition=Content.Specials[special.Id];
            Vector2 visualPosition=p.Position;
            if(definition.Kind=="heal") {if(p.Hp>=p.MaxHp){Notice("Health is already full");return false;}RestoreHealth((p.MaxHp-p.Hp)*definition.MissingHealthRatio);Effect(p.Position,"ring",.6f,50,definition.Color);}
            else if(definition.Kind=="haste") {p.HasteRemaining=definition.Duration;p.HasteSpeedMultiplier=definition.SpeedMultiplier;Effect(p.Position,"ring",.4f,55,definition.Color);}
            else if(definition.Kind=="shield") {p.Shield=p.MaxHp*definition.MaxHealthRatio;p.ShieldRemaining=definition.Duration;}
            else if(definition.Kind=="pull") {var delta=target-p.Position;Vector2 center=p.Position+Vector2.ClampMagnitude(delta,definition.Range);visualPosition=center;pulls.Add(new PullField{Position=center,Life=definition.Duration,Radius=definition.Radius,Speed=definition.PullSpeed});State.Zones.Add(new ZoneState{Id=++serial,Position=center,Radius=definition.Radius,Life=definition.Duration,Interval=10,Tick=10,Visual="blackhole"});}
            else return false;
            // Presentation event IDs are independent of combat IDs and random rolls.
            Effect(visualPosition,"special",.8f,definition.Kind=="pull"?definition.Radius:65,definition.Color,special.Id);
            special.Remaining=special.Cooldown;State.AimSlot=-1;return true;
        }
        void TickPulls(float dt)
        {
            foreach(var field in pulls) {field.Life-=dt;foreach(var e in State.Enemies) {Vector2 d=field.Position-e.Position;if(e.Hp>0&&d.magnitude<=field.Radius+e.Radius) MoveEnemyForced(e,e.Position+d.normalized*Mathf.Min(d.magnitude,field.Speed*dt));}}
            pulls.RemoveAll(f=>f.Life<=0);
        }
        void TickEnemy(EnemyState e,float dt)
        {
            e.Root=Mathf.Max(0,e.Root-dt);e.Flash=Mathf.Max(0,e.Flash-dt);e.AttackTimer-=dt;
            var player=State.Player;Vector2 d=player.Position-e.Position;float distance=d.magnitude;
            bool elite=IsElite(e.Kind);var enemyData=Content.Enemies[e.Kind];var ability=Content.Skills[enemyData.Patterns[e.PatternIndex]];
            float range=elite?(ability.Targeting.Kind==Data.TargetKind.Self?ability.Targeting.Radius:Content.Config.EliteAttackRange):enemyData.AttackRange;
            bool blocked=!ClearAttackLine(e.Position,player.Position);
            e.Facing=Angle(e.Aiming?e.Aim-e.Position:d);
            e.Moving=(distance>range||blocked)&&e.Root<=0&&(!elite||!e.Aiming);
            if(e.Moving)
            {
                Vector2 waypoint=Navigation.HasObstacles?Navigation.NextWaypoint(e.Position,player.Position,e.Radius):player.Position;
                Vector2 heading=waypoint-e.Position,before=e.Position;
                MoveEnemyForced(e,e.Position+heading.normalized*(Navigation.HasObstacles?Mathf.Min(heading.magnitude,e.Speed*dt):e.Speed*dt));
                Vector2 actual=e.Position-before;e.Moving=actual.sqrMagnitude>.000001f;if(e.Moving&&!e.Aiming)e.Facing=Angle(actual);
            }
            if(!elite&&(distance>range||blocked)) {e.Aiming=false;e.Windup=0;e.AttackTimer=Mathf.Max(e.AttackTimer,EnemyWindup(e.Kind));return;}
            if(!e.Aiming && e.AttackTimer<=(elite?0:EnemyWindup(e.Kind))&&distance<=range+Content.Config.PlayerRadius&&!blocked)
            {
                e.Aiming=true;e.Aim=player.Position;e.PatternAngle=Angle(d);
                e.Windup=elite?(e.PatternIndex==0?Content.Config.EliteCleaveWindup:Content.Config.EliteWindup):EnemyWindup(e.Kind);
                e.Pattern=ability.Id;
            }
            if(!e.Aiming)return;
            e.Windup=Mathf.Max(0,e.Windup-dt);if(e.Windup>0)return;
            foreach(var effect in ability.Effects)
            {
            if(effect.Kind==Data.DeliveryKind.Area)
            {
                Effect(e.Position,"ring",Content.Config.EliteMotion,effect.Radius,ability.Color);
                if(Vector2.Distance(e.Position,player.Position)<=effect.Radius+Content.Config.PlayerRadius)DamagePlayer(e.Damage*effect.DamageMultiplier,e.Id);
            }
            else if(effect.Kind==Data.DeliveryKind.Projectile)
            {
                int count=Math.Max(1,(int)effect.Count);
                for(int i=0;i<count;i++){float angle=e.PatternAngle+(count==1?0:i/(count-1f)-.5f)*effect.Spread*Mathf.Deg2Rad;var shot=Shoot(e.Position,e.Position+Direction(angle),e.Damage*effect.DamageMultiplier,effect.Speed,effect.Radius,effect.Range/effect.Speed,true,false);shot.SourceId=e.Id;}
            }
            else if(effect.Kind==Data.DeliveryKind.Zone) AddHostileZone(e.Aim,effect.Radius,e.Damage*effect.DamageMultiplier,effect.Delay,effect.Duration,effect.Interval,e.Id);
            }
            e.AttackTimer=ability.Cooldown;e.Aiming=false;e.PatternIndex=(e.PatternIndex+1)%enemyData.Patterns.Length;
        }
        float EnemyWindup(string kind)=>Content.Enemies[kind].Windup;
        void AddHostileZone(Vector2 point,float radius,float damage,float warning,float duration,float interval,int sourceId) {State.Zones.Add(new ZoneState{Id=++serial,Position=point,Radius=radius,Damage=damage,Warning=warning,Life=duration,Interval=interval,Hostile=true,SourceId=sourceId,TicksLeft=Mathf.Max(1,Mathf.FloorToInt(duration/interval+.5f)),Tick=warning>0?0:interval,Visual="hazard"});}
    }
}


