using UnityEngine;
using System.Collections.Generic;

public class GhostUnitsSpawner : Singleton<GhostUnitsSpawner>
{
    [SerializeField] private GameObject ghostUnitPrefab;
    [SerializeField] private int numberOfPlayers = 2;
    [SerializeField] private int maxNumberOfGhostsPerPlayer = 2;
    [SerializeField] private UnitDataSO ghostUnitDataSO;

    private int width;
    private int height;
    private int q_min;
    private int q_max;
    private int r_min;
    private int r_max;
    private List<UnitController> ghosts = new List<UnitController>();
    private List<Tile> tiles_available = new List<Tile>();

    void Start()
    {
        width = HexGridManager.Instance.width;
        height = HexGridManager.Instance.height;

        q_min = HexGridManager.Instance.PlayableOffsetQ;
        q_max = width + HexGridManager.Instance.PlayableOffsetQ;
        r_min = HexGridManager.Instance.PlayableOffsetR;
        r_max = height + HexGridManager.Instance.PlayableOffsetR;

        InstantiateGhostUnits();
    }

    private void InstantiateGhostUnits()
    {
        for (int i = 0; i < maxNumberOfGhostsPerPlayer * numberOfPlayers; i++)
        {
            Tile tile = GetValidTileForGhostSpawning();
            if (tile == null)
                break;
            tiles_available.Add(tile);
        }

        int number_of_ghosts_per_player = tiles_available.Count / numberOfPlayers;
        int tile_index = 0;
        for (int k = 0; k < number_of_ghosts_per_player; k++)
        {
            for (int i = 0; i < numberOfPlayers; i++)
            {
                GameObject ghost = Instantiate(ghostUnitPrefab);
                UnitController ghost_uc = ghost.GetComponent<UnitController>();
                ghost_uc.SetTeam((Player)(i + 1));
                Tile tile = tiles_available[tile_index];
                ghost_uc.position = tile;
                tile.ghostOccupant = ghost_uc;
                ghosts.Add(ghost_uc);
                tile_index++;
            }
        }
    }

    private Tile GetValidTileForGhostSpawning()
    {
        for (int i = 0; i < 1000; i++)
        {
            Tile random_tile = GetRandomTile();
            if (IsTileValidForGhostUnitSpawn(random_tile))
                return random_tile;
        }
        return null;
    }

    private bool IsTileValidForGhostUnitSpawn(Tile tileToTest)
    {
        HashSet<Tile> set_a = GetVisibleTiles(tileToTest, ghostUnitDataSO.visionRange, HexGridManager.Instance.Grid);
        foreach (Tile tile in tiles_available)
        {
            HashSet<Tile> set_b = GetVisibleTiles(tile, ghostUnitDataSO.visionRange, HexGridManager.Instance.Grid);
            if (set_a.Overlaps(set_b))
                return false;
        }

        return true;
    }

    private HashSet<Tile> GetVisibleTiles(Tile origin, int visionRange, Tile[,] grid)
    {
        HashSet<Tile> visibleTiles = new HashSet<Tile>();

        foreach (Tile tile in grid)
        {
            if (tile == null)
                continue;

            if (Tile.GetDistance(origin, tile) <= visionRange)
                visibleTiles.Add(tile);
        }

        return visibleTiles;
    }

    private Tile GetRandomTile()
    {
        int q = Random.Range(q_min, q_max);
        int r = Random.Range(r_min, r_max);
        int tileID = HexGridManager.Instance.GetTileId(q, r);
        return HexGridManager.Instance.GetTileById(tileID);
    }
}
