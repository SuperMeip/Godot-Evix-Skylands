using Evix.Terrain.Collections;
using Evix.Terrain.DataGeneration;
using System;

namespace Evix.Terrain.Resolution {
  class VoxelDataLoadedAperture : ChunkResolutionAperture {
    public VoxelDataLoadedAperture(IFocusLens lens, int managedChunkRadius, int managedChunkHeight = 0) 
      : base(Chunk.Resolution.Loaded, lens, managedChunkRadius, managedChunkHeight) {
    }

    #region Aperture Functions

    /// <summary>
    /// Make sure it's not already loaded, or unloaded.
    /// </summary>
    /// <param name="adjustment"></param>
    /// <param name="chunk"></param>
    /// <returns></returns>
    internal override bool isValid(Adjustment adjustment, out Chunk chunk) {
      return base.isValid(adjustment, out chunk)
        && ((adjustment.type == FocusAdjustmentType.InFocus && chunk.currentResolution == Chunk.Resolution.UnLoaded)
          || (adjustment.type == FocusAdjustmentType.OutOfFocus && chunk.currentResolution != Chunk.Resolution.UnLoaded));
    }

    /// <summary>
    /// Get the job. There's 3 types. Load from file, from noise, and save to file
    /// </summary>
    /// <param name="adjustment"></param>
    /// <returns></returns>
    protected override IAdjustmentJob getJob(Adjustment adjustment) {
      if (adjustment.type == FocusAdjustmentType.InFocus) {
        if (LevelDAO.ChunkFileExists(adjustment.chunkID, lens.level)) {
          return new LevelDAO.LoadChunkDataFromFileJob(adjustment, lens.level);
          // if there's no file, we need to generate the chunk data from scratch
        } else {
          return BiomeMap.GetTerrainGenerationJob(adjustment, lens.level);
        }
        /// if it's out of focus, we want to save the chunk to file
      } else {
        Chunk chunkToSave = lens.level.getChunk(adjustment.chunkID);
        if (chunkToSave.currentResolution != Chunk.Resolution.UnLoaded) {
          return new LevelDAO.SaveChunkDataToFileJob(
            new Adjustment(adjustment.chunkID, adjustment.type, Chunk.Resolution.UnLoaded, adjustment.focusID),
            lens.level
          );
        } else throw new System.MissingMemberException(
          $"VoxelDataAperture is trying to save chunk data for {adjustment.chunkID} but could not find the chunk data in the level"
        );
      }
    }
  }

  #endregion
}
