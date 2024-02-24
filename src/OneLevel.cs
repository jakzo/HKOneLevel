namespace OneLevel;

class OneLevel : Mod, IGlobalSettings<Settings>, ITogglableMod {
  public static OneLevel Instance { get; private set; }

  public Settings Settings = new();
  public Camera Camera { get; private set; }
  public Misc Misc { get; private set; }
  public ChunkLoader ChunkLoader { get; private set; }
  public bool IsStarted = false;

  public OneLevel() : base("OneLevel") {
    Instance = this;
    Camera = new(this);
    Misc = new(this);
    ChunkLoader = new(this);
  }

  public override string GetVersion() =>
      Assembly.GetExecutingAssembly().GetName().Version.ToString();

  public bool IsInChunkedArea(string sceneName) =>
      ChunkLoader.ChunkMap.ContainsKey(sceneName);

  public override void Initialize() {
    var sceneName = GameManager._instance?.sceneName;
    if (IsInChunkedArea(sceneName)) {
      Start(sceneName);
    }

    On.SceneLoad.ctor += OnSceneLoad;
  }

  public void Unload() {
    Stop();

    On.SceneLoad.ctor -= OnSceneLoad;
  }

  public void Start(string currentSceneName) {
    if (IsStarted)
      return;
    IsStarted = true;

    ChunkLoader.Initialize(currentSceneName);
    Camera.Initialize();
    Misc.Initialize();
  }

  public void Stop() {
    if (!IsStarted)
      return;
    IsStarted = false;

    ChunkLoader.Unload();
    Camera.Unload();
    Misc.Unload();
  }

  public Settings OnSaveGlobal() => Settings;

  public void OnLoadGlobal(Settings settings) => Settings = settings;

  // TODO: Implement settings menu
  // public bool ToggleButtonInsideMenu { get => true; }
  // public List<IMenuMod.MenuEntry> GetMenuData(
  //     IMenuMod.MenuEntry? toggleButtonEntry) => new() {
  //   new IMenuMod.MenuEntry {
  //     Name = "Camera -> Zoom speed",
  //     Description =
  //         "Speed that scrolling the mouse wheel zooms the camera in and
  //         out.",
  //     Type = "float",
  //     Saver = value => Settings.ZoomSpeed = value,
  //     Loader = () => Settings.ZoomSpeed,
  //   },
  // };

  // TODO: The code transforms sceneName, is this safe to use?
  public bool IsInGameplay() =>
      ChunkLoader.ChunkMap.ContainsKey(GameManager._instance?.sceneName);

  public void OnSceneLoad(On.SceneLoad.orig_ctor orig, SceneLoad self,
                          MonoBehaviour runner, string targetSceneName) {
    orig(self, runner, targetSceneName);

    Utils.Try(() => {
      LogDebug($"SceneLoad.ctor {self.TargetSceneName}");
      self.ActivationComplete += () => {
        try {
          if (IsInChunkedArea(targetSceneName)) {
            LogDebug("IsInChunkedArea");
            Start(targetSceneName);
          } else {
            LogDebug("NOT IsInChunkedArea");
            Stop();
          }
        } catch (Exception ex) {
          LogError("Error in OneLevel.OnSceneLoad.ActivationComplete:");
          LogError(ex);
        }
      };
    });
  }

  public void TestSetChunkPos(string sceneName, Vector3 pos) {
    ChunkLoader.ChunkMap.TryGetValue(sceneName, out var chunk);
    if (chunk == null) {
      Log("Could not find chunk, creating...");
      chunk = new Chunk() {
        SceneName = sceneName,
        Position = pos,
      };
    }
    ChunkLoader.LoadedChunks.TryGetValue(chunk, out var cs);
    if (cs == null) {
      ChunkLoader.InitializeChunkState(chunk);
    } else {
      var oldChunkPos = chunk.Position;
      chunk.Position = pos;
      foreach (var obj in cs.Scene.GetRootGameObjects()) {
        obj.transform.localPosition += pos - oldChunkPos;
      }
    }
  }
}
