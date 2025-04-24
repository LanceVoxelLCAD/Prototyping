using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlacedGoo : MonoBehaviour
{
    public float growTime;
    private Vector3 finalScale;

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
}
