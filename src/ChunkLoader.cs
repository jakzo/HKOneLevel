namespace OneLevel;

class ChunkLoader {
  public static readonly Vector3 WORLD_OFFSET = new(200f, 200f, 0f);
  private static readonly MethodInfo _unloadSceneMethod =
      typeof(USceneManager)
          .GetMethod(nameof(USceneManager.UnloadScene),
                     new Type[] { typeof(string) });

  public Dictionary<string, Chunk> ChunkMap = Chunk.BASE_GAME;
  public readonly Dictionary<Chunk, ChunkState> LoadedChunks = new();
  public readonly Dictionary<Scene, ChunkState> LoadedScenes = new();
  public Chunk CurrentChunk;
  public string BlockUnloadingScene;
  public event Action<ChunkState> OnChunkLoad;

  private readonly OneLevel _mod;
  private Hook _unloadSceneHook;

  public ChunkLoader(OneLevel mod) { _mod = mod; }

  public void Initialize(string currentSceneName = null) {
    USceneManager.activeSceneChanged += HandleActiveSceneChanged;
    On.SceneLoad.ctor += OnSceneLoad;
    _unloadSceneHook = new Hook(_unloadSceneMethod, OnUnloadScene);

    ChunkMap.TryGetValue(currentSceneName, out var currentChunk);
    CurrentChunk = currentChunk;

    LoadAllChunks();
  }

  // When transitioning while multiple chunks are loaded, the active scene gets
  // switched to one of the chunk scenes, but we want to keep the active scene
  // set to the chunk the player is in because that's what the game expects
  private void HandleActiveSceneChanged(Scene current, Scene next) {
    Utils.Try(() => {
      // TODO: Verify this works correctly
      if (next.name != GameManager.instance.sceneName &&
          ChunkMap.TryGetValue(next.name, out var nextSceneChunk) &&
          LoadedChunks.ContainsKey(nextSceneChunk)) {
        var gameScene =
            USceneManager.GetSceneByName(GameManager.instance.sceneName);
        if (gameScene.isLoaded) {
          USceneManager.SetActiveScene(gameScene);
        }
      }
    });
  }

  public void Unload() {
    USceneManager.activeSceneChanged -= HandleActiveSceneChanged;
    On.SceneLoad.ctor -= OnSceneLoad;
    _unloadSceneHook.Dispose();

    BlockUnloadingScene = null;

    UnloadAllChunks();
  }

  public void LoadAllChunks() {
    Logger.LogDebug("Loading all map chunks");

    foreach (var chunk in ChunkMap.Values) {
      InitializeChunkState(chunk);
    }
  }

  public void OnSceneLoad(On.SceneLoad.orig_ctor orig, SceneLoad self,
                          MonoBehaviour runner, string targetSceneName) {
    orig(self, runner, targetSceneName);

    Utils.Try(() => {
      if (!ChunkMap.TryGetValue(targetSceneName, out var chunk) ||
          !LoadedChunks.TryGetValue(chunk, out var cs)) {
        return;
      }

      Logger.LogDebug("ChunkLoader OnSceneLoad");
      self.ActivationComplete += () => {
        Utils.Try("OnSceneLoad.ActivationComplete", () => {
          Logger.LogDebug("SceneLoad.ActivationComplete");

#pragma warning disable CS0618 // Type or member is obsolete
          // TODO: Use async but have it start at the end of the fade out and
          // block this scene activation
          USceneManager.UnloadScene(cs.Scene);
#pragma warning restore CS0618 // Type or member is obsolete

          var prev = LoadedChunks[CurrentChunk];
          BlockUnloadingScene = prev.Chunk.SceneName;
          CurrentChunk = cs.Chunk;
          cs.Scene = USceneManager.GetSceneByName(targetSceneName);
          USceneManager.SetActiveScene(cs.Scene);
          InitializeChunkScene(cs);

          foreach (var obj in prev.Scene.GetRootGameObjects()) {
            UpdateChunkRootObject(prev, obj);
          }
        });
      };
    });
  }

