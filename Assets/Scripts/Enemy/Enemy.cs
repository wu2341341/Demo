using UnityEngine;
using System.Collections.Generic;

public class Enemy : MonoBehaviour,IDamageable
{
    [Header("属性")]
    public int maxHealth = 30;
    public float moveSpeed = 2f;
    public float attackRange = 1.5f;
    public int attackDamage = 5;
    public float attackCooldown = 1.5f;

    [Header("受击反馈")]
    public float hitStunDuration = 0.5f;
    public float hitRecoverDuration = 0.5f;
    public GameObject hitEffectPrefab;

    [Header("寻路")]
    public float pathUpdateInterval = 0.3f;
    public float nodeReachDistance = 0.3f;

    [Header("卡住绕行")]
    public StuckAvoidance stuckAvoidance;

    private int currentHealth;
    private Animator animator;
    private Transform player;
    private CharacterController characterController;
    private AStarPathfinding pathfinder;

    private bool isStunned;
    private bool isAttacking;
    private float lastAttackTime;
    private List<Vector3> currentPath;
    private int currentPathIndex;
    private float lastPathUpdateTime;

    private Coroutine hitCoroutine;
    private float hitRecoverEndTime;

    private Transform attackTarget;   // 攻击时锁定的目标
    private float attackStartTime;

    private bool isDead = false;
    public bool IsDead => isDead;

    private PlayerController playerController;

    void Start()
    {
        currentHealth = maxHealth;
        animator = GetComponent<Animator>();
        lastAttackTime = -attackCooldown;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;

        characterController = GetComponent<CharacterController>();
        if (characterController == null)
            Debug.LogError($"{name} 缺少 CharacterController！");

        pathfinder = FindObjectOfType<AStarPathfinding>();
        if (pathfinder == null)
            Debug.LogWarning("场景中没有 AStarPathfinding，将使用直线追击。");

        if (animator != null)
            animator.SetFloat("Speed", 0f);

        //禁用所有 Ragdoll Collider，防止和玩家 / 自己碰撞
        DisableRagdollColliders();
        EnableRagdollKinematic();

        playerController = player.GetComponent<PlayerController>();

        if (stuckAvoidance == null) stuckAvoidance = GetComponent<StuckAvoidance>();
    }

    void Update()
    {
        if (isAttacking && Time.time - attackStartTime > 5f)
        {
            isAttacking = false;
            Debug.Log($"{name} 攻击状态超时，强制解锁");
        }

        //攻击期间持续转向玩家
        if (isAttacking && attackTarget != null)
        {
            Vector3 lookDir = player.position - transform.position;
            lookDir.y = 0;
            if (lookDir.sqrMagnitude > 0.01f)
            {
                Quaternion targetRot = Quaternion.LookRotation(lookDir.normalized);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 8f);
            }
        }

        //死亡、眩晕、攻击动画中、玩家不存在 -> 不执行任何逻辑
        if (currentHealth <= 0 || isStunned || isAttacking || player == null)
        {
            //保持 Speed 为 0，防止动画残留
            animator.SetFloat("Speed", 0f);
            return;
        }

        if (Time.time < hitRecoverEndTime)
        {
            animator.SetFloat("Speed", 0f);
            return;
        }

        //玩家已死 -> 停止追击和攻击
        if (playerController != null && playerController.IsDead)
        {
            animator.SetFloat("Speed", 0f);
            return;
        }

        float distance = Vector3.Distance(transform.position, player.position);

        //---- 攻击判断 ----
        if (distance <= attackRange && Time.time >= lastAttackTime + attackCooldown)
        {
            Attack();
            return; //攻击状态下本帧不移动
        }

