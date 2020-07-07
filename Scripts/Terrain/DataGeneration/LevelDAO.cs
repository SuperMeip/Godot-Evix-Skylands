using Evix.Terrain.Collections;
using Evix.Terrain.Resolution;
using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.RegularExpressions;

namespace Evix.Terrain.DataGeneration {

  /// <summary>
  /// Class for accessing level data from files
  /// @todo, this should become an object made in the Level constructor, with file names pre-generated that's grabbed by the apeture from the level {get}
  /// </summary>
  public static class LevelDAO {

    /// <summary>
    /// A regex we can use to remove illegal file name chars.
    /// @TODO: move this to Level's constructor so we only need to construct the level's file safe name once.
    /// </summary>
    static readonly Regex IllegalCharactersForFileName = new Regex(
      string.Format("[{0}]",
      Regex.Escape(new string(Path.GetInvalidFileNameChars()))),
      RegexOptions.Compiled
    );

    #region FileDataManipulation

    /// <summary>
    /// Check if the chunk save file exists
    /// </summary>
    /// <param name=""></param>
    /// <param name="level"></param>
    /// <returns></returns>
    public static bool ChunkFileExists(Coordinate chunkID, Level level) {
      return File.Exists(GetChunkDataFileName(chunkID, level.name));
    }

    /// <summary>
    /// Get the voxeldata for a chunk location from file
    /// </summary>
    /// <returns>False if the chunk is empty</returns>
    static bool GetDataForChunkFromFile(Coordinate chunkId, string levelName, out ChunkSaveData chunkData) {
      chunkData = default;
      IFormatter formatter = new BinaryFormatter();
      Stream readStream = new FileStream(
        GetChunkDataFileName(chunkId, levelName),
        FileMode.Open,
        FileAccess.Read,
        FileShare.Read
      ) {
        Position = 0
      };
      var fileData = formatter.Deserialize(readStream);
      if (fileData is ChunkSaveData) {
        chunkData = (ChunkSaveData)fileData;
        readStream.Close();
        return true;
      }

      readStream.Close();
      return false;
    }

    /// <summary>
    /// Only to be used by jobs
    /// Save a chunk to file
    /// </summary>
    /// <param name="chunkLocation"></param>
    static public void SaveChunkDataToFile(Coordinate chunkId, string levelName, ChunkSaveData chunkData) {
      IFormatter formatter = new BinaryFormatter();
      CheckForSaveDirectory(levelName);
      Stream stream = new FileStream(GetChunkDataFileName(chunkId, levelName), FileMode.Create, FileAccess.Write, FileShare.None);
      formatter.Serialize(stream, chunkData);
      stream.Close();
    }

    /// <summary>
    /// Get the file name a chunk is saved to based on it's location
    /// </summary>
    /// <param name="chunkLocation">the location of the chunk</param>
    /// <returns></returns>
    static string GetChunkDataFileName(Coordinate chunkID, string levelName) {
      return $"{GetChunkDataFolder(levelName)}{chunkID}.evxch";
    }

    /// <summary>
    /// Get the name of the folder we use to store chunk data for this level
    /// </summary>
    /// <param name="levelName"></param>
    /// <returns></returns>
    static string GetChunkDataFolder(string levelName) {
      return $"{GetLevelFolder(levelName)}chunkdata/";
    }

    /// <summary>
    /// Get the save directory for the given level
    /// </summary>
    /// <param name="level"></param>
    /// <returns></returns>
    static string GetLevelFolder(string levelName) {
      return $"{World.GameSaveFilePath}/leveldata/{IllegalCharactersForFileName.Replace(levelName, "")}/";
    }

    /// <summary>
    /// Create the save file directory if it doesn't exist for the level yet
    /// @todo: put this in the save function
    /// </summary>
    static void CheckForSaveDirectory(string levelName) {
      if (Directory.Exists(GetChunkDataFolder(levelName))) {
        return;
      }

      Directory.CreateDirectory(GetChunkDataFolder(levelName));
    }

