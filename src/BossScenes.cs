namespace OneLevel;

class BossScenes {
  private readonly OneLevel _mod;

  public BossScenes(OneLevel mod) { _mod = mod; }

  public void Initialize() {
    On.SceneAdditiveLoadConditional.LoadRoutine += OnLoadRoutine;
    // On.WaitForBossLoad.OnEnter += OnWaitForBossLoadEnter;
  }

  public void Unload() {
    On.SceneAdditiveLoadConditional.LoadRoutine -= OnLoadRoutine;
    // On.WaitForBossLoad.OnEnter -= OnWaitForBossLoadEnter;
  }

  // This is called when chunk map loading finishes
  private IEnumerator
  OnLoadRoutine(On.SceneAdditiveLoadConditional.orig_LoadRoutine orig,
                SceneAdditiveLoadConditional self, bool callEvent) {
    var enumerator = orig(self, callEvent);
    while (enumerator.MoveNext()) {
      var current = enumerator.Current;
      yield return current;

      Utils.Try(() => {
        var parentScene = self.gameObject.scene.name;
        if (current is AsyncOperation &&
            _mod.SceneLoader.LoadedChunks.ContainsKey(parentScene)) {
          (current as AsyncOperation).completed += op =>
              Utils.Try("BossSceneCompleted", () => {
                var scene = USceneManager.GetSceneByName(self.sceneNameToLoad);
                var cs = _mod.SceneLoader.LoadedChunks[parentScene];
                cs.Scenes.Add(scene);
                _mod.SceneLoader.InitializeScene(cs, scene);
              });
        }
      });
    }
  }

  // TODO: Check where this FSM action is used
  // private void OnWaitForBossLoadEnter(On.WaitForBossLoad.orig_OnEnter orig,
  //                                     WaitForBossLoad self) {
  //   if (!GameManager.instance ||
  //   !SceneAdditiveLoadConditional.ShouldLoadBoss) {
  //     self.Finish();
  //     return;
  //   }

  //   void OnLoadedBoss() {
  //     self.Fsm.Event(self.sendEvent);
  //     GameManager.instance.OnLoadedBoss -= OnLoadedBoss;
  //     self.Finish();
  //   }

  //   GameManager.instance.OnLoadedBoss += OnLoadedBoss;
  // }
}
