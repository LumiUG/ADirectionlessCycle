using System;
using System.Collections.Generic;
using UnityEngine;
using static GameTile;

public class Serializables
{
    // Level as a serializable class
    [Serializable]
    public class SerializableLevel
    {
        public string levelName;
        public Tiles tiles = new();
    }

    // Collection of tiles in a level
    [Serializable]
    public class Tiles
    {
        public List<SerializableTile> solidTiles = new();
        public List<SerializableTile> objectTiles = new();
        public List<SerializableTile> overlapTiles = new();
    }

    // Similar to a GameTile, but able to serialize to json
    [Serializable]
    public class SerializableTile
    {
        public string type;
        public Directions directions;
        public Vector3Int position;

        // Tile constructor
        public SerializableTile(ObjectTypes tileType, Directions tileDirections, Vector3Int tilePosition)
        {
            type = tileType.ToString();
            directions = tileDirections;
            position = tilePosition;
        }
    }
}
