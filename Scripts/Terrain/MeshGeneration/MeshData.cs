﻿using Godot;

namespace Evix.Terrain.MeshGeneration {

  /// <summary>
  /// Data for a chunks mesh
  /// </summary>
  public class ChunkMeshData {

    /// <summary>
    /// the compiled arraymesh
    /// </summary>
    public ArrayMesh arrayMesh {
      get;
    }

    /// <summary>
    /// the verts
    /// </summary>
    public Vector3[] verticies {
      get;
    }

    /// <summary>
    /// Make a new set of mesh data
    /// </summary>
    /// <param name="arrayMesh"></param>
    /// <param name="verticies"></param>
    public ChunkMeshData(ArrayMesh arrayMesh, Vector3[] verticies) {
      this.arrayMesh = arrayMesh;
      this.verticies = verticies;
    }
  }
}
