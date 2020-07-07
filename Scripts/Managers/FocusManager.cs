using Evix.Terrain.Collections;
using Evix.Terrain.Resolution;
using Godot;

namespace Evix.Managers {

  /// <summary>
  /// Used to track and manage a focus's position in the game world
  /// </summary>
  public class FocusManager : Spatial, ILevelFocus {

    /// <summary>
    /// The id of this focus in its level
    /// </summary>
    public int id {
      get;
      private set;
    }

    /// <summary>
    /// If this player is active
    /// </summary>
    public bool isActive {
      get;
      private set;
    }

    /// <summary>
    /// The current live chunk location of this focus
    /// </summary>
    public Coordinate currentChunkID {
      get;
      private set;
    }

    /// <summary>
    /// The previously sampled chunk location of this focus
    /// </summary>
    public Coordinate previousChunkID {
      get;
      private set;
    }

    /// <summary>
    /// The world (voxel) location of this player
    /// </summary>
    Vector3 worldLocation;

    /// <summary>
    /// the previous world location of the character
    /// </summary>
    Vector3 previousWorldLocation;

    #region Game Loop

    public override void _Process(float delta) {
      /// check to see if we should update the chunks
      if (!isActive) {
        return;
      }

      // if this is active and the world position has changed, check if the chunk has changed
      worldLocation = Translation;
      if (worldLocation != previousWorldLocation) {
        previousWorldLocation = worldLocation;
        currentChunkID = worldLocation / Chunk.Diameter;
      }
    }

    #endregion

    #region Focus Functions

    /// <summary>
    /// set the controller active
    /// </summary>
    public void activate() {
      isActive = true;
    }

    /// <summary>
    /// Get the new chunk id and update the previous location to the current location
    /// </summary>
    /// <returns></returns>
    public Coordinate getUpdatedChunkID() {
      previousChunkID = currentChunkID;
      return currentChunkID;
    }

    /// <summary>
    /// Set the world position of the focus. Also sets the chunk position.
    /// </summary>
    public void setPosition(Coordinate worldPosition) {
      Translation = worldLocation = previousWorldLocation = worldPosition.vec3;
      currentChunkID = previousChunkID = worldLocation / Chunk.Diameter;
    }

    /// <summary>
    /// Register this focus' id for a given level
    /// </summary>
    /// <param name="level"></param>
    /// <param name="id"></param>
    public void registerTo(Level level, int id) {
      this.id = id;
    }

    #endregion

    #region Equality Functions

    /// <summary>
    /// Equal override
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public bool Equals(ILevelFocus other) {
      return id.Equals(other.id);
    }

    /// <summary>
    /// hash code override
    /// </summary>
    /// <returns></returns>
    public override int GetHashCode() {
      return id;
    }

    #endregion
  }
}
