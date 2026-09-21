using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using SurvivalLegend.Data;
using SurvivalLegend.World;
using Object=UnityEngine.Object;

namespace SurvivalLegend.Editor.FX
{
    /// <summary>Creates missing editable FX assets, then binds only presentation references.</summary>
    public static class SkillFxBuilder
    {
        public const string CatalogPath="Assets/SurvivalLegend/Resources/FX/SkillFxCatalog.asset";
        const string Profiles="Assets/SurvivalLegend/Resources/FX/Profiles";
        const string Prefabs="Assets/SurvivalLegend/Prefabs/FX";
        const string WorldPrefabs="Assets/SurvivalLegend/Prefabs/World";
        static Material glow,spark,slash,vortex;
        enum Style { Burst, Aura, Shield, Spin, Slam, Frost, Blink, Overload, Volley, Pierce, Rain, Heal, Haste, Pull, Lightning, LevelUp }

        [MenuItem("Survival Legend/FX/Apply Skill FX to Current Scene")]
        public static void ApplyToCurrentScene()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Apply FX in Edit Mode.");
            var world=Object.FindAnyObjectByType<WorldPresenter>(FindObjectsInactive.Include);
            var game=Object.FindAnyObjectByType<SurvivorGame>(FindObjectsInactive.Include);
            if(world==null||game==null)throw new InvalidOperationException("Open the authored Survival Legend scene first.");
            EnsureAssets();Configure(world,game);
            EditorSceneManager.MarkSceneDirty(world.gameObject.scene);EditorSceneManager.SaveScene(world.gameObject.scene);AssetDatabase.SaveAssets();
        }
        public static void Configure(WorldPresenter world,SurvivorGame game)
        {
            var catalog=AssetDatabase.LoadAssetAtPath<SkillFxCatalog>(CatalogPath);
            if(catalog==null){EnsureAssets();catalog=AssetDatabase.LoadAssetAtPath<SkillFxCatalog>(CatalogPath);}
            var presenter=world.GetComponent<SkillFxPresenter>();if(presenter==null)presenter=world.gameObject.AddComponent<SkillFxPresenter>();
            var root=world.transform.Find("ParticleEffects");if(root==null){root=new GameObject("ParticleEffects").transform;root.SetParent(world.transform,false);}
            presenter.EditorSet(game,catalog,root);world.EditorSetFx(presenter);EditorUtility.SetDirty(presenter);EditorUtility.SetDirty(world);
        }
        public static void EnsureAssets()
        {
            Directory.CreateDirectory(Profiles);Directory.CreateDirectory(Prefabs);AssetDatabase.Refresh();
            FxTextureBuilder.EnsureAssets();glow=FxTextureBuilder.AdditiveMaterial;
            spark=Material("SparkAdditive",FxTextureBuilder.SparkStreakPath);
            slash=Material("SlashAdditive",FxTextureBuilder.CrescentSlashPath);
            vortex=Material("VortexAdditive",FxTextureBuilder.VortexSoftPath);
            var catalog=AssetDatabase.LoadAssetAtPath<SkillFxCatalog>(CatalogPath);
            if(catalog==null){catalog=ScriptableObject.CreateInstance<SkillFxCatalog>();AssetDatabase.CreateAsset(catalog,CatalogPath);}
            var profiles=new List<FxProfile>(catalog.Profiles??Array.Empty<FxProfile>());
            var database=AssetDatabase.LoadAssetAtPath<GameDatabase>("Assets/Resources/Data/SurvivalLegendGameDatabase.asset");
            var content=database!=null?database.BuildSnapshot():GameContentSnapshot.CreateDefault();
            var used=new HashSet<string>();
            foreach(var character in content.Characters.Values)foreach(string id in character.Skills)
            {
                if(!used.Add(id))continue;var skill=content.Skills[id];Style style=SkillStyle(skill.Animation);
                Add(profiles,id,Parse(skill.Color),style);
            }
            foreach(var special in content.Specials.Values)
                Add(profiles,special.Id,Parse(special.Color),special.Kind=="heal"?Style.Heal:special.Kind=="haste"?Style.Haste:special.Kind=="shield"?Style.Shield:Style.Pull);
            Add(profiles,"event.levelup",new Color(1,.82f,.32f),Style.LevelUp);
            Add(profiles,"event.ring",new Color(.8f,.95f,.7f),Style.Burst);
            Add(profiles,"event.slash",new Color(1,.78f,.4f),Style.Spin);
            Add(profiles,"event.lightning",new Color(.3f,.77f,1),Style.Lightning);
            Add(profiles,"event.dodge",new Color(.55f,.84f,.79f),Style.Haste);
            Add(profiles,"event.move",new Color(.6f,.85f,.54f),Style.Burst);
            Add(profiles,"zone.rain",new Color(.85f,1,.68f),Style.Rain);
            Add(profiles,"zone.spin",new Color(1,.79f,.42f),Style.Spin);
            Add(profiles,"zone.sword",new Color(1,.82f,.47f),Style.Slam);
            Add(profiles,"zone.sword.impact",new Color(1,.9f,.62f),Style.Frost);
            Add(profiles,"zone.blackhole",new Color(.63f,.33f,1),Style.Pull);
            Add(profiles,"zone.hazard",new Color(1,.25f,.24f),Style.Frost);
            catalog.Profiles=profiles.ToArray();EditorUtility.SetDirty(catalog);
            RepairVelocityCurveModes();UpgradeProjectile();UpgradeZone();AssetDatabase.SaveAssets();
        }
        /// <summary>Repairs the generated XY random velocity/Z constant mismatch without changing motion or artist curves.</summary>
        public static int RepairVelocityCurveModes()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Repair FX in Edit Mode.");
            int repaired=0;
            foreach(string guid in AssetDatabase.FindAssets("t:Prefab",new[]{Prefabs}))
            {
                string path=AssetDatabase.GUIDToAssetPath(guid);var root=PrefabUtility.LoadPrefabContents(path);bool changed=false;
                try
                {
                    foreach(var system in root.GetComponentsInChildren<ParticleSystem>(true))
                    {
                        var velocity=system.velocityOverLifetime;
                        if(!velocity.enabled||velocity.x.mode!=ParticleSystemCurveMode.TwoConstants||velocity.y.mode!=ParticleSystemCurveMode.TwoConstants||velocity.z.mode!=ParticleSystemCurveMode.Constant)continue;
                        float z=velocity.z.constant;velocity.z=new ParticleSystem.MinMaxCurve(z,z);changed=true;repaired++;
                    }
                    if(changed)PrefabUtility.SaveAsPrefabAsset(root,path);
                }
                finally{PrefabUtility.UnloadPrefabContents(root);}
            }
            return repaired;
        }
        static Style SkillStyle(string animation)
        {
            switch(animation)
            {
                case "empower":case "focus":return Style.Aura;case "guard":return Style.Shield;
                case "spin":return Style.Spin;case "slam":return Style.Slam;case "volley":return Style.Volley;
                case "pierce":return Style.Pierce;case "rain":return Style.Volley;case "fireball":return Style.Burst;
                case "frost":return Style.Frost;case "blink":return Style.Blink;case "overload":return Style.Overload;
                default:return Style.Burst;
            }
        }
        static void Add(List<FxProfile> list,string id,Color tint,Style style)
        {
            foreach(var entry in list)if(entry!=null&&entry.Id==id)return;
            string safe=id.Replace('/','-');string path=Profiles+"/"+safe+".asset";
            var profile=AssetDatabase.LoadAssetAtPath<FxProfile>(path);
            if(profile==null)
            {
                profile=ScriptableObject.CreateInstance<FxProfile>();profile.Id=id;profile.Tint=tint;
                profile.Duration=style==Style.LevelUp?.38f:style==Style.Rain?.45f:style==Style.Burst?.12f:.5f;
                profile.Tail=style==Style.LevelUp?.55f:style==Style.Rain?.14f:style==Style.Burst?.3f:.6f;
                profile.ScaleByRadius=style==Style.Frost||style==Style.Pull||style==Style.Slam||id=="zone.spin";
                profile.FollowPlayer=style==Style.Aura||style==Style.Shield||style==Style.Haste||style==Style.Overload||style==Style.LevelUp;
                profile.SustainWhileBuff=style==Style.Aura||style==Style.Shield||style==Style.Overload;
                profile.Scale=style==Style.Rain?.75f:id=="event.ring"?.32f:id=="event.dodge"?.25f:1;
                profile.Prefab=CreatePrefab(safe,style);AssetDatabase.CreateAsset(profile,path);
            }
            list.Add(profile);
        }
        static SkillFxView CreatePrefab(string id,Style style)
        {
            string path=Prefabs+"/"+id+".prefab";var saved=AssetDatabase.LoadAssetAtPath<SkillFxView>(path);if(saved!=null)return saved;
            var root=new GameObject("FX_"+id,typeof(SkillFxView));var systems=new List<ParticleSystem>();
            bool looping=style==Style.Aura||style==Style.Shield||style==Style.Overload||style==Style.Spin||style==Style.Pull||style==Style.Haste||style==Style.Slam;
            if(style==Style.Rain)
            {
                var streak=Particles(root.transform,"FallingStreak",spark,true,18,.2f,.035f,.025f);var main=streak.main;
                main.startSize3D=true;main.startSizeX=.48f;main.startSizeY=.065f;main.startSizeZ=.028f;main.startRotation=Mathf.PI*.5f;main.simulationSpace=ParticleSystemSimulationSpace.Local;
                systems.Add(streak);
                var motes=Particles(root.transform,"FallingSparks",glow,true,10,.3f,.055f,.05f);systems.Add(motes);
            }
            else
            {
                var halo=Particles(root.transform,"Glow",style==Style.Pull?vortex:glow,looping,looping?5:2,.48f,style==Style.Slam?1.25f:.65f,.06f);
                halo.transform.localScale=new Vector3(1,.42f,1);systems.Add(halo);
                int count=style==Style.Frost?30:style==Style.LevelUp?24:style==Style.Burst?8:18;
                var motes=Particles(root.transform,"Sparks",spark,looping,count,style==Style.Lightning?.18f:.65f,style==Style.LevelUp?.11f:.085f,style==Style.Shield?.46f:.28f);
                var velocity=motes.velocityOverLifetime;velocity.enabled=true;velocity.space=ParticleSystemSimulationSpace.Local;
                if(style==Style.Heal||style==Style.LevelUp||style==Style.Aura){velocity.x=new ParticleSystem.MinMaxCurve(-.12f,.12f);velocity.y=new ParticleSystem.MinMaxCurve(.3f,.85f);velocity.z=new ParticleSystem.MinMaxCurve(0,0);}
                else if(style==Style.Pull){velocity.orbitalZ=3;velocity.radial=-.25f;}
                else if(style==Style.Spin||style==Style.Overload||style==Style.Shield){velocity.orbitalZ=style==Style.Spin?7:2;velocity.radial=.04f;}
                else{velocity.x=new ParticleSystem.MinMaxCurve(-.85f,.85f);velocity.y=new ParticleSystem.MinMaxCurve(-.4f,.65f);velocity.z=new ParticleSystem.MinMaxCurve(0,0);}
                systems.Add(motes);
                if(style==Style.Spin||style==Style.Slam||style==Style.Blink||style==Style.Frost||style==Style.Pull)
                {
                    var sweep=Particles(root.transform,"Arc",style==Style.Pull?vortex:slash,looping,looping?4:2,.38f,style==Style.Slam?1.6f:1.05f,.04f);
                    var rotation=sweep.rotationOverLifetime;rotation.enabled=true;rotation.z=style==Style.Pull?-5:7;
                    sweep.transform.localScale=new Vector3(1,.48f,1);systems.Add(sweep);
                }
                if(style==Style.Pull)
                {
                    var shadow=Particles(root.transform,"DarkVortex",FxTextureBuilder.AlphaMaterial,true,3,.8f,1.15f,.025f);
                    shadow.transform.localScale=new Vector3(1,.45f,1);shadow.GetComponent<ParticleSystemRenderer>().sortingOrder=9400;
                    var dark=shadow.colorOverLifetime;var shade=new Gradient();shade.SetKeys(new[]{new GradientColorKey(new Color(.07f,.07f,.09f),0),new GradientColorKey(new Color(.12f,.10f,.15f),1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(.75f,.2f),new GradientAlphaKey(0,1)});dark.color=shade;
                    var rotation=shadow.rotationOverLifetime;rotation.enabled=true;rotation.z=-2;systems.Add(shadow);
                }
            }
            TextMesh label=null;
            if(style==Style.LevelUp)
            {
                var text=new GameObject("LevelUpLabel");text.transform.SetParent(root.transform,false);label=text.AddComponent<TextMesh>();
                label.text="LEVEL UP";label.fontSize=48;label.characterSize=.035f;label.anchor=TextAnchor.MiddleCenter;label.alignment=TextAlignment.Center;
                var font=AssetDatabase.LoadAssetAtPath<Font>("Assets/SurvivalLegend/Resources/Art/Fonts/NotoSansKR-Bold.otf");
                if(font!=null){label.font=font;label.GetComponent<MeshRenderer>().sharedMaterial=font.material;}
                label.GetComponent<MeshRenderer>().sortingOrder=10500;
            }
            root.GetComponent<SkillFxView>().EditorSet(systems.ToArray(),label);
            PrefabUtility.SaveAsPrefabAsset(root,path);Object.DestroyImmediate(root);return AssetDatabase.LoadAssetAtPath<SkillFxView>(path);
        }
        static ParticleSystem Particles(Transform parent,string name,Material material,bool loop,int amount,float lifetime,float size,float radius)
        {
            var child=new GameObject(name);child.transform.SetParent(parent,false);var system=child.AddComponent<ParticleSystem>();
            system.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=system.main;main.playOnAwake=false;main.loop=loop;main.duration=1;main.startLifetime=lifetime;main.startSpeed=0;main.startSize=size;
            main.maxParticles=64;main.simulationSpace=ParticleSystemSimulationSpace.World;main.scalingMode=ParticleSystemScalingMode.Hierarchy;main.cullingMode=ParticleSystemCullingMode.AlwaysSimulate;
            var emission=system.emission;emission.rateOverTime=loop?amount:0;
            if(!loop)emission.SetBursts(new[]{new ParticleSystem.Burst(0,(short)amount)});
            var shape=system.shape;shape.enabled=true;shape.shapeType=ParticleSystemShapeType.Circle;shape.radius=radius;shape.radiusThickness=.2f;
            var color=system.colorOverLifetime;color.enabled=true;var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(.8f,.08f),new GradientAlphaKey(0,1)});color.color=gradient;
            var growth=system.sizeOverLifetime;growth.enabled=true;growth.size=new ParticleSystem.MinMaxCurve(1,new AnimationCurve(new Keyframe(0,.35f),new Keyframe(.2f,1),new Keyframe(1,.1f)));
            var renderer=system.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=material;renderer.renderMode=ParticleSystemRenderMode.Billboard;renderer.sortingOrder=9500;
            system.useAutoRandomSeed=false;system.randomSeed=1;return system;
        }
        static Material Material(string name,string texture)
        {
            string path=FxTextureBuilder.ArtFolder+"/"+name+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);if(material!=null)return material;
            material=new Material(FxTextureBuilder.AdditiveMaterial){name=name};material.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(texture);AssetDatabase.CreateAsset(material,path);return material;
        }
        static Color Parse(string hex){ColorUtility.TryParseHtmlString(hex,out var color);return color;}
        static void UpgradeProjectile()
        {
            string path=WorldPrefabs+"/Projectile.prefab";if(!File.Exists(path))return;
            var importer=AssetImporter.GetAtPath(FxTextureBuilder.SoftGlowPath) as TextureImporter;
            if(importer!=null&&(importer.textureType!=TextureImporterType.Sprite||importer.spriteImportMode!=SpriteImportMode.Single))
            {
                importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
                importer.spritePixelsPerUnit=100;importer.alphaIsTransparency=true;importer.SaveAndReimport();
            }
            var glowSprite=AssetDatabase.LoadAssetAtPath<Sprite>(FxTextureBuilder.SoftGlowPath);
            var root=PrefabUtility.LoadPrefabContents(path);
            try
            {
                foreach(string name in new[]{"Head","Glow"})
                {
                    var child=root.transform.Find(name);var renderer=child!=null?child.GetComponent<SpriteRenderer>():null;
                    if(renderer==null||glowSprite==null)continue;
                    string current=renderer.sprite!=null?AssetDatabase.GetAssetPath(renderer.sprite):string.Empty;
                    if(string.IsNullOrEmpty(current)||current.EndsWith("/World/white.png",StringComparison.Ordinal)||current.EndsWith("/World/ellipse.png",StringComparison.Ordinal)||current==FxTextureBuilder.SoftGlowPath)
                        renderer.sprite=glowSprite;
                }
                var trailObject=root.transform.Find("ParticleTrail");TrailRenderer trail;
                if(trailObject==null){trailObject=new GameObject("ParticleTrail").transform;trailObject.SetParent(root.transform,false);trail=trailObject.gameObject.AddComponent<TrailRenderer>();trail.sharedMaterial=glow;trail.time=10000;trail.emitting=false;trail.minVertexDistance=.01f;trail.widthMultiplier=.025f;trail.sortingOrder=9499;trail.numCapVertices=2;}
                else trail=trailObject.GetComponent<TrailRenderer>();
                var sparks=root.transform.Find("FlightParticles");var system=sparks!=null?sparks.GetComponent<ParticleSystem>():Particles(root.transform,"FlightParticles",glow,true,18,.3f,.06f,.02f);
                root.GetComponent<ProjectileView>().EditorSetParticles(trail,system);PrefabUtility.SaveAsPrefabAsset(root,path);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        static void UpgradeZone()
        {
            string path=WorldPrefabs+"/Zone.prefab";if(!File.Exists(path))return;var root=PrefabUtility.LoadPrefabContents(path);
            try{root.GetComponent<ZoneView>().EditorEnableParticles(true);PrefabUtility.SaveAsPrefabAsset(root,path);}finally{PrefabUtility.UnloadPrefabContents(root);}
        }
    }
}
