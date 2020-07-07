using System.Linq;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Godot;

namespace Evix {

  #region Coordinate

  /// <summary>
  /// A block position in a level
  /// </summary>
  [System.Serializable]
  public struct Coordinate : IComparable<Coordinate>, IEquatable<Coordinate>, ISerializable {

    /// <summary>
    /// The coordinate for 0, 0, 0
    /// </summary>
    public static readonly Coordinate Zero = (0, 0, 0);

    // The coordinate values
    /// <summary>
    /// east west
    /// </summary>
    public int x;

    /// <summary>
    /// up down
    /// </summary>
    public int y;

    /// <summary>
    /// north south
    /// </summary>
    public int z;

    #region Constructors

    /// <summary>
    /// Create a coordinate with one value for all 3
    /// </summary>
    /// <param name="xyz"></param>
    public Coordinate(int xyz) {
      x = y = z = xyz;
    }

    /// <summary>
    /// Create a 3d coordinate
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="z"></param>
    public Coordinate(int x, int y, int z) {
      this.x = x;
      this.y = y;
      this.z = z;
    }

    /// see #serialization for more
    #endregion

    #region Implicit Conversions

    /// <summary>
    /// Turn a set of coordinates into a coordinate.
    /// </summary>
    /// <param name="coordinates"></param>
    public static implicit operator Coordinate((int, int, int) coordinates) {
      return new Coordinate(coordinates.Item1, coordinates.Item2, coordinates.Item3);
    }

    /// <summary>
    /// Turn a vector3 into a coordinate.
    /// </summary>
    /// <param name="coordinates"></param>
    public static implicit operator Coordinate(Vector3 coordinate) {
      return new Coordinate((int)coordinate.x, (int)coordinate.y, (int)coordinate.z);
    }

    #endregion

    #region Operator Overrides

    public static Coordinate operator +(Coordinate a, Coordinate b) {
      return (
        a.x + b.x,
        a.y + b.y,
        a.z + b.z
      );
    }

    public static Coordinate operator -(Coordinate a, Coordinate b) {
      return (
        a.x - b.x,
        a.y - b.y,
        a.z - b.z
      );
    }

    public static Coordinate operator +(Coordinate a, int b) {
      return (
        a.x + b,
        a.y + b,
        a.z + b
      );
    }

    public static Coordinate operator *(Coordinate a, int b) {
      return (
        a.x * b,
        a.y * b,
        a.z * b
      );
    }

    public static Coordinate operator /(Coordinate a, int b) {
      return (
        a.x / b,
        a.y / b,
        a.z / b
      );
    }

    public static Coordinate operator *(Coordinate a, Coordinate b) {
      return (
        a.x * b.x,
        a.y * b.y,
        a.z * b.z
      );
    }

    public static Coordinate operator -(Coordinate a, int b) {
      return a + (-b);
    }

    public static bool operator ==(Coordinate a, Coordinate b) {
      return a.Equals(b);
    }

    public static bool operator !=(Coordinate a, Coordinate b) {
      return !a.Equals(b);
    }

    #endregion

    #region Get and Set

    /// <summary>
    /// This as a vector 3
    /// </summary>
    public Vector3 vec3 {
      get {
        var _vec3 = new Vector3(x, y, z);
        return _vec3;
      }
    }

    /// <summary>
    /// shortcut for geting just the 2D X and Z of a coordinate
    /// </summary>
    public Coordinate xz {
      get => (x, 0, z);
    }

    /// <summary>
    /// Replace the x value, chainable.
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    public Coordinate replaceX(int x) {
      return (x, y, z);
    }

    /// <summary>
    /// Replace the x value, chainable.
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    public Coordinate replaceY(int y) {
      return (x, y, z);
    }

    /// <summary>
    /// Replace the x value, chainable.
    /// </summary>
    /// <param name="x"></param>
    /// <returns></returns>
    public Coordinate replaceZ(int z) {
      return (x, y, z);
    }

    #endregion

    #region String Functions

    /// <summary>
    /// Get a file saveable string name for this coordinate
    /// </summary>
    /// <returns></returns>
    public string ToSaveString() {
      return $"{x}-{y}-{z}";
    }

    /// <summary>
    /// Tostring override
    /// </summary>
    /// <returns></returns>
    public override string ToString() {
      return "{" + x.ToString() + ", " + y.ToString() + ", " + z.ToString() + "}";
    }

    #endregion

    #region Equality Functions

