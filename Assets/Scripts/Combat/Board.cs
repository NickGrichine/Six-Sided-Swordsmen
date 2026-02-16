using UnityEngine;
//TODO: complete all helper functions for validating commands.
public class Board : MonoBehaviour
{
    public bool WithinBounds(Tile tile) => true;
    public bool CanMove(Tile from, Tile to, int movementAvailable) => true;
}