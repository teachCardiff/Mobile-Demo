using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    [System.Serializable]
    public class SceneMusic
    {
        [Tooltip("Scene name as shown in Build Settings.")]
        public string sceneName;

        public AudioClip clip;

        [Min(0f)]
        public float volume = 1f;

        [Min(0f)]
        public float fadeInSeconds = 0.5f;

        [Min(0f)]
        public float fadeOutSeconds = 0.5f;

        [Tooltip("If true, continues playing the current clip when entering this scene if it's already playing.")]
        public bool keepIfAlreadyPlaying = false;
    }

    [Header("Audio")]
    [SerializeField] private AudioSource source;

    [Tooltip("Music mapping by scene name.")]
    [SerializeField] private List<SceneMusic> musicByScene = new List<SceneMusic>();

    [Tooltip("Scene music to use if no match is found.")]
    [SerializeField] private SceneMusic fallback;

    [Header("Behavior")]
    [SerializeField] private bool dontDestroyOnLoad = true;

    [Tooltip("If true, MusicManager will fade out when TransitionController starts a transition.")]
    [SerializeField] private bool listenToTransitionController = true;

    [Tooltip("If true, will also react to SceneManager.sceneLoaded (useful even without TransitionController).")]
    [SerializeField] private bool listenToSceneLoaded = true;

    private Coroutine fadeRoutine;
    private string currentSceneName;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        if (dontDestroyOnLoad)
            DontDestroyOnLoad(gameObject);

        if (source == null)
        {
            source = GetComponent<AudioSource>();
            if (source == null)
                source = gameObject.AddComponent<AudioSource>();
        }

        source.loop = true;
        source.playOnAwake = false;
    }

    private void OnEnable()
    {
        if (listenToSceneLoaded)
            SceneManager.sceneLoaded += OnSceneLoaded;

        if (listenToTransitionController)
        {
            TransitionController.TransitionStarted += OnTransitionStarted;
            TransitionController.SceneActivated += OnSceneActivated;
        }
    }

    private void OnDisable()
    {
        if (listenToSceneLoaded)
            SceneManager.sceneLoaded -= OnSceneLoaded;

        if (listenToTransitionController)
        {
            TransitionController.TransitionStarted -= OnTransitionStarted;
            TransitionController.SceneActivated -= OnSceneActivated;
        }
    }

    private void Start()
    {
        // Start with whatever scene is already active.
        var active = SceneManager.GetActiveScene();
        currentSceneName = active.name;
        PlayForScene(active.name, fadeIn: true);
    }

    private void OnTransitionStarted()
    {
        // Fade out current music while the next scene loads.
        var cfg = FindConfig(currentSceneName) ?? fallback;
        float fadeOut = cfg != null ? cfg.fadeOutSeconds : 0.5f;
        FadeOut(fadeOut);
    }

    private void OnSceneActivated(Scene scene)
    {
        currentSceneName = scene.name;
        PlayForScene(scene.name, fadeIn: true);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // If TransitionController is driving transitions, SceneActivated will handle fade-in.
        // This is a fallback for non-transition loads.
        if (listenToTransitionController)
            return;

        currentSceneName = scene.name;
        PlayForScene(scene.name, fadeIn: true);
    }

    private void PlayForScene(string sceneName, bool fadeIn)
    {
        var cfg = FindConfig(sceneName) ?? fallback;
        if (cfg == null || cfg.clip == null)
            return;

        bool alreadyPlayingSame = source.isPlaying && source.clip == cfg.clip;
        if (alreadyPlayingSame && cfg.keepIfAlreadyPlaying)
            return;

        StopFade();

        source.clip = cfg.clip;
        source.volume = 0f;
        source.Play();

        if (fadeIn)
            FadeTo(cfg.volume, cfg.fadeInSeconds);
        else
            source.volume = cfg.volume;
    }

    private SceneMusic FindConfig(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName) || musicByScene == null)
            return null;

        for (int i = 0; i < musicByScene.Count; i++)
        {
            var m = musicByScene[i];
            if (m != null && m.sceneName == sceneName)
                return m;
        }

        return null;
    }

    public void FadeOut(float seconds)
    {
        FadeTo(0f, seconds);
    }

    public void FadeTo(float targetVolume, float seconds)
    {
        StopFade();
        fadeRoutine = StartCoroutine(FadeRoutine(targetVolume, seconds));
    }

    private void StopFade()
    {
        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
            fadeRoutine = null;
        }
    }

    private IEnumerator FadeRoutine(float targetVolume, float seconds)
    {
        float start = source != null ? source.volume : 0f;

        if (seconds <= 0f)
        {
            source.volume = targetVolume;
            if (Mathf.Approximately(targetVolume, 0f))
                source.Stop();
            fadeRoutine = null;
            yield break;
        }

        float t = 0f;
        while (t < seconds)
        {
            t += Time.unscaledDeltaTime;
            float u = Mathf.Clamp01(t / seconds);
            source.volume = Mathf.Lerp(start, targetVolume, u);
            yield return null;
        }

        source.volume = targetVolume;
        if (Mathf.Approximately(targetVolume, 0f))
            source.Stop();

        fadeRoutine = null;
    }
}
