using System;
using System.Collections.Generic;
using UnityEngine;

public class HealthManager : MonoBehaviour
{
    [SerializeField] private int maxHealth;
    private int health;
    public StatModifierManager DamageTakenModifier { get; private set; }
    public StatModifierManager HealingReceivedModifier { get; private set; }
    private float dodge;


    public Action<int> onDamage;
    public Action<int> onPermanentDamage;
    public Action<int> onCrit;
    public Action onDodge;
    public Action<int> onHeal;
    public Action onDeath;

    public HealthBar healthBar;
    public Action<int, int> onHealthChanged;


    private void Awake()
    {
        health = maxHealth;
        DamageTakenModifier = gameObject.AddComponent<StatModifierManager>();
        HealingReceivedModifier = gameObject.AddComponent<StatModifierManager>();

        onHealthChanged?.Invoke(health, maxHealth);
    }

    public void SetMaxHealth(int health)
    {
        if (health <= 0) return;
        maxHealth = health;
        this.health = maxHealth;
    }

    public void SetMaxHealth(float relativeHealth)
    {
        if (relativeHealth <= 0f) return;
        maxHealth = Mathf.RoundToInt(relativeHealth * maxHealth);
        this.health = maxHealth;
    }

    public void SetDodge(float dodge)
    {
        this.dodge = Mathf.Clamp01(dodge);
    }

    public void AddDodge(float dodge)
    {
        this.dodge = Mathf.Clamp01(this.dodge + dodge);
    }


    public virtual void TakeDamage(int amount)
    {
        int modifiedAmount = (int)(DamageTakenModifier.GetMultiplier() * amount);
        if (modifiedAmount < 0) throw new Exception("cannot resolve negative damage");
        if (modifiedAmount == 0) return;

        float dodgeRoll = UnityEngine.Random.value;
        if (dodgeRoll < dodge)
        {
            onDodge?.Invoke();
            return;
        }

        health -= modifiedAmount;

        if (health <= 0)
        {
            health = 0;
            onDeath?.Invoke();
        }
        onDamage?.Invoke(modifiedAmount);
    }


    public virtual void TakeCriticalDamage(int amount)
    {
        // crit does 2x damage and cannot be dodged. it invokes its own event for specific sounds and number popup
        int modifiedAmount = (int)(2f * amount);


        modifiedAmount = (int)(DamageTakenModifier.GetMultiplier() * modifiedAmount);
        if (modifiedAmount < 0) throw new Exception("cannot resolve negative damage");
        if (modifiedAmount == 0) return;


        health -= modifiedAmount;

        if (health <= 0)
        {
            health = 0;
            onDeath?.Invoke();
        }
        onCrit?.Invoke(modifiedAmount);
    }


    public virtual void TakeLethalDamage()
    {
        health = 0;
        onDamage?.Invoke(maxHealth);
        onDeath?.Invoke();
    }


    // lower max health, cannot kill
    public void TakePermanentDamage(int amount)
    {
        if (amount < 0) throw new Exception("cannot resolve negative permanent damage");
        if (amount == 0) return;

        maxHealth -= amount;
        if (maxHealth < 1) maxHealth = 1;
        if (health > maxHealth) health = maxHealth;
        onPermanentDamage?.Invoke(amount);
    }
    

    public void GainHealth(int amount)
    {
        if (amount < 0) throw new Exception("cannot resolve negative heal");
        if (amount == 0) return;
        int modifiedAmount = (int) (HealingReceivedModifier.GetMultiplier() * amount);
        if (modifiedAmount < 1) modifiedAmount = 1;
        
        
        health += modifiedAmount;
        if (health > maxHealth) health = maxHealth;
        //Debug.Log("initial = " + amount + " x" +healReceivedModifier.getMultiplier()+ "modifier; healed " + modifiedAmount +" ; total " + health);
        
        onHeal?.Invoke(modifiedAmount);
    }

    
    
    public int GetHealth() => health;
    public int GetMaxHealth() => maxHealth;
}