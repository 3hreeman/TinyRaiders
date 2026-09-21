using System;
using System.Collections.Generic;
using UnityEngine;
using SurvivalLegend.Data;

namespace SurvivalLegend
{
    public sealed partial class GameSimulation
    {
        float Patched(SkillState skill,string path,float value)
        {
            foreach(var patch in skill.Patches)
                if(patch.Path==path)value=patch.Operation=="add"?value+patch.Value:value*patch.Value;
            return value;
        }

        public TargetingData GetSkillTargeting(int slot)
        {
            if(slot<0||slot>=State.Skills.Length)throw new ArgumentOutOfRangeException(nameof(slot));
            var skill=State.Skills[slot];var target=Content.Skills[skill.Id].Targeting;
            return new TargetingData(target.Kind,
                Patched(skill,"targeting.range",target.Range),
                Patched(skill,"targeting.radius",target.Radius*skill.AreaMultiplier),
                Patched(skill,"targeting.width",target.Width*skill.AreaMultiplier),
                Patched(skill,"targeting.spread",target.Spread),
                Patched(skill,"targeting.coreRadius",target.CoreRadius*skill.AreaMultiplier));
        }

        static float MotionSeconds(string motion)
        {
            switch(motion)
            {
                case "empower":return .55f;case "guard":return .6f;case "spin":return .5f;case "slam":return .7f;
                case "focus":case "volley":return .5f;case "pierce":return .6f;case "rain":return .75f;
                case "fireball":return .55f;case "frost":return .65f;case "blink":return .4f;case "overload":return .8f;
                default:return .45f;
            }
        }

        static float OptionalMultiplier(float value)=>value==0?1:value;