    /// <summary>
    /// Get hash code override for being used as chunk ID
    /// </summary>
    /// <returns></returns>
    public override int GetHashCode() {
      unchecked {
        var hashCode = x;
        hashCode = (hashCode * 397) ^ y;
        hashCode = (hashCode * 397) ^ z;
        return hashCode;
      }
    }

    /// <summary>
    /// Comparison overrider
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public int CompareTo(Coordinate other) {
      return other.isBeyond(this)
        ? 1
        : other.Equals(this)
          ? 0
          : -1;
    }

    /// <summary>
    /// equals overrider
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public bool Equals(Coordinate other) {
      return other.x == x
        && other.y == y
        && other.z == z;
    }

    /// <summary>
    /// Override general equals
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public override bool Equals(object obj) {
      if (obj is null) return false;
      return obj is Coordinate other && Equals(other);
    }

    #endregion

    #region Serialization

    /// <summary>
    /// Serialization override
    /// </summary>
    /// <param name="info"></param>
    /// <param name="context"></param>
    public void GetObjectData(SerializationInfo info, StreamingContext context) {
      info.AddValue("x", x, typeof(int));
      info.AddValue("y", y, typeof(int));
      info.AddValue("z", z, typeof(int));
    }

    /// <summary>
    /// Deserialize a coordinate
    /// </summary>
    /// <param name="info"></param>
    /// <param name="context"></param>
    public Coordinate(SerializationInfo info, StreamingContext context) {
      x = (int)info.GetValue("x", typeof(int));
      y = (int)info.GetValue("y", typeof(int));
      z = (int)info.GetValue("z", typeof(int));
    }

    #endregion

    #region Utility Functions

    /// <summary>
    /// Get the coordinate one over in another direction.
    /// </summary>
    /// <param name="direction">The direction to move in</param>
    /// <param name="magnitude">The distance to move</param>
    /// <returns>The coordinate one over in the requested direction</returns>
    public Coordinate go(Directions.Direction direction, int magnitude = 1) {
      if (direction.Equals(Directions.North)) {
        return (x, y, z + magnitude);
      }
      if (direction.Equals(Directions.South)) {
        return (x, y, z - magnitude);
      }
      if (direction.Equals(Directions.East)) {
        return (x + magnitude, y, z);
      }
      if (direction.Equals(Directions.West)) {
        return (x - magnitude, y, z);
      }
      if (direction.Equals(Directions.Below)) {
        return (x, y - magnitude, z);
      }
      if (direction.Equals(Directions.Above)) {
        return (x, y + magnitude, z);
      }

      return this;
    }

    /// <summary>
    /// Get the distance between this and otherPoint
    /// </summary>
    /// <param name="otherPoint"></param>
    /// <returns></returns>
    public float distance(Coordinate otherPoint) {
      float a = (float)Math.Pow(x - otherPoint.x, 2);
      float b = (float)Math.Pow(y - otherPoint.y, 2);
      float c = (float)Math.Pow(z - otherPoint.z, 2);

      float d = (float)Math.Sqrt(
        a + b + c
      );

      return d;
    }

    /// <summary>
    /// Flatten the values using the coordinate flatten formula
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="z"></param>
    /// <param name="diameter"></param>
    /// <returns></returns>
    public static int Flatten(int x, int y, int z, int diameter) {
      return x * diameter * diameter + y * diameter + z;
    }

    /// <summary>
    /// Flatten to an index for a 1D array of the given diameter
    /// </summary>
    /// <param name="diameter"></param>
    /// <returns></returns>
    public int flatten(int diameter) {
      return Flatten(x, y, z, diameter);
    }

    /// <summary>
    /// Get the distance between this and otherPoint with more weight on y
    /// </summary>
    /// <param name="otherPoint"></param>
    /// <returns></returns>
    public float distanceYFlattened(Coordinate otherPoint, float yMultiplier) {
      float a = (float)Math.Pow(x - otherPoint.x, 2);
      float b = (float)Math.Pow(y - otherPoint.y, 2) * yMultiplier;
      float c = (float)Math.Pow(z - otherPoint.z, 2);

      float d = (float)Math.Sqrt(
        a + b + c
      );

      return d;
    }

    /// <summary>
    /// Checks if this coordinate is within a bounds coodinate (exclusive)
    /// </summary>
    public bool isWithin(Coordinate bounds) {
      return x < bounds.x
        && y < bounds.y
        && z < bounds.z;
    }


