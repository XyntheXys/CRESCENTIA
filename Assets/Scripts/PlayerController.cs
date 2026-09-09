using System;
using System.Collections;
using UnityEngine;
using static UnityEngine.UI.Image;

public class PlayerController : MonoBehaviour
{
    [Header("Player State")]
    [SerializeField] private float realHp = 10f;
    [SerializeField] private float rallyHp = 10f;
    [SerializeField] private float maxHp = 10f;

    // Event signature: passes (currentHealth, currentRallyHealth, maxHealth)
    public event Action<float, float, float> OnHealthChanged;

    [Header("Horizontal Movement Settings")]
    [SerializeField] private float walkSpeed = 5;
    [SerializeField] private float jumpForce = 15;
    private int jumpBufferCounter;
    [SerializeField] private int jumpBufferFrames;
    private float coyoteTimeCounter;
    [SerializeField] private float coyoteTime;
    private int airJumpCounter = 0;
    [SerializeField] private int maxAirJumps;

    [Header("Ground Check Settings")]
    [SerializeField] private Transform groundCheckPoint;
    [SerializeField] private float groundCheckY = 0.2f;
    [SerializeField] private float groundCheckX = 0.5f;
    [SerializeField] private LayerMask whatIsGround;

    [Header("Attack Settings")]
    [SerializeField] private float atkDamage = 1f;
    [SerializeField] private float atkDuration = 1f;
    private bool atkInput;
    private float timeSinceLastAtk = 0f;
    [SerializeField] [Tooltip("X:\tSide Attack Offset\nY:\tUp Attack Offset\nZ:\tDown Attack Offset")] private Vector3 atkOffset;
    [SerializeField] private Vector2 sideAtkRange, upAtkRange, downAtkRange;
    [SerializeField] private LayerMask atkLayer;

    // Properties: PascalCase
    private Vector3 SideAtkCenter => transform.position + new Vector3(transform.localScale.x * atkOffset.x, -0.5f, 0);
    private Vector3 UpAtkCenter => transform.position + new Vector3(0, atkOffset.y, 0);
    private Vector3 DownAtkCenter => transform.position + new Vector3(0, atkOffset.z, 0);

    [Header("Rally System")]
    [Tooltip("X:\tDelay before rally HP starts to decay\nY:\tAmount of HP to decay every time step\nZ:\tDecay HP every ... seconds")]
    [SerializeField] private Vector3 rallySettings;
    [SerializeField] [Tooltip("Amount of HP to regain when player hit success")] private float hpRegainStep;
    private Coroutine rallyCoroutine;


    private PlayerStateList pState;
    private Rigidbody2D rb;
    private float xAxis, yAxis;
    private Animator anim;

