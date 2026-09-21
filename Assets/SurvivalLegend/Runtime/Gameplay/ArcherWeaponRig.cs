using UnityEngine;
using Catalog = SurvivalLegend.Data.SurvivalLegendCatalog;

namespace SurvivalLegend
{
    public sealed partial class GameSimulation
    {
        // Exact default SD socket from weapon-rig.ts and visual-scale.ts. World
        // collision remains on the ground plane; elevation is presentation only.
        void PlayMotion(string motion,float angle,float duration)
        {
            var p=State.Player;
            p.Facing=angle;p.Motion=motion;p.MotionAngle=angle;
            p.AttackAnimation=duration;p.MotionDuration=duration;
        }

        Vector3 ArrowSocket(Vector2 cursor)
        {
            return WeaponSocket(cursor,false);
        }

        Vector3 WeaponSocket(Vector2 cursor,bool staff)
        {
            var p=State.Player;
            bool active=p.AttackAnimation>0;
            string motion=active?p.Motion:"idle";
            float progress=active?Mathf.Clamp01(1-p.AttackAnimation/Mathf.Max(.000001f,p.MotionDuration)):0;
            float facing=State.AimSlot>=0?Angle(cursor-p.Position):active?p.MotionAngle:p.Facing;
            float pulse=Mathf.Sin(Mathf.PI*progress);
            float stride=p.Moving?Mathf.Sin(State.Time*Content.Characters[State.Character].AnimationStride):0;
            float bob=p.Moving?Mathf.Abs(stride)*1.5f:Mathf.Sin(State.Time*2)*.45f;
            float lift=bob-(motion=="pierce"?pulse*2:motion=="frost"?pulse*5:0);
            float reach=(motion=="draw"||motion=="focus"||motion=="volley"||motion=="pierce")?-pulse*9:0;
            float raise=motion=="rain"?pulse*42:0;
            if(motion=="cast"||motion=="fireball"||motion=="blink"||motion=="gravity")reach=pulse*17;
            if(motion=="frost")raise=25*Mathf.Sin(Mathf.PI*progress*2);
            if(motion=="overload"||motion=="recover")raise=pulse*42;
            if(motion=="haste")reach=pulse*10;
            const float sdScale=.55f;
            var forward=Direction(facing);var side=new Vector2(-forward.y,forward.x);
            Vector2 offset=(forward*((staff?25:59)+reach)+side*(staff?13:0))*sdScale;
            return new Vector3(offset.x,offset.y,((staff?57:30)+raise+lift)*sdScale);
        }

        static void SetFlight(ProjectileState shot,float height,float distance)
        {
            shot.HasFlight=true;shot.FlightStartHeight=height;
            shot.FlightDistance=distance;shot.FlightTraveled=0;
        }
    }
}

