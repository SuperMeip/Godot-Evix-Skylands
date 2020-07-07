using System;

namespace Evix.Events {

  /// <summary>
  /// An event system capabple of sending out notifications
  /// </summary>
  public interface IEventSystem<ChannelList>
    where ChannelList : struct, Enum {

    /// <summary>
    /// Subscribe to the listener list.
    /// </summary>
    void subscribe(IObserver newListener, ChannelList? channelToSubscribeTo = null);

    /// <summary>
    /// Notify all listening observers of an event
    /// </summary>
    /// <param name="event">The event to notify all listening observers of</param>
    void notifyAllOf(IEvent @event, bool sendAsync = false);

    /// <summary>
    /// Notify all listening observers of an event
    /// </summary>
    /// <param name="event">The event to notify all listening observers of</param>
    /// <param name="channelToNotify">The channel to notify</param>
    /// <param name="sendAsync">(optional)Whether to send asyncly in a thread</param>
    void notifyChannelOf(IEvent @event, ChannelList channelToNotify, bool sendAsync = false);
  }
}