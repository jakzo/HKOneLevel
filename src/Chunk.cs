namespace OneLevel;

public class Chunk {
  public string SceneName;
  public Vector3 Position;
  public Rect[] Colliders;
  public Action<Scene> OnLoad;
}

class ChunkState {
  public Chunk Chunk;
  public Scene MainScene;
  public List<Scene> Scenes;

  public ChunkState(Chunk chunk, Scene mainScene) {
    Chunk = chunk;
    MainScene = mainScene;
    Scenes = new List<Scene> { mainScene };
  }
}

public class ChunkMap {
  public static Dictionary<string, ChunkMap> BySceneName = new();

  public static void Register(ChunkMap chunkMap) {
    foreach (var chunk in chunkMap.Chunks) {
      BySceneName.Add(chunk.SceneName, chunkMap);
    }
  }

  public static ChunkMap CreateAndRegister(List<Chunk> chunks) {
    var chunkMap = new ChunkMap(chunks);
    Register(chunkMap);
    return chunkMap;
  }

  public List<Chunk> Chunks;
  public Dictionary<string, Chunk> ChunkBySceneName;

  public ChunkMap(List<Chunk> chunks) {
    Chunks = chunks;
    ChunkBySceneName = chunks.ToDictionary(chunk => chunk.SceneName);
  }

  public void Add(Chunk chunk) {
    Chunks.Add(chunk);
    ChunkBySceneName.Add(chunk.SceneName, chunk);
  }

  public static readonly ChunkMap HALLOWNEST =
      CreateAndRegister(new List<Chunk>() {
        // Dirtmouth
        new() {
          SceneName = "Town",
          Position = new(-132.5f, 58f),
        },
        // Exit to Dirtmouth
        new() {
          SceneName = "Crossroads_01",
          Position = new(0f, 0f),
        },
        // Black egg temple entrance
        new() {
          SceneName = "Crossroads_02",
          Position = new(100f, 9f),
        },
        // Corridor right of black egg temple
        new() {
          SceneName = "Crossroads_39",
          Position = new(190f, 7f),
        },
        // Right corner room
        new() {
          SceneName = "Crossroads_14",
          Position = new(278f, -20f),
        },
        // Gruz platform room
        new() {
          SceneName = "Crossroads_07",
          Position = new(-50f, -74f),
          Colliders = new Rect[] { new(42f, 76f, 10f, 5f) },
        },
        // Grub room
        new() {
          SceneName = "Crossroads_38",
          Position = new(-121f, 5f),
        },
        // Cornifer room
        new() {
          SceneName = "Crossroads_33",
          Position = new(-51f, -124f),
          Colliders = new Rect[] { new(20f, 30f, 2f, 1f) },
        },
        // Shaman entrance
        new() {
          SceneName = "Crossroads_06",
          Position = new(-6f, -93f),
          OnLoad =
              scene => {
                // TODO: Make collider not overlap gruz platform room
              },
        },
        // False knight arena
        // TODO: Game hangs when unloading chunks if this is part of the chunk
        // map (something to do with the Crossroads_10_boss scene?)
        new() {
          SceneName = "Crossroads_10",
          Position = new(55f, -93f),
        },
        // Central-left corridor
        new() {
          SceneName = "Crossroads_05",
          Position = new(32f, -26f),
        },
        // Central-middle corridor
        new() {
          SceneName = "Crossroads_40",
          Position = new(114f, -18f),
        },
        // Central-right corridor
        new() {
          SceneName = "Crossroads_16",
          Position = new(202f, -24f),
        },
        // Right vertical passage
        new() {
          SceneName = "Crossroads_03",
          Position = new(246f, -112f),
        },
        // Pre-boss room
        new() {
          SceneName = "Crossroads_21",
          Position = new(138f, -95f),
        },
        // Glowing womb room
        new() {
          SceneName = "Crossroads_22",
          Position = new(139f, -64f),
        },
        // Hot springs entrance
        new() {
          SceneName = "Crossroads_08",
          Position = new(-6f, -136f),
        },
        // Left goam room
        new() {
          SceneName = "Crossroads_13",
          Position = new(53f, -140f),
        },
        // Right goam room
        new() {
          SceneName = "Crossroads_42",
          Position = new(133f, -130f),
        },
        // Bottom-right intersection room
        new() {
          SceneName = "Crossroads_19",
          Position = new(243f, -160f),
        },
        // Stag room
        new() {
          SceneName = "Crossroads_47",
          Position = new(199f, -104f),
        },
      });
}
