using System;
using UnityEngine;
using SurvivalLegend.Data;
using SurvivalLegend.World;

namespace SurvivalLegend
{
    [DefaultExecutionOrder(100)]
    public sealed class SurvivorGame : MonoBehaviour
    {
        public static SurvivorGame Instance {get;private set;}
        [Header("Authored run configuration")]
        public GameDatabase Database;
        public ArenaMapView ArenaMap;
        public GameContentSnapshot Content {get;private set;}
        public GameSimulation Simulation {get;private set;}
        readonly RunState unavailableState=new RunState{Phase=RunPhase.Title};
        public RunState State=>Simulation?.State??unavailableState;
        public int Generation {get;private set;}
        public string InitializationError {get;private set;}
        public bool CanStart=>Content!=null&&string.IsNullOrEmpty(InitializationError);
        public event Action<int> RunReset;
        [NonSerialized] public Vector2 CursorWorld=new Vector2(720,720);
        [NonSerialized] public bool PointerOverUi;
        public bool QuickCast;
        public bool[] QuickCastSlots=new bool[6];
        float accumulator;

        void Awake(){Instance=this;Initialize();}
        void OnDestroy(){if(Instance==this)Instance=null;}
        public bool Initialize()
        {
            if(!LoadContent())return false;
            ResetSimulation(1,null,null,null,RunPhase.Title);
            return CanStart&&Simulation!=null;
        }
        bool LoadContent()
        {
            try
            {
                if(Database==null)throw new InvalidOperationException("Assign a GameDatabase asset on the Game Session before playing.");
                var snapshot=Database.BuildSnapshot();
                Content=ArenaMap!=null?ArenaMap.BuildSnapshot(snapshot):snapshot;
                InitializationError=null;return true;
            }
            catch(Exception error)
            {
                Content=null;Simulation=null;InitializationError=error.Message;accumulator=0;
                Generation++;RunReset?.Invoke(Generation);
                Debug.LogError("Survival Legend cannot start: "+InitializationError,this);return false;
            }
        }
        void ResetSimulation(uint seed,string specialD,string specialF,string character,RunPhase phase)
        {
            try
            {
                Simulation=new GameSimulation(seed,specialD,specialF,character,Content);
                Simulation.State.Phase=phase;accumulator=0;Generation++;RunReset?.Invoke(Generation);
            }
            catch(Exception error)
            {
                Simulation=null;InitializationError=error.Message;accumulator=0;Generation++;RunReset?.Invoke(Generation);
                Debug.LogError("Survival Legend cannot start: "+InitializationError,this);
            }
        }
        public void StartRun(uint seed=1,string specialD=null,string specialF=null,string character=null)
        {
            if(LoadContent())ResetSimulation(seed,specialD,specialF,character,RunPhase.Playing);
        }
        public void ReturnToTitle(){if(LoadContent())ResetSimulation(1,null,null,null,RunPhase.Title);}
        public void TogglePause()=>Simulation?.TogglePause();
        public void ChooseAugment(int index)=>Simulation?.ChooseAugment(index);
        public void RerollAugment(int index)=>Simulation?.RerollAugment(index);
        public void CastSkill(int slot,Vector2 target)=>Simulation?.CastSkill(slot,target);
        public void UseSpecial(int slot,Vector2 target)=>Simulation?.UseSpecial(slot,target);
        public void Dodge(Vector2 target)=>Simulation?.Dodge(target);
        public void MoveTo(Vector2 point)=>Simulation?.MoveTo(point);
        public void AttackMove(Vector2 point)=>Simulation?.AttackMove(point);
        public void Attack(int id)=>Simulation?.Attack(id);
        public void Stop()=>Simulation?.Stop();
        void Update()
        {
            if(Simulation==null)return;
            accumulator+=Mathf.Min(Time.unscaledDeltaTime,.25f);
            float step=Content.Config.FixedStep;
            while(accumulator>=step){Simulation.Step(step);accumulator-=step;}
        }
        public void RequestCast(int slot)
        {
            if(Simulation==null||!Simulation.CombatActive||State.DodgeRemaining>0||slot<0||slot>=6)return;
            State.AimSlot=-1;
            if(slot<4&&(State.Skills[slot].Remaining>0||(Content.Skills[State.Skills[slot].Id].Ultimate&&(State.Ultimate<Content.Config.UltimateMaximum||State.Buffs.Exists(b=>b.Id==State.Skills[slot].Id))))){CastSkill(slot,CursorWorld);return;}
            if(slot>=4&&State.Specials[slot-4].Remaining>0){UseSpecial(slot-4,CursorWorld);return;}
            bool self=slot<4?Content.Skills[State.Skills[slot].Id].Targeting.Kind==TargetKind.Self:Content.Specials[State.Specials[slot-4].Id].Kind!="pull";
            if(self||QuickCast||(QuickCastSlots!=null&&QuickCastSlots.Length>slot&&QuickCastSlots[slot])){if(slot>=4)UseSpecial(slot-4,CursorWorld);else CastSkill(slot,CursorWorld);}
            else State.AimSlot=slot;
        }
        void OnApplicationFocus(bool focus){if(!focus&&Simulation!=null&&Simulation.CombatActive)TogglePause();}
    }
}
