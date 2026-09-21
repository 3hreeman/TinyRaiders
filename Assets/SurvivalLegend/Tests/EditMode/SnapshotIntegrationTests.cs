using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using SurvivalLegend.Data;
using SurvivalLegend.World;

namespace SurvivalLegend.Tests
{
    public class SnapshotIntegrationTests
    {
        static GameContentSnapshot Copy(GameContentSnapshot source,
            IDictionary<string,CharacterData> characters=null,IDictionary<string,SkillData> skills=null,
            IDictionary<string,EnemyData> enemies=null,SpawnEntryData[] normal=null,SpawnEntryData[] elite=null,string character=null)
            =>new GameContentSnapshot(characters??new Dictionary<string,CharacterData>(source.Characters),
                skills??new Dictionary<string,SkillData>(source.Skills),new Dictionary<string,SpecialData>(source.Specials),
                enemies??new Dictionary<string,EnemyData>(source.Enemies),source.SpawnStages,source.NormalAugments,source.SpecialAugments,
                source.Config,source.Combat,source.Rain,source.Rewards,normal??source.NormalSpawnPool,elite??source.EliteSpawnPool,
                source.EliteOffsetX,source.EliteOffsetY,source.EliteInset,character??source.DefaultCharacterId,source.DefaultSpecialDId,source.DefaultSpecialFId,source.SpawnPoints);

        [Test] public void AuthoredStatEditChangesNextRunWithoutChangingActiveRun()
        {
            var source=GameContentSnapshot.CreateDefault();var baseline=source.Characters["archer"];
            var authoring=ScriptableObject.CreateInstance<CharacterDefinition>();
            var skills=new List<SkillDefinition>();
            try
            {
                authoring.Id="new-ranger";authoring.BasicKind=baseline.BasicKind;authoring.BasicWeapon=baseline.BasicWeapon;
                authoring.Stats=new StatDefinition{Hp=200,Damage=37,Speed=216,AttackSpeed=1.7f,Range=300};
                for(int i=0;i<4;i++){var skill=ScriptableObject.CreateInstance<SkillDefinition>();skill.Id=baseline.Skills[i];skills.Add(skill);authoring.Skills[i]=skill;}
                var roster=new Dictionary<string,CharacterData>(source.Characters){[authoring.Id]=authoring.ToData()};
                var first=new GameSimulation(content:Copy(source,characters:roster,character:authoring.Id));
                authoring.Stats.Damage=81;roster[authoring.Id]=authoring.ToData();
                var next=new GameSimulation(content:Copy(source,characters:roster,character:authoring.Id));
                Assert.That(first.State.Character,Is.EqualTo("new-ranger"));
                Assert.That(first.State.Player.Damage,Is.EqualTo(37));Assert.That(next.State.Player.Damage,Is.EqualTo(81));
                Assert.That(new GameSimulation().State.Player.Damage,Is.EqualTo(baseline.Stats.Damage));
            }
            finally{foreach(var skill in skills)Object.DestroyImmediate(skill);Object.DestroyImmediate(authoring);}
        }

        [Test] public void SnapshotDeepCopiesSkillArraysBeforeRun()
        {
            var source=GameContentSnapshot.CreateDefault();var run=new GameSimulation(content:source);
            source.Characters["archer"].Skills[1]="missing-after-start";
            source.Skills["bow-w"].Effects[0]=new EffectData(DeliveryKind.Area,999,radius:999);
            Assert.That(run.CastSkill(1,new Vector2(1200,720)),Is.True);
            Assert.That(run.State.Projectiles.Count,Is.EqualTo(7));
        }

        [Test] public void RenamedSkillUsesDefinitionUltimateFlagAndProjectileValues()
        {
            var source=GameContentSnapshot.CreateDefault();var c=source.Characters["archer"];
            var skillIds=(string[])c.Skills.Clone();skillIds[0]="custom-ultimate";
            var roster=new Dictionary<string,CharacterData>(source.Characters){[c.Id]=new CharacterData(c.Id,c.Name,c.Subtitle,c.Traits,c.Color,c.BasicKind,c.Stats,skillIds,c.AnimationStride,c.AttackDuration,c.BasicWeapon)};
            var skills=new Dictionary<string,SkillData>(source.Skills){[skillIds[0]]=new SkillData(skillIds[0],"Custom","","#ffffff",7,true,"",new TargetingData(TargetKind.Direction),"volley",new EffectData(DeliveryKind.Projectile,2,speed:333,range:900,radius:9,count:3))};
            var run=new GameSimulation(content:Copy(source,characters:roster,skills:skills));
            Assert.That(run.CastSkill(0,new Vector2(1100,720)),Is.False);
            run.State.Ultimate=100;Assert.That(run.CastSkill(0,new Vector2(1100,720)),Is.True);
            Assert.That(run.State.Ultimate,Is.Zero);Assert.That(run.State.Projectiles.Count,Is.EqualTo(3));
            Assert.That(run.State.Projectiles[0].Velocity.magnitude,Is.EqualTo(333).Within(.001));
            Assert.That(run.State.Projectiles[0].Charge,Is.False);
        }

