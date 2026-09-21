using System.IO;
using SurvivalLegend.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace SurvivalLegend.Editor
{
    /// <summary>Creates editable tier art and the saved augment card prefab, then places its viewport.</summary>
    public static class AugmentCardMigration
    {
        const string Art = "Assets/SurvivalLegend/Resources/Art/UI/Augment";
        const string Prefab = "Assets/SurvivalLegend/Prefabs/UI/AugmentCard.prefab";
        static readonly Color Paper = new Color(.94f, .94f, .86f);
        static Font regular, bold;

        public static void Apply(SurvivorCanvasUI ui)
        {
            if (ui == null) return;
            EnsureAssets();
            var cardAsset = BuildPrefab();
            ui.AugmentTemplate = cardAsset.GetComponent<AugmentCardUI>();
            var panel = ui.AugmentPanel != null ? ui.AugmentPanel.transform : null;
            if (panel != null)
            {
                var heading = panel.Find("Heading") as RectTransform;
                var hint = panel.Find("AugmentHint") as RectTransform;
                var viewport = panel.Find("AugmentChoices") as RectTransform;
                if (heading != null) Place(heading, 280, 47, 720, 52);
                if (hint != null) Place(hint, 330, 113, 620, 30);
                if (viewport != null)
                {
                    Place(viewport, 170, 166, 940, 486);
                    var image = viewport.GetComponent<Image>();
                    if (image != null) image.color = new Color(.015f, .055f, .065f, .18f);
                    var scroll = viewport.GetComponent<ScrollRect>();
                    if (scroll != null)
                    {
                        scroll.horizontal = true; scroll.vertical = false;
                        scroll.movementType = ScrollRect.MovementType.Clamped;
                    }
                }
            }
            if (ui.AugmentContent != null)
            {
                var content = ui.AugmentContent;
                content.sizeDelta = new Vector2(content.sizeDelta.x, 486);
                var layout = content.GetComponent<HorizontalLayoutGroup>();
                if (layout != null)
                {
                    layout.spacing = 16;
                    layout.childAlignment = TextAnchor.MiddleCenter;
                    layout.padding = new RectOffset(10, 10, 10, 10);
                }
                // At three cards the content fills the 940 px viewport; fewer remain centered.
                var element = content.GetComponent<LayoutElement>();
                if (element == null) element = content.gameObject.AddComponent<LayoutElement>();
                element.minWidth = 940;
                element.minHeight = 486;
            }
            EditorUtility.SetDirty(ui);
        }

        public static void ApplyToCurrentScene()
        {
            var ui = Object.FindAnyObjectByType<SurvivorCanvasUI>(FindObjectsInactive.Include);
            if (ui == null) throw new System.InvalidOperationException("No SurvivorCanvasUI in the active scene.");
            Apply(ui);
            EditorSceneManager.MarkSceneDirty(ui.gameObject.scene);
            EditorSceneManager.SaveScene(ui.gameObject.scene);
        }

        public static void EnsureAssets()
        {
            Directory.CreateDirectory(Art);
            regular = AssetDatabase.LoadAssetAtPath<Font>("Assets/SurvivalLegend/Resources/Art/Fonts/NotoSansKR-Regular.otf");
            bold = AssetDatabase.LoadAssetAtPath<Font>("Assets/SurvivalLegend/Resources/Art/Fonts/NotoSansKR-Bold.otf");
            EnsureFrame("silver", new Color(.68f, .77f, .83f), new Color(.94f, .97f, 1f));
            EnsureFrame("gold", new Color(.55f, .37f, .14f), new Color(1f, .83f, .46f));
            EnsureFrame("prism", new Color(.40f, .21f, .62f), new Color(.86f, .67f, 1f));
            EnsureMedallion();
            EnsureGlow();
        }

        static void EnsureFrame(string tier, Color shadow, Color highlight)
        {
            string path = Art + "/augment-frame-" + tier + ".png";
            if (File.Exists(path)) return;
            const int n = 128;
            var texture = new Texture2D(n, n, TextureFormat.RGBA32, false);
            var pixels = new Color[n * n];
            for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
            {
                int edge = Mathf.Min(x, y, n - 1 - x, n - 1 - y);
                // Chamfered corners keep the silhouette like a physical carved card.
                int cut = Mathf.Max(0, 9 - Mathf.Min(x, n - 1 - x)) + Mathf.Max(0, 9 - Mathf.Min(y, n - 1 - y));
                if (edge < 9 && cut > 9) { pixels[y * n + x] = Color.clear; continue; }
                Color c;
                if (edge <= 1) c = new Color(.035f, .065f, .075f);
                else if (edge <= 3) c = Color.Lerp(shadow, highlight, .35f + .35f * y / n);
                else if (edge <= 5) c = highlight;
                else if (edge <= 9) c = Color.Lerp(shadow, new Color(.08f,.14f,.16f), (edge - 5f) / 4f);
                else if (edge <= 12) c = new Color(.19f, .31f, .33f);
                else c = new Color(.035f, .105f, .12f);
                if (edge > 18 && edge < 21) c = Color.Lerp(c, highlight, .24f);
                if (edge > 23 && edge < 25) c = new Color(.02f, .07f, .08f);
                if (tier == "prism" && edge <= 9)
                {
                    float hue = (x + y * .67f) / (n * 1.67f);
                    Color spectral = Color.Lerp(new Color(.43f,.9f,1f), new Color(1f,.51f,.84f), hue);
                    c = Color.Lerp(c, spectral, .55f);
                }
                pixels[y * n + x] = c;
            }
            SaveSprite(path, texture, pixels, new Vector4(28,28,28,28));
        }

        static void EnsureMedallion()
        {
            string path = Art + "/augment-medallion.png";
            if (File.Exists(path)) return;
            const int n = 128;
            var texture = new Texture2D(n,n,TextureFormat.RGBA32,false);
            var pixels = new Color[n*n];
            for (int y=0;y<n;y++) for(int x=0;x<n;x++)
            {
                float r = Vector2.Distance(new Vector2(x+.5f,y+.5f),new Vector2(64,64));
                Color c = Color.clear;
                if (r < 60 && r > 55) c = new Color(.08f,.14f,.16f,1);
                if (r <= 55 && r > 50) c = new Color(.94f,.93f,.75f,1);
                if (r <= 50 && r > 45) c = new Color(.16f,.26f,.27f,1);
                if (r <= 45) c = new Color(.025f,.08f,.10f,.98f);
                if (r <= 41 && r >= 39) c = new Color(.76f,.80f,.69f,.7f);
                pixels[y*n+x]=c;
            }
            SaveSprite(path,texture,pixels,Vector4.zero);
        }

        static void EnsureGlow()
        {
            string path = Art + "/augment-glow.png";
            if (File.Exists(path)) return;
            const int n=128;
            var texture=new Texture2D(n,n,TextureFormat.RGBA32,false);
            var pixels=new Color[n*n];
            for(int y=0;y<n;y++) for(int x=0;x<n;x++)
            {
                float r=Vector2.Distance(new Vector2(x+.5f,y+.5f),new Vector2(64,64))/64f;
                float a=Mathf.Pow(Mathf.Clamp01(1-r),2f);
                pixels[y*n+x]=new Color(1,1,1,a);
            }
            SaveSprite(path,texture,pixels,Vector4.zero);
        }

        static void SaveSprite(string path, Texture2D texture, Color[] pixels, Vector4 border)
        {
            texture.SetPixels(pixels); texture.Apply();
            File.WriteAllBytes(path,texture.EncodeToPNG()); Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType=TextureImporterType.Sprite;
            importer.spriteImportMode=SpriteImportMode.Single;
            importer.spriteBorder=border;
            importer.alphaIsTransparency=true;
            importer.mipmapEnabled=false;
            importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.filterMode=FilterMode.Bilinear;
            importer.SaveAndReimport();
        }

        static GameObject BuildPrefab()
        {
            var saved=AssetDatabase.LoadAssetAtPath<GameObject>(Prefab);
            if(saved!=null && saved.transform.Find("VisualRoot")!=null)
            {
                var contents=PrefabUtility.LoadPrefabContents(Prefab);
                try
                {
                    bool changed=false;
                    foreach(var graphic in contents.GetComponentsInChildren<AugmentSparkleGraphic>(true))
                        if(graphic.GetComponent<CanvasRenderer>()==null){graphic.gameObject.AddComponent<CanvasRenderer>();changed=true;}
                    if(changed)PrefabUtility.SaveAsPrefabAsset(contents,Prefab);
                }
                finally{PrefabUtility.UnloadPrefabContents(contents);}
                return saved;
            }
            var root=Rect("AugmentCard",null,0,0,292,432);
            var card=root.gameObject.AddComponent<AugmentCardUI>();
            var element=root.gameObject.AddComponent<LayoutElement>();
            element.preferredWidth=292; element.preferredHeight=432;
            element.minWidth=292; element.minHeight=432;
            var visual=Rect("VisualRoot",root,0,0,292,432);
            // Runtime animation moves this child around its layout slot's center.
            visual.anchorMin=visual.anchorMax=new Vector2(.5f,.5f);
            visual.anchoredPosition=Vector2.zero;
            var group=visual.gameObject.AddComponent<CanvasGroup>();
            var glow=MakeImage("TierGlow",visual,-12,-12,316,456,
                AssetDatabase.LoadAssetAtPath<Sprite>(Art+"/augment-glow.png"),new Color(.7f,.7f,1f,.13f));
            var frame=MakeImage("TierFrame",visual,0,0,292,432,
                AssetDatabase.LoadAssetAtPath<Sprite>(Art+"/augment-frame-silver.png"),Color.white);
            frame.type=UnityEngine.UI.Image.Type.Sliced; frame.raycastTarget=true;
            var tier=Label("Tier",visual,"실버",26,19,240,24,16,new Color(.78f,.88f,1f),true);
            var medal=MakeImage("Medallion",visual,96,43,100,100,
                AssetDatabase.LoadAssetAtPath<Sprite>(Art+"/augment-medallion.png"),Color.white);
            var icon=Rect("SkillIcon",visual,113,60,66,66).gameObject.AddComponent<RawImage>();
            icon.raycastTarget=false;
            var title=Label("Title",visual,"능력 강화",20,147,252,45,24,Paper,true);
            var tagBack=MakeImage("AbilityTagPlate",visual,47,196,198,30,null,new Color(.07f,.18f,.19f,.93f));
            tagBack.raycastTarget=false;
            var tag=Label("AbilityTag",visual,"스킬 강화",48,196,196,30,14,new Color(.77f,.87f,.81f),false);
            var description=Label("Description",visual,"이번 전투에서 새로운 힘을 얻습니다.",24,235,244,88,16,Paper,false);
            description.alignment=TextAnchor.UpperCenter;
            var choose=Button("Choose",visual,"선택",28,336,236,42,true);
            var reroll=Button("Reroll",visual,"다시 뽑기  ↻",64,387,164,28,false);
            var glints=Rect("EdgeSparkles",visual,0,0,292,432).gameObject.AddComponent<AugmentSparkleGraphic>();
            glints.raycastTarget=false;
            card.Choose=choose; card.Reroll=reroll; card.Title=title; card.Description=description; card.Tier=tier;
            card.EditorSet(visual,group,frame,medal,glow,icon,tag,glints,
                AssetDatabase.LoadAssetAtPath<Sprite>(Art+"/augment-frame-silver.png"),
                AssetDatabase.LoadAssetAtPath<Sprite>(Art+"/augment-frame-gold.png"),
                AssetDatabase.LoadAssetAtPath<Sprite>(Art+"/augment-frame-prism.png"));
            saved=PrefabUtility.SaveAsPrefabAsset(root.gameObject,Prefab);
            Object.DestroyImmediate(root.gameObject);
            return saved;
        }

        static RectTransform Rect(string name,Transform parent,float x,float y,float w,float h)
        {
            var rt=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();
            rt.SetParent(parent,false); Place(rt,x,y,w,h); return rt;
        }
        static void Place(RectTransform rt,float x,float y,float w,float h)
        {
            rt.anchorMin=rt.anchorMax=new Vector2(0,1); rt.pivot=new Vector2(.5f,.5f);
            rt.sizeDelta=new Vector2(w,h); rt.anchoredPosition=new Vector2(x+w*.5f,-y-h*.5f);
        }
        static Image MakeImage(string name,Transform parent,float x,float y,float w,float h,Sprite sprite,Color tint)
        {
            var image=Rect(name,parent,x,y,w,h).gameObject.AddComponent<Image>();
            image.sprite=sprite; image.color=tint; image.raycastTarget=false; return image;
        }
        static Text Label(string name,Transform parent,string value,float x,float y,float w,float h,int size,Color tint,bool heading)
        {
            var text=Rect(name,parent,x,y,w,h).gameObject.AddComponent<Text>();
            text.font=heading?bold:regular; text.text=value; text.fontSize=size; text.color=tint;
            text.alignment=TextAnchor.MiddleCenter; text.raycastTarget=false;
            text.resizeTextForBestFit=true; text.resizeTextMinSize=heading?15:11; text.resizeTextMaxSize=size;
            text.horizontalOverflow=HorizontalWrapMode.Wrap; text.verticalOverflow=VerticalWrapMode.Truncate;
            return text;
        }
        static Button Button(string name,Transform parent,string value,float x,float y,float w,float h,bool main)
        {
            var image=MakeImage(name,parent,x,y,w,h,null,main?new Color(.31f,.38f,.32f):new Color(.12f,.23f,.24f));
            image.raycastTarget=true;
            var button=image.gameObject.AddComponent<Button>(); button.targetGraphic=image;
            var colors=button.colors; colors.highlightedColor=main?new Color(1f,.92f,.68f):new Color(.76f,.9f,.9f);
            colors.pressedColor=new Color(.68f,.71f,.60f); button.colors=colors;
            Label("Label",image.transform,value,4,1,w-8,h-2,main?18:14,Paper,true);
            return button;
        }
    }
}
