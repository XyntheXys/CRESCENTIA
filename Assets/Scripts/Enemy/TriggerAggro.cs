using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class TriggerAggro : MonoBehaviour
{
    Enemy enemy;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        enemy = GetComponentInParent<Enemy>();
        GetComponent<Collider2D>().isTrigger = true;
    }

    public void OnTriggerEnter2D(Collider2D collider)
    {
        if (enemy.isDead) return;
        if (collider.CompareTag("Player"))
        {
            enemy.isPlayerInRange = true;
            enemy.StartAggro();
        }
    }

    public void OnTriggerExit2D(Collider2D collider)
    {
        if (enemy.isDead) return;
        if (collider.CompareTag("Player"))
        {
            enemy.isPlayerInRange = false;
        }
    }
}