        void ExecuteSkill(int slot,Vector2 cursor)
        {
            var p=State.Player;var skill=State.Skills[slot];var definition=Content.Skills[skill.Id];var targeting=GetSkillTargeting(slot);
            Vector2 delta=cursor-p.Position;float angle=delta.sqrMagnitude<.0001f?p.Facing:Angle(delta);
            Vector2 center=targeting.Kind==TargetKind.Point?ClampWorld(p.Position+Vector2.ClampMagnitude(delta,targeting.Range)):p.Position;
            float power=skill.DamageMultiplier;
            float damage=p.Damage*power*BuffModifier(b=>b.Damage);
            PlayMotion(definition.Animation,targeting.Kind==TargetKind.Self?p.Facing:angle,MotionSeconds(definition.Animation));
            State.Buffs.RemoveAll(b=>b.Id==skill.Id);
            Effect(center,"skill",.75f,targeting.Radius>0?targeting.Radius:85,definition.Color,skill.Id);
            var effects=new List<EffectData>(definition.Effects);effects.AddRange(skill.AddedEffects);
            for(int i=0;i<effects.Count;i++)
            {
                var part=effects[i];string prefix="effects."+i+".";
                float Value(string path,float value)=>Patched(skill,prefix+path,value);
                float radius=Value("shape.radius",Value("radius",part.Radius*skill.AreaMultiplier));
                float root=Value("payload.root",part.RootSeconds);
                float payloadDamage=damage*Value("payload.multiplier",part.DamageMultiplier);
                if(part.Kind==DeliveryKind.Buff)
                {
                    float Mult(string key,float value)=>1+(Value("modifiers."+key,OptionalMultiplier(value))-1)*power;
                    var buff=new BuffState{
                        Id=skill.Id,Remaining=part.UntilHit?float.PositiveInfinity:Value("duration",part.Duration),Power=power,
                        Damage=Mult("damage",part.BuffDamageMultiplier),AttackSpeed=Mult("attackSpeed",part.AttackSpeedMultiplier),
                        MoveSpeed=Mult("moveSpeed",part.MoveSpeedMultiplier),CooldownRate=Mult("cooldownRate",part.CooldownRateMultiplier),
                        AttackRange=Mult("attackRange",part.AttackRangeMultiplier),IncomingDamage=Mult("incomingDamage",part.IncomingDamageMultiplier),
                        ExtraTargets=(int)Value("modifiers.extraTargets",part.ExtraTargets),
                        NextAttackMultiplier=Value("nextAttack.multiplier",part.NextAttackMultiplier),NextAttackSplash=Value("nextAttack.splash",part.NextAttackSplash),
                        NextAttackRadius=Value("nextAttack.radius",part.NextAttackRadius),NextAttackCharges=(int)Value("nextAttack.charges",part.NextAttackCharges)
                    };
                    State.Buffs.Add(buff);if(buff.ExtraTargets>0)p.FocusRemaining=buff.Remaining;
                    Effect(p.Position,"ring",.4f,targeting.Kind==TargetKind.Self?targeting.Radius:65,definition.Color);
                }
                else if(part.Kind==DeliveryKind.Shield)
                {
                    p.SkillShield=p.MaxHp*Value("maxHealthRatio",part.MaxHealthRatio)*power;
                    p.SkillShieldRemaining=Value("duration",part.Duration);p.SkillShieldReflect=Value("reflect",part.Reflect);
                    Effect(p.Position,"ring",.4f,65,definition.Color);
                }
                else if(part.Kind==DeliveryKind.Teleport)
                {
                    Effect(p.Position,"ring",.4f,65,definition.Color);MovePlayerForced(center);center=p.Position;Stop();Effect(p.Position,"ring",.4f,120,definition.Color);
                }
                else if(part.Kind==DeliveryKind.Area)
                {
                    DamageArea(center,radius,payloadDamage,!definition.Ultimate,root,Value("shape.coreRadius",part.CoreRadius*skill.AreaMultiplier),Value("shape.coreMultiplier",OptionalMultiplier(part.CoreMultiplier)));
                    Effect(center,"ring",.4f,radius,definition.Color);
                }
                else if(part.Kind==DeliveryKind.Projectile)
                {
                    bool bow=Content.Characters[State.Character].BasicWeapon=="bow";
                    Vector3 socket=bow?ArrowSocket(cursor):Vector3.zero;
                    Vector2 offset=new Vector2(socket.x,socket.y),origin=p.Position+offset;
                    float travel=Mathf.Max(1,Value("range",part.Range)-offset.magnitude),speed=Value("speed",part.Speed),spread=Value("spread",part.Spread);
                    int count=Math.Max(1,(int)Value("count",part.Count));
                    for(int n=0;n<count;n++)
                    {
                        float a=angle+(count==1?0:n/(count-1f)-.5f)*spread*Mathf.Deg2Rad;
                        var shot=Shoot(origin,origin+Direction(a),payloadDamage,speed,radius,travel/speed,false,part.Pierce,!definition.Ultimate);
                        shot.Penetrations=(int)Value("penetrations",part.Penetrations);shot.Explosion=Value("payload.explosion",part.ExplosionRadius*skill.AreaMultiplier);shot.Root=root;shot.Visual=shot.Explosion>0?"fireball":"arrow";shot.Color=definition.Color;
                        if(bow)SetFlight(shot,socket.z,travel);
                    }
                }
                else if(part.Kind==DeliveryKind.Rain)
                {
                    var rain=Content.Rain;int count=(int)Value("count",part.Count),waves=(int)Value("waves",part.Waves);
                    Vector2 forward=Direction(angle),side=new Vector2(-forward.y,forward.x);
                    for(int wave=0;wave<waves;wave++)for(int n=0;n<count;n++)
                    {
                        int row=n/rain.Columns,col=n%rain.Columns;
                        float along=(row+rain.JitterStart+Random01()*rain.JitterWidth)/Mathf.Ceil((float)count/rain.Columns)*Value("shape.length",part.Length*skill.AreaMultiplier);
                        float across=((col+rain.JitterStart+Random01()*rain.JitterWidth)/rain.Columns-.5f)*Value("shape.width",part.Width*skill.AreaMultiplier);
                        State.Zones.Add(new ZoneState{Id=++serial,Position=p.Position+forward*along+side*across,Radius=rain.HitRadius,Warning=wave*Value("interval",part.Interval)+row*rain.RowDelay+rain.FallTime,Life=.01f,Interval=1,Damage=payloadDamage,Charge=false,Visual="rain"});
                    }
                }
                else if(part.Kind==DeliveryKind.Zone)
                {
                    float interval=Value("interval",part.Interval),duration=Value("duration",part.Duration),delay=Value("delay",part.Delay);
                    State.Zones.Add(new ZoneState{Id=++serial,Position=center,Radius=radius,Shape=part.Shape==ShapeKind.Rectangle?"rectangle":"circle",Angle=angle,
                        Length=Value("shape.length",part.Length*skill.AreaMultiplier),Width=Value("shape.width",part.Width*skill.AreaMultiplier),
                        CoreRadius=Value("shape.coreRadius",part.CoreRadius*skill.AreaMultiplier),CoreMultiplier=Value("shape.coreMultiplier",OptionalMultiplier(part.CoreMultiplier)),Root=root,
                        Warning=delay,Life=duration,Tick=delay>0?0:interval,Interval=interval,TicksLeft=Mathf.Max(1,Mathf.FloorToInt(duration/interval+.5f)),Damage=payloadDamage,Charge=!definition.Ultimate,FollowCaster=part.FollowCaster,
                        Visual=definition.Animation=="slam"?"sword":part.FollowCaster?"spin":"rain"});
                }
                if(!CombatActive)break;
            }
        }
    }
}

