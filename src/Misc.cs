namespace OneLevel;

class Misc {
  private readonly OneLevel _mod;

  public Misc(OneLevel mod) { _mod = mod; }

  public void Initialize() {
    _mod.SceneLoader.OnSceneInit += InitializeScene;

    // Do not draw scene borders because they cover neighboring scenes
    // TODO: Delete existing borders/restore on unload
    On.SceneManager.DrawBlackBorders += OnDrawBlackBorders;

    // Particles are next to the camera and obscure the world
    // TODO: Or should I move them to be in the world?
    On.SceneParticlesController.EnableParticles += OnEnableParticles;

    // The vignette stops us seeing the rest of the world so remove it
    // TODO: What to do about dark areas?
    HeroController.instance.vignette.gameObject.SetActive(false);

    // Move the hero along with the scene it was in
    if (_mod.SceneLoader.CurrentChunk != null) {
      HeroController.instance.transform.localPosition +=
          _mod.SceneLoader.CurrentChunk.Position + SceneLoader.WORLD_OFFSET;
    }

    // The killplane kills NPCs in other chunks so just remove it
    // TODO: Put killplane below lowest chunk and resize to cover all chunks?
    GameManager.instance.gameObject.GetComponentInChildren<KillOnContact>()
        ?.gameObject.SetActive(false);
  }

  public void Unload() {
    _mod.SceneLoader.OnSceneInit -= InitializeScene;
    On.SceneManager.DrawBlackBorders -= OnDrawBlackBorders;
    On.SceneParticlesController.EnableParticles -= OnEnableParticles;

    HeroController.instance.vignette.gameObject.SetActive(true);

    if (_mod.SceneLoader.CurrentChunk != null) {
      HeroController.instance.transform.localPosition -=
          _mod.SceneLoader.CurrentChunk.Position + SceneLoader.WORLD_OFFSET;
    }

    GameManager.instance.gameObject.GetComponentInChildren<KillOnContact>()
        ?.gameObject.SetActive(true);
  }

  public void InitializeScene(Scene scene) {
    // TODO: Hook OnEnter instead
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
