using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FogOfWar : MonoBehaviour
{
    private void Start()
    {
        GameManager.Instance.OnGameStateChanged += UpdateFog;
    }

    private void OnDisable()
    {

        GameManager.Instance.OnGameStateChanged -= UpdateFog;
        
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
                if (tile != null && !tile.IsNull)
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
        }
    }
}
