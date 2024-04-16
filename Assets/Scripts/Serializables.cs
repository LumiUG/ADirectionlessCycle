using System;
using UnityEngine;
using static GameTile;

public class Serializables
{
    [Serializable]
    public class SerializableLevel
    {
        public string levelName;
        [SerializeField] public Tiles tiles;

        // Constructor
        public SerializableLevel(int solidSize, int objectSize, int overlapSize)
        {
            tiles = new Tiles(solidSize, objectSize, overlapSize);
        }
    }

    [Serializable]
    public class Tiles
    {
        [SerializeField] public SerializableTile[] solidTiles;
        [SerializeField] public SerializableTile[] objectTiles;
        [SerializeField] public SerializableTile[] overlapTiles;

        // Constructor
        public Tiles(int solidSize, int objectSize, int overlapSize)
        {
            solidTiles = new SerializableTile[solidSize];
            objectTiles = new SerializableTile[objectSize];
            overlapTiles = new SerializableTile[overlapSize];
        }
    }

    [Serializable]
    public class SerializableTile
    {
        public string type;
        [SerializeField] public BootlegDirections directions;
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
