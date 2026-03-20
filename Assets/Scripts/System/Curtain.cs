using System;
using System.Collections;
using UnityEngine;


public class Curtain : Singleton<Curtain>
{
    [SerializeField] private CanvasGroup canvasGroup;
    private Coroutine ongoingCoroutine;    // keep track of this, to stop current transition if need to start a new one
    public bool FadedToBlack { get; private set; }
    
    public void LongTransition()
    {
        if (ongoingCoroutine != null)
        {
            StopCoroutine(ongoingCoroutine);
        }
        ongoingCoroutine = StartCoroutine(FadeTransitionCoroutine(1f, 1f, 1f));
    }

    public void ShortTransition()
    {
        if (ongoingCoroutine != null)
        {
            StopCoroutine(ongoingCoroutine);
        }
        ongoingCoroutine = StartCoroutine(FadeTransitionCoroutine(0.5f, 0.5f, 0.5f));
    }

    private IEnumerator FadeTransitionCoroutine(float fadeInDuration, float fadeOutDuration, float blackDuration)
    {
        FadedToBlack = false;
        
        // fade to black
        float timer = 0f;
        while (timer < fadeInDuration)
        {
            timer += Time.deltaTime;
            canvasGroup.alpha = Mathf.Clamp01(timer / fadeInDuration);
            yield return null;
        }

        canvasGroup.alpha = 1f;
        FadedToBlack = true;

        // stay black
        yield return new WaitForSeconds(blackDuration);

        // fade from black
        timer = 0f;
        while (timer < fadeOutDuration)
        {
            timer += Time.deltaTime;
            canvasGroup.alpha = 1f - Mathf.Clamp01(timer / fadeOutDuration);
            yield return null;
        }

        canvasGroup.alpha = 0f;
        FadedToBlack = false;
    }

}