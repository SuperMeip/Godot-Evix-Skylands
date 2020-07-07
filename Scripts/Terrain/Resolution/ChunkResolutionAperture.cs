using Evix.Terrain.Collections;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Evix.Terrain.Resolution {
  public abstract class ChunkResolutionAperture : IChunkResolutionAperture {

	/// <summary>
	/// The focus change type of an adjustment to be made to a chunk
	/// </summary>
	public enum FocusAdjustmentType { InFocus, OutOfFocus };

	/// <summary>
	/// The resolution this aperture loads to
	/// </summary>
	public Chunk.Resolution resolution {
	  get;
	  private set;
	}

	/// <summary>
	/// The managed chunk area radius, X and Z. Height may be different.
	/// </summary>
	public int managedChunkRadius {
	  get;
	  private set;
	}

	/// <summary>
	/// The managed chunk area height
	/// </summary>
	public int managedChunkHeightRadius {
	  get;
	  private set;
	}

	/// <summary>
	/// The level this apeture works for
	/// </summary>
	protected IFocusLens lens {
	  get;
	}

	/// <summary>
	/// The maximum distance a managed chunk should be from the center of this aperture.
	/// </summary>
	readonly int MaxManagedChunkDistance;

	/// <summary>
	/// The y weight multiplier of this apeture, used for priority and distance skewing
	/// </summary>
	readonly float YWeightMultiplier;

	/// <summary>
	/// The priority queue that this aperture manages
	/// </summary>
	readonly PriorityQueue<int, Adjustment> adjustmentQueue
	  = new PriorityQueue<int, Adjustment>();

	/// <summary>
	/// The chunk bounds this aperture is managing
	/// </summary>
	Coordinate[] managedChunkBounds
	  = new Coordinate[2];

	#region constructors

	/// <summary>
	/// Default constructor
	/// </summary>
	/// <param name="level"></param>
	/// <param name="managedChunkRadius"></param>
	/// <param name="managedChunkHeight"></param>
	protected ChunkResolutionAperture(
	  Chunk.Resolution resolution,
	  IFocusLens lens,
	  int managedChunkRadius,
	  int managedChunkHeight = 0,
	  float yDistanceWeightMultiplier = 5.0f
	  ) {
	  this.resolution = resolution;
	  this.lens = lens;
	  this.managedChunkRadius = managedChunkRadius;
	  YWeightMultiplier = yDistanceWeightMultiplier;
	  managedChunkHeightRadius = managedChunkHeight == 0 ? managedChunkRadius : managedChunkHeight;
	  double distanceSquared = Math.Pow(managedChunkRadius, 2);
	  double distanceHeightSquared = Math.Pow(managedChunkHeightRadius, 2);
	  MaxManagedChunkDistance = (int)Math.Sqrt(
	  // a[a'^2 + b'^2] squared + b squared 
	  distanceSquared + distanceSquared + distanceHeightSquared
	  ) + 1;
	}

	#endregion

	#region IChunkResolutionAperture API Functions

	/// <summary>
	/// Check if this adjustment is valid for this aperture still.
	/// </summary>
	/// <param name="adjustment"></param>
	/// <param name="chunk"></param>
	/// <returns></returns>
	internal virtual bool isValid(Adjustment adjustment, out Chunk chunk) {
	  chunk = lens.level.getChunk(adjustment.chunkID);

	  return true;
	}

	/// <summary>
	/// Check if this adjustment is ready to schedule a job for.
	/// </summary>
	/// <param name="adjustment"></param>
	/// <returns></returns>
	protected virtual bool isReady(Adjustment adjustment, Chunk validatedChunk) {
	  return true;
	}

	/// <summary>
	/// Construct a job for this adjustment to be scheduled.
	/// </summary>
	/// <param name="adjustment"></param>
	/// <returns></returns>
	protected abstract IAdjustmentJob getJob(Adjustment adjustment);

	#endregion

	#region Lens Adjustment API

	/// <summary>
	/// Try to get the next adjustment job that should be run for this aperture
	/// </summary>
	/// <param name="jobHandle"></param>
	/// <returns></returns>
	public bool tryToGetNextAdjustmentJob(ILevelFocus focus, out ApetureJobHandle jobHandle) {
	  if (adjustmentQueue.tryDequeue(out Adjustment adjustment)) {
		if (!isWithinManagedBounds(adjustment) || !isValid(adjustment, out Chunk validatedChunk)) {
		  jobHandle = null;
		  return false;
		}

		// if the item is not locked by another job, check if it's ready
		// if it's ready, we'll try to lock it so this aperture can work on it
		if (!validatedChunk.isLockedForWork
		  && isReady(adjustment, validatedChunk)
		  && validatedChunk.tryToLock(adjustment.resolution)
		) {
		  //World.Debugger.log($"Aperture Type {GetType()} running job for adjustment: {adjustment}");
		  jobHandle = new ApetureJobHandle(getJob(adjustment));
		  return true;
		  // if it's not ready, or there's a conflict requeue
		  // if there's a conflict, it means a job is already running on this chunk and we should wait for that one to finish
		} else {
		  adjustmentQueue.Enqueue(
		  getPriority(adjustment, focus),
		  adjustment
		  );
		  jobHandle = null;
		  return false;
		}
		// if there's no jobs left, we can get nothing
	  } else {
		jobHandle = null;
		return false;
	  }
	}

	/// <summary>
	/// Get the chunks for a new focus point being initilized
	/// </summary>
	/// <param name="newFocalPoint"></param>
	public int updateAdjustmentsForFocusInitilization(ILevelFocus newFocalPoint) {
	  List<Adjustment> chunkAdjustments = new List<Adjustment>();
	  int newAdjustmentCount = 0;
	  managedChunkBounds = getManagedChunkBounds(newFocalPoint);

	  /// just get the new in focus chunks for the whole managed area
	  managedChunkBounds[0].until(managedChunkBounds[1], inFocusChunkLocation => {
		chunkAdjustments.Add(new Adjustment(inFocusChunkLocation, FocusAdjustmentType.InFocus, resolution, newFocalPoint.id));
	  });

	  foreach (Adjustment adjustment in chunkAdjustments) {
		adjustmentQueue.Enqueue(getPriority(adjustment, newFocalPoint), adjustment);
		newAdjustmentCount++;
	  }

	  return newAdjustmentCount;
	}

	/// <summary>
	/// Adjust the bounds and resolution loading for the given focus.
	/// </summary>
	/// <param name="focus"></param>
	public void updateAdjustmentsForFocusLocationChange(ILevelFocus focus) {
	  List<Adjustment> chunkAdjustments = new List<Adjustment>();
	  Coordinate[] newManagedChunkBounds = getManagedChunkBounds(focus);

	  /// get newly in focus chunks
	  newManagedChunkBounds.forEachPointNotWithin(managedChunkBounds, inFocusChunkLocation => {
		chunkAdjustments.Add(new Adjustment(inFocusChunkLocation, FocusAdjustmentType.InFocus, resolution, focus.id));
	  });

	  /// see if we should get newly out of focus chunks
	  managedChunkBounds.forEachPointNotWithin(newManagedChunkBounds, inFocusChunkLocation => {
		chunkAdjustments.Add(new Adjustment(inFocusChunkLocation, FocusAdjustmentType.OutOfFocus, resolution, focus.id));
	  });

	  /// update the new managed bounds
	  managedChunkBounds = newManagedChunkBounds;

	  /// enqueue each new adjustment
	  foreach (Adjustment adjustment in chunkAdjustments) {
		adjustmentQueue.Enqueue(getPriority(adjustment, focus), adjustment);
	  }
	}

	/// <summary>
	/// Get an adjustment's priority for this aperture.
	/// Lower is better(?)
	/// </summary>
	/// <param name="adjustment"></param>
	/// <returns></returns>
	public int getPriority(Adjustment adjustment, ILevelFocus focus) {
	  int distancePriority = adjustment.type == FocusAdjustmentType.InFocus
	  ? (int)adjustment.chunkID.distanceYFlattened(focus.currentChunkID, YWeightMultiplier)
	  : MaxManagedChunkDistance - (int)adjustment.chunkID.distanceYFlattened(focus.currentChunkID, YWeightMultiplier);

	  return distancePriority/* + (int)adjustment.resolution * 3*/;
	}

	/// <summary>
	/// Do work on the completion of a job
	/// </summary>
	/// <param name="job"></param>
	public virtual void onJobComplete(IAdjustmentJob job) { }

	/// <summary>
	/// An adjustment done on a chunk
	/// </summary>
	public struct Adjustment {

	  /// <summary>
	  /// The chunk to adjust
	  /// </summary>
	  public readonly Coordinate chunkID;

	  /// <summary>
	  /// The type of focus adjustment, in or out of focus
	  /// </summary>
	  public readonly FocusAdjustmentType type;

	  /// <summary>
	  /// The resolution of the aperture making the adjustment
	  /// </summary>
	  public readonly Chunk.Resolution resolution;

	  /// <summary>
	  /// The id of the focus this adjustment was made for
	  /// </summary>
	  public readonly int focusID;

	  /// <summary>
	  /// Make a new adjustment
	  /// </summary>
	  /// <param name="chunkID"></param>
	  /// <param name="adjustmentType"></param>
	  /// <param name="resolution"></param>
	  public Adjustment(Coordinate chunkID, FocusAdjustmentType adjustmentType, Chunk.Resolution resolution, int focusID) {
		this.chunkID = chunkID;
		type = adjustmentType;
		this.resolution = resolution;
		this.focusID = focusID;
	  }
	}

	/// <summary>
	/// Handle used to control, schedule, and access an Apeture job
	/// </summary>
	public class ApetureJobHandle {

	  /// <summary>
	  /// The job to run.
	  /// Struct that stores input, output, and the function to run
	  /// </summary>
	  public readonly IAdjustmentJob job;

	  /// <summary>
	  /// Check if the job completed
	  /// </summary>
	  public bool jobIsComplete {
		get => task != null && task.IsCompleted;
	  }

	  /// <summary>
	  /// The task running the job
	  /// </summary>
	  Task task;

	  /// <summary>
	  /// Make a new handle for a job
	  /// </summary>
	  /// <param name="job"></param>
	  public ApetureJobHandle(IAdjustmentJob job) {
		this.job = job;
		task = null;
	  }

	  /// <summary>
	  /// Schedule the job to run async
	  /// </summary>
	  public void schedule() {
		task = new Task(() => job.doWork());
		task.Start();
	  }
	}

	/// <summary>
	/// An interface for a simple job to handle an adjustment
	/// </summary>
	public interface IAdjustmentJob {

	  /// <summary>
	  /// The adjustment being handled
	  /// </summary>
	  Adjustment adjustment {
		get;
	  }

	  /// <summary>
	  /// The job function to run asycn
	  /// </summary>
	  /// <returns></returns>
	  void doWork();
	}

	#endregion

	#region Utility Functions

	/// <summary>
	/// Check if the location is within any of the managed bounds
	/// </summary>
	/// <param name="chunkID"></param>
	/// <returns></returns>
	public bool isWithinManagedBounds(Coordinate chunkID) {
	  return chunkID.isWithin(managedChunkBounds);
	}

	/// <summary>
	/// Get a sibiling aperture that shares this lens of the given type
	/// </summary>
	/// <param name="resolution"></param>
	/// <returns></returns>
	protected bool tryToGetSiblingAperture(Chunk.Resolution resolution, out ChunkResolutionAperture siblingAperture) {
	  if (lens.tryToGetAperture(resolution, out IChunkResolutionAperture genericSibling)) {
		siblingAperture = genericSibling as ChunkResolutionAperture;
		return true;
	  }

	  siblingAperture = null;
	  return false;
	}

	/// <summary>
	/// Get the managed chunk bounds for the given focus.
	/// </summary>
	/// <param name="focus"></param>
	/// <returns></returns>
	Coordinate[] getManagedChunkBounds(ILevelFocus focus) {
	  Coordinate focusLocation = focus.currentChunkID;
	  return new Coordinate[] {
				(
					Math.Max(focusLocation.x - managedChunkRadius, 0),
					Math.Max(focusLocation.y - managedChunkHeightRadius, 0),
					Math.Max(focusLocation.z - managedChunkRadius, 0)
				),
				(
					Math.Min(focusLocation.x + managedChunkRadius, lens.level.chunkBounds.x),
					Math.Min(focusLocation.y + managedChunkHeightRadius, lens.level.chunkBounds.y),
					Math.Min(focusLocation.z + managedChunkRadius, lens.level.chunkBounds.z)
				)
			};
	}

	/// <summary>
	/// Get if this adjustment is still at a valid distance for being managed by this aperture
	/// </summary>
	/// <param name="coordinate"></param>
	/// <returns></returns>
	bool isWithinManagedBounds(Adjustment adjustment) {
	  bool chunkIsInFocusBounds = isWithinManagedBounds(adjustment.chunkID);
	  if ((!chunkIsInFocusBounds && adjustment.type == FocusAdjustmentType.InFocus)
	  || (chunkIsInFocusBounds && adjustment.type == FocusAdjustmentType.OutOfFocus)
	  ) {
		World.Debugger.log($"Aperture Type {GetType()} has dropped out of bounds adjustment: {adjustment}");
		return false;
	  }

	  return true;
	}

	#endregion
  }
}
