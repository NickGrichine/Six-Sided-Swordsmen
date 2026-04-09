using System.Collections.Generic;
using UnityEngine;

public enum Team { Player1 = 1, Player2 = 2 }

public class UnitController : MonoBehaviour, IOccupant
{
    // public Team teamID;
    public Player teamID;
    public Tile position;
    public UnitDataSO refData;
    public HealthManager healthManager;    
    public List<UnitCommandSO> commands = new List<UnitCommandSO>();
    public int movesRemaining;
    public int range;

    [SerializeField] private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        if (healthManager == null)
            healthManager = GetComponent<HealthManager>();
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        // ensure health manager exists and wire death event
        if (healthManager != null)
        {
            healthManager.onDeath += OnDeath;
        }
        range = refData.attackRange;

        //healthManager.SetMaxHealth(healthManager.maxHealth);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.K))
        {
            healthManager.TakeDamage(5);
        }


        // TESTING HEALTH BAR
        if (!IsCurrentlySelected())
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.O))
        {
            healthManager.TakeDamage(1);

            Debug.Log("UNIT TOOK DAMAGE! New health: " + healthManager.health);
        }
        if (Input.GetKeyDown(KeyCode.P))
        {
            healthManager.GainHealth(1);

            Debug.Log("UNIT GAINED HEALTH! New health: " + healthManager.health);
        }
        // if (Input.GetKeyDown(KeyCode.I))
        // {
        //     healthManager.SetMaxHealth(10);

        //     Debug.Log("UNIT HEALTH RESET! New health: " + healthManager.health);
        // }
    }

    // This function is for making sure that only the unit that is currently selected (highlighted tile) will be affected
    // ie. making sure that any changes in Health status only applies to this selected unit, rather than ALL units
    private bool IsCurrentlySelected()
    {
        if (position == null || !ReferenceEquals(position.occupant, this))
        {
            return false;
        }

        GridEventHandler gridEventHandler = GridEventHandler.Instance;
        if (gridEventHandler == null)
        {
            return false;
        }

        return gridEventHandler.SelectedTile == position;
    }



    public void SetTeam(Team team)
    {
        teamID = (Player)team;
        if (spriteRenderer != null)
        {
            spriteRenderer.color = team == Team.Player1 ? Colours.PLAYER_GREEN : Colours.PLAYER_YELLOW;
        }
    }

    public void Show(bool shouldShow)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = shouldShow;
        }

        if (healthManager != null && healthManager.healthBar != null)
        {
            healthManager.healthBar.Show(shouldShow);
        }
    }

    public void StartTurn()
    {
        movesRemaining = refData.maxMovesPerTurn;
    }

    private void Start()
    {
        // initialize health based on unit data scriptable object
        if (healthManager != null && refData != null)
        {
            healthManager.SetMaxHealth(refData.maxHealth);
        }

        // initialize moves and range on spawn (GameManager.OnTurnStart fires before units exist on turn 1)
        if (refData != null)
        {
            movesRemaining = refData.maxMovesPerTurn;
            range = refData.attackRange;
        }

        if (GameManager.Instance != null)
            GameManager.Instance.OnTurnStart += OnTurnStarted;
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnTurnStart -= OnTurnStarted;
    }

    private void OnTurnStarted(int turnNumber)
    {
        if ((Player)teamID == GameManager.Instance.TurnPlayer)
            StartTurn();
    }

    public void ConsumeMoves()
    {
        movesRemaining = 0;
    }

    public void OnDeath()
    {
        // clear tile reference so it can be reused
        if (position != null)
        {
            position.occupant = null;
            position = null;
        }

        GameManager.Instance?.NotifyGameStateChanged();
        Destroy(gameObject);
    }

    public bool MoveToAdjacentTile(Tile destination)
    {
        if (destination == null || destination.IsOccupied || !destination.passable)
            return false;

        if (position == null || !position.neighbors.Contains(destination))
            return false;

        if (!destination.CanClimbFrom(position))
            return false;

        // leaving current tile.
        position.occupant = null;

        // entering new tile
        if (destination.TryEnter(this))
        {
            OnMoved(position, destination);
            return true;
        }

        // Failed, re-enter old tile
        position.TryEnter(this);
        return false;
    }

    public HashSet<Tile> CanSee()
    {
        HashSet<Tile> visibleTiles = new HashSet<Tile>();

        if (position == null)
            return visibleTiles;

        Tile[,] grid = HexGridManager.Instance != null ? HexGridManager.Instance.Grid : null;
        if (grid == null)
            return visibleTiles;

        int visionRange = refData != null ? Mathf.Max(0, refData.visionRange) : 0;

        foreach (Tile tile in grid)
        {
            if (tile == null)
                continue;

            if (Tile.GetDistance(position, tile) <= visionRange)
                visibleTiles.Add(tile);
        }

        return visibleTiles;
    }

/*
    public bool MoveToTile(Tile destination)
    {
        if (destination == null || destination.IsOccupied || !destination.passable)
            return false;

        var path = HexPathfinder.FindPath(position, destination);
        if (path.Count == 0) return false;

        if (path.Count == 2) // Adjacent
        {
            return MoveToAdjacentTile(destination);
        }

        var nextTile = path[1];
        return MoveToAdjacentTile(nextTile);
    } */
    public int OwnerId => (int)teamID;
    public Tile CurrentTile { get => position; set { position = value; UpdatePosition(); } }

    private void UpdatePosition()
    {
        if (position != null)
        {
            transform.position = position.transform.position;
        }
    }

    public void OnNewTurn() => StartTurn();
    public void OnMoved(Tile from, Tile to)
    {
        UpdatePosition();
        GameManager.Instance?.NotifyGameStateChanged();
    }
    public void onDeath() => OnDeath();
}