using Evix.Terrain.Collections;
using System.Collections.Generic;

namespace Evix.Terrain.Resolution {
  public abstract class FocusLens : IFocusLens {

	/// <summary>
	/// The level this apeture works for
	/// </summary>
	public Level level {
	  get;
	  private set;
	}

	/// <summary>
	/// The managed focus
	/// </summary>
	protected ILevelFocus focus {
	  get;
	  private set;
	}

	/// <summary>
	/// The layered apertures that make up this lens, in order of priority
	/// </summary>
	readonly protected Dictionary<Chunk.Resolution, IChunkResolutionAperture> apeturesByResolution
	  = new Dictionary<Chunk.Resolution, IChunkResolutionAperture>();

	/// <summary>
	/// The layered apertures that make up this lens, in order of priority
	/// </summary>
	readonly IChunkResolutionAperture[] apeturesByPriority;

	/// <summary>
	/// All running jobs
	/// </summary>
	readonly List<ChunkResolutionAperture.ApetureJobHandle> runningJobs
	  = new List<ChunkResolutionAperture.ApetureJobHandle>();

	/// <summary>
	/// Create a new level of the given size that uses the given apetures.
	/// </summary>
	/// <param name="chunkBounds"></param>
	/// <param name="apeturesByPriority"></param>
	protected FocusLens(Level level, ILevelFocus focus, ILensOptions lensOptions) {
	  this.level = level;
	  this.focus = focus;
	  apeturesByPriority = initializeApertures(lensOptions);
	  foreach (IChunkResolutionAperture aperture in apeturesByPriority) {
		apeturesByResolution[aperture.resolution] = aperture;
	  }
	}

	/// <summary>
	/// Create a new level of the given size that uses the given apetures.
	/// </summary>
	/// <param name="chunkBounds"></param>
	/// <param name="apeturesByPriority"></param>
	protected FocusLens(Level level, ILevelFocus focus) {
	  this.level = level;
	  this.focus = focus;
	  apeturesByPriority = initializeApertures();
	  foreach (IChunkResolutionAperture aperture in apeturesByPriority) {
		apeturesByResolution[aperture.resolution] = aperture;
	  }
	}

	/// <summary>
	/// Initialize this lens around it's foucs.
	/// </summary>
	/// <returns>the number of chunk nodes this focus will need for rendering</returns>
	public int initialize() {
	  int chunksThatNeedARenderNode = 0;
	  foreach (IChunkResolutionAperture aperture in apeturesByPriority) {
		int newAdjustments = aperture.updateAdjustmentsForFocusInitilization(focus);
		if (aperture.resolution == Chunk.Resolution.Meshed) {
		  chunksThatNeedARenderNode = newAdjustments;
		}
	  }

	  return chunksThatNeedARenderNode;
	}

	/// <summary>
	/// Schedule the next chunk adjustment job for this lens
	/// </summary>
	public void scheduleNextChunkAdjustment() {
	  // go through each apeture in reverse priorty order, and check if their
	  // next priorityqueue item is valid. return the first valid one.
	  for (int i = apeturesByPriority.Length - 1; i > -1; i--) {
		IChunkResolutionAperture aperture = apeturesByPriority[i];
		if (aperture.tryToGetNextAdjustmentJob(focus, out ChunkResolutionAperture.ApetureJobHandle jobHandle)) {
		  jobHandle.schedule();
		  runningJobs.Add(jobHandle);
		  return;
		}
	  }
	}

	/// <summary>
	/// Handle all of the jobs that have finished for this lens
	/// </summary>
	public void handleFinishedJobs() {
	  runningJobs.RemoveAll(jobHandle => {
		if (jobHandle.jobIsComplete) {
		  apeturesByResolution[jobHandle.job.adjustment.resolution].onJobComplete(jobHandle.job);
		  return true;
		}

		return false;
	  });
	}

	/// <summary>
	/// Update each apeture whenever the focus this lens is focused on moves
	/// </summary>
	public void updateAdjustmentsForFocusMovement() {
	  foreach (IChunkResolutionAperture aperture in apeturesByPriority) {
		aperture.updateAdjustmentsForFocusLocationChange(focus);
	  }
	}

	/// <summary>
	/// Get the priority for an adjustment from aperture calculations
	/// </summary>
	/// <param name="adjustment"></param>
	/// <param name="focus"></param>
	/// <returns></returns>
	public float getAdjustmentPriority(ChunkResolutionAperture.Adjustment adjustment, ILevelFocus focus) {
	  return apeturesByResolution[adjustment.resolution].getPriority(adjustment, focus);
	}

	/// <summary>
	/// Try to get the apeture type from this lens
	/// </summary>
	/// <param name="resolution"></param>
	/// <param name="aperture"></param>
	/// <returns></returns>
	public bool tryToGetAperture(Chunk.Resolution resolution, out IChunkResolutionAperture aperture) {
	  return apeturesByResolution.TryGetValue(resolution, out aperture);
	}

	///// internal functions

	/// <summary>
	/// Get the initialized apertures this lens will be constructed of in order of priority
	/// </summary>
	/// <returns></returns>
	protected abstract IChunkResolutionAperture[] initializeApertures(ILensOptions lensOptions = null);

	/// <summary>
	/// Base interface for lens option structs
	/// </summary>
	public interface ILensOptions {

	  /// <summary>
	  /// The chunk radius to use as a base for all others.
	  /// Ususally the visual chunk radius
	  /// </summary>
	  int baseChunkRadius {
		get;
	  }
	}
  }
}
