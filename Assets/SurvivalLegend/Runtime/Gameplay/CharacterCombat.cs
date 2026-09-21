using System;
using UnityEngine;

namespace SurvivalLegend
{
    public sealed partial class GameSimulation
    {
        float BuffModifier(Func<BuffState,float> select)
        {
            float value=1;
            foreach(var buff in State.Buffs)value*=select(buff);
            return value;
        }

        public float CooldownRecovery(int slot)
        {
            if(slot<0||slot>=State.Skills.Length)return 1;
            return (1+State.Skills[slot].Haste/Content.Config.HastePointsPerDoubleRate)*BuffModifier(b=>b.CooldownRate);
        }

        void BasicAttack(EnemyState target)
        {
            var p=State.Player;
            var character=Content.Characters[State.Character];
            float power=p.Damage*BuffModifier(b=>b.Damage);
            var empowered=State.Buffs.Find(b=>b.NextAttackCharges>0);
            if(empowered!=null||p.AttackAnimation<=0||p.Motion=="idle"||p.Motion=="swing"||p.Motion=="cleave"||p.Motion=="draw"||p.Motion=="cast")
                PlayMotion(character.BasicKind=="melee"?(empowered!=null?"cleave":"swing"):character.BasicKind=="chain"?"cast":"draw",Angle(target.Position-p.Position),character.AttackDuration);

            if(character.BasicKind=="melee")
            {
                DamageEnemy(target,power*(empowered?.NextAttackMultiplier??1)*(empowered?.Power??1));
                Effect(target.Position,"slash",.25f,22,character.Color);
                if(empowered!=null)
                {
                    foreach(var e in State.Enemies)
                        if(e.Id!=target.Id&&e.Hp>0&&Vector2.Distance(e.Position,target.Position)<=empowered.NextAttackRadius+e.Radius)
                            DamageEnemy(e,power*empowered.NextAttackSplash*empowered.Power);
                    Effect(target.Position,"ring",.4f,empowered.NextAttackRadius,character.Color);
                    if(--empowered.NextAttackCharges<=0)State.Buffs.Remove(empowered);
                }
                return;
            }
            if(character.BasicKind=="chain")
            {
                var socket=WeaponSocket(target.Position,true);
                DamageEnemy(target,power*character.ChainRatios[0]);
                Lightning(p.Position+new Vector2(socket.x,socket.y),target.Position,socket.z,true);
                var chain=new ChainState{Origin=target.Position,LastId=target.Id,Damage=power,Delay=Content.Combat.ChainDelay,Range=character.ChainRange};
                chain.Hits.Add(target.Id);
                for(int i=1;i<character.ChainRatios.Length;i++)chain.Ratios.Enqueue(character.ChainRatios[i]);
                for(int i=0;i<p.ChainTargets;i++)chain.Ratios.Enqueue(Content.Combat.ExtraChainRatio);
                State.Chains.Add(chain);
                return;
            }
            BasicShot(target);
            int count=0;foreach(var buff in State.Buffs)count+=buff.ExtraTargets;
            if(count<=0)return;
            var others=State.Enemies.FindAll(e=>e.Hp>0&&e.Id!=target.Id&&Vector2.Distance(e.Position,p.Position)<=AttackRange+e.Radius);
            others.Sort((a,b)=>(a.Position-target.Position).sqrMagnitude.CompareTo((b.Position-target.Position).sqrMagnitude));
            for(int i=0;i<Math.Min(count,others.Count);i++)BasicShot(others[i],target.Position);
        }

        void TickChains(float dt)
        {
            foreach(var chain in State.Chains)
            {
                chain.Delay-=dt;if(chain.Delay>0)continue;
                var previous=State.Enemies.Find(e=>e.Id==chain.LastId);
                if(previous!=null)chain.Origin=previous.Position;
                EnemyState target=null;float nearest=float.MaxValue;
                foreach(var enemy in State.Enemies)
                {
                    if(enemy.Hp<=0||chain.Hits.Contains(enemy.Id))continue;
                    float distance=Vector2.Distance(chain.Origin,enemy.Position);
                    if(distance<=chain.Range&&distance<nearest){nearest=distance;target=enemy;}
                }
                if(target==null||chain.Ratios.Count==0){chain.Ratios.Clear();continue;}
                DamageEnemy(target,chain.Damage*chain.Ratios.Dequeue());
                Lightning(chain.Origin,target.Position,20,false);
                chain.Origin=target.Position;chain.LastId=target.Id;chain.Hits.Add(target.Id);chain.Delay=Content.Combat.ChainDelay;
                if(!CombatActive)break;
            }
            State.Chains.RemoveAll(c=>c.Ratios.Count==0);
        }

        void Lightning(Vector2 start,Vector2 end,float elevation,bool fromStaff)
        {
            RecordEffect(new EffectState{Position=start,End=end,Kind="lightning",Life=.25f,MaxLife=.25f,Radius=5,Color="#53c7ff",Elevation=elevation,FromStaff=fromStaff});
        }

        void DamageArea(Vector2 center,float radius,float damage,bool charge,float root=0,float coreRadius=0,float coreMultiplier=1)
        {
            foreach(var enemy in State.Enemies)
            {
                if(enemy.Hp<=0||Vector2.Distance(center,enemy.Position)>radius+enemy.Radius)continue;
                float multiplier=coreRadius>0&&Vector2.Distance(center,enemy.Position)<=coreRadius?coreMultiplier:1;
                DamageEnemy(enemy,damage*multiplier,charge);
                if(root>0)enemy.Root=Mathf.Max(enemy.Root,root);
                if(!CombatActive)break;
            }
        }
    }
}

