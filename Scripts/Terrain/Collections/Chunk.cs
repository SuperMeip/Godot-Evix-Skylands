using Evix.Terrain.DataGeneration;
using Evix.Terrain.MeshGeneration;
using Evix.Terrain.Resolution;
using Evix.Voxels;

namespace Evix.Terrain.Collections {

  /// <summary>
  /// A chunk of terrain that uses the Coordinate of it's 0,0,0 as an ID in the level
  /// </summary>
  public class Chunk {

    /// <summary>
    /// Levels of how fully loaded a chunk's data can be, and how it's displayed
    /// It's "resolution"
    /// </summary>
    public enum Resolution { UnLoaded, Loaded, Meshed, Visible };

    /// <summary>
    /// The chunk of terrain's diameter in voxels. Used for x y and z
    /// </summary>
    public const int Diameter = 16;

    /// <summary>
    /// The current resolution of this chunk
    /// </summary>
    public Resolution currentResolution {
      get;
      private set;
    } = Resolution.UnLoaded;

    /// <summary>
    /// This chunks generated mesh
    /// </summary>
    public ChunkMeshData meshData {
      get;
      private set;
    } = null;

    /// <summary>
    /// The number of solid (non 0) voxels in the chunk
    /// </summary>
    public int solidVoxelCount {
      get;
      private set;
    } = 0;

    /// <summary>
    /// get if this chunk is empty
    /// </summary>
    public bool isEmpty {
      get => voxels == null;
    }

    /// <summary>
    /// get if the generated mesh is empty
    /// </summary>
    public bool meshIsEmpty {
      get => meshData == null;
    }

    /// <summary>
    /// The chunk is solid if the solid voxel count equals the max voxel count
    /// </summary>
    public bool isSolid {
      get => solidVoxelCount == Diameter * Diameter * Diameter;
    }

    /// <summary>
    /// If this chunk is locked for work by an aperture
    /// </summary>
    public bool isLockedForWork {
      get;
      private set;
    } = false;

    /// <summary>
    /// The type of resolution work being preformed on this chunk
    /// </summary>
    Resolution resolutionModificationLockType;

    /// <summary>
    /// The voxels
    /// </summary>
    byte[] voxels = null;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="z"></param>
    /// <returns></returns>
    public static Coordinate IDFromWorldLocation(int x, int y, int z) {
      return new Coordinate(x >> 4, y >> 4, z >> 4);
    }

    /// <summary>
    /// Get the world location of the 0,0,0 of the chunk with this id
    /// </summary>
    /// <returns></returns>
    public static Coordinate IDToWorldLocation(Coordinate chunkID) {
      return new Coordinate(chunkID.x * Diameter, chunkID.y * Diameter, chunkID.z * Diameter);
    }

    /// <summary>
    /// Get the voxel value stored at
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="z"></param>
    /// <returns></returns>
    public byte this[int x, int y, int z] {
      // uses same forula as in Coordinate.flatten
      get {
        return voxels != null
          ? voxels[Coordinate.Flatten(x, y, z, Diameter)]
          : (byte)0;
      }
      set {
        if (value != Voxel.Types.Empty.Id) {
          if (voxels == null) {
            voxels = new byte[Diameter * Diameter * Diameter];
          }
          if (voxels[Coordinate.Flatten(x, y, z, Diameter)] == Voxel.Types.Empty.Id) {
            solidVoxelCount++;
          }
          voxels[Coordinate.Flatten(x, y, z, Diameter)] = value;
        } else {
          if (voxels != null && voxels[Coordinate.Flatten(x, y, z, Diameter)] != Voxel.Types.Empty.Id) {
            voxels[Coordinate.Flatten(x, y, z, Diameter)] = value;
            solidVoxelCount--;
          }
        }
      }
    }

    /// <summary>
    /// Try to lock this chunk for work with this aperture
    /// </summary>
    /// <param name="aperture"></param>
    /// <returns></returns>
    public bool tryToLock(Resolution lockType) {
      if (isLockedForWork) {
        return false;
      } else {
        isLockedForWork = true;
        resolutionModificationLockType = lockType;
        return true;
      }
    }

