using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Fades the screen to black, loads a scene, then fades back in.
// No setup needed: it builds its own full-screen black overlay the first time it is used.
public class SceneFader : MonoBehaviour
{
    private const float DelayBeforeFade = 0.5f; // wait before the screen starts going black
    private const float Duration = 0.4f;        // time to fade out, and again to fade in
    private const float HoldBlack = 0.3f;       // stay fully black after the new scene has loaded
    private const float QuickDuration = 0.25f;  // Transition(): fade time for changing level inside the same scene
    private const float QuickHoldBlack = 0.1f;

    private static SceneFader instance;

    private CanvasGroup group;
    private bool busy;

    public static void FadeTo(string sceneName) => Get().Begin(sceneName, -1);

    public static void FadeTo(int buildIndex) => Get().Begin(null, buildIndex);

    public static void Reload() => FadeTo(SceneManager.GetActiveScene().buildIndex);

    // Fades to black, runs onBlack (e.g. reset the level), then fades back in. Stays in the same scene.
    public static void Transition(System.Action onBlack)
    {
        SceneFader fader = Get();
        if (fader.busy)
        {
            onBlack(); // already fading (e.g. scene just loaded): just do it
            return;
        }
        fader.StartCoroutine(fader.RunTransition(onBlack));
    }

    // After the last scene in Build Settings this goes back to scene 0 (main menu)
    public static void FadeToNext()
    {
        int next = SceneManager.GetActiveScene().buildIndex + 1;
        FadeTo(next < SceneManager.sceneCountInBuildSettings ? next : 0);
    }

    private static SceneFader Get()
    {
        if (instance != null) return instance;

        var go = new GameObject("SceneFader");
        DontDestroyOnLoad(go);

        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999; // above every other UI

        var image = new GameObject("Black").AddComponent<Image>();
        image.transform.SetParent(go.transform, false);
        image.color = Color.black;
        image.rectTransform.anchorMin = Vector2.zero;
        image.rectTransform.anchorMax = Vector2.one;
        image.rectTransform.offsetMin = Vector2.zero;
        image.rectTransform.offsetMax = Vector2.zero;

        instance = go.AddComponent<SceneFader>();
        instance.group = go.AddComponent<CanvasGroup>();
        instance.group.alpha = 0f;
        instance.group.blocksRaycasts = false;
        return instance;
    }

    private void Begin(string sceneName, int buildIndex)
    {
        if (busy) return; // already changing scene
        StartCoroutine(Run(sceneName, buildIndex));
    }

    private IEnumerator Run(string sceneName, int buildIndex)
    {
        busy = true;
        group.blocksRaycasts = true; // no clicking buttons mid-fade
        yield return new WaitForSecondsRealtime(DelayBeforeFade);
        yield return Fade(0f, 1f, Duration);

        Time.timeScale = 1f;
        yield return sceneName != null ? SceneManager.LoadSceneAsync(sceneName) : SceneManager.LoadSceneAsync(buildIndex);
        yield return new WaitForSecondsRealtime(HoldBlack);

        yield return Fade(1f, 0f, Duration);
        group.blocksRaycasts = false;
        busy = false;
    }

    private IEnumerator RunTransition(System.Action onBlack)
    {
        busy = true;
        group.blocksRaycasts = true;
        yield return Fade(0f, 1f, QuickDuration);

        onBlack();
        yield return new WaitForSecondsRealtime(QuickHoldBlack);

        yield return Fade(1f, 0f, QuickDuration);
        group.blocksRaycasts = false;
        busy = false;
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        // Unscaled so it still works while the game is paused (Time.timeScale = 0)
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / duration)
        {
            group.alpha = Mathf.Lerp(from, to, t);
            yield return null;
        }
        group.alpha = to;
    }
}
