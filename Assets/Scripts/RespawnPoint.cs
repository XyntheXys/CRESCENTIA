using System;
using System.Collections;
using UnityEngine;

public class RespawnPoint : InteractableBase
{
    private SpriteRenderer sr;
    Color origColor;
    protected  void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr == null) sr = GetComponentInChildren<SpriteRenderer>();
        origColor = sr.color;
    }
    public override void Interact()
    {
        Debug.Log($"set spawn point to {gameObject.name} ({transform.position}) at {DateTime.Now}");
        PlayerController.Instance.SetRespawnPoint(transform.position);
        StartCoroutine(Blink());
    }

    protected override void OnTriggerEnter2D(Collider2D collision)
    {
        sr.color = Color.blue;
        base.OnTriggerEnter2D(collision);
    }

    protected override void OnTriggerExit2D(Collider2D collision)
    {
        sr.color = origColor;
        base.OnTriggerExit2D(collision);
    }

    IEnumerator Blink()
    {
        Color blinkColor = Color.yellow;
        float blinkDuration = 0.2f; // Duration of each blink
        int blinkCount = 3; // Number of blinks
        for (int i = 0; i < blinkCount; i++)
        {
            sr.color = blinkColor;
            yield return new WaitForSeconds(blinkDuration);
            sr.color = origColor;
            yield return new WaitForSeconds(blinkDuration);
        }
    }
}
