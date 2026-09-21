using UnityEngine;

namespace SurvivalLegend.World
{
    /// <summary>Presentation-only clock: particles never read or write gameplay or its RNG.</summary>
    public sealed class SkillFxView : MonoBehaviour
    {
        [SerializeField] ParticleSystem[] systems;
        [SerializeField] TextMesh label;
        FxProfile profile;
        float age,tailRemaining,flightDuration;
        bool emitting,sustained,flight;
        Vector3 flightStart,flightEnd;
        Renderer[] renderers;
        public void SetVisible(bool visible)
        {
            if(renderers==null)renderers=GetComponentsInChildren<Renderer>(true);
            foreach(var renderer in renderers)renderer.enabled=visible;
        }
        public FxProfile Profile=>profile;
        public bool Alive=>emitting||tailRemaining>0;
        public bool Following=>profile!=null&&profile.FollowPlayer&&!flight;

        public void Begin(FxProfile definition,Vector3 position,float radius,uint seed,bool sustain=false,Color? tintOverride=null)
        {
            ResetView();SetVisible(true);profile=definition;transform.position=position;transform.rotation=Quaternion.identity;
            transform.localScale=Vector3.one*profile.Scale*(profile.ScaleByRadius?Mathf.Clamp(radius/100f,.35f,3.2f):1);
            age=0;tailRemaining=profile.Tail;emitting=true;sustained=sustain;
            if(systems!=null)for(int i=0;i<systems.Length;i++)
            {
                var system=systems[i];if(system==null)continue;
                system.useAutoRandomSeed=false;system.randomSeed=seed+(uint)(i*7919)+1;
                var main=system.main;main.startColor=tintOverride??profile.Tint;
                system.Play(false);system.Simulate(0,false,true,false);system.Pause(false);
            }
            if(label!=null){label.gameObject.SetActive(profile.Id=="event.levelup");label.text="LEVEL UP";label.color=profile.Tint;label.transform.localPosition=new Vector3(0,.68f,0);}
        }
        public void SetFlight(Vector3 from,Vector3 to,float duration)
        {flight=true;flightStart=from;flightEnd=to;flightDuration=Mathf.Max(.04f,duration);transform.position=from;}
        public void Release()
        {
            if(!emitting)return;emitting=false;sustained=false;tailRemaining=profile!=null?profile.Tail:0;
            if(systems!=null)foreach(var system in systems)if(system!=null)system.Stop(false,ParticleSystemStopBehavior.StopEmitting);
        }
        public void Advance(float delta)
        {
            if(delta<=0||profile==null)return;
            age+=delta;
            if(flight)transform.position=Vector3.Lerp(flightStart,flightEnd,Mathf.Clamp01(age/flightDuration));
            if(emitting&&!sustained&&age>=profile.Duration)Release();
            if(!emitting)tailRemaining-=delta;
            if(systems!=null)foreach(var system in systems)if(system!=null)system.Simulate(delta,false,false,false);
            if(label!=null&&label.gameObject.activeSelf)
            {label.transform.localPosition=new Vector3(0,.68f+age*.32f,0);var tint=profile.Tint;tint.a=Mathf.Clamp01((profile.Duration+profile.Tail-age)/.35f);label.color=tint;}
        }
        public void ResetView()
        {
            if(systems!=null)foreach(var system in systems)if(system!=null)system.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);
            if(label!=null)label.gameObject.SetActive(false);
            profile=null;age=0;tailRemaining=0;emitting=false;sustained=false;flight=false;transform.localScale=Vector3.one;
        }
#if UNITY_EDITOR
        public void EditorSet(ParticleSystem[] particles,TextMesh text){systems=particles;label=text;}
#endif
    }
}
