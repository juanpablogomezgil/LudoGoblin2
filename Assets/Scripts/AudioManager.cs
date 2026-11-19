using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Gestor central de audio para LudoGoblin.
/// Controla la reproducción de música en bucle adaptativa por escena
/// y efectos de sonido (SFX) para combate, dados, ruleta y UI.
/// Todos los clips y volúmenes son configurables desde el Inspector de Unity.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Ajustes Generales de Volumen")]
    [Range(0f, 1f)] public float masterVolume = 1f;
    [Range(0f, 1f)] public float musicVolume = 0.75f;
    [Range(0f, 1f)] public float sfxVolume = 1f;

    [Tooltip("Aplica una leve variación aleatoria de tono para evitar que los efectos repetitivos cansen")]
    public bool randomizeSfxPitch = true;
    [Range(0f, 0.2f)] public float pitchVariation = 0.05f;

    [Header("Música por Escena (En Loop)")]
    [Tooltip("Música para el Menú Principal (MainMenuScene)")]
    public AudioClip mainMenuMusic;
    [Tooltip("Música para la Pantalla de Build y Tienda (BuildScene)")]
    public AudioClip buildSceneMusic;
    [Tooltip("Música para el Combate del Casino-Mazmorra (SampleScene)")]
    public AudioClip combatMusic;
    [Tooltip("Música de reserva en caso de no definir una específica")]
    public AudioClip defaultMusic;
    [Tooltip("Duración de la transición suave al cambiar de pista")]
    public float musicFadeDuration = 0.8f;

    [Header("SFX: Combate y Habilidades")]
    [Tooltip("Sonido de ataque de Kael o del enemigo")]
    public AudioClip attackSound;
    [Tooltip("Sonido al recibir daño")]
    public AudioClip hurtSound;
    [Tooltip("Sonido al bloquear daño o activar Escudo/Armadura")]
    public AudioClip shieldBlockSound;
    [Tooltip("Sonido de curación (HP recuperado)")]
    public AudioClip healSound;
    [Tooltip("Sonido al morir una unidad o Kael")]
    public AudioClip deathSound;

    [Header("SFX: Ruleta")]
    [Tooltip("Sonido al iniciar el giro o loop de giro")]
    public AudioClip rouletteSpinSound;
    [Tooltip("Sonido al detenerse la ruleta y fijar la habilidad")]
    public AudioClip rouletteStopSound;

    [Header("SFX: Dados y Dadopool")]
    [Tooltip("Sonido al rodar los dados del Dadopool")]
    public AudioClip diceRollSound;
    [Tooltip("Sonido al encajar con éxito un dado en una ranura")]
    public AudioClip dicePlaceSound;
    [Tooltip("Sonido al levantar/agarrar un dado")]
    public AudioClip dicePickupSound;
    [Tooltip("Sonido cuando un dado regresa a la mesa (jugada rechazada)")]
    public AudioClip diceReturnSound;
    [Tooltip("Sonido al usar un dado consumible especial")]
    public AudioClip specialDiceUseSound;

    [Header("SFX: Interfaz y Flujo de Juego")]
    [Tooltip("Sonido genérico de clic en botones")]
    public AudioClip buttonClickSound;
    [Tooltip("Sonido de compra en la tienda o posada")]
    public AudioClip shopBuySound;
    [Tooltip("Sonido de victoria al superar un combate")]
    public AudioClip victorySound;
    [Tooltip("Sonido de derrota permanente (Permadeath)")]
    public AudioClip defeatSound;

    // Componentes de audio internos
    private AudioSource musicSource;
    private AudioSource sfxSource;
    private AudioSource rouletteLoopSource;
    private Coroutine musicFadeCoroutine;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoInitialize()
    {
        if (Instance != null) return;

        // Intentamos cargar un Prefab existente en Resources/AudioManager si el desarrollador lo creó
        GameObject prefab = Resources.Load<GameObject>("AudioManager");
        if (prefab != null)
        {
            GameObject instantiated = Instantiate(prefab);
            instantiated.name = "AudioManager";
            DontDestroyOnLoad(instantiated);
            return;
        }

        // Si no hay prefab, creamos el GameObject de forma automática en tiempo de ejecución
        GameObject go = new GameObject("AudioManager");
        go.AddComponent<AudioManager>();
        DontDestroyOnLoad(go);
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

        SetupAudioSources();
        LoadSavedVolumePreferences();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        // Reproduce la música de la escena activa inicial
        PlayMusicForScene(SceneManager.GetActiveScene().name);
    }

    private void SetupAudioSources()
    {
        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.playOnAwake = false;
            musicSource.loop = true;
            musicSource.volume = musicVolume * masterVolume;
        }

        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.loop = false;
            sfxSource.volume = sfxVolume * masterVolume;
        }

        if (rouletteLoopSource == null)
        {
            rouletteLoopSource = gameObject.AddComponent<AudioSource>();
            rouletteLoopSource.playOnAwake = false;
            rouletteLoopSource.loop = true;
            rouletteLoopSource.volume = sfxVolume * masterVolume;
        }
    }
    //Para que el inspector actualice los volúmenes en tiempo real al cambiar los sliders
    private void OnValidate()
    {
        UpdateSourceVolumes();
    }


    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        PlayMusicForScene(scene.name);
    }

    // ==========================================
    // REPRODUCTOR DE MÚSICA POR ESCENA
    // ==========================================

    /// <summary>
    /// Selecciona y reproduce la canción correspondiente según el nombre de la escena.
    /// </summary>
    public void PlayMusicForScene(string sceneName)
    {
        AudioClip targetTrack = null;

        if (sceneName == GameManager.MainMenuSceneName || sceneName.Contains("MainMenu"))
        {
            targetTrack = mainMenuMusic != null ? mainMenuMusic : defaultMusic;
        }
        else if (sceneName == GameManager.BuildSceneName || sceneName.Contains("Build"))
        {
            targetTrack = buildSceneMusic != null ? buildSceneMusic : defaultMusic;
        }
        else if (sceneName == GameManager.CombatSceneName || sceneName.Contains("Sample") || sceneName.Contains("Combat"))
        {
            targetTrack = combatMusic != null ? combatMusic : defaultMusic;
        }
        else
        {
            targetTrack = defaultMusic;
        }

        PlayMusic(targetTrack);
    }

    /// <summary>
    /// Reproduce una pista de música con transición suave (fade).
    /// </summary>
    public void PlayMusic(AudioClip clip)
    {
        if (musicSource == null) SetupAudioSources();

        // Si ya está sonando la misma canción, no la reiniciamos
        if (musicSource.clip == clip && musicSource.isPlaying) return;

        if (musicFadeCoroutine != null)
        {
            StopCoroutine(musicFadeCoroutine);
        }

        musicFadeCoroutine = StartCoroutine(FadeMusicRoutine(clip, musicFadeDuration));
    }

    private IEnumerator FadeMusicRoutine(AudioClip newClip, float duration)
    {
        float targetVolume = musicVolume * masterVolume;
        float halfDuration = Mathf.Max(0.05f, duration * 0.5f);

        // 1. Fade out de la música anterior si estaba sonando
        if (musicSource.isPlaying && musicSource.volume > 0.01f)
        {
            float startVol = musicSource.volume;
            float elapsed = 0f;
            while (elapsed < halfDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                musicSource.volume = Mathf.Lerp(startVol, 0f, elapsed / halfDuration);
                yield return null;
            }
            musicSource.volume = 0f;
            musicSource.Stop();
        }

        // 2. Si no hay nueva pista asignada, terminamos en silencio
        if (newClip == null)
        {
            musicSource.clip = null;
            yield break;
        }

        // 3. Fade in de la nueva pista
        musicSource.clip = newClip;
        musicSource.loop = true;
        musicSource.Play();

        float inElapsed = 0f;
        while (inElapsed < halfDuration)
        {
            inElapsed += Time.unscaledDeltaTime;
            musicSource.volume = Mathf.Lerp(0f, targetVolume, inElapsed / halfDuration);
            yield return null;
        }

        musicSource.volume = targetVolume;
    }

    public void StopMusic()
    {
        if (musicSource != null)
        {
            musicSource.Stop();
            musicSource.clip = null;
        }
    }

    // ==========================================
    // REPRODUCCIÓN DE EFECTOS DE SONIDO (SFX)
    // ==========================================

    public void PlaySFX(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null) return;
        if (sfxSource == null) SetupAudioSources();

        if (randomizeSfxPitch)
        {
            sfxSource.pitch = 1f + UnityEngine.Random.Range(-pitchVariation, pitchVariation);
        }
        else
        {
            sfxSource.pitch = 1f;
        }

        float finalVol = Mathf.Clamp01(volumeScale * sfxVolume * masterVolume);
        sfxSource.PlayOneShot(clip, finalVol);
    }

    // Métodos semánticos para combate
    public void PlayAttackSound() => PlaySFX(attackSound);
    public void PlayHurtSound() => PlaySFX(hurtSound);
    public void PlayShieldBlockSound() => PlaySFX(shieldBlockSound);
    public void PlayHealSound() => PlaySFX(healSound);
    public void PlayDeathSound() => PlaySFX(deathSound);

    // Métodos para ruleta
    public void PlayRouletteSpin()
    {
        if (rouletteSpinSound != null && rouletteLoopSource != null)
        {
            rouletteLoopSource.clip = rouletteSpinSound;
            rouletteLoopSource.volume = sfxVolume * masterVolume;
            rouletteLoopSource.loop = true;
            rouletteLoopSource.Play();
        }
        else
        {
            PlaySFX(rouletteSpinSound);
        }
    }

    public void PlayRouletteStop()
    {
        if (rouletteLoopSource != null && rouletteLoopSource.isPlaying)
        {
            rouletteLoopSource.Stop();
        }
        PlaySFX(rouletteStopSound);
    }

    // Métodos para dados
    public void PlayDiceRoll() => PlaySFX(diceRollSound);
    public void PlayDicePlace() => PlaySFX(dicePlaceSound);
    public void PlayDicePickup() => PlaySFX(dicePickupSound);
    public void PlayDiceReturn() => PlaySFX(diceReturnSound);
    public void PlaySpecialDiceUseSound() => PlaySFX(specialDiceUseSound);

    // Métodos para interfaz y flujo
    public void PlayButtonClick() => PlaySFX(buttonClickSound);
    public void PlayShopBuySound() => PlaySFX(shopBuySound);
    public void PlayVictorySound() => PlaySFX(victorySound);
    public void PlayDefeatSound() => PlaySFX(defeatSound);

    // Control de volúmenes dinámicos
    public void SetMasterVolume(float value)
    {
        masterVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat("Settings_MasterVolume", masterVolume);
        UpdateSourceVolumes();
    }

    public void SetMusicVolume(float value)
    {
        musicVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat("Settings_MusicVolume", musicVolume);
        UpdateSourceVolumes();
    }

    public void SetSfxVolume(float value)
    {
        sfxVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat("Settings_SfxVolume", sfxVolume);
        UpdateSourceVolumes();
    }

    public void LoadSavedVolumePreferences()
    {
        if (PlayerPrefs.HasKey("Settings_MasterVolume"))
        {
            masterVolume = Mathf.Clamp01(PlayerPrefs.GetFloat("Settings_MasterVolume", 1f));
        }
        if (PlayerPrefs.HasKey("Settings_MusicVolume"))
        {
            musicVolume = Mathf.Clamp01(PlayerPrefs.GetFloat("Settings_MusicVolume", 0.75f));
        }
        if (PlayerPrefs.HasKey("Settings_SfxVolume"))
        {
            sfxVolume = Mathf.Clamp01(PlayerPrefs.GetFloat("Settings_SfxVolume", 1f));
        }
        UpdateSourceVolumes();
    }

    private void UpdateSourceVolumes()
    {
        if (musicSource != null)
        {
            musicSource.volume = musicVolume * masterVolume;
        }
        if (sfxSource != null)
        {
            sfxSource.volume = sfxVolume * masterVolume;
        }
        if (rouletteLoopSource != null)
        {
            rouletteLoopSource.volume = sfxVolume * masterVolume;
        }
    }
}
