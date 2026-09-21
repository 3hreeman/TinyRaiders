using UnityEngine;

namespace SurvivalLegend.World
{
    [DefaultExecutionOrder(125)]
    public sealed class FogOfWarPresenter : MonoBehaviour
    {
        [SerializeField] SurvivorGame game;
        [SerializeField] SpriteRenderer overlay;
        [SerializeField,Min(100)] float visionRadius=560;
        [SerializeField,Range(32,256)] int resolution=192;
        public FogVisibilityGrid Grid { get; private set; }
        public Texture2D FogTexture { get; private set; }
        public float VisionRadius=>visionRadius;
        Color32[] pixels;
        Sprite dynamicSprite;
        int generation=-1;
        Vector2 lastPosition;
        public bool IsVisible(Vector2 p)=>Grid==null||Grid.IsVisible(p);
        public bool IsExplored(Vector2 p)=>Grid!=null&&Grid.IsExplored(p);
        void Update()=>RefreshVisibility();
        public void RefreshVisibility(bool force=false)
        {
            if(game==null||game.Content==null||overlay==null)return;
            var size=new Vector2(game.Content.Config.ArenaWidth,game.Content.Config.ArenaHeight);
            bool reset=generation!=game.Generation||Grid==null||Grid.LogicalSize!=size;
            if(reset)
            {
                generation=game.Generation;ReleaseTexture();Grid=new FogVisibilityGrid(size,resolution);
                FogTexture=new Texture2D(Grid.Width,Grid.Height,TextureFormat.RGBA32,false){name="Player exploration mask",filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};
                pixels=new Color32[Grid.Width*Grid.Height];
                dynamicSprite=Sprite.Create(FogTexture,new Rect(0,0,Grid.Width,Grid.Height),Vector2.one*.5f,100,0,SpriteMeshType.FullRect);
                overlay.sprite=dynamicSprite;overlay.sortingOrder=30000;
                var topLeft=WorldProjection.WorldToScene(Vector2.zero);var bottomRight=WorldProjection.WorldToScene(size);
                overlay.transform.position=(topLeft+bottomRight)*.5f;
                overlay.transform.localScale=new Vector3((bottomRight.x-topLeft.x)/dynamicSprite.bounds.size.x,(topLeft.y-bottomRight.y)/dynamicSprite.bounds.size.y,1);
            }
            bool active=game.State.Phase!=RunPhase.Title;
            overlay.enabled=active;
            if(!active){if(reset)Upload();return;}
            var p=game.State.Player.Position;
            if(force||reset||(lastPosition-p).sqrMagnitude>.01f)
            {Grid.Reveal(p,visionRadius);lastPosition=p;Upload();}
        }
        void Upload()
        {
            for(int y=0;y<Grid.Height;y++)for(int x=0;x<Grid.Width;x++)
                pixels[(Grid.Height-1-y)*Grid.Width+x]=new Color32(4,9,13,(byte)Mathf.RoundToInt(Grid.GetAlpha(x,y)*255));
            FogTexture.SetPixels32(pixels);FogTexture.Apply(false,false);
        }
        void ReleaseTexture()
        {if(dynamicSprite!=null)Destroy(dynamicSprite);if(FogTexture!=null)Destroy(FogTexture);dynamicSprite=null;FogTexture=null;}
        void OnDestroy()=>ReleaseTexture();
#if UNITY_EDITOR
        public void EditorSet(SurvivorGame host,SpriteRenderer renderer){game=host;overlay=renderer;}
#endif
    }
}
