using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class PlayerController : MonoBehaviour, IDamageable
{
    [Header("移动")]
    public float moveSpeed = 5f;

    [Header("战斗")]
    public float attackRange = 2f;
    public int attackDamage = 10;
    public Transform attackPoint;
    public LayerMask enemyLayer;

    [Header("受击反馈")]
    public UnityEngine.UI.Image damageFlashImage;
    public float flashDuration = 0.15f;
    public float maxFlashAlpha = 0.4f;

    [Header("血量")]
    public int maxHealth = 1000;
    public float hitCooldown = 0.5f;

    [Header("血量UI")]
    public RectTransform healthFillMask;
    public TextMeshProUGUI healthText;

    [Header("连招")]
    public ComboSystem comboSystem;

    [Header("闪避")]
    public float dodgeDuration = 0.4f;
    public float dodgeSpeed = 6f;
    public float dodgeCooldown = 1f;

    [Header("受击")]
    public float hitStunDuration = 0.3f;   //受击硬直时间
    public float postHitAttackLock = 0.3f;   //受击后禁止攻击的时间
    private float attackLockEndTime;

    private bool isHurt;

    private float healthBarFullWidth;

    private Coroutine dodgeCoroutine;

    private CharacterController characterController;
    private Transform camTransform;
    private Animator animator;

    private bool isDead = false;
    public bool IsDead => isDead;

    private bool isInvincible;

    private int currentHealth;
    private float lastHitTime;
    private bool isDodging;
    private float lastDodgeTime;

    private PlayerCamera playerCamera;

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        camTransform = Camera.main.transform;
        animator = GetComponent<Animator>();

        if (characterController == null)
            Debug.LogError("玩家缺少 CharacterController 组件！");

        if (comboSystem == null)
            comboSystem = GetComponent<ComboSystem>();

        if (healthFillMask != null)
            healthBarFullWidth = healthFillMask.sizeDelta.x;

        currentHealth = maxHealth;
        UpdateHealthUI();

        playerCamera = Camera.main.GetComponent<PlayerCamera>();

        DisableRagdollColliders();
        SetRagdollKinematic(true);
    }

    void Update()
    {
        //闪避状态超时兜底
        if (isDodging && Time.time - lastDodgeTime > 1.2f)
        {
            isDodging = false;
            isInvincible = false;
            Debug.LogWarning("闪避状态超时，强制解锁");
        }

        //受击状态超时兜底
        if (isHurt && Time.time - lastHitTime > 1.0f)
        {
            isHurt = false;
            Debug.LogWarning("受击状态超时，强制解锁");
        }

        //统一用 ComboSystem 的状态
        bool isBusy = (comboSystem != null && comboSystem.IsAttacking) || isDodging || isHurt;

        //---- 移动 ----
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        Vector3 forward = camTransform.forward;
        forward.y = 0;
        forward.Normalize();
        Vector3 right = camTransform.right;
        right.y = 0;
        right.Normalize();

        Vector3 moveDir = (forward * v + right * h).normalized;

        //攻击/闪避时，动画速度设为 0，且不执行移动
        float speed = isBusy ? 0f : moveDir.magnitude;
        animator.SetFloat("Speed", speed);

        if (!isBusy && moveDir.magnitude > 0.1f)
        {
            characterController.Move(moveDir * moveSpeed * Time.deltaTime);
            Quaternion targetRot = Quaternion.LookRotation(moveDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 10f);
        }

        // 攻击
        if (Input.GetMouseButtonDown(0))
        {
            //受击后锁定时间内不能攻击
            if (Time.time < attackLockEndTime) return;

            if (isHurt) isHurt = false;
            if (!isDodging)
                comboSystem?.TryAttack();
        }

        // ---- 闪避（PerformDodge 里会清理 isHurt） ----
        if (Input.GetKeyDown(KeyCode.Space) && Time.time >= lastDodgeTime + dodgeCooldown && !isDodging)
        {
            PerformDodge();
        }
    }

    void PerformDodge()
    {
        if (comboSystem != null && comboSystem.IsAttacking)
            comboSystem.CancelAttack();

        isHurt = false;
        isDodging = true;
        isInvincible = true;   //闪避期间无敌
        lastDodgeTime = Time.time;
        animator.SetTrigger("Dodge");

        if (dodgeCoroutine != null) StopCoroutine(dodgeCoroutine);
        dodgeCoroutine = StartCoroutine(DodgeCoroutine());
    }

    System.Collections.IEnumerator DodgeCoroutine()
    {
        float elapsed = 0f;
        while (elapsed < dodgeDuration)
        {
            elapsed += Time.deltaTime;
            Vector3 dodgeDir = -transform.forward;
            characterController.Move(dodgeDir * dodgeSpeed * Time.deltaTime);
            yield return null;
        }
    }

    public void EndDodge()
    {
        isDodging = false;
        isInvincible = false;   //闪避结束，取消无敌
    }

    //---- 伤害接口（动画事件调用） ----

    public void ApplyDamage()
    {
        ApplyDamageWithMultiplier(1.0f);
    }

    public void ApplyDamageWithMultiplier(float multiplier)
    {
        Collider[] hitColliders = Physics.OverlapSphere(attackPoint.position, attackRange, enemyLayer);
        int finalDamage = Mathf.RoundToInt(attackDamage * multiplier);

        HashSet<IDamageable> hitTargets = new HashSet<IDamageable>();

        foreach (var hit in hitColliders)
        {
            IDamageable target = hit.GetComponent<IDamageable>();
            if (target == null)
                target = hit.GetComponentInParent<IDamageable>();

            if (target == null || target.IsDead || hitTargets.Contains(target))
                continue;

            hitTargets.Add(target);
            bool success = target.TakeDamage(finalDamage);
            if (success)
            {
                Debug.Log($"击中 {hit.name}，伤害 {finalDamage}");
                playerCamera?.Shake(0.15f, 0.2f + multiplier * 0.1f);
            }
        }
    }

    public bool TakeDamage(int damage)
    {
        if (isDead) return false;
        if (isInvincible) return false;
        if (Time.time < lastHitTime + hitCooldown) return false;
        if (currentHealth <= 0) return false;

        currentHealth -= damage;
        lastHitTime = Time.time;

        //受击时强制取消攻击
        if (comboSystem != null) comboSystem.CancelAttack(true);

        if (dodgeCoroutine != null)
        {
            StopCoroutine(dodgeCoroutine);
            dodgeCoroutine = null;
        }
        isDodging = false;

        //播放受击动画 + 锁定移动
        animator.ResetTrigger("Hit");
        animator.SetTrigger("Hit");
        isHurt = true;

        //设置攻击锁定时间
        attackLockEndTime = Time.time + postHitAttackLock;

        StartCoroutine(FlashDamage());
        UpdateHealthUI();
        playerCamera?.Shake(0.2f, 0.3f);

        if (currentHealth <= 0)
            Die();

        return true;
    }

    System.Collections.IEnumerator FlashDamage()
    {
        if (damageFlashImage == null) yield break;

        Color color = damageFlashImage.color;
        color.a = maxFlashAlpha;
        damageFlashImage.color = color;

        yield return new WaitForSeconds(flashDuration);

        float elapsed = 0f;
        float fadeDuration = 0.2f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(maxFlashAlpha, 0f, elapsed / fadeDuration);
            color.a = alpha;
            damageFlashImage.color = color;
            yield return null;
        }

        color.a = 0f;
        damageFlashImage.color = color;
    }

    void UpdateHealthUI()
    {
        float percent = (float)currentHealth / maxHealth;

        if (healthFillMask != null)
        {
            Vector2 size = healthFillMask.sizeDelta;
            size.x = healthBarFullWidth * percent;
            healthFillMask.sizeDelta = size;
        }

        if (healthText != null)
            healthText.text = $"{currentHealth} / {maxHealth}";
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;
        Debug.Log("玩家死亡！");

        //取消攻击、闪避
        if (comboSystem != null) comboSystem.CancelAttack(true);
        isDodging = false;

        //禁用输入和碰撞
        enabled = false;
        if (characterController != null)
            characterController.enabled = false;

        //禁用 Animator，启用 Ragdoll
        animator.enabled = false;
        EnableRagdoll();

        //死亡时禁用相机脚本
        if (playerCamera != null)
            playerCamera.enabled = false;

        //3秒后显示 Game Over 界面
        Invoke(nameof(ShowGameOverDelayed), 3f);
    }

    void DisableRagdollColliders()
    {
        Collider[] colliders = GetComponentsInChildren<Collider>(true);
        foreach (var col in colliders)
        {
            if (col is CharacterController) continue;
            col.enabled = false;
        }
    }

    void SetRagdollKinematic(bool kinematic)
    {
        Rigidbody[] rigidbodies = GetComponentsInChildren<Rigidbody>(true);
        foreach (var rb in rigidbodies)
        {
            rb.isKinematic = kinematic;
        }
    }

    void EnableRagdoll()
    {
        //启用 Collider，并切换到 Corpse Layer
        Collider[] colliders = GetComponentsInChildren<Collider>(true);
        foreach (var col in colliders)
        {
            if (col is CharacterController) continue;
            col.enabled = true;
            col.gameObject.layer = LayerMask.NameToLayer("Corpse");
        }

        //启用物理
        Rigidbody[] rigidbodies = GetComponentsInChildren<Rigidbody>(true);
        foreach (var rb in rigidbodies)
        {
            rb.isKinematic = false;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.gameObject.layer = LayerMask.NameToLayer("Corpse");
        }
    }

    void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }

    public void EndHit()
    {
        isHurt = false;
    }

    void ShowGameOverDelayed()
    {
        GameOverUI ui = FindObjectOfType<GameOverUI>();
        if (ui != null) ui.ShowGameOver(false);
    }

    //动画脚步声事件
    public void OnFootstep() { }
}