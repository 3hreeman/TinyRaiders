using UnityEngine;
using System.Collections.Generic;

namespace SurvivalLegend.World
{
    public sealed class ProjectileView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer head, glow;
        [SerializeField] private LineRenderer trail;
        [SerializeField] private TrailRenderer nativeTrail;
        [SerializeField] private ParticleSystem flightParticles;
        [SerializeField] private float trailSeconds=.24f;
        readonly List<Vector3> trailPoints=new List<Vector3>(48);
        readonly List<float> trailAges=new List<float>(48);
        bool releasing;
        float tailRemaining;
        public bool TailAlive=>tailRemaining>0;
        public int BoundId { get; private set; } = -1;

        public void Bind(ProjectileState shot)
        {
            bool fresh=BoundId!=shot.Id;BoundId = shot.Id;
            var position = WorldProjection.WorldToScene(shot.Position, shot.Elevation);
            transform.position = position;
            Vector2 velocity = shot.Velocity;
            Vector2 projected = WorldProjection.DirectionToScene(velocity).normalized;
            if (projected.sqrMagnitude < .01f) projected = Vector2.right;
            float angle = Mathf.Atan2(projected.y, projected.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);
            var color = shot.Hostile ? new Color(1f, .45f, .34f) : WorldPrimitives.Parse(shot.Color,
                shot.Visual == "fireball" ? new Color(1f, .59f, .35f) : new Color(.88f, 1f, .79f));
            bool fireball = shot.Visual == "fireball";
            if(fresh)
            {
                releasing=false;tailRemaining=0;trailPoints.Clear();trailAges.Clear();
                if(nativeTrail!=null){nativeTrail.Clear();nativeTrail.emitting=false;nativeTrail.time=10000;nativeTrail.enabled=true;nativeTrail.startColor=color;nativeTrail.endColor=new Color(color.r,color.g,color.b,0);nativeTrail.widthMultiplier=fireball?.09f:shot.Pierce?.055f:.022f;}
                if(flightParticles!=null)
                {
                    flightParticles.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);flightParticles.useAutoRandomSeed=false;flightParticles.randomSeed=(uint)shot.Id+1;
                    var main=flightParticles.main;main.startColor=color;main.startSize=fireball?.08f:.035f;
                    var emission=flightParticles.emission;emission.rateOverTime=fireball?28:shot.Pierce?18:8;
                    flightParticles.Play(false);flightParticles.Pause(false);
                }
            }
            if (head != null)
            {
                head.enabled=true;
                head.color = fireball ? Color.Lerp(color, Color.white, .48f) : color;
                head.transform.localScale = fireball ? new Vector3(.1f, .1f, 1) : new Vector3(.04f, .04f, 1);
            }
            if (glow != null)
            {
                glow.enabled = fireball;
                glow.color = new Color(color.r, color.g, color.b, .42f);
                glow.transform.localScale = new Vector3(.26f, .26f, 1);
            }
            if(trail!=null&&nativeTrail!=null)trail.enabled=false;
            if (trail != null&&nativeTrail==null)
                WorldPrimitives.Line(trail, position - new Vector3(projected.x, projected.y) * (fireball ? .18f : .11f),
                    position + new Vector3(projected.x, projected.y) * (fireball ? 0 : .09f),
                    fireball ? .07f : shot.Hostile || shot.Pierce ? .04f : .02f, color);
        }
        public void Release()
        {
            releasing=true;tailRemaining=Mathf.Max(trailSeconds,.35f);
            if(head!=null)head.enabled=false;if(glow!=null)glow.enabled=false;if(trail!=null)trail.enabled=false;
            if(flightParticles!=null)flightParticles.Stop(false,ParticleSystemStopBehavior.StopEmitting);
        }
        public void AdvanceVisual(float delta)
        {
            if(delta<=0)return;
            if(releasing)tailRemaining-=delta;
            if(flightParticles!=null)flightParticles.Simulate(delta,false,false,false);
            if(nativeTrail==null)return;
            for(int i=trailAges.Count-1;i>=0;i--){trailAges[i]+=delta;if(trailAges[i]>trailSeconds){trailAges.RemoveAt(i);trailPoints.RemoveAt(i);}}
            if(!releasing&&(trailPoints.Count==0||Vector3.Distance(trailPoints[trailPoints.Count-1],transform.position)>.01f))
            {if(trailPoints.Count>=48){trailPoints.RemoveAt(0);trailAges.RemoveAt(0);}trailPoints.Add(transform.position);trailAges.Add(0);}
            // Timestamp ownership stays here: native real-time expiry cannot eat paused trails.
            nativeTrail.Clear();foreach(var point in trailPoints)nativeTrail.AddPosition(point);
        }
        public void ResetView()
        {
            BoundId=-1;releasing=false;tailRemaining=0;trailPoints.Clear();trailAges.Clear();
            if(trail!=null)trail.enabled=false;if(nativeTrail!=null){nativeTrail.Clear();nativeTrail.enabled=false;}
            if(flightParticles!=null)flightParticles.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);
        }
#if UNITY_EDITOR
        public void EditorSet(SpriteRenderer projectileHead, SpriteRenderer projectileGlow, LineRenderer projectileTrail)
        { head = projectileHead; glow = projectileGlow; trail = projectileTrail; }
        public void EditorSetParticles(TrailRenderer native,ParticleSystem particles){nativeTrail=native;flightParticles=particles;}
#endif
    }
}
