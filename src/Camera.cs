namespace OneLevel;

class Camera {
  private const float INITIAL_CAM_OFFSET = 30.8f;

  public float Zoom = 2f;

  private readonly OneLevel _mod;

  public Camera(OneLevel mod) { _mod = mod; }

  public void Initialize() {
    // TODO: Update limits and locks instead of just removing them
    On.CameraController.LateUpdate += OnCameraLateUpdate;
    On.CameraController.LockToArea += OnLockToArea;
    UnlockCamera();

    On.LightBlurredBackground.UpdateCameraClipPlanes +=
        OnUpdateCameraClipPlanes;

    // TODO: On scene entry set camera position to knight
  }

  public void Unload() {
    SetCameraPosition(2f);
    GameCameras.instance.tk2dCam.ZoomFactor = 1f;

    // TODO: Restore camera limits
    On.CameraController.LateUpdate -= OnCameraLateUpdate;
    On.CameraController.LockToArea -= OnLockToArea;

    On.LightBlurredBackground.UpdateCameraClipPlanes -=
        OnUpdateCameraClipPlanes;
  }

  private void OnCameraLateUpdate(On.CameraController.orig_LateUpdate orig,
                                  CameraController self) {
    orig(self);

    Utils.Try(() => {
      DoCameraZoom();
      RemoveLimits();
    });
  }

  public void DoCameraZoom() {
    var scrollDelta = Input.mouseScrollDelta.y;
    if (scrollDelta != 0f) {
      Zoom =
          Mathf.Pow(Zoom, 1f - scrollDelta * _mod.Settings.ZoomSpeed * 0.01f);
      Logger.LogDebug($"Zoom = {Zoom}");
    }
    SetCameraPosition(Zoom);
  }

  public void SetCameraPosition(float zoom) {
    var newCamZ = -((zoom - 2f) * INITIAL_CAM_OFFSET);
    var cam = GameCameras.instance.cameraParent;
    if (newCamZ != cam.localPosition.z) {
      cam.localPosition =
          new Vector3(cam.localPosition.x, cam.localPosition.y, newCamZ);
      // TODO: Move audio listener to original cam position?
    }
  }

  public void RemoveLimits() {
    var cam = GameManager.instance.cameraCtrl;
    cam.xLimit = cam.yLimit = float.PositiveInfinity;
  }

  public void UnlockCamera() {
    var camCtrl = GameManager.instance.cameraCtrl;
    while (camCtrl.lockZoneList.Count > 0) {
      camCtrl.ReleaseLock(camCtrl.lockZoneList[0]);
    }
    camCtrl.xLimit = camCtrl.yLimit = float.PositiveInfinity;

    var camTarget = GameCameras.instance.cameraTarget;
    camTarget.xLockMin = camTarget.yLockMin = float.NegativeInfinity;
    camTarget.xLockMax = camTarget.yLockMax = float.PositiveInfinity;
  }

  private void OnLockToArea(On.CameraController.orig_LockToArea orig,
                            CameraController self, CameraLockArea lockArea) {
    // Do not lock to area
  }

  private void OnUpdateCameraClipPlanes(
      On.LightBlurredBackground.orig_UpdateCameraClipPlanes orig,
      LightBlurredBackground self) {
    orig(self);

    Utils.Try(() => {
      // TODO: Fix blurred lights flickering when zooming
      // TODO: Fix blurred background items not showing as they normally appear
      ReflectionHelper
          .GetField<LightBlurredBackground, UCamera>(self, "sceneCamera")
          .farClipPlane += Zoom * 10f;
    });
  }
}
