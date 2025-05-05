using UnityEngine;
using DG.Tweening;

public class FadeOnAwake : MonoBehaviour
{
    public float fadeTime = 2f;
    public float delayBeforeFade = 2f;

    private CanvasGroup canvasGroup;
    private Tween fadeTween;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }

    // Update is called once per frame
    void OnEnable()
    {
        //this doesn't work. how do I kill a tween?
        if (fadeTween != null && fadeTween.IsActive())
        {
            fadeTween.Kill();
        }

        canvasGroup.alpha = 1f;

        fadeTween = canvasGroup.DOFade(0f, fadeTime)
            .SetDelay(delayBeforeFade)
            .SetEase(Ease.InOutSine)
            .OnComplete(() =>
        {
            gameObject.SetActive(false); //this part is stolen, is this how this works?
        });
    }
}
