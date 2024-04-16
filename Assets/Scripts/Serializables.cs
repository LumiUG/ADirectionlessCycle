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
        public Directions directions;
        public Vector3Int position;

        // Constructor
        public SerializableTile(ObjectTypes tileType, Directions tileDirections, Vector3Int tilePosition)
        {
            type = tileType.ToString();
            directions = tileDirections;
            position = tilePosition;
        }
    }
}
