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

  public static void InvokeEvent(object target, string eventName) {
    InvokeEvent(target, eventName, EventArgs.Empty);
  }

  public static void InvokeEvent<TEventArgs>(object target, string eventName,
                                             TEventArgs eventArgs)
      where TEventArgs : EventArgs {
    EventInfo eventInfo = target.GetType().GetEvent(
        eventName,
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
    if (eventInfo == null) {
      throw new ArgumentException(
          $"Event '{eventName}' not found on type '{target.GetType()}'.",
          nameof(eventName));
    }

    FieldInfo field = target.GetType().GetField(
        eventInfo.Name, BindingFlags.Instance | BindingFlags.NonPublic);
    if (field == null) {
      throw new InvalidOperationException(
          $"Event '{eventName}' does not have a backing field. This method cannot be used to invoke it.");
    }

    MulticastDelegate multicastDelegate =
        (MulticastDelegate)field.GetValue(target);
    if (multicastDelegate == null) {
      return;
    }

    var sender = target;
    foreach (Delegate del in multicastDelegate.GetInvocationList()) {
      del.Method.Invoke(del.Target, new object[] { sender, eventArgs });
    }
  }
}
