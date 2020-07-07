using Evix.Terrain.Collections;
using Evix.Terrain.Resolution;
using System;
using System.Collections.Generic;

namespace Evix {
	public class Level {

		/// <summary>
		/// The overall bounds of the level, max x y and z
		/// </summary>
		public readonly Coordinate chunkBounds;

		/// <summary>
		/// The seed the level uses for generation
		/// </summary>
		public readonly int seed;

		/// <summary>
		/// The name of the level
		/// </summary>
		public string name = "No Man's Land";

		/// <summary>
		/// The collection of chunks
		/// </summary>
		readonly Dictionary<Coordinate, Chunk> chunks
			= new Dictionary<Coordinate, Chunk>();

		/// <summary>
		/// The focuses in this level and the lense to use for each of them
		/// </summary>
		readonly Dictionary<ILevelFocus, IFocusLens> focalLenses
			= new Dictionary<ILevelFocus, IFocusLens>();

		/// <summary>
		/// The focuses in this level indexed by ID
		/// </summary>
		readonly Dictionary<int, ILevelFocus> fociByID
			= new Dictionary<int, ILevelFocus>();

		/// <summary>
		/// The current highest assigned focus id.
		/// </summary>
		int currentMaxFocusID = 0;

	#region Constructors

	/// <summary>
	/// Create a new level of the given size that uses the given apetures.
	/// </summary>
	/// <param name="chunkBounds"></param>
	/// <param name="apeturesByPriority"></param>
	public Level(Coordinate chunkBounds) {
			seed = 1234;
			this.chunkBounds = chunkBounds;
		}

	#endregion

	#region Access Functions

	/// <summary>
	/// Get a terrain voxel based on it's world location
	/// </summary>
	/// <param name="x"></param>
	/// <param name="y"></param>
	/// <param name="z"></param>
	/// <returns></returns>
	public byte this[int x, int y, int z] {
			get {
				if (chunks.TryGetValue(Chunk.IDFromWorldLocation(x, y, z), out Chunk chunk)) {
					return chunk[x & 0xF, y & 0xF, z & 0xF];
				}

				return 0;
			}

			set {
				if (chunks.TryGetValue(Chunk.IDFromWorldLocation(x, y, z), out Chunk chunk)) {
					chunk[x & 0xF, y & 0xF, z & 0xF] = value;
				} else {
					World.Debugger.logError($"Tried to set a value in non existent chunk {x}, {y}, {z}");
				}
			}
		}

		/// <summary>
		/// Get a terrain voxel based on it's worldloacation
		/// </summary>
		/// <param name="worldLocation"></param>
		/// <returns></returns>
		public byte this[Coordinate worldLocation] {
			get => this[worldLocation.x, worldLocation.y, worldLocation.z];
			set {
				this[worldLocation.x, worldLocation.y, worldLocation.z] = value;
			}
		}

		/// <summary>
		/// Get the chunk at the given location.
		/// This creates a new chunk if we don't have one
		/// </summary>
		/// <param name="chunkID"></param>
		/// <returns></returns>
		public Chunk getChunk(Coordinate chunkID) {
			if (chunks.TryGetValue(chunkID, out Chunk chunk)) {
				return chunk;
			} else {
				Chunk newChunk = new Chunk();
				chunks.Add(chunkID, newChunk);
				return newChunk;
			}
		}

		/// <summary>
		/// Get the voxel at the given world coordinate
		/// </summary>
		/// <param name="worldLocation"></param>
		/// <returns></returns>
		public byte getTerrainVoxel(Coordinate worldLocation) {
			return this[worldLocation.x, worldLocation.y, worldLocation.z];
		}

		/// <summary>
		/// Get the id for the given level focus+
		/// </summary>
		/// <param name="focus"></param>
		/// <returns></returns>
		public int getFocusID(ILevelFocus focus) {
			foreach (KeyValuePair<ILevelFocus, IFocusLens> storedFocus in focalLenses) {
				if (storedFocus.Key == focus) {
					return storedFocus.Key.id;
				}
			}

			return 0;
		}

		/// <summary>
		/// Add a focus to be managed by this level
		/// @TODO: the player should be pased in eventually too, and we'll use there prefs to size the lens
		/// </summary>
		/// <param name="newFocus"></param>
		public IFocusLens addPlayerFocus(ILevelFocus newFocus) {
			// register the focus to the level
			newFocus.registerTo(this, ++currentMaxFocusID);
			fociByID[newFocus.id] = newFocus;
			// create a new lens for the focus
			IFocusLens lens = new PlayerLens(newFocus, this, 5);
			// add the lens and focus to the level storage
			focalLenses.Add(newFocus, lens);

			return lens;
		}

	#endregion

	#region Utility Functions

	/// <summary>
	/// Do something for each focus and lens managing it
	/// </summary>
	/// <param name="action"></param>
	public void forEachFocalLens(Action<IFocusLens, ILevelFocus> action) {
			foreach (KeyValuePair<ILevelFocus, IFocusLens> focalLens in focalLenses) {
				action(focalLens.Value, focalLens.Key);
			}
		}

		/// <summary>
		/// Get the priority of the given adjustment for this level as a float value
		/// </summary>
		/// <param name="adjustment"></param>
		/// <returns></returns>
		public float getPriorityForAdjustment(ChunkResolutionAperture.Adjustment adjustment) {
			ILevelFocus focus = fociByID[adjustment.focusID];
			return focalLenses[focus].getAdjustmentPriority(adjustment, focus);
		}

	#endregion
  }
}
