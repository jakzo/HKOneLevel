namespace OneLevel;

static class Utils {
  public static class Layers {
    public class LazyLayer {
      public string Name;
      private int _id;
      private bool _hasComputedId = false;
      public LazyLayer(string name) { Name = name; }
      public int Id {
        get => _hasComputedId ? _id : (_id = LayerMask.NameToLayer(Name));
      }
    }

    public static LazyLayer Player = new("Player");
  }

  public static void Try(string id, Action action, Action onError = null) {
    try {
      action();
    } catch (Exception ex) {
      TryLogException(ex, id);
      onError();
    }
  }

  public static void Try(Action action, Action onError = null) {
    try {
      action();
    } catch (Exception ex) {
      TryLogException(ex);
      onError();
    }
  }

  public static T Try<T>(Func<T> action, Func<T> onError = null) {
    try {
      return action();
    } catch (Exception ex) {
      TryLogException(ex);
      return onError != null ? onError() : default;
    }
  }

  private static void TryLogException(Exception ex, string id = null) {
    // OuterMethod() -> Try() -> TryLogException()
    id ??= new StackTrace().GetFrame(2).GetMethod().Name;
    Logger.LogError($"Failed to execute {id}:");
    Logger.LogError(ex);
  }
}
