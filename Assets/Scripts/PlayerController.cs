using System;
using System.Collections;
using Unity.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.UI.Image;

public class PlayerController : MonoBehaviour
{
    [Header("Player Health")]
    [Tooltip("X: realHp, Y: rallyHp, Z: maxHp")]
    [SerializeField] private Vector3 hp_rally_max = new(10f, 10f, 10f); // X: realHp, Y: rallyHp, Z: maxHp

    // Event signature: passes (currentHealth, currentRallyHealth, maxHealth)
    public event Action<float, float, float> OnHealthChanged;

    [Header("Horizontal Movement Settings")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float jumpForce = 15f;
    private int jumpBufferCounter;
    [SerializeField] private int jumpBufferFrames;
    private float coyoteTimeCounter;
    [SerializeField] private float coyoteTime;
    private int airJumpCounter = 0;
    [SerializeField] private int maxAirJumps;

    [Header("Ground Check Settings")]
    [SerializeField] private Transform groundCheckPoint;
    [SerializeField] private Vector2 groundCheckOffset = new(0.2f, 0.5f);
    [SerializeField] private LayerMask whatIsGround;

    [Header("Attack Settings")]
    [SerializeField] private float atkDamage = 1f;
    [SerializeField] private float atkDuration = 1f;
    private float timeSinceLastAtk = 0f;
    private float chargeTime = 0f;
    [SerializeField] [Tooltip("X:\tSide Attack Offset\nY:\tUp Attack Offset\nZ:\tDown Attack Offset")] private Vector3 atkOffset;
    [SerializeField] private Vector2 sideAtkRange, upAtkRange, downAtkRange;
    [SerializeField] private LayerMask atkLayer;
    [SerializeField] private GameObject slashEffect;

    [Header("Dash Settings")]
    [Tooltip("X: dashSpeed, Y: dashDuration, Z: dashCooldown")]
    [SerializeField] private Vector3 dashSettings = new Vector3(20f, 0.2f, 0.1f); // X: dashSpeed, Y: dashDuration, Z: dashCooldown

    // Properties: PascalCase
    private Vector3 SideAtkCenter => transform.position + new Vector3(transform.localScale.x * atkOffset.x, -0.5f, 0);
    private Vector3 UpAtkCenter => transform.position + new Vector3(0, atkOffset.y, 0);
    private Vector3 DownAtkCenter => transform.position + new Vector3(0, atkOffset.z, 0);

    [Header("Rally System")]
    [Tooltip("X:\tDelay before rally HP starts to decay\nY:\tAmount of HP to decay every time step\nZ:\tDecay HP every ... seconds")]
    [SerializeField] private Vector3 rallySettings;
    [SerializeField] [Tooltip("Amount of HP to regain when player hit success")] private float hpRegainStep;
    private Coroutine rallyCoroutine;

    [Header("Other")]
    //[SerializeField] private bool debugMode = true;
    [SerializeField] private GameObject settingsPage;
    private bool isSettingsPageActive = false;
    [SerializeField] private Vector3 latestGroundPos;

    private PlayerStateList pState;
    private Rigidbody2D rb;
    private Animator anim;

    // Input variables
    private float xAxis, yAxis;
    private bool atkChargeInput, atkReleaseInput;
    private bool dashInput;
    private bool interactInput;

    public static PlayerController Instance;
    private IInteractable interactable;
    private Vector3 respawnPoint;


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Optional: Persist across scenes
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        pState = GetComponent<PlayerStateList>();
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        isSettingsPageActive = settingsPage.activeSelf;
    }

    // Update is called once per frame
    void Update()
    {
        //if (Keyboard.current.escapeKey.wasPressedThisFrame)
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            isSettingsPageActive = !isSettingsPageActive;
            settingsPage.SetActive(isSettingsPageActive);
            rb.linearVelocity = Vector2.zero; // Stop player movement when settings page is active
        }
        if (isSettingsPageActive) return; // Prevent player actions when settings page is active

