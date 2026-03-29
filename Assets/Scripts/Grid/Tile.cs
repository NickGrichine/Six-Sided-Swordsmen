using UnityEngine;
using System.Collections.Generic;

public class Tile : MonoBehaviour  
{
    // Null-object pattern
    private static Tile _nullTile;
    public static Tile NullTile // Creates hidden placeholder tile 
    {
        get
        {
            if (_nullTile != null) return _nullTile;

            var go = new GameObject("[NullTile]");
            go.hideFlags = HideFlags.HideAndDontSave;
            DontDestroyOnLoad(go);

            _nullTile = go.AddComponent<Tile>();
            _nullTile.enabled = false; // never runs Update; Start won't run when disabled
            _nullTile.passable = false;
            _nullTile.moveCost = int.MaxValue;
            _nullTile.axialPos = new Vector2Int(int.MinValue, int.MinValue);

            return _nullTile;
        }
    }

    public bool IsNull => this == NullTile;
    public TileType type;
    public int altitude = 0;

    public SpriteRenderer spriteRenderer;
    public GameObject selectionOutline;

    public List<Tile> neighbors = new List<Tile>(6); // 6 hex neighbors
    public Vector2Int axialPos; // q (col), r (row) for axial coords
    public int tileId = -1; // tile id

    public bool passable = true; // Checks whether tile is water / unmovable    
    public int moveCost = 1; // Cost to travel to current tile

    public IOccupant occupant;
    public bool IsOccupied => occupant != null;
    public bool BlockSight =>
        altitude >= 2 ||
        type == TileType.MOUNTAIN; // Mountain always blocks

    private void Awake()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (selectionOutline != null)
        {
            selectionOutline.SetActive(false);
        }
    }

    public void ShowOutline()
    {
        if (selectionOutline != null)
        {
            selectionOutline.SetActive(true);
        }
    }

    public void HideOutline()
    {
        if (selectionOutline != null)
        {
            selectionOutline.SetActive(false);
        }
    }

    // Static method for distance 
    public static int GetDistance(Tile a, Tile b)
    {
        int dq = a.axialPos.x - b.axialPos.x;
        int dr = a.axialPos.y - b.axialPos.y;
        int ds = -dq - dr;
        return (Mathf.Abs(dq) + Mathf.Abs(dr) + Mathf.Abs(ds)) / 2;
    }

    public bool CanClimbFrom(Tile from)
    {
        int heightDiff = Mathf.Abs(altitude - from.altitude);
        return heightDiff < 2;
    }

    // Add neighbor if not included in neighbors
    public void AddNeighbor(Tile neighbor)
    {
        if (!neighbors.Contains(neighbor)) neighbors.Add(neighbor);
    }

    // Checks about occupied
    public bool TryEnter(IOccupant unit)
    {
        if (!passable || IsOccupied) return false;

        if (unit.CurrentTile != null && !CanClimbFrom(unit.CurrentTile))
        {
            Debug.Log($"Can't climb {Mathf.Abs(altitude - unit.CurrentTile.altitude)} height!");
            return false;
        }

        occupant = unit;
        unit.CurrentTile = this;
        return true;
    }
}