namespace OneLevel;

static class Utils {
  public static void Try(string id, Action action, Action onError = null) {
    try {
      action();
    } catch (Exception ex) {
      TryLogException(ex, id);
      onError();
    }
  }

  public static void Try(Action action,
                         Action onError = null) => Try(null, action, onError);

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
