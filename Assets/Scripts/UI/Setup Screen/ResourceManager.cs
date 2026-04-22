using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Collections;
using System;


public class ResourceManager : Singleton<ResourceManager>
{
    [SerializeField] private readonly int startingResourceAmount = 150;
    [SerializeField] private TextMeshProUGUI resourceTextObject;

    public event Action<Player, int> OnResourceDeduction;

    private Dictionary<Player, int> playerResources;



    void Start()
    {
        // Initialize players' starting resources
        playerResources = new Dictionary<Player, int>()
        {
            { Player.PLAYER_1, startingResourceAmount },
            { Player.PLAYER_2, startingResourceAmount },
        };

        // Subscription
        OnResourceDeduction += (_, _) => UpdateResourceDisplay();
        UnitArray.Instance.OnUnitPlacement += (player, unit, _) => DeductResourceFromPlayer(player, unit.cost);

        IEnumerator delayedStart()
        {
            yield return null;
            UpdateResourceDisplay();
        }
        StartCoroutine(delayedStart());
    }

    public void UpdateResourceDisplay()
    {
        Player player = SetupManager.Instance.CurrentPlayer;
        int amount = GetPlayerResourceAmount(player);
        resourceTextObject.text = amount + " Gold";
    }

    public int GetPlayerResourceAmount(Player player)
    {
        if (playerResources.TryGetValue(player, out int resource))
            return resource;

        throw new Exception($"No resource amount set for {player}.");
    }

    public bool CheckValidDeductionOfResource(Player player, int deduction)
    {
        if (deduction > playerResources[player])
            return false;
        return true;
    }

    public void DeductResourceFromPlayer(Player player, int deduction)
    {
        int current_amount = playerResources[player];
        if (deduction > current_amount)
            throw new Exception($"Deduction bigger than current amount of resource for {player}.");
        playerResources[player] -= deduction;

        OnResourceDeduction?.Invoke(player, deduction);
    }



}
