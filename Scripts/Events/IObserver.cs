namespace Evix.Events {

  /// <summary>
  /// An object that can listen for global events
  /// </summary>
  public interface IObserver {

    /// <summary>
    /// Notify this observer of an event
    /// </summary>
    /// <param name="event">The event to notify this observer of</param>
    void notifyOf(IEvent @event);
  }
}
