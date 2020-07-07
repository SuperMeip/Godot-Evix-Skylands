using Evix.Terrain.Collections;
using Evix.Terrain.DataGeneration.Sources;
using Evix.Terrain.Resolution;
using Evix.Voxels;

namespace Evix.Terrain.DataGeneration {
  public static class BiomeMap {

    /// <summary>
    /// Get a chunk terrain generation job from the biome map
    /// </summary>
    /// <param name="chunkID"></param>
    /// <param name="level"></param>
    /// <returns></returns>
    public static GenerateChunkDataFromSourceJob GetTerrainGenerationJob(ChunkResolutionAperture.Adjustment adjustment, Level level) {
      return new GenerateChunkDataFromSourceJob(adjustment, level);
    }

    /// <summary>
    /// Get the biome for the given level seed and chunk id
    /// </summary>
    /// <param name="chunkID"></param>
    /// <param name="levelSeed"></param>
    /// <returns></returns>
    static VoxelSource GetBiomeForChunk(Coordinate chunkID, int levelSeed) {
      return new PerlinSource(levelSeed);
    }

    /// <summary>
    /// Generate all the voxels for the given chunk id using the provided biome
    /// </summary>
    /// <param name="biome"></param>
    /// <param name="chunkID"></param>
    /// <param name="generatedVoxels"></param>
    /// <returns>the number of solid voxels generated</returns>
    static int GenerateTerrainDataForChunk(VoxelSource biome, Coordinate chunkID, out byte[] generatedVoxels) {
      int solidVoxelCount = 0;
      generatedVoxels = null;
      byte[] voxels = new byte[Chunk.Diameter * Chunk.Diameter * Chunk.Diameter];
      Coordinate chunkWorldLocation = Chunk.IDToWorldLocation(chunkID);

      chunkWorldLocation.until(chunkWorldLocation + Chunk.Diameter, currentWorldLocation => {
        byte voxelValue = biome.getVoxelValueAt(currentWorldLocation);
        if (voxelValue != Voxel.Types.Empty.Id) {
          solidVoxelCount++;
          Coordinate localChunkVoxelLocation = currentWorldLocation - chunkWorldLocation;
          voxels[localChunkVoxelLocation.flatten(Chunk.Diameter)] = voxelValue;
        }
      });

      generatedVoxels = voxels;
      return solidVoxelCount;
    }

    /// <summary>
    /// Generates the chunk data from a biome source
    /// </summary>
    public struct GenerateChunkDataFromSourceJob : ChunkResolutionAperture.IAdjustmentJob {

      /// <summary>
      /// The adjustment this job is running on
      /// </summary>
      public ChunkResolutionAperture.Adjustment adjustment {
        get;
      }

      /// <summary>
      /// The level to get the chunk from
      /// </summary>
      readonly Level level;

      public GenerateChunkDataFromSourceJob(ChunkResolutionAperture.Adjustment adjustment, Level level) {
        this.adjustment = adjustment;
        this.level = level;
      }

      public void doWork() {
        VoxelSource biome = GetBiomeForChunk(adjustment.chunkID, level.seed);
        int solidVoxelCount = GenerateTerrainDataForChunk(biome, adjustment.chunkID, out byte[] generatedVoxels);
        Chunk chunk = level.getChunk(adjustment.chunkID);
        chunk.setVoxelData(generatedVoxels, solidVoxelCount);
        chunk.unlock(adjustment.resolution);
      }
    }
  }
}
