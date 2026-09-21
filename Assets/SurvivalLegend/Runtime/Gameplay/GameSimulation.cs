using System;
using System.Collections.Generic;
using UnityEngine;
using Catalog = SurvivalLegend.Data.SurvivalLegendCatalog;
using SurvivalLegend.Data;

namespace SurvivalLegend
{
    // Deterministic world simulation. It never reads Unity time, input or Random.
    public sealed partial class GameSimulation
    {
        public const float WorldSize = 1440;
        public readonly RunState State;
        public GameContentSnapshot Content {get;}
        public ArenaNavigation Navigation {get;}
        public long TickNumber {get;private set;}
        long visualSerial;
        readonly Queue<EffectState> visualEvents=new Queue<EffectState>();
        int serial;
        float spawnTimer = 1.2f, attackTimer;
        Vector2 dodgeStart, dodgeEnd;
        bool dodgingThisStep, eliteActive;
        RunPhase resumePhase;
        List<AugmentChoice> deferredNormal;
        readonly Dictionary<string, Vector2> bonuses = new Dictionary<string, Vector2>();
        readonly List<PendingCast> pendingCasts = new List<PendingCast>();
        sealed class PendingCast { public int Slot; public Vector2 Target; public float Delay; }

        public GameSimulation(uint seed = 1, string specialD = null, string specialF = null, string character = null,GameContentSnapshot content=null)
        {
            Content=(content??GameContentSnapshot.CreateDefault()).Clone();
            Navigation=new ArenaNavigation(Content);
            State = new RunState { Seed = seed, Character = character };
            InitializeContent(specialD, specialF);
        }
        public float Random01() { State.Seed = unchecked(1664525u * State.Seed + 1013904223u); return (float)(State.Seed / 4294967296.0); }
        // Compatibility helpers for existing baseline tests; live runs use their injected Content.
        public static int KillsForNextLevel(int level) => (int)Catalog.KillsForNextLevel(level);
        public static float MitigatedDamage(float amount, float armor) => Catalog.MitigatedDamage(amount,armor);
        public bool CombatActive => State.Phase == RunPhase.Playing || State.Phase == RunPhase.LevelUp;
        Vector2 ClampWorld(Vector2 p) => new Vector2(Mathf.Clamp(p.x,Content.Config.ArenaInset,Content.Config.ArenaWidth-Content.Config.ArenaInset), Mathf.Clamp(p.y,Content.Config.ArenaInset,Content.Config.ArenaHeight-Content.Config.ArenaInset));
        Vector2 SafeDestination(Vector2 point)=>Navigation.HasObstacles?Navigation.ResolvePosition(point,Content.Config.PlayerRadius):ClampWorld(point);
        bool ClearAttackLine(Vector2 from,Vector2 to)=>!Navigation.HasObstacles||Navigation.HasLineOfSight(from,to,0);
        public void MovePlayerForced(Vector2 destination)
        {State.Player.Position=Navigation.HasObstacles?Navigation.SweepMove(State.Player.Position,destination,Content.Config.PlayerRadius):ClampWorld(destination);}
        public void MoveEnemyForced(EnemyState enemy,Vector2 destination)
        {if(enemy!=null)enemy.Position=Navigation.HasObstacles?Navigation.SweepMove(enemy.Position,destination,enemy.Radius):destination;}
        static float Angle(Vector2 d) => Mathf.Atan2(d.y,d.x);
        static Vector2 Direction(float a) => new Vector2(Mathf.Cos(a),Mathf.Sin(a));
        public void Notice(string text) { State.Notice = text; State.NoticeTime = 2.5f; }
        public void MoveTo(Vector2 point) { if (!CombatActive) return; State.Order="move"; State.Destination=SafeDestination(point); State.TargetId=-1; State.AimSlot=-1; Effect(State.Destination.Value,"move",.45f,12,"#b3f783"); }
        public void Stop() { State.Order="idle"; State.Destination=null; State.TargetId=-1; State.Player.Moving=false; }
        public void Attack(int id) { if (!CombatActive || FindEnemy(id)==null) return; State.Order="attack"; State.TargetId=id; State.Destination=null; State.AimSlot=-1; }
        public void AttackMove(Vector2 point) { if (!CombatActive) return; State.Order="attack-move"; State.Destination=SafeDestination(point); State.TargetId=ClosestEnemy(point,AttackRange)?.Id ?? -1; State.AimSlot=-1; Effect(State.Destination.Value,"move",.45f,12,"#ffb982"); }
        public void TogglePause() { if (CombatActive) { resumePhase=State.Phase; State.Phase=RunPhase.Paused; Stop(); State.AimSlot=-1; } else if (State.Phase==RunPhase.Paused) State.Phase=resumePhase; }
        EnemyState FindEnemy(int id) => State.Enemies.Find(e=>e.Id==id && e.Hp>0);
        EnemyState ClosestEnemy(Vector2 point,float range) { EnemyState best=null; float d=float.MaxValue; foreach(var e in State.Enemies) { if(e.Hp<=0 || Vector2.Distance(State.Player.Position,e.Position)>range+e.Radius) continue; float v=(point-e.Position).sqrMagnitude; if(v<d) {d=v;best=e;} } return best; }
        public bool Dodge(Vector2 cursor)
        {
            if(!CombatActive || State.DodgeCharges<=0 || State.DodgeRemaining>0) return false;
            var p=State.Player; var d=cursor-p.Position; if(d.sqrMagnitude<.000001f) d=Direction(p.Facing);
            dodgeStart=p.Position; dodgeEnd=ClampWorld(p.Position+d.normalized*Content.Config.DodgeDistance);
            if(Navigation.HasObstacles)dodgeEnd=Navigation.SweepMove(dodgeStart,dodgeEnd,Content.Config.PlayerRadius);
            if((dodgeEnd-dodgeStart).sqrMagnitude<.000001f) return false;
            if(State.DodgeCharges==Content.Config.DodgeCapacity) State.DodgeRecharge=Content.Config.DodgeRecharge;
            var weapon=Content.Characters[State.Character].BasicWeapon;
            State.DodgeCharges--; State.DodgeRemaining=Content.Config.DodgeDuration; PlayMotion(weapon=="staff"?"phase":weapon=="bow"?"lunge":"roll",Angle(d),Content.Config.DodgeDuration); Stop(); State.AimSlot=-1; return true;
        }
        void TickDodge(float dt)
        {
            if(State.DodgeCharges<Content.Config.DodgeCapacity) { State.DodgeRecharge-=dt; while(State.DodgeRecharge<=.00001f && State.DodgeCharges<Content.Config.DodgeCapacity) { State.DodgeCharges++; State.DodgeRecharge=State.DodgeCharges<Content.Config.DodgeCapacity?State.DodgeRecharge+Content.Config.DodgeRecharge:0; } }
            dodgingThisStep=State.DodgeRemaining>0;
            if(!dodgingThisStep) return;
            Effect(State.Player.Position,"dodge",.2f,15,"#b3f783");
            State.DodgeRemaining=Mathf.Max(0,State.DodgeRemaining-dt);
            MovePlayerForced(Vector2.Lerp(dodgeStart,dodgeEnd,1-State.DodgeRemaining/Content.Config.DodgeDuration));
        }
        public void Step(float delta)
        {
            float dt=Mathf.Clamp(delta,0,.05f); if(dt<=0) return;
            TickNumber++;
            if(State.Phase==RunPhase.Dying) { State.PhaseElapsed+=dt; if(State.PhaseElapsed>=Content.Config.DeathSeconds) State.Phase=RunPhase.Results; return; }
            if(State.Phase==RunPhase.EliteDeath) { State.Projectiles.RemoveAll(s=>s.Hostile);State.Zones.RemoveAll(z=>z.Hostile); State.PhaseElapsed+=dt; if(State.PhaseElapsed>=Content.Config.EliteDeathSeconds) { State.SpecialReward=true; RollChoices(true); EnterPhase(RunPhase.AugmentEnter); } return; }
            if(State.Phase==RunPhase.AugmentEnter) { State.PhaseElapsed+=dt; if(State.PhaseElapsed>=Content.Config.CardDealSeconds) EnterPhase(RunPhase.Augment); return; }
            if(!CombatActive) return;
            State.Time+=dt; var p=State.Player; RestoreHealth(p.HealthRegen*dt); TickDodge(dt);
            TickChains(dt);if(!CombatActive)return;
            State.NoticeTime=Mathf.Max(0,State.NoticeTime-dt);
            p.Invulnerable=Mathf.Max(0,p.Invulnerable-dt); attackTimer=Mathf.Max(0,attackTimer-dt);
            p.AttackAnimation=Mathf.Max(0,p.AttackAnimation-dt); p.FocusRemaining=Mathf.Max(0,p.FocusRemaining-dt); p.HasteRemaining=Mathf.Max(0,p.HasteRemaining-dt);
            p.ShieldRemaining=Mathf.Max(0,p.ShieldRemaining-dt); if(p.ShieldRemaining==0) p.Shield=0;
            foreach(var s in State.Skills) s.Remaining=Mathf.Max(0,s.Remaining-dt*(1+s.Haste/Content.Config.HastePointsPerDoubleRate)*BuffModifier(b=>b.CooldownRate));
            p.SkillShieldRemaining=Mathf.Max(0,p.SkillShieldRemaining-dt);if(p.SkillShieldRemaining==0)p.SkillShield=0;
            foreach(var buff in State.Buffs)buff.Remaining-=dt;State.Buffs.RemoveAll(b=>b.Remaining<=.00000001f);
            foreach(var s in State.Specials) s.Remaining=Mathf.Max(0,s.Remaining-dt);
            for(int i=pendingCasts.Count-1;i>=0;i--) { var c=pendingCasts[i]; c.Delay-=dt; if(c.Delay<=0) { ExecuteSkill(c.Slot,c.Target); pendingCasts.RemoveAt(i); } }
            UpdateMovement(dt);
            if(!eliteActive) TickSpawns(dt);
            foreach(var e in State.Enemies) { if(e.Hp>0) TickEnemy(e,dt); if(!CombatActive) return; }
            TickPulls(dt);
            var target=FindEnemy(State.TargetId);
            if(!dodgingThisStep && attackTimer<=0 && target!=null && Vector2.Distance(p.Position,target.Position)<=AttackRange+target.Radius&&ClearAttackLine(p.Position,target.Position))
            {
                attackTimer=1/(p.AttackSpeed*BuffModifier(b=>b.AttackSpeed));
                BasicAttack(target);
            }
            if(!CombatActive)return;
            TickProjectiles(dt); if(!CombatActive) return;
            TickZones(dt); if(!CombatActive) return;
            State.Enemies.RemoveAll(e=>e.Hp<=0);
            foreach(var e in State.Effects) e.Life-=dt; State.Effects.RemoveAll(e=>e.Life<=0);
            if(State.Phase==RunPhase.LevelUp) { State.PhaseElapsed+=dt; if(State.PhaseElapsed>=Content.Config.LevelUpSeconds) { EnterPhase(RunPhase.AugmentEnter); State.AimSlot=-1; } }
            else CheckLevelUp();
        }
        void EnterPhase(RunPhase phase) { State.Phase=phase; State.PhaseElapsed=0; }
        void UpdateMovement(float dt)
        {
            var p=State.Player; p.Moving=false; if(dodgingThisStep) return;
            Vector2? destination=State.Destination;
            if(State.Order=="attack" || State.Order=="attack-move")
            {
                var target=FindEnemy(State.TargetId);
                if(State.Order=="attack-move" && (target==null || Vector2.Distance(p.Position,target.Position)>Content.Combat.Sight+target.Radius)) { target=ClosestEnemy(p.Position,Mathf.Max(Content.Combat.Sight,AttackRange)); State.TargetId=target?.Id??-1; }
                if(target!=null) destination=Vector2.Distance(p.Position,target.Position)<=AttackRange+target.Radius&&ClearAttackLine(p.Position,target.Position) ? (Vector2?)null : target.Position;
                else if(State.Order=="attack") { Stop(); return; }
            }
            if(!destination.HasValue) return;
            Vector2 waypoint=Navigation.HasObstacles?Navigation.NextWaypoint(p.Position,destination.Value,Content.Config.PlayerRadius):destination.Value;
            Vector2 d=waypoint-p.Position; float travel=Mathf.Min(d.magnitude,p.Speed*BuffModifier(b=>b.MoveSpeed)*(p.HasteRemaining>0?p.HasteSpeedMultiplier:1)*dt);
            if(travel>.001f) {Vector2 before=p.Position;MovePlayerForced(p.Position+d.normalized*travel);Vector2 actual=p.Position-before;if(actual.sqrMagnitude>.000001f){p.Facing=Angle(actual);p.Moving=true;}}
            bool arrived=Navigation.HasObstacles?Vector2.Distance(p.Position,destination.Value)<.5f:d.magnitude<=travel+.001f;
            if(arrived&&State.TargetId<0)Stop();
        }
        void BasicShot(EnemyState target,Vector2? aim=null)
        {
            var socket=ArrowSocket(aim??target.Position);
            var origin=State.Player.Position+new Vector2(socket.x,socket.y);
            var s=Shoot(origin,target.Position,State.Player.Damage*BuffModifier(b=>b.Damage),Content.Combat.PlayerShotSpeed,Content.Combat.PlayerShotRadius,Content.Combat.PlayerShotLifetime,false,false);
            s.TargetId=target.Id;SetFlight(s,socket.z,Vector2.Distance(origin,target.Position));
        }
        ProjectileState Shoot(Vector2 origin,Vector2 target,float damage,float speed,float radius,float life,bool hostile,bool pierce,bool charge=true)
        {
            var shot=new ProjectileState { Id=++serial,Position=origin,Velocity=(target-origin).normalized*speed,Damage=damage,Radius=radius,Life=life,Hostile=hostile,Pierce=pierce,Charge=charge,Visual=hostile?"fireball":"arrow" }; State.Projectiles.Add(shot); return shot;
        }
        void TickProjectiles(float dt)
        {
            foreach(var shot in State.Projectiles)
            {
                Vector2 before=shot.Position;
                if(shot.TargetId>=0) { var target=FindEnemy(shot.TargetId); if(target==null){shot.Life=0;continue;} shot.Velocity=(target.Position-shot.Position).normalized*Mathf.Min(Content.Combat.PlayerShotSpeed,Vector2.Distance(shot.Position,target.Position)/dt); }
                if(shot.HasFlight)shot.FlightTraveled+=shot.Velocity.magnitude*dt;
                shot.Position+=shot.Velocity*dt; shot.Life-=dt;
                if(shot.Hostile) { if(SegmentDistance(State.Player.Position,before,shot.Position)<=shot.Radius+Content.Config.PlayerRadius) { DamagePlayer(shot.Damage,shot.SourceId); shot.Life=0; } }
                else foreach(var enemy in State.Enemies)
                {
                    if(enemy.Hp<=0 || (shot.TargetId>=0&&shot.TargetId!=enemy.Id) || shot.Hits.Contains(enemy.Id) || SegmentDistance(enemy.Position,before,shot.Position)>shot.Radius+enemy.Radius) continue;
                    shot.Hits.Add(enemy.Id);
                    if(shot.Explosion>0){DamageArea(enemy.Position,shot.Explosion,shot.Damage,shot.Charge,shot.Root);Effect(enemy.Position,"ring",.4f,shot.Explosion,shot.Color??"#ffac79");}
                    else DamageEnemy(enemy,shot.Damage,shot.Charge);
                    if(shot.Root>0)enemy.Root=Mathf.Max(enemy.Root,shot.Root);
                    if(!shot.Pierce&&shot.Hits.Count>shot.Penetrations) {shot.Life=0;break;} if(!CombatActive) break;
                }
                if(!CombatActive) break;
            }
            State.Projectiles.RemoveAll(s=>s.Life<=0 || s.Position.x< -Content.Config.ProjectileMargin || s.Position.y< -Content.Config.ProjectileMargin || s.Position.x>Content.Config.ArenaWidth+Content.Config.ProjectileMargin || s.Position.y>Content.Config.ArenaHeight+Content.Config.ProjectileMargin);
        }
        static float SegmentDistance(Vector2 point,Vector2 a,Vector2 b) { var d=b-a; float t=d.sqrMagnitude>.00001f?Mathf.Clamp01(Vector2.Dot(point-a,d)/d.sqrMagnitude):0; return Vector2.Distance(point,a+d*t); }
        void TickZones(float dt)
        {
            foreach(var z in State.Zones)
            {
                if(z.FollowCaster)z.Position=State.Player.Position;
                if(z.Warning>0) { z.Warning=Mathf.Max(0,z.Warning-dt);continue; }
                z.Life-=dt; z.Tick-=dt;
                if(z.Tick>.000001f||z.TicksLeft==0) continue; z.Tick+=Mathf.Max(.01f,z.Interval);
                if(z.TicksLeft>0)z.TicksLeft--;
                if(z.Hostile) { if(InZone(State.Player.Position,Content.Config.PlayerRadius,z)) DamagePlayer(z.Damage,z.SourceId); }
                else if(z.Visual=="rain"&&z.TicksLeft<0) { var victim=State.Enemies.FindAll(e=>e.Hp>0&&InZone(e.Position,e.Radius,z)); victim.Sort((a,b)=>(a.Position-z.Position).sqrMagnitude.CompareTo((b.Position-z.Position).sqrMagnitude)); if(victim.Count>0) DamageEnemy(victim[0],z.Damage,false); }
                else if(z.Shape=="circle")DamageArea(z.Position,z.Radius,z.Damage,z.Charge,z.Root,z.CoreRadius,z.CoreMultiplier);
                else foreach(var e in State.Enemies) if(e.Hp>0&&InZone(e.Position,e.Radius,z)){DamageEnemy(e,z.Damage,z.Charge);if(z.Root>0)e.Root=Mathf.Max(e.Root,z.Root);}
                Effect(z.Position,"ring",.3f,z.Radius,z.Hostile?"#ff7180":"#d8faaa");
                if(!CombatActive) break;
            }
            State.Zones.RemoveAll(z=>z.Warning<=0&&z.Life<=0);
        }
        static bool InZone(Vector2 position,float radius,ZoneState z)
        {
            if(z.Shape=="circle") return Vector2.Distance(position,z.Position)<=z.Radius+radius;
            var offset=position-z.Position; var axis=Direction(z.Angle); float along=Vector2.Dot(offset,axis), side=Vector2.Dot(offset,new Vector2(-axis.y,axis.x));
            return along>=-radius&&along<=z.Length+radius&&Mathf.Abs(side)<=z.Width/2+radius;
        }
        public void RestoreHealth(float amount) { if(amount<=0||!CombatActive||State.Player.Hp<=0) return; float actual=Mathf.Min(amount,State.Player.MaxHp-State.Player.Hp); State.Player.Hp+=actual; State.Healing+=actual; }
        public void DamagePlayer(float amount,int sourceId=-1)
        {
            var p=State.Player; if(!CombatActive||p.Invulnerable>0||dodgingThisStep||State.DodgeRemaining>0||amount<=0) return;
            float damage=Content.MitigatedDamage(amount*BuffModifier(b=>b.IncomingDamage),p.Armor);
            if(p.SkillShieldRemaining>0){var attacker=FindEnemy(sourceId);if(attacker!=null)DamageEnemy(attacker,damage*p.SkillShieldReflect*BuffModifier(b=>b.Damage));float skillAbsorb=Mathf.Min(p.SkillShield,damage);p.SkillShield-=skillAbsorb;damage-=skillAbsorb;}
            float absorb=Mathf.Min(p.Shield,damage); p.Shield-=absorb; damage-=absorb;
            State.DamageTaken+=Mathf.Min(p.Hp,damage); p.Hp=Mathf.Max(0,p.Hp-damage); p.Invulnerable=Content.Config.HitInvulnerability;
            Effect(p.Position,"text",.65f,0,"#ff7180",Mathf.RoundToInt(damage).ToString());
            if(p.Hp<=0) { Stop(); State.AimSlot=-1; State.AugmentChoices.Clear();p.FocusRemaining=0;p.HasteRemaining=0;p.Shield=0;p.ShieldRemaining=0;p.SkillShield=0;p.SkillShieldRemaining=0;State.Buffs.Clear();pulls.Clear(); EnterPhase(RunPhase.Dying); }
        }
        public void DamageEnemy(EnemyState enemy,float amount,bool charge=true)
        {
            if(!CombatActive||enemy==null||enemy.Hp<=0||amount<=0) return;
            if(charge) State.Ultimate=Mathf.Min(Content.Config.UltimateMaximum,State.Ultimate+Content.Config.UltimateChargePerHit);
            if(IsElite(enemy.Kind)) amount*=1+State.Player.EliteDamage;
            bool critical=Random01()<State.Player.CritChance; if(critical) amount*=State.Player.CritMultiplier;
            float actual=Mathf.Min(enemy.Hp,amount); State.DamageDealt+=actual; RestoreHealth(actual*State.Player.Lifesteal); enemy.Hp-=amount; enemy.Flash=.12f;
            Effect(enemy.Position,"text",critical?.9f:.65f,0,critical?"#ffd46b":"#f1f7ff",Mathf.RoundToInt(amount).ToString(),critical);
            if(enemy.Hp>0) return;
            State.Kills++; State.Xp++; Effect(enemy.Position,"ring",.4f,27,"#b3f783");
            if(IsElite(enemy.Kind)) { State.EliteKills++; deferredNormal=State.Phase==RunPhase.LevelUp?new List<AugmentChoice>(State.AugmentChoices):null; EnterPhase(RunPhase.EliteDeath); Stop(); State.AimSlot=-1; }
        }
        void Effect(Vector2 position,string kind,float life,float radius,string color,string text=null,bool critical=false) { RecordEffect(new EffectState {Position=position,Kind=kind,Life=life,MaxLife=life,Radius=radius,Color=color,Text=text,Critical=critical}); }
        void RecordEffect(EffectState effect)
        {
            effect.Id=++visualSerial;effect.Tick=TickNumber;State.Effects.Add(effect);visualEvents.Enqueue(effect.Snapshot());
        }
        public void DrainVisualEvents(List<EffectState> destination)
        {
            if(destination==null)throw new ArgumentNullException(nameof(destination));
            while(visualEvents.Count>0)destination.Add(visualEvents.Dequeue());
        }
    }
}



