namespace OneLevel;

class Chunk {
  public string SceneName;
  public Vector3 Position;

  public static readonly Dictionary<string, Chunk> BASE_GAME =
      new Chunk[] {
        new() {
          SceneName = "Town",
          Position = new Vector3(-132.5f, 58f, 0f),
        },
        new() {
          SceneName = "Crossroads_01",
          Position = new Vector3(0f, 0f, 0f),
        },
        new() {
          SceneName = "Crossroads_07",
          Position = new Vector3(-50f, -74f, 0f),
        },
        new() {
          SceneName = "Crossroads_02",
          Position = new Vector3(105f, 9f, 0f),
        },
        new() {
          SceneName = "Crossroads_39",
          Position = new Vector3(205f, 9f, 0f),
        },
      }
          .ToDictionary(chunk => chunk.SceneName);
}
