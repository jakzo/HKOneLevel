namespace OneLevel;

class OneLevel : Mod, IGlobalSettings<Settings>, ITogglableMod {
  public static OneLevel Instance { get; private set; }

  public Settings Settings = new();
  public Camera Camera { get; private set; }
  public BossScenes BossScenes { get; private set; }
  public Misc Misc { get; private set; }
  public ChunkLoader ChunkLoader { get; private set; }
  public bool IsStarted = false;
  public bool DisableTransitions = false;

  public OneLevel() : base("OneLevel") {
    Instance = this;
    Camera = new(this);
    Misc = new(this);
    BossScenes = new(this);
    ChunkLoader = new(this);
  }

  public override string GetVersion() =>
      Assembly.GetExecutingAssembly().GetName().Version.ToString();

  public bool IsInChunkedArea(string sceneName) =>
      ChunkLoader.ChunkMap.ContainsKey(sceneName);

  public override void Initialize() {
    var sceneName = GameManager._instance?.sceneName;
    if (IsInChunkedArea(sceneName)) {
      Start();
    }

    On.SceneLoad.ctor += OnSceneLoad;
    On.GameManager.ReturnToMainMenu += OnReturnToMainMenu;

#if DEBUG
    OneLevelDebug.Initialize();
#endif
  }

  public void Unload() {
    Stop();

    On.SceneLoad.ctor -= OnSceneLoad;
    On.GameManager.ReturnToMainMenu -= OnReturnToMainMenu;

#if DEBUG
    OneLevelDebug.Unload();
#endif
  }

  public void Start() {
    if (IsStarted)
      return;
    IsStarted = true;

    DisableTransitions = Settings.Beta_DisableTransitions;
    Logger.LogDebug($"DisableTransitions {DisableTransitions}");

    ChunkLoader.Initialize();
    Camera.Initialize();
    BossScenes.Initialize();
    Misc.Initialize();
  }

  public void Stop() {
    if (!IsStarted)
      return;
    IsStarted = false;

    Misc.Unload();
    BossScenes.Unload();
    Camera.Unload();
    ChunkLoader.Unload();
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
      LogDebug($"SceneLoad: {self.TargetSceneName}");
      self.ActivationComplete += () => {
        Utils.Try("OneLevel.OnSceneLoad.ActivationComplete", () => {
          if (IsInChunkedArea(targetSceneName)) {
            LogDebug("IsInChunkedArea");
            Start();
          } else {
            LogDebug("NOT IsInChunkedArea");
            Stop();
          }
        });
      };
    });
  }

  public IEnumerator OnReturnToMainMenu(
      On.GameManager.orig_ReturnToMainMenu orig, GameManager self,
      GameManager.ReturnToMainMenuSaveModes saveMode, Action<bool> callback) {
    Utils.Try(() => {
      LogDebug("OnReturnToMainMenu");
      Stop();
    });

    return orig(self, saveMode, callback);
  }
}
