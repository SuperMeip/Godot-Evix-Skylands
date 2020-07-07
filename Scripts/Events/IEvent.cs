namespace Evix.Events {

  /// <summary>
  /// An event, the way observers communicate and notify eachother of changes
  /// </summary>
  public interface IEvent {
    
    /// <summary>
    /// The name of this event
    /// </summary>
    string name {
      get;
    }
  }
}