    public static PlayerController Instance;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        pState = GetComponent<PlayerStateList>();
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        GetInputs();
        UpdateJumpVariables();
        Flip();
        Move();
        Jump();
        Attack();
    }

    void GetInputs() 
    {
        xAxis = Input.GetAxisRaw("Horizontal");
        yAxis = Input.GetAxisRaw("Vertical");
        atkInput = Input.GetButtonDown("Fire1");
    }

    void Flip()
    {
        if (xAxis < 0)
        {
            transform.localScale = new Vector2(-Mathf.Abs(transform.localScale.x), transform.localScale.y);
        }
        else if (xAxis > 0)
        {
            transform.localScale = new Vector2(Mathf.Abs(transform.localScale.x), transform.localScale.y);
        }
    }

    private void Move()
    {
        rb.linearVelocity = new Vector2(xAxis * walkSpeed, rb.linearVelocity.y);
        anim.SetBool("Walking", rb.linearVelocity.x != 0 && Grounded());
    }

    public bool Grounded()
    {
        return Physics2D.Raycast(groundCheckPoint.position, Vector2.down, groundCheckY, whatIsGround)
            || Physics2D.Raycast(groundCheckPoint.position + new Vector3(groundCheckX, 0, 0), Vector2.down, groundCheckY, whatIsGround)
            || Physics2D.Raycast(groundCheckPoint.position + new Vector3(-groundCheckX, 0, 0), Vector2.down, groundCheckY, whatIsGround);
    }

    void Attack()
    {
        if (atkInput && timeSinceLastAtk >= atkDuration)
        {
            pState.attacking = true;
            timeSinceLastAtk = 0f;
            anim.SetTrigger("Attacking");

            if (yAxis == 0)
            {
                Hit(SideAtkCenter, sideAtkRange);
                DisplayAttackDebug(0);
            }
            else if (yAxis > 0)
            {
                Hit(UpAtkCenter, upAtkRange);
                DisplayAttackDebug(1);
            }
            else
            {
                Hit(DownAtkCenter, downAtkRange);
                DisplayAttackDebug(2);
            }
        }
        else
        {
            pState.attacking = false;
        }
        timeSinceLastAtk += Time.deltaTime;
    }

    private void OnDrawGizmosSelected()
    {
        // Do not draw while running in Play Mode (even if Game View Gizmos are turned on)
        if (Camera.current.cameraType == CameraType.Game) return;

        Gizmos.color = Color.red;

        // Side Attack
        Gizmos.DrawSphere(SideAtkCenter, 0.04f);            // Center
        Gizmos.DrawWireCube(SideAtkCenter, sideAtkRange);   // Range

        // Up Attack
        Gizmos.DrawSphere(UpAtkCenter, 0.04f);              // Center
        Gizmos.DrawWireCube(UpAtkCenter, upAtkRange);       // Range

        // Down Attack
        Gizmos.DrawSphere(DownAtkCenter, 0.04f);            // Center
        Gizmos.DrawWireCube(DownAtkCenter, downAtkRange);   // Range
    }

    void Hit(Vector3 atkCenter, Vector3 atkRange)
    {
        Collider2D[] hitEnemies = Physics2D.OverlapBoxAll(atkCenter, atkRange, 0, atkLayer);
        foreach (Collider2D enemy in hitEnemies)
        {
            enemy.GetComponent<Enemy>()?.Enemyhit(atkDamage); // Adjust damage value as needed
            float previousHp = realHp;
            realHp = Mathf.Min(Mathf.Min(previousHp + hpRegainStep, rallyHp), maxHp); // Regain HP when hitting an enemy
            if (Mathf.Abs(rallyHp-realHp) < 0.0001f && rallyCoroutine != null) StopRallyCoroutine(); // Stop rally countdown if realHp reaches rallyHp
            NotifyHealthChanged();
            Debug.Log($"Hit {enemy.name}! Regain {previousHp + hpRegainStep}, Rally {rallyHp}, Real {realHp}");
        }
    }

    public void TakeDamage(float damage)
    {
        // Implement damage logic here (e.g., reduce health, play hit animation, etc.)
        Debug.Log($"Player took {damage} damage! Remain: {realHp - damage} at {DateTime.Now.ToString()}");
        realHp -= damage;
        if (rallyCoroutine != null) StopRallyCoroutine();
        else rallyCoroutine = StartCoroutine(RallyHpCountdown());
        NotifyHealthChanged();
    }

    IEnumerator RallyHpCountdown()
    {
        yield return new WaitForSeconds(rallySettings.x);
        while (rallyHp >= realHp)
        {
            rallyHp -= rallySettings.y;
            NotifyHealthChanged();
            yield return new WaitForSeconds(rallySettings.z);
        }
        rallyHp = realHp; // Ensure rallyHp doesn't go below realHp
        rallyCoroutine = null; // Reset the coroutine reference
        NotifyHealthChanged();
    }

    private void StopRallyCoroutine()
    {
        StopCoroutine(rallyCoroutine);
        rallyHp = realHp; // Reset rallyHp to realHp when taking damage
        rallyCoroutine = null;
    }

    private void NotifyHealthChanged() => OnHealthChanged?.Invoke(realHp, rallyHp, maxHp);

    void Jump()
    {
        if(Input.GetButtonUp("Jump") && rb.linearVelocity.y > 0)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
            pState.jumping = false;
        }

        if (!pState.jumping)
        {
            if (jumpBufferCounter > 0 && coyoteTimeCounter>0)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
                pState.jumping = true;
            }
            else if (!Grounded() && airJumpCounter < maxAirJumps && Input.GetButtonDown("Jump"))
            {
                pState.jumping = true;
                airJumpCounter++;
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            }
        }  

        anim.SetBool("Jumping", !Grounded());
    }

    void UpdateJumpVariables()
    {
        if (Grounded())
        {
            pState.jumping = false;
            coyoteTimeCounter = coyoteTime;
            airJumpCounter = 0;
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
        }

        if (Input.GetButtonDown("Jump"))
        {
            jumpBufferCounter = jumpBufferFrames;
        }
        else
        {
            jumpBufferCounter--;
        }
    }

    void DisplayAttackDebug(int type)
    {
        switch (type)
        {
            case 0:
                float direction = transform.localScale.x > 0 ? 1 : -1;
                Debug.Log($"Attack {(direction > 0 ? "Right" : "Left")}! {DateTime.Now.ToString()}");
                Debug.DrawLine(SideAtkCenter + Vector3.up * sideAtkRange.y, SideAtkCenter + Vector3.right * sideAtkRange.x * direction, Color.red, atkDuration);
                Debug.DrawLine(SideAtkCenter - Vector3.up * sideAtkRange.y, SideAtkCenter + Vector3.right * sideAtkRange.x * direction, Color.red, atkDuration);
                break;
            case 1:
                Debug.Log($"Attack Up! {DateTime.Now.ToString()}");
                Debug.DrawLine(UpAtkCenter - Vector3.right * upAtkRange.x, UpAtkCenter + Vector3.up * upAtkRange.y, Color.red, atkDuration);
                Debug.DrawLine(UpAtkCenter + Vector3.right * upAtkRange.x, UpAtkCenter + Vector3.up * upAtkRange.y, Color.red, atkDuration);
                break;
            case 2:
                Debug.Log($"Attack Down! {DateTime.Now.ToString()}");
                Debug.DrawLine(DownAtkCenter - Vector3.right * downAtkRange.x, DownAtkCenter - Vector3.up * downAtkRange.y, Color.red, atkDuration);
                Debug.DrawLine(DownAtkCenter + Vector3.right * downAtkRange.x, DownAtkCenter - Vector3.up * downAtkRange.y, Color.red, atkDuration);
                break;
        }
    }

    // GETTERS & SETTERS
    public float RealHp => realHp;
    public float RallyHp => rallyHp;
    public float MaxHp => maxHp;
}
