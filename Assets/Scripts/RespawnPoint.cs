using System;
using System.Collections;
using UnityEngine;

public class RespawnPoint : InteractableBase
{
    public override void Interact()
    {
        Debug.Log($"set spawn point to {gameObject.name} ({transform.position}) at {DateTime.Now}");
        PlayerController.Instance.SetRespawnPoint(transform.position);
        StartCoroutine(Blink());
    }

    IEnumerator Blink()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr==null) sr = GetComponentInChildren<SpriteRenderer>();
        Color originalColor = sr.color;
        Color blinkColor = Color.yellow;
        float blinkDuration = 0.2f; // Duration of each blink
        int blinkCount = 3; // Number of blinks
        for (int i = 0; i < blinkCount; i++)
        {
            sr.color = blinkColor;
            yield return new WaitForSeconds(blinkDuration);
            sr.color = originalColor;
            yield return new WaitForSeconds(blinkDuration);
        }
    }
}
