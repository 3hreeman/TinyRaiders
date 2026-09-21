using System.Collections.Generic;
using UnityEngine;

namespace SurvivalLegend.World
{
    /// <summary>Owns pooled finite tails, sustained buffs, and local level-up feedback.</summary>
    public sealed class SkillFxPresenter : MonoBehaviour
    {
        [SerializeField] SurvivorGame game;
        [SerializeField] SkillFxCatalog catalog;
        [SerializeField] Transform effectRoot;
        FogOfWarPresenter fog;
        readonly List<SkillFxView> active=new List<SkillFxView>();
        readonly Dictionary<FxProfile,Stack<SkillFxView>> pools=new Dictionary<FxProfile,Stack<SkillFxView>>();
        readonly Dictionary<string,SkillFxView> buffs=new Dictionary<string,SkillFxView>();
        readonly Dictionary<int,SkillFxView> persistentZones=new Dictionary<int,SkillFxView>();
        readonly HashSet<int> rainEmitted=new HashSet<int>();
        readonly HashSet<int> impactedZones=new HashSet<int>();
        readonly HashSet<int> seenZones=new HashSet<int>();
        readonly HashSet<string> seenBuffs=new HashSet<string>();
        readonly List<string> staleBuffs=new List<string>();
        readonly List<int> staleZones=new List<int>();
        int generation=-1,lastLevel,pooledCount;
        uint visualSeed=1;
        public SkillFxCatalog Catalog=>catalog;
        public int ActiveCount=>active.Count;
        public int InactiveCount=>pooledCount;
        public bool IsConfigured=>catalog!=null;

