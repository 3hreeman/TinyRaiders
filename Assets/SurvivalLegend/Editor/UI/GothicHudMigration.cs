using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using SurvivalLegend.UI;
using Object = UnityEngine.Object;

namespace SurvivalLegend.Editor
{
    /// <summary>Explicit authoring migration: leaves world, menus and their bindings intact.</summary>
    public static class GothicHudMigration
    {
        public const string ArtFolder="Assets/SurvivalLegend/Resources/Art/UI/Gothic";
        const string PrefabFolder="Assets/SurvivalLegend/Prefabs/UI";
        static Sprite frameSprite,guardianSprite,orbSprite;
        static Font font;
        static readonly Color Paper=new Color(.92f,.91f,.84f);
        static readonly Color Muted=new Color(.57f,.62f,.62f);
        static readonly Color Brass=new Color(.80f,.69f,.43f);
        static readonly Color Dark=new Color(.055f,.075f,.08f);

        [MenuItem("Survival Legend/UI/Apply Gothic HUD to Current Scene")]
        public static void ApplyToCurrentScene()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Apply the HUD migration in Edit Mode.");
            var ui=Object.FindAnyObjectByType<SurvivorCanvasUI>(FindObjectsInactive.Include);
            if(ui==null)throw new InvalidOperationException("Open the authored Survival Legend scene before applying the HUD migration.");
            EnsureArtImports();
            Undo.RecordObject(ui,"Apply Gothic HUD");
            var previous=ui.HudPanel;var parent=previous!=null?previous.transform.parent:ui.transform.Find("ReferenceFrame");
            if(parent==null)throw new InvalidOperationException("The Canvas ReferenceFrame is missing.");
            int order=previous!=null?previous.transform.GetSiblingIndex():2;
            bool active=previous!=null&&previous.activeSelf;
            if(previous!=null)Undo.DestroyObjectImmediate(previous);
            BuildHud(ui,parent);
            Undo.RegisterCreatedObjectUndo(ui.HudPanel,"Create Gothic HUD");
            ui.HudPanel.transform.SetSiblingIndex(order);ui.HudPanel.SetActive(active);
            SkinCanvas(ui);
            EditorUtility.SetDirty(ui);EditorSceneManager.MarkSceneDirty(ui.gameObject.scene);
            EditorSceneManager.SaveScene(ui.gameObject.scene);AssetDatabase.SaveAssets();
        }

