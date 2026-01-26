public interface IOccupant
{
    int OwnerId { get; }

    Tile CurrentTile { get; set; }

    void OnNewTurn();
    void OnMoved(Tile from, Tile to);
    void onDeath();
}