        [Test] public void AddedEnemySpawnsFromPoolWithoutHardcodedCategory()
        {
            var source=GameContentSnapshot.CreateDefault();var e=source.Enemies["chaser"];
            var registry=new Dictionary<string,EnemyData>(source.Enemies){["new-mob"]=new EnemyData("new-mob","New","custom",155,12,e.Radius,23,e.AttackRange,e.Windup,e.Patterns)};
            var snapshot=Copy(source,enemies:registry,normal:new[]{new SpawnEntryData("new-mob",1,0,0,true)});
            var run=new GameSimulation(content:snapshot);
            for(int i=0;i<80;i++)run.Step(1f/60);
            Assert.That(run.State.Enemies.Count,Is.GreaterThan(0));
            foreach(var enemy in run.State.Enemies){Assert.That(enemy.Kind,Is.EqualTo("new-mob"));Assert.That(enemy.MaxHp,Is.EqualTo(155));}
        }

        [Test] public void EliteClassificationComesFromPoolMembership()
        {
            var source=GameContentSnapshot.CreateDefault();var e=source.Enemies["elite"];
            var enemies=new Dictionary<string,EnemyData>(source.Enemies){["new-boss"]=new EnemyData("new-boss","Boss","custom",e.Hp,e.Speed,e.Radius,e.Damage,e.AttackRange,e.Windup,e.Patterns)};
            var run=new GameSimulation(content:Copy(source,enemies:enemies,elite:new[]{new SpawnEntryData("new-boss",1,0,0,true)}));
            Assert.That(run.IsElite("new-boss"),Is.True);Assert.That(run.IsElite("elite"),Is.False);
            run.DamageEnemy(run.SpawnEnemy("new-boss",new Vector2(100,100)),100000);
            Assert.That(run.State.EliteKills,Is.EqualTo(1));Assert.That(run.State.Phase,Is.EqualTo(RunPhase.EliteDeath));
        }

        [Test] public void AuthoredArenaBoundsAndSpawnPointsAffectSimulation()
        {
            var spawn=new Vector2(80,90);var points=new[]{spawn};
            var run=new GameSimulation(content:GameContentSnapshot.CreateDefault().WithArena(1000,800,50,points));
            points[0]=Vector2.zero;
            Assert.That(run.State.Player.Position,Is.EqualTo(new Vector2(500,400)));
            run.MoveTo(new Vector2(4000,-100));Assert.That(run.State.Destination,Is.EqualTo(new Vector2(950,50)));
            run.Stop();for(int i=0;i<73;i++)run.Step(1f/60);
            Assert.That(run.State.Enemies.Count,Is.GreaterThan(0));
            Assert.That(Vector2.Distance(run.State.Enemies[0].Position,spawn),Is.LessThan(5));
        }

        [Test] public void DrainingVisualEventsCannotChangeCombatRandomSequence()
        {
            var left=new GameSimulation(77);var right=new GameSimulation(77);var events=new List<EffectState>();
            left.MoveTo(new Vector2(500,500));right.MoveTo(new Vector2(500,500));left.DrainVisualEvents(events);
            Assert.That(events.Count,Is.EqualTo(1));Assert.That(events[0].Id,Is.EqualTo(1));
            events[0].Position=Vector2.zero;Assert.That(left.State.Effects[0].Position,Is.EqualTo(new Vector2(500,500)));
            events.Clear();left.DrainVisualEvents(events);Assert.That(events,Is.Empty);
            for(int i=0;i<120;i++){left.Step(1f/60);right.Step(1f/60);left.DrainVisualEvents(events);events.Clear();}
            Assert.That(left.State.Seed,Is.EqualTo(right.State.Seed));Assert.That(left.State.Enemies.Count,Is.EqualTo(right.State.Enemies.Count));
            for(int i=0;i<left.State.Enemies.Count;i++){Assert.That(left.State.Enemies[i].Id,Is.EqualTo(right.State.Enemies[i].Id));Assert.That(left.State.Enemies[i].Position,Is.EqualTo(right.State.Enemies[i].Position));}
        }

        [TestCase(0,0)] [TestCase(720,720)] [TestCase(1440,1440)] [TestCase(240,1200)]
        public void ProjectionRoundTripsLogicalCoordinates(float x,float y)
        {
            var point=new Vector2(x,y);var inverse=WorldProjection.SceneToWorld(WorldProjection.WorldToScene(point));
            Assert.That(Vector2.Distance(point,inverse),Is.LessThan(.001f));
            var viewport=WorldProjection.ViewportPixels(1280,900);Assert.That(viewport,Is.EqualTo(new Rect(0,90,1280,720)));
        }
    }
}
