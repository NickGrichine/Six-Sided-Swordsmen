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
            print("NullTile GameObject is null? " + (go == null));

            _nullTile = go.AddComponent<Tile>();
            _nullTile.enabled = false; // never runs Update; Start won't run when disabled
            _nullTile.passable = false;
            _nullTile.moveCost = int.MaxValue;
            _nullTile.gridPos = new Vector2Int(int.MinValue, int.MinValue);
            _nullTile.tileId = -1;

            print("returning NullTile");

            return _nullTile;
        }
    }

    public bool IsNull => this == NullTile;
    public TileType type;
    public int altitude = 0;
    public int grassVariant = 0; 
    // 0 = flat, 1 = rock1, 2 = rock2, 3 = flower

    public SpriteRenderer spriteRenderer;
    public GameObject selectionOutline;

    public List<Tile> neighbors = new List<Tile>(6); // 6 hex neighbors
    public List<int> neighborIds = new List<int>(6); // for saving/loading neighbors by id
    public Vector2Int gridPos; // q column, r row in flat-top odd-q offset coords
    public int tileId = -1; // tile id

    public bool passable = true; // Checks whether tile is water / unmovable    
    public int moveCost = 1; // Cost to travel to current tile

    public IOccupant occupant;
    public bool IsOccupied => occupant != null;
    public bool BlockSight =>
        altitude >= 2 ||
        type == TileType.MOUNTAIN; // Mountain always blocks

    [SerializeField] private Sprite[] fogSprites = new Sprite[0];
    [SerializeField] private SpriteRenderer fogSpriteRenderer;


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

print (fogSprites == null ? "fogSprites is null" : $"fogSprites length: {fogSprites.Length}");
        float randomFogIndex = Random.Range(0, fogSprites.Length);
        if (fogSpriteRenderer != null && fogSprites.Length > 0)
        {
            fogSpriteRenderer.sprite = fogSprites[(int)randomFogIndex];
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

    public void ShowFog(bool hasFog)
    {
        if (fogSpriteRenderer != null)
        {
            fogSpriteRenderer.enabled = hasFog;
        }
    }

    // Static method for distance 
   public static int GetDistance(Tile a, Tile b)
    {
       return HexMath.Distance(a.gridPos, b.gridPos);
    }

    public bool CanClimbFrom(Tile from)
    {
        int heightDiff = Mathf.Abs(altitude - from.altitude);
        return heightDiff < 2;
    }

    public void ClearNeighbors()
    {
        neighbors.Clear();
        neighborIds.Clear();
    }

    // Add neighbor if not included in neighbors
    public void AddNeighbor(Tile neighbor)
    {
        if (neighbor == null || neighbor.IsNull || neighbor == this)
            return;

        if (!neighbors.Contains(neighbor))
        {
            neighbors.Add(neighbor); 
            neighborIds.Add(neighbor.tileId); 
        }
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