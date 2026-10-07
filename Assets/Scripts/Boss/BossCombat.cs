using UnityEngine;
using System.Collections.Generic;

//Boss 战斗模块：负责伤害判定、AOE、攻击数值

public class BossCombat : MonoBehaviour
{
    [Header("攻击属性")]
    public float attackRange = 2.5f;
    public int attackDamage = 15;
    public float aoeRadius = 2.5f;

    private Transform player;

    public void Initialize(Transform player)
    {
        this.player = player;
    }

    //Phase1 的单体近战伤害
    public void ApplyNormalDamage()
    {
        if (player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);
        if (distance > attackRange * 1.2f) return;

        PlayerController pc = player.GetComponent<PlayerController>();
        if (pc != null)
        {
            bool success = pc.TakeDamage(attackDamage);
            if (success)
                Debug.Log($"Boss 对玩家造成 {attackDamage} 伤害");
            else
                Debug.Log("玩家闪避/无敌，Boss 攻击未命中");
        }
    }

    //Phase3 的 AOE 伤害（不分敌我）
    public void ApplyBerserkDamage()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, aoeRadius);
        HashSet<IDamageable> hitTargets = new HashSet<IDamageable>();

        Debug.Log($"AOE 释放，半径 {aoeRadius}，检测到 {hits.Length} 个碰撞体");

        foreach (var hit in hits)
        {
            IDamageable target = hit.GetComponent<IDamageable>();
            if (target == null)
                target = hit.GetComponentInParent<IDamageable>();

            if (target == null) continue;
            if (target.IsDead || hitTargets.Contains(target)) continue;

            Component targetComp = target as Component;
            if (targetComp != null && targetComp.transform.root == transform.root) continue;

            hitTargets.Add(target);
            bool success = target.TakeDamage(attackDamage * 4 / 3);
            if (success)
                Debug.Log($"AOE 命中 {hit.name}（不分敌我）");
        }
    }
}