  public void InitializeChunkState(Chunk chunk) {
    var existingScene = USceneManager.GetSceneByName(chunk.SceneName);
    if (existingScene.isLoaded) {
      var chunkState = new ChunkState() {
        Chunk = chunk,
        Scene = existingScene,
      };
      InitializeChunkScene(chunkState);
      LoadedScenes.Add(chunkState.Scene, chunkState);
      LoadedChunks.Add(chunk, chunkState);
    } else {
      var chunkState = new ChunkState() {
        Chunk = chunk,
        LoadOp = USceneManager.LoadSceneAsync(chunk.SceneName,
                                              LoadSceneMode.Additive),
      };
      LoadedChunks.Add(chunk, chunkState);
      chunkState.LoadOp.completed += op => {
        // TODO: May break if multiple of the same chunk gets loaded (but
        // shouldn't happen in normal play)
        chunkState.Scene = USceneManager.GetSceneByName(chunk.SceneName);
        if (LoadedChunks.ContainsKey(chunk)) {
          InitializeChunkScene(chunkState);
          LoadedScenes.Add(chunkState.Scene, chunkState);
        } else {
          Logger.LogWarn($"Scene finished loading after chunk was unloaded: " +
                         chunk.SceneName);
          USceneManager.UnloadSceneAsync(chunkState.Scene);
        }
      };
    }
  }

  // Only to be called when the chunk scene is loaded but unmodified
  private void InitializeChunkScene(ChunkState cs) {
    Logger.LogDebug(
        $"InitializeChunkScene {cs.Chunk.SceneName} {cs.Scene.name}");
    foreach (var obj in cs.Scene.GetRootGameObjects()) {
      // Move scene from origin to chunk position
      obj.transform.localPosition += cs.Chunk.Position + WORLD_OFFSET;

      UpdateChunkRootObject(cs, obj);
    }
    OnChunkLoad?.Invoke(cs);
  }

  private void UpdateChunkRootObject(ChunkState cs, GameObject obj) {
    // Disable transitions so that we can't walk into a neighboring
    // scene's transition point which overlaps with the current scene
    foreach (var tp in obj.GetComponentsInChildren<TransitionPoint>(true)) {
      tp.gameObject.SetActive(cs.Chunk == CurrentChunk);
    }
  }

  private void RestoreChunkScene(ChunkState cs) {
    foreach (var obj in cs.Scene.GetRootGameObjects()) {
      // Move scene back to original position
      obj.transform.localPosition -= cs.Chunk.Position + WORLD_OFFSET;
    }
  }

  private bool OnUnloadScene(Func<string, bool> orig, string sceneName) {
    var isBlocked = Utils.Try(() => {
      if (BlockUnloadingScene != null && sceneName == BlockUnloadingScene) {
        BlockUnloadingScene = null;
        Logger.LogDebug($"OnUnloadScene blocked: {sceneName}");
        return true;
      }
      return false;
    });
    return isBlocked || orig(sceneName);
  }

  public void UnloadAllChunks() {
    Logger.LogDebug("Unloading all map chunks");

    HeroController.instance.vignette.gameObject.SetActive(true);

    var currentChunkOffset = CurrentChunk.Position + WORLD_OFFSET;
    HeroController.instance.transform.localPosition -= currentChunkOffset;

    foreach (var cs in LoadedChunks.Values) {
      if (cs.Chunk == CurrentChunk) {
        RestoreChunkScene(cs);
      } else if (cs.Scene.isLoaded) {
        USceneManager.UnloadSceneAsync(cs.Scene);
      }
    }

    LoadedChunks.Clear();
    LoadedScenes.Clear();
  }
}

class ChunkState {
  public Chunk Chunk;
  public Scene Scene;
  public AsyncOperation LoadOp;
}
