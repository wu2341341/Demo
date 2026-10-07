using UnityEngine;
using BehaviorTree;
using System.Collections.Generic;

public class BossController : MonoBehaviour, IDamageable
{
    [Header("Boss 属性")]
    public int maxHealth = 100;
    public float moveSpeed = 3f;
    public float attackCooldown = 1.5f;
    public float detectionRange = 15f;

    [Header("寻路")]
    public float pathUpdateInterval = 0.3f;
    public float nodeReachDistance = 0.3f;

    private AStarPathfinding pathfinder;
    private List<Vector3> currentPath;
    private int currentPathIndex;
    private float lastPathUpdateTime;

    private StuckAvoidance stuckAvoidance;

    [Header("阶段配置")]
    public float phase2Threshold = 0.6f;
    public float phase3Threshold = 0.3f;

    [Header("Phase2 - 防御")]
    public float defenseDamageReduction = 0.5f;

    [Header("模块引用")]
    public BossSummoner summoner;
    public BossCombat combat;

    [Header("受击特效")]
    public GameObject hitEffectPrefab;

    //内部状态
    private int currentHealth;
    private Transform player;
    private Animator animator;
    private CharacterController characterController;
    private Blackboard blackboard;
    private BTNode behaviorTree;
    private BossPhase currentPhase;

    private float lastAttackTime;
    private float attackStartTime;
    private bool isDefending;
    private bool isAttacking;
    private bool isDead;

    public bool IsDead => isDead;
    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;

    private GridManager gridManager;

    private PlayerController playerController;

    void Start()
    {
        currentHealth = maxHealth;
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        animator = GetComponent<Animator>();
        characterController = GetComponent<CharacterController>();

        if (player == null)
            Debug.LogError("Boss: 场景中没有找到 Player 标签的对象！");

        pathfinder = FindObjectOfType<AStarPathfinding>();
        if (pathfinder == null)
            Debug.LogWarning("场景中没有 AStarPathfinding，Boss 将使用直线追击。");

        //自动获取模块
        if (summoner == null) summoner = GetComponent<BossSummoner>();
        if (combat == null) combat = GetComponent<BossCombat>();

        //初始化战斗模块
        combat?.Initialize(player);

        //初始化黑板
        InitBlackboard();

        //构建行为树
        BuildBehaviorTree();

        Debug.Log($"Boss 初始化完成，HP: {currentHealth}/{maxHealth}");

        DisableRagdollColliders();
        SetRagdollKinematic(true);

        gridManager = FindObjectOfType<GridManager>();
        playerController = player.GetComponent<PlayerController>();
        stuckAvoidance = GetComponent<StuckAvoidance>();
    }

    void InitBlackboard()
    {
        blackboard = new Blackboard();
        blackboard.Set(BBKeys.Player, player);
        blackboard.Set(BBKeys.Transform, transform);
        blackboard.Set(BBKeys.Animator, animator);
        blackboard.Set(BBKeys.CharacterController, characterController);
        blackboard.Set(BBKeys.MoveSpeed, moveSpeed);
        blackboard.Set(BBKeys.AttackRange, combat != null ? combat.attackRange : 2.5f);
        blackboard.Set(BBKeys.AttackDamage, combat != null ? combat.attackDamage : 15);
        blackboard.Set(BBKeys.AttackCooldown, attackCooldown);
        blackboard.Set(BBKeys.DetectionRange, detectionRange);
        blackboard.Set(BBKeys.Phase, BossPhase.Phase1);
    }

    void BuildBehaviorTree()
    {
        behaviorTree = BossBTBuilder.Build(
            phaseProvider: () => currentPhase,
            onPhase1: Phase1Behavior,
            onPhase2: Phase2Behavior,
            onPhase3: Phase3Behavior,
            blackboard: blackboard
        );
    }

    void Update()
    {
        if (currentHealth <= 0 || player == null) return;

        //玩家已死->停止行为树
        if (playerController != null && playerController.IsDead)
        {
            animator.SetFloat("Speed", 0f);
            return;
        }

        UpdatePhase();

        //攻击超时兜底
        if (isAttacking && Time.time - attackStartTime > 5f)
        {
            isAttacking = false;
        }

        if (isAttacking)
        {
            Vector3 lookDir = player.position - transform.position;
            lookDir.y = 0;
            if (lookDir.sqrMagnitude > 0.01f)
            {
                Quaternion targetRot = Quaternion.LookRotation(lookDir.normalized);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 8f);
            }
            animator.SetFloat("Speed", 0f);
            return;
        }

