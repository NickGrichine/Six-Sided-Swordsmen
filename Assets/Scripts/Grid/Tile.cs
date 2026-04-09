using UnityEngine;
using System.Collections.Generic;

public class Tile : MonoBehaviour
{
    public TileType type;
    public int altitude = 0;
    public int grassVariant = 0;
    // 0 = flat, 1 = flower

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
    private bool hasFog = true;
    public bool Visible => !hasFog;

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
        this.hasFog = hasFog;

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
        if (neighbor == null || neighbor == this)
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