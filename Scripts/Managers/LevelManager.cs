using System;
using Evix.Controllers;
using Evix.Events;
using Evix.Terrain.Collections;
using Evix.Terrain.Resolution;
using Godot;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Evix.Managers {

  public class LevelManager : Spatial, IObserver {

    [Export]
    readonly PackedScene ChunkNode;

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
		/// IDs used to name chunks uniquely
		/// </summary>
    int currentMaxChunkID = 0;

    #region Chunk Queues

    /// <summary>
    /// The chunk node pool
    /// <summary>
    readonly ConcurrentQueue<ChunkController> freeChunkControllerPool
      = new ConcurrentQueue<ChunkController>();

    /// <summary>
    /// The chunk node pool
    /// <summary>
    readonly ConcurrentDictionary<Coordinate, ChunkController> usedChunkControllers
      = new ConcurrentDictionary<Coordinate, ChunkController>();

    /// <summary>
    /// Chunk controllers waiting for assignement and activation
    /// </summary>
    readonly ConcurrentPriorityQueue<float, ChunkResolutionAperture.Adjustment> chunkMeshesWaitingForAFreeController
      = new ConcurrentPriorityQueue<float, ChunkResolutionAperture.Adjustment>();

    /// <summary>
    /// Chunk controllers waiting for assignement and activation
    /// </summary>
    readonly ConcurrentPriorityQueue<float, Coordinate> chunksToActivate
      = new ConcurrentPriorityQueue<float, Coordinate>();

    /// <summary>
    /// Chunk controllers waiting for assignement and activation
    /// </summary>
    readonly ConcurrentPriorityQueue<float, Coordinate> chunksToDeactivate
      = new ConcurrentPriorityQueue<float, Coordinate>();

    /// <summary>
    /// Chunk controllers waiting for assignement and activation
    /// </summary>
    readonly ConcurrentPriorityQueue<float, ChunkController> chunksToDemesh
      = new ConcurrentPriorityQueue<float, ChunkController>();

    #endregion

    #region Initialization

    /// <summary>
    /// Initilize the level queue manager to follow the foci and appetures of the level
    /// </summary>
    /// <param name="level"></param>
    public void initializeFor(Level level, ILevelFocus initialFocus) {
      if (ChunkNode == null) {
        throw new System.MissingMemberException("LevelManager Missing ChunkNode, can't work");
      } else if (level == null) {
        throw new System.MissingMemberException("LevelManager Missing a Level, can't work");
      } else {
        /// subscribe to async chunk updates
        World.EventSystem.subscribe(
          this,
          EventSystems.WorldEventSystem.Channels.ChunkActivationUpdates
        );

        ///  init the level
        this.level = level;
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
      int requiredNewNodeCount = newLens.initialize();

      // create the nodes the new lens will need to render
      for (int i = 0; i < requiredNewNodeCount; i++) {
        ChunkController chunkController = (ChunkController)ChunkNode.Instance();
        chunkController.Name = $"Chunk {++currentMaxChunkID}";
        chunkController.initialize(this);
        freeChunkControllerPool.Enqueue(chunkController);
      }

      // activate the new focus so it's being tracked
      newFocus.activate();
    }

    #endregion

    #region Game Loop

    //  // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(float delta) {
      if (!isLoaded) {
        return;
      }

      /// try to assign newly mehsed chunks that are waiting on controllers, if we run out.
      if (chunkMeshesWaitingForAFreeController.tryDequeue(out KeyValuePair<float, ChunkResolutionAperture.Adjustment> chunkMeshWaitingForController)) {
        if (!tryToAssignMeshedChunkToNode(chunkMeshWaitingForController.Value.chunkID)) {
          chunkMeshesWaitingForAFreeController.enqueue(level.getPriorityForAdjustment(chunkMeshWaitingForController.Value), chunkMeshWaitingForController.Value);
        }
      }

      /// go through the chunk activation queue and activate chunks
      if (chunksToActivate.tryDequeue(out KeyValuePair<float, Coordinate> activatedChunkLocation)) {
        // if the chunk doesn't have a meshed and baked controller yet, we can't activate it, so wait.
        if (tryToGetAssignedChunkController(activatedChunkLocation.Value, out ChunkController assignedController)) {
          // is active and meshed
          if (assignedController.isActive && assignedController.isMeshed) {
            assignedController.setVisible();
          } else {
            chunksToActivate.enqueue(activatedChunkLocation);
          }
        } else if (chunkMeshesWaitingForAFreeController.Count == 0) {
          Chunk chunk = level.getChunk(activatedChunkLocation.Value);
          if (!chunk.meshIsEmpty) {
            chunksToActivate.enqueue(activatedChunkLocation);
          }
        } else {
          chunksToActivate.enqueue(activatedChunkLocation);
        }
      }

      /// go through the de-activation queue
      if (chunksToDeactivate.tryDequeue(out KeyValuePair<float, Coordinate> deactivatedChunkLocation)) {
        if (tryToGetAssignedChunkController(deactivatedChunkLocation.Value, out ChunkController assignedController)) {
          assignedController.setVisible(false);
        } // if there's no controller found then it was never set active and we can just drop it too
      }

      /// try to remove meshes for the given chunk and reset it's mesh data
      if (chunksToDemesh.tryDequeue(out KeyValuePair<float, ChunkController> chunkNodeToDemesh)) {
        chunkNodeToDemesh.Value.clearAssignedChunk();
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
          if (!tryToAssignMeshedChunkToNode(cmfle.adjustment.chunkID)) {
            chunkMeshesWaitingForAFreeController.enqueue(level.getPriorityForAdjustment(cmfle.adjustment), cmfle.adjustment);
          }
          break;
        // when the level finishes loading a chunk's mesh. Render it in world
        case ChunkVisibilityAperture.SetChunkVisibleEvent scae:
          chunksToActivate.enqueue(level.getPriorityForAdjustment(scae.adjustment), scae.adjustment.chunkID);
          break;
        case ChunkVisibilityAperture.SetChunkInvisibleEvent scie:
          chunksToDeactivate.enqueue(level.getPriorityForAdjustment(scie.adjustment), scie.adjustment.chunkID);
          break;
        case MeshGenerationAperture.RemoveChunkMeshEvent rcme:
          if (tryToGetAssignedChunkController(rcme.adjustment.chunkID, out ChunkController assignedChunkController)) {
            chunksToDemesh.enqueue(level.getPriorityForAdjustment(rcme.adjustment), assignedChunkController);
          }
          break;
        default:
          return;
      }
    }

    #endregion

    #region Utility Functions

    /// <summary>
    /// Try to get a free controller and assign this chunk mesh to it
    /// </summary>
    /// <param name="chunkID"></param>
    /// <returns></returns>
    bool tryToAssignMeshedChunkToNode(Coordinate chunkID) {
      Chunk chunk = level.getChunk(chunkID);
      if (!chunk.meshIsEmpty) {
        try {
          if (freeChunkControllerPool.TryDequeue(out ChunkController freeController)) {
            try {
              freeController.setChunkMesh(chunkID, chunk);
              usedChunkControllers.TryAdd(chunkID, freeController);
              return true;
            } catch (Exception e) {
              World.Debugger.log($"Error caught: {e.Message}");
              freeChunkControllerPool.Enqueue(freeController);
              return false;
            }
            // if there's no free controllers yet, return false
          } else {
            return false;
          }
        } catch (Exception e) {
					World.Debugger.log($"Error caught: {e.Message}");
          return false;
        }
      }

      /// if the mesh is empty, we can just drop the chunk without assigning it
      return true;
    }

    /// <summary>
    /// Try to get the controller assigned to this chunk for meshing
    /// </summary>
    /// <param name="chunkID"></param>
    /// <param name="assignedController"></param>
    /// <returns></returns>
    bool tryToGetAssignedChunkController(Coordinate chunkID, out ChunkController assignedController) {
      return usedChunkControllers.TryGetValue(chunkID, out assignedController);
    }

    #endregion
  }
}