using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using SurvivalLegend.UI;

namespace SurvivalLegend.Editor
{
    /// <summary>One-time authoring: saves editable UI prefabs and a fully wired Canvas in the scene.</summary>
    public static class CanvasUiBuilder
    {
        const string Folder = "Assets/SurvivalLegend/Prefabs/UI";
        static Font font;
        static Sprite flatSprite;
        static readonly Color Ink = new Color(.035f, .085f, .095f, .97f);
        static readonly Color Plate = new Color(.09f, .16f, .17f, .98f);
        static readonly Color Gold = new Color(.72f, .60f, .34f);
        static readonly Color Paper = new Color(.89f, .92f, .83f);

        public static GameObject Build(SurvivorGame game, Transform parent)
        {
            Directory.CreateDirectory(Folder); AssetDatabase.Refresh();
            font = AssetDatabase.LoadAssetAtPath<Font>("Assets/SurvivalLegend/Resources/Art/Fonts/NotoSansKR-Regular.otf");
            var root = new GameObject("UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.transform.SetParent(parent, false);
            root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720); scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            var ui = root.AddComponent<SurvivorCanvasUI>(); ui.Game = game;
            var frame = Rect("ReferenceFrame", root.transform, 0, 0, 1280, 720);
            frame.anchorMin = frame.anchorMax = new Vector2(.5f, .5f); frame.anchoredPosition = Vector2.zero;
            // Templates are project assets, referenced directly by the scene binder.
            ui.CharacterTemplate = CharacterPrefab(); ui.AugmentTemplate = AugmentPrefab();
            ui.HistoryTemplate = HistoryPrefab();

            ui.TitlePanel = Panel("Title", frame, 0, 0, 1280, 720, new Color(.015f,.04f,.045f,.7f)).gameObject;
            var title = ui.TitlePanel.transform;
            Label("Eyebrow", title, "T H E   A R E N A   A W A I T S", 350, 80, 580, 32, 15, Gold);
            Label("Title", title, "SURVIVAL", 280, 125, 720, 94, 72, Paper);
            Label("Subtitle", title, "L E G E N D", 280, 215, 720, 80, 55, Gold);
            Label("Tagline", title, "끝까지 살아남아라.", 400, 307, 480, 36, 21, Paper);
            for (int i = 0; i < 3; i++)
            {
                string id = new[] { "swordsman", "archer", "mage" }[i];
                var portrait = Rect(id + "Portrait", title, 345 + i * 205, 353, 180, 180).gameObject.AddComponent<RawImage>();
                portrait.texture = Resources.Load<Texture2D>("Art/Characters/" + id + "-sd-atlas"); portrait.uvRect = new Rect(0,.75f,1f/6,1f/8); portrait.raycastTarget = false;
            }
            ui.Enter = Button("EnterArena", title, "전장으로    ↗", 490, 555, 300, 60);
            Label("Controls", title, "마우스 + 키보드", 440, 631, 400, 25, 13, Paper);

            ui.LoadoutPanel = Panel("Loadout", frame, 0, 0, 1280, 720, Ink).gameObject;
            var loadout = ui.LoadoutPanel.transform;
            Label("Heading", loadout, "캐릭터를 선택하세요", 280, 48, 720, 48, 32, Paper);
            Label("Hint", loadout, "적 우클릭 공격 · A 후 좌클릭 공격 이동 · 모든 스킬을 보유하고 시작", 220, 100, 840, 30, 13, Gold);
            var roster = Scroll("CharacterRoster", loadout, 148, 149, 984, 236, true, out var rosterContent);
            ui.RosterContent = rosterContent;
            ui.SelectedKit = Label("SelectedKit", loadout, "", 170, 395, 940, 36, 16, Gold);
            ui.SpecialD = Button("SpecialD", loadout, "D", 265, 450, 365, 75);
            ui.SpecialF = Button("SpecialF", loadout, "F", 650, 450, 365, 75);
            ui.SpecialDLabel = ui.SpecialD.GetComponentInChildren<Text>(); ui.SpecialDLabel.fontSize = 13;
            ui.SpecialFLabel = ui.SpecialF.GetComponentInChildren<Text>(); ui.SpecialFLabel.fontSize = 13;
            ui.Start = Button("StartRun", loadout, "생존 시작    ↗", 410, 559, 460, 56);
            ui.Back = Button("BackToTitle", loadout, "‹ 타이틀로", 540, 634, 200, 35);

            GothicHudMigration.BuildHud(ui, frame);

            ui.PausePanel = Modal("PauseModal", frame, "일시 정지", out var pause);
            ui.Resume = Button("Resume", pause, "계속하기", 390, 285, 500, 58);
            ui.Quit = Button("ReturnToTitle", pause, "타이틀로", 390, 375, 500, 58);
            Label("PauseHelp", pause, "ESC  계속하기 · SPACE  플레이어 추적", 390, 452, 500, 30, 14, Gold);

            ui.AugmentPanel = Modal("AugmentModal", frame, "새로운 힘을 선택하세요", out var aug);
            Label("AugmentHint", aug, "선택한 능력은 이번 전투에 적용됩니다", 330, 160, 620, 30, 15, Gold);
            Scroll("AugmentChoices", aug, 140, 220, 1000, 350, true, out var augContent); ui.AugmentContent = augContent;

            ui.ResultsPanel = Modal("ResultsModal", frame, "전투 기록", out var result);
            ui.ResultSummary = Label("Summary", result, "", 270, 170, 740, 90, 21, Paper);
            ui.HistoryScroll = Scroll("AugmentHistory", result, 310, 295, 660, 230, false, out var hist); ui.HistoryContent = hist;
            ui.Retry = Button("Retry", result, "다시 도전", 350, 565, 270, 56);
            ui.ResultsHome = Button("Home", result, "타이틀로", 660, 565, 270, 56);
            ui.TransitionPanel = Panel("Transition", frame, 340, 230, 600, 200, Ink).gameObject;
            ui.TransitionLabel = Label("Message", ui.TransitionPanel.transform, "", 30, 30, 540, 140, 30, Gold);
            ui.ErrorLabel = Label("DataValidationError", frame, "", 80, 15, 1120, 55, 15, new Color(1,.42f,.35f));
            ui.ErrorLabel.transform.SetAsLastSibling();
            if (Object.FindAnyObjectByType<EventSystem>() == null)
            {
                var events = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                events.transform.SetParent(parent, false);
            }
            ui.LoadoutPanel.SetActive(false); ui.HudPanel.SetActive(false); ui.PausePanel.SetActive(false);
            ui.AugmentPanel.SetActive(false); ui.ResultsPanel.SetActive(false); ui.TransitionPanel.SetActive(false);
            GothicHudMigration.SkinCanvas(ui);
            AugmentCardMigration.Apply(ui);
            var world=Object.FindAnyObjectByType<SurvivalLegend.World.WorldPresenter>();
            if(world!=null)SurvivalLegend.Editor.World.WorldExplorationBuilder.Configure(world,ui.Game,Camera.main);
            EditorUtility.SetDirty(ui); return root;
        }

