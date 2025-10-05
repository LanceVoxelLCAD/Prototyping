using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FMODUnity;

public class PlacedGoo : MonoBehaviour
{
    private GameObject player;
    private PlayerController playCont;

    public float growTime;
    private Vector3 finalScale;

    public float gooRefundAmt = 5f;

    [Header("FMOD Events")]
    public EventReference impactSound;
    public EventReference destroySound;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //to refund staMana
        player = GameObject.Find("Player");
        playCont = player.GetComponent<PlayerController>();

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

        playCont.currStaMana = Mathf.Min(playCont.maxStaMana, playCont.currStaMana + gooRefundAmt);
    }
}
