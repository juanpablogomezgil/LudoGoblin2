using System;
using System.Collections.Generic;
using UnityEngine;

public class HealthSystem : MonoBehaviour
{
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int currentHealth;
    [SerializeField] private int currentShield = 0;

    [Header("Animación SPUM")]
    public Animator unitAnimator; // Arrastraremos el prefab de SPUM aquí

    // Eventos que se disparan al cambiar la vida, escudo, estados o al morir
    public event Action OnHealthChanged;
    public event Action<HealthSystem> OnDied;

    // Diccionario de estados alterados activos (Veneno, Vulnerabilidad, Debilidad, Aturdimiento)
    private Dictionary<StatusEffectType, int> activeEffects = new Dictionary<StatusEffectType, int>();

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public int CurrentShield => currentShield;
    public bool IsDead => currentHealth <= 0;
    public IReadOnlyDictionary<StatusEffectType, int> ActiveEffects => activeEffects;

    private void Awake()
    {
        if (currentHealth <= 0)
        {
            currentHealth = maxHealth;
        }
        if (unitAnimator == null)
        {
            unitAnimator = GetComponentInChildren<Animator>();
        }
    }

    public void InitializeHealth(int maxHp, int currentHp = -1)
    {
        maxHealth = Mathf.Max(1, maxHp);
        currentHealth = (currentHp < 0) ? maxHealth : Mathf.Clamp(currentHp, 0, maxHealth);
        currentShield = 0;
        activeEffects.Clear();
        OnHealthChanged?.Invoke();
    }

    public void TakeDamage(int amount, bool ignoreShield = false, bool ignoreVulnerability = false)
    {
        if (amount <= 0 || IsDead) return;

        int finalDamage = amount;

        // Si tiene Vulnerabilidad y no es daño directo (ej. veneno), recibe +50% de daño
        if (!ignoreVulnerability && activeEffects.ContainsKey(StatusEffectType.Vulnerability))
        {
            finalDamage = Mathf.RoundToInt(finalDamage * 1.5f);
            Debug.Log($"¡Vulnerabilidad amplifica el daño en {gameObject.name} a {finalDamage}!");
        }

        // El escudo absorbe daño antes de afectar a la salud
        if (!ignoreShield && currentShield > 0)
        {
            int absorbed = Mathf.Min(currentShield, finalDamage);
            currentShield -= absorbed;
            finalDamage -= absorbed;
            Debug.Log($"{gameObject.name} absorbe {absorbed} de daño con Escudo (Escudo restante: {currentShield}).");
        }

        if (finalDamage > 0)
        {
            currentHealth = Mathf.Max(0, currentHealth - finalDamage);

            if (unitAnimator != null)
            {
                unitAnimator.SetTrigger("3_Damaged");
            }
        }

        OnHealthChanged?.Invoke();

        if (currentHealth == 0)
        {
            Die();
        }
    }

    public void Heal(int amount)
    {
        if (amount <= 0 || IsDead) return;

        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        Debug.Log($"{gameObject.name} recupera {amount} HP ({currentHealth}/{maxHealth}).");
        OnHealthChanged?.Invoke();
    }

    public void AddShield(int amount)
    {
        if (amount <= 0 || IsDead) return;

        currentShield += amount;
        Debug.Log($"{gameObject.name} gana {amount} de Escudo (Total: {currentShield}).");
        OnHealthChanged?.Invoke();
    }

    public void ClearShield()
    {
        if (currentShield == 0) return;
        currentShield = 0;
        OnHealthChanged?.Invoke();
    }

    public void ApplyStatusEffect(StatusEffectType type, int stacksOrDuration)
    {
        if (stacksOrDuration <= 0 || IsDead) return;

        if (activeEffects.ContainsKey(type))
        {
            activeEffects[type] += stacksOrDuration;
        }
        else
        {
            activeEffects.Add(type, stacksOrDuration);
        }

        Debug.Log($"{gameObject.name} recibe estado {type} (+{stacksOrDuration}, total: {activeEffects[type]}).");
        OnHealthChanged?.Invoke();
    }

    public void ApplyStatusEffects(List<StatusEffect> effects)
    {
        if (effects == null) return;
        foreach (StatusEffect effect in effects)
        {
            ApplyStatusEffect(effect.effectType, effect.durationOrStacks);
        }
    }

    public void ClearNegativeEffects()
    {
        activeEffects.Remove(StatusEffectType.Poison);
        activeEffects.Remove(StatusEffectType.Vulnerability);
        activeEffects.Remove(StatusEffectType.Weakness);
        activeEffects.Remove(StatusEffectType.Stun);
        Debug.Log($"Se han eliminado los efectos negativos de {gameObject.name}.");
        OnHealthChanged?.Invoke();
    }

    public bool HasStatusEffect(StatusEffectType type)
    {
        return activeEffects.TryGetValue(type, out int stacks) && stacks > 0;
    }

    public int GetStatusEffectStacks(StatusEffectType type)
    {
        return activeEffects.TryGetValue(type, out int stacks) ? stacks : 0;
    }

    public void ProcessEndOfTurnEffects()
    {
        if (IsDead) return;

        // Procesar Veneno (ignora escudo y vulnerabilidad según las reglas del GDD)
        if (activeEffects.TryGetValue(StatusEffectType.Poison, out int poisonDamage) && poisonDamage > 0)
        {
            Debug.Log($"{gameObject.name} sufre {poisonDamage} de daño por Veneno.");
            TakeDamage(poisonDamage, ignoreShield: true, ignoreVulnerability: true);
            DecreaseEffectDuration(StatusEffectType.Poison);
        }

        DecreaseEffectDuration(StatusEffectType.Vulnerability);
        DecreaseEffectDuration(StatusEffectType.Weakness);
        DecreaseEffectDuration(StatusEffectType.Stun);

        OnHealthChanged?.Invoke();
    }

    public void DecreaseEffectDuration(StatusEffectType type)
    {
        if (activeEffects.ContainsKey(type))
        {
            activeEffects[type]--;
            if (activeEffects[type] <= 0)
            {
                activeEffects.Remove(type);
                Debug.Log($"El efecto {type} en {gameObject.name} se ha disipado.");
            }
        }
    }

    private void Die()
    {
        if (unitAnimator != null)
        {
            unitAnimator.SetTrigger("4_Death");
            unitAnimator.SetBool("isDeath", true);
        }
        Debug.Log($"{gameObject.name} se ha quedado sin salud.");
        OnDied?.Invoke(this);
    }
}