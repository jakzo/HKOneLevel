namespace OneLevel.New;

class ChunkLoader {
  public Dictionary<string, Chunk> ChunkMapToLoad;

  private readonly OneLevel _mod;
  private List<SceneLoad> _chunkSceneLoads;

  public ChunkLoader(OneLevel mod) { _mod = mod; }

  public void Initialize() { On.SceneLoad.ctor += OnSceneLoad; }

  public void Unload() { On.SceneLoad.ctor -= OnSceneLoad; }

  public void OnSceneLoad(On.SceneLoad.orig_ctor orig, SceneLoad self,
                          MonoBehaviour runner, string targetSceneName) {
    orig(self, runner, targetSceneName);

    Utils.Try(() => {
      // When the game loads a new area the GameManager creates a SceneLoad
      // which handles scene loading and fires events at particular moments of
      // the loading process. If we are loading into a scene which is part of a
      // larger chunk map, create SceneLoads for each of the chunks then proxy
      // the events through the GameManager's SceneLoad (eg. GameManager's
      // SceneLoad will only fire OnActivationComplete once all chunk SceneLoads
      // have fired that event).
      if (ChunkMapToLoad != null) {
        var chunkMapToLoad = ChunkMapToLoad;
        ChunkMapToLoad = null;

        _chunkSceneLoads = new List<SceneLoad>();
        foreach (var chunk in chunkMapToLoad.Values) {
          var sceneLoad = new SceneLoad(runner, chunk.SceneName);
        }
      }
    });
  }
}

class SceneLoadChunked {
  public List<Chunk> ChunkMap;

  SceneLoadChunked(MonoBehaviour runner, List<string> sceneNames) {
    runner.StartCoroutine(BeginRoutine(runner, sceneNames));
  }

  public IEnumerator BeginRoutine(MonoBehaviour runner,
                                  List<string> sceneNames) {
    var sceneLoads = new List<SceneLoad>();
    foreach (var sceneName in sceneNames) {
      var sceneLoad = new SceneLoad(runner, sceneName);
      sceneLoads.Add(sceneLoad);
      sceneLoad.Begin();
    }
    yield return null;
  }
}
