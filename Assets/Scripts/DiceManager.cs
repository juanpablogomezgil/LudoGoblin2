using UnityEngine;
using System.Collections.Generic;

public class DiceManager : MonoBehaviour
{
    [Header("Configuración del Dadopool")]
    public GameObject dicePrefab; // El Prefab de tu UI que tiene el DiceDraggable
    public Transform diceSpawnArea; // Un panel vacío en tu Canvas
    public int defaultDiceAmount = 3; // Tirada básica de Kael (3 a 5 según GDD)

    // Lista fundamental para rastrear qué dados están en la mesa y poder destruirlos
    private List<GameObject> activeDice = new List<GameObject>();

    public void RollAllDice()
    {
        ClearDice(); // Limpiamos la mesa por si quedaron dados del turno anterior

        AudioManager.Instance?.PlayDiceRoll();

        for (int i = 0; i < defaultDiceAmount; i++)
        {
            SpawnDice(6); // Lanzamos un d6 estándar
        }
        
        Debug.Log($"Dadopool generado: {defaultDiceAmount} dados lanzados.");
    }

    public DiceDraggable SpawnDice(int sides, SpecialDiceBonusType bonus = SpecialDiceBonusType.None, string labelPrefix = "")
    {
        if (dicePrefab == null || diceSpawnArea == null) return null;

        int rollValue = Random.Range(1, sides + 1);
        return SpawnFixedValueDice(rollValue, bonus, labelPrefix);
    }

    public DiceDraggable SpawnFixedValueDice(int fixedValue, SpecialDiceBonusType bonus = SpecialDiceBonusType.None, string labelPrefix = "")
    {
        if (dicePrefab == null || diceSpawnArea == null) return null;

        GameObject newDice = Instantiate(dicePrefab, diceSpawnArea);
        activeDice.Add(newDice);

        if (newDice.TryGetComponent(out DiceDraggable draggable))
        {
            draggable.SetupDice(fixedValue, bonus, labelPrefix);
            return draggable;
        }

        return null;
    }

    // Métodos directos para los 6 Dados Consumibles del GDD
    public void UseIronDiceBasic() => SpawnDice(6, SpecialDiceBonusType.IronArmor, "Hierro d6");
    public void UseIronDiceMaster() => SpawnDice(20, SpecialDiceBonusType.IronArmor, "Hierro d20");
    public void UseHealDiceBasic() => SpawnFixedValueDice(1, SpecialDiceBonusType.HealBasic, "Cura 1");
    public void UseHealDiceSuperior() => SpawnDice(12, SpecialDiceBonusType.HealSuperiorCleanse, "Cura+ d12");
    public void UsePoisonDiceBasic() => SpawnFixedValueDice(3, SpecialDiceBonusType.PoisonEnemy, "Veneno 3");
    public void UsePoisonDiceSuperior() => SpawnDice(12, SpecialDiceBonusType.PoisonEnemy, "Veneno d12");

    // Destruye los dados y vacía la lista
    public void ClearDice()
    {
        foreach (GameObject dice in activeDice)
        {
            if (dice != null) 
            {
                Destroy(dice);
            }
        }
        activeDice.Clear();
    }
}