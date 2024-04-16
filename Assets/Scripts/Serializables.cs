using System;
using UnityEngine;
using static GameTile;

public class Serializables
{
    [Serializable]
    public class SerializableLevel
    {
        public string levelName;
        public Tiles tiles;

        // Constructor
        public SerializableLevel(int solidSize, int objectSize, int overlapSize)
        {
            tiles = new Tiles(solidSize, objectSize, overlapSize);
        }
    }

    [Serializable]
    public class Tiles
    {
        public SerializableTile[] solidTiles;
        public SerializableTile[] objectTiles;
        public SerializableTile[] overlapTiles;

        // Constructor
        public Tiles(int solidSize, int objectSize, int overlapSize)
        {
            // Debug.LogError($"{solidSize}, {objectSize}, {overlapSize}");
            solidTiles = new SerializableTile[solidSize];
            objectTiles = new SerializableTile[objectSize];
            overlapTiles = new SerializableTile[overlapSize];
        }
    }

    [Serializable]
    public class SerializableTile
    {
        public string type;
        public BootlegDirections directions;
        public Vector3 position;

        // Constructor
        public SerializableTile(ObjectTypes tileType, Directions tileDirections, Vector3Int tilePosition)
        {
            type = tileType.ToString();
            directions = new(tileDirections);
            position = tilePosition;
        }
    }

    [Serializable]
    public class BootlegDirections
    {
        public bool pushable;
        public bool up;
        public bool down;
        public bool left;
        public bool right;

        // Constructor
        public BootlegDirections(Directions directions)
        {
            pushable = directions.pushable;
            up = directions.up;
            down = directions.down;
            left = directions.left;
            right = directions.right;
        }
    }
}
