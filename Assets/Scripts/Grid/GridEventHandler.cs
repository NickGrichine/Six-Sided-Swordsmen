using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class GridEventHandler : Singleton<GridEventHandler>
{
    public event Action<Tile> onTileClicked;

    [Header("Input")]
    [SerializeField] private Camera worldCamera;
    [SerializeField] private LayerMask tileLayerMask = ~0;
    [SerializeField] private int mouseButton = 0;

    private Tile _selectedTile;

    public Tile SelectedTile => _selectedTile != null ? _selectedTile : Tile.NullTile;

    protected override void Awake()
    {
        base.Awake();
        _selectedTile = Tile.NullTile;
    }

    public void ClearSelectedTile()
    {
        if (_selectedTile != null && _selectedTile != Tile.NullTile)
        {
            _selectedTile.HideOutline();
        }

        _selectedTile = Tile.NullTile;
        Debug.Log("Selection cleared (SelectedTile = NullTile).");
    }

    private void Reset()
    {
        worldCamera = Camera.main;
    }

    private void Update()
    {
        if (!Input.GetMouseButtonDown(mouseButton))
            return;

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        if (worldCamera == null)
            worldCamera = Camera.main;

        if (worldCamera == null)
            return;

        Vector3 world = worldCamera.ScreenToWorldPoint(Input.mousePosition);
        Vector2 world2 = new Vector2(world.x, world.y);

        Collider2D col = Physics2D.OverlapPoint(world2, tileLayerMask);
        if (!col)
            return;

        Tile tile = col.GetComponentInParent<Tile>();
        if (tile == null)
            return;

        if (_selectedTile != null && _selectedTile != Tile.NullTile && _selectedTile != tile)
        {
            _selectedTile.HideOutline();
        }

        _selectedTile = tile;
        _selectedTile.ShowOutline();

        Debug.Log($"Clicked tile: {tile.name} at {tile.transform.position} layer={LayerMask.LayerToName(tile.gameObject.layer)}");

        onTileClicked?.Invoke(tile);
    }
}