    /// <summary>
    /// Checks if this coordinate is within a set, dictated by boundaries. (inclusive, and exclusive)
    /// </summary>
    /// <param name="start">The starting location to check if the point is within, inclusive</param>
    /// <param name="bounds">The outer boundary to check for point inclusion. Exclusive</param>
    /// <returns></returns>
    public bool isWithin(Coordinate start, Coordinate bounds) {
      return isWithin(bounds) && isBeyond(start);
    }


    /// <summary>
    /// Checks if this coordinate is within a set, dictated by boundaries. (inclusive, and exclusive)
    /// </summary>
    /// <param name="bounds">The inner [0](inclusive) and outer [1](exclusive) boundary to check for point inclusion</param>
    /// <returns></returns>
    public bool isWithin(Coordinate[] bounds) {
      if (bounds != null && bounds.Length == 2) {
        return isWithin(bounds[1]) && isBeyond(bounds[0]);
      } else throw new ArgumentOutOfRangeException("Coordinate.isWithin must take an array of size 2.");
    }

    /// <summary>
    /// Checks if this coordinate is greater than a lower bounds coordinate (Inclusive)
    /// </summary>
    public bool isBeyond(Coordinate bounds) {
      return x >= bounds.x
        && y >= bounds.y
        && z >= bounds.z;
    }

    /// <summary>
    /// preform the acton on all coordinates between this one and the end coordinate
    /// </summary>
    /// <param name="end">The final point to run on, exclusive</param>
    /// <param name="action">the function to run on each point</param>
    /// <param name="step">the value by which the coordinate values are incrimented</param>
    public void until(Coordinate end, Action<Coordinate> action, int step = 1) {
      Coordinate current = (x, y, z);
      for (current.x = x; current.x < end.x; current.x += step) {
        for (current.y = y; current.y < end.y; current.y += step) {
          for (current.z = z; current.z < end.z; current.z += step) {
            action(current);
          }
        }
      }
    }

    /// <summary>
    /// preform the acton on all coordinates between this one and the end coordinate
    /// </summary>
    /// <param name="end">The final point to run on, exclusive</param>
    /// <param name="action">the function to run on each point</param>
    /// <param name="step">the value by which the coordinate values are incrimented</param>
    public void until(Coordinate end, Func<Coordinate, bool> action, int step = 1) {
      Coordinate current = (x, y, z);
      for (current.x = x; current.x < end.x; current.x += step) {
        for (current.y = y; current.y < end.y; current.y += step) {
          for (current.z = z; current.z < end.z; current.z += step) {
            if (!action(current)) {
              return;
            }
          }
        }
      }
    }

    /// <summary>
    /// Get all the points within two sets of bounds
    /// </summary>
    /// <param name="westBottomSouthBound"> the lesser bound, -,-,- (inclusice)</param>
    /// <param name="eastTopNorthBound">the greater bound, +,+,+ (exclusive)</param>
    /// <returns>All points between these bounds</returns>
    public static Coordinate[] GetAllPointsBetween(Coordinate westBottomSouthBound, Coordinate eastTopNorthBound) {
      List<Coordinate> points = new List<Coordinate>();
      westBottomSouthBound.until(eastTopNorthBound, (coordinate) => {
        points.Add(coordinate);
      });

      return points.ToArray();
    }

    /// <summary>
    /// Get all points in one set of bounds but not the other
    /// </summary>
    /// <param name="boundsA">2 coordinates dictating a rectangular set of bounds (A)</param>
    /// <param name="boundsB">2 coordinates dictating a rectangular set of bounds (B)</param>
    /// <returns> All of the points that are in A but not B </returns>
    public static Coordinate[] GetPointDiff(Coordinate[] boundsA, Coordinate[] boundsB) {
      List<Coordinate> diffPoints = new List<Coordinate>();
      // For all points within bounds set A
      boundsA[0].until(boundsA[1], coordinate => {
        // check if that point is in bounds set B
        if (!coordinate.isWithin(boundsB)) {
          diffPoints.Add(coordinate);
        }
      });

      return diffPoints.ToArray();
    }

    /// <summary>
    /// Get the octant the given point would be in assuming this is the center.
    /// </summary>
    /// <param name="otherPoint"></param>
    /// <returns></returns>
    public Octants.Octant octantToDirectionOf(Coordinate otherPoint) {
      return Octants.Octant.Get(
        otherPoint.x >= x,
        otherPoint.y >= y,
        otherPoint.z >= z
      );
    }

    #endregion
  }

  #region Utility Extention Functions

  public static class CoordinateCollectionExtentions {

