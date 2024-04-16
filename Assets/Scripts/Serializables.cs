using System;
using System.Collections.Generic;
using UnityEngine;
using static GameTile;

public class Serializables
{
    [Serializable]
    public class SerializableLevel
    {
        public string levelName;
        public Tiles tiles = new();
    }

    [Serializable]
    public class Tiles
    {
        public List<SerializableTile> solidTiles = new();
        public List<SerializableTile> objectTiles = new();
        public List<SerializableTile> overlapTiles = new();
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
