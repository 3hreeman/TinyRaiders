#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using SurvivalLegend.Data;

namespace SurvivalLegend.Editor.Data
{
    /// <summary>Creates the initial editable source-derived assets once. Existing databases are never repaired or overwritten.</summary>
    public static class GameDatabaseSeed
    {
        public const string Root = "Assets/Resources/Data/SurvivalLegend";
        const string DatabasePath = "Assets/Resources/Data/SurvivalLegendGameDatabase.asset";

        [MenuItem("Survival Legend/Data/Ensure Game Database")]
        public static GameDatabase EnsureDatabase()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameDatabase>(DatabasePath);
            if (existing != null) return existing;
            EnsureFolder("Assets/Resources/Data"); EnsureFolder(Root);
            var database = ScriptableObject.CreateInstance<GameDatabase>(); database.name = "SurvivalLegendGameDatabase";
            AssetDatabase.CreateAsset(database, DatabasePath);
            SeedNewDatabase(database);
            EditorUtility.SetDirty(database); AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
            return database;
        }

        [MenuItem("Survival Legend/Data/Repair Blank Initial Seed")]
        static void RepairBlankInitialSeedMenu()
        {
            var database=AssetDatabase.LoadAssetAtPath<GameDatabase>(DatabasePath);
            if(database==null) { EditorUtility.DisplayDialog("Repair blank seed","No GameDatabase exists. Use Ensure Game Database instead.","OK"); return; }
            if(!IsBlankInitialSeed(database)) { EditorUtility.DisplayDialog("Repair blank seed","Repair is blocked: this is not the known all-blank initial seed shape, so no authored content was changed.","OK"); return; }
            if(!EditorUtility.DisplayDialog("Repair blank initial seed","Restore exact legacy baseline fields into the existing blank assets? This keeps their GUIDs and is only safe for the detected all-blank initial seed.","Restore baseline","Cancel")) return;
            RepairBlankInitialSeedAssets();
        }

        /// <summary>Explicit recovery only. Throws unless every expected initial asset is blank, avoiding edits to authored data.</summary>
        public static GameDatabase RepairBlankInitialSeedAssets()
        {
            var database=AssetDatabase.LoadAssetAtPath<GameDatabase>(DatabasePath);
            if(database==null) throw new InvalidOperationException("No GameDatabase exists; call EnsureDatabase instead.");
            if(!IsBlankInitialSeed(database)) throw new InvalidOperationException("Repair is blocked because this is not the known all-blank initial seed shape.");
            SeedNewDatabase(database); MarkDirty(database); AssetDatabase.SaveAssets(); AssetDatabase.Refresh(); return database;
        }

