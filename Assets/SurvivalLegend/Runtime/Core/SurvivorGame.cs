using UnityEngine;
using UnityEngine.InputSystem;

namespace SurvivalLegend
{
    [DefaultExecutionOrder(100)]
    public sealed class SurvivorGame : MonoBehaviour
    {
        public static SurvivorGame Instance {get;private set;}
        public GameSimulation Simulation {get;private set;}
        public RunState State => Simulation.State;
        public Vector2 CursorWorld = new Vector2(720,720);
        public bool PointerOverUi;
        public bool QuickCast;
        public bool[] QuickCastSlots = new bool[6];
        float accumulator;
        float moveRepeat;
        bool holdingMove;
        void Awake() {Instance=this;Simulation=new GameSimulation();State.Phase=RunPhase.Title;}
        void OnDestroy() {if(Instance==this)Instance=null;}
        public void StartRun(uint seed=1,string specialD="heal",string specialF="haste",string character="archer") {Simulation=new GameSimulation(seed,specialD,specialF,character);accumulator=0;}
        public void ReturnToTitle() {Simulation=new GameSimulation();State.Phase=RunPhase.Title;accumulator=0;}
        public void TogglePause()=>Simulation.TogglePause();
        public void ChooseAugment(int index)=>Simulation.ChooseAugment(index);
        public void RerollAugment(int index)=>Simulation.RerollAugment(index);
        public void CastSkill(int slot,Vector2 target)=>Simulation.CastSkill(slot,target);
        public void UseSpecial(int slot,Vector2 target)=>Simulation.UseSpecial(slot,target);
        public void Dodge(Vector2 target)=>Simulation.Dodge(target);
        public void MoveTo(Vector2 point)=>Simulation.MoveTo(point);
        public void AttackMove(Vector2 point)=>Simulation.AttackMove(point);
        public void Attack(int id)=>Simulation.Attack(id);
        public void Stop()=>Simulation.Stop();
        void Update()
        {
            ReadInput();
            accumulator+=Mathf.Min(Time.unscaledDeltaTime,.25f);
            const float step=1f/60;
            while(accumulator>=step) {Simulation.Step(step);accumulator-=step;}
        }
        void ReadInput()
        {
            var keyboard=Keyboard.current;var mouse=Mouse.current;
            if(keyboard!=null)
            {
                if(keyboard.escapeKey.wasPressedThisFrame) {if(State.AimSlot>=0)State.AimSlot=-1;else TogglePause();}
                if(keyboard.spaceKey.wasPressedThisFrame)TogglePause();
                if(Simulation.CombatActive)
                {
                    if(keyboard.qKey.wasPressedThisFrame)RequestCast(0);
                    if(keyboard.wKey.wasPressedThisFrame)RequestCast(1);
                    if(keyboard.eKey.wasPressedThisFrame)RequestCast(2);
                    if(keyboard.rKey.wasPressedThisFrame)RequestCast(3);
                    if(keyboard.dKey.wasPressedThisFrame)RequestCast(4);
                    if(keyboard.fKey.wasPressedThisFrame)RequestCast(5);
                    if(keyboard.leftShiftKey.wasPressedThisFrame)Dodge(CursorWorld);
                    if(keyboard.aKey.wasPressedThisFrame)State.AimSlot=6;
                    if(keyboard.sKey.wasPressedThisFrame){Stop();State.AimSlot=-1;}
                }
            }
            if(mouse==null||!Simulation.CombatActive||PointerOverUi){holdingMove=false;return;}
            if(!mouse.rightButton.isPressed)holdingMove=false;
            if(mouse.rightButton.wasPressedThisFrame)
            {
                holdingMove=false;
                if(State.AimSlot>=0){State.AimSlot=-1;return;}
                var target=EnemyAtCursor();
                if(target!=null)Attack(target.Id);else{MoveTo(CursorWorld);holdingMove=true;moveRepeat=.25f;}
            }
            if(holdingMove){moveRepeat-=Time.unscaledDeltaTime;if(moveRepeat<=0){MoveTo(CursorWorld);moveRepeat=.25f;}}
            if(mouse.leftButton.wasPressedThisFrame&&State.AimSlot>=0)
            {
                holdingMove=false;
                int slot=State.AimSlot;State.AimSlot=-1;
                if(slot==6){var enemy=EnemyAtCursor();if(enemy!=null)Attack(enemy.Id);else AttackMove(CursorWorld);}else if(slot>=4)UseSpecial(slot-4,CursorWorld);else CastSkill(slot,CursorWorld);
            }
        }
        EnemyState EnemyAtCursor()
        {
            EnemyState target=null;float distance=float.MaxValue;
            foreach(var enemy in State.Enemies){float d=Vector2.Distance(enemy.Position,CursorWorld);if(enemy.Hp>0&&d<enemy.Radius+20&&d<distance){target=enemy;distance=d;}}
            return target;
        }
        public void RequestCast(int slot)
        {
            if(!Simulation.CombatActive||State.DodgeRemaining>0||slot<0||slot>=6)return;
            State.AimSlot=-1;
            if(slot<4&&(State.Skills[slot].Remaining>0||(slot==3&&(State.Ultimate<100||State.Buffs.Exists(b=>b.Id==State.Skills[slot].Id))))) {CastSkill(slot,CursorWorld);return;}
            if(slot>=4&&State.Specials[slot-4].Remaining>0) {UseSpecial(slot-4,CursorWorld);return;}
            bool self=slot<4?Data.SurvivalLegendCatalog.Skills[State.Skills[slot].Id].Targeting.Kind==Data.TargetKind.Self:State.Specials[slot-4].Id!="blackhole";
            if(self||QuickCast||(QuickCastSlots!=null&&QuickCastSlots.Length>slot&&QuickCastSlots[slot])) {if(slot>=4)UseSpecial(slot-4,CursorWorld);else CastSkill(slot,CursorWorld);}
            else State.AimSlot=slot;
        }
        void OnApplicationFocus(bool focus) {if(!focus&&Simulation!=null&&Simulation.CombatActive)TogglePause();}
    }
}
