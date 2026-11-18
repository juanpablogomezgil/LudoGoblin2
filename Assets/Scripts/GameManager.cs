using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

[Serializable]
public class ConsumableDiceEntry
{
    public string id;
    public string displayName;
    public string description;
    public int sides; // 0 = valor fijo
    public int fixedValue;
    public SpecialDiceBonusType bonusType;
    public int price;
    public int count;
}

[Serializable]
public class LudoGoblinSaveData
{
    public int currentRoom = 1;
    public int maxRooms = 4; // Salas 1-3 esbirros/chamán, Sala 4 Rey Goblin Brujo
    public int playerCurrentHealth = 100;
    public int playerMaxHealth = 100;
    public int gold = 30;
    public int rouletteSegmentCount = 8;
    public List<string> equippedAbilityNames = new List<string>();
    public List<int> consumableCounts = new List<int>();
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public const string MainMenuSceneName = "MainMenuScene";
    public const string BuildSceneName = "BuildScene";
    public const string CombatSceneName = "SampleScene";

    [Header("Estado de la Run (Roguelike)")]
    public int currentRoom = 1;
    public int maxRooms = 4;
    public int playerCurrentHealth = 100;
    public int playerMaxHealth = 100;
    public int gold = 30;
    [Range(4, 10)] public int rouletteSegmentCount = 8;

    [Header("Catálogo y Build de Habilidades")]
    public List<AbilitySO> allAbilitiesCatalog = new List<AbilitySO>();
    public List<AbilitySO> equippedRouletteAbilities = new List<AbilitySO>();

    [Header("Inventario de Dados Consumibles (GDD)")]
    public List<ConsumableDiceEntry> consumableDiceCatalog = new List<ConsumableDiceEntry>();

    public bool LastRunEndedInVictory { get; set; } = false;
    public bool ShowEpilogueOnMenu { get; set; } = false;