    /// <summary>
    /// Get all points in one set of bounds but not the other
    /// </summary>
    /// <param name="boundsA">2 coordinates dictating a rectangular set of bounds (A)</param>
    /// <param name="boundsToExclude">2 coordinates dictating a rectangular set of bounds (B)</param>
    /// <returns> All of the points that are in A but not B </returns>
    public static void forEachPointNotWithin(this Coordinate[] bounds, Coordinate[] boundsToExclude, Action<Coordinate> action) {
      List<Coordinate> diffPoints = new List<Coordinate>();
      // For all points within bounds set A
      bounds[0].until(bounds[1], coordinate => {
        // check if that point is in bounds set B
        if (!coordinate.isWithin(boundsToExclude)) {
          action(coordinate);
        }
      });
    }
  }

  #endregion

  #endregion

  #region Direction Constants

  /// <summary>
  /// Direction constants
  /// </summary>
  public static class Directions {

    /// <summary>
    /// A valid direction
    /// </summary>
    public class Direction : IEquatable<Direction> {

      /// <summary>
      /// The id of the direction
      /// </summary>
      public int Value {
        get;
        private set;
      }

      /// <summary>
      /// The name of this direction
      /// </summary>
      public string Name {
        get;
        private set;
      }

      /// <summary>
      /// The x y z offset of this direction from the origin
      /// </summary>
      public Coordinate Offset {
        get => Offsets[Value];
      }

      /// <summary>
      /// Get the oposite of this direction
      /// </summary>
      /// <param name="direction"></param>
      /// <returns></returns>
      public Direction Reverse {
        get {
          if (Equals(North)) {
            return South;
          }
          if (Equals(South)) {
            return North;
          }
          if (Equals(East)) {
            return West;
          }
          if (Equals(West)) {
            return East;
          }
          if (Equals(Below)) {
            return Above;
          }

          return Below;
        }
      }

      internal Direction(int value, string name) {
        Value = value;
        Name = name;
      }

      /// <summary>
      /// Get the 4 octants for a given direction
      /// </summary>
      /// <returns></returns>
      public Octants.Octant[] getOctants() {
        if (Equals(North)) {
          return Octants.North;
        }
        if (Equals(South)) {
          return Octants.All.Except(Octants.North).ToArray();
        }
        if (Equals(East)) {
          return Octants.East;
        }
        if (Equals(West)) {
          return Octants.All.Except(Octants.East).ToArray();
        }
        if (Equals(Below)) {
          return Octants.All.Except(Octants.Top).ToArray();
        }

        return Octants.Top;
      }

      /// <summary>
      /// To string
      /// </summary>
      /// <returns></returns>
      public override string ToString() {
        return Name;
      }

      public override int GetHashCode() {
        return Value;
      }

      /// <summary>
      /// Equatable
      /// </summary>
      /// <param name="other"></param>
      /// <returns></returns>
      public bool Equals(Direction other) {
        return other.Value == Value;
      }

      /// <summary>
      /// Override equals
      /// </summary>
      /// <param name="obj"></param>
      /// <returns></returns>
      public override bool Equals(object obj) {
        return (obj != null)
          && !GetType().Equals(obj.GetType())
          && ((Direction)obj).Value == Value;
      }
    }

    /// <summary>
    /// Z+
    /// </summary>
    public static Direction North = new Direction(0, "North");

    /// <summary>
    /// X+
    /// </summary>
    public static Direction East = new Direction(1, "East");

    /// <summary>
    /// Z-
    /// </summary>
    public static Direction South = new Direction(2, "South");

    /// <summary>
    /// X-
    /// </summary>
    public static Direction West = new Direction(3, "West");

    /// <summary>
    /// Y+
    /// </summary>
    public static Direction Above = new Direction(4, "Above");

    /// <summary>
    /// Y-
    /// </summary>
    public static Direction Below = new Direction(5, "Below");

    /// <summary>
    /// All the directions in order
    /// </summary>
    public static Direction[] All = new Direction[6] {
      North,
      East,
      South,
      West,
      Above,
      Below
    };

    /// <summary>
    /// The cardinal directions. Non Y related
    /// </summary>
    public static Direction[] Cardinal = new Direction[4] {
      North,
      East,
      South,
      West
    };

    /// <summary>
    /// The coordinate directional offsets
    /// </summary>
    public static Coordinate[] Offsets = new Coordinate[6] {
      (0,0,1),
      (1,0,0),
      (0,0,-1),
      (-1, 0, 0),
      (0, 1, 0),
      (0, -1, 0)
    };
  }

  #endregion

