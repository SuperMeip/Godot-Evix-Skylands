using System;
using Godot;

/// <summary>
/// Used for interfacing with blocks
/// </summary>
namespace Evix.Voxels {

  /// <summary>
  /// Used for creating voxel constants
  /// </summary>
  public abstract class Voxel {

    public abstract class Types {

      /// <summary>
      /// Basic empty voxel
      /// </summary>
      public static Type Empty {
        get;
      } = new EmptyVoxelType();

      /// <summary>
      /// basic solid voxel
      /// </summary>
      public static Type Solid {
        get;
      } = new SolidVoxelType();

      /// <summary>
      /// Get basic voxel types
      /// </summary>
      /// <param name="voxelID"></param>
      /// <returns></returns>
      public static Type Get(byte voxelID) {
        return voxelID > 0
          ? Solid
          : Empty;
      }
    }

    /// <summary>
    /// A class for storing the values of each type of block
    /// </summary>
    public abstract class Type : IVoxelType, IEquatable<Type> {

      /// <summary>
      /// The ID of the block
      /// </summary>
      public byte Id {
        get;
        protected set;
      }

      /// <summary>
      /// If this block type is solid block or not
      /// </summary>
      public bool IsSolid {
        get;
        protected set;
      } = true;

      /// <summary>
      /// If this block type is solid block or not
      /// </summary>
      public Color Color {
        get;
        protected set;
      } = new Color(0, 0, 0, 0);

      /// <summary>
      /// Make a new type
      /// </summary>
      /// <param name="id"></param>
      internal Type(byte id) {
        Id = id;
      }

      /// <summary>
      /// Compare two voxels by id
      /// </summary>
      /// <param name="other"></param>
      /// <returns></returns>
      public bool Equals(Type other) {
        return other.Id == Id;
      }
    }

    /// <summary>
    /// basic type for empty voxel
    /// </summary>
    class EmptyVoxelType : Type {
      internal EmptyVoxelType() : base(0) {
        IsSolid = false;
      }
    }
    
    /// <summary>
    /// basic type for solid voxel
    /// </summary>
    class SolidVoxelType : Type {
      internal SolidVoxelType() : base(1) {
        IsSolid = true;
      }
    }
  }
}