        behaviorTree.Execute();
    }

    void UpdatePhase()
    {
        float healthPercent = (float)currentHealth / maxHealth;
        BossPhase newPhase;

        if (healthPercent <= phase3Threshold) newPhase = BossPhase.Phase3;
        else if (healthPercent <= phase2Threshold) newPhase = BossPhase.Phase2;
        else newPhase = BossPhase.Phase1;

        if (newPhase != currentPhase)
        {
            currentPhase = newPhase;
            blackboard.Set(BBKeys.Phase, currentPhase);
            behaviorTree.Reset();
            Debug.Log($"Boss 进入阶段: {currentPhase} (HP: {healthPercent:P0})");
            OnPhaseChanged(currentPhase);
        }
    }

    void OnPhaseChanged(BossPhase phase)
    {
        switch (phase)
        {
            case BossPhase.Phase2:
                Debug.Log("Boss 进入 Phase2：防御 + 召唤（注册为动态障碍物）");
                gridManager?.RegisterDynamicObstacle(transform);   //注册
                summoner?.SummonBatch(5);
                break;

            case BossPhase.Phase3:
                Debug.Log("Boss 进入 Phase3：狂暴 + 召唤 2 个小兵");
                gridManager?.UnregisterDynamicObstacle(transform); //取消注册

                summoner?.CancelCurrentSummon();
                isDefending = false;
                isAttacking = false;
                animator.SetBool("IsDefending", false);
                animator.ResetTrigger("Attack");
                animator.ResetTrigger("Summon");

                summoner?.SummonBatch(2);
                break;
        }
    }

    //---- 各阶段行为 ----
    BTStatus Phase1Behavior()
    {
        Transform target = blackboard.Get<Transform>(BBKeys.Player);
        float range = blackboard.Get<float>(BBKeys.AttackRange);
        float speed = blackboard.Get<float>(BBKeys.MoveSpeed);
        float cooldown = blackboard.Get<float>(BBKeys.AttackCooldown);

        float distance = Vector3.Distance(transform.position, target.position);

        if (distance <= range && Time.time >= lastAttackTime + cooldown)
        {
            PerformAttack();
            return BTStatus.Success;
        }
        else if (distance <= detectionRange)
        {
            MoveTowards(target, speed);
            return BTStatus.Running;
        }

        animator.SetFloat("Speed", 0f);
        return BTStatus.Success;
    }

    BTStatus Phase2Behavior()
    {
        Transform target = blackboard.Get<Transform>(BBKeys.Player);

        //面向玩家
        if (target != null)
        {
            Vector3 lookDir = target.position - transform.position;
            lookDir.y = 0;
            if (lookDir.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    Quaternion.LookRotation(lookDir.normalized),
                    Time.deltaTime * 5f);
            }
        }

        //召唤中：暂停防御
        if (summoner != null && summoner.IsSummoning)
        {
            isDefending = false;
            animator.SetBool("IsDefending", false);
            animator.SetFloat("Speed", 0f);
            return BTStatus.Running;
        }

        //检查是否可以召唤
        if (summoner != null && summoner.CanSummon)
        {
            summoner.SummonOne();
            return BTStatus.Success;
        }

        //默认防御姿态
        isDefending = true;
        animator.SetBool("IsDefending", true);
        animator.SetFloat("Speed", 0f);
        return BTStatus.Running;
    }

    BTStatus Phase3Behavior()
    {
        if (isDefending)
        {
            isDefending = false;
            animator.SetBool("IsDefending", false);
        }

        Transform target = blackboard.Get<Transform>(BBKeys.Player);
        float range = blackboard.Get<float>(BBKeys.AttackRange);
        float baseSpeed = blackboard.Get<float>(BBKeys.MoveSpeed);
        float baseCooldown = blackboard.Get<float>(BBKeys.AttackCooldown);

        float speed = baseSpeed * 1.5f;
        float cooldown = baseCooldown * 0.5f;
        float distance = Vector3.Distance(transform.position, target.position);

        if (distance <= range && Time.time >= lastAttackTime + cooldown)
        {
            PerformBerserkAttack();
            return BTStatus.Success;
        }
        else if (distance <= detectionRange * 1.2f)
        {
            MoveTowards(target, speed);
            return BTStatus.Running;
        }

        animator.SetFloat("Speed", 0f);
        return BTStatus.Success;
    }

    //---- 工具方法 ----
    void MoveTowards(Transform target, float speed)
    {
        //卡住绕行检测
        if (stuckAvoidance != null)
        {
            stuckAvoidance.Tick();
            if (stuckAvoidance.IsDodging)
            {
                animator.SetFloat("Speed", 1f);
                return;
            }
        }

        //如果没有寻路组件，回退到直线追击
        if (pathfinder == null)
        {
            MoveDirectly(target, speed);
            return;
        }

        //定期更新路径
        if (Time.time >= lastPathUpdateTime + pathUpdateInterval)
        {
            lastPathUpdateTime = Time.time;
            currentPath = pathfinder.FindPath(transform.position, target.position);
            currentPathIndex = 0;
        }

        //沿路径移动
        if (currentPath != null && currentPath.Count > 0 && currentPathIndex < currentPath.Count)
        {
            Vector3 targetPos = currentPath[currentPathIndex];
            Vector3 direction = (targetPos - transform.position).normalized;

            if (direction.magnitude > 0.1f)
            {
                characterController.Move(direction * speed * Time.deltaTime);

                //平滑转向
                Quaternion targetRot = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 8f);
            }

            //到达节点，前进到下一个
            if (Vector3.Distance(transform.position, targetPos) < nodeReachDistance)
            {
                currentPathIndex++;
            }

            if (currentPathIndex >= currentPath.Count)
            {
                currentPath = null;
                currentPathIndex = 0;
            }

            animator.SetFloat("Speed", 1f);
        }
        else
        {
            //路径为空或计算中，回退到直线追击
            MoveDirectly(target, speed);
        }

        //强制贴地
        if (!characterController.isGrounded)
        {
            characterController.Move(Vector3.down * 0.5f * Time.deltaTime);
        }
    }

    void MoveDirectly(Transform target, float speed)
    {
        Vector3 dir = (target.position - transform.position).normalized;
        dir.y = 0;

        if (dir.magnitude > 0.1f)
        {
            characterController.Move(dir * speed * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(dir),
                Time.deltaTime * 8f);
            animator.SetFloat("Speed", 1f);
        }
        else
        {
            animator.SetFloat("Speed", 0f);
        }
    }

    void PerformAttack()
    {
        isAttacking = true;
        attackStartTime = Time.time;
        lastAttackTime = Time.time;
        animator.SetTrigger("Attack");
    }

    void PerformBerserkAttack()
    {
        isAttacking = true;
        attackStartTime = Time.time;
        lastAttackTime = Time.time;
        animator.SetTrigger("Attack");
    }

    //---- 动画事件 ----
    public void ApplyBossDamage()
    {
        if (currentPhase == BossPhase.Phase3)
            combat?.ApplyBerserkDamage();
        else
            combat?.ApplyNormalDamage();
    }

    public void EndBossAttack()
    {
        isAttacking = false;
    }

    public void OnFootstep() { }

    //---- 受击 ----
    public bool TakeDamage(int damage)
    {
        if (currentHealth <= 0 || isDead) return false;

        int finalDamage = damage;
        if (isDefending)
        {
            finalDamage = Mathf.RoundToInt(damage * defenseDamageReduction);
        }

        currentHealth = Mathf.Max(0, currentHealth - finalDamage);
        Debug.Log($"Boss 受到 {finalDamage} 伤害，剩余 HP: {currentHealth}/{maxHealth}");

        if (hitEffectPrefab != null)
        {
            GameObject effect = Instantiate(hitEffectPrefab,
                transform.position + Vector3.up * 1.2f,
                Quaternion.identity);
            effect.transform.localScale = Vector3.one * 1.5f;
            Destroy(effect, 1f);
        }

        if (DamageNumberPool.Instance != null)
        {
            GameObject numObj = DamageNumberPool.Instance.Get();
            if (numObj != null)
            {
                DamageNumber dn = numObj.GetComponent<DamageNumber>();
                if (dn != null) dn.Show(finalDamage, transform.position);
            }
        }

        bool canBeStunned = !isDefending
                         && currentPhase != BossPhase.Phase3
                         && (summoner == null || !summoner.IsSummoning);

        if (canBeStunned)
        {
            isAttacking = false;
            animator.ResetTrigger("Attack");
            animator.ResetTrigger("Hit");
            animator.SetTrigger("Hit");
        }

        if (currentHealth <= 0) Die();

        return true;
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        Debug.Log("Boss 死亡！");

        summoner?.KillAll();

        BossHealthUI ui = FindObjectOfType<BossHealthUI>();
        if (ui != null) ui.HideHealthBar();

        //禁用脚本和 CharacterController
        this.enabled = false;
        if (characterController != null)
            characterController.enabled = false;

        //禁用 Animator，启用 Ragdoll
        animator.enabled = false;
        EnableRagdoll();

        //Boss 体型大，给 5 秒
        Destroy(gameObject, 5f);

        Invoke(nameof(ShowVictoryDelayed), 3f);
    }

    //禁用所有非 CharacterController 的 Collider
    void DisableRagdollColliders()
    {
        Collider[] colliders = GetComponentsInChildren<Collider>(true);
        foreach (var col in colliders)
        {
            if (col is CharacterController) continue;  //跳过 CharacterController
            col.enabled = false;
        }
    }

    //启用/禁用所有 Ragdoll 的物理模拟
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

    void ShowVictoryDelayed()
    {
        GameOverUI ui = FindObjectOfType<GameOverUI>();
        if (ui != null) ui.ShowGameOver(true);
    }

    //---- 调试可视化 ----
    void OnDrawGizmosSelected()
    {
        float range = combat != null ? combat.attackRange : 2.5f;
        float aoe = combat != null ? combat.aoeRadius : 2.5f;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, range);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, aoe);

        if (currentPath != null && currentPath.Count > 0)
        {
            Gizmos.color = Color.cyan;
            for (int i = 0; i < currentPath.Count - 1; i++)
            {
                Gizmos.DrawLine(currentPath[i], currentPath[i + 1]);
            }
        }
    }
}