    /// <summary>
    /// A serializable bit of chunk data
    /// </summary>
    [Serializable]
    public struct ChunkSaveData : ISerializable {
      /// <summary>
      /// The voxels to save
      /// </summary>
      public byte[] voxels {
        get;
        private set;
      }

      /// <summary>
      /// the solid voxel count
      /// </summary>
      public int solidVoxelCount {
        get;
        private set;
      }

      /// <summary>
      /// Make a new set of save data from a job
      /// </summary>
      /// <param name="voxels"></param>
      /// <param name="solidVoxelCount"></param>
      public ChunkSaveData(byte[] voxels, int solidVoxelCount) {
        this.voxels = solidVoxelCount == 0 ? null : voxels;
        this.solidVoxelCount = solidVoxelCount;
      }

      /// <summary>
      /// deserialize
      /// </summary>
      /// <param name="info"></param>
      /// <param name="context"></param>
      public ChunkSaveData(SerializationInfo info, StreamingContext context) {
        voxels = (byte[])info.GetValue("voxels", typeof(byte[]));
        solidVoxelCount = (int)info.GetValue("voxelCount", typeof(int));
      }

      /// <summary>
      /// serizalize
      /// </summary>
      /// <param name="info"></param>
      /// <param name="context"></param>
      public void GetObjectData(SerializationInfo info, StreamingContext context) {
        info.AddValue("voxels", voxels, typeof(byte[]));
        info.AddValue("voxelCount", solidVoxelCount, typeof(int));
      }
    }

    #endregion

    #region Jobs

    /// <summary>
    /// Get a file load job
    /// </summary>
    /// <param name="chunkID"></param>
    /// <param name="level"></param>
    /// <returns></returns>
    public static LoadChunkDataFromFileJob GetFileLoadJob(ChunkResolutionAperture.Adjustment adjustment, Level level) {
      return new LoadChunkDataFromFileJob(adjustment, level);
    }

    /// <summary>
    /// Job for loading chunk voxel data from a file
    /// </summary>
    public struct LoadChunkDataFromFileJob : ChunkResolutionAperture.IAdjustmentJob {

      /// <summary>
      /// The adjustment this job is running on
      /// </summary>
      public ChunkResolutionAperture.Adjustment adjustment {
        get;
      }

      /// <summary>
      /// The level name, used for finding the file
      /// </summary>
      readonly Level level;

      public LoadChunkDataFromFileJob(ChunkResolutionAperture.Adjustment adjustment, Level level) {
        this.adjustment = adjustment;
        this.level = level;
      }

      /// <summary>
      /// Load the chunk data from file
      /// </summary>
      public void doWork() {
        if (GetDataForChunkFromFile(adjustment.chunkID, level.name, out ChunkSaveData chunkData)) {
          Chunk chunk = level.getChunk(adjustment.chunkID);
          chunk.setVoxelData(chunkData.voxels, chunkData.solidVoxelCount);
          chunk.unlock(adjustment.resolution);
        }
      }
    }

    /// <summary>
    /// Job for loading chunk voxel data from a file
    /// </summary>
    public struct SaveChunkDataToFileJob : ChunkResolutionAperture.IAdjustmentJob {

      /// <summary>
      /// The adjustment this job is running on
      /// </summary>
      public ChunkResolutionAperture.Adjustment adjustment {
        get;
      }

      /// <summary>
      /// The level name, used for finding the file
      /// </summary>
      readonly Level level;

      public SaveChunkDataToFileJob(ChunkResolutionAperture.Adjustment adjustment, Level level) {
        this.adjustment = adjustment;
        this.level = level;
      }

      /// <summary>
      /// Load the chunk data from file
      /// </summary>
      public void doWork() {
        Chunk chunk = level.getChunk(adjustment.chunkID);
        SaveChunkDataToFile(adjustment.chunkID, level.name, chunk.emptyChunkData(adjustment));
        chunk.unlock(adjustment.resolution);
      }
    }

    #endregion
  }
}
