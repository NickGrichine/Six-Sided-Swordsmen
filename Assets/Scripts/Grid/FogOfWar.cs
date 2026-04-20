using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FogOfWar : MonoBehaviour
{
    [SerializeField] private bool queryGhost = false;

    private void Start()
    {
        if (queryGhost)
        {
            SetupManager.Instance.onTurnPass += UpdateFogForGhost;
            SetupManager.Instance.onSetupStart += UpdateFogForGhost;
            return;
        }
        GameManager.Instance.OnGameStateChanged += UpdateFog;
    }

    private void OnDisable()
    {
        if (queryGhost)
        {
            SetupManager.Instance.onTurnPass -= UpdateFogForGhost;
            SetupManager.Instance.onSetupStart -= UpdateFogForGhost;
            return;
        }
        if (GameManager.Instance != null)
            GameManager.Instance.OnGameStateChanged -= UpdateFog;
    }

    public void UpdateFogForGhost()
    {
        print("Call to update fog");
        Player currentTurnPlayer = SetupManager.Instance.CurrentPlayer; // setup additions
        HashSet<Tile> visibleTiles = new HashSet<Tile>();

        foreach (Tile sourceTile in HexGridManager.Instance.Grid)
        {
            if (sourceTile == null || !sourceTile.IsGhostOccupied)
                continue;

            print("current player turn: " + currentTurnPlayer + ", tile occupant owner: " + sourceTile.ghostOccupant.OwnerId);
            if (sourceTile.ghostOccupant.OwnerId != (int)currentTurnPlayer)
                continue;

            UnitController unit = sourceTile.ghostOccupant as UnitController; // setup additions
            if (unit == null)
                continue;

            HashSet<Tile> unitVisibleTiles = unit.CanSee();
            print("Unit " + unit.name + " can see " + unitVisibleTiles.Count + " tiles.");
            if (unitVisibleTiles == null)
                continue;

            foreach (Tile tile in unitVisibleTiles)
            {
                if (tile != null)
                    visibleTiles.Add(tile);
            }
        }

        print("Updating fog of war. Visible tiles count: " + visibleTiles.Count);
        foreach (Tile tile in HexGridManager.Instance.Grid)
        {
            if (tile == null)
                continue;

            bool hasFog = !visibleTiles.Contains(tile);
            tile.ShowFog(hasFog);

            if (tile.IsGhostOccupied && tile.ghostOccupant is UnitController unit)
            {
                unit.Show(tile.Visible);
            }
        }
    }

    public void UpdateFog()
    {
        print("Call to update fog");
        Player currentTurnPlayer = GameManager.Instance.TurnPlayer;
        HashSet<Tile> visibleTiles = new HashSet<Tile>();

        foreach (Tile sourceTile in HexGridManager.Instance.Grid)
        {
            if (sourceTile == null || !sourceTile.IsOccupied)
                continue;

            print("current player turn: " + currentTurnPlayer + ", tile occupant owner: " + sourceTile.occupant.OwnerId);
            if (sourceTile.occupant.OwnerId != (int)currentTurnPlayer)
                continue;

            UnitController unit = sourceTile.occupant as UnitController;
            if (unit == null)
                continue;

            HashSet<Tile> unitVisibleTiles = unit.CanSee();
            print("Unit " + unit.name + " can see " + unitVisibleTiles.Count + " tiles.");
            if (unitVisibleTiles == null)
                continue;

            foreach (Tile tile in unitVisibleTiles)
            {
                if (tile != null)
                    visibleTiles.Add(tile);
            }
        }

        print("Updating fog of war. Visible tiles count: " + visibleTiles.Count);
        foreach (Tile tile in HexGridManager.Instance.Grid)
        {
            if (tile == null)
                continue;

            bool hasFog = !visibleTiles.Contains(tile);
            tile.ShowFog(hasFog);

            if (tile.IsOccupied && tile.occupant is UnitController unit)
            {
                unit.Show(tile.Visible);
            }
        }
    }
}
