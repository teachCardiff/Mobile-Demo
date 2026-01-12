using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour
{
    [Header("Transition")]
    [Tooltip("Optional time to wait for UI exit animations to finish before activating the new scene.")]
    [Min(0f)]
    [SerializeField] private float waitBeforeActivateSeconds = 1f;

    private AsyncOperation op;
    private Coroutine loadRoutine;

    public void StartGame(string levelToLoad)
    {
        StartLoad(levelToLoad);
    }

    public void StartGame()
    {
        StartLoad(1);
    }

    private void StartLoad(string levelToLoad)
    {
        // Cancel any previous load.
        if (loadRoutine != null)
        {
            StopCoroutine(loadRoutine);
            loadRoutine = null;
        }

        loadRoutine = StartCoroutine(LoadNextSceneAsync(() => SceneManager.LoadSceneAsync(levelToLoad)));
    }

    private void StartLoad(int buildIndex)
    {
        if (loadRoutine != null)
        {
            StopCoroutine(loadRoutine);
            loadRoutine = null;
        }

        loadRoutine = StartCoroutine(LoadNextSceneAsync(() => SceneManager.LoadSceneAsync(buildIndex)));
    }

    private IEnumerator LoadNextSceneAsync(System.Func<AsyncOperation> beginLoad)
    {
        // 1) Start loading in the background.
        op = beginLoad?.Invoke();
        if (op == null)
        {
            loadRoutine = null;
            yield break;
        }

        op.allowSceneActivation = false;

        // 2) Wait until the scene is loaded to the activation point (~0.9).
        while (op.progress < 0.9f)
            yield return null;

        // 3) Wait for animations to finish (or replace this with events/callbacks).
        if (waitBeforeActivateSeconds > 0f)
            yield return new WaitForSeconds(waitBeforeActivateSeconds);

        // 4) Activate the scene.
        op.allowSceneActivation = true;

        loadRoutine = null;
    }
}
