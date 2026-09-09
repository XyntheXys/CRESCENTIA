using UnityEngine;

public class Enemy : MonoBehaviour
{
    [SerializeField] private float health = 10f;
    [SerializeField] private bool isImmortal = false; // Set to true if the enemy should not take damage

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            PlayerController player = collision.gameObject.GetComponent<PlayerController>();
            if (player != null)
            {
                player.TakeDamage(1f); // Adjust damage value as needed
            }
        }
    }

    public void Enemyhit(float damage)
    {
        if (health <= 0) return;
        //Debug.Log($"Enemy took {damage} damage! Remain: {health - damage} at {System.DateTime.Now.ToString()}");
        health -= damage;
        if (health <= 0 && !isImmortal)
        {
            Die();
        }
    }

    void Die()
    {
        // Add death logic here (e.g., play animation, drop loot, etc.)
        Destroy(gameObject);
    }
}
