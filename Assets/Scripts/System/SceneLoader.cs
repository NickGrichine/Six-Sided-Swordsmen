using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : Singleton<SceneLoader>
{
    private Coroutine ongoingCoroutine;

    private void Start()
    {
        LoadScene("Title");
    }

    public void LoadScene(string sceneName)
    {
        if (ongoingCoroutine != null)
        {
            StopCoroutine(ongoingCoroutine);
        }
        ongoingCoroutine = StartCoroutine(LoadSceneCoroutine(sceneName));
    }

    private IEnumerator LoadSceneCoroutine(string sceneName)
    {
        Curtain.Instance.LongTransition();

        // wait until the screen is fully black before changing scenes
        yield return new WaitUntil(() => Curtain.Instance.FadedToBlack);

        SceneManager.LoadScene(sceneName);
    }
}
