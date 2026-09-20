using System;
using System.Collections.Generic;
using UnityEngine;
using Catalog = SurvivalLegend.Data.SurvivalLegendCatalog;

namespace SurvivalLegend
{
    public sealed partial class GameSimulation
    {
        readonly List<PullField> pulls=new List<PullField>();
        sealed class PullField { public Vector2 Position; public float Life; }
        public bool CastSkill(int slot,Vector2 target)
        {
            if(!CombatActive||State.DodgeRemaining>0||slot<0||slot>=4) return false;
            var s=State.Skills[slot]; if(s.Remaining>.00001f) {Notice("Skill cooling down");return false;}
            if(slot==3&&(State.Ultimate<100||State.Buffs.Exists(b=>b.Id==s.Id))) {Notice("Ultimate requires 100 charge and no active ultimate buff");return false;}
            if(slot==3) State.Ultimate=0; else s.Remaining=s.Cooldown;
            State.AimSlot=-1; ExecuteSkill(slot,target);
            for(int i=0;i<s.Repeats;i++) pendingCasts.Add(new PendingCast{Slot=slot,Target=target,Delay=Catalog.Combat.RecastDelay*(i+1)});
            return true;
        }
        public bool UseSpecial(int slot,Vector2 target)
        {
            if(!CombatActive||State.DodgeRemaining>0||slot<0||slot>=2) return false;
            var special=State.Specials[slot];if(special.Remaining>.00001f){Notice("Special cooling down");return false;}
            var p=State.Player;var definition=Catalog.Specials[special.Id];
            if(special.Id=="heal") {if(p.Hp>=p.MaxHp){Notice("Health is already full");return false;}RestoreHealth((p.MaxHp-p.Hp)*definition.MissingHealthRatio);Effect(p.Position,"ring",.6f,50,"#b3f783");}
            else if(special.Id=="haste") {p.HasteRemaining=definition.Duration;Effect(p.Position,"ring",.4f,55,"#ffe49d");}
            else if(special.Id=="barrier") {p.Shield=p.MaxHp*definition.MaxHealthRatio;p.ShieldRemaining=definition.Duration;}
            else if(special.Id=="blackhole") {var delta=target-p.Position;Vector2 center=p.Position+Vector2.ClampMagnitude(delta,definition.Range);pulls.Add(new PullField{Position=center,Life=definition.Duration});State.Zones.Add(new ZoneState{Id=++serial,Position=center,Radius=definition.Radius,Life=definition.Duration,Interval=10,Tick=10,Visual="blackhole"});}
            else return false;
            special.Remaining=special.Cooldown;State.AimSlot=-1;return true;
        }
        void TickPulls(float dt)
        {
            var definition=Catalog.Specials["blackhole"];
            foreach(var field in pulls) {field.Life-=dt;foreach(var e in State.Enemies) {Vector2 d=field.Position-e.Position;if(e.Hp>0&&d.magnitude<=definition.Radius+e.Radius) e.Position+=d.normalized*Mathf.Min(d.magnitude,definition.PullSpeed*dt);}}
            pulls.RemoveAll(f=>f.Life<=0);
        }
        void TickEnemy(EnemyState e,float dt)
        {
            e.Root=Mathf.Max(0,e.Root-dt);e.Flash=Mathf.Max(0,e.Flash-dt);e.AttackTimer-=dt;
            var player=State.Player;Vector2 d=player.Position-e.Position;float distance=d.magnitude;
            bool elite=e.Kind=="elite";var enemyData=Catalog.Enemies[e.Kind];var ability=Catalog.Skills[enemyData.Patterns[e.PatternIndex]];var effect=ability.Effects[0];
            float range=elite?(ability.Targeting.Kind==Data.TargetKind.Self?ability.Targeting.Radius:Catalog.Config.EliteAttackRange):enemyData.AttackRange;
            e.Facing=Angle(e.Aiming?e.Aim-e.Position:d);
            e.Moving=distance>range&&e.Root<=0&&(!elite||!e.Aiming);
            if(e.Moving)e.Position+=d.normalized*e.Speed*dt;
            if(!elite&&distance>range) {e.Aiming=false;e.Windup=0;e.AttackTimer=Mathf.Max(e.AttackTimer,EnemyWindup(e.Kind));return;}
            if(!e.Aiming && e.AttackTimer<=(elite?0:EnemyWindup(e.Kind))&&distance<=range+15)
            {
                e.Aiming=true;e.Aim=player.Position;e.PatternAngle=Angle(d);
                e.Windup=elite?(e.PatternIndex==0?Catalog.Config.EliteCleaveWindup:Catalog.Config.EliteWindup):EnemyWindup(e.Kind);
                e.Pattern=ability.Id;
            }
            if(!e.Aiming)return;
            e.Windup=Mathf.Max(0,e.Windup-dt);if(e.Windup>0)return;
            if(effect.Kind==Data.DeliveryKind.Area)
            {
                Effect(e.Position,"ring",Catalog.Config.EliteMotion,effect.Radius,ability.Color);
                if(Vector2.Distance(e.Position,player.Position)<=effect.Radius+Catalog.Config.PlayerRadius)DamagePlayer(e.Damage*effect.DamageMultiplier,e.Id);
            }
            else if(effect.Kind==Data.DeliveryKind.Projectile)
            {
                int count=Math.Max(1,(int)effect.Count);
                for(int i=0;i<count;i++){float angle=e.PatternAngle+(count==1?0:i/(count-1f)-.5f)*effect.Spread*Mathf.Deg2Rad;var shot=Shoot(e.Position,e.Position+Direction(angle),e.Damage*effect.DamageMultiplier,effect.Speed,effect.Radius,effect.Range/effect.Speed,true,false);shot.SourceId=e.Id;}
            }
            else AddHostileZone(e.Aim,effect.Radius,e.Damage*effect.DamageMultiplier,effect.Delay,effect.Duration,effect.Interval,e.Id);
            e.AttackTimer=ability.Cooldown;e.Aiming=false;if(elite)e.PatternIndex=(e.PatternIndex+1)%enemyData.Patterns.Length;
        }
        static float EnemyWindup(string kind)=>Catalog.Enemies[kind].Windup;
        void AddHostileZone(Vector2 point,float radius,float damage,float warning,float duration,float interval,int sourceId) {State.Zones.Add(new ZoneState{Id=++serial,Position=point,Radius=radius,Damage=damage,Warning=warning,Life=duration,Interval=interval,Hostile=true,SourceId=sourceId,TicksLeft=Mathf.Max(1,Mathf.FloorToInt(duration/interval+.5f)),Tick=warning>0?0:interval,Visual="hazard"});}
    }
}

