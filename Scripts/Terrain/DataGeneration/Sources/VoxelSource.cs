namespace Evix.Terrain.DataGeneration.Sources {

  /// <summary>
  /// Base class for a voxel source
  /// </summary>
  public abstract class VoxelSource {

    /// <summary>
    /// Generated voxels for this source.
    /// @todod, make this instanced to make it thread safe?
    /// </summary>
    public static int VoxelsGenerated = 0;

    /// <summary>
    /// The generation seed
    /// </summary>
    public int seed {
      get;
    }

    /// <summary>
    /// The noise generator used for this voxel source
    /// </summary>
    protected Noise.FastNoise noise;

    /// <summary>
    /// Create a new voxel source
    /// </summary>
    public VoxelSource(int seed = 1234) {
      this.seed = seed;
      noise = new Noise.FastNoise(seed);
      setUpNoise();
    }

    /// <summary>
    /// Generate all the voxels in the given collection with this source
    /// </summary>
    /// <param name="voxelData"></param>
    public byte getVoxelValueAt(Coordinate location) {
      float noiseValue = getNoiseValueAt(location);
      return getVoxelTypeFor(noiseValue, location);
    }

    /// <summary>
    /// Function for setting up noise before generation
    /// </summary>
    protected virtual void setUpNoise() { }

    /// <summary>
    /// Must be implimented, get the noise density float (0 -> 1) for a given point
    /// </summary>
    /// <param name="location">the x y z to get the iso density for</param>
    /// <returns></returns>
    protected abstract float getNoiseValueAt(Coordinate location);

    /// <summary>
    /// Get the voxel type for the density
    /// </summary>
    /// <param name="noiseValue"></param>
    /// <returns></returns>
    protected abstract byte getVoxelTypeFor(float noiseValue, Coordinate location);
  }
}