        public bool Handles(EffectState effect)
        {
            if(catalog==null||effect.Kind=="text"||effect.Kind=="lightning")return false;
            string key=effect.Kind=="skill"||effect.Kind=="special"?effect.Text:"event."+effect.Kind;
            return catalog.Find(key)!=null;
        }
        public void ReceiveEvents(List<EffectState> events)
        {
            if(game==null||catalog==null)return;
            EnsureGeneration();
            foreach(var effect in events)
            {
                if(effect.Kind=="text")continue;
                if(effect.Kind=="levelup"){Spawn(catalog.Find("event.levelup"),WorldProjection.WorldToScene(game.State.Player.Position),100);lastLevel=game.State.Level;continue;}
                string key=(effect.Kind=="skill"||effect.Kind=="special")?effect.Text:"event."+effect.Kind;
                var profile=catalog.Find(key);if(profile==null)continue;
                if(profile.SustainWhileBuff&&game.State.Buffs.Exists(b=>b.Id==key))continue;
                Color? tint=effect.Kind=="skill"||effect.Kind=="special"?(Color?)null:WorldPrimitives.Parse(effect.Color,profile.Tint);
                Spawn(profile,WorldProjection.WorldToScene(effect.Position,effect.Elevation),effect.Radius,false,tint);
            }
        }
        public void Present(RunState run,float delta)
        {
            if(game==null||catalog==null)return;EnsureGeneration();
            if(fog==null)fog=GetComponent<FogOfWarPresenter>();
            if(run.Phase==RunPhase.Title){Clear();lastLevel=run.Level;return;}
            if(lastLevel==0)lastLevel=run.Level;
            if(run.Level>lastLevel){Spawn(catalog.Find("event.levelup"),WorldProjection.WorldToScene(run.Player.Position),100);lastLevel=run.Level;}
            bool animate=run.Phase==RunPhase.Playing||run.Phase==RunPhase.LevelUp||run.Phase==RunPhase.Dying||run.Phase==RunPhase.EliteDeath;
            float step=animate?Mathf.Min(delta,.1f):0;
            SyncBuffs(run);SyncZones(run);
            for(int i=active.Count-1;i>=0;i--)
            {
                var view=active[i];if(view.Following)view.transform.position=WorldProjection.WorldToScene(run.Player.Position);
                view.SetVisible(fog==null||fog.IsVisible(WorldProjection.SceneToWorld(view.transform.position)));
                view.Advance(step);
                if(view.Alive)continue;active.RemoveAt(i);Return(view);
            }
        }
        void EnsureGeneration()
        {if(generation==game.Generation)return;Clear();generation=game.Generation;lastLevel=game.State.Level;visualSeed=1;}
        void SyncBuffs(RunState run)
        {
            seenBuffs.Clear();
            foreach(var buff in run.Buffs)
            {
                var profile=catalog.Find(buff.Id);if(profile==null||!profile.SustainWhileBuff)continue;
                seenBuffs.Add(buff.Id);
                if(!buffs.ContainsKey(buff.Id)){var view=Spawn(profile,WorldProjection.WorldToScene(run.Player.Position),100,true);if(view!=null)buffs.Add(buff.Id,view);}
            }
            if(run.Player.SkillShieldRemaining>0)
                foreach(var skill in run.Skills)
                {
                    var profile=catalog.Find(skill.Id);if(profile==null||!profile.SustainWhileBuff)continue;
                    bool shielding=false;foreach(var effect in game.Content.Skills[skill.Id].Effects)if(effect.Kind==SurvivalLegend.Data.DeliveryKind.Shield){shielding=true;break;}
                    if(!shielding)continue;seenBuffs.Add(skill.Id);
                    if(!buffs.ContainsKey(skill.Id)){var view=Spawn(profile,WorldProjection.WorldToScene(run.Player.Position),100,true);if(view!=null)buffs.Add(skill.Id,view);}
                }
            // D/F shields and haste have explicit source timers outside skill buffs.
            foreach(var special in run.Specials)
            {
                var definition=game.Content.Specials[special.Id];
                bool alive=definition.Kind=="haste"?run.Player.HasteRemaining>0:definition.Kind=="shield"&&run.Player.ShieldRemaining>0;
                if(!alive)continue;var profile=catalog.Find(special.Id);if(profile==null)continue;
                seenBuffs.Add(special.Id);
                if(!buffs.ContainsKey(special.Id)){var view=Spawn(profile,WorldProjection.WorldToScene(run.Player.Position),100,true);if(view!=null)buffs.Add(special.Id,view);}
            }
            staleBuffs.Clear();foreach(var pair in buffs)if(!seenBuffs.Contains(pair.Key))staleBuffs.Add(pair.Key);
            foreach(string id in staleBuffs){buffs[id].Release();buffs.Remove(id);}
        }
        void SyncZones(RunState run)
        {
            seenZones.Clear();
            foreach(var zone in run.Zones)
            {
                seenZones.Add(zone.Id);
                if(zone.Visual=="sword"&&zone.Warning<=0&&impactedZones.Add(zone.Id))
                    Spawn(catalog.Find("zone.sword.impact"),WorldProjection.WorldToScene(zone.Position),zone.Radius);
                if(zone.Visual=="rain"&&zone.TicksLeft<0)
                {
                    if(zone.Warning>.45f||!rainEmitted.Add(zone.Id))continue;
                    var end=WorldProjection.WorldToScene(zone.Position);var view=Spawn(catalog.Find("zone.rain"),end,zone.Radius);
                    if(view!=null)view.SetFlight(end+new Vector3(.18f,1.55f,0),end,Mathf.Max(.04f,zone.Warning));
                    continue;
                }
                if(!persistentZones.TryGetValue(zone.Id,out var persistent))
                {
                    var profile=catalog.Find("zone."+zone.Visual);if(profile==null)continue;
                    persistent=Spawn(profile,WorldProjection.WorldToScene(zone.Position),zone.Radius,true);
                    if(persistent!=null)persistentZones.Add(zone.Id,persistent);
                }
                if(persistent!=null)persistent.transform.position=WorldProjection.WorldToScene(zone.Position);
            }
            staleZones.Clear();foreach(var pair in persistentZones)if(!seenZones.Contains(pair.Key))staleZones.Add(pair.Key);
            foreach(int id in staleZones){persistentZones[id].Release();persistentZones.Remove(id);}
            rainEmitted.RemoveWhere(id=>!seenZones.Contains(id));
            impactedZones.RemoveWhere(id=>!seenZones.Contains(id));
        }
        SkillFxView Spawn(FxProfile profile,Vector3 position,float radius,bool sustain=false,Color? tint=null)
        {
            if(profile==null||profile.Prefab==null||active.Count>=catalog.MaximumActiveEffects)return null;
            if(!pools.TryGetValue(profile,out var pool)){pool=new Stack<SkillFxView>();pools.Add(profile,pool);}
            SkillFxView view;if(pool.Count>0){view=pool.Pop();pooledCount--;}else view=Instantiate(profile.Prefab,effectRoot!=null?effectRoot:transform);
            view.gameObject.SetActive(true);view.Begin(profile,position,radius,++visualSeed,sustain,tint);active.Add(view);return view;
        }
        void Return(SkillFxView view)
        {var profile=view.Profile;view.ResetView();view.gameObject.SetActive(false);if(profile!=null){if(pools[profile].Count<48&&pooledCount<(catalog!=null?catalog.MaximumActiveEffects:128)){pools[profile].Push(view);pooledCount++;}else Destroy(view.gameObject);}}
        public void Clear()
        {foreach(var view in active)Return(view);active.Clear();buffs.Clear();persistentZones.Clear();rainEmitted.Clear();impactedZones.Clear();}
        void OnDisable()=>Clear();
#if UNITY_EDITOR
        public void EditorSet(SurvivorGame host,SkillFxCatalog profiles,Transform root){game=host;catalog=profiles;effectRoot=root;}
#endif
    }
}
