using System.Collections.Generic;
using UnityEngine;
using System;
using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GhostUnitsSpawner : Singleton<GhostUnitsSpawner>
{
    [SerializeField] private GameObject ghostUnitPrefab;
    [SerializeField] private int numberOfPlayers = 2;
    [SerializeField] private int minimumPlayableTilesForSpawnArea = 25;
    [SerializeField] private UnitDataSO ghostUnitDataSO;

    private int width;
    private int height;
    private int q_min;
    private int q_max;
    private int r_min;
    private int r_max;
    private Dictionary<Player, UnitController> ghosts = new Dictionary<Player, UnitController>();
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
        CenterCameraToGhostUnit();

        SetupManager.Instance.onTurnPass += CenterCameraToGhostUnit;
    }

    private void CenterCameraToGhostUnit()
    {
        Player currentPlayer = SetupManager.Instance.CurrentPlayer;
        UnitController ghost_uc = ghosts[currentPlayer];
        Tile tile = ghost_uc.position;
        CameraController.Instance.SetPosition(tile.transform.position);
    }

    private void InstantiateGhostUnits()
    {
        for (int i = 0; i < numberOfPlayers; i++)
        {
            Tile tile = GetValidTileForGhostSpawning();
            if (tile == null)
                throw new System.Exception("Cannot place ghost units, map not big enough.");
            tiles_available.Add(tile);
        }

        int tile_index = 0;
        for (int i = 0; i < numberOfPlayers; i++)
        {
            GameObject ghost = Instantiate(ghostUnitPrefab);
            UnitController ghost_uc = ghost.GetComponent<UnitController>();
            Player player = (Player)(i + 1);
            Tile tile = tiles_available[tile_index++];

            ghost_uc.SetTeam(player);
            ghost_uc.position = tile;
            tile.ghostOccupant = ghost_uc;
            ghosts[player] = ghost_uc;
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
        // Tile itself is not water, constraint:
        if (tileToTest.type == TileType.SHORE
                || tileToTest.type == TileType.OCEAN_DEEP)
            return false;

        HashSet<Tile> set_a = GetVisibleTiles(tileToTest, ghostUnitDataSO.visionRange, HexGridManager.Instance.Grid);

        // Minimum playable tiles constraint:
        int num_playable_tiles = 0;
        foreach (Tile tile in set_a)
        {
            if (tileToTest.type == TileType.SHORE
                    || tileToTest.type == TileType.OCEAN_DEEP)
                continue;
            num_playable_tiles++;
        }
        if (num_playable_tiles < minimumPlayableTilesForSpawnArea)
            return false;

        // Overlap constraint:
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
        int q = UnityEngine.Random.Range(q_min, q_max);
        int r = UnityEngine.Random.Range(r_min, r_max);
        int tileID = HexGridManager.Instance.GetTileId(q, r);
        return HexGridManager.Instance.GetTileById(tileID);
    }
}
