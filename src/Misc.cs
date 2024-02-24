namespace OneLevel;

class Misc {
  private readonly OneLevel _mod;

  public Misc(OneLevel mod) { _mod = mod; }

  public void Initialize() {
    _mod.ChunkLoader.OnChunkLoad += InitializeChunk;

    // Do not draw scene borders because they cover neighboring scenes
    // TODO: Delete existing borders
    On.SceneManager.DrawBlackBorders += OnDrawBlackBorders;

    // Particles are next to the camera and obscure the world
    // TODO: Or should I move them to be in the world?
    On.SceneParticlesController.EnableParticles += OnEnableParticles;

    // The vignette stops us seeing the rest of the world so remove it
    // TODO: What to do about dark areas?
    HeroController.instance.vignette.gameObject.SetActive(false);

    // Move the hero along with the scene it was in
    if (_mod.ChunkLoader.CurrentChunk != null) {
      HeroController.instance.transform.localPosition +=
          _mod.ChunkLoader.CurrentChunk.Position + ChunkLoader.WORLD_OFFSET;
    }

    // The killplane kills NPCs in other chunks so just remove it
    // TODO: Put killplane below lowest chunk and resize to cover all chunks?
    GameManager.instance.gameObject.GetComponentInChildren<KillOnContact>()
        ?.gameObject.SetActive(false);
  }

  public void Unload() {
    _mod.ChunkLoader.OnChunkLoad -= InitializeChunk;
    On.SceneManager.DrawBlackBorders -= OnDrawBlackBorders;
    On.SceneParticlesController.EnableParticles -= OnEnableParticles;

    // The vignette stops us seeing the rest of the world so remove it
    // TODO: What to do about dark areas?
    HeroController.instance.vignette.gameObject.SetActive(false);

    // Move the hero along with the scene it was in
    if (_mod.ChunkLoader.CurrentChunk != null) {
      HeroController.instance.transform.localPosition +=
          _mod.ChunkLoader.CurrentChunk.Position + ChunkLoader.WORLD_OFFSET;
    }

    // The killplane kills NPCs in other chunks so just remove it
    // TODO: Put killplane below lowest chunk and resize to cover all chunks?
    GameManager.instance.gameObject.GetComponentInChildren<KillOnContact>()
        ?.gameObject.SetActive(false);

    if (_mod.IsInGameplay()) {
      RestoreChunk(USceneManager.GetActiveScene());
    }
  }

  public void InitializeChunk(ChunkState cs) {
    foreach (var obj in cs.Scene.GetRootGameObjects()) {
      foreach (var cla in obj.GetComponentsInChildren<CameraLockArea>()) {
        cla.gameObject.SetActive(false);
      }
    }
  }

  public void RestoreChunk(Scene scene) {
    foreach (var obj in scene.GetRootGameObjects()) {
      foreach (var cla in obj.GetComponentsInChildren<CameraLockArea>()) {
        cla.gameObject.SetActive(false);
      }
    }
  }

  private void OnDrawBlackBorders(On.SceneManager.orig_DrawBlackBorders orig,
                                  SceneManager self) {}

  private void
  OnEnableParticles(On.SceneParticlesController.orig_EnableParticles orig,
                    SceneParticlesController self) {}
}
