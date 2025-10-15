// GlobalHighlightPulse.cs
using System.Collections;
using UnityEngine;

public class GlobalHighlightPulse : MonoBehaviour
{
    [Header("Input")]
    public KeyCode triggerKey = KeyCode.Q;

    [Header("Timing")]
    public float duration = 1.0f;
    public AnimationCurve curve = AnimationCurve.EaseInOut(0, 0, 1, 0);

    [Header("Look")]
    public Color color = Color.cyan;
    public float intensity = 6f; // multiplies curve

    static readonly int ID_H = Shader.PropertyToID("_GlobalHighlight");
    static readonly int ID_C = Shader.PropertyToID("_GlobalHighlightColor");

    Coroutine pulse;

    void Update()
    {
        if (Input.GetKeyDown(triggerKey))
        {
            if (pulse != null) StopCoroutine(pulse);
            pulse = StartCoroutine(Pulse());
        }
    }

    IEnumerator Pulse()
    {
        Shader.SetGlobalColor(ID_C, color);
        float t = 0f;
        while (t < duration)
        {
            float k = Mathf.Clamp01(curve.Evaluate(t / Mathf.Max(0.0001f, duration)));
            Shader.SetGlobalFloat(ID_H, k * intensity);
            t += Time.unscaledDeltaTime;
            yield return null;
        }
        Shader.SetGlobalFloat(ID_H, 0f);
        pulse = null;
    }
}
