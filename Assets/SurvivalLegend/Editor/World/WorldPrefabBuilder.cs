using System;
using System.IO;
using System.Linq;
using SurvivalLegend.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace SurvivalLegend.Editor.World
{
    /// <summary>Idempotent authoring entry point. Existing prefabs are kept for artist edits.</summary>
    public static class WorldPrefabBuilder
    {
        const string Root = "Assets/SurvivalLegend";
        const string Art = Root + "/Resources/Art";
        const string WorldArt = Art + "/World";
        const string Prefabs = Root + "/Prefabs/World";
        const int Cell = 128;
        private static readonly string[] Heroes = { "swordsman", "archer", "mage" };
        private static readonly string[] Enemies = { "chaser", "shooter", "caster", "elite" };

        [MenuItem("Survival Legend/Build World Prefabs")]
        public static void EnsureAssets()
        {
            EnsureDirectory(WorldArt); EnsureDirectory(Prefabs);
            var white = EnsureWhiteSprite();
            var ellipse = EnsureEllipseSprite();
            var ring = EnsureRingSprite();
            var lineMaterial = EnsureLineMaterial();
            var actorSets = Heroes.Select(EnsurePlayerSet).ToArray();
            var enemySets = Enemies.Select(EnsureEnemySet).ToArray();
            var baseActor = EnsureActorBase(white, ellipse, ring);
            var characterPrefabs = new ActorView[Heroes.Length];
            var enemyPrefabs = new ActorView[Enemies.Length];
            for (int i = 0; i < Heroes.Length; i++)
                characterPrefabs[i] = EnsureActorVariant(Heroes[i], true, baseActor, actorSets[i], HeroColor(Heroes[i]), .55f);
            for (int i = 0; i < Enemies.Length; i++)
                enemyPrefabs[i] = EnsureActorVariant(Enemies[i], false, baseActor, enemySets[i], EnemyColor(Enemies[i]), .58f);
            var projectile = EnsureProjectilePrefab(white, ellipse, lineMaterial);
            var zone = EnsureZonePrefab(lineMaterial);
            var effect = EnsureEffectPrefab(lineMaterial);
            var marker = EnsureAimPrefab(lineMaterial);
            EnvironmentArtBuilder.EnsureAssets();
            string dbPath = WorldArt + "/WorldVisualDatabase.asset";
            var database = AssetDatabase.LoadAssetAtPath<WorldVisualDatabase>(dbPath);
            if (database == null) { database = ScriptableObject.CreateInstance<WorldVisualDatabase>(); AssetDatabase.CreateAsset(database, dbPath); }
            var characters = Heroes.Select((id, i) => new ActorVisualDefinition
            { id = id, prefab = characterPrefabs[i], spriteSet = actorSets[i], accent = HeroColor(id), displayScale = .55f }).ToArray();
            var enemyEntries = Enemies.Select((id, i) => new ActorVisualDefinition
            { id = id, prefab = enemyPrefabs[i], spriteSet = enemySets[i], accent = EnemyColor(id), displayScale = .58f }).ToArray();
            database.EditorSet(characters, enemyEntries, projectile, zone, effect, marker);
            EditorUtility.SetDirty(database);
            SurvivalLegend.Editor.FX.SkillFxBuilder.EnsureAssets();
            AssetDatabase.SaveAssets();
        }

        public static WorldPresenter ConfigureScene(GameObject worldRoot, SurvivorGame game, Camera camera)
        {
            if (AssetDatabase.LoadAssetAtPath<WorldVisualDatabase>(WorldArt + "/WorldVisualDatabase.asset") == null)
                EnsureAssets();
            if (worldRoot == null) throw new ArgumentNullException(nameof(worldRoot));
            var database = AssetDatabase.LoadAssetAtPath<WorldVisualDatabase>(WorldArt + "/WorldVisualDatabase.asset");
            var mapPrefab = EnvironmentArtBuilder.LoadMapPrefab("grassland");
            ArenaMapView map = worldRoot.GetComponentInChildren<ArenaMapView>(true);
            if (map == null && mapPrefab != null)
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(mapPrefab.gameObject);
                instance.transform.SetParent(worldRoot.transform, false);
                map = instance.GetComponent<ArenaMapView>();
            }
            var actors = Layer(worldRoot.transform, "Actors");
            var projectiles = Layer(worldRoot.transform, "Projectiles");
            var zones = Layer(worldRoot.transform, "Zones");
            var effects = Layer(worldRoot.transform, "Effects");
            var markers = Layer(worldRoot.transform, "AimMarkers");
            var presenter = worldRoot.GetComponent<WorldPresenter>();
            if (presenter == null) presenter = worldRoot.AddComponent<WorldPresenter>();
            presenter.EditorSet(game, camera, database, map, actors, projectiles, zones, effects, markers);
            SurvivalLegend.Editor.FX.SkillFxBuilder.Configure(presenter,game);
            if (game != null) { game.ArenaMap = map; EditorUtility.SetDirty(game); }
            WorldExplorationBuilder.Configure(presenter,game,camera);
            EditorUtility.SetDirty(presenter);
            return presenter;
        }

        private static Transform Layer(Transform root, string name)
        {
            var found = root.Find(name);
            if (found != null) return found;
            var layer = new GameObject(name).transform; layer.SetParent(root, false); return layer;
        }
        private static void EnsureDirectory(string path) { Directory.CreateDirectory(path); }
        private static Color HeroColor(string id) => id == "swordsman" ? new Color(.91f, .72f, .44f) :
            id == "mage" ? new Color(.65f, .69f, 1f) : new Color(.68f, .95f, .72f);
        private static Color EnemyColor(string id) => id == "elite" ? new Color(1f, .43f, .35f) :
            id == "shooter" ? new Color(1f, .83f, .55f) : id == "caster" ? new Color(.69f, .53f, .86f) : new Color(.93f, .53f, .52f);

        private static DirectionalSpriteSet EnsurePlayerSet(string id)
        {
            string atlas = Art + "/Characters/" + id + "-sd-atlas.png";
            ImportSheet(atlas, id, 6, 8, new Vector2(.5f, .125f));
            var sprites = LoadNamedSprites(atlas, id, 6, 8);
            return SaveSet(id, sprites, 9);
        }
        private static DirectionalSpriteSet EnsureEnemySet(string id)
        {
            string atlas = WorldArt + "/" + id + "-sd-atlas.png";
            if (!File.Exists(atlas)) BakeEnemySheet(id, atlas);
            ImportSheet(atlas, id, 4, 8, new Vector2(.5f, .125f));
            var four = LoadNamedSprites(atlas, id, 4, 8);
            var six = new Sprite[48];
            for (int direction = 0; direction < 8; direction++)
            {
                for (int frame = 0; frame < 3; frame++) six[direction * 6 + frame] = four[direction * 4 + frame];
                for (int frame = 3; frame < 6; frame++) six[direction * 6 + frame] = four[direction * 4 + 3];
            }
            return SaveSet(id, six, 7);
        }
        private static DirectionalSpriteSet SaveSet(string id, Sprite[] sprites, float stride)
        {
            string path = WorldArt + "/" + id + "-directions.asset";
            var set = AssetDatabase.LoadAssetAtPath<DirectionalSpriteSet>(path);
            if (set == null) { set = ScriptableObject.CreateInstance<DirectionalSpriteSet>(); AssetDatabase.CreateAsset(set, path); }
            set.EditorSet(sprites, stride); EditorUtility.SetDirty(set); return set;
        }
        private static void ImportSheet(string path, string prefix, int columns, int rows, Vector2 pivot)
        {
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer == null) throw new InvalidOperationException("Missing atlas: " + path);
            var current = importer.spritesheet;
            bool needs = importer.textureType != TextureImporterType.Sprite || importer.spriteImportMode != SpriteImportMode.Multiple
                || current == null || current.Length != columns * rows || importer.spritePixelsPerUnit != 100;
            if (!needs)
                for (int d = 0; d < rows && !needs; d++) for (int f = 0; f < columns; f++)
                {
                    var sprite = current[d * columns + f];
                    var expected = new Rect(f * Cell, (rows - 1 - d) * Cell, Cell, Cell);
                    if (sprite.name != $"{prefix}-d{d}-f{f}" || sprite.rect != expected ||
                        Vector2.Distance(sprite.pivot, pivot) > .001f) { needs = true; break; }
                }
            if (!needs) return;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = 100;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;
            var metadata = new SpriteMetaData[columns * rows];
            for (int d = 0; d < rows; d++) for (int f = 0; f < columns; f++)
                metadata[d * columns + f] = new SpriteMetaData
                { name = $"{prefix}-d{d}-f{f}", rect = new Rect(f * Cell, (rows - 1 - d) * Cell, Cell, Cell),
                    alignment = (int)SpriteAlignment.Custom, pivot = pivot };
            importer.spritesheet = metadata;
            importer.SaveAndReimport();
        }
        private static Sprite[] LoadNamedSprites(string path, string prefix, int columns, int rows)
        {
            var all = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToDictionary(s => s.name);
            var result = new Sprite[columns * rows];
            for (int i = 0; i < result.Length; i++)
            {
                string name = $"{prefix}-d{i / columns}-f{i % columns}";
                if (!all.TryGetValue(name, out result[i])) throw new InvalidOperationException("Sprite missing: " + name);
            }
            return result;
        }
        private static void BakeEnemySheet(string id, string path)
        {
            var sheet = new Texture2D(Cell * 4, Cell * 8, TextureFormat.RGBA32, false);
            var clear = new Color32[sheet.width * sheet.height]; sheet.SetPixels32(clear);
            for (int d = 0; d < 8; d++) for (int f = 0; f < 4; f++)
            {
                var original = SurvivalLegendArt.EnemyFrame(id, d, f == 2 ? 1 : 0, f == 3);
                var copy = ReadTexture(original);
                var pixels = copy.GetPixels32();
                int offsetX = (Cell - copy.width) / 2, offsetY = 4;
                for (int y = 0; y < copy.height; y++) for (int x = 0; x < copy.width; x++)
                    sheet.SetPixel(f * Cell + offsetX + x, (7 - d) * Cell + offsetY + y, pixels[y * copy.width + x]);
                UnityEngine.Object.DestroyImmediate(copy);
                UnityEngine.Object.DestroyImmediate(original);
            }
            sheet.Apply(false, false); File.WriteAllBytes(path, sheet.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(sheet); AssetDatabase.ImportAsset(path);
        }
        private static Texture2D ReadTexture(Texture2D source)
        {
            var rt = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32);
            Graphics.Blit(source, rt);
            var previous = RenderTexture.active; RenderTexture.active = rt;
            var copy = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
            copy.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0); copy.Apply();
            RenderTexture.active = previous; RenderTexture.ReleaseTemporary(rt); return copy;
        }

        private static Sprite EnsureWhiteSprite()
        {
            string path = WorldArt + "/white.png";
            if (!File.Exists(path))
            {
                var texture = new Texture2D(100, 100, TextureFormat.RGBA32, false);
                var pixels = Enumerable.Repeat(new Color32(255, 255, 255, 255), 10000).ToArray();
                texture.SetPixels32(pixels); texture.Apply(); File.WriteAllBytes(path, texture.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(texture);
            }
            return ImportSingle(path);
        }
        private static Sprite EnsureEllipseSprite()
        {
            string path = WorldArt + "/ellipse.png";
            if (!File.Exists(path))
            {
                var texture = new Texture2D(128, 128, TextureFormat.RGBA32, false);
                var pixels = new Color32[128 * 128];
                for (int y = 0; y < 128; y++) for (int x = 0; x < 128; x++)
                {
                    float u = (x - 63.5f) / 63.5f, v = (y - 63.5f) / 63.5f;
                    float distance = u * u + v * v;
                    pixels[y * 128 + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01((1 - distance) * 5) * 255));
                }
                texture.SetPixels32(pixels); texture.Apply(); File.WriteAllBytes(path, texture.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(texture);
            }
            return ImportSingle(path);
        }
        private static Sprite EnsureRingSprite()
        {
            string path = WorldArt + "/ring.png";
            if (!File.Exists(path))
            {
                var texture = new Texture2D(128, 128, TextureFormat.RGBA32, false);
                var pixels = new Color32[128 * 128];
                for (int y = 0; y < 128; y++) for (int x = 0; x < 128; x++)
                {
                    float u = (x - 63.5f) / 63.5f, v = (y - 63.5f) / 63.5f;
                    float distance = Mathf.Sqrt(u * u + v * v);
                    pixels[y * 128 + x] = new Color32(255, 255, 255,
                        (byte)(Mathf.Clamp01((.06f - Mathf.Abs(distance - .89f)) * 25) * 255));
                }
                texture.SetPixels32(pixels); texture.Apply(); File.WriteAllBytes(path, texture.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(texture);
            }
            return ImportSingle(path);
        }
        private static Sprite ImportSingle(string path)
        {
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer.textureType != TextureImporterType.Sprite || importer.spriteImportMode != SpriteImportMode.Single)
            {
                importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 100; importer.filterMode = FilterMode.Point;
                importer.mipmapEnabled = false; importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.alphaIsTransparency = true; importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        private static Material EnsureLineMaterial()
        {
            string path = WorldArt + "/WorldLines.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader == null) throw new InvalidOperationException("Sprite shader unavailable");
            material = new Material(shader) { name = "WorldLines" };
            AssetDatabase.CreateAsset(material, path); return material;
        }
        private static Material EnsureSpriteMaterial()
        {
            string path = WorldArt + "/WorldSpritesUnlit.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader == null) throw new InvalidOperationException("Unlit sprite shader unavailable");
            material = new Material(shader) { name = "WorldSpritesUnlit" };
            AssetDatabase.CreateAsset(material, path); return material;
        }
        private static void AssignUnlit(SpriteRenderer renderer)
        {
            if (renderer == null) return;
            var current = renderer.sharedMaterial;
            if (current == null || (current.shader != null && current.shader.name.Contains("Sprite-Lit")))
                renderer.sharedMaterial = EnsureSpriteMaterial();
        }
        private static void EnsureUnlitPrefab(string path)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            bool changed = false;
            foreach (var renderer in root.GetComponentsInChildren<SpriteRenderer>(true))
            {
                var before = renderer.sharedMaterial;
                AssignUnlit(renderer);
                if (before != renderer.sharedMaterial) changed = true;
            }
            if (changed) PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
        }
        private static ActorView EnsureActorBase(Sprite white, Sprite ellipse, Sprite ring)
        {
            string path = Prefabs + "/ActorBase.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<ActorView>(path);
            if (prefab != null)
            {
                EnsureUnlitPrefab(path);
                var contents = PrefabUtility.LoadPrefabContents(path);
                var existingLabel = contents.GetComponentInChildren<TextMesh>(true);
                var windup = contents.transform.Find("WindupLine");
                bool changed = false;
                changed |= EnsureBuffViews(contents, ellipse);
                if (windup == null)
                {
                    contents.GetComponent<ActorView>().EditorSetWindup(AddLine(contents, "WindupLine", EnsureLineMaterial(), -1));
                    changed = true;
                }
                if (existingLabel != null && Mathf.Approximately(existingLabel.characterSize, .1f))
                { existingLabel.characterSize = .02f; changed = true; }
                if (changed) PrefabUtility.SaveAsPrefabAsset(contents, path);
                PrefabUtility.UnloadPrefabContents(contents);
                return AssetDatabase.LoadAssetAtPath<ActorView>(path);
            }
            var root = new GameObject("ActorBase", typeof(SortingGroup), typeof(ActorView));
            var group = root.GetComponent<SortingGroup>(); group.sortingLayerName = "Default";
            var shadow = SpriteChild(root.transform, "Shadow", ellipse, -2);
            var shield = SpriteChild(root.transform, "Shield", ring, -1);
            var body = SpriteChild(root.transform, "Body", null, 0);
            var healthBg = SpriteChild(root.transform, "HealthBackground", white, 3);
            var healthFill = SpriteChild(root.transform, "HealthFill", white, 4);
            healthBg.color = new Color(.06f, .1f, .13f, .9f);
            var labelObject = new GameObject("NameLabel"); labelObject.transform.SetParent(root.transform, false);
            var label = labelObject.AddComponent<TextMesh>();
            label.anchor = TextAnchor.MiddleCenter; label.alignment = TextAlignment.Center;
            label.fontSize = 42; label.characterSize = .02f;
            var font = AssetDatabase.LoadAssetAtPath<Font>(Art + "/Fonts/NotoSansKR-Bold.otf");
            if (font != null) { label.font = font; label.GetComponent<MeshRenderer>().sharedMaterial = font.material; }
            label.GetComponent<MeshRenderer>().sortingOrder = 5;
            root.GetComponent<ActorView>().EditorSet(null, body, shadow, shield, healthBg, healthFill, label, .55f, Color.white);
            root.GetComponent<ActorView>().EditorSetWindup(AddLine(root, "WindupLine", EnsureLineMaterial(), -1));
            EnsureBuffViews(root, ellipse);
            PrefabUtility.SaveAsPrefabAsset(root, path); UnityEngine.Object.DestroyImmediate(root);
            return AssetDatabase.LoadAssetAtPath<ActorView>(path);
        }
        private static SpriteRenderer SpriteChild(Transform root, string name, Sprite sprite, int order)
        {
            var child = new GameObject(name); child.transform.SetParent(root, false);
            var renderer = child.AddComponent<SpriteRenderer>(); renderer.sprite = sprite; renderer.sortingOrder = order;
            renderer.sharedMaterial = EnsureSpriteMaterial();
            return renderer;
        }
        private static bool EnsureBuffViews(GameObject root, Sprite orbSprite)
        {
            if (root.transform.Find("EmpowermentRing") != null)
            {
                bool changed = false;
                for (int i = 0; i < 3; i++)
                {
                    var orb = root.transform.Find("OverloadOrb" + i);
                    if (orb != null && orb.localScale == new Vector3(.07f, .13f, 1))
                    { orb.localScale = new Vector3(.025f, .025f, 1); changed = true; }
                }
                return changed;
            }
            var ring = AddLine(root, "EmpowermentRing", EnsureLineMaterial(), -1); ring.enabled = false;
            var orbs = new SpriteRenderer[3];
            for (int i = 0; i < orbs.Length; i++)
            {
                orbs[i] = SpriteChild(root.transform, "OverloadOrb" + i, orbSprite, 2);
                orbs[i].transform.localScale = new Vector3(.025f, .025f, 1);
                orbs[i].color = new Color(.72f, .51f, 1); orbs[i].enabled = false;
            }
            root.GetComponent<ActorView>().EditorSetBuffs(ring, orbs); return true;
        }
        private static ActorView EnsureActorVariant(string id, bool hero, ActorView baseActor,
            DirectionalSpriteSet set, Color color, float scale)
        {
            string path = Prefabs + "/" + (hero ? "Player-" : "Enemy-") + id + ".prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<ActorView>(path);
            if (prefab != null) return prefab;
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(baseActor.gameObject);
            instance.name = hero ? "Player-" + id : "Enemy-" + id;
            var view = instance.GetComponent<ActorView>();
            view.SetDefinition(new ActorVisualDefinition { id = id, spriteSet = set, accent = color, displayScale = scale });
            PrefabUtility.SaveAsPrefabAsset(instance, path); UnityEngine.Object.DestroyImmediate(instance);
            return AssetDatabase.LoadAssetAtPath<ActorView>(path);
        }
        private static LineRenderer AddLine(GameObject root, string name, Material material, int order)
        {
            var child = new GameObject(name); child.transform.SetParent(root.transform, false);
            var line = child.AddComponent<LineRenderer>();
            line.sharedMaterial = material; line.useWorldSpace = true; line.numCapVertices = 0;
            line.numCornerVertices = 0; line.textureMode = LineTextureMode.Stretch;
            line.sortingOrder = order; line.enabled = false;
            return line;
        }
        private static ProjectileView EnsureProjectilePrefab(Sprite white, Sprite ellipse, Material material)
        {
            string path = Prefabs + "/Projectile.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<ProjectileView>(path);
            if (prefab != null) { EnsureUnlitPrefab(path); return prefab; }
            var root = new GameObject("Projectile", typeof(ProjectileView));
            var glow = SpriteChild(root.transform, "Glow", ellipse, 9001);
            var head = SpriteChild(root.transform, "Head", white, 9002);
            var trail = AddLine(root, "Trail", material, 9000);
            root.GetComponent<ProjectileView>().EditorSet(head, glow, trail);
            PrefabUtility.SaveAsPrefabAsset(root, path); UnityEngine.Object.DestroyImmediate(root);
            return AssetDatabase.LoadAssetAtPath<ProjectileView>(path);
        }
        private static ZoneView EnsureZonePrefab(Material material)
        {
            string path = Prefabs + "/Zone.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<ZoneView>(path);
            if (prefab != null) return prefab;
            var root = new GameObject("Zone", typeof(ZoneView));
            root.GetComponent<ZoneView>().EditorSet(AddLine(root, "Outline", material, -4000), AddLine(root, "Core", material, -3999));
            PrefabUtility.SaveAsPrefabAsset(root, path); UnityEngine.Object.DestroyImmediate(root);
            return AssetDatabase.LoadAssetAtPath<ZoneView>(path);
        }
        private static WorldEffectView EnsureEffectPrefab(Material material)
        {
            string path = Prefabs + "/Effect.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<WorldEffectView>(path);
            if (prefab != null)
            {
                var contents = PrefabUtility.LoadPrefabContents(path);
                var existingLabel = contents.GetComponentInChildren<TextMesh>(true);
                if (existingLabel != null && Mathf.Approximately(existingLabel.characterSize, .13f))
                { existingLabel.characterSize = .026f; PrefabUtility.SaveAsPrefabAsset(contents, path); }
                PrefabUtility.UnloadPrefabContents(contents);
                return AssetDatabase.LoadAssetAtPath<WorldEffectView>(path);
            }
            var root = new GameObject("Effect", typeof(WorldEffectView));
            var line = AddLine(root, "Stroke", material, 10000);
            var labelObject = new GameObject("Text"); labelObject.transform.SetParent(root.transform, false);
            var label = labelObject.AddComponent<TextMesh>(); label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center; label.fontSize = 42; label.characterSize = .026f;
            var font = AssetDatabase.LoadAssetAtPath<Font>(Art + "/Fonts/NotoSansKR-Bold.otf");
            if (font != null) { label.font = font; label.GetComponent<MeshRenderer>().sharedMaterial = font.material; }
            label.GetComponent<MeshRenderer>().sortingOrder = 10001;
            root.GetComponent<WorldEffectView>().EditorSet(line, label);
            PrefabUtility.SaveAsPrefabAsset(root, path); UnityEngine.Object.DestroyImmediate(root);
            return AssetDatabase.LoadAssetAtPath<WorldEffectView>(path);
        }
        private static AimMarkerView EnsureAimPrefab(Material material)
        {
            string path = Prefabs + "/AimMarker.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<AimMarkerView>(path);
            if (prefab != null) return prefab;
            var root = new GameObject("AimMarker", typeof(AimMarkerView));
            root.GetComponent<AimMarkerView>().EditorSet(AddLine(root, "Primary", material, 11000),
                AddLine(root, "Secondary", material, 10999), AddLine(root, "MoveOrder", material, 11001));
            PrefabUtility.SaveAsPrefabAsset(root, path); UnityEngine.Object.DestroyImmediate(root);
            return AssetDatabase.LoadAssetAtPath<AimMarkerView>(path);
        }
        private static ArenaMapView EnsureArenaPrefab()
        {
            var sprite = ExportSprite(WorldArt + "/arena-ground.png", SurvivalLegendArt.ArenaGround());
            var ritual = ExportSprite(WorldArt + "/arena-ritual.png", SurvivalLegendArt.ArenaRitual());
            var lamp = ExportSprite(WorldArt + "/arena-corner-lamp.png", SurvivalLegendArt.ArenaCornerLamp());
            string path = Prefabs + "/ArenaMap.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<ArenaMapView>(path);
            bool editingExisting = prefab != null;
            var root = editingExisting ? PrefabUtility.LoadPrefabContents(path) : new GameObject("ArenaMap", typeof(ArenaMapView));
            var backdropTransform = root.transform.Find("Backdrop");
            var backdrop = backdropTransform != null ? backdropTransform.GetComponent<SpriteRenderer>()
                : SpriteChild(root.transform, "Backdrop", sprite, -10000);
            string existingArt = backdrop.sprite != null ? AssetDatabase.GetAssetPath(backdrop.sprite) : string.Empty;
            if (backdrop.sprite == null || existingArt == WorldArt + "/arena-base.png")
                backdrop.sprite = sprite;
            var ground = Layer(root.transform, "Ground");
            var floor = Layer(root.transform, "FloorTiles");
            var decorations = Layer(root.transform, "Decorations");
            var bounds = Layer(root.transform, "Bounds");
            var spawns = Layer(root.transform, "SpawnMarkers");
            if (decorations.Find("RitualRings") == null)
            {
                var ring = SpriteChild(decorations, "RitualRings", ritual, -9999);
                ring.transform.localPosition = WorldProjection.WorldToScene(new Vector2(720, 720));
            }
            Vector2[] corners = { new Vector2(0, 0), new Vector2(1440, 0),
                new Vector2(1440, 1440), new Vector2(0, 1440) };
            string[] names = { "LampNorth", "LampEast", "LampSouth", "LampWest" };
            for (int i = 0; i < corners.Length; i++)
                if (decorations.Find(names[i]) == null)
                {
                    var feature = SpriteChild(decorations, names[i], lamp, -9998);
                    feature.transform.localPosition = WorldProjection.WorldToScene(corners[i]);
                }
            root.GetComponent<ArenaMapView>().EditorSet(backdrop, ground, floor, decorations, bounds, spawns);
            foreach (var renderer in root.GetComponentsInChildren<SpriteRenderer>(true)) AssignUnlit(renderer);
            PrefabUtility.SaveAsPrefabAsset(root, path);
            if (editingExisting) PrefabUtility.UnloadPrefabContents(root);
            else UnityEngine.Object.DestroyImmediate(root);
            return AssetDatabase.LoadAssetAtPath<ArenaMapView>(path);
        }
        private static Sprite ExportSprite(string path, Texture2D source)
        {
            if (!File.Exists(path))
            {
                var copy = ReadTexture(source);
                File.WriteAllBytes(path, copy.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(copy);
            }
            return ImportSingle(path);
        }
    }
}
