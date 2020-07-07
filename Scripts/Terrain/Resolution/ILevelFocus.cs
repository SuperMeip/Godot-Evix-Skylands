using System;

namespace Evix.Terrain.Resolution {

  /// <summary>
  /// A focus in the level that terrain and events load around
  /// </summary>
  public interface ILevelFocus : IEquatable<ILevelFocus> {

    /// <summary>
    /// The ID of this focus for the level it's in.
    /// </summary>
    int id {
      get;
    }

    /// <summary>
    /// The current live chunk location of this focus
    /// </summary>
    Coordinate currentChunkID {
      get;
    }

    /// <summary>
    /// The previously sampled chunk location of this focus
    /// </summary>
    Coordinate previousChunkID {
      get;
    }

    /// <summary>
    /// If this focus is active
    /// </summary>
    bool isActive { 
      get;
    }

    /// <summary>
    /// Get the new current chunkID of the chunk this player is in.
    /// This call should update the previous and current chunk ID as well.
    /// </summary>
    Coordinate getUpdatedChunkID();

    /// <summary>
    /// Register this focus to the given level. This should set this focus' id
    /// </summary>
    /// <param name="level"></param>
    void registerTo(Level level, int id);

    /// <summary>
    /// Activat the focus' tracking
    /// </summary>
    void activate();

    /// <summary>
    /// Move this focus to the given world location
    /// </summary>
    void setPosition(Coordinate worldLocation);
  }
}
