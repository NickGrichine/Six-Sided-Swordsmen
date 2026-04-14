using UnityEngine;
using UnityEngine.UI;
using System;

public class UnitArray : Singleton<UnitArray>
{
    [SerializeField] private GameObject unitButtonPrefab;
    [SerializeField] private UnitDataSO[] allUnits;
    [SerializeField] private RectTransform unitArray;
    [SerializeField] private RectTransform unitPanel;

    private UnitDataSO selected_unit = null;

    public event Action<Player, UnitDataSO, Tile> OnUnitPlacement;



    void Start()
    {
        foreach (UnitDataSO unit in allUnits)
        {
            GameObject instance = Instantiate(unitButtonPrefab, this.transform);
            Image image = instance.GetComponent<Image>();
            image.sprite = unit.icon;

            // Subscribe "select unit" function to onClick event:
            Button button = instance.GetComponent<Button>();
            button.onClick += (_) => { SelectUnit(unit); };

        }

        // Subscribe "place unit" function to onTileClicked event:
        GridEventHandler.Instance.onTileClicked += (tile) =>
        {
            Player team = GameManager.Instance.TurnPlayer;
            UnitDataSO unit = this.selected_unit;
            PlaceUnit(team, unit, tile);
        };

        ForceResizePanel();
    }

    private void ForceResizePanel()
    {
        LayoutRebuilder.ForceRebuildLayoutImmediate(unitArray);
        LayoutRebuilder.ForceRebuildLayoutImmediate(unitPanel);
    }

    private void SelectUnit(UnitDataSO unit)
    {
        selected_unit = unit;
        Debug.Log("selected unit changed to " + selected_unit);
    }

    private void PlaceUnit(Player team, UnitDataSO unit, Tile tile)
    {
        if (tile.IsOccupied) return;
        if (selected_unit == null) return;
        if (ResourceManager.Instance.CheckValidDeductionOfResource(team, unit.cost) == false) return;

        UnitSpawner.TagUnitType tagUnitType = _convertUnitTypeToTagUnitType(unit);
        UnitSpawner.Instance.SpawnUnit(team, tile, tagUnitType);

        OnUnitPlacement?.Invoke(team, unit, tile);
    }
    private UnitSpawner.TagUnitType _convertUnitTypeToTagUnitType(UnitDataSO unit)
    {
        switch (unit.unitType)
        {
            case UnitDataSO.UnitType.Archer:
                return UnitSpawner.TagUnitType.Archer;
            case UnitDataSO.UnitType.Knight:
                return UnitSpawner.TagUnitType.Cleric;
            case UnitDataSO.UnitType.Spearman:
                return UnitSpawner.TagUnitType.Spearman;
            case UnitDataSO.UnitType.Swordsman:
                return UnitSpawner.TagUnitType.Knight;
            default:
                throw new System.Exception("Unit type unrecognized.");
        }
    }


}
