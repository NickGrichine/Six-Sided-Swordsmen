using UnityEngine;
using System.Collections.Generic;

public class Colours
{
    public static readonly Color32 PLAYER_RED = new Color32(220, 25, 25, 255);
    public static readonly Color32 PLAYER_BLUE = new Color32(25, 25, 220, 255);
    public static readonly Color32 PLAYER_PINK = new Color32(220, 25, 125, 255);
    public static readonly Color32 PLAYER_YELLOW = new Color32(220, 220, 25, 255);
    public static readonly Color32 PLAYER_PURPLE = new Color32(125, 25, 220, 255);
    public static readonly Color32 PLAYER_GREEN = new Color32(25, 220, 25, 255);

    private static Dictionary<Player, Color32> _player_to_color = new Dictionary<Player, Color32>()
    {
        { Player.PLAYER_1, PLAYER_GREEN },
        { Player.PLAYER_2, PLAYER_YELLOW }, // NOTE: Add more as needed.
    };

    public static Color32 GetColor(Player player)
    {
        if (_player_to_color.TryGetValue(player, out Color32 playerColor))
            return playerColor;
        else
        {
            Debug.LogError($"No color defined for player: {player}.");
            return Color.magenta;
        }
    }
}
