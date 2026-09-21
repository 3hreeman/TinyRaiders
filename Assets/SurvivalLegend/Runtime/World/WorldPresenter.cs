using System.Collections.Generic;
using UnityEngine;

namespace SurvivalLegend.World
{
    /// <summary>Reconciles simulation IDs with saved prefab instances. No gameplay state lives in views.</summary>
    [DefaultExecutionOrder(200)]
    public sealed class WorldPresenter : MonoBehaviour
    {
        [SerializeField] private SurvivorGame game;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private WorldVisualDatabase visuals;
        [SerializeField] private ArenaMapView arena;
        [SerializeField] private SkillFxPresenter skillEffects;
        [SerializeField] private FogOfWarPresenter fog;
        [SerializeField] private Transform actorsRoot, projectilesRoot, zonesRoot, effectsRoot, markersRoot;
        private ActorView player;
        private string playerAppearance;
        private readonly Dictionary<int, ActorView> enemies = new Dictionary<int, ActorView>();
        private readonly Dictionary<int, ProjectileView> projectiles = new Dictionary<int, ProjectileView>();
        private readonly Dictionary<int, ZoneView> zones = new Dictionary<int, ZoneView>();
        private readonly Dictionary<long, WorldEffectView> effects = new Dictionary<long, WorldEffectView>();
        private readonly Dictionary<string, Stack<ActorView>> actorPools = new Dictionary<string, Stack<ActorView>>();
        private readonly Stack<ProjectileView> projectilePool = new Stack<ProjectileView>();
        private readonly List<ProjectileView> projectileTails = new List<ProjectileView>();
        private readonly Stack<ZoneView> zonePool = new Stack<ZoneView>();
        private readonly Stack<WorldEffectView> effectPool = new Stack<WorldEffectView>();
        private readonly List<int> staleIds = new List<int>();
        private readonly List<long> staleEffectIds = new List<long>();
        private readonly List<EffectState> visualEvents = new List<EffectState>();
        private readonly HashSet<int> liveIds = new HashSet<int>();
        private readonly HashSet<long> liveEffects = new HashSet<long>();
        private AimMarkerView marker;
        private int generation = -1;
        private bool worldWasVisible;