        static void SeedNewDatabase(GameDatabase db)
        {
            db.Characters.Clear(); db.Skills.Clear(); db.Specials.Clear(); db.Enemies.Clear(); db.NormalAugments.Clear(); db.SpecialAugments.Clear(); db.SpawnStages.Clear(); db.NormalSpawnPool.Clear(); db.EliteSpawnPool.Clear();
            var skills = new Dictionary<string, SkillDefinition>(StringComparer.Ordinal);
            foreach (var pair in SurvivalLegendCatalog.Skills) { var source = pair.Value; var item = Create<SkillDefinition>("Skills", source.Id); item.Id=source.Id; item.DisplayName=source.Name; item.Icon=source.Icon; item.Color=source.Color; item.Description=source.Description; item.Animation=source.Animation; item.Cooldown=source.Cooldown; item.Ultimate=source.Ultimate; item.Targeting=Targeting(source.Targeting); item.Effects=Effects(source.Effects); skills.Add(item.Id,item); db.Skills.Add(item); }
            var characters = new Dictionary<string, CharacterDefinition>(StringComparer.Ordinal);
            foreach (var pair in SurvivalLegendCatalog.Characters) { var source=pair.Value; var item=Create<CharacterDefinition>("Characters",source.Id); item.Id=source.Id;item.DisplayName=source.Name;item.Subtitle=source.Subtitle;item.Traits=source.Traits;item.Color=source.Color;item.BasicKind=source.BasicKind;item.BasicWeapon=source.BasicWeapon;item.Stats=Stats(source.Stats);item.Skills=Links(source.Skills,skills);item.AnimationStride=source.AnimationStride;item.AttackDuration=source.AttackDuration;item.ChainRange=source.ChainRange;item.ChainRatios=Copy(source.ChainRatios);characters.Add(item.Id,item);db.Characters.Add(item); }
            var specials = new Dictionary<string, SpecialDefinition>(StringComparer.Ordinal);
            foreach (var pair in SurvivalLegendCatalog.Specials) {var source=pair.Value;var item=Create<SpecialDefinition>("Specials",source.Id);item.Id=source.Id;item.DisplayName=source.Name;item.Icon=source.Icon;item.Color=source.Color;item.Description=source.Description;item.Kind=source.Kind;item.Cooldown=source.Cooldown;item.Duration=source.Duration;item.MissingHealthRatio=source.MissingHealthRatio;item.SpeedMultiplier=source.SpeedMultiplier;item.MaxHealthRatio=source.MaxHealthRatio;item.Range=source.Range;item.Radius=source.Radius;item.PullSpeed=source.PullSpeed;specials.Add(item.Id,item);db.Specials.Add(item);}
            var enemies = new Dictionary<string, EnemyDefinition>(StringComparer.Ordinal);
            foreach (var pair in SurvivalLegendCatalog.Enemies) {var source=pair.Value;var item=Create<EnemyDefinition>("Enemies",source.Id);item.Id=source.Id;item.DisplayName=source.Name;item.Category=source.Category;item.Hp=source.Hp;item.Speed=source.Speed;item.Radius=source.Radius;item.Damage=source.Damage;item.AttackRange=source.AttackRange;item.Windup=source.Windup;item.Patterns=Links(source.Patterns,skills);enemies.Add(item.Id,item);db.Enemies.Add(item);}
            foreach(var source in SurvivalLegendCatalog.NormalAugments) {var item=Create<NormalAugmentDefinition>("NormalAugments",source.Id);item.Id=source.Id;item.Title=source.Title;item.Icon=source.Icon;item.StatOrScope=source.StatOrScope;item.Operation=source.Operation;item.Silver=source.Silver;item.Gold=source.Gold;item.Prism=source.Prism;item.Weight=source.Weight;db.NormalAugments.Add(item);}
            foreach(var source in SurvivalLegendCatalog.SpecialAugments) {var item=Create<SpecialAugmentDefinition>("SpecialAugments",source.Id);item.Id=source.Id;item.Title=source.Title;item.Description=source.Description;item.Icon=source.Icon;item.Weight=source.Weight; if(!string.IsNullOrEmpty(source.CharacterId))item.Character=characters[source.CharacterId];item.Effects=AugmentEffects(source.Effects,skills);db.SpecialAugments.Add(item);}
            foreach(var source in SurvivalLegendCatalog.SpawnStages) db.SpawnStages.Add(new SpawnStageDefinition{Time=source.Time,Interval=source.Interval,Batch=source.Batch,Cap=source.Cap});
            db.NormalSpawnPool.Add(new SpawnEntryDefinition{Enemy=enemies["chaser"],Weight=1,FillRemainder=true});
            db.NormalSpawnPool.Add(new SpawnEntryDefinition{Enemy=enemies["shooter"],Weight=.3f,StartTime=20,RampSeconds=100});
            db.NormalSpawnPool.Add(new SpawnEntryDefinition{Enemy=enemies["caster"],Weight=.2f,StartTime=60,RampSeconds=120});
            db.EliteSpawnPool.Add(new SpawnEntryDefinition{Enemy=enemies["elite"],Weight=1,FillRemainder=true});
            db.DefaultCharacter=characters["archer"];db.DefaultSpecialD=specials["heal"];db.DefaultSpecialF=specials["haste"];
            db.Balance=Balance(SurvivalLegendCatalog.Config);db.Combat=Combat(SurvivalLegendCatalog.Combat);db.Rain=Rain(SurvivalLegendCatalog.Rain);db.Rewards=Rewards(SurvivalLegendCatalog.Rewards);db.EliteOffsetX=SurvivalLegendCatalog.EliteOffsetX;db.EliteOffsetY=SurvivalLegendCatalog.EliteOffsetY;db.EliteInset=SurvivalLegendCatalog.EliteInset;
            MarkDirty(db);
        }

