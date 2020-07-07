using Godot;

namespace Evix.Voxels {

  /// <summary>
  /// USed for manipulating generic voxeltypes
  /// </summary>
  public interface IVoxelType {

    /// <summary>
    /// The ID of the voxel
    /// </summary>
    byte Id {
      get;
    }

    /// <summary>
    /// If this voxel type is solid or not
    /// </summary>
    bool IsSolid {
      get;
    }

    /// <summary>
    /// The color of this block
    /// </summary>
    Color Color {
      get;
    }
  }
}