using System.Collections.Generic;
using UnityEngine;

// public enum Team { Player1 = 1, Player2 = 2 }

public class UnitController : MonoBehaviour, IOccupant
{
    // public Team teamID;
    public Player teamID;
    public Tile position;
    public UnitDataSO refData;
    public HealthManager healthManager;
    public List<UnitCommandSO> commands = new List<UnitCommandSO>();
    public int movesRemaining;

    [SerializeField] private SpriteRenderer spriteRenderer;

    private Dictionary<Player, Color> playerColors = new Dictionary<Player, Color>()
    {
        { Player.PLAYER_1, Color.blue },
        { Player.PLAYER_2, Color.red }, // NOTE: Add more as needed.
    };

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
    }

    public void SetTeam(Player team)
    {
        teamID = team;
        if (spriteRenderer != null)
        {
            // spriteRenderer.color = team == Player.PLAYER_1 ? Color.blue : Color.red;
            if (playerColors.TryGetValue(team, out Color teamColor))
                spriteRenderer.color = teamColor;
            else
            {
                Debug.LogError($"No color defined for player: {team}.");
                spriteRenderer.color = Color.magenta;
            }
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
    }

    public void ConsumeMoves(int cost)
    {
        movesRemaining -= cost;
        if (movesRemaining < 0) movesRemaining = 0;
    }

    public void OnDeath()
    {
        // clear tile reference so it can be reused
        if (position != null)
        {
            position.occupant = null;
            position = null;
        }
        Destroy(gameObject);
    }

    public bool Attack(UnitController target)
    {
        var command = new AttackCommand();
        var ctx = new CommandContext(); // no board
        var cmdTarget = new CommandTarget(target.position, target);

        if (!command.CanExecute(ctx, this, cmdTarget)) return false;

        var record = command.Execute(ctx, this, cmdTarget);
        if (record != null)
        {
            // Optionally store record for undo. done later.
            return true;
        }
        return false;
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
    public void OnMoved(Tile from, Tile to) { UpdatePosition(); }
    public void onDeath() => OnDeath();
}
