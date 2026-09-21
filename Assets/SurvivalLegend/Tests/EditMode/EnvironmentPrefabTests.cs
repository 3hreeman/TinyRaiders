using System.Linq;
using NUnit.Framework;
using SurvivalLegend.Data;
using SurvivalLegend.World;
using UnityEditor;
using UnityEngine;

namespace SurvivalLegend.Tests
{
    public sealed class EnvironmentPrefabTests
    {
        const string Root="Assets/SurvivalLegend/Prefabs/World/Environments/";
        [TestCase("grassland")][TestCase("desert")][TestCase("cave")]
        public void SavedEnvironmentHasFloorAndEditableConnectedObstacles(string id)
        {
            var map=AssetDatabase.LoadAssetAtPath<ArenaMapView>(Root+"ArenaMap-"+id+".prefab");
            Assert.That(map,Is.Not.Null);
            Assert.That(map.Decorations,Is.Not.Null);
            Assert.That(map.LogicalSize,Is.EqualTo(new Vector2(3600,3600)));
            var floorChunks=map.FloorTiles.GetComponentsInChildren<SpriteRenderer>(true).Where(sprite=>sprite.sprite!=null).ToArray();
            Assert.That(floorChunks.Length,Is.EqualTo(9));
            var floorBounds=floorChunks[0].bounds;foreach(var chunk in floorChunks.Skip(1))floorBounds.Encapsulate(chunk.bounds);
            Assert.That(floorBounds.size.x,Is.EqualTo(30).Within(.01f));
            Assert.That(floorBounds.size.y,Is.EqualTo(11.75f).Within(.01f));
            foreach(var chunk in floorChunks)Assert.That(chunk.sortingOrder,Is.LessThan(-4000),"Ground must remain below hostile warning zones.");
            var props=map.GetComponentsInChildren<EnvironmentDecoration>(true);
            var snapshot=map.BuildSnapshot(GameContentSnapshot.CreateDefault());
            Assert.That(snapshot.Config.ArenaWidth,Is.EqualTo(3600));Assert.That(snapshot.Config.ArenaHeight,Is.EqualTo(3600));
            Assert.That(snapshot.Obstacles.Count,Is.EqualTo(60));
            Assert.That(props.Count(p=>p.BlocksMovement),Is.EqualTo(snapshot.Obstacles.Count));
            Assert.That(props.Count(p=>!p.BlocksMovement),Is.GreaterThanOrEqualTo(120));
            foreach(var prop in props)
            {
                Assert.That(PrefabUtility.IsPartOfPrefabInstance(prop),Is.True,"Map props should retain reusable prefab links.");
                foreach(var sprite in prop.GetComponentsInChildren<SpriteRenderer>(true))Assert.That(sprite.sprite,Is.Not.Null,prop.name);
            }
            var nav=new ArenaNavigation(snapshot);
            float radius=snapshot.Enemies.Values.Max(e=>e.Radius);
            Assert.That(nav.IsWalkable(new Vector2(1800,1800),radius),Is.True,"Keep the player spawn clear for all actors.");
            var run=new GameSimulation(content:snapshot);
            Assert.That(Vector2.Distance(run.State.Player.Position,new Vector2(1800,1800)),Is.LessThan(.01f),"The simulation must start the player at the expanded-map center.");
            foreach(var solid in snapshot.Obstacles)Assert.That(nav.IsWalkable(solid.Position,1),Is.False);
            // All four entry corridors must connect to the central combat space, even for elites.
            foreach(var point in new[]{new Vector2(1800,80),new Vector2(3520,1800),new Vector2(1800,3520),new Vector2(80,1800)})
            {
                var current=nav.ResolvePosition(point,radius);var target=new Vector2(1800,1800);
                for(int i=0;i<500&&Vector2.Distance(current,target)>1;i++)
                {
                    var next=nav.NextWaypoint(current,target,radius);
                    current=nav.SweepMove(current,Vector2.MoveTowards(current,next,12),radius);
                    Assert.That(nav.IsWalkable(current,radius),Is.True,id+" entry path intersects a prop.");
                }
                Assert.That(Vector2.Distance(current,target),Is.LessThan(1),id+" entry corridor is disconnected.");
            }
        }
    }
}
