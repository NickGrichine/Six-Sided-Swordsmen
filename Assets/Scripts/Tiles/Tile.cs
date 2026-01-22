using UnityEngine;

public class Tile: MonoBehaviour
{
    
    public TileType type;

    public boolean passable; // Checks whether if the tile is water / unmovable

    public int moveCost; // Cost to travel to current tile

    public list[Tile] adjacentTiles;

    void start();

    public distanceTo(Tile tile)
    {
        
    }

}