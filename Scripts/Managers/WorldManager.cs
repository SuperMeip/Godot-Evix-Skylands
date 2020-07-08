using Godot;
using Evix.Terrain.Collections;

namespace Evix.Managers {

	public class WorldManager : Node {
		// Declare member variables here. Examples:
		// private int a = 2;
		// private string b = "text";

		/// <summary>
		/// The current focus of the active level
		/// </summary>
		[Export]
		readonly NodePath startingFocus;

		/// <summary>
		/// The Level manager to use for the active level
		/// </summary>
		[Export]
		readonly NodePath levelManagerNode;

		// Called when the node enters the scene tree for the first time.
		public override void _Ready() {
			FocusManager starterFocus = GetNode<FocusManager>(startingFocus);
			//LevelManager levelManager = GetNode<LevelManager>(levelManagerNode);
			LevelTerrainManager levelManager = GetNode<LevelTerrainManager>(levelManagerNode);
			World.SetActiveLevel(new Level((1000, 20, 1000)));

			starterFocus.setPosition((World.Current.activeLevel.chunkBounds) / 2 * Chunk.Diameter);
			levelManager.initializeFor(World.Current.activeLevel, starterFocus);
		}

		//  // Called every frame. 'delta' is the elapsed time since the previous frame.
		//  public override void _Process(float delta)
		//  {
		//      
		//  }
	}

}
