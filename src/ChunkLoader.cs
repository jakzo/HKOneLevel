namespace OneLevel;

class ChunkLoader {
  public static readonly Vector3 WORLD_OFFSET = new(200f, 200f, 0f);
  private static readonly MethodInfo _unloadSceneMethod =
      typeof(USceneManager)
          .GetMethod(nameof(USceneManager.UnloadScene),
                     new Type[] { typeof(string) });

  private static int LAYER_TERRAIN;
  private static int LAYER_PLAYER;
  private static PhysicsMaterial2D PHYSICS_MATERIAL_TERRAIN;

  public Dictionary<string, Chunk> ChunkMap = Chunk.HALLOWNEST;
  public readonly Dictionary<Chunk, ChunkState> LoadedChunks = new();
  public Chunk CurrentChunk;
  public string BlockUnloadingScene;
  public event Action<ChunkState> OnChunkInit;
  public event Action<Scene> OnSceneInit;

  private readonly OneLevel _mod;
  private Hook _unloadSceneHook;

  public ChunkLoader(OneLevel mod) { _mod = mod; }

  public void Initialize() {
    LAYER_TERRAIN = LayerMask.NameToLayer("Terrain");
    LAYER_PLAYER = LayerMask.NameToLayer("Player");
    PHYSICS_MATERIAL_TERRAIN =
        Resources.FindObjectsOfTypeAll<PhysicsMaterial2D>().First(
            m => m.name == "Terrain");

    USceneManager.activeSceneChanged += HandleActiveSceneChanged;
    On.SceneLoad.ctor += OnSceneLoad;
    _unloadSceneHook = new Hook(_unloadSceneMethod, OnUnloadScene);
    On.TransitionPoint.OnTriggerEnter2D += OnTransitionPointEnter;
    On.GameManager.SceneLoadInfo.IsReadyToActivate += OnIsReadyToActivate;

    LoadAllChunks();
  }

