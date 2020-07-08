using Evix.Events;
using Evix.Terrain.MeshGeneration;
using Evix.Terrain.Resolution;
using Godot;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Evix.Managers {
  class LevelTerrainManager : GridMap, IObserver {

	/// <summary>
	/// The level this apeture works for
	/// </summary>
	public Level level {
	  get;
	  private set;
	}

	/// <summary>
	/// The thread running for the apertureJobQueue
	/// </summary>
	System.Threading.Thread chunkApertureManagerThread;

	/// <summary>
	/// IF the chunk manager queue should be running for this level
	/// </summary>
	bool runChunkManager = false;

	/// <summary>
	/// If the manager is loaded yet
	/// </summary>
	bool isLoaded = false;

	/// <summary>
	/// Chunks waiting to be meshed
	/// </summary>
	readonly ConcurrentPriorityQueue<float, MeshGenerationAperture.ChunkMeshLoadingFinishedEvent> chunksToMesh
	  = new ConcurrentPriorityQueue<float, MeshGenerationAperture.ChunkMeshLoadingFinishedEvent>();

	/// <summary>
	/// Chunk controllers waiting for assignement and activation
	/// </summary>
	readonly ConcurrentPriorityQueue<float, ChunkResolutionAperture.Adjustment> chunksToSetVisible
	  = new ConcurrentPriorityQueue<float, ChunkResolutionAperture.Adjustment>();

	/// <summary>
	/// Chunks waiting to be hidden
	/// </summary>
	readonly ConcurrentPriorityQueue<float, Coordinate> chunksToSetInvisible
	  = new ConcurrentPriorityQueue<float, Coordinate>();

	/// <summary>
	/// Chunks waiting for meshes to clear
	/// </summary>
	readonly ConcurrentPriorityQueue<float, Coordinate> chunksToDemesh
	  = new ConcurrentPriorityQueue<float, Coordinate>();

	/// <summary>
	/// The ID of the chunk mesh in the mesh library
	/// </summary>
	readonly ConcurrentDictionary<Coordinate, int> chunkMeshesByID
	  = new ConcurrentDictionary<Coordinate, int>();

	/// <summary>
	/// The current ID to use for new meshes
	/// </summary>
	int currentMaxMeshCount = 0;

	#region Initialization

	/// <summary>
	/// Initilize the level queue manager to follow the foci and appetures of the level
	/// </summary>
	/// <param name="level"></param>
	public void initializeFor(Level level, ILevelFocus initialFocus) {
	  if (level == null) {
		throw new System.MissingMemberException("LevelManager Missing a Level, can't work");
	  } else {
		/// subscribe to async chunk updates
		World.EventSystem.subscribe(
		  this,
		  EventSystems.WorldEventSystem.Channels.ChunkActivationUpdates
		);

		///  init the level
		this.level = level;
		MeshLibrary = new MeshLibrary();
		initilizePlayerFocus(initialFocus);

		/// start the manager job in a seperate thread
		chunkApertureManagerThread = new System.Threading.Thread(() => ManageLoadedChunks()) {
		  Name = "Level Aperture Queue Manager"
		};
		chunkApertureManagerThread.Start();
		isLoaded = true;
		runChunkManager = true;
	  }
	}

	/// <summary>
	/// Spawn a new player in and initialize their level focus
	/// </summary>
	/// <param name="newFocus"></param>
	void initilizePlayerFocus(ILevelFocus newFocus) {
	  IFocusLens newLens = level.addPlayerFocus(newFocus);
	  newLens.initialize();
	  newFocus.activate();
	}

	#endregion

	#region Game Loop

	//  // Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(float delta) {
	  if (!isLoaded) {
		return;
	  }

	  /// try to set the chunk visible if it's mesh is ready
	  if (chunksToMesh.tryDequeue(out KeyValuePair<float, MeshGenerationAperture.ChunkMeshLoadingFinishedEvent> eventAndPriority)) {
		setChunkMesh(eventAndPriority.Value.adjustment.chunkID, eventAndPriority.Value.generatedChunkMesh);
	  }

	  /// try to set the chunk visible if it's mesh is ready
	  if (chunksToSetVisible.tryDequeue(out KeyValuePair<float, ChunkResolutionAperture.Adjustment> adjustmentAndPriority)) {
		if (!tryToSetChunkVisible(adjustmentAndPriority.Value)) {
		  chunksToSetVisible.enqueue(level.getPriorityForAdjustment(adjustmentAndPriority.Value), adjustmentAndPriority.Value);
		}
	  }

	  /// try to set the chunk visible if it's mesh is ready
	  if (chunksToSetInvisible.tryDequeue(out KeyValuePair<float, Coordinate> chunkLocationAndPriority)) {
		setChunkInvisible(chunkLocationAndPriority.Value);
	  }

	  /// try to set the chunk visible if it's mesh is ready
	  if (chunksToDemesh.tryDequeue(out chunkLocationAndPriority)) {
		unMeshChunk(chunkLocationAndPriority.Value);
	  }
	}

	#endregion

	#region Level Management Loop

	/// <summary>
	/// A loop to be run seperately to manage the lenses for this level.
	/// </summary>
	void ManageLoadedChunks() {
	  while (runChunkManager) {
		level.forEachFocalLens((lens, focus) => {
		  if (focus.isActive && focus.previousChunkID != focus.getUpdatedChunkID()) {
			lens.updateAdjustmentsForFocusMovement();
		  }
		  lens.scheduleNextChunkAdjustment();
		  lens.handleFinishedJobs();
		});
	  }
	}

	/// <summary>
	/// Get notifications from other observers, EX:
	///   block breaking and placing
	///   player chunk location changes
	/// </summary>
	/// <param name="event">The event to notify this observer of</param>
	/// <param name="origin">(optional) the source of the event</param>
	public void notifyOf(IEvent @event) {
	  // ignore events if we have no level to control
	  if (!isLoaded || level == null) {
		return;
	  }

	  switch (@event) {
		// when a chunk mesh comes into focus, or loads, set the mesh to a chunkManager
		case MeshGenerationAperture.ChunkMeshLoadingFinishedEvent cmfle:
		  chunksToMesh.enqueue(level.getPriorityForAdjustment(cmfle.adjustment), cmfle);
		  break;
		// when the level finishes loading a chunk's mesh. Render it in world
		case ChunkVisibilityAperture.SetChunkVisibleEvent scae:
		  chunksToSetVisible.enqueue(level.getPriorityForAdjustment(scae.adjustment), scae.adjustment);
		  break;
		case ChunkVisibilityAperture.SetChunkInvisibleEvent scie:
		  chunksToSetInvisible.enqueue(level.getPriorityForAdjustment(scie.adjustment), scie.adjustment.chunkID);
		  break;
		case MeshGenerationAperture.RemoveChunkMeshEvent rcme:
		  chunksToDemesh.enqueue(level.getPriorityForAdjustment(rcme.adjustment), rcme.adjustment.chunkID);
		  break;
		default:
		  return;
	  }
	}

	#endregion

	#region Utility Functions

	/// <summary>
	/// Set a new mesh in the mesh library
	/// </summary>
	/// <param name="chunkID"></param>
	/// <param name="meshData"></param>
	void setChunkMesh(Coordinate chunkID, ChunkMeshData meshData) {
	  int newMeshID = currentMaxMeshCount++;
	  MeshLibrary.CreateItem(newMeshID);
	  MeshLibrary.SetItemMesh(newMeshID, meshData.arrayMesh);
		// set the collider
	  MeshLibrary.SetItemShapes(
		newMeshID,
			new Godot.Collections.Array(new object[] {
				new ConcavePolygonShape() {
						Data = meshData.generatedColliderVerts
				},
				Transform.Identity
			})
	  );

	  chunkMeshesByID.TryAdd(chunkID, newMeshID);
	}

	/// <summary>
	/// Set the chunk mesh at the given location visible
	/// </summary>
	/// <param name="chunkID"></param>
	bool tryToSetChunkVisible(ChunkResolutionAperture.Adjustment adjustment) {
	  if (chunkMeshesByID.ContainsKey(adjustment.chunkID)) {
		SetCellItem(adjustment.chunkID.x, adjustment.chunkID.y, adjustment.chunkID.z, chunkMeshesByID[adjustment.chunkID]);

		return true;
	  }

	  return false;
	}

	/// <summary>
	/// Set the chunk mesh at the given location visible
	/// </summary>
	/// <param name="chunkID"></param>
	void setChunkInvisible(Coordinate chunkID) {
	  if (chunkMeshesByID.ContainsKey(chunkID)) {
		SetCellItem(chunkID.x, chunkID.y, chunkID.z, InvalidCellItem);
	  }
	}

	/// <summary>
	/// Remove the stored mesh for the given chunk
	/// </summary>
	/// <param name="ChunkID"></param>
	void unMeshChunk(Coordinate ChunkID) {
	  if (chunkMeshesByID.ContainsKey(ChunkID)) {
		MeshLibrary.RemoveItem(chunkMeshesByID[ChunkID]);
		chunkMeshesByID.TryRemove(ChunkID, out _);
	  }
	}

	#endregion
  }
}
