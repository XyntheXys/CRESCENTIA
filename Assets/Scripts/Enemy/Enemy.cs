using System;
using System.Collections;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    [SerializeField] private float health = 10f;
    [SerializeField] private bool isImmortal = false; // Set to true if the enemy should not take damage

    public bool isInAnimation = false;
    public bool isPlayerInRange = false;
    public bool isDead = false;
    protected bool isAttacking = false;

    protected int direction => transform.position.x > PlayerController.Instance.transform.position.x ? -1 : 1;
    protected Vector3 originalScale;

    protected SpriteRenderer sr;
    protected Color origColor;

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

    virtual public void Attack()
    {
        if (isDead || isAttacking) return;
        isAttacking = true;
        // Add attack logic here (e.g., play animation, deal damage, etc.)
        Debug.Log($"Enemy {gameObject.name} is attacking! {DateTime.Now}");
        StartCoroutine(AttackCoroutine());
    }

    protected virtual IEnumerator AttackCoroutine()
    {
        sr.color = Color.red; // Change color to red to indicate attack
        yield return new WaitForSeconds(1f); // Adjust attack duration as needed
        sr.color = origColor; // Change color back to original
        isAttacking = false;
        isInAnimation = false;
    }

    virtual public void StartAggro()
    {
        isInAnimation = true;
        StartCoroutine(AggroCoroutine());
    }

    protected virtual IEnumerator AggroCoroutine()
    {
        sr.color = Color.orange; // Change color to red to indicate aggro
        yield return new WaitForSeconds(0.5f); // Wait for 0.5 seconds (adjust as needed)
        sr.color = origColor; // Change color back to original
        Attack(); // Start attacking after aggro
    }

    protected virtual void Start()
    {
        originalScale = transform.localScale;

        sr = GetComponent<SpriteRenderer>();
        if (sr == null) sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null) origColor = sr.color;
    }

    protected virtual void Update()
    {
        if (!isInAnimation) transform.localScale = new Vector3(direction * originalScale.x, originalScale.y, originalScale.z);
    }

    public void TakeDamage(float damage)
    {
        if (health <= 0 || isDead) return;
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
        isDead = true;
        Destroy(gameObject);
    }
}
