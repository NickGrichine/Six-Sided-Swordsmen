using UnityEngine;
using UnityEngine.EventSystems;

public class GridEventHandler : Singleton<GridEventHandler>
{
    [Header("Mouse Settings")]
    [SerializeField] private int mouseButton = 0; // 0 = left click, 1 = right click, etc.

    private Tile _selectedTile;
    public Tile SelectedTile => _selectedTile;

    public event System.Action<Tile> onTileClicked;

    protected override void Awake()
    {
        base.Awake();
        _selectedTile = null;
    }

    public void ClearSelectedTile()
    {
        if (_selectedTile != null)
        {
            _selectedTile.HideOutline();
        }

        _selectedTile = null;
        Debug.Log("Selection cleared (SelectedTile = null).");
    }

    private void Update()
    {
        if (!Input.GetMouseButtonDown(mouseButton))
            return;

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 point = new Vector2(mouseWorld.x, mouseWorld.y);

        Collider2D hit = Physics2D.OverlapPoint(point);
        if (hit == null)
            return;

        Tile tile = hit.GetComponent<Tile>();
        if (tile == null)
            return;

        if (_selectedTile != null && _selectedTile != tile)
        {
            _selectedTile.HideOutline();
        }

        _selectedTile = tile;
        _selectedTile.ShowOutline();

        Debug.Log($"Clicked tile: {_selectedTile.name} at {_selectedTile.gridPos}");

        onTileClicked?.Invoke(tile);
    }
}