        static bool IsBlankInitialSeed(GameDatabase db)
        {
            if(db.Characters==null||db.Skills==null||db.Specials==null||db.Enemies==null||db.NormalAugments==null||db.SpecialAugments==null) return false;
            if(db.Characters.Count!=SurvivalLegendCatalog.Characters.Count||db.Skills.Count!=SurvivalLegendCatalog.Skills.Count||db.Specials.Count!=SurvivalLegendCatalog.Specials.Count||db.Enemies.Count!=SurvivalLegendCatalog.Enemies.Count||db.NormalAugments.Count!=SurvivalLegendCatalog.NormalAugments.Length||db.SpecialAugments.Count!=SurvivalLegendCatalog.SpecialAugments.Length) return false;
            foreach(var item in db.Characters)if(item==null||!string.IsNullOrEmpty(item.Id))return false; foreach(var item in db.Skills)if(item==null||!string.IsNullOrEmpty(item.Id))return false; foreach(var item in db.Specials)if(item==null||!string.IsNullOrEmpty(item.Id))return false; foreach(var item in db.Enemies)if(item==null||!string.IsNullOrEmpty(item.Id))return false; foreach(var item in db.NormalAugments)if(item==null||!string.IsNullOrEmpty(item.Id))return false; foreach(var item in db.SpecialAugments)if(item==null||!string.IsNullOrEmpty(item.Id))return false;
            return true;
        }
        static void MarkDirty(GameDatabase db)
        {
            foreach(var item in db.Characters) EditorUtility.SetDirty(item); foreach(var item in db.Skills) EditorUtility.SetDirty(item); foreach(var item in db.Specials) EditorUtility.SetDirty(item); foreach(var item in db.Enemies) EditorUtility.SetDirty(item); foreach(var item in db.NormalAugments) EditorUtility.SetDirty(item); foreach(var item in db.SpecialAugments) EditorUtility.SetDirty(item); EditorUtility.SetDirty(db);
        }

