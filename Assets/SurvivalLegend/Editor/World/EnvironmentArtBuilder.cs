using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SurvivalLegend.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace SurvivalLegend.Editor.World
{
    /// <summary>Independent, editable biome maps and reusable hand-authored pixel props.</summary>
    public static class EnvironmentArtBuilder
    {
        public const string Art = "Assets/SurvivalLegend/Resources/Art/Environments";
        public const string Prefabs = "Assets/SurvivalLegend/Prefabs/World/Environments";
        const int Size = 64, Scale = 2;
        const float ExpandedSize = 3600;
        const float ChunkSize = 1440;

        struct Placement
        {
            public readonly string Kind;
            public readonly Vector2 Position;
            public readonly float Radius;
            public readonly float Scale;
            public Placement(string kind, float x, float y, float radius, float scale = 1)
            { Kind = kind; Position = new Vector2(x, y); Radius = radius; Scale = scale; }
        }

        static readonly Placement[] GrasslandObstacles =
        {
            new Placement("oak",230,245,55,1.05f),new Placement("oak",1150,235,55,.94f),
            new Placement("oak",230,1170,55,.96f),new Placement("oak",1170,1175,55,1.03f),
            new Placement("boulder",520,225,35,1.1f),new Placement("boulder",930,260,35,.88f),
            new Placement("boulder",215,725,35,1.02f),new Placement("boulder",1205,755,35,1.12f),
            new Placement("boulder",520,1190,35,.91f),new Placement("boulder",910,1175,35,1.08f),
            new Placement("stump",385,470,26,1.03f),new Placement("stump",1070,960,26,.94f),
        };
        static readonly Placement[] DesertObstacles =
        {
            new Placement("cactus",245,255,39,1.05f),new Placement("cactus",1150,215,39,.94f),
            new Placement("cactus",260,1175,39,1.1f),new Placement("cactus",1180,1170,39,.98f),
            new Placement("sandstone",515,225,45,1.08f),new Placement("sandstone",960,260,45,.92f),
            new Placement("sandstone",225,750,45,.9f),new Placement("sandstone",1200,760,45,1.12f),
            new Placement("sandstone",515,1185,45,.94f),new Placement("sandstone",945,1190,45,1.05f),
            new Placement("desert-rock",390,465,29,1.02f),new Placement("desert-rock",1060,955,29,.97f),
        };
        static readonly Placement[] CaveObstacles =
        {
            new Placement("basalt",230,230,47,1.1f),new Placement("basalt",1160,230,47,.97f),
            new Placement("basalt",230,1175,47,.96f),new Placement("basalt",1180,1180,47,1.07f),
            new Placement("crystal",515,235,36,1.05f),new Placement("crystal",955,270,36,.95f),
            new Placement("crystal",215,760,36,.92f),new Placement("crystal",1195,760,36,1.08f),
            new Placement("crystal",535,1175,36,1.11f),new Placement("crystal",925,1175,36,.98f),
            new Placement("basalt",390,465,47,.83f),new Placement("basalt",1060,960,47,.87f),
        };
        static readonly Vector2[] DetailPositions =
        {
            new Vector2(370,235),new Vector2(730,220),new Vector2(1105,415),new Vector2(265,425),
            new Vector2(440,610),new Vector2(1000,570),new Vector2(190,920),new Vector2(1220,990),
            new Vector2(425,985),new Vector2(740,1180),new Vector2(1020,1200),new Vector2(705,380),
            new Vector2(850,1015),new Vector2(310,1020),new Vector2(1190,480),new Vector2(555,365),
            new Vector2(920,425),new Vector2(455,845),new Vector2(990,805),new Vector2(620,1100),
            new Vector2(280,610),new Vector2(1120,665),new Vector2(755,1120),new Vector2(590,925),
        };

        [MenuItem("Survival Legend/Build Environment Art")]
        public static void EnsureAssets()
        {
            Directory.CreateDirectory(Art);
            Directory.CreateDirectory(Prefabs);
            foreach (var kind in new[] {"oak","boulder","stump","cactus","sandstone","desert-rock",
                "basalt","crystal","grass","flower","dry-grass","sand-ripple","mushroom","cave-shard"})
                EnsureProp(kind);
            EnsureShadow();
            foreach (var id in new[] {"grassland","desert","cave"})
            {
                EnsureFloor(id);
                EnsureFloorEdges(id);
                EnsureMap(id);
            }
            AssetDatabase.SaveAssets();
        }

        public static ArenaMapView LoadMapPrefab(string id)
        {
            if (id != "grassland" && id != "desert" && id != "cave")
                throw new ArgumentException("Unknown environment: " + id, nameof(id));
            return AssetDatabase.LoadAssetAtPath<ArenaMapView>(Prefabs + "/ArenaMap-" + id + ".prefab");
        }

        static void EnsureFloor(string id)
        {
            string path = Art + "/" + id + "-floor.png";
            if (!File.Exists(path)) return; // Parent supplies the three original image assets.
            AssetDatabase.ImportAsset(path);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;
            if (importer.textureType == TextureImporterType.Sprite && importer.spriteImportMode == SpriteImportMode.Single
                && importer.spritePixelsPerUnit == 100 && HasCustomAlignment(importer)
                && importer.spritePivot == new Vector2(.5f,.5f) && !importer.mipmapEnabled
                && importer.wrapMode == TextureWrapMode.Mirror) return;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100;
            SetCustomAlignment(importer);
            importer.spritePivot = new Vector2(.5f,.5f);
            importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Mirror;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        static void EnsureMap(string id)
        {
            string path = Prefabs + "/ArenaMap-" + id + ".prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            bool isExisting = existing != null;
            var root = isExisting ? PrefabUtility.LoadPrefabContents(path)
                : new GameObject("ArenaMap-" + id, typeof(ArenaMapView));
            try
            {
                var ground = Layer(root.transform,"Ground");
                var floorTiles = Layer(root.transform,"FloorTiles");
                var decorations = Layer(root.transform,"Decorations");
                var bounds = Layer(root.transform,"Bounds");
                var spawns = Layer(root.transform,"SpawnMarkers");
                var back = root.transform.Find("Backdrop");
                if (back == null)
                {
                    back = new GameObject("Backdrop",typeof(SpriteRenderer)).transform;
                    back.SetParent(root.transform,false);
                }
                var backdrop = back.GetComponent<SpriteRenderer>();
                var floor = AssetDatabase.LoadAssetAtPath<Sprite>(Art + "/" + id + "-floor.png");
                if (floor != null && !isExisting)
                {
                    backdrop.sprite = floor;
                    // Display the full source sprite, without cropping, over the authored arena rectangle.
                    back.localPosition = WorldProjection.WorldToScene(new Vector2(720,720));
                    back.localScale = new Vector3(12f / floor.bounds.size.x, 4.7f / floor.bounds.size.y, 1);
                }
                float floorTint = id == "cave" ? .86f : .78f;
                backdrop.color = new Color(floorTint, floorTint, floorTint, 1);
                backdrop.sortingOrder = -10000;
                if (decorations.childCount == 0)
                {
                    var placements = id == "grassland" ? GrasslandObstacles : id == "desert" ? DesertObstacles : CaveObstacles;
                    var props = Layer(decorations,"BlockingProps");
                    for (int i=0;i<placements.Length;i++) PlaceProp(props, placements[i], i);
                    var details = Layer(decorations,"GroundDetails");
                    string[] kinds = id == "grassland" ? new[] {"grass","flower","grass","flower"}
                        : id == "desert" ? new[] {"sand-ripple","dry-grass","sand-ripple","dry-grass"}
                        : new[] {"cave-shard","mushroom","cave-shard","mushroom"};
                    for (int i=0;i<DetailPositions.Length;i++) PlaceDetail(details,kinds[i%kinds.Length],DetailPositions[i],i);
                }
                if (root.transform.Find("Layout3600-v1") == null)
                {
                    ExpandDecorations(decorations);
                    var marker = new GameObject("Layout3600-v1");
                    marker.transform.SetParent(root.transform, false);
                }
                EnsureFloorChunks(id, floorTiles, backdrop);
                root.GetComponent<ArenaMapView>().EditorSet(backdrop,ground,floorTiles,decorations,bounds,spawns);
                root.GetComponent<ArenaMapView>().EditorSetSize(new Vector2(ExpandedSize,ExpandedSize));
                PrefabUtility.SaveAsPrefabAsset(root,path);
            }
            finally
            {
                if (isExisting) PrefabUtility.UnloadPrefabContents(root);
                else UnityEngine.Object.DestroyImmediate(root);
            }
        }

        static void ExpandDecorations(Transform decorations)
        {
            var props = Layer(decorations,"BlockingProps");
            var details = Layer(decorations,"GroundDetails");
            ExpandLayer(props,true);
            ExpandLayer(details,false);
        }

        static void ExpandLayer(Transform layer,bool blocking)
        {
            var originals=layer.Cast<Transform>().Where(t=>t.GetComponent<EnvironmentDecoration>()!=null).ToArray();
            foreach(var source in originals)
            {
                var component=source.GetComponent<EnvironmentDecoration>();
                Vector2 original=component.LogicalPosition;
                for(int quadrant=0;quadrant<4;quadrant++)
                {
                    // Four outer districts plus the retained central authored district.
                    float x=600+(original.x-720)*.65f;
                    float y=600+(original.y-720)*.65f;
                    if((quadrant&1)!=0)x=ExpandedSize-x;
                    if((quadrant&2)!=0)y=ExpandedSize-y;
                    var sourcePrefab=PrefabUtility.GetCorrespondingObjectFromSource(source.gameObject) as GameObject;
                    var clone=sourcePrefab!=null
                        ?(GameObject)PrefabUtility.InstantiatePrefab(sourcePrefab)
                        :UnityEngine.Object.Instantiate(source.gameObject);
                    clone.transform.SetParent(layer,false);
                    clone.name=source.name+"-outer-"+(quadrant+1);
                    clone.transform.localPosition=WorldProjection.WorldToScene(new Vector2(x,y));
                    clone.transform.localScale=source.localScale;
                    var cloneDecoration=clone.GetComponent<EnvironmentDecoration>();
                    cloneDecoration.EditorSet(component.Id+"-outer-"+(quadrant+1),component.Radius,blocking);
                    if(PrefabUtility.IsPartOfPrefabInstance(clone))
                    {
                        PrefabUtility.RecordPrefabInstancePropertyModifications(clone.transform);
                        PrefabUtility.RecordPrefabInstancePropertyModifications(clone.GetComponent<EnvironmentDecoration>());
                        var cloneSorting=clone.GetComponent<SortingGroup>();
                        if(cloneSorting!=null)PrefabUtility.RecordPrefabInstancePropertyModifications(cloneSorting);
                    }
                }
                source.localPosition=WorldProjection.WorldToScene(original+new Vector2(1080,1080));
                component.EditorSet(component.Id,component.Radius,component.BlocksMovement);
                if(PrefabUtility.IsPartOfPrefabInstance(source))
                {
                    PrefabUtility.RecordPrefabInstancePropertyModifications(source);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(component);
                    var sorting=source.GetComponent<SortingGroup>();
                    if(sorting!=null)PrefabUtility.RecordPrefabInstancePropertyModifications(sorting);
                }
            }
        }

        static void EnsureFloorChunks(string id,Transform floorTiles,SpriteRenderer backdrop)
        {
            var full=AssetDatabase.LoadAssetAtPath<Sprite>(Art+"/"+id+"-floor.png");
            if(full==null)return;
            float scaleX=12f/full.bounds.size.x,scaleY=4.7f/full.bounds.size.y;
            for(int y=0;y<3;y++)for(int x=0;x<3;x++)
            {
                string name="FloorChunk_"+x+"_"+y;
                var existing=floorTiles.Find(name);
                if(existing!=null)continue;
                string suffix=x==2?(y==2?"-top-left-quarter":"-left-half")
                    : y==2?"-top-half":"";
                var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(Art+"/"+id+"-floor"+suffix+".png");
                if(sprite==null)continue;
                var tile=new GameObject(name,typeof(SpriteRenderer));
                tile.transform.SetParent(floorTiles,false);
                float tileWidth=x==2?720:ChunkSize,tileHeight=y==2?720:ChunkSize;
                tile.transform.localPosition=WorldProjection.WorldToScene(new Vector2(x*ChunkSize+tileWidth*.5f,
                    y*ChunkSize+tileHeight*.5f));
                tile.transform.localScale=new Vector3(scaleX,scaleY,1);
                var renderer=tile.GetComponent<SpriteRenderer>();
                renderer.sprite=sprite;
                renderer.flipX=x==1;renderer.flipY=y==1;
                renderer.color=backdrop.color;
                renderer.sortingOrder=-10000;
            }
            backdrop.enabled=false; // Retain the original authored source sprite for inspection.
        }

        static void EnsureFloorEdges(string id)
        {
            string sourcePath=Art+"/"+id+"-floor.png";
            if(!File.Exists(sourcePath))return;
            var source=AssetDatabase.LoadAssetAtPath<Texture2D>(sourcePath);
            if(source==null)return;
            string[] suffixes={"-left-half","-top-half","-top-left-quarter"};
            bool stale=suffixes.Any(s=>!File.Exists(Art+"/"+id+"-floor"+s+".png") ||
                File.GetLastWriteTimeUtc(sourcePath)>File.GetLastWriteTimeUtc(Art+"/"+id+"-floor"+s+".png"));
            if(!stale)return;
            var copy=ReadTexture(source);
            try
            {
                ExportCrop(copy,id,"-left-half",0,0,copy.width/2,copy.height);
                ExportCrop(copy,id,"-top-half",0,copy.height/2,copy.width,copy.height/2);
                ExportCrop(copy,id,"-top-left-quarter",0,copy.height/2,copy.width/2,copy.height/2);
            }
            finally{UnityEngine.Object.DestroyImmediate(copy);}
        }
        static void ExportCrop(Texture2D source,string id,string suffix,int x,int y,int width,int height)
        {
            string path=Art+"/"+id+"-floor"+suffix+".png";
            var cropped=new Texture2D(width,height,TextureFormat.RGBA32,false);
            cropped.SetPixels(source.GetPixels(x,y,width,height));cropped.Apply(false,false);
            File.WriteAllBytes(path,cropped.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(cropped);
            EnsureFloorAssetImport(path);
        }
        static Texture2D ReadTexture(Texture2D source)
        {
            var rt=RenderTexture.GetTemporary(source.width,source.height,0,RenderTextureFormat.ARGB32);
            Graphics.Blit(source,rt);
            var previous=RenderTexture.active;RenderTexture.active=rt;
            var copy=new Texture2D(source.width,source.height,TextureFormat.RGBA32,false);
            copy.ReadPixels(new Rect(0,0,source.width,source.height),0,0);copy.Apply(false,false);
            RenderTexture.active=previous;RenderTexture.ReleaseTemporary(rt);return copy;
        }
        static void EnsureFloorAssetImport(string path)
        {
            AssetDatabase.ImportAsset(path);
            var importer=AssetImporter.GetAtPath(path) as TextureImporter;
            if(importer==null)return;
            importer.textureType=TextureImporterType.Sprite;
            importer.spriteImportMode=SpriteImportMode.Single;
            importer.spritePixelsPerUnit=100;
            SetCustomAlignment(importer);
            importer.spritePivot=new Vector2(.5f,.5f);
            importer.filterMode=FilterMode.Point;
            importer.mipmapEnabled=false;
            importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        static Transform Layer(Transform parent,string name)
        {
            var found=parent.Find(name); if(found!=null)return found;
            var child=new GameObject(name).transform; child.SetParent(parent,false); return child;
        }
        static void PlaceProp(Transform parent,Placement placement,int index)
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs+"/Prop-"+placement.Kind+".prefab");
            if(prefab==null)throw new InvalidOperationException("Missing environment prop: "+placement.Kind);
            var instance=(GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name=placement.Kind+"-"+(index+1).ToString("00");
            instance.transform.SetParent(parent,false);
            instance.transform.localPosition=WorldProjection.WorldToScene(placement.Position);
            instance.transform.localScale=Vector3.one*placement.Scale;
            var component=instance.GetComponent<EnvironmentDecoration>();
            component.EditorSet(placement.Kind+"-"+(index+1).ToString("00"),placement.Radius,true);
            PrefabUtility.RecordPrefabInstancePropertyModifications(instance.transform);
            PrefabUtility.RecordPrefabInstancePropertyModifications(component);
            var sorting = instance.GetComponent<SortingGroup>();
            if (sorting != null) PrefabUtility.RecordPrefabInstancePropertyModifications(sorting);
        }
        static void PlaceDetail(Transform parent,string kind,Vector2 position,int index)
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs+"/Detail-"+kind+".prefab");
            if(prefab==null)throw new InvalidOperationException("Missing environment detail: "+kind);
            var instance=(GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name=kind+"-detail-"+(index+1).ToString("00");
            instance.transform.SetParent(parent,false);
            instance.transform.localPosition=WorldProjection.WorldToScene(position);
            float variability = .55f + (index * 17 % 7) * .085f;
            instance.transform.localScale=Vector3.one*variability;
            PrefabUtility.RecordPrefabInstancePropertyModifications(instance.transform);
        }

        static void EnsureProp(string kind)
        {
            string spritePath=Art+"/"+kind+".png";
            if(!File.Exists(spritePath))
            {
                var canvas=new PixelCanvas(); Draw(kind,canvas);
                canvas.Export(spritePath);
            }
            var sprite=ImportSprite(spritePath,new Vector2(.5f,.08f));
            bool detail=kind=="grass"||kind=="flower"||kind=="dry-grass"||kind=="sand-ripple"||kind=="mushroom"||kind=="cave-shard";
            string prefabPath=Prefabs+"/"+(detail?"Detail-":"Prop-")+kind+".prefab";
            if(AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath)!=null)return;
            var go=new GameObject((detail?"Detail-":"Prop-")+kind,typeof(EnvironmentDecoration));
            if(!detail)go.AddComponent<SortingGroup>();
            var visual=new GameObject("Sprite",typeof(SpriteRenderer)); visual.transform.SetParent(go.transform,false);
            visual.transform.localScale=Vector3.one*(detail?.38f:.77f);
            var renderer=visual.GetComponent<SpriteRenderer>(); renderer.sprite=sprite;
            renderer.sortingOrder=detail?-5000:1;
            if(!detail)
            {
                var shadow=new GameObject("ContactShadow",typeof(SpriteRenderer));
                shadow.transform.SetParent(go.transform,false);
                shadow.transform.localScale=new Vector3(.8f,.45f,1);
                shadow.transform.localPosition=new Vector3(0,-.035f,0);
                var shadowRenderer=shadow.GetComponent<SpriteRenderer>();
                shadowRenderer.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(Art+"/contact-shadow.png");
                shadowRenderer.sortingOrder=0;
                shadow.transform.SetAsFirstSibling();
            }
            go.GetComponent<EnvironmentDecoration>().EditorSet(kind,detail?0:DefaultRadius(kind),!detail);
            PrefabUtility.SaveAsPrefabAsset(go,prefabPath); UnityEngine.Object.DestroyImmediate(go);
        }

        static float DefaultRadius(string kind)
        {
            switch(kind)
            {
                case "oak": return 55;
                case "boulder": case "crystal": return 35;
                case "stump": return 26;
                case "cactus": return 39;
                case "sandstone": case "basalt": return 45;
                case "desert-rock": return 29;
                default: return 0;
            }
        }

        static void EnsureShadow()
        {
            string path=Art+"/contact-shadow.png";
            if(!File.Exists(path))
            {
                var canvas=new PixelCanvas();
                canvas.Ellipse(32,13,23,7,new Color32(4,17,18,80));
                canvas.Ellipse(32,13,17,5,new Color32(4,17,18,58));
                canvas.Export(path);
            }
            var shadow=ImportSprite(path,new Vector2(.5f,.5f));
            // First creation may have produced prefabs before this texture existed.
            foreach(var kind in new[]{"oak","boulder","stump","cactus","sandstone","desert-rock","basalt","crystal"})
            {
                string pathPrefab=Prefabs+"/Prop-"+kind+".prefab";
                var existing=AssetDatabase.LoadAssetAtPath<GameObject>(pathPrefab);
                if(existing==null)continue;
                var child=existing.transform.Find("ContactShadow");
                if(child==null||child.GetComponent<SpriteRenderer>().sprite!=null)continue;
                var root=PrefabUtility.LoadPrefabContents(pathPrefab);
                root.transform.Find("ContactShadow").GetComponent<SpriteRenderer>().sprite=shadow;
                PrefabUtility.SaveAsPrefabAsset(root,pathPrefab);
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        static Sprite ImportSprite(string path,Vector2 pivot)
        {
            AssetDatabase.ImportAsset(path);
            var importer=AssetImporter.GetAtPath(path) as TextureImporter;
            if(importer==null)throw new InvalidOperationException("Sprite importer missing: "+path);
            if(importer.textureType!=TextureImporterType.Sprite||importer.spriteImportMode!=SpriteImportMode.Single
                ||importer.spritePixelsPerUnit!=100||!HasCustomAlignment(importer)
                ||importer.spritePivot!=pivot||importer.mipmapEnabled)
            {
                importer.textureType=TextureImporterType.Sprite;
                importer.spriteImportMode=SpriteImportMode.Single;
                importer.spritePixelsPerUnit=100;
                SetCustomAlignment(importer);
                importer.spritePivot=pivot;
                importer.alphaIsTransparency=true;
                importer.mipmapEnabled=false;
                importer.filterMode=FilterMode.Point;
                importer.textureCompression=TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        static bool HasCustomAlignment(TextureImporter importer)
        {
            var settings=new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            return settings.spriteAlignment==(int)SpriteAlignment.Custom;
        }
        static void SetCustomAlignment(TextureImporter importer)
        {
            var settings=new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment=(int)SpriteAlignment.Custom;
            importer.SetTextureSettings(settings);
        }

        static Color32 C(string hex) { ColorUtility.TryParseHtmlString(hex,out Color c); return c; }
        static void Draw(string kind,PixelCanvas p)
        {
            switch(kind)
            {
                case "oak":
                    p.Poly(C("#342c28"),new Vector2(26,9),new Vector2(39,9),new Vector2(37,36),new Vector2(28,39));
                    p.Poly(C("#79533b"),new Vector2(29,11),new Vector2(36,12),new Vector2(35,37),new Vector2(30,37));
                    p.Line(33,13,31,33,C("#bd8751"),2);
                    p.Ellipse(21,41,16,14,C("#13372f")); p.Ellipse(42,40,17,15,C("#143e34"));
                    p.Ellipse(33,50,22,13,C("#154838"));
                    p.Ellipse(21,44,13,10,C("#2d6846")); p.Ellipse(42,44,14,10,C("#30734c"));
                    p.Ellipse(32,53,17,8,C("#3e8253"));
                    p.Scatter(84,17,58,27,57,C("#78a866"),3);
                    p.Scatter(36,12,57,26,59,C("#a4c277"),2);
                    break;
                case "boulder":
                case "desert-rock":
                    bool desert=kind=="desert-rock";
                    p.Poly(C(desert?"#553a2e":"#273c40"),new Vector2(10,12),new Vector2(16,30),new Vector2(28,36),new Vector2(47,32),new Vector2(55,18),new Vector2(49,10),new Vector2(17,8));
                    p.Poly(C(desert?"#a26843":"#536d6a"),new Vector2(14,17),new Vector2(22,30),new Vector2(31,34),new Vector2(45,30),new Vector2(52,19),new Vector2(43,13),new Vector2(20,12));
                    p.Poly(C(desert?"#d1945d":"#82938a"),new Vector2(22,27),new Vector2(30,33),new Vector2(45,29),new Vector2(38,21),new Vector2(27,22));
                    p.Poly(C(desert?"#704932":"#394f51"),new Vector2(14,17),new Vector2(20,13),new Vector2(27,22),new Vector2(22,30));
                    p.Line(42,27,48,22,C(desert?"#f1b977":"#b4b9a1"),1);
                    p.Line(20,19,25,23,C("#1d3032"),1);
                    break;
                case "stump":
                    p.Poly(C("#3c3028"),new Vector2(18,12),new Vector2(44,12),new Vector2(41,28),new Vector2(21,29));
                    p.Poly(C("#8c593b"),new Vector2(21,13),new Vector2(40,13),new Vector2(38,28),new Vector2(23,27));
                    p.Line(28,12,27,24,C("#bb7950"),2); p.Line(36,12,37,21,C("#65412f"),2);
                    p.Ellipse(31,29,14,6,C("#322b27")); p.Ellipse(31,30,11,4,C("#bb8960"));
                    p.Ellipse(31,30,6,2,C("#7c563e")); p.Line(26,30,35,30,C("#e5ad75"),1);
                    break;
                case "cactus":
                    p.Line(32,13,32,54,C("#25473d"),12); p.Line(32,15,32,53,C("#448560"),8);
                    p.Line(29,18,29,51,C("#72a873"),2);
                    p.Line(28,34,17,34,C("#25473d"),9); p.Line(17,34,17,46,C("#25473d"),9);
                    p.Line(28,35,17,35,C("#4f9867"),5); p.Line(17,35,17,46,C("#4f9867"),5);
                    p.Line(37,29,48,29,C("#25473d"),9); p.Line(48,29,48,42,C("#25473d"),9);
                    p.Line(37,30,48,30,C("#5a9868"),5); p.Line(48,30,48,42,C("#5a9868"),5);
                    p.Scatter(32,13,53,13,51,C("#c7d9a0"),2);
                    break;
                case "sandstone":
                    p.Poly(C("#523a32"),new Vector2(19,9),new Vector2(44,9),new Vector2(47,18),new Vector2(43,53),new Vector2(38,59),new Vector2(22,56),new Vector2(17,49));
                    p.Poly(C("#a87351"),new Vector2(22,12),new Vector2(42,12),new Vector2(43,19),new Vector2(39,53),new Vector2(22,52));
                    p.Poly(C("#d4a16b"),new Vector2(22,52),new Vector2(28,55),new Vector2(38,54),new Vector2(42,45),new Vector2(29,48));
                    p.Poly(C("#7b553e"),new Vector2(22,12),new Vector2(29,13),new Vector2(28,48),new Vector2(22,51));
                    for(int y=20;y<=48;y+=8)p.Line(23,y,41,y+2,C("#ecba78"),1);
                    p.Line(18,45,24,42,C("#654735"),2);
                    break;
                case "basalt":
                    p.Poly(C("#101d2b"),new Vector2(17,10),new Vector2(46,10),new Vector2(49,24),new Vector2(43,56),new Vector2(35,60),new Vector2(22,53));
                    p.Poly(C("#344354"),new Vector2(20,13),new Vector2(44,13),new Vector2(45,24),new Vector2(40,54),new Vector2(31,57),new Vector2(22,51));
                    p.Poly(C("#637080"),new Vector2(24,16),new Vector2(31,17),new Vector2(29,52),new Vector2(22,47));
                    p.Poly(C("#253244"),new Vector2(33,18),new Vector2(44,15),new Vector2(41,53),new Vector2(31,57));
                    p.Line(31,44,40,39,C("#8290a0"),1); p.Line(25,29,31,25,C("#96a0a8"),1);
                    break;
                case "crystal":
                    p.Poly(C("#182837"),new Vector2(14,10),new Vector2(18,34),new Vector2(24,40),new Vector2(29,14),new Vector2(35,57),new Vector2(43,42),new Vector2(50,15),new Vector2(53,9));
                    p.Poly(C("#327b8e"),new Vector2(17,13),new Vector2(19,33),new Vector2(25,39),new Vector2(28,15),new Vector2(30,11));
                    p.Poly(C("#80d2d3"),new Vector2(25,39),new Vector2(29,15),new Vector2(35,56),new Vector2(38,19),new Vector2(44,39),new Vector2(35,12));
                    p.Poly(C("#7370b7"),new Vector2(36,13),new Vector2(43,42),new Vector2(49,16),new Vector2(51,11));
                    p.Poly(C("#caf7e9"),new Vector2(29,17),new Vector2(35,52),new Vector2(33,21));
                    p.Line(43,39,48,17,C("#c3a6ec"),2);
                    break;
                case "grass":
                    for(int i=0;i<9;i++) p.Line(25+i*2,10,19+i*3,27+(i*7)%11,C(i%3==0?"#9fbe75":i%2==0?"#4f8f57":"#28694c"),2);
                    break;
                case "flower":
                    p.Line(32,10,31,30,C("#467a4e"),2);
                    p.Line(31,18,23,23,C("#5c985d"),2);
                    p.Ellipse(31,35,8,7,C("#c6cf85"));
                    for(int i=0;i<6;i++){float a=i*Mathf.PI/3;p.Ellipse(31+Mathf.RoundToInt(Mathf.Cos(a)*5),35+Mathf.RoundToInt(Mathf.Sin(a)*5),3,3,C("#f4e2a1"));}
                    p.Ellipse(31,35,3,3,C("#c88152"));
                    break;
                case "dry-grass":
                    for(int i=0;i<9;i++)p.Line(26+i*2,10,21+i*3,24+(i*7)%10,C(i%2==0?"#b88c55":"#d2b074"),2);
                    break;
                case "sand-ripple":
                    for(int j=0;j<3;j++)p.Line(12+j*5,17+j*5,45+j*2,18+j*5,C(j==1?"#c69967":"#a77951"),2);
                    break;
                case "mushroom":
                    p.Poly(C("#536f70"),new Vector2(29,10),new Vector2(36,10),new Vector2(35,27),new Vector2(30,27));
                    p.Ellipse(32,29,16,8,C("#333958")); p.Ellipse(32,32,13,7,C("#6c6b9d"));
                    p.Ellipse(27,34,2,2,C("#c1bcdf"));p.Ellipse(36,35,2,2,C("#b8b6d5"));
                    break;
                case "cave-shard":
                    p.Poly(C("#294653"),new Vector2(20,10),new Vector2(25,27),new Vector2(30,10));
                    p.Poly(C("#6db8b8"),new Vector2(28,10),new Vector2(34,36),new Vector2(40,10));
                    p.Poly(C("#867db1"),new Vector2(38,10),new Vector2(44,25),new Vector2(48,10));
                    p.Line(33,14,34,34,C("#d6eee0"),1);
                    break;
            }
        }

        sealed class PixelCanvas
        {
            readonly Color32[] pixels=new Color32[Size*Size];
            public void Pixel(int x,int y,Color32 c)
            { if(x>=0&&x<Size&&y>=0&&y<Size)pixels[y*Size+x]=c; }
            public void Ellipse(int cx,int cy,int rx,int ry,Color32 c)
            {
                for(int y=Mathf.Max(0,cy-ry);y<=Mathf.Min(Size-1,cy+ry);y++)
                    for(int x=Mathf.Max(0,cx-rx);x<=Mathf.Min(Size-1,cx+rx);x++)
                        if(Mathf.Pow((x-cx)/(float)rx,2)+Mathf.Pow((y-cy)/(float)ry,2)<=1)Pixel(x,y,c);
            }
            public void Poly(Color32 c,params Vector2[] points)
            {
                for(int y=0;y<Size;y++)for(int x=0;x<Size;x++)
                {
                    bool inside=false;
                    for(int i=0,j=points.Length-1;i<points.Length;j=i++)
                        if(((points[i].y>y)!=(points[j].y>y)) &&
                            x<(points[j].x-points[i].x)*(y-points[i].y)/(points[j].y-points[i].y)+points[i].x)
                            inside=!inside;
                    if(inside)Pixel(x,y,c);
                }
            }
            public void Line(int x0,int y0,int x1,int y1,Color32 c,int width)
            {
                int steps=Mathf.Max(Mathf.Abs(x1-x0),Mathf.Abs(y1-y0));
                for(int i=0;i<=steps;i++)
                {
                    float t=steps==0?0:i/(float)steps;
                    int x=Mathf.RoundToInt(Mathf.Lerp(x0,x1,t)), y=Mathf.RoundToInt(Mathf.Lerp(y0,y1,t));
                    for(int dy=-width/2;dy<=width/2;dy++)for(int dx=-width/2;dx<=width/2;dx++)Pixel(x+dx,y+dy,c);
                }
            }
            public void Scatter(int count,int xmin,int xmax,int ymin,int ymax,Color32 c,int radius)
            {
                uint state=381921u;
                for(int i=0;i<count;i++)
                {
                    state=state*1664525u+1013904223u; int x=xmin+(int)(state%(uint)(xmax-xmin+1));
                    state=state*1664525u+1013904223u; int y=ymin+(int)(state%(uint)(ymax-ymin+1));
                    if(pixels[y*Size+x].a==0)continue;
                    Ellipse(x,y,radius,radius,c);
                }
            }
            public void Export(string path)
            {
                var image=new Texture2D(Size*Scale,Size*Scale,TextureFormat.RGBA32,false);
                var output=new Color32[Size*Size*Scale*Scale];
                for(int y=0;y<Size;y++)for(int x=0;x<Size;x++)
                    for(int sy=0;sy<Scale;sy++)for(int sx=0;sx<Scale;sx++)
                        output[(y*Scale+sy)*Size*Scale+x*Scale+sx]=pixels[y*Size+x];
                image.SetPixels32(output);image.Apply(false,false);
                File.WriteAllBytes(path,image.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(image);
            }
        }
    }
}