        public static void EnsureArtImports()
        {
            Import("PanelFrame.png",true);Import("gargoyle-guardian-left.png",false);Import("ivory-glass-orb.png",false);
            LoadArt();
        }
        static void Import(string file,bool sliced)
        {
            string path=ArtFolder+"/"+file;if(!File.Exists(path))return;
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var importer=AssetImporter.GetAtPath(path) as TextureImporter;if(importer==null)return;
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
            importer.spritePixelsPerUnit=100;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;
            importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Bilinear;
            importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=2048;
            importer.spriteBorder=sliced?new Vector4(250,250,250,250):Vector4.zero;
            var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);
            settings.spriteMeshType=SpriteMeshType.FullRect;settings.spriteAlignment=(int)SpriteAlignment.Center;
            importer.SetTextureSettings(settings);importer.SaveAndReimport();
        }
        static void LoadArt()
        {
            font=AssetDatabase.LoadAssetAtPath<Font>("Assets/SurvivalLegend/Resources/Art/Fonts/NotoSansKR-Regular.otf");
            frameSprite=AssetDatabase.LoadAssetAtPath<Sprite>(ArtFolder+"/PanelFrame.png");
            guardianSprite=AssetDatabase.LoadAssetAtPath<Sprite>(ArtFolder+"/gargoyle-guardian-left.png");
            orbSprite=AssetDatabase.LoadAssetAtPath<Sprite>(ArtFolder+"/ivory-glass-orb.png");
            if(orbSprite==null)orbSprite=AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        }

        public static void BuildHud(SurvivorCanvasUI ui,Transform parent)
        {
            EnsureArtImports();ui.OrbHud=true;
            var hud=Rect("HUD",parent,0,0,1280,720);ui.HudPanel=hud.gameObject;
            ui.SectorLabel=Text("Sector",hud,"SURVIVAL LEGEND\nSECTOR 01  /  생존의 전장",19,13,240,35,10,Muted,TextAnchor.UpperLeft);
            ui.Status=Text("RunStatus",hud,"LV. 1  ·  00:00  ·  처치 0",928,13,290,26,11,Muted,TextAnchor.MiddleRight);
            ui.Pause=Button("Pause",hud,"Ⅱ",1228,10,36,30,14);
            ui.Notice=Text("ContextNotice",hud,"",370,484,540,28,12,Paper);

            var elite=Rect("EliteStatus",hud,429,44,422,40);ui.ElitePanel=elite.gameObject;
            ui.EliteLabel=Text("EliteName",elite,"정예",10,0,402,22,12,Brass);
            Framed("BarFrame",elite,0,25,422,12,20,false);
            ui.EliteFill=Fill("EliteHealth",elite,5,29,412,4,new Color(.70f,.16f,.19f));
            elite.gameObject.SetActive(false);

            var badge=Framed("DodgeBadge",hud,549,520,182,36,19,false);
            ui.DodgeLabel=Text("Dodge",badge,"SHIFT  회피  2/2  준비",10,5,162,26,10,Paper);
            var panel=Framed("SkillPanel",hud,402,568,476,144,9,true);
            ui.Slots=new SkillSlotUI[6];
            int[] keys={0,1,2,4,5};
            for(int n=0;n<keys.Length;n++)
            {
                int key=keys[n];ui.Slots[key]=BuildCenterSlot(panel,key,16+n*90,14);
            }
            ui.KitLabel=Text("KitSummary",panel,"",17,112,150,18,10,Muted,TextAnchor.MiddleLeft);
            ui.CombatStatsLabel=Text("CombatStats",panel,"",309,112,150,18,10,Muted,TextAnchor.MiddleRight);
            ui.XpLabel=Text("ExperienceLabel",panel,"",179,112,118,18,9,Muted);
            ui.XpFill=Fill("Experience",panel,17,129,442,3,new Color(.39f,.64f,.60f));

            Text("UltimateTitle",hud,"U L T I M A T E",263,532,134,17,9,Brass);
            Text("UltimateSubtitle",hud,"궁극기",281,550,98,17,10,Muted);
            Text("HealthTitle",hud,"V I T A L I T Y",883,532,134,17,9,Brass);
            Text("HealthSubtitle",hud,"체력",901,550,98,17,10,Muted);

            // Art has transparent padding: 134px quads keep the visible glass close to r60.
            var left=Rect("UltimateOrb",hud,263,557,134,134);
            var right=Rect("HealthOrb",hud,883,557,134,134);
            var healthHit=right.gameObject.AddComponent<Image>();healthHit.color=Color.clear;healthHit.raycastTarget=true;
            var leftBase=Orb("DarkGlass",left,Color.gray*.17f);leftBase.color=new Color(.10f,.12f,.13f,1);
            ui.UltimateFill=Orb("Charge",left,new Color(1f,.76f,.30f));Vertical(ui.UltimateFill);
            var rightBase=Orb("DarkGlass",right,Dark);rightBase.color=new Color(.10f,.12f,.13f,1);
            ui.HealthFill=Orb("Vitality",right,new Color(.90f,.12f,.16f));Vertical(ui.HealthFill);

            // Matching guardians face inward and sit outside the orb, never over its values.
            if(guardianSprite!=null)
            {
                var guardian=Image("LeftGuardian",hud,204,536,175,175,guardianSprite,Color.white,false);guardian.preserveAspect=true;
                var mirror=Image("RightGuardian",hud,901,536,175,175,guardianSprite,Color.white,false);mirror.preserveAspect=true;mirror.rectTransform.localScale=new Vector3(-1,1,1);
            }
            // Inputs/labels follow the decoration in sibling order so neither can obscure them.
            var slot=left.gameObject.AddComponent<SkillSlotUI>();ui.Slots[3]=slot;
            var input=Rect("Cast_R",hud,270,564,120,120);
            var hit=input.gameObject.AddComponent<Image>();hit.sprite=orbSprite;hit.color=new Color(1,1,1,.001f);hit.raycastTarget=true;
            slot.Cast=input.gameObject.AddComponent<Button>();slot.Cast.targetGraphic=hit;slot.Cast.transition=Selectable.Transition.None;
            Framed("KeyPlate",input,47,5,26,25,34,false);
            slot.Key=Text("Key",input,"R",49,7,22,21,14,Paper);
            slot.Icon=Rect("Icon",input,40,29,40,40).gameObject.AddComponent<RawImage>();slot.Icon.raycastTarget=false;
            slot.CooldownFill=Image("CooldownShade",input,-7,-7,134,134,orbSprite,new Color(0,0,0,.65f),false);Vertical(slot.CooldownFill);slot.CooldownFill.fillOrigin=1;slot.CooldownFill.fillAmount=0;
            slot.Cooldown=Text("Cooldown",input,"",13,43,94,35,22,Paper);
            Image("ReadinessBackdrop",input,8,69,104,44,CanvasUiBuilder.FlatSprite(),new Color(.02f,.035f,.04f,.80f),false);
            ui.UltimateLabel=Text("Readiness",input,"충전 0%",10,70,100,20,12,Paper);
            slot.Name=Text("SkillName",input,"",10,91,100,20,11,Paper);
            slot.Level=Text("SkillLevel",input,"",78,8,27,17,8,Brass);
            slot.QuickCast=QuickCast(hud,291,693,78);
            ui.HealthLabel=Text("HealthValue",hud,"120",899,601,102,48,29,Paper);
            ui.HealthLabel.resizeTextForBestFit=false;
            ui.HealthMaximumLabel=Text("HealthMaximum",hud,"/ 120",899,641,102,22,12,Paper);
            ui.ShieldLabel=Text("Shield",hud,"생명력 100%",882,692,136,22,10,Muted);
            EditorUtility.SetDirty(ui);
        }

        static SkillSlotUI BuildCenterSlot(Transform parent,int index,float x,float y)
        {
            var root=Rect("Skill_"+"QWERDF"[index],parent,x,y,84,104);
            var slot=root.gameObject.AddComponent<SkillSlotUI>();
            slot.Cast=Button("Cast",root,"",1,0,82,76,12);
            slot.Icon=Rect("Icon",slot.Cast.transform,15,8,52,48).gameObject.AddComponent<RawImage>();slot.Icon.raycastTarget=false;
            slot.CooldownFill=Fill("CooldownShade",slot.Cast.transform,7,6,68,64,new Color(.015f,.02f,.02f,.82f));
            slot.CooldownFill.fillMethod=UnityEngine.UI.Image.FillMethod.Vertical;slot.CooldownFill.fillOrigin=1;slot.CooldownFill.fillAmount=0;
            slot.Cooldown=Text("Cooldown",slot.Cast.transform,"",8,15,66,32,22,Paper);
            slot.Key=Text("Key",slot.Cast.transform,"QWERDF"[index].ToString(),8,5,15,17,10,Paper);
            slot.Level=Text("Level",slot.Cast.transform,index<4?"Lv.1":"",48,5,26,15,8,Muted,TextAnchor.MiddleRight);
            slot.Name=Text("Name",slot.Cast.transform,"",7,53,68,19,11,Paper);
            slot.QuickCast=QuickCast(root,7,80,72);
            return slot;
        }

        public static void SkinCanvas(SurvivorCanvasUI ui)
        {
            LoadArt();
            foreach(var panel in new[]{ui.TitlePanel,ui.LoadoutPanel,ui.PausePanel,ui.AugmentPanel,ui.ResultsPanel,ui.TransitionPanel})
            {
                if(panel==null)continue;
                var image=panel.GetComponent<Image>();if(image!=null)ApplyFrame(image,8,true);
                foreach(var button in panel.GetComponentsInChildren<Button>(true))SkinButton(button);
            }
            foreach(var label in ui.GetComponentsInChildren<Text>(true))
            {
                if(font!=null)label.font=font;
                label.raycastTarget=false;
                if(label!=ui.HealthLabel)CanvasUiBuilder.FitText(label);
            }
            SkinSavedCards();
        }
        static void SkinSavedCards()
        {
            foreach(string file in new[]{"CharacterCard.prefab","AugmentCard.prefab"})
            {
                string path=PrefabFolder+"/"+file;if(!File.Exists(path))continue;
                var prefab=PrefabUtility.LoadPrefabContents(path);
                try
                {
                    // Tier cards own their frame and reveal hierarchy independently of the HUD theme.
                    if (prefab.GetComponent<AugmentCardUI>() != null && prefab.transform.Find("VisualRoot") != null) continue;
                    var image=prefab.GetComponent<Image>();if(image!=null)ApplyFrame(image,9,true);
                    foreach(var button in prefab.GetComponentsInChildren<Button>(true))SkinButton(button);
                    foreach(var label in prefab.GetComponentsInChildren<Text>(true)){if(font!=null)label.font=font;CanvasUiBuilder.FitText(label);label.raycastTarget=false;}
                    PrefabUtility.SaveAsPrefabAsset(prefab,path);
                }
                finally{PrefabUtility.UnloadPrefabContents(prefab);}
            }
        }
        static void SkinButton(Button button)
        {
            if(button==null)return;var image=button.GetComponent<Image>();if(image==null)return;
            ApplyFrame(image,18,true);button.targetGraphic=image;
            var colors=button.colors;colors.normalColor=Color.white;colors.highlightedColor=new Color(1,.94f,.75f);
            colors.pressedColor=new Color(.72f,.70f,.59f);colors.selectedColor=colors.highlightedColor;
            colors.disabledColor=new Color(.44f,.46f,.47f,.85f);button.colors=colors;
        }
        static void ApplyFrame(Image image,float multiplier,bool raycast)
        {
            image.sprite=frameSprite;image.type=frameSprite!=null?UnityEngine.UI.Image.Type.Sliced:UnityEngine.UI.Image.Type.Simple;
            image.pixelsPerUnitMultiplier=multiplier;image.color=frameSprite!=null?Color.white:Dark;
            image.raycastTarget=raycast;
        }
        static RectTransform Rect(string name,Transform parent,float x,float y,float width,float height)
        {
            var rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();rect.SetParent(parent,false);
            rect.anchorMin=rect.anchorMax=new Vector2(0,1);rect.pivot=new Vector2(.5f,.5f);
            rect.sizeDelta=new Vector2(width,height);rect.anchoredPosition=new Vector2(x+width*.5f,-y-height*.5f);return rect;
        }
        static RectTransform Framed(string name,Transform parent,float x,float y,float width,float height,float multiplier,bool raycast)
        {var rect=Rect(name,parent,x,y,width,height);ApplyFrame(rect.gameObject.AddComponent<Image>(),multiplier,raycast);return rect;}
        static Image Image(string name,Transform parent,float x,float y,float width,float height,Sprite sprite,Color color,bool raycast)
        {var image=Rect(name,parent,x,y,width,height).gameObject.AddComponent<Image>();image.sprite=sprite;image.color=color;image.raycastTarget=raycast;return image;}
        static Image Orb(string name,Transform parent,Color tint)=>Image(name,parent,0,0,134,134,orbSprite,tint,false);
        static void Vertical(Image image){image.type=UnityEngine.UI.Image.Type.Filled;image.fillMethod=UnityEngine.UI.Image.FillMethod.Vertical;image.fillOrigin=0;image.fillAmount=1;}
        static Image Fill(string name,Transform parent,float x,float y,float width,float height,Color tint)
        {
            var image=Image(name,parent,x,y,width,height,CanvasUiBuilder.FlatSprite(),tint,false);
            image.type=UnityEngine.UI.Image.Type.Filled;image.fillMethod=UnityEngine.UI.Image.FillMethod.Horizontal;image.fillAmount=1;return image;
        }
        static Text Text(string name,Transform parent,string value,float x,float y,float width,float height,int size,Color color,TextAnchor alignment=TextAnchor.MiddleCenter)
        {
            var text=Rect(name,parent,x,y,width,height).gameObject.AddComponent<Text>();text.font=font;text.fontSize=size;text.text=value;
            text.color=color;text.alignment=alignment;text.supportRichText=false;text.raycastTarget=false;
            CanvasUiBuilder.FitText(text);
            var shadow=text.gameObject.AddComponent<Shadow>();shadow.effectColor=new Color(0,0,0,.8f);shadow.effectDistance=new Vector2(0,-1);
            return text;
        }
        static Button Button(string name,Transform parent,string label,float x,float y,float width,float height,int size)
        {
            var rect=Framed(name,parent,x,y,width,height,18,true);var button=rect.gameObject.AddComponent<Button>();SkinButton(button);
            Text("Label",rect,label,5,3,width-10,height-6,size,Paper);return button;
        }
        static Toggle QuickCast(Transform parent,float x,float y,float width)
        {
            var rect=Rect("QuickCast",parent,x,y,width,18);var toggle=rect.gameObject.AddComponent<Toggle>();
            var hit=rect.gameObject.AddComponent<Image>();hit.color=Color.clear;hit.raycastTarget=true;
            var box=Framed("Box",rect,0,3,15,15,34,false).GetComponent<Image>();
            var check=Image("Check",rect,5,8,5,5,CanvasUiBuilder.FlatSprite(),new Color(.55f,.86f,.72f),false);
            toggle.targetGraphic=box;toggle.graphic=check;
            Text("Label",rect,"즉시 시전",18,1,width-18,18,10,Muted,TextAnchor.MiddleLeft);
            return toggle;
        }
    }
}
