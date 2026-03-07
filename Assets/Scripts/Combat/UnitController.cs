using System.Collections.Generic;
using UnityEngine;

public enum Team { Player1 = 1, Player2 = 2 }

public class UnitController : MonoBehaviour, IOccupant
{
    public int UnitID;
    public Team teamID;
    public Tile position;
    public UnitDataSO refData;
    public HealthManager healthManager;
    public List<UnitCommandSO> commands = new List<UnitCommandSO>();
    public int movesRemaining;

    [SerializeField] private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        if (healthManager == null)
            healthManager = GetComponent<HealthManager>();
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void SetTeam(Team team)
    {
        teamID = team;
        if (spriteRenderer != null)
        {
            spriteRenderer.color = team == Team.Player1 ? Color.blue : Color.red;
        }
    }

    public void StartTurn()
    {
        movesRemaining = refData.maxMovesPerTurn;
    }

    public void ConsumeMoves(int cost)
    {
        movesRemaining -= cost;
        if (movesRemaining < 0) movesRemaining = 0;
    }

    public void OnDeath()
    {
        //TODO:  will have be expanded later. TBA to Kinson?
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
            // Optionally store record for undo
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

    // IOccupant implementation
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