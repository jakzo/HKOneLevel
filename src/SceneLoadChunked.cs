namespace OneLevel.New;

// When the game transitions to a new area the GameManager creates a
// SceneLoad which handles scene loading and fires events at particular
// moments of the loading process. If we are loading into a scene which is
// part of a larger chunk map, create SceneLoads for each of the chunks
// then proxy the events through the GameManager's SceneLoad (eg.
// GameManager's SceneLoad will only fire OnActivationComplete once all
// chunk SceneLoads have fired that event).
class SceneLoadChunked {
  private static readonly MethodInfo _methodSetFetchAllowed =
      typeof(SceneLoad).GetMethod("set_IsFetchAllowed");
  private static readonly MethodInfo _methodSetActivationAllowed =
      typeof(SceneLoad).GetMethod("set_IsActivationAllowed");

  public Dictionary<string, ChunkState> LoadedChunks =
      new Dictionary<string, ChunkState>();

  private readonly OneLevel _mod;
  private Hook _hookSetFetchAllowed;
  private Hook _hookSetActivationAllowed;
  private ConditionalWeakTable<SceneLoad, List<SceneLoad>> _sceneLoads =
      new ConditionalWeakTable<SceneLoad, List<SceneLoad>>();
  private bool _isFinishPendingOperationsRunning = false;

  public SceneLoadChunked(OneLevel mod) { _mod = mod; }

  public void Initialize() {
    On.SceneLoad.Begin += OnBegin;
    On.ScenePreloader.FinishPendingOperations += OnFinishPendingOperations;
    _hookSetFetchAllowed = new Hook(_methodSetFetchAllowed, OnSetAllowed);
    _hookSetActivationAllowed =
        new Hook(_methodSetActivationAllowed, OnSetAllowed);
  }

  public void Unload() {
    On.SceneLoad.Begin -= OnBegin;
    On.ScenePreloader.FinishPendingOperations -= OnFinishPendingOperations;
    _hookSetFetchAllowed.Dispose();
    _hookSetActivationAllowed.Dispose();
  }

  private void OnSetAllowed(Action<SceneLoad, bool> orig, SceneLoad self,
                            bool value) {
    orig(self, value);

    Utils.Try(() => {
      if (_sceneLoads.TryGetValue(self, out var sceneLoads)) {
        foreach (var sceneLoad in sceneLoads) {
          orig(sceneLoad, value);
        }
      }
    });
  }

  private void OnSetFirst(Action<SceneLoad, bool> orig, SceneLoad self,
                          bool value) {
    orig(self, value);

    Utils.Try(() => {
      if (_sceneLoads.TryGetValue(self, out var sceneLoads)) {
        foreach (var sceneLoad in sceneLoads) {
          orig(sceneLoads[0], value);
        }
      }
    });
  }

  // TODO
  private Dictionary<string, Chunk>
  GetChunkMapOfScene(string sceneName) => new Dictionary<string, Chunk>();

  private void OnBegin(On.SceneLoad.orig_Begin orig, SceneLoad self) {
    var chunkMap = Utils.Try(() => {
      var targetSceneName =
          ReflectionHelper.GetField<SceneLoad, string>(self, "targetSceneName");
      return GetChunkMapOfScene(targetSceneName);
    }, () => null);

    if (chunkMap == null) {
      orig(self);
      return;
    }

    Utils.Try(() => {
      var runner =
          ReflectionHelper.GetField<SceneLoad, MonoBehaviour>(self, "runner");
      var targetSceneName =
          ReflectionHelper.GetField<SceneLoad, string>(self, "targetSceneName");

      var sceneLoads = new List<SceneLoad>();
      _sceneLoads.Add(self, sceneLoads);

      var gmSceneLoad = self;
      var sceneLoadEvents =
          typeof(SceneLoad)
              .GetEvents()
              .Select(eventInfo => (eventInfo, new HashSet<SceneLoad>()))
              .ToArray();

      // Disable hook temporarily so we can create some "real" SceneLoads
      foreach (var chunk in chunkMap.Values) {
        var sceneLoad = new SceneLoad(runner, chunk.SceneName);

        sceneLoad.ActivationComplete += () => {
          var scene = USceneManager.GetSceneByName(chunk.SceneName);
          LoadedChunks.Add(chunk.SceneName, new ChunkState() {
            Chunk = chunk,
            MainScene = scene,
          });
        };

        foreach (var (eventInfo, scenesWaiting) in sceneLoadEvents) {
          scenesWaiting.Add(sceneLoad);
          eventInfo.AddEventHandler(
              sceneLoad,
              () => Utils.Try($"Chunk.SceneLoad.{eventInfo.Name}", () => {
                if (scenesWaiting.Remove(sceneLoad) &&
                    scenesWaiting.Count == 0) {
                  Utils.InvokeEvent(gmSceneLoad, eventInfo.Name);
                }
              }));
        }

        // sceneLoad.Begin();
        orig(sceneLoad);
      }
    });
  }

  // Every SceneLoad will call this so make sure it only runs once
  private IEnumerator OnFinishPendingOperations(
      On.ScenePreloader.orig_FinishPendingOperations orig) {
    if (_isFinishPendingOperationsRunning)
      yield break;

    _isFinishPendingOperationsRunning = true;
    yield return orig();
    _isFinishPendingOperationsRunning = false;
  }
}