    private string SaveFilePath => Path.Combine(Application.persistentDataPath, "ludogoblin_save.json");

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoCreateGameManager()
    {
        if (Instance == null)
        {
            GameObject go = new GameObject("GameManager");
            Instance = go.AddComponent<GameManager>();
            DontDestroyOnLoad(go);
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeCatalogsIfNeeded();
    }

    public void InitializeCatalogsIfNeeded()
    {
        if (consumableDiceCatalog.Count == 0)
        {
            consumableDiceCatalog = new List<ConsumableDiceEntry>
            {
                new ConsumableDiceEntry
                {
                    id = "iron_basic",
                    displayName = "Dado de Hierro",
                    description = "Genera 1d6 y otorga el valor salido como Armadura al usarse.",
                    sides = 6,
                    fixedValue = 0,
                    bonusType = SpecialDiceBonusType.IronArmor,
                    price = 15,
                    count = 1
                },
                new ConsumableDiceEntry
                {
                    id = "iron_master",
                    displayName = "Dado Hierro Maestro",
                    description = "Genera 1d20 y otorga el valor salido como Armadura al usarse.",
                    sides = 20,
                    fixedValue = 0,
                    bonusType = SpecialDiceBonusType.IronArmor,
                    price = 35,
                    count = 0
                },
                new ConsumableDiceEntry
                {
                    id = "heal_basic",
                    displayName = "Dado Curación Básico",
                    description = "Genera un dado de valor 1 que cura al jugador al usarse.",
                    sides = 0,
                    fixedValue = 1,
                    bonusType = SpecialDiceBonusType.HealBasic,
                    price = 12,
                    count = 1
                },
                new ConsumableDiceEntry
                {
                    id = "heal_superior",
                    displayName = "Dado Curación Superior",
                    description = "Genera 1d12, cura el valor salido y elimina efectos negativos.",
                    sides = 12,
                    fixedValue = 0,
                    bonusType = SpecialDiceBonusType.HealSuperiorCleanse,
                    price = 30,
                    count = 0
                },
                new ConsumableDiceEntry
                {
                    id = "poison_basic",
                    displayName = "Dado Veneno Básico",
                    description = "Genera un dado de valor 3 que envenena al enemigo al usarse.",
                    sides = 0,
                    fixedValue = 3,
                    bonusType = SpecialDiceBonusType.PoisonEnemy,
                    price = 15,
                    count = 1
                },
                new ConsumableDiceEntry
                {
                    id = "poison_superior",
                    displayName = "Dado Veneno Superior",
                    description = "Genera 1d12 y envenena al enemigo con el valor que ha salido.",
                    sides = 12,
                    fixedValue = 0,
                    bonusType = SpecialDiceBonusType.PoisonEnemy,
                    price = 32,
                    count = 0
                }
            };
        }

        if (allAbilitiesCatalog.Count == 0)
        {
            BuildDefaultAbilityCatalog();
        }

        SyncEquippedRouletteSize();
    }

    private void BuildDefaultAbilityCatalog()
    {
        allAbilitiesCatalog.Clear();

        allAbilitiesCatalog.Add(CreateAbility("Ataque Base", "Inflige 3 de daño más la suma de dados (Req: Suma >= 6).",
            RequirementType.MinimumValue, 6, 2, true, 3, false, 0, false, 0));

        allAbilitiesCatalog.Add(CreateAbility("Golpe Doble", "Suma 7 o más con hasta 2 dados para infligir la suma como daño.",
            RequirementType.MinimumValue, 7, 2, true, 0, false, 0, false, 0));

        allAbilitiesCatalog.Add(CreateAbility("Escudo Férreo", "Otorga Escudo igual al valor del dado + 3 (Req: Suma >= 4).",
            RequirementType.MinimumValue, 4, 2, false, 0, false, 0, true, 3));

        allAbilitiesCatalog.Add(CreateAbility("Curación Arcana", "Recupera HP igual al dado + 3 (Req: Suma >= 3).",
            RequirementType.MinimumValue, 3, 1, false, 0, true, 3, false, 0));

        AbilitySO poisonDagger = CreateAbility("Daga Venenosa", "Inflige 3 de daño y aplica 3 cargas de Veneno (Req: Dado <= 4).",
            RequirementType.MaximumValue, 4, 1, false, 3, false, 0, false, 0);
        poisonDagger.statusEffects.Add(new StatusEffect { effectType = StatusEffectType.Poison, durationOrStacks = 3 });
        allAbilitiesCatalog.Add(poisonDagger);

        AbilitySO fireball = CreateAbility("Bola de Fuego", "Inflige 6 + suma de dados y aplica 2 de Vulnerabilidad (Req: Suma >= 9).",
            RequirementType.MinimumValue, 9, 2, true, 6, false, 0, false, 0);
        fireball.statusEffects.Add(new StatusEffect { effectType = StatusEffectType.Vulnerability, durationOrStacks = 2 });
        allAbilitiesCatalog.Add(fireball);

        AbilitySO soulStrike = CreateAbility("Apuesta Letal", "Si la suma es exactamente 7, inflige 16 de daño y Debilidad.",
            RequirementType.SumExactly, 7, 2, false, 16, false, 0, false, 0);
        soulStrike.statusEffects.Add(new StatusEffect { effectType = StatusEffectType.Weakness, durationOrStacks = 2 });
        allAbilitiesCatalog.Add(soulStrike);

        AbilitySO stunHammer = CreateAbility("Martillo de Casino", "Si sacas un 6 exacto, inflige 8 de daño y Aturde 1 turno.",
            RequirementType.ExactValue, 6, 1, false, 8, false, 0, false, 0);
        stunHammer.statusEffects.Add(new StatusEffect { effectType = StatusEffectType.Stun, durationOrStacks = 1 });
        allAbilitiesCatalog.Add(stunHammer);
    }

    private AbilitySO CreateAbility(
        string name, string desc,
        RequirementType req, int targetVal, int maxSlots,
        bool useDmg, int baseDmg, bool useHeal, int baseHeal, bool useShield, int baseShield)
    {
        AbilitySO ab = ScriptableObject.CreateInstance<AbilitySO>();
        ab.name = name;
        ab.abilityName = name;
        ab.description = desc;
        ab.requirement = req;
        ab.targetValue = targetVal;
        ab.maxDiceSlots = maxSlots;
        ab.useDiceValueAsDamage = useDmg;
        ab.baseDamageAmount = baseDmg;
        ab.useDiceValueAsHeal = useHeal;
        ab.healAmount = baseHeal;
        ab.useDiceValueAsShield = useShield;
        ab.shieldAmount = baseShield;
        return ab;
    }

    public void SyncEquippedRouletteSize()
    {
        rouletteSegmentCount = Mathf.Clamp(rouletteSegmentCount, 4, 10);
        if (allAbilitiesCatalog.Count == 0) return;

        while (equippedRouletteAbilities.Count < rouletteSegmentCount)
        {
            AbilitySO nextAb = allAbilitiesCatalog[equippedRouletteAbilities.Count % allAbilitiesCatalog.Count];
            equippedRouletteAbilities.Add(nextAb);
        }

        while (equippedRouletteAbilities.Count > rouletteSegmentCount)
        {
            equippedRouletteAbilities.RemoveAt(equippedRouletteAbilities.Count - 1);
        }
    }

    public void StartNewRun()
    {
        InitializeCatalogsIfNeeded();
        currentRoom = 1;
        playerMaxHealth = 100;
        playerCurrentHealth = 100;
        gold = 30;
        rouletteSegmentCount = 8;
        ShowEpilogueOnMenu = false;

        equippedRouletteAbilities.Clear();
        SyncEquippedRouletteSize();

        // Resetear dados consumibles iniciales
        for (int i = 0; i < consumableDiceCatalog.Count; i++)
        {
            consumableDiceCatalog[i].count = (i == 0 || i == 2 || i == 4) ? 1 : 0;
        }

        SaveRun();
    }

    public void OnCombatVictory(int remainingPlayerHp)
    {
        playerCurrentHealth = Mathf.Clamp(remainingPlayerHp, 1, playerMaxHealth);
        int goldReward = 25 + (currentRoom * 10);
        gold += goldReward;

        if (currentRoom >= maxRooms)
        {
            // ¡Derrotado el Rey Goblin Brujo! Victoria de la partida
            DeleteSaveFile();
            LastRunEndedInVictory = true;
            ShowEpilogueOnMenu = true;
            LoadSceneSafe(MainMenuSceneName, CombatSceneName);
        }
        else
        {
            currentRoom++;
            SaveRun();
            LoadSceneSafe(BuildSceneName, CombatSceneName);
        }
    }

    public void OnCombatDefeatPermadeath()
    {
        // Permadeath: borra el guardado y muestra el epílogo de derrota
        DeleteSaveFile();
        LastRunEndedInVictory = false;
        ShowEpilogueOnMenu = true;
        LoadSceneSafe(MainMenuSceneName, CombatSceneName);
    }

    public void LoadSceneSafe(string preferredSceneName, string fallbackSceneName)
    {
        if (Application.CanStreamedLevelBeLoaded(preferredSceneName))
        {
            SceneManager.LoadScene(preferredSceneName);
        }
        else
        {
            SceneManager.LoadScene(fallbackSceneName);
        }
    }

    public bool HasSavedRun()
    {
        return File.Exists(SaveFilePath);
    }

    public void SaveRun()
    {
        try
        {
            LudoGoblinSaveData data = new LudoGoblinSaveData
            {
                currentRoom = currentRoom,
                maxRooms = maxRooms,
                playerCurrentHealth = playerCurrentHealth,
                playerMaxHealth = playerMaxHealth,
                gold = gold,
                rouletteSegmentCount = rouletteSegmentCount
            };

            foreach (AbilitySO ab in equippedRouletteAbilities)
            {
                data.equippedAbilityNames.Add(ab != null ? ab.abilityName : "");
            }

            foreach (ConsumableDiceEntry entry in consumableDiceCatalog)
            {
                data.consumableCounts.Add(entry.count);
            }

            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(SaveFilePath, json);
            Debug.Log($"[SaveSystem] Partida guardada en Sala {currentRoom} (Oro: {gold}, HP: {playerCurrentHealth}/{playerMaxHealth}).");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"No se pudo guardar la partida: {e.Message}");
        }
    }