  #region Octant Constants

  /// <summary>
  /// Valid octant constants
  /// </summary>
  public static class Octants {

    /// <summary>
    /// One of 8 cubes making up a larger cube
    /// </summary>
    public class Octant {

      /// <summary>
      /// The id value of the octant
      /// </summary>
      public int Value { get; private set; }

      /// <summary>
      /// if this is an eastern octant
      /// </summary>
      /// <returns></returns>
      public bool IsEastern {
        get {
          return Value == EastBottomSouth.Value
            || Value == EastBottomNorth.Value
            || Value == EastTopSouth.Value
            || Value == EastTopNorth.Value;
        }
      }

      /// <summary>
      /// if this is a nothern octant
      /// </summary>
      /// <returns></returns>
      public bool IsNorthern {
        get {
          return Value == EastTopNorth.Value
          || Value == EastBottomNorth.Value
          || Value == WestTopNorth.Value
          || Value == WestBottomNorth.Value;
        }
      }

      /// <summary>
      /// if this is an upper/top octant
      /// </summary>
      /// <returns></returns>
      public bool IsUpper {
        get {
          return Value == EastTopNorth.Value
          || Value == EastTopSouth.Value
          || Value == WestTopNorth.Value
          || Value == WestTopSouth.Value;
        }
      }

      /// <summary>
      /// Get the opposite/reversed octant around the origin
      /// </summary>
      public Octant Reverse {
        get {
          switch (Value) {
            case 0: return EastTopNorth;
            case 1: return WestTopNorth;
            case 2: return WestTopSouth;
            case 3: return EastTopSouth;
            case 4: return EastBottomNorth;
            case 5: return WestBottomNorth;
            case 6: return WestBottomSouth;
            case 7: return EastBottomSouth;
            default:
              return EastTopNorth;
          }
        }
      }

      /// <summary>
      /// Get the coordinate plane offset from the 000 vertex of the cube of this octant
      /// </summary>
      public Coordinate Offset {
        get {
          switch (Value) {
            case 0: return (0, 0, 0);
            case 1: return (1, 0, 0);
            case 2: return (1, 0, 1);
            case 3: return (0, 0, 1);
            case 4: return (0, 1, 0);
            case 5: return (1, 1, 0);
            case 6: return (1, 1, 1);
            case 7: return (0, 1, 1);
            default:
              return (0, 0, 0);
          };
        }
      }

      internal Octant(int value) {
        Value = value;
      }

      /// <summary>
      /// Make the correct octant based on given directions.
      /// </summary>
      /// <param name="isNorthern"></param>
      /// <param name="isEastern"></param>
      /// <param name="isUpper"></param>
      /// <returns></returns>
      public static Octant Get(bool isEastern = true, bool isUpper = true, bool isNorthern = true) {
        return isNorthern
          ? isEastern
            ? isUpper
              ? EastTopNorth
              : EastBottomNorth
            : isUpper
              ? WestTopNorth
              : WestBottomNorth
          : isEastern
            ? isUpper
              ? EastTopSouth
              : EastBottomSouth
            : isUpper
              ? WestTopSouth
              : WestBottomSouth;
      }

      /// <summary>
      /// Get the octant to the direction of the current octant
      /// </summary>
      /// <param name="direction"></param>
      /// <returns>the octant to the direction, or null if it's out of the current bounds</returns>
      public Octant toThe(Directions.Direction direction) {
        if (direction.Equals(Directions.North)) {
          if (IsNorthern) {
            return null;
          } else {
            return Get(IsEastern, IsUpper, true);
          }
        }
        if (direction.Equals(Directions.South)) {
          if (!IsNorthern) {
            return null;
          } else {
            return Get(IsEastern, IsUpper, false);
          }
        }
        if (direction.Equals(Directions.East)) {
          if (IsEastern) {
            return null;
          } else {
            return Get(true, IsUpper, IsNorthern);
          }
        }
        if (direction.Equals(Directions.West)) {
          if (!IsEastern) {
            return null;
          } else {
            return Get(false, IsUpper, IsNorthern);
          }
        }
        if (direction.Equals(Directions.Above)) {
          if (IsUpper) {
            return null;
          } else {
            return Get(IsEastern, true, IsNorthern);
          }
        }
        if (direction.Equals(Directions.Below)) {
          if (!IsUpper) {
            return null;
          } else {
            return Get(IsEastern, false, IsNorthern);
          }
        }

        return null;
      }