        //---- 移动（追击） ----
        if (distance <= 15f)
        {
            ChasePlayer();
        }
        else
        {
            //远离玩家时，停止移动
            animator.SetFloat("Speed", 0f);
        }
    }

    void ChasePlayer()
    {
        //检查是否卡住
        if (stuckAvoidance != null)
        {
            stuckAvoidance.Tick();

            if (stuckAvoidance.IsDodging)
            {
                animator.SetFloat("Speed", 1f);  //绕行时保持移动动画
                return;
            }
        }

        //如果没有寻路组件，直接追击
        if (pathfinder == null)
        {
            MoveDirectly();
            return;
        }

        //定期更新路径
        if (Time.time >= lastPathUpdateTime + pathUpdateInterval)
        {
            lastPathUpdateTime = Time.time;
            currentPath = pathfinder.FindPath(transform.position, player.position);
            currentPathIndex = 0;
        }

        //有有效路径时沿路径移动
        if (currentPath != null && currentPath.Count > 0 && currentPathIndex < currentPath.Count)
        {
            Vector3 targetPos = currentPath[currentPathIndex];
            Vector3 direction = (targetPos - transform.position).normalized;

            if (direction.magnitude > 0.1f)
            {
                //移动
                characterController.Move(direction * moveSpeed * Time.deltaTime);
                //平滑转向
                Quaternion targetRot = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 8f);

                //动画参数：移动中
                animator.SetFloat("Speed", 1f);
            }

            //到达节点
            if (Vector3.Distance(transform.position, targetPos) < nodeReachDistance)
            {
                currentPathIndex++;
            }

            if (currentPathIndex >= currentPath.Count)
            {
                currentPath = null;
                currentPathIndex = 0;
            }
        }
        else
        {
            //路径为空或计算中，直接追击
            MoveDirectly();
        }

        //强制贴地
        if (!characterController.isGrounded)
        {
            characterController.Move(Vector3.down * 0.5f * Time.deltaTime);
        }
    }

    void MoveDirectly()
    {
        Vector3 dir = (player.position - transform.position).normalized;
        if (dir.magnitude > 0.1f)
        {
            characterController.Move(dir * moveSpeed * Time.deltaTime);
            Quaternion targetRot = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 8f);
            animator.SetFloat("Speed", 1f);
        }
        else
        {
            animator.SetFloat("Speed", 0f);
        }
    }

    void Attack()
    {
        if (isAttacking) return;
        isAttacking = true;
        lastAttackTime = Time.time;
        attackStartTime = Time.time;

        //锁定攻击目标（玩家）
        attackTarget = player;

        animator.SetTrigger("Attack");
        Debug.Log("攻击玩家！");
    }

    //---- 动画事件 ----
    public void ApplyEnemyDamage()
    {
        if (player != null)
        {
            float distance = Vector3.Distance(transform.position, player.position);
            if (distance > attackRange)
            {
                Debug.Log("玩家已离开攻击范围，攻击无效");
                return;
            }

            PlayerController pc = player.GetComponent<PlayerController>();
            if (pc != null)
            {
                bool success = pc.TakeDamage(attackDamage);
                if (success)
                    Debug.Log($"{name} 对玩家造成 {attackDamage} 伤害");
            }
        }
    }

    public void EndAttack()
    {
        isAttacking = false;
        //攻击结束后，允许移动，Speed 将在下一帧由 Update 设置
        Debug.Log($"{name} 攻击动画结束");
        attackTarget = null;
    }

    public void OnFootstep()
    {
        //空方法，接收脚步声事件
    }

    //---- 受击 ----
    public bool TakeDamage(int damage)
    {
        if (currentHealth <= 0 || isDead) return false;

        currentHealth -= damage;
        Debug.Log($"{name} 受到 {damage} 伤害，剩余 HP: {currentHealth}");

        if (hitCoroutine != null)
            StopCoroutine(hitCoroutine);

        hitCoroutine = StartCoroutine(HitReaction());

        //特效
        if (hitEffectPrefab != null)
        {
            GameObject effect = Instantiate(hitEffectPrefab, transform.position + Vector3.up * 0.8f, Quaternion.identity);
            Destroy(effect, 1f);
        }

        //伤害数字
        if (DamageNumberPool.Instance != null)
        {
            GameObject numObj = DamageNumberPool.Instance.Get();
            if (numObj != null)
            {
                DamageNumber dn = numObj.GetComponent<DamageNumber>();
                if (dn != null) dn.Show(damage, transform.position);
            }
        }

        if (currentHealth <= 0)
        {
            Die();
        }

        return true;
    }

    System.Collections.IEnumerator HitReaction()
    {
        //无条件重置攻击状态
        isAttacking = false;
        animator.ResetTrigger("Attack");

        lastAttackTime = Time.time;

        isStunned = true;
        animator.ResetTrigger("Hit");
        animator.SetTrigger("Hit");

        yield return new WaitForSeconds(hitStunDuration);

        animator.SetFloat("Speed", 0f);

        hitRecoverEndTime = Time.time + hitRecoverDuration;

        Debug.Log($"受击恢复期开始，持续到 {hitRecoverEndTime}，当前时间 {Time.time}");

        isStunned = false;
        hitCoroutine = null;
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        Debug.Log($"{name} 死亡！");

        this.enabled = false;
        if (characterController != null)
            characterController.enabled = false;

        //禁用 CharacterController 前，先启用 Ragdoll 的碰撞
        animator.enabled = false;
        EnableRagdoll();

        Destroy(gameObject, 3f);
    }

    //启用布娃娃物理
    void EnableRagdoll()
    {
        //启用所有 Collider
        Collider[] colliders = GetComponentsInChildren<Collider>(true);
        foreach (var col in colliders)
        {
            if (col is CharacterController) continue;
            col.enabled = true;

            //切换到 Corpse Layer
            col.gameObject.layer = LayerMask.NameToLayer("Corpse");
        }

        //启用物理
        Rigidbody[] rigidbodies = GetComponentsInChildren<Rigidbody>(true);
        foreach (var rb in rigidbodies)
        {
            rb.isKinematic = false;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            //Rigidbody 所在的 GameObject 也要换 Layer
            rb.gameObject.layer = LayerMask.NameToLayer("Corpse");
        }
    }

    //禁用所有非 CharacterController 的 Collider
    void DisableRagdollColliders()
    {
        Collider[] colliders = GetComponentsInChildren<Collider>(true);
        foreach (var col in colliders)
        {
            if (col is CharacterController) continue;  //跳过玩家的碰撞体
            col.enabled = false;
        }
    }

    //确保所有 Ragdoll Rigidbody 为 Kinematic
    void EnableRagdollKinematic()
    {
        Rigidbody[] rigidbodies = GetComponentsInChildren<Rigidbody>(true);
        foreach (var rb in rigidbodies)
        {
            rb.isKinematic = true;
        }
    }

    //---- 调试可视化 ----
    void OnDrawGizmosSelected()
    {
        //绘制路径
        if (currentPath != null)
        {
            Gizmos.color = Color.yellow;
            for (int i = 0; i < currentPath.Count - 1; i++)
            {
                Gizmos.DrawLine(currentPath[i], currentPath[i + 1]);
            }
        }
    }
}