        static T Create<T>(string folder,string id) where T:ScriptableObject { EnsureFolder(Root+"/"+folder);var path=Root+"/"+folder+"/"+id+".asset";var found=AssetDatabase.LoadAssetAtPath<T>(path);if(found!=null)return found;var asset=ScriptableObject.CreateInstance<T>();asset.name=id;AssetDatabase.CreateAsset(asset,path);return asset; }
        static void EnsureFolder(string path) { if(AssetDatabase.IsValidFolder(path))return;var parent=System.IO.Path.GetDirectoryName(path).Replace('\\','/');var name=System.IO.Path.GetFileName(path);if(!AssetDatabase.IsValidFolder(parent))EnsureFolder(parent);AssetDatabase.CreateFolder(parent,name); }
        static float[] Copy(float[] source){return source==null?Array.Empty<float>():(float[])source.Clone();}
        static SkillDefinition[] Links(string[] ids,Dictionary<string,SkillDefinition> map){var output=new SkillDefinition[ids.Length];for(var i=0;i<output.Length;i++)output[i]=map[ids[i]];return output;}
        static StatDefinition Stats(StatBlock d){return new StatDefinition{Hp=d.Hp,Speed=d.Speed,AttackSpeed=d.AttackSpeed,Damage=d.Damage,Range=d.Range,HealthRegen=d.HealthRegen,Lifesteal=d.Lifesteal,Armor=d.Armor};}
        static TargetingDefinition Targeting(TargetingData d){return new TargetingDefinition{Kind=d.Kind,Range=d.Range,Radius=d.Radius,Width=d.Width,Spread=d.Spread,CoreRadius=d.CoreRadius};}
        static EffectDefinition[] Effects(EffectData[] source){var output=new EffectDefinition[source==null?0:source.Length];for(var i=0;i<output.Length;i++)output[i]=EffectDefinition.From(source[i]);return output;}
        static AugmentEffectDefinition[] AugmentEffects(AugmentEffectData[] source,Dictionary<string,SkillDefinition> skills){var output=new AugmentEffectDefinition[source==null?0:source.Length];for(var i=0;i<output.Length;i++){var d=source[i];output[i]=new AugmentEffectDefinition{Kind=d.Kind,StatOrScope=d.StatOrScope,Path=d.Path,Skill=string.IsNullOrEmpty(d.SkillId)?null:skills[d.SkillId],Operation=d.Operation,Value=d.Value,InjectedEffect=EffectDefinition.From(d.InjectedEffect)};}return output;}
        static BalanceDefinition Balance(BalanceData d){return new BalanceDefinition{FixedStep=d.FixedStep,ArenaWidth=d.ArenaWidth,ArenaHeight=d.ArenaHeight,ArenaInset=d.ArenaInset,ProjectileMargin=d.ProjectileMargin,PlayerRadius=d.PlayerRadius,HitInvulnerability=d.HitInvulnerability,CritChance=d.CritChance,CritMultiplier=d.CritMultiplier,DamageGrowth=d.DamageGrowth,SpeedGrowth=d.SpeedGrowth,AttackSpeedGrowth=d.AttackSpeedGrowth,HpGrowth=d.HpGrowth,EnemyHpGrowth=d.EnemyHpGrowth,EnemyDamageGrowth=d.EnemyDamageGrowth,EnemySpeedGrowth=d.EnemySpeedGrowth,EnemyInitialDelay=d.EnemyInitialDelay,EnemyDelayJitter=d.EnemyDelayJitter,EnemyEdgeInset=d.EnemyEdgeInset,EnemyCornerInset=d.EnemyCornerInset,EnemySlowMultiplier=d.EnemySlowMultiplier,SpawnInitialDelay=d.SpawnInitialDelay,SpawnJitterMin=d.SpawnJitterMin,SpawnJitterWidth=d.SpawnJitterWidth,DodgeDistance=d.DodgeDistance,DodgeDuration=d.DodgeDuration,DodgeRecharge=d.DodgeRecharge,DodgeCapacity=d.DodgeCapacity,UltimateMaximum=d.UltimateMaximum,UltimateChargePerHit=d.UltimateChargePerHit,HastePointsPerDoubleRate=d.HastePointsPerDoubleRate,LevelUpSeconds=d.LevelUpSeconds,CardDealSeconds=d.CardDealSeconds,EliteDeathSeconds=d.EliteDeathSeconds,DeathSeconds=d.DeathSeconds,EliteInitialDelay=d.EliteInitialDelay,EliteAttackRange=d.EliteAttackRange,EliteWindup=d.EliteWindup,EliteCleaveWindup=d.EliteCleaveWindup,EliteMotion=d.EliteMotion,EliteEveryLevels=d.EliteEveryLevels,KillQuadratic=d.KillQuadratic,KillLinear=d.KillLinear,KillConstant=d.KillConstant,ArmorConstant=d.ArmorConstant};}
        static CombatTuningDefinition Combat(CombatTuningData d){return new CombatTuningDefinition{Sight=d.Sight,PlayerShotSpeed=d.PlayerShotSpeed,PlayerShotRadius=d.PlayerShotRadius,PlayerShotLifetime=d.PlayerShotLifetime,HostileShotSpeed=d.HostileShotSpeed,HostileSpeedCap=d.HostileSpeedCap,HostileSpeedGrowth=d.HostileSpeedGrowth,HostileShotLifetime=d.HostileShotLifetime,ChainDelay=d.ChainDelay,ChainRange=d.ChainRange,ExtraChainRatio=d.ExtraChainRatio,RecastDelay=d.RecastDelay,ZoneInterval=d.ZoneInterval};}
        static RainTuningDefinition Rain(RainTuningData d){return new RainTuningDefinition{Columns=d.Columns,JitterStart=d.JitterStart,JitterWidth=d.JitterWidth,RowDelay=d.RowDelay,FallTime=d.FallTime,HitRadius=d.HitRadius};}
        static RewardTuningDefinition Rewards(RewardTuningData d){return new RewardTuningDefinition{SilverChance=d.SilverChance,GoldChance=d.GoldChance,RerollPromotion=d.RerollPromotion,Choices=d.Choices};}
    }
}
#endif
