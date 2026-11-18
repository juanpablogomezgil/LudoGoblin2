using UnityEngine;
using System.Collections.Generic;

public enum RequirementType
{
    ExactValue,
    MinimumValue,
    MaximumValue,
    SumExactly
}

public enum StatusEffectType
{
    Poison,         
    Stun,           
    Weakness,       
    Vulnerability   
}

[System.Serializable] 
public struct StatusEffect
{
    public StatusEffectType effectType;
    public int durationOrStacks; 
}

[CreateAssetMenu(fileName = "NewAbility", menuName = "LudoGoblin/Ability")]
public class AbilitySO : ScriptableObject
{
    [Header("Información Visual")]
    public string abilityName = "Habilidad";
    [TextArea(2, 4)]
    public string description;
    public Sprite icon;

    [Header("Requisitos de Activación")]
    public RequirementType requirement;
    public int targetValue = 1; 
    public int maxDiceSlots = 1; 

    [Header("Efectos Numéricos Base")]
    [Tooltip("Si se marca, el daño total será el valor de los dados sumado al Daño Base.")]
    public bool useDiceValueAsDamage; 
    public int baseDamageAmount;
    public bool useDiceValueAsHeal;
    public int healAmount;
    public bool useDiceValueAsShield;
    public int shieldAmount;

    [Header("Efectos de Estado (Buffs/Debuffs)")]
    public List<StatusEffect> statusEffects = new List<StatusEffect>();

    public int GetDiceSum(int[] droppedDiceValues)
    {
        if (droppedDiceValues == null) return 0;
        int sum = 0;
        foreach (int val in droppedDiceValues) sum += val;
        return sum;
    }

    // 1. Método para validar si la jugada es legal
    public bool IsMoveValid(int[] droppedDiceValues)
    {
        if (droppedDiceValues == null || droppedDiceValues.Length == 0) return false;
        if (droppedDiceValues.Length > maxDiceSlots) return false;

        int sum = GetDiceSum(droppedDiceValues);

        return requirement switch
        {
            RequirementType.ExactValue => droppedDiceValues.Length == 1 && droppedDiceValues[0] == targetValue,
            RequirementType.MinimumValue => sum >= targetValue,
            RequirementType.MaximumValue => sum <= targetValue && droppedDiceValues.Length >= 1,
            RequirementType.SumExactly => sum == targetValue,
            _ => false
        };
    }

    // Comprueba si ya es imposible cumplir el requisito añadiendo más dados positivos
    public bool IsImpossibleToComplete(int[] droppedDiceValues)
    {
        if (droppedDiceValues == null || droppedDiceValues.Length == 0) return false;

        int sum = GetDiceSum(droppedDiceValues);

        return requirement switch
        {
            RequirementType.ExactValue => droppedDiceValues[0] != targetValue,
            RequirementType.MaximumValue => sum > targetValue,
            RequirementType.SumExactly => sum > targetValue,
            _ => false
        };
    }

    // 2. Método para calcular el daño en el momento que se ejecuta la habilidad
    public int CalculateDamage(int[] droppedDiceValues)
    {
        int totalDamage = baseDamageAmount;

        if (useDiceValueAsDamage && droppedDiceValues != null)
        {
            totalDamage += GetDiceSum(droppedDiceValues);
        }

        return totalDamage;
    }

    public int CalculateHeal(int[] droppedDiceValues)
    {
        int totalHeal = healAmount;
        if (useDiceValueAsHeal && droppedDiceValues != null)
        {
            totalHeal += GetDiceSum(droppedDiceValues);
        }
        return totalHeal;
    }

    public int CalculateShield(int[] droppedDiceValues)
    {
        int totalShield = shieldAmount;
        if (useDiceValueAsShield && droppedDiceValues != null)
        {
            totalShield += GetDiceSum(droppedDiceValues);
        }
        return totalShield;
    }

    public string GetRequirementSummary()
    {
        string slotsInfo = maxDiceSlots > 1 ? $" (hasta {maxDiceSlots} dados)" : " (1 dado)";
        return requirement switch
        {
            RequirementType.ExactValue => $"Dado exacto = {targetValue}",
            RequirementType.MinimumValue => $"Suma >= {targetValue}{slotsInfo}",
            RequirementType.MaximumValue => $"Suma <= {targetValue}{slotsInfo}",
            RequirementType.SumExactly => $"Suma exacta = {targetValue}{slotsInfo}",
            _ => $"Requisito: {targetValue}"
        };
    }

    public string GetEffectSummary()
    {
        List<string> parts = new List<string>();

        if (baseDamageAmount > 0 && useDiceValueAsDamage)
            parts.Add($"Daño: {baseDamageAmount} + Dados");
        else if (useDiceValueAsDamage)
            parts.Add("Daño: Suma de Dados");
        else if (baseDamageAmount > 0)
            parts.Add($"Daño: {baseDamageAmount}");

        if (healAmount > 0 && useDiceValueAsHeal)
            parts.Add($"Cura: {healAmount} + Dados");
        else if (useDiceValueAsHeal)
            parts.Add("Cura: Suma de Dados");
        else if (healAmount > 0)
            parts.Add($"Cura: {healAmount} HP");

        if (shieldAmount > 0 && useDiceValueAsShield)
            parts.Add($"Escudo: {shieldAmount} + Dados");
        else if (useDiceValueAsShield)
            parts.Add("Escudo: Suma de Dados");
        else if (shieldAmount > 0)
            parts.Add($"Escudo: +{shieldAmount}");

        if (statusEffects != null)
        {
            foreach (var ef in statusEffects)
            {
                string efName = ef.effectType switch
                {
                    StatusEffectType.Poison => "Veneno",
                    StatusEffectType.Vulnerability => "Vulnerabilidad",
                    StatusEffectType.Weakness => "Debilidad",
                    StatusEffectType.Stun => "Aturdir",
                    _ => ef.effectType.ToString()
                };
                parts.Add($"+{ef.durationOrStacks} {efName}");
            }
        }

        return parts.Count > 0 ? string.Join(" | ", parts) : description;
    }
}