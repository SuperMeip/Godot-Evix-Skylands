using Evix.Managers;
using Evix.Terrain.Collections;
using Evix.Terrain.MeshGeneration;
using Godot;

namespace Evix.Controllers {

  /// <summary>
  /// Controls a chunk in world
  /// </summary>
  public class ChunkController : MeshInstance {

    /// <summary>
    /// The current chunk location of the chunk this gameobject is representing.
    /// </summary>
    public Coordinate chunkLocation;

    /// <summary>
    /// If this controller is being used.
    /// </summary>
    public bool isActive = false;

    /// <summary>
    /// If this chunk has been meshed with chunk data.
    /// </summary>
    public bool isMeshed {
      get;
      private set;
    } = false;

    /// <summary>
    /// The Manager for the active level.
    /// </summary>
    LevelManager levelManager;

    /// <summary>
    /// The current generated mesh
    /// @TODO: remove this at some point
    /// </summary>
    ArrayMesh generatedMesh;

    /// <summary>
    /// The current collider node
    /// </summary>
    StaticBody collider;

    ///// PUBLIC FUNCTIONS

    /// <summary>
    /// Initialize this inactive chunk node under a level manager
    /// </summary>
    /// <param name="levelManager"></param>
    /// <param name="meshInstance"></param>
    public void initialize(LevelManager levelManager) {
      this.levelManager = levelManager;
      isMeshed = false;
    }

    /// <summary>
    /// Set the chunk to render. Returns true if the data was set up
    /// </summary>
    public void setChunkMesh(Coordinate chunkID, Chunk chunk) {
      chunkLocation = chunkID;
      generatedMesh = chunk.meshData.arrayMesh;
      isMeshed = true;
      isActive = true;
    }

    /// <summary>
    /// Set the active state of this chunk.
    /// This adds and removes it from the node tree
    /// </summary>
    /// <param name="activeState"></param>
    public void setVisible(bool activeState = true) {
      Chunk chunk = levelManager.level.getChunk(chunkLocation);
      if (activeState) {
        //levelManager.AddChild(this);
        //Translation = (chunkLocation.vec3 * MarchingTetsMeshGenerator.BlockSize);
        chunk.setVisible();
        chunk.unlock(Chunk.Resolution.Visible);
        // Mesh = generatedMesh;
        /*if (collider == null) {
          createCollider(Mesh.GetFaces());
        }*/
      } else {
        //clearCollider();
        levelManager.RemoveChild(this);
        chunk.setVisible(false);
        chunk.unlock(Chunk.Resolution.Visible);
      }
    }

    /// <summary>
    /// deactivate and free up this object for use again by the level controller
    /// </summary>
    public void clearAssignedChunk() {
      Mesh = null;
      generatedMesh = null;
      clearCollider();
      isMeshed = false;
      isActive = false;
      chunkLocation = default;
    }

    /// <summary>
    /// Create and set the collider
    /// </summary>
    /// <param name="mesh"></param>
    void createCollider(Vector3[] meshVerticies) {
      ConcavePolygonShape colliderShape = new ConcavePolygonShape();
      colliderShape.Data = meshVerticies;
      StaticBody colliderBody = new StaticBody();
      World.Debugger.log($"event2");
      uint ownerID = colliderBody.CreateShapeOwner(colliderBody);
      colliderBody.ShapeOwnerAddShape(ownerID, colliderShape);
      collider = colliderBody;
      AddChild(collider);
    }

    /// <summary>
    /// Remove the collider
    /// </summary>
    void clearCollider() {
      collider.QueueFree();
      collider = null;
    }
  }
}