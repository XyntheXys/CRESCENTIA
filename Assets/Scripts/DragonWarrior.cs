using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class DragonWarrior : Enemy
{
    [SerializeField] private bool allowDmgFromHitbox = true;

    [Header("Fireball Settings")]
    [SerializeField] private float fireballDuration = 3f;
    [SerializeField] private float fireballSpeed = 1.0f;
    [SerializeField] GameObject fireball;

    [Header("Flying Kick Settings")]
    [SerializeField] private Vector3 step1;
    [SerializeField] private Vector3 step2;
    [SerializeField][Tooltip("time delay for each step")] private Vector2 delayForEachStep;

    [Header("Strike Settings")]
    [SerializeField] private Vector3 setting;

    private Animator anim;

    private void Start()
    {
        anim = GetComponent<Animator>();
    }

    private void Update()
    {
        if (Keyboard.current.digit1Key.wasPressedThisFrame) Fireball();
        if (Keyboard.current.digit2Key.wasPressedThisFrame) StartCoroutine(FlyingKick());
        if (Keyboard.current.digit3Key.wasPressedThisFrame) StartCoroutine(Strike());
    }

    void Fireball()
    {
        anim.SetTrigger("Fire_Ball");
        fireball.transform.localPosition = new Vector3(1, -0.5f, 0);
        if (fireball.activeSelf) fireball.SetActive(false);
        fireball.SetActive(true);
        fireball.GetComponent<SpriteRenderer>().enabled = false;
        fireball.GetComponent<Fireball>().Shoot(fireballDuration, fireballSpeed);
    }

    IEnumerator FlyingKick()
    {
        anim.SetTrigger("Flying_Kick");
        Vector3 startPos = transform.position;

        // STEP 1: Move from ground to mid-air position
        yield return new WaitForSeconds(delayForEachStep.x); // Wait for the animation to reach the first step
        float t = 0;
        while (t < step1.z)
        {
            t += Time.deltaTime;
            transform.position = Vector3.Lerp(startPos, startPos + new Vector3(step1.x, step1.y, 0), t / step1.z);
            yield return null;
        }

        // STEP 2: Move from mid-air to landing position
        yield return new WaitForSeconds(delayForEachStep.y);
        allowDmgFromHitbox = true;
        t = 0;
        while (t < step2.z)
        {
            t += Time.deltaTime;
            transform.position = Vector3.Lerp(startPos + new Vector3(step1.x, step1.y, 0), startPos + new Vector3(step2.x, step2.y, 0), t / step2.z);
            yield return null;
        }
        allowDmgFromHitbox = false;
    }

    IEnumerator Strike()
    {
        anim.SetTrigger("Strike");
        float t = 0;
        Vector3 startPos = transform.position;

        yield return new WaitForSeconds(setting.y);
        allowDmgFromHitbox = true;
        while (t < setting.z)
        {
            t += Time.deltaTime;
            transform.position = Vector3.Lerp(startPos, startPos + new Vector3(setting.x, 0, 0), t / setting.z);
            yield return null;
        }
        allowDmgFromHitbox = false;
    }

    private void OnTriggerEnter2D(Collider2D col)
    {
        if (!col.gameObject.CompareTag("Player")) return;
        if (!allowDmgFromHitbox) return;
        PlayerController p = PlayerController.Instance;
        p.TakeDamage(1f);
    }
}
