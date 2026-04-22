using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;


public class UnitController : MonoBehaviour, IOccupant
{
    public Player teamID;
    public Tile position;
    public UnitDataSO refData;
    public HealthManager healthManager;
    public List<UnitCommandSO> commands = new List<UnitCommandSO>();
    public int movesRemaining;
    public int range;

    private bool loadedFromSave = false;

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

        range = refData != null ? refData.attackRange : 0;
        //healthManager.SetMaxHealth(healthManager.maxHealth);
    }

    private void Update()
    {
        // if (Input.GetKeyDown(KeyCode.K))
        // {
        //     healthManager.TakeDamage(5);
        // }


        // TESTING HEALTH BAR
        if (!IsCurrentlySelected())
        {
            return;
        }

        // if (Input.GetKeyDown(KeyCode.O))
        // {
        //     healthManager.TakeDamage(1);

        //     Debug.Log("UNIT TOOK DAMAGE! New health: " + healthManager.health);
        // }
        // if (Input.GetKeyDown(KeyCode.P))
        // {
        //     healthManager.GainHealth(1);

        //     Debug.Log("UNIT GAINED HEALTH! New health: " + healthManager.health);
        // }
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



    public void SetTeam(Player team)
    {
        teamID = team;
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Colours.GetColor(team);
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
        movesRemaining = refData != null ? refData.maxMovesPerTurn : 0;
    }

    private void Start()
    {
        if (!loadedFromSave)
        {
            // initialize health based on unit data scriptable object
            if (healthManager != null && refData != null)
            {
                healthManager.SetMaxHealth(refData.maxHealth);
            }

            // initialize moves and range on spawn
            if (refData != null)
            {
                movesRemaining = refData.maxMovesPerTurn;
                range = refData.attackRange;
            }
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


    // To retrieve bonus damage value
    public int GetBonusDamageAgainst(UnitController target)
    {
        if (refData == null || target == null || target.refData == null)
        {
            return 0;
        }

        UnitDataSO.UnitType targetType = target.refData.unitType;

        foreach (UnitDataSO.DamageBonus bonus in refData.damageBonuses)
        {
            if (bonus.targetType == targetType)
            {
                return bonus.bonusDamage;
            }
        }

        return 0;

    }


    public void OnDeath()
    {
        Tile deathTile = position;

        // clear tile reference so it can be reused
        if (position != null)
        {
            position.occupant = null;
            position = null;
        }

        if (deathTile != null)
        {
            ReplayManager.EnsureExists().RecordUnitDied(this, deathTile);
        }

        GameManager.Instance?.NotifyGameStateChanged();
        Destroy(gameObject);
    }

    public bool MoveToAdjacentTile(Tile destination)
    {
        if (destination == null || destination.IsOccupied || !destination.passable)
            return false;

        Tile fromTile = position;

        if (fromTile == null || !fromTile.neighbors.Contains(destination))
            return false;

        if (!destination.CanClimbFrom(fromTile))
            return false;

        // leaving current tile.
        fromTile.occupant = null;

        // entering new tile
        if (destination.TryEnter(this))
        {
            ReplayManager.EnsureExists().RecordUnitMoved(this, fromTile, destination);
            OnMoved(fromTile, destination);
            return true;
        }

        // Failed, re-enter old tile
        fromTile.TryEnter(this);
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

    public void ApplyLoadedState(int currentHealth, int maxHealth, int movesRemaining, int attackRange, string loadedName)
    {
        loadedFromSave = true;

        if (healthManager != null)
        {
            healthManager.SetMaxHealth(maxHealth);
            healthManager.SetCurrentHealth(currentHealth);
        }

        this.movesRemaining = movesRemaining;
        this.range = attackRange;

        if (!string.IsNullOrWhiteSpace(loadedName))
        {
            name = loadedName;
        }
    }
}
