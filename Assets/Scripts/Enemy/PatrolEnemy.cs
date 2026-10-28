using System.Collections;
using UnityEngine;

public class PatrolEnemy : Enemy
{
    [SerializeField] private Animator anim;
    [SerializeField] private float atkTime = 5f;
    [SerializeField] private BoxCollider2D aggroRange;
    [SerializeField] private BoxCollider2D attackRange;
    [SerializeField] private float approchSpeed = 5f;

    [Header("Patrol Settings")]
    [SerializeField] private float walkSpeed = 2f;
    [SerializeField] private Transform[] patrolPoints;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    protected override void Start()
    {
        base.Start();
        if (anim == null) anim = GetComponent<Animator>();
        if (anim == null) anim = GetComponentInChildren<Animator>();
    }

    public override void Attack()
    {
        StartCoroutine(AttackCoroutine());
    }

    protected override IEnumerator AttackCoroutine()
    {
        if (anim != null) anim.SetTrigger("Attacking");
        yield return new WaitForSeconds(atkTime);
        if (isPlayerInRange) StartCoroutine(AttackCoroutine());
        else
        {
            isAttacking = false;
            isInAnimation = false;
        }
    }

    public override void StartAggro()
    {
        base.StartAggro();
    }

    //IEnumerator ApprochAndAttack()
    //{
    //    while (PlayerController.Instance.transform.position)
    //}
}
