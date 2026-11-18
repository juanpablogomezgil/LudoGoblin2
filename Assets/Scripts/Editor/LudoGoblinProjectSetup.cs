#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class LudoGoblinProjectSetup
{
    private const string MainMenuScenePath = "Assets/Scenes/MainMenuScene.unity";
    private const string BuildScenePath = "Assets/Scenes/BuildScene.unity";
    private const string CombatScenePath = "Assets/Scenes/SampleScene.unity";

    static LudoGoblinProjectSetup()
    {
        EditorApplication.delayCall += EnsureProjectConfiguredAutomatically;
    }

    private static void EnsureProjectConfiguredAutomatically()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;

        bool needsSceneSetup = !File.Exists(MainMenuScenePath) || !File.Exists(BuildScenePath);
        if (needsSceneSetup)
        {
            SetupAllScenesAndAssets();
        }
        else
        {
            EnsureBuildSettingsRegistered();
        }
    }

    [MenuItem("LudoGoblin/Configurar Escenas y Datos del GDD")]
    public static void SetupAllScenesAndAssets()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Sal del modo Play antes de configurar las escenas.");
            return;
        }

        CreateDefaultAbilityAssets();
        EnsureAudioManagerPrefabExists();

        string previousScenePath = SceneManager.GetActiveScene().path;

        if (!File.Exists(MainMenuScenePath))
        {
            Scene menuScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            GameObject ctrl = new GameObject("MainMenuController");
            ctrl.AddComponent<MainMenuController>();
            EditorSceneManager.SaveScene(menuScene, MainMenuScenePath);
            Debug.Log($"[LudoGoblin] Escena creada: {MainMenuScenePath}");
        }

        if (!File.Exists(BuildScenePath))
        {
            Scene buildScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            GameObject ctrl = new GameObject("BuildScreenController");
            ctrl.AddComponent<BuildScreenController>();
            EditorSceneManager.SaveScene(buildScene, BuildScenePath);
            Debug.Log($"[LudoGoblin] Escena creada: {BuildScenePath}");
        }

        EnsureBuildSettingsRegistered();

        if (!string.IsNullOrEmpty(previousScenePath) && File.Exists(previousScenePath))
        {
            EditorSceneManager.OpenScene(previousScenePath, OpenSceneMode.Single);
        }
        else if (File.Exists(CombatScenePath))
        {
            EditorSceneManager.OpenScene(CombatScenePath, OpenSceneMode.Single);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[LudoGoblin] Proyecto configurado con éxito (MainMenuScene, BuildScene, SampleScene y Habilidades del GDD).");
    }

    private static void EnsureBuildSettingsRegistered()
    {
        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>();

        if (File.Exists(MainMenuScenePath))
            scenes.Add(new EditorBuildSettingsScene(MainMenuScenePath, true));
        if (File.Exists(BuildScenePath))
            scenes.Add(new EditorBuildSettingsScene(BuildScenePath, true));
        if (File.Exists(CombatScenePath))
            scenes.Add(new EditorBuildSettingsScene(CombatScenePath, true));

        if (scenes.Count > 0)
        {
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }

    private static void CreateDefaultAbilityAssets()
    {
        string folder = "Assets/Scripts/Abilities";
        if (!AssetDatabase.IsValidFolder(folder)) return;

        AbilitySO baseAttack = AssetDatabase.LoadAssetAtPath<AbilitySO>($"{folder}/BaseAttack.asset");
        Sprite defaultIcon = baseAttack != null ? baseAttack.icon : null;

        CreateOrUpdateAbilityAsset($"{folder}/EscudoFerreo.asset", "Escudo Férreo",
            "Otorga Escudo igual a la suma de dados + 3.", defaultIcon,
            RequirementType.MinimumValue, 4, 2, false, 0, false, 0, true, 3, null);

        CreateOrUpdateAbilityAsset($"{folder}/CuracionArcana.asset", "Curación Arcana",
            "Recupera HP igual al dado + 3.", defaultIcon,
            RequirementType.MinimumValue, 3, 1, false, 0, true, 3, false, 0, null);

        CreateOrUpdateAbilityAsset($"{folder}/DagaVenenosa.asset", "Daga Venenosa",
            "Inflige 3 de daño y aplica 3 cargas de Veneno.", defaultIcon,
            RequirementType.MaximumValue, 4, 1, false, 3, false, 0, false, 0,
            new List<StatusEffect> { new StatusEffect { effectType = StatusEffectType.Poison, durationOrStacks = 3 } });

        CreateOrUpdateAbilityAsset($"{folder}/BolaDeFuego.asset", "Bola de Fuego",
            "Inflige 6 + suma de dados y aplica 2 cargas de Vulnerabilidad.", defaultIcon,
            RequirementType.MinimumValue, 9, 2, true, 6, false, 0, false, 0,
            new List<StatusEffect> { new StatusEffect { effectType = StatusEffectType.Vulnerability, durationOrStacks = 2 } });
    }

    private static void CreateOrUpdateAbilityAsset(
        string assetPath, string abName, string desc, Sprite icon,
        RequirementType req, int targetVal, int maxSlots,
        bool useDmg, int baseDmg, bool useHeal, int baseHeal, bool useShield, int baseShield,
        List<StatusEffect> effects)
    {
        if (File.Exists(assetPath)) return;

        AbilitySO ab = ScriptableObject.CreateInstance<AbilitySO>();
        ab.abilityName = abName;
        ab.description = desc;
        ab.icon = icon;
        ab.requirement = req;
        ab.targetValue = targetVal;
        ab.maxDiceSlots = maxSlots;
        ab.useDiceValueAsDamage = useDmg;
        ab.baseDamageAmount = baseDmg;
        ab.useDiceValueAsHeal = useHeal;
        ab.healAmount = baseHeal;
        ab.useDiceValueAsShield = useShield;
        ab.shieldAmount = baseShield;
        if (effects != null) ab.statusEffects = effects;

        AssetDatabase.CreateAsset(ab, assetPath);
    }

    [MenuItem("LudoGoblin/Crear o Actualizar Prefab de AudioManager")]
    public static void EnsureAudioManagerPrefabExists()
    {
        string resourcesFolder = "Assets/Resources";
        if (!AssetDatabase.IsValidFolder(resourcesFolder))
        {
            AssetDatabase.CreateFolder("Assets", "Resources");
        }

        string prefabPath = $"{resourcesFolder}/AudioManager.prefab";
        if (!File.Exists(prefabPath))
        {
            GameObject tempGo = new GameObject("AudioManager");
            tempGo.AddComponent<AudioManager>();
            PrefabUtility.SaveAsPrefabAsset(tempGo, prefabPath);
            Object.DestroyImmediate(tempGo);
            Debug.Log($"[LudoGoblin] Prefab de AudioManager creado en: {prefabPath}");
        }

        string prefabsFolder = "Assets/Prefabs";
        string secondaryPrefabPath = $"{prefabsFolder}/AudioManager.prefab";
        if (AssetDatabase.IsValidFolder(prefabsFolder) && !File.Exists(secondaryPrefabPath))
        {
            AssetDatabase.CopyAsset(prefabPath, secondaryPrefabPath);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}
#endif
