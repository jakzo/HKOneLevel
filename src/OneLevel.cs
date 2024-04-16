namespace OneLevel;

class OneLevel : Mod, IGlobalSettings<Settings>, ITogglableMod {
  public static OneLevel Instance { get; private set; }

  public Settings Settings = new();
  public Camera Camera { get; private set; }
  public BossScenes BossScenes { get; private set; }
  public Misc Misc { get; private set; }
  public SceneLoader SceneLoader { get; private set; }
  public TransitionHooks TransitionHooks { get; private set; }
  public ChunkMap CurrentMap;

  public OneLevel() : base("OneLevel") {
    Instance = this;
    Camera = new(this);
    Misc = new(this);
    BossScenes = new(this);
    SceneLoader = new(this);
    TransitionHooks = new(this);
  }

  public override string GetVersion() =>
      Assembly.GetExecutingAssembly().GetName().Version.ToString();

  public override void Initialize() {
    TransitionHooks.Initialize();
    SceneLoader.Initialize();
    Camera.Initialize();
    BossScenes.Initialize();
    Misc.Initialize();

#if DEBUG
    OneLevelDebug.Initialize();
#endif
  }

  public void Unload() {
    Misc.Unload();
    BossScenes.Unload();
    Camera.Unload();
    SceneLoader.Unload();
    TransitionHooks.Unload();

#if DEBUG
    OneLevelDebug.Unload();
#endif
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
  // public bool IsInGameplay() =>
  //     ChunkLoader.ChunkMap.ContainsKey(GameManager._instance?.sceneName);
}
