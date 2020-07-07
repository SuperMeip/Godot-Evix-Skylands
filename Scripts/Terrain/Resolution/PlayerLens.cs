using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Evix.Terrain.Resolution {
  public class PlayerLens : FocusLens {

    /// <summary>
    /// Meshed chunk buffer around the visible chunks
    /// </summary>
    const int MeshedChunkBuffer = 5;

    /// <summary>
    /// Meshed chunk buffer around the visible chunks for when the height is overrided. Ususally the height is set smaller.
    /// </summary>
    const int ChunkBufferHeightOverride = 3;

    /// <summary>
    /// Loaded chunk data buffer around the meshed chunks
    /// </summary>
    const int LoadedChunkBuffer = 5;

    /// <summary>
    /// Create a new player lens
    /// </summary>
    /// <param name="level"></param>
    /// <param name="visibleChunkRadius"></param>
    /// <param name="visibleChunkRadiusHeightOverride"></param>
    public PlayerLens(
      ILevelFocus playerFocus,
      Level level,
      int visibleChunkRadius,
      int visibleChunkRadiusHeightOverride = 0
    ) : base(
      level,
      playerFocus,
      new PlayerLensOptions(visibleChunkRadius, visibleChunkRadiusHeightOverride)
    ) { }

    /// <summary>
    /// Get the apertures
    /// </summary>
    /// <param name="lensOptions"></param>
    /// <returns></returns>
    protected override IChunkResolutionAperture[] initializeApertures(ILensOptions lensOptions) {
      PlayerLensOptions playerLensOptions = (PlayerLensOptions)lensOptions;
      return new IChunkResolutionAperture[] {
        new VoxelDataLoadedAperture(
          this,
          playerLensOptions.baseChunkRadius + MeshedChunkBuffer + LoadedChunkBuffer,
          playerLensOptions.baseChunkRadiusHeightOverride == 0
            ? playerLensOptions.baseChunkRadius + MeshedChunkBuffer + LoadedChunkBuffer
            : playerLensOptions.baseChunkRadiusHeightOverride + ChunkBufferHeightOverride + ChunkBufferHeightOverride
        ),
        new MeshGenerationAperture(
          this,
          playerLensOptions.baseChunkRadius + MeshedChunkBuffer,
          playerLensOptions.baseChunkRadiusHeightOverride == 0
            ? playerLensOptions.baseChunkRadius +MeshedChunkBuffer
            : playerLensOptions.baseChunkRadiusHeightOverride + ChunkBufferHeightOverride
        ),
        new ChunkVisibilityAperture(
          this,
          playerLensOptions.baseChunkRadius,
          playerLensOptions.baseChunkRadiusHeightOverride == 0
            ? playerLensOptions.baseChunkRadius
            : playerLensOptions.baseChunkRadiusHeightOverride
        )
      };
    }

    /// <summary>
    /// Player lens options
    /// </summary>
    struct PlayerLensOptions : ILensOptions {
      public int baseChunkRadius {
        get;
      }

      public int baseChunkRadiusHeightOverride {
        get;
      }

      public PlayerLensOptions(int visibleChunkRadius, int visibleChunkRadiusHeightOverride) {
        baseChunkRadius = visibleChunkRadius;
        baseChunkRadiusHeightOverride = visibleChunkRadiusHeightOverride;
      }
    }
  }
}
