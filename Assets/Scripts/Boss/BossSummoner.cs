using UnityEngine;
using System.Collections;
using System.Collections.Generic;

//Boss 召唤模块：负责小兵生成、数量管理、协程调度

public class BossSummoner : MonoBehaviour
{
    [Header("召唤配置")]
    public GameObject minionPrefab;
    public float summonCooldown = 5f;
    public int maxMinions = 7;
    public float summonRange = 5f;

    [Header("小兵属性")]
    public float minionMoveSpeed = 1.5f;
    public int minionAttackDamage = 8;

    //内部状态
    private float lastSummonTime;
    private Coroutine summonCoroutine;
    private List<GameObject> activeMinions = new List<GameObject>();
    private Animator animator;

    //对外属性
    public bool IsSummoning { get; private set; }
    public int ActiveMinionCount => activeMinions.Count;
    public bool CanSummon => Time.time >= lastSummonTime + summonCooldown
                          && activeMinions.Count < maxMinions;

    void Awake()
    {
        animator = GetComponent<Animator>();
    }

    void Start()
    {
        lastSummonTime = -summonCooldown;
    }

    void Update()
    {
        //清理已销毁的小兵
        activeMinions.RemoveAll(m => m == null);
    }

    //---- 单个召唤（Phase2 周期使用）----

    public void SummonOne()
    {
        if (minionPrefab == null || IsSummoning) return;

        IsSummoning = true;
        animator.SetTrigger("Summon");

        if (summonCoroutine != null) StopCoroutine(summonCoroutine);
        summonCoroutine = StartCoroutine(SummonOneRoutine());
    }

    IEnumerator SummonOneRoutine()
    {
        yield return new WaitForSeconds(1.0f);

        Vector2 randomCircle = Random.insideUnitCircle.normalized * summonRange;
        SpawnMinion(transform.position + new Vector3(randomCircle.x, 0, randomCircle.y));

        lastSummonTime = Time.time;
        Debug.Log($"Boss 召唤小兵！当前数量: {activeMinions.Count}/{maxMinions}");

        yield return new WaitForSeconds(0.5f);
        IsSummoning = false;
        summonCoroutine = null;
    }

    //---- 批量召唤（Phase3 使用）----
    public void SummonBatch(int count)
    {
        if (minionPrefab == null) return;

        IsSummoning = true;
        animator.SetTrigger("Summon");

        if (summonCoroutine != null) StopCoroutine(summonCoroutine);
        summonCoroutine = StartCoroutine(SummonBatchRoutine(count));
    }

    IEnumerator SummonBatchRoutine(int count)
    {
        yield return new WaitForSeconds(0.8f);

        for (int i = 0; i < count; i++)
        {
            float angle = (360f / count) * i + Random.Range(-15f, 15f);
            float rad = angle * Mathf.Deg2Rad;

            Vector3 offset = new Vector3(Mathf.Sin(rad), 0, Mathf.Cos(rad)) * summonRange;
            SpawnMinion(transform.position + offset);

            Debug.Log($"Boss 召唤第 {i + 1}/{count} 个小兵");
            yield return new WaitForSeconds(0.15f);
        }

        IsSummoning = false;
        summonCoroutine = null;
        Debug.Log($"Boss Phase3 召唤完成，当前数量: {activeMinions.Count}");
    }

    //---- 工具方法 ----

    private void SpawnMinion(Vector3 spawnPos)
    {
        //射线检测地面高度
        if (Physics.Raycast(spawnPos + Vector3.up * 10f, Vector3.down, out RaycastHit hit, 20f))
            spawnPos.y = hit.point.y;

        GameObject minion = Instantiate(minionPrefab, spawnPos, Quaternion.identity);
        activeMinions.Add(minion);

        Enemy enemy = minion.GetComponent<Enemy>();
        if (enemy != null)
        {
            enemy.moveSpeed = minionMoveSpeed;
            enemy.attackDamage = minionAttackDamage;
        }
    }

    //Boss 死亡时调用：杀死所有小兵
    public void KillAll()
    {
        foreach (var minion in activeMinions)
        {
            if (minion != null)
            {
                Enemy enemy = minion.GetComponent<Enemy>();
                if (enemy != null) enemy.TakeDamage(9999);
                else Destroy(minion);
            }
        }
        activeMinions.Clear();
    }

    //中断当前召唤协程
    public void CancelCurrentSummon()
    {
        if (summonCoroutine != null)
        {
            StopCoroutine(summonCoroutine);
            summonCoroutine = null;
        }
        IsSummoning = false;
    }
}