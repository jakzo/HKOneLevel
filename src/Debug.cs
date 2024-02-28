#if DEBUG
using OneLevel;

// Useful commands to use in the UnityExplorer C# console:
/*
var pos = tk2dCamera.Instance.transform.position -
              OneLevel.ChunkLoader.WORLD_OFFSET;
pos.z = 0f;
OneLevelDebug.MoveChunk("Crossroads_38", pos + new Vector3(10f, 0f));

tk2dCamera.Instance.transform.position - OneLevel.ChunkLoader.WORLD_OFFSET;
OneLevelDebug.MoveChunk("Crossroads_38", new Vector3(-120f, 5f));
OneLevelDebug.LogAndResetChangedChunks();
*/

static class OneLevelDebug {
  public static HashSet<Chunk> ChangedChunks = new();

  public static void MoveChunk(string sceneName, Vector3 pos) {
    var mod = OneLevel.OneLevel.Instance;
    mod.ChunkLoader.ChunkMap.TryGetValue(sceneName, out var chunk);
    if (chunk == null) {
      Logger.LogDebug($"Creating chunk because it did not exist: {sceneName}");
      chunk = new Chunk() {
        SceneName = sceneName,
        Position = pos,
      };
      mod.ChunkLoader.ChunkMap.Add(sceneName, chunk);
    }
    if (mod.ChunkLoader.LoadedChunks.TryGetValue(chunk, out var cs)) {
      MoveChunk(cs, pos);
    } else {
      mod.ChunkLoader.LoadChunk(chunk);
      ChangedChunks.Add(chunk);
    }
  }

  public static void MoveChunk(ChunkState cs, Vector3 pos) {
    var oldChunkPos = cs.Chunk.Position;
    if (pos == oldChunkPos)
      return;
    cs.Chunk.Position = pos;
    OneLevel.OneLevel.Instance.ChunkLoader.MoveChunk(cs, pos - oldChunkPos);
    ChangedChunks.Add(cs.Chunk);
  }

  public static void LogAndResetChangedChunks() {
    Logger.LogDebug("===== CHANGED CHUNKS");
    foreach (var chunk in ChangedChunks) {
      Logger.LogDebug($"SceneName = \"{chunk.SceneName}\",\n");
      Logger.LogDebug(
          $"Position = new({chunk.Position.x}f, {chunk.Position.y}f),");
    }
    Logger.LogDebug("=====");
    ChangedChunks.Clear();
  }

  public static void Initialize() {
    On.CameraController.LateUpdate += OnUpdate;
  }

  public static void Unload() { On.CameraController.LateUpdate -= OnUpdate; }

  public static ChunkState DraggingChunk;
  private static Vector2 DragStartMouse;
  private static Vector3 DragStartChunk;

  public static void OnUpdate(On.CameraController.orig_LateUpdate orig,
                              CameraController self) {
    orig(self);

    Utils.Try(() => {
      if (DraggingChunk != null) {
        if (Input.GetMouseButtonUp(0) || !Input.GetMouseButton(0)) {
          DraggingChunk = null;
        } else {
          var mousePos = GetMouseWorldPoint();
          var mouseDelta = mousePos - DragStartMouse;
          var newChunkPos = new Vector3(
              Mathf.Round(DragStartChunk.x + mouseDelta.x),
              Mathf.Round(DragStartChunk.y + mouseDelta.y), DragStartChunk.z);
          MoveChunk(DraggingChunk, newChunkPos);
        }
      }

      var isCtrlDown = Input.GetKey(KeyCode.LeftControl) ||
                       Input.GetKey(KeyCode.RightControl);
      if (DraggingChunk == null && isCtrlDown && Input.GetMouseButtonDown(0)) {
        var mousePos = GetMouseWorldPoint();
        var collider = Physics2D.OverlapPoint(mousePos, Physics2D.AllLayers);
        if (collider != null) {
          var sceneName = collider.gameObject.scene.name;
          var mod = OneLevel.OneLevel.Instance;
          if (mod.ChunkLoader.ChunkMap.TryGetValue(sceneName, out var chunk) &&
              mod.ChunkLoader.LoadedChunks.TryGetValue(chunk, out var cs)) {
            Logger.LogDebug($"Dragging chunk = {cs.Chunk.SceneName}");
            DraggingChunk = cs;
            DragStartMouse = mousePos;
            DragStartChunk = cs.Chunk.Position;
          }
        }
      }

      if (ThingToMove != null) {
        ThingToMove.transform.position = GetMouseWorldPoint();
        var cam = GameCameras.instance.tk2dCam.GetComponent<UCamera>();
        if (cam.transform.position != LastCamPos) {
          // Logger.LogDebug($"LastCamPos = {LastCamPos}");
          LastCamPos = cam.transform.position;
        }
      }
    });
  }

  public static GameObject ThingToMove;
  public static Vector3 LastCamPos;
  public static void DebugMouse(Vector2 size = default) {
    if (size == Vector2.zero)
      size = new(2f, 2f);

    var go = new GameObject("OneLevel_DebugMouse");

    var meshFilter = go.AddComponent<MeshFilter>();
    meshFilter.mesh = new() {
      vertices =
          new Vector3[] {
            new(0f, 0f),
            new(size.x, 0f),
            new(0f, size.y),
            new(size.x, size.y),
          },
      triangles = new[] { 0, 2, 1, 2, 3, 1 },
    };
    meshFilter.mesh.RecalculateNormals();

    var meshRenderer = go.AddComponent<MeshRenderer>();
    meshRenderer.material = new Material(Shader.Find("Sprites/Lit")) {
      color = Color.magenta,
    };

    ThingToMove = go;
  }

  private static Vector2 GetMouseWorldPoint() {
    var mousePos = Input.mousePosition;
    var cam = GameCameras.instance.tk2dCam.GetComponent<UCamera>();
    mousePos.z = cam.WorldToScreenPoint(Vector3.zero).z;
    return cam.ScreenToWorldPoint(mousePos);
  }
}
#endif
