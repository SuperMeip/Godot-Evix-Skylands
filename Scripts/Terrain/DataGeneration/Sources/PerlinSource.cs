
namespace Evix.Terrain.DataGeneration.Sources {
  public class PerlinSource : VoxelSource {

    public PerlinSource(int seed = 1234) : base(seed) {}

    protected override float getNoiseValueAt(Coordinate location) {
      return noise.GetPerlin(location.x, location.y, location.z);
    }

    protected override byte getVoxelTypeFor(float noiseValue, Coordinate location) {
      return (byte)(noiseValue > 0 ? 2 : 0);
    }
  }
}