        GetInputs();
        UpdateJumpVariables();
        if (pState.dashing || !pState.alive) return; // Prevent other actions while dashing or if not alive
        Flip();
        Dash();
        Walk();
        Jump();
        Attack();
        Interact();
    }

    void GetInputs() 
    {
        xAxis = Input.GetAxisRaw("Horizontal");
        yAxis = Input.GetAxisRaw("Vertical");
        //atkInput = Input.GetButtonDown("Fire1");
        atkChargeInput = Input.GetKey(KeyCode.J);
        atkReleaseInput = Input.GetKeyUp(KeyCode.J);
        dashInput = Input.GetKeyDown(KeyCode.K);
        interactInput = Input.GetKeyDown(KeyCode.F);
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

    private void Dash()
    {
        if (dashInput && !pState.dashing) 
        {
            Debug.Log($"Dashing! {DateTime.Now}");
            pState.dashing = true;
            anim.SetTrigger("Dashing"); 
            StartCoroutine(DashCoroutine());
        }
    }

    IEnumerator DashCoroutine()
    {
        float originalGravity = rb.gravityScale;
        rb.gravityScale = 0f; // Disable gravity during dash
        rb.linearVelocity = new Vector2(transform.localScale.x * dashSettings.x, 0);
        yield return new WaitForSeconds(dashSettings.y);
        rb.gravityScale = originalGravity; // Restore original gravity
        pState.dashing = false;
        yield return new WaitForSeconds(dashSettings.z); // Cooldown before next dash
    }

    private void Walk()
    {
        if (pState.dashing) return; // Prevent walking while dashing
        rb.linearVelocity = new Vector2(xAxis * walkSpeed, rb.linearVelocity.y);
        anim.SetBool("Walking", rb.linearVelocity.x != 0 && Grounded());
    }

    public bool Grounded()
    {
        return Physics2D.Raycast(groundCheckPoint.position, Vector2.down, groundCheckOffset.y, whatIsGround)
            || Physics2D.Raycast(groundCheckPoint.position + new Vector3(groundCheckOffset.x, 0, 0), Vector2.down, groundCheckOffset.y, whatIsGround)
            || Physics2D.Raycast(groundCheckPoint.position + new Vector3(-groundCheckOffset.x, 0, 0), Vector2.down, groundCheckOffset.y, whatIsGround);
    }

    void Attack()
    {
        if (settingsPage.activeSelf) return; // Prevent attacking when settings page is active
        timeSinceLastAtk += Time.deltaTime;
        if (timeSinceLastAtk < atkDuration) return; // Prevent attacking if still in attack duration
        if (atkChargeInput)
        {
            chargeTime += Time.deltaTime; // Start charging attack
            Debug.Log($"Charging attack for {chargeTime} seconds! {DateTime.Now}");
        }
        if (atkReleaseInput)
        {
            pState.attacking = true;
            timeSinceLastAtk = 0f;
            anim.SetTrigger("Attacking");

            if (yAxis == 0)
            {
                Hit(SideAtkCenter, sideAtkRange);
                StartCoroutine(ShowSlashEffect(SideAtkCenter));
                //DisplayAttackDebug(0);
            }
            else if (yAxis > 0)
            {
                Hit(UpAtkCenter, upAtkRange);
                StartCoroutine(ShowSlashEffect(UpAtkCenter, 90));
                //DisplayAttackDebug(1);
            }
            else
            {
                Hit(DownAtkCenter, downAtkRange);
                StartCoroutine(ShowSlashEffect(DownAtkCenter, -90));
                //DisplayAttackDebug(2);
            }
            chargeTime = 0f; // Reset charge time after attack
        }
        else
        {
            pState.attacking = false;
        }
    }

    void Hit(Vector3 atkCenter, Vector3 atkRange)
    {
        Debug.Log($"Attack with {atkDamage + chargeTime} damage! {DateTime.Now}");
        Collider2D[] hitEnemies = Physics2D.OverlapBoxAll(atkCenter, atkRange, 0, atkLayer);
        foreach (Collider2D enemy in hitEnemies)
        {
            enemy.GetComponent<Enemy>()?.TakeDamage(atkDamage + chargeTime); // Adjust damage value as needed
            float previousHp = hp_rally_max.x;
            hp_rally_max.x = Mathf.Min(Mathf.Min(previousHp + hpRegainStep, hp_rally_max.y), hp_rally_max.z); // Regain HP when hitting an enemy
            if (Mathf.Abs(hp_rally_max.y-hp_rally_max.x) < 0.0001f && rallyCoroutine != null) StopRallyCoroutine(); // Stop rally countdown if realHp reaches rallyHp
            NotifyHealthChanged();
            Debug.Log($"Hit {enemy.name}! Regain {previousHp + hpRegainStep}, Rally {hp_rally_max.y}, Real {hp_rally_max.x}");
        }
    }

    IEnumerator ShowSlashEffect(Vector3 atkCenter, float angle = 0)
    {
        slashEffect.SetActive(true);
        slashEffect.transform.position = atkCenter;
        slashEffect.transform.localEulerAngles = new Vector3(0,0,angle);
        yield return new WaitForSeconds(0.2f);
        slashEffect.SetActive(false);
    }

    public void TakeDamage(float damage)
    {
        if (!pState.alive) return;
        // Implement damage logic here (e.g., reduce health, play hit animation, etc.)
        hp_rally_max.x -= damage;
        Debug.Log($"Player took {damage} damage! Remain: {hp_rally_max.x} at {DateTime.Now}");
        if (rallyCoroutine != null) StopRallyCoroutine();
        else rallyCoroutine = StartCoroutine(RallyHpCountdown());
        NotifyHealthChanged();
        if (hp_rally_max.x <= 0)
        {
            hp_rally_max.x = 0;
            pState.alive = false;
            anim.SetTrigger("Dead");
            Debug.Log($"Player is dead at {DateTime.Now}");
            UIManager.Instance.ShowDeathScene();
            rb.linearVelocity = Vector2.zero; // Stop player movement when dead
        }
    }

    IEnumerator RallyHpCountdown()
    {
        yield return new WaitForSeconds(rallySettings.x);
        while (hp_rally_max.y >= hp_rally_max.x)
        {
            hp_rally_max.y -= rallySettings.y;
            NotifyHealthChanged();
            yield return new WaitForSeconds(rallySettings.z);
        }
        hp_rally_max.y = hp_rally_max.x; // Ensure rallyHp doesn't go below realHp
        rallyCoroutine = null; // Reset the coroutine reference
        NotifyHealthChanged();
    }

    private void StopRallyCoroutine()
    {
        StopCoroutine(rallyCoroutine);
        hp_rally_max.y = hp_rally_max.x; // Reset rallyHp to realHp when taking damage
        rallyCoroutine = null;
    }

    private void NotifyHealthChanged() => OnHealthChanged?.Invoke(hp_rally_max.x, hp_rally_max.y, hp_rally_max.z);
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
            bool safeGround = Physics2D.Raycast(groundCheckPoint.position + new Vector3(groundCheckOffset.x + 0.5f, 0, 0), Vector2.down, groundCheckOffset.y, whatIsGround)
                           && Physics2D.Raycast(groundCheckPoint.position + new Vector3(-groundCheckOffset.x - 0.5f, 0, 0), Vector2.down, groundCheckOffset.y, whatIsGround);
            if (safeGround) latestGroundPos = transform.position; // Update latest ground position when grounded
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
        }

        if (Input.GetButtonDown("Jump")) jumpBufferCounter = jumpBufferFrames;
        else jumpBufferCounter--;
    }


    void Interact()
    {
        if (!interactInput) return;
        MonoBehaviour mb = interactable as MonoBehaviour;

        // Log the interaction attempt with the target's name and whether it's interactable
        string targetName = (mb != null) ? mb.gameObject.name : "null";
        string extraInfo = (mb != null) ? $" (interactable {interactable})" : "";
        Debug.Log($"Interacting with {targetName}{extraInfo} at {DateTime.Now}");

        if (interactable == null || !interactable.IsInteractable) return;
        interactable.Interact();
    }

    public void ResetPlayer()
    {
        hp_rally_max.x = hp_rally_max.z; // Reset real HP to max HP
        hp_rally_max.y = hp_rally_max.z; // Reset rally HP to max HP
        NotifyHealthChanged();
        pState.alive = true;
        anim.Play("Player_Idle"); // Reset animation to Idle
        rb.linearVelocity = Vector2.zero; // Stop player movement when respawning
        transform.position = respawnPoint; // Move player to respawn point
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

    public void MoveBackToGround()
    {
        transform.position = latestGroundPos + Vector3.up * 0.15f;
        rb.linearVelocity = Vector2.zero; // Stop player movement when moving back to latest ground
        Debug.Log($"Moved back to latest ground position: {latestGroundPos} at {DateTime.Now}");
    }

    // GETTERS & SETTERS
    public float RealHp => hp_rally_max.x;
    public float RallyHp => hp_rally_max.y;
    public float MaxHp => hp_rally_max.z;

    public void SetInteractable(IInteractable interactable)
    {
        this.interactable = interactable;
    }
    public IInteractable GetInteractable()
    {
        return interactable;
    }

    public void SetRespawnPoint(Vector3 respawnPoint)
    {
        this.respawnPoint = respawnPoint;
    }
    public Vector3 GetRespawnPoint()
    {
        return respawnPoint;
    }
}
