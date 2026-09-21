using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using SurvivalLegend.World;

namespace SurvivalLegend
{
    [DefaultExecutionOrder(175)]
    public sealed class PlayerInputRouter : MonoBehaviour
    {
        public SurvivorGame Game;
        public Camera WorldCamera;
        public FogOfWarPresenter Fog;
        readonly List<RaycastResult> uiHits=new List<RaycastResult>();
        PointerEventData pointerEvent;
        EventSystem pointerEventSystem;
        bool holdingMove;
        float moveRepeat;
        int generation=-1;

        void Update()
        {
            if(Game==null)return;
            if(generation!=Game.Generation){generation=Game.Generation;holdingMove=false;}
            var mouse=Mouse.current;var keyboard=Keyboard.current;
            if(mouse!=null)
            {
                Vector2 screen=mouse.position.ReadValue();
                Game.CursorWorld=WorldProjection.ScreenToWorld(WorldCamera,screen);
                Game.PointerOverUi=PointerBlocked(screen);
            }
            var simulation=Game.Simulation;if(simulation==null)return;
            var state=Game.State;
            if(keyboard!=null)
            {
                if(keyboard.escapeKey.wasPressedThisFrame){if(state.AimSlot>=0)state.AimSlot=-1;else Game.TogglePause();}
                if(simulation.CombatActive)
                {
                    if(keyboard.qKey.wasPressedThisFrame)Game.RequestCast(0);
                    if(keyboard.wKey.wasPressedThisFrame)Game.RequestCast(1);
                    if(keyboard.eKey.wasPressedThisFrame)Game.RequestCast(2);
                    if(keyboard.rKey.wasPressedThisFrame)Game.RequestCast(3);
                    if(keyboard.dKey.wasPressedThisFrame)Game.RequestCast(4);
                    if(keyboard.fKey.wasPressedThisFrame)Game.RequestCast(5);
                    if(keyboard.leftShiftKey.wasPressedThisFrame)Game.Dodge(Game.CursorWorld);
                    if(keyboard.aKey.wasPressedThisFrame)state.AimSlot=6;
                    if(keyboard.sKey.wasPressedThisFrame){Game.Stop();state.AimSlot=-1;holdingMove=false;}
                }
            }
            if(mouse==null||!simulation.CombatActive||Game.PointerOverUi){holdingMove=false;return;}
            if(!mouse.rightButton.isPressed)holdingMove=false;
            if(mouse.rightButton.wasPressedThisFrame)
            {
                holdingMove=false;
                if(state.AimSlot>=0){state.AimSlot=-1;return;}
                var enemy=EnemyAtCursor();
                if(enemy!=null)Game.Attack(enemy.Id);
                else{Game.MoveTo(Game.CursorWorld);holdingMove=true;moveRepeat=.25f;}
            }
            if(holdingMove){moveRepeat-=Time.unscaledDeltaTime;if(moveRepeat<=0){Game.MoveTo(Game.CursorWorld);moveRepeat=.25f;}}
            if(mouse.leftButton.wasPressedThisFrame&&state.AimSlot>=0)
            {
                holdingMove=false;int slot=state.AimSlot;state.AimSlot=-1;
                if(slot==6){var enemy=EnemyAtCursor();if(enemy!=null)Game.Attack(enemy.Id);else Game.AttackMove(Game.CursorWorld);}
                else if(slot>=4)Game.UseSpecial(slot-4,Game.CursorWorld);else Game.CastSkill(slot,Game.CursorWorld);
            }
        }

        bool PointerBlocked(Vector2 screen)
        {
            var phase=Game.State.Phase;
            if(phase!=RunPhase.Playing&&phase!=RunPhase.LevelUp)return true;
            var viewport=WorldCamera!=null?WorldCamera.pixelRect:WorldProjection.FieldViewportPixels(Screen.width,Screen.height);
            if(!viewport.Contains(screen))return true;
            var system=EventSystem.current;if(system==null)return false;
            bool hovered=system.IsPointerOverGameObject();
            if(pointerEvent==null||pointerEventSystem!=system){pointerEventSystem=system;pointerEvent=new PointerEventData(system);}
            pointerEvent.Reset();pointerEvent.position=screen;uiHits.Clear();system.RaycastAll(pointerEvent,uiHits);
            return hovered||uiHits.Count>0;
        }

        EnemyState EnemyAtCursor()
        {
            EnemyState target=null;float nearest=float.MaxValue;
            foreach(var enemy in Game.State.Enemies)
            {
                if(Fog!=null&&!Fog.IsVisible(enemy.Position))continue;
                float distance=Vector2.Distance(enemy.Position,Game.CursorWorld);
                if(enemy.Hp>0&&distance<enemy.Radius+20&&distance<nearest){target=enemy;nearest=distance;}
            }
            return target;
        }
    }
}
