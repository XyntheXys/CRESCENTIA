using System.Collections;
using UnityEngine;

public class Fireball : MonoBehaviour
{
    [SerializeField] private float damage = 1f;

    private Coroutine moveFbCoroutine;

    private void OnTriggerEnter2D(Collider2D col)
    {
        if (!col.gameObject.CompareTag("Player")) return;
        //PlayerController p = PlayerController.Instance;
        PlayerController p = col.gameObject.GetComponent<PlayerController>();
        p.TakeDamage(damage);
        StopCoroutine(moveFbCoroutine);
        StartCoroutine(Explosion());
    }

    IEnumerator Explosion()
    {
        GetComponent<Animator>().SetTrigger("Explosion");
        yield return new WaitForSeconds(0.5f);
        gameObject.SetActive(false);
    }

    public void Shoot(float duration, float speed)
    {
        moveFbCoroutine = StartCoroutine(MoveFireball(duration, speed));
    }

    private IEnumerator MoveFireball(float duration, float speed)
    {
        yield return new WaitForSeconds(0.2f);
        GetComponent<SpriteRenderer>().enabled = true;
        float t = 0f;
        while (t < duration)
        {
            transform.position += new Vector3(speed, 0, 0);
            t += Time.deltaTime;
            yield return null;
        }
        gameObject.SetActive(false);
    }
}