    public bool LoadRun()
    {
        if (!HasSavedRun()) return false;

        try
        {
            InitializeCatalogsIfNeeded();
            string json = File.ReadAllText(SaveFilePath);
            LudoGoblinSaveData data = JsonUtility.FromJson<LudoGoblinSaveData>(json);
            if (data == null) return false;

            currentRoom = data.currentRoom;
            maxRooms = data.maxRooms;
            playerCurrentHealth = data.playerCurrentHealth;
            playerMaxHealth = data.playerMaxHealth;
            gold = data.gold;
            rouletteSegmentCount = Mathf.Clamp(data.rouletteSegmentCount, 4, 10);

            equippedRouletteAbilities.Clear();
            foreach (string abName in data.equippedAbilityNames)
            {
                AbilitySO match = allAbilitiesCatalog.Find(a => a.abilityName == abName);
                if (match != null) equippedRouletteAbilities.Add(match);
            }
            SyncEquippedRouletteSize();

            for (int i = 0; i < consumableDiceCatalog.Count && i < data.consumableCounts.Count; i++)
            {
                consumableDiceCatalog[i].count = data.consumableCounts[i];
            }

            return true;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Error al cargar partida: {e.Message}");
            return false;
        }
    }

    public void DeleteSaveFile()
    {
        if (File.Exists(SaveFilePath))
        {
            File.Delete(SaveFilePath);
            Debug.Log("[SaveSystem] Guardado eliminado (Permadeath / Fin de Run).");
        }
    }
}
