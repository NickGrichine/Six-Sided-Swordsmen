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
            image.sprite = unit.worldSpaceIcon;

            // Subscribe "select unit" function to onClick event:
            Button button = instance.GetComponent<Button>();
            button.onClick += (_) => { SelectUnit(unit); };
            button.onClick += (_) => { _update_unit_display(unit); };
        }

        // Subscribe "place unit" function to onTileClicked event:
        GridEventHandler.Instance.onTileClicked += _place_unit_on_tile_click;

        ForceResizePanel();
    }

    private void _update_unit_display(UnitDataSO unitData) =>
        UnitDisplay.Instance.UpdateUnitDisplay(unitData);

    void OnDestroy()
    {
        GridEventHandler.Instance.onTileClicked -= _place_unit_on_tile_click;
    }

    private void _place_unit_on_tile_click(Tile tile)
    {
        Player team = SetupManager.Instance.CurrentPlayer;
        UnitDataSO unit = this.selected_unit;
        PlaceUnit(team, unit, tile);
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
        if (tile == null) return;
        if (!tile.Visible) return;
        if (tile.IsOccupied) return;
        if (!tile.passable) return;
        if (selected_unit == null) return;
        if (ResourceManager.Instance.CheckValidDeductionOfResource(team, unit.cost) == false) return;

        UnitSpawner.Instance.SpawnUnit(team, tile, unit.unitType);

        OnUnitPlacement?.Invoke(team, unit, tile);
    }
}