        static CharacterCardUI CharacterPrefab()
        {
            string path = Folder + "/CharacterCard.prefab";
            var saved = AssetDatabase.LoadAssetAtPath<GameObject>(path); if (saved != null) return saved.GetComponent<CharacterCardUI>();
            var go = Panel("CharacterCard", null, 0, 0, 312, 222, Plate).gameObject;
            var card = go.AddComponent<CharacterCardUI>(); card.Select = go.AddComponent<Button>(); card.Selection = go.GetComponent<Image>();
            card.Name = Label("Name", go.transform, "캐릭터", 110, 18, 192, 34, 25, Paper);
            card.Subtitle = Label("Subtitle", go.transform, "", 110, 58, 188, 42, 13, Gold);
            card.Stats = Label("Stats", go.transform, "", 16, 143, 280, 65, 12, Paper);
            card.Portrait = Rect("Portrait", go.transform, 5, 9, 120, 128).gameObject.AddComponent<RawImage>(); card.Portrait.raycastTarget = false;
            return Save(go,path).GetComponent<CharacterCardUI>();
        }
        static AugmentCardUI AugmentPrefab()
        {
            string path = Folder + "/AugmentCard.prefab";
            var saved = AssetDatabase.LoadAssetAtPath<GameObject>(path); if (saved != null) return saved.GetComponent<AugmentCardUI>();
            var go = Panel("AugmentCard", null, 0, 0, 316, 330, Plate).gameObject;
            var card = go.AddComponent<AugmentCardUI>();
            card.Tier = Label("Tier", go.transform, "", 18, 16, 280, 25, 14, Gold);
            card.Title = Label("Title", go.transform, "", 18, 57, 280, 55, 23, Paper);
            card.Description = Label("Description", go.transform, "", 22, 115, 272, 98, 16, Paper);
            card.Choose = Button("Choose", go.transform, "선택", 30, 230, 256, 43);
            card.Reroll = Button("Reroll", go.transform, "다시 뽑기  ↻", 70, 282, 176, 30);
            return Save(go,path).GetComponent<AugmentCardUI>();
        }
        static SkillSlotUI SlotPrefab()
        {
            string path = Folder + "/SkillSlot.prefab";
            var saved = AssetDatabase.LoadAssetAtPath<GameObject>(path); if (saved != null) return saved.GetComponent<SkillSlotUI>();
            var go = Rect("SkillSlot", null, 0, 0, 86, 124).gameObject; var slot = go.AddComponent<SkillSlotUI>();
            slot.Cast = Button("Cast", go.transform, "", 6, 0, 74, 73);
            slot.Icon = Rect("Icon", slot.Cast.transform, 11, 7, 52, 52).gameObject.AddComponent<RawImage>(); slot.Icon.raycastTarget = false;
            slot.CooldownFill = Fill("CooldownFill", slot.Cast.transform, 0, 0, 74, 73, new Color(0,0,0,.7f)); slot.CooldownFill.fillOrigin = 1;
            slot.CooldownFill.transform.parent.GetComponent<Image>().color = Color.clear;
            slot.Cooldown = Label("Cooldown", slot.Cast.transform, "", 0, 12, 74, 38, 23, Paper);
            slot.Key = Label("Key", slot.Cast.transform, "Q", 0, 52, 74, 20, 14, Gold);
            slot.Name = Label("Name", go.transform, "", 0, 75, 86, 22, 11, Paper);
            var toggle = Rect("QuickCast", go.transform, 3, 102, 80, 20).gameObject; slot.QuickCast = toggle.AddComponent<Toggle>();
            var background = Panel("Box", toggle.transform, 0, 2, 15, 15, Plate).GetComponent<Image>();
            var check = Panel("Check", background.transform, 3, 3, 9, 9, Gold).GetComponent<Image>();
            slot.QuickCast.targetGraphic = background; slot.QuickCast.graphic = check;
            Label("Label", toggle.transform, "즉시 시전", 18, 0, 62, 20, 10, Paper);
            return Save(go,path).GetComponent<SkillSlotUI>();
        }
        static Text HistoryPrefab()
        {
            string path = Folder + "/HistoryRow.prefab"; var saved = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (saved != null) return saved.GetComponent<Text>();
            return Save(Label("HistoryRow", null, "", 0, 0, 625, 36, 15, Paper).gameObject,path).GetComponent<Text>();
        }
        static GameObject Save(GameObject go,string path) { var saved = PrefabUtility.SaveAsPrefabAsset(go,path); Object.DestroyImmediate(go); return saved; }
        static GameObject Modal(string name, Transform parent,string heading,out Transform body)
        {
            var go = Panel(name,parent,0,0,1280,720,new Color(.018f,.04f,.045f,.94f)).gameObject; body=go.transform;
            Label("Heading",body,heading,260,85,760,60,34,Paper); return go;
        }
        static RectTransform Rect(string name,Transform parent,float x,float y,float w,float h)
        {
            var rt = new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>(); rt.SetParent(parent,false); Place(rt,x,y,w,h); return rt;
        }
        static void Place(RectTransform rt,float x,float y,float w,float h)
        { rt.anchorMin=rt.anchorMax=new Vector2(0,1); rt.pivot=new Vector2(.5f,.5f); rt.sizeDelta=new Vector2(w,h); rt.anchoredPosition=new Vector2(x+w*.5f,-y-h*.5f); }
        static RectTransform Panel(string name,Transform parent,float x,float y,float w,float h,Color color)
        { var rt=Rect(name,parent,x,y,w,h); rt.gameObject.AddComponent<Image>().color=color; return rt; }
        static Text Label(string name,Transform parent,string value,float x,float y,float w,float h,int size,Color color)
        {
            var text=Rect(name,parent,x,y,w,h).gameObject.AddComponent<Text>(); text.font=font; text.fontSize=size; text.color=color;
            text.text=value; text.alignment=TextAnchor.MiddleCenter; text.raycastTarget=false; text.supportRichText=false;
            FitText(text); return text;
        }
        public static void FitText(Text text)
        {
            text.resizeTextForBestFit = true;
            text.resizeTextMaxSize = text.fontSize;
            text.resizeTextMinSize = Mathf.Max(8, Mathf.FloorToInt(text.fontSize * .65f));
        }
        public static Sprite FlatSprite()
        {
            if (flatSprite != null) return flatSprite;
            string path = Folder + "/FlatFill.png";
            if (!File.Exists(path))
            {
                var texture = new Texture2D(4, 4, TextureFormat.RGBA32, false);
                var pixels = new Color[16]; for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
                texture.SetPixels(pixels); texture.Apply(); File.WriteAllBytes(path, texture.EncodeToPNG()); Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(path);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path); importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single; importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.filterMode = FilterMode.Point; importer.mipmapEnabled = false; importer.SaveAndReimport();
            }
            flatSprite = AssetDatabase.LoadAssetAtPath<Sprite>(path); return flatSprite;
        }
        static Button Button(string name,Transform parent,string value,float x,float y,float w,float h)
        {
            var rt=Panel(name,parent,x,y,w,h,Plate); var button=rt.gameObject.AddComponent<Button>();
            var colors=button.colors; colors.highlightedColor=new Color(1,.89f,.62f); colors.selectedColor=colors.highlightedColor; button.colors=colors;
            Label("Label",rt,value,6,2,w-12,h-4,19,Paper); return button;
        }
        static Image Fill(string name,Transform parent,float x,float y,float w,float h,Color color)
        {
            var bg=Panel(name,parent,x,y,w,h,new Color(.14f,.19f,.2f)); bg.GetComponent<Image>().raycastTarget=false;
            var image=Panel("Fill",bg,0,0,w,h,color).GetComponent<Image>(); image.raycastTarget=false;
            image.sprite=FlatSprite(); image.type=Image.Type.Filled; image.fillMethod=Image.FillMethod.Horizontal; return image;
        }
        static ScrollRect Scroll(string name,Transform parent,float x,float y,float w,float h,bool horizontal,out RectTransform content)
        {
            var viewport=Panel(name,parent,x,y,w,h,new Color(.02f,.05f,.06f,.4f)); viewport.gameObject.AddComponent<RectMask2D>();
            var scroll=viewport.gameObject.AddComponent<ScrollRect>(); scroll.viewport=viewport; scroll.horizontal=horizontal; scroll.vertical=!horizontal; scroll.movementType=ScrollRect.MovementType.Clamped;
            content=Rect("Content",viewport,0,0,w,h); content.pivot=new Vector2(0,1); content.anchoredPosition=Vector2.zero;
            HorizontalOrVerticalLayoutGroup layout = horizontal ? (HorizontalOrVerticalLayoutGroup)content.gameObject.AddComponent<HorizontalLayoutGroup>() : content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing=16; layout.childControlWidth=false; layout.childControlHeight=false; layout.childForceExpandWidth=false; layout.childForceExpandHeight=false; layout.padding=new RectOffset(8,8,6,6);
            var fitter=content.gameObject.AddComponent<ContentSizeFitter>(); fitter.horizontalFit=horizontal?ContentSizeFitter.FitMode.PreferredSize:ContentSizeFitter.FitMode.Unconstrained; fitter.verticalFit=horizontal?ContentSizeFitter.FitMode.Unconstrained:ContentSizeFitter.FitMode.PreferredSize;
            scroll.content=content; return scroll;
        }
    }
}