    /// <summary>
    /// Have the aperture unlock the chunk when it's done working on it
    /// </summary>
    /// <param name="aperture"></param>
    internal void unlock(Resolution lockType) {
      if (lockType == resolutionModificationLockType) {
        isLockedForWork = false;
        resolutionModificationLockType = default;
      } else throw new System.AccessViolationException($"Wrong adjustment resolution type tried to unlock chunk: {lockType}. Expecting {resolutionModificationLockType}");
    }

    /// <summary>
    /// Set this chunk's voxel data.
    /// Can only be used when a load aperture has a lock
    /// </summary>
    /// <param name="voxels"></param>
    /// <param name="solidVoxelCount"></param>
    public void setVoxelData(byte[] voxels, int solidVoxelCount) {
      if (isLockedForWork && resolutionModificationLockType == Resolution.Loaded && currentResolution == Resolution.UnLoaded) {
        if (solidVoxelCount > 0) {
          this.voxels = voxels;
        }
        this.solidVoxelCount = solidVoxelCount;
        currentResolution = Resolution.Loaded;
      } else throw new System.AccessViolationException($"Attempting to set voxel data on a chunk without the correct aperture lock or resolution level: {currentResolution}");
    }

    /// <summary>
    /// Return the voxel data in a struct for saving
    /// </summary>
    /// <returns></returns>
    public LevelDAO.ChunkSaveData emptyChunkData(ChunkResolutionAperture.Adjustment adjustment) {
      if (isLockedForWork 
        && currentResolution >= Resolution.Loaded 
        && adjustment.resolution == Resolution.UnLoaded
        && resolutionModificationLockType == Resolution.Loaded
      ) {
        // @todo: check if the voxels aren't nulled
        LevelDAO.ChunkSaveData saveData = new LevelDAO.ChunkSaveData(voxels, solidVoxelCount);
        voxels = null;
        meshData = null;
        solidVoxelCount = 0;
        currentResolution = Resolution.UnLoaded;

        return saveData;
      } else throw new System.AccessViolationException($"Attempting to remove voxel data from a chunk without the correct aperture lock or resolution level: {currentResolution}");
    }

    /// <summary>
    /// Set that the chunk node has been meshed in game world for this chunk, or unmesh it
    /// </summary>
    public void setMesh(bool activeState = true, ChunkMeshData meshData = null) {
      if (activeState) {
        if (isLockedForWork && resolutionModificationLockType == Resolution.Meshed && currentResolution == Resolution.Loaded) {
          currentResolution = Resolution.Meshed;
          this.meshData = meshData?.arrayMesh == null ? null : meshData;
        } else throw new System.AccessViolationException($"Attempting to set a chunk as mehsed on a chunk without the correct aperture lock or resolution level: {currentResolution}");
      } else {
        if (isLockedForWork && resolutionModificationLockType == Resolution.Meshed && currentResolution == Resolution.Meshed) {
          currentResolution = Resolution.Loaded;
          this.meshData = null;
        } else throw new System.AccessViolationException($"Attempting to remove a chunk mesh from a chunk without the correct aperture lock or resolution level: {currentResolution}");
      }
    }

    /// <summary>
    /// Set the chunks visible resolution state
    /// </summary>
    /// <param name="activeState"></param>
    public void setVisible(bool activeState = true) {
      if (activeState) {
        if (isLockedForWork && resolutionModificationLockType == Resolution.Visible && currentResolution == Resolution.Meshed) {
          currentResolution = Resolution.Visible;
        } else throw new System.AccessViolationException($"Attempting to set a chunk visible without the correct aperture lock or resolution level: {currentResolution}");
      } else {
        if (isLockedForWork && resolutionModificationLockType == Resolution.Visible && currentResolution == Resolution.Visible) {
          currentResolution = Resolution.Meshed;
        } else throw new System.AccessViolationException($"Attempting to set a chunk invisible without the correct aperture lock or resolution level: {currentResolution}");
      }
    }

    /// <summary>
    /// string override
    /// 
    /// </summary>
    /// <returns></returns>
    public override string ToString() {
      return $"[#{solidVoxelCount}::%{currentResolution}]";
    }
  }
}