  public void Unload() {
    USceneManager.activeSceneChanged -= HandleActiveSceneChanged;
    On.SceneLoad.ctor -= OnSceneLoad;
    _unloadSceneHook.Dispose();
    On.TransitionPoint.OnTriggerEnter2D -= OnTransitionPointEnter;
    On.GameManager.SceneLoadInfo.IsReadyToActivate -= OnIsReadyToActivate;

    BlockUnloadingScene = null;

    UnloadAllChunks();
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

  public void LoadAllChunks() {
    Logger.LogDebug("Loading all map chunks");

    var activeSceneName =
        GameManager.instance.nextSceneName ?? GameManager.instance.sceneName;
    var activeScene = USceneManager.GetSceneByName(activeSceneName);
    if (!activeScene.isLoaded) {
      throw new Exception($"Could not find active scene: {activeSceneName}");
    }

    Logger.LogDebug("=== activeScene " + activeScene.name);
    if (ChunkMap.TryGetValue(activeScene.name, out var activeChunk)) {
      CurrentChunk = activeChunk;
      Logger.LogDebug("=== CurrentChunk loadall " + CurrentChunk?.SceneName);
      var cs = new ChunkState() {
        Chunk = activeChunk,
        MainScene = activeScene,
      };
      for (var i = 0; i < USceneManager.sceneCount; i++) {
        var scene = USceneManager.GetSceneAt(i);
        cs.Scenes.Add(scene);
      }
      LoadedChunks.Add(activeChunk, cs);
      InitializeChunk(cs);
    }

    foreach (var chunk in ChunkMap.Values) {
      if (chunk.SceneName != activeScene.name) {
        LoadChunk(chunk);
      }
    }
  }

  public void LoadChunk(Chunk chunk) {
    Logger.LogDebug($"LoadChunk {chunk.SceneName}");
    var chunkState = new ChunkState() {
      Chunk = chunk,
      LoadOp =
          USceneManager.LoadSceneAsync(chunk.SceneName, LoadSceneMode.Additive),
    };
    LoadedChunks.Add(chunk, chunkState);
    chunkState.LoadOp.completed += op => {
      Utils.Try("chunkState.LoadOp.completed", () => {
        chunkState.MainScene = USceneManager.GetSceneByName(chunk.SceneName);
        chunkState.Scenes.Add(chunkState.MainScene);
        if (LoadedChunks.ContainsKey(chunk)) {
          InitializeChunk(chunkState);
        } else {
          Logger.LogWarn($"Scene finished loading after chunk was unloaded: " +
                         chunk.SceneName);
          USceneManager.UnloadSceneAsync(chunkState.MainScene);
        }
      });
    };
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
        Utils.Try("ChunkLoader.OnSceneLoad.ActivationComplete", () => {
          Logger.LogDebug($"SceneLoad.ActivationComplete: {targetSceneName}");

          foreach (var scene in cs.Scenes) {
            if (scene.IsValid()) {
#pragma warning disable CS0618 // Type or member is obsolete
              // TODO: Use async but have it start at the end of the fade out
              // and block this scene activation
              USceneManager.UnloadScene(scene);
#pragma warning restore CS0618 // Type or member is obsolete
            }
          }

          var prev = LoadedChunks[CurrentChunk];
          BlockUnloadingScene = prev.Chunk.SceneName;
          CurrentChunk = cs.Chunk;
          cs.MainScene = USceneManager.GetSceneByName(targetSceneName);
          cs.Scenes.Clear();
          cs.Scenes.Add(cs.MainScene);
          USceneManager.SetActiveScene(cs.MainScene);
          InitializeChunk(cs);
        });
      };
    });
  }

  // Only to be called when the chunk scene is loaded but unmodified
  private void InitializeChunk(ChunkState cs) {
    Logger.LogDebug($"InitializeChunk: {cs.Chunk.SceneName}");
    CreateColliders(cs);
    OnChunkInit?.Invoke(cs);
    foreach (var scene in cs.Scenes) {
      InitializeScene(cs, scene);
    }
  }

  // Only to be called when the chunk scene is loaded but unmodified
  public void InitializeScene(ChunkState cs, Scene scene) {
    Logger.LogDebug($"InitializeScene: {cs.Chunk.SceneName}");
    MoveScene(scene, cs.Chunk.Position + WORLD_OFFSET);
    OnSceneInit?.Invoke(scene);
  }

  public void MoveChunk(ChunkState cs, Vector3 offset) {
    foreach (var scene in cs.Scenes) {
      if (!scene.IsValid())
        continue;
      MoveScene(scene, offset);
    }
  }

  public void MoveScene(Scene scene, Vector3 offset) {
    foreach (var obj in scene.GetRootGameObjects()) {
      obj.transform.localPosition += offset;
    }
  }

  private void CreateColliders(ChunkState cs) {
    if (!_mod.DisableTransitions || cs.Chunk.Colliders == null)
      return;
    Logger.LogDebug($"CreateColliders: {cs.Chunk.SceneName}");

    var parent = new GameObject("OneLevel_TransitionColliders").transform;
    USceneManager.MoveGameObjectToScene(parent.gameObject, cs.MainScene);
    parent.localPosition = cs.Chunk.Position + WORLD_OFFSET;

    foreach (var rect in cs.Chunk.Colliders) {
      var go = new GameObject("OneLevel_TransitionCollider");
      go.transform.SetParent(parent, false);
      go.transform.localPosition = rect.position;
      go.layer = LAYER_TERRAIN;

      var collider = go.AddComponent<BoxCollider2D>();
      collider.size = rect.size;
      collider.sharedMaterial = PHYSICS_MATERIAL_TERRAIN;
      collider.offset = rect.size / 2f;

      // TODO: Use existing/modified art assets
      var meshFilter = go.AddComponent<MeshFilter>();
      meshFilter.mesh = new() {
        vertices =
            new Vector3[] {
              new(0f, 0f),
              new(rect.width, 0f),
              new(0f, rect.height),
              new(rect.width, rect.height),
            },
        triangles = new[] { 0, 2, 1, 2, 3, 1 },
      };
      meshFilter.mesh.RecalculateNormals();

      var meshRenderer = go.AddComponent<MeshRenderer>();
      meshRenderer.material = new Material(Shader.Find("Sprites/Lit")) {
        color = new Color(0.1f, 0.1f, 0.2f),
      };
    }
  }

  private void RestoreChunkScenes(ChunkState cs) {
    MoveChunk(cs, -cs.Chunk.Position - WORLD_OFFSET);
    // TODO: Delete colliders, etc.
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

    foreach (var cs in LoadedChunks.Values) {
      Logger.LogDebug($"=== Unloading chunk: {cs.Chunk.SceneName}");
      if (cs.Chunk == CurrentChunk) {
        RestoreChunkScenes(cs);
      } else {
        // var i = 0;
        // void unloadNext() {
        //   if (i >= cs.Scenes.Count)
        //     return;
        //   var scene = cs.Scenes[i++];
        //   if (scene.IsValid()) {
        //     var sceneName = scene.name;
        //     Logger.LogDebug($"=== Unloading scene: {sceneName}");
        //     var op = USceneManager.UnloadSceneAsync(scene);
        //     op.completed += op => {
        //       Logger.LogDebug(
        //           $"====== Unload finished for scene {sceneName}
        //           {op.progress} {op.isDone}");
        //       unloadNext();
        //     };
        //   } else {
        //     unloadNext();
        //   }
        // }
        // unloadNext();

        // foreach (var scene in cs.Scenes) {
        //   if (!scene.IsValid())
        //     continue;
        //   try {
        //     var sceneName = scene.name;
        //     Logger.LogDebug($"=== Unloading scene: {sceneName}");
        //     var op = USceneManager.UnloadSceneAsync(scene);
        //     op.completed += op => {
        //       Logger.LogDebug(
        //           $"====== Unload finished for scene {sceneName}
        //           {op.progress} {op.isDone}");
        //     };
        //   } catch (Exception ex) {
        //     Logger.LogWarn(
        //         $"Error unloading chunk scene: {cs.Chunk.SceneName}");
        //     Logger.LogWarn(ex);
        //   }
        // }

        if (!cs.MainScene.IsValid())
          continue;
        try {
          var sceneName = cs.MainScene.name;
          Logger.LogDebug($"=== Unloading scene: {sceneName}");
          var op = USceneManager.UnloadSceneAsync(cs.MainScene);
          op.completed += op => {
            Logger.LogDebug(
                $"====== Unload finished for scene {sceneName} {op.progress} {op.isDone}");
          };
        } catch (Exception ex) {
          Logger.LogWarn($"Error unloading chunk scene: {cs.Chunk.SceneName}");
          Logger.LogWarn(ex);
        }
      }
    }

    LoadedChunks.Clear();
    CurrentChunk = null;
  }

  private void
  OnTransitionPointEnter(On.TransitionPoint.orig_OnTriggerEnter2D orig,
                         TransitionPoint self, Collider2D movingObj) {
    var isBlocked = Utils.Try(() => {
      if (movingObj.gameObject.layer != LAYER_PLAYER)
        return false;
      if (_mod.DisableTransitions)
        return ChunkMap.ContainsKey(self.targetScene);
      return self.targetScene != CurrentChunk.SceneName;
    });

    if (!isBlocked)
      orig(self, movingObj);
  }

  private bool
  OnIsReadyToActivate(On.GameManager.SceneLoadInfo.orig_IsReadyToActivate orig,
                      GameManager.SceneLoadInfo self) {
    var result = orig(self);
    var isBlocked = Utils.Try(() => {
      // TODO
      return false;
    });
    return !isBlocked && result;
  }
}

class ChunkState {
  public Chunk Chunk;
  public Scene MainScene;
  public List<Scene> Scenes = new();
  public AsyncOperation LoadOp;
}