        public ArenaMapView Arena => arena;
        public WorldVisualDatabase Visuals => visuals;
        public FogOfWarPresenter Fog => fog;
        public void Configure(SurvivorGame runtime, Camera camera, WorldVisualDatabase database)
        { game = runtime; worldCamera = camera; visuals = database; generation = -1; }

#if UNITY_EDITOR
        public void EditorSetFx(SkillFxPresenter fx){skillEffects=fx;}
        public void EditorSetFog(FogOfWarPresenter value){fog=value;}
        public void EditorSet(SurvivorGame runtime, Camera camera, WorldVisualDatabase database,
            ArenaMapView map, Transform actorLayer, Transform projectileLayer, Transform zoneLayer,
            Transform effectLayer, Transform markerLayer)
        {
            game = runtime; worldCamera = camera; visuals = database; arena = map;
            actorsRoot = actorLayer; projectilesRoot = projectileLayer; zonesRoot = zoneLayer;
            effectsRoot = effectLayer; markersRoot = markerLayer;
        }
#endif
        private void OnEnable()
        {
            if (game == null) game = SurvivorGame.Instance;
            if (game != null) game.RunReset += HandleReset;
        }
        private void OnDisable()
        {
            if (game != null) game.RunReset -= HandleReset;
            ClearAll();
        }
        private void HandleReset(int nextGeneration)
        {
            generation = nextGeneration;
            ClearAll();
            worldWasVisible = false;
        }
        private void LateUpdate()
        {
            if (game == null) { game = SurvivorGame.Instance; if (game != null) game.RunReset += HandleReset; }
            if (game == null || game.Simulation == null || visuals == null) return;
            if (generation != game.Generation) HandleReset(game.Generation);
            RunState run = game.State;
            bool visible = run.Phase != RunPhase.Title;
            // The arena artwork remains behind the title, as in the source. Only transient
            // combat actors disappear, and the pool is cleared once per title transition.
            if (arena != null && !arena.gameObject.activeSelf) arena.gameObject.SetActive(true);
            if (!visible) { if (worldWasVisible) ClearAll(); worldWasVisible = false; return; }
            worldWasVisible = true;
            BindPlayer(run);
            BindEnemies(run);
            BindProjectiles(run);
            BindZones(run);
            BindEffects(run);
            if(skillEffects!=null)skillEffects.Present(run,Time.unscaledDeltaTime);
            if (marker == null && visuals.AimMarkerPrefab != null)
                marker = Instantiate(visuals.AimMarkerPrefab, markersRoot != null ? markersRoot : transform);
            if (marker != null) marker.Bind(game);
        }
        private void BindPlayer(RunState run)
        {
            var definition = visuals.Character(run.Character);
            if (definition == null || definition.prefab == null) return;
            if (player == null || playerAppearance != definition.id)
            {
                if (player != null) ReturnActor(player);
                player = SpawnActor(definition);
                playerAppearance = definition.id;
            }
            player.BindPlayer(run);
        }
        private void BindEnemies(RunState run)
        {
            liveIds.Clear();
            foreach (var enemy in run.Enemies)
            {
                if (enemy.Hp <= 0) continue;
                liveIds.Add(enemy.Id);
                if(fog!=null&&!fog.IsVisible(enemy.Position))
                {
                    if(enemies.TryGetValue(enemy.Id,out var hidden))hidden.gameObject.SetActive(false);
                    continue;
                }
                if (!enemies.TryGetValue(enemy.Id, out var view))
                {
                    var definition = visuals.Enemy(enemy.Kind);
                    if (definition == null || definition.prefab == null) continue;
                    view = SpawnActor(definition);
                    enemies[enemy.Id] = view;
                }
                bool elite = false;
                foreach (var entry in game.Content.EliteSpawnPool)
                    if (entry.EnemyId == enemy.Kind) { elite = true; break; }
                string name = game.Content.Enemies.TryGetValue(enemy.Kind, out var data) ? data.Name : enemy.Kind;
                view.gameObject.SetActive(true);
                view.BindEnemy(enemy, run.Time, elite, name);
            }
            staleIds.Clear();
            foreach (var pair in enemies) if (!liveIds.Contains(pair.Key)) staleIds.Add(pair.Key);
            foreach (int id in staleIds) { var view = enemies[id]; enemies.Remove(id); ReturnActor(view); }
        }
        private ActorView SpawnActor(ActorVisualDefinition definition)
        {
            if (!actorPools.TryGetValue(definition.id, out var pool))
            { pool = new Stack<ActorView>(); actorPools[definition.id] = pool; }
            ActorView view = pool.Count > 0 ? pool.Pop() : Instantiate(definition.prefab, actorsRoot != null ? actorsRoot : transform);
            view.SetDefinition(definition); view.gameObject.SetActive(true); return view;
        }
        private void ReturnActor(ActorView view)
        {
            string visualId = view.VisualId ?? string.Empty;
            if (!actorPools.TryGetValue(visualId, out var pool))
            { pool = new Stack<ActorView>(); actorPools[visualId] = pool; }
            view.ResetView(); view.gameObject.SetActive(false); pool.Push(view);
        }
        private void BindProjectiles(RunState run)
        {
            liveIds.Clear();
            foreach (var state in run.Projectiles)
            {
                liveIds.Add(state.Id);
                if (!projectiles.TryGetValue(state.Id, out var view))
                {
                    view = projectilePool.Count > 0 ? projectilePool.Pop() : visuals.ProjectilePrefab != null
                        ? Instantiate(visuals.ProjectilePrefab, projectilesRoot != null ? projectilesRoot : transform) : null;
                    if (view == null) continue;
                    projectiles[state.Id] = view; view.gameObject.SetActive(true);
                }
                view.Bind(state);
                view.gameObject.SetActive(fog==null||fog.IsVisible(state.Position));
            }
            staleIds.Clear(); foreach (var pair in projectiles) if (!liveIds.Contains(pair.Key)) staleIds.Add(pair.Key);
            foreach (int id in staleIds) { var view = projectiles[id]; projectiles.Remove(id); view.Release(); projectileTails.Add(view); }
            bool advancing=run.Phase==RunPhase.Playing||run.Phase==RunPhase.LevelUp||run.Phase==RunPhase.Dying||run.Phase==RunPhase.EliteDeath;
            float delta=advancing?Mathf.Min(Time.unscaledDeltaTime,.1f):0;
            foreach(var view in projectiles.Values)view.AdvanceVisual(delta);
            for(int i=projectileTails.Count-1;i>=0;i--){var view=projectileTails[i];view.AdvanceVisual(delta);if(view.TailAlive)continue;projectileTails.RemoveAt(i);view.ResetView();view.gameObject.SetActive(false);projectilePool.Push(view);}
        }
        private void BindZones(RunState run)
        {
            liveIds.Clear();
            foreach (var state in run.Zones)
            {
                liveIds.Add(state.Id);
                if (!zones.TryGetValue(state.Id, out var view))
                {
                    view = zonePool.Count > 0 ? zonePool.Pop() : visuals.ZonePrefab != null
                        ? Instantiate(visuals.ZonePrefab, zonesRoot != null ? zonesRoot : transform) : null;
                    if (view == null) continue;
                    zones[state.Id] = view; view.gameObject.SetActive(true);
                }
                view.Bind(state);
                view.gameObject.SetActive(fog==null||fog.IsVisible(state.Position));
            }
            staleIds.Clear(); foreach (var pair in zones) if (!liveIds.Contains(pair.Key)) staleIds.Add(pair.Key);
            foreach (int id in staleIds) { var view = zones[id]; zones.Remove(id); view.ResetView(); view.gameObject.SetActive(false); zonePool.Push(view); }
        }
        private void BindEffects(RunState run)
        {
            // Drain once per rendered frame even if individual effects have already expired in this tick.
            visualEvents.Clear(); game.Simulation.DrainVisualEvents(visualEvents);
            if(skillEffects!=null)skillEffects.ReceiveEvents(visualEvents);
            liveEffects.Clear();
            foreach (var state in run.Effects)
            {
                if(skillEffects!=null&&skillEffects.Handles(state))continue;
                liveEffects.Add(state.Id);
                if (!effects.TryGetValue(state.Id, out var view))
                {
                    view = effectPool.Count > 0 ? effectPool.Pop() : visuals.EffectPrefab != null
                        ? Instantiate(visuals.EffectPrefab, effectsRoot != null ? effectsRoot : transform) : null;
                    if (view == null) continue;
                    effects[state.Id] = view; view.gameObject.SetActive(true);
                }
                view.Bind(state);
                view.gameObject.SetActive(fog==null||fog.IsVisible(state.Position));
            }
            // Fixed-step effects can be born and expire between rendered frames. Keep the
            // one-shot event for one presentation frame even when it missed the state list.
            foreach (var state in visualEvents)
            {
                if(skillEffects!=null&&skillEffects.Handles(state))continue;
                if (!liveEffects.Add(state.Id)) continue;
                if (!effects.TryGetValue(state.Id, out var view))
                {
                    view = effectPool.Count > 0 ? effectPool.Pop() : visuals.EffectPrefab != null
                        ? Instantiate(visuals.EffectPrefab, effectsRoot != null ? effectsRoot : transform) : null;
                    if (view == null) continue;
                    effects[state.Id] = view; view.gameObject.SetActive(true);
                }
                view.Bind(state);
                view.gameObject.SetActive(fog==null||fog.IsVisible(state.Position));
            }
            staleEffectIds.Clear(); foreach (var pair in effects) if (!liveEffects.Contains(pair.Key)) staleEffectIds.Add(pair.Key);
            foreach (long id in staleEffectIds)
            {
                var view = effects[id]; effects.Remove(id); view.ResetView(); view.gameObject.SetActive(false); effectPool.Push(view);
            }
        }
        private void ClearAll()
        {
            if(skillEffects!=null)skillEffects.Clear();
            foreach(var view in projectileTails){view.ResetView();view.gameObject.SetActive(false);projectilePool.Push(view);}projectileTails.Clear();
            if (player != null) { ReturnActor(player); player = null; playerAppearance = null; }
            foreach (var view in enemies.Values) ReturnActor(view); enemies.Clear();
            foreach (var view in projectiles.Values) { view.ResetView(); view.gameObject.SetActive(false); projectilePool.Push(view); } projectiles.Clear();
            foreach (var view in zones.Values) { view.ResetView(); view.gameObject.SetActive(false); zonePool.Push(view); } zones.Clear();
            foreach (var view in effects.Values) { view.ResetView(); view.gameObject.SetActive(false); effectPool.Push(view); } effects.Clear();
            if (marker != null) marker.Clear();
            visualEvents.Clear();
        }
    }
}
