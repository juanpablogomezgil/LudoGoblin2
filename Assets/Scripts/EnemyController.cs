using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(HealthSystem))] // Obliga a que el objeto tenga HealthSystem
public class EnemyController : MonoBehaviour
{
    [Header("Identificación")]
    public string enemyName = "Rey Goblin";

    [Header("Estadísticas de Combate")]
    public int baseAttackDamage = 10;
    public int defendShieldAmount = 5;
    public int poisonStacksToApply = 3;

    [Header("Pesos de IA (GDD: Atacar / Defender / Envenenar)")]
    [Range(0, 100)] public int attackWeight = 60;
    [Range(0, 100)] public int defendWeight = 20;
    [Range(0, 100)] public int poisonWeight = 20;

    [Header("Animación SPUM")]
    public Animator unitAnimator;

    private HealthSystem healthSystem;
    public HealthSystem Health => healthSystem;
    public string LastActionDescription { get; private set; } = "";

    private void Awake()
    {
        healthSystem = GetComponent<HealthSystem>();
        if (unitAnimator == null && healthSystem != null)
        {
            unitAnimator = healthSystem.unitAnimator;
        }
    }

    // 1. Método principal para recibir daño directo desde las habilidades
    public void ReceiveAttack(int incomingDamage)
    {
        if (healthSystem == null || healthSystem.IsDead) return;

        // HealthSystem ya calcula Vulnerabilidad (+50%) y absorción por Escudo
        healthSystem.TakeDamage(incomingDamage);
    }

    // 2. Método para aplicar una lista de efectos de estado desde AbilitySO
    public void ApplyStatusEffects(List<StatusEffect> effects)
    {
        if (healthSystem == null || healthSystem.IsDead) return;
        healthSystem.ApplyStatusEffects(effects);
    }

    public void ApplySingleStatusEffect(StatusEffectType type, int stacksOrDuration)
    {
        if (healthSystem == null || healthSystem.IsDead) return;
        healthSystem.ApplyStatusEffect(type, stacksOrDuration);
    }

    // 3. Llamado por TurnManager al inicio/final del turno del enemigo
    public void ProcessEndOfTurnEffects()
    {
        if (healthSystem == null || healthSystem.IsDead) return;
        healthSystem.ProcessEndOfTurnEffects();
    }

    // 4. Selección y ejecución de acción de IA según el GDD
    public void ExecuteAITurn(HealthSystem playerHealth)
    {
        if (healthSystem == null || healthSystem.IsDead || playerHealth == null || playerHealth.IsDead) return;

        // Al iniciar su propio turno, el escudo temporal del turno previo se renueva/limpia
        healthSystem.ClearShield();

        // Si está aturdido (Stun), pierde la acción de este turno
        if (healthSystem.HasStatusEffect(StatusEffectType.Stun))
        {
            LastActionDescription = $"¡{enemyName} está aturdido y pierde su turno!";
            Debug.Log(LastActionDescription);
            return;
        }

        int totalWeight = Mathf.Max(1, attackWeight + defendWeight + poisonWeight);
        int roll = Random.Range(0, totalWeight);

        if (roll < attackWeight)
        {
            PerformAttack(playerHealth);
        }
        else if (roll < attackWeight + defendWeight)
        {
            PerformDefend();
        }
        else
        {
            PerformPoisonAttack(playerHealth);
        }
    }

    public void PerformAttack(HealthSystem playerHealth)
    {
        if (playerHealth == null || healthSystem == null || healthSystem.IsDead) return;

        if (unitAnimator != null)
        {
            unitAnimator.SetTrigger("2_Attack");
        }
        AudioManager.Instance?.PlayAttackSound();

        int finalDamage = baseAttackDamage;

        // Comprobamos si el enemigo tiene el estado "Debilidad" para reducir su daño a la mitad
        if (healthSystem.HasStatusEffect(StatusEffectType.Weakness))
        {
            finalDamage = Mathf.RoundToInt(finalDamage * 0.5f);
            Debug.Log($"¡Debilidad reduce el ataque de {enemyName}!");
        }

        LastActionDescription = $"{enemyName} ataca a Kael por {finalDamage} de daño.";
        Debug.Log(LastActionDescription);
        playerHealth.TakeDamage(finalDamage);
    }

    public void PerformDefend()
    {
        if (healthSystem == null || healthSystem.IsDead) return;

        healthSystem.AddShield(defendShieldAmount);
        LastActionDescription = $"{enemyName} adopta postura defensiva (+{defendShieldAmount} Escudo).";
        Debug.Log(LastActionDescription);
    }

    public void PerformPoisonAttack(HealthSystem playerHealth)
    {
        if (playerHealth == null || healthSystem == null || healthSystem.IsDead) return;

        if (unitAnimator != null)
        {
            unitAnimator.SetTrigger("2_Attack");
        }
        AudioManager.Instance?.PlayAttackSound();

        playerHealth.ApplyStatusEffect(StatusEffectType.Poison, poisonStacksToApply);
        LastActionDescription = $"{enemyName} lanza toxina a Kael (+{poisonStacksToApply} Veneno).";
        Debug.Log(LastActionDescription);
    }
}