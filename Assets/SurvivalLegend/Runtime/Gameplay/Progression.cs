using System;
using System.Collections.Generic;
using UnityEngine;
using SurvivalLegend.Data;

namespace SurvivalLegend
{
    public sealed partial class GameSimulation
    {
        readonly Dictionary<string,Vector2> hasteBonuses=new Dictionary<string,Vector2>();
        readonly Queue<int> pendingElites=new Queue<int>();
        void InitializeContent(string specialD,string specialF)
        {
            State.Character=State.Character??Content.DefaultCharacterId;
            if(!Content.Characters.ContainsKey(State.Character))throw new InvalidOperationException("Unknown character definition: "+State.Character);
            var c=Content.Characters[State.Character];State.Skills=new SkillState[4];
            for(int i=0;i<4;i++) {var data=Content.Skills[c.Skills[i]];State.Skills[i]=new SkillState{Id=data.Id,Name=data.Name,Description=data.Description,Cooldown=data.Cooldown};}
            specialD=specialD??Content.DefaultSpecialDId;specialF=specialF??Content.DefaultSpecialFId;
            if(!Content.Specials.ContainsKey(specialD)||!Content.Specials.ContainsKey(specialF)||specialD==specialF)throw new InvalidOperationException("Special loadout needs two distinct registered definitions.");
            State.Specials=new[]{MakeSpecial(specialD),MakeSpecial(specialF)};
            State.DodgeCharges=Content.Config.DodgeCapacity;
            spawnTimer=Content.Config.SpawnInitialDelay;
            State.NextXp=(int)Content.KillsForNextLevel(1);State.Player.Position=new Vector2(Content.Config.ArenaWidth/2,Content.Config.ArenaHeight/2);
            if(Navigation.HasObstacles)State.Player.Position=Navigation.ResolvePosition(State.Player.Position,Content.Config.PlayerRadius);
            RebuildStats();State.Player.Hp=State.Player.MaxHp;
        }
        SpecialState MakeSpecial(string id) {var d=Content.Specials[id];return new SpecialState{Id=id,Name=d.Name,Cooldown=d.Cooldown};}
        public EnemyState SpawnEnemy(string kind,Vector2 position,int level=1)
        {
            // The web generator consumes edge and kind rolls even for explicit spawns.
            Random01();Random01();return CreateEnemy(kind,position,level);
        }
        EnemyState CreateEnemy(string kind,Vector2 position,int level,bool edgeSpawn=false)
        {
            var data=Content.Enemies[kind];var b=Content.Config;float growth=Mathf.Max(0,level-1);
            if(Navigation.HasObstacles)position=edgeSpawn?Navigation.ResolveEdgePosition(position,data.Radius):Navigation.ResolvePosition(position,data.Radius);
            var enemy=new EnemyState{Id=++serial,Kind=kind,Level=level,Position=position,Hp=data.Hp*(1+growth*b.EnemyHpGrowth),MaxHp=data.Hp*(1+growth*b.EnemyHpGrowth),Radius=data.Radius,Speed=data.Speed*(1+growth*b.EnemySpeedGrowth),Damage=data.Damage*(1+growth*b.EnemyDamageGrowth),AttackTimer=b.EnemyInitialDelay+Random01()*b.EnemyDelayJitter,Pattern=data.Patterns[0]};
            State.Enemies.Add(enemy);if(IsElite(kind))eliteActive=true;return enemy;
        }
        public bool IsElite(string enemyId)
        {
            foreach(var entry in Content.EliteSpawnPool)if(entry.EnemyId==enemyId)return true;
            return false;
        }
        string SelectSpawn(SpawnEntryData[] pool,float roll)
        {
            float total=0;foreach(var entry in pool)if(!entry.FillRemainder)total+=entry.ChanceAt(State.Time);
            float scale=total>1?1/total:1;
            // Reverse order preserves the original caster/shooter/remainder probability bands.
            for(int i=pool.Length-1;i>=0;i--){var entry=pool[i];if(entry.FillRemainder)continue;roll-=entry.ChanceAt(State.Time)*scale;if(roll<0)return entry.EnemyId;}
            foreach(var entry in pool)if(entry.FillRemainder)return entry.EnemyId;
            throw new InvalidOperationException("Spawn pool has no remainder entry.");
        }
        void TickSpawns(float dt)
        {
            spawnTimer-=dt;if(spawnTimer>0)return;
            var stages=Content.SpawnStages;int index=0;while(index<stages.Length-1&&State.Time>=stages[index+1].Time)index++;
            var from=stages[index];var to=stages[Math.Min(index+1,stages.Length-1)];float fraction=to.Time==from.Time?0:Mathf.Clamp01((State.Time-from.Time)/(to.Time-from.Time));
            float batch=Mathf.Lerp(from.Batch,to.Batch,fraction);int cap=Mathf.FloorToInt(Mathf.Lerp(from.Cap,to.Cap,fraction));
            int count=Mathf.FloorToInt(batch)+(Random01()<batch%1?1:0);count=Math.Min(count,Math.Max(0,cap-State.Enemies.FindAll(e=>e.Hp>0).Count));
            var b=Content.Config;spawnTimer=Mathf.Lerp(from.Interval,to.Interval,fraction)*(b.SpawnJitterMin+Random01()*b.SpawnJitterWidth);
            for(int i=0;i<count;i++)
            {
                int edge=Mathf.FloorToInt(Random01()*4);float positionRoll=Random01();float along=b.EnemyCornerInset+positionRoll*((edge<2?b.ArenaHeight:b.ArenaWidth)-2*b.EnemyCornerInset);
                var position=edge<2?new Vector2(edge==0?b.EnemyEdgeInset:b.ArenaWidth-b.EnemyEdgeInset,along):new Vector2(along,edge==2?b.EnemyEdgeInset:b.ArenaHeight-b.EnemyEdgeInset);
                if(Content.SpawnPoints.Length>0)position=Content.SpawnPoints[Mathf.Min(Content.SpawnPoints.Length-1,Mathf.FloorToInt(positionRoll*Content.SpawnPoints.Length))];
                CreateEnemy(SelectSpawn(Content.NormalSpawnPool,Random01()),position,State.Level,Navigation.HasObstacles);
            }
        }
        void CheckLevelUp()
        {
            if(State.Phase!=RunPhase.Playing||State.Xp<State.NextXp)return;
            State.Xp-=State.NextXp;State.Level++;State.NextXp=(int)Content.KillsForNextLevel(State.Level);RebuildStats();RestoreHealth(State.Player.MaxHp-State.Player.Hp);
            State.SpecialReward=false;RollChoices(false);EnterPhase(RunPhase.LevelUp);Effect(State.Player.Position,"levelup",.85f,120,"#ffd46b");
            if(State.Level%Content.Config.EliteEveryLevels==0)pendingElites.Enqueue(State.Level);
            StartPendingElite();
        }
        void StartPendingElite()
        {
            if(!eliteActive&&pendingElites.Count>0)
            {
                pendingElites.Dequeue();
                Random01();string enemyId=SelectSpawn(Content.EliteSpawnPool,Random01());
                var p=State.Player.Position;var e=CreateEnemy(enemyId,new Vector2(Mathf.Clamp(p.x+Content.EliteOffsetX,Content.EliteInset,Content.Config.ArenaWidth-Content.EliteInset),Mathf.Clamp(p.y+Content.EliteOffsetY,Content.EliteInset,Content.Config.ArenaHeight-Content.EliteInset)),State.Level,Navigation.HasObstacles);e.AttackTimer=Content.Config.EliteInitialDelay;
                Notice("Elite appeared · Normal enemy spawning paused");
            }
        }
        string RollTier() {float roll=Random01();return roll<Content.Rewards.SilverChance?"silver":roll<Content.Rewards.SilverChance+Content.Rewards.GoldChance?"gold":"prism";}
        void RollChoices(bool special)
        {
            State.AugmentChoices.Clear();
            if(special)
            {
                var pool=new List<AugmentData>(Content.SpecialAugments);pool.RemoveAll(a=>a.Weight<=0||(!string.IsNullOrEmpty(a.CharacterId)&&a.CharacterId!=State.Character));
                for(int i=0;i<Content.Rewards.Choices&&pool.Count>0;i++) {float total=0;foreach(var a in pool)total+=a.Weight;float roll=Random01()*total;int pick=pool.Count-1;for(int n=0;n<pool.Count;n++){roll-=pool[n].Weight;if(roll<0){pick=n;break;}}var item=pool[pick];pool.RemoveAt(pick);State.AugmentChoices.Add(new AugmentChoice{Id=item.Id,Title=item.Title,Description=item.Description,Tier="special",Kind="passive"});}
            }
            else {string tier=RollTier();var excluded=new HashSet<string>();for(int i=0;i<Content.Rewards.Choices;i++){var choice=NormalChoice(tier,excluded);if(choice==null)break;State.AugmentChoices.Add(choice);excluded.Add(choice.Id.Substring(choice.Id.IndexOf(':')+1));}}
        }
        AugmentChoice NormalChoice(string tier,HashSet<string> excluded)
        {
            var pool=new List<NormalAugmentData>();float total=0;foreach(var a in Content.NormalAugments)if(a.Weight>0&&!excluded.Contains(a.Id)){pool.Add(a);total+=a.Weight;}
            if(pool.Count==0)return null;float roll=Random01()*total;var selected=pool[pool.Count-1];foreach(var item in pool){roll-=item.Weight;if(roll<0){selected=item;break;}}
            float value=tier=="silver"?selected.Silver:tier=="gold"?selected.Gold:selected.Prism;
            bool haste=IsHasteScope(selected.StatOrScope),percent=selected.Operation==AugmentOperation.Percent||selected.StatOrScope=="critChance"||selected.StatOrScope=="lifesteal";
            return new AugmentChoice{Id=tier+":"+selected.Id,Title=selected.Title,Tier=tier,Kind="passive",Stat=selected.StatOrScope,Value=value,Operation=selected.Operation==AugmentOperation.Add?"add":"percent",Description=haste?selected.StatOrScope.ToUpperInvariant()+" skill haste +"+value+" (QWE only)":selected.Title+" +"+(percent?(value*100).ToString("0.#")+"%":value.ToString("0.#"))};
        }
        public bool RerollAugment(int index)
        {
            if(State.Phase!=RunPhase.Augment||State.SpecialReward||index<0||index>=State.AugmentChoices.Count)return false;
            var old=State.AugmentChoices[index];if(old.Rerolled)return false;
            string tier=old.Tier;if(tier!="prism"&&Random01()<Content.Rewards.RerollPromotion)tier=tier=="silver"?"gold":"prism";
            var excluded=new HashSet<string>();foreach(var a in State.AugmentChoices)excluded.Add(a.Id.Substring(a.Id.IndexOf(':')+1));
            var replacement=NormalChoice(tier,excluded);if(replacement==null)return false;replacement.Rerolled=true;State.AugmentChoices[index]=replacement;return true;
        }
        public bool ChooseAugment(int index)
        {
            if(State.Phase!=RunPhase.Augment||index<0||index>=State.AugmentChoices.Count)return false;
            var item=State.AugmentChoices[index];
            if(item.Tier=="special") {foreach(var data in Content.SpecialAugments)if(data.Id==item.Id){var changed=new HashSet<int>();foreach(var effect in data.Effects){ApplyEffect(effect);if(!string.IsNullOrEmpty(effect.SkillId)){int slot=Array.FindIndex(State.Skills,s=>s.Id==effect.SkillId);if(slot>=0)changed.Add(slot);}}foreach(int slot in changed)State.Skills[slot].Level++;break;}}
            else ApplyEffect(new AugmentEffectData(IsHasteScope(item.Stat)?AugmentEffectKind.Haste:AugmentEffectKind.Stat,item.Stat,item.Operation=="add"?AugmentOperation.Add:AugmentOperation.Percent,item.Value));
            RebuildStats();State.History.Add(item.Title);State.AugmentChoices.Clear();EnterPhase(RunPhase.Playing);
            if(State.SpecialReward)
            {
                State.SpecialReward=false;eliteActive=false;spawnTimer=Content.Config.SpawnInitialDelay;
                State.Projectiles.RemoveAll(s=>s.Hostile);State.Zones.RemoveAll(z=>z.Hostile);State.Enemies.RemoveAll(e=>e.Hp<=0);
                if(deferredNormal!=null){State.AugmentChoices=deferredNormal;deferredNormal=null;EnterPhase(RunPhase.AugmentEnter);return true;}
            }
            StartPendingElite();Notice(item.Title+" applied");CheckLevelUp();return true;
        }
        void ApplyEffect(AugmentEffectData effect)
        {
            if(effect.Kind==AugmentEffectKind.Stat||effect.Kind==AugmentEffectKind.Haste)
            {
                var target=effect.Kind==AugmentEffectKind.Haste?hasteBonuses:bonuses;target.TryGetValue(effect.StatOrScope,out var bonus);if(effect.Operation==AugmentOperation.Add)bonus.x+=effect.Value;else bonus.y+=effect.Value;target[effect.StatOrScope]=bonus;return;
            }
            int slot=Array.FindIndex(State.Skills,s=>s.Id==effect.SkillId);if(slot<0)return;
            if(effect.Kind==AugmentEffectKind.Repeat)State.Skills[slot].Repeats+=(int)effect.Value;
            else if(effect.Kind==AugmentEffectKind.AddEffect)State.Skills[slot].AddedEffects.Add(effect.InjectedEffect);
            else State.Skills[slot].Patches.Add(new SkillPatchState{Path=effect.Path,Operation=effect.Operation==AugmentOperation.Add?"add":"multiply",Value=effect.Value});
        }
        bool IsHasteScope(string scope)=>scope=="all"||scope=="q"||scope=="w"||scope=="e"||scope=="r"||Content.Skills.ContainsKey(scope);
        float Stat(string id,float baseValue) {bonuses.TryGetValue(id,out var b);return (baseValue+b.x)*(1+b.y);}
        void RebuildStats()
        {
            var b=Content.Characters[State.Character].Stats;var c=Content.Config;var p=State.Player;int n=State.Level-1;float previous=p.MaxHp;
            p.Damage=Stat("damage",b.Damage*(1+n*c.DamageGrowth));p.Armor=Stat("armor",b.Armor);p.Speed=Stat("speed",b.Speed*(1+n*c.SpeedGrowth));p.AttackSpeed=Stat("attackSpeed",b.AttackSpeed*(1+n*c.AttackSpeedGrowth));p.MaxHp=Stat("maxHp",b.Hp*(1+n*c.HpGrowth));p.Range=Stat("range",b.Range);p.HealthRegen=Stat("healthRegen",b.HealthRegen);p.Lifesteal=Stat("lifesteal",b.Lifesteal);p.CritChance=Mathf.Min(1,Stat("critChance",c.CritChance));p.CritMultiplier=Stat("critMultiplier",c.CritMultiplier);p.EliteDamage=Stat("eliteDamage",0);p.Hp=Mathf.Min(p.MaxHp,p.Hp+Mathf.Max(0,p.MaxHp-previous));
            hasteBonuses.TryGetValue("all",out var all);for(int i=0;i<State.Skills.Length;i++){var skill=State.Skills[i];if(Content.Skills[skill.Id].Ultimate){skill.Haste=0;continue;}hasteBonuses.TryGetValue(i==0?"q":i==1?"w":i==2?"e":"r",out var own);hasteBonuses.TryGetValue(skill.Id,out var specific);skill.Haste=(all.x+own.x+specific.x)*(1+all.y+own.y+specific.y);}
            p.ChainTargets=Mathf.Max(0,Mathf.FloorToInt(Stat("chainTargets",0)));
        }
        float AttackRange => State.Player.Range*BuffModifier(b=>b.AttackRange);
    }
}


