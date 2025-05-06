using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FMODUnity;

public class PlacedGoo : MonoBehaviour
{
    public float growTime;
    private Vector3 finalScale;

    [Header("FMOD Events")]
    public EventReference impactSound;
    public EventReference destroySound;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        finalScale = transform.localScale;
        transform.localScale = Vector3.zero;
        StartCoroutine(Grow());
    }

    private IEnumerator Grow()
    {
        float timer = 0f;
        Vector3 startScale = Vector3.zero;

        while (timer < growTime)
        {
            timer += Time.deltaTime;
            float t = timer / growTime;
            transform.localScale = Vector3.Lerp(startScale, finalScale, t);
            yield return null;
        }

        transform.localScale = finalScale;
    }

    private void OnDisable()
    {
        if (!destroySound.IsNull)
        {
            RuntimeManager.PlayOneShot(destroySound, transform.position);
        }
    }
}
