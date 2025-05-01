using UnityEngine;
using DG.Tweening;

public class FadeOnAwake : MonoBehaviour
{
    public float fadeTime = 2f;
    public float delayBeforeFade = 2f;
    private CanvasGroup canvasGroup;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }

    // Update is called once per frame
    void OnEnable()
    {
        canvasGroup.alpha = 1f;

        canvasGroup.DOFade(0f, fadeTime)
            .SetDelay(delayBeforeFade)
            .SetEase(Ease.InOutSine)
            .OnComplete(() =>
        {
            gameObject.SetActive(false); //this part is stolen, is this how this works?
        });
    }
}