      /// <summary>
      /// To string
      /// </summary>
      /// <returns></returns>
      public override string ToString() {
        return (IsEastern ? "East" : "West") +
          (IsUpper ? "Top" : "Bottom") +
          (IsNorthern ? "North" : "South") +
          " {" +
          (IsEastern ? "+" : "-") +
          (IsUpper ? "+" : "-") +
          (IsNorthern ? "+" : "-") +
          "}";
      }
    }

    /// <summary>
    /// X-Y-Z-
    /// </summary>
    public static Octant WestBottomSouth = new Octant(0);

    /// <summary>
    /// X+Y-Z-
    /// </summary>
    public static Octant EastBottomSouth = new Octant(/*4*/1);

    /// <summary>
    /// X+Y-Z+
    /// </summary>
    public static Octant EastBottomNorth = new Octant(/*5*/2);

    /// <summary>
    /// X-Y-Z+
    /// </summary>
    public static Octant WestBottomNorth = new Octant(/*1*/3);

    /// <summary>
    /// X-Y+Z-
    /// </summary>
    public static Octant WestTopSouth = new Octant(/*2*/4);

    /// <summary>
    /// X+Y+Z-
    /// </summary>
    public static Octant EastTopSouth = new Octant(/*6*/5);

    /// <summary>
    /// X+Y+Z+
    /// </summary>
    public static Octant EastTopNorth = new Octant(/*7*/6);

    /// <summary>
    /// X-Y+Z+
    /// </summary>
    public static Octant WestTopNorth = new Octant(/*3*/7);

    /// <summary>
    /// All of the octants in order
    /// </summary>
    public static Octant[] All = new Octant[8] {
      WestBottomSouth,
      EastBottomSouth,
      EastBottomNorth,
      WestBottomNorth,
      WestTopSouth,
      EastTopSouth,
      EastTopNorth,
      WestTopNorth
    };

    /// <summary>
    /// All eastern octants
    /// </summary>
    public static Octant[] East = new Octant[4] {
      EastBottomSouth,
      EastBottomNorth,
      EastTopSouth,
      EastTopNorth
    };

    /// <summary>
    /// all northern octants
    /// </summary>
    public static Octant[] North = new Octant[4] {
      WestBottomNorth,
      WestTopNorth,
      EastBottomNorth,
      EastTopNorth
    };

    /// <summary>
    /// all upper/top octants
    /// </summary>
    public static Octant[] Top = new Octant[4] {
      WestTopSouth,
      WestTopNorth,
      EastTopSouth,
      EastTopNorth
    };
  }

  #endregion

  #region Float Utilities

  public static class RangeUtilities {

    /// <summary>
    /// fast clamp a float to between 0 and 1
    /// </summary>
    /// <param name="value"></param>
    /// <param name="minValue"></param>
    /// <param name="maxValue"></param>
    /// <returns></returns>
    public static float clampToFloat(float value, int minValue, int maxValue) {
      return (
        (value - minValue)
        / (maxValue - minValue)
      );
    }

    /// <summary>
    /// fast clamp float to short
    /// </summary>
    /// <param name="value"></param>
    /// <param name="minFloat"></param>
    /// <param name="maxFloat"></param>
    /// <returns></returns>
    public static short clampToShort(float value, float minFloat = 0.0f, float maxFloat = 1.0f) {
      return (short)((short.MaxValue - short.MinValue)
        * ((value - minFloat) / (maxFloat - minFloat))
        + short.MinValue);
    }

    /// <summary>
    /// Clamp a value between two numbers
    /// </summary>
    /// <param name="value"></param>
    /// <param name="startingMin"></param>
    /// <param name="startingMax"></param>
    /// <param name="targetMin"></param>
    /// <param name="targetMax"></param>
    /// <returns></returns>
    public static double clamp(double value, double startingMin, double startingMax, double targetMin, double targetMax) {
      return (targetMax - targetMin)
        * ((value - startingMin) / (startingMax - startingMin))
        + targetMin;
    }

    /// <summary>
    /// Clamp the values between these numbers in a non scaling way.
    /// </summary>
    /// <param name="number"></param>
    /// <param name="min"></param>
    /// <param name="max"></param>
    /// <returns></returns>
    public static float box(this float number, float min, float max) {
      if (number < min)
        return min;
      else if (number > max)
        return max;
      else
        return number;
    }

    /// <summary>
    /// Box a float between 0 and 1
    /// </summary>
    /// <param name="number"></param>
    /// <returns></returns>
    public static float box01(this float number) {
      return box(number, 0, 1);
    }
  }

  #endregion
}