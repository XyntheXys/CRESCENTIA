using System;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class PitfallTrigger : MonoBehaviour
{
    private void Start()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col == null) Debug.LogError($"PitfallTrigger requires a Collider2D component on {gameObject.name}");
        else col.isTrigger = true;
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;
        PlayerController p = PlayerController.Instance;
        if (p != null)
        {
            p.MoveBackToGround();
        }
    }
}
