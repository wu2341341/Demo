using UnityEngine;

//连招系统：管理多段攻击的状态、窗口和取消
public class ComboSystem : MonoBehaviour
{
    [Header("连招配置")]
    public int maxComboCount = 3;           //最大连招段数
    public float comboResetTime = 0.8f;     //超过这个时间未攻击则重置连招
    public float[] comboDamageMultiplier = { 1.0f, 1.2f, 1.8f }; //每段伤害倍率

    //当前状态
    private int currentComboIndex = 0;      //当前连招段数（0=未连招）
    private float lastAttackTime;
    private bool isAttacking;
    private bool comboWindowOpen;           //连招窗口是否开启

    //输入缓冲
    private InputBuffer inputBuffer = new InputBuffer();

    //引用
    private Animator animator;
    private PlayerController playerController;

    //属性
    public int CurrentComboIndex => currentComboIndex;
    public bool IsAttacking => isAttacking;
    public bool ComboWindowOpen => comboWindowOpen;
    public float CurrentDamageMultiplier =>
        currentComboIndex > 0 && currentComboIndex <= comboDamageMultiplier.Length
            ? comboDamageMultiplier[currentComboIndex - 1]
            : 1.0f;

    void Start()
    {
        animator = GetComponent<Animator>();
        playerController = GetComponent<PlayerController>();
    }

    void Update()
    {
        //只保留超时兜底（删除自动解锁）
        if (isAttacking && Time.time - lastAttackTime > 3f)
        {
            Debug.LogWarning("ComboSystem 超时解锁");
            isAttacking = false;
            comboWindowOpen = false;
        }

        //检查连招超时重置
        if (!isAttacking && currentComboIndex > 0)
        {
            if (Time.time - lastAttackTime > comboResetTime)
            {
                ResetCombo();
            }
        }

        //在连招窗口期间，检查是否有缓冲的攻击输入
        if (comboWindowOpen && inputBuffer.ConsumeInput("Attack", true))
        {
            if (currentComboIndex < maxComboCount)
            {
                AdvanceCombo();
            }
        }
    }

    //尝试发起攻击（由 PlayerController 调用）
    public bool TryAttack()
    {
        if (isAttacking)
        {
            //只缓冲输入，不直接推进（交给 Update 处理）
            inputBuffer.BufferInput("Attack");
            return false;
        }

        //不在攻击中，直接发起攻击
        if (currentComboIndex == 0 || Time.time - lastAttackTime > comboResetTime)
        {
            StartCombo(1);
        }
        else if (currentComboIndex < maxComboCount)
        {
            StartCombo(currentComboIndex + 1);
        }
        else
        {
            StartCombo(1);
        }

        return true;
    }

    //开始/继续连招
    private void StartCombo(int index)
    {
        currentComboIndex = index;
        isAttacking = true;
        lastAttackTime = Time.time;

        //关键：进入新一段时，先关闭连招窗口
        comboWindowOpen = false;

        animator.SetInteger("ComboIndex", index);
        animator.SetTrigger("Attack");

        Debug.Log($"连招第 {index} 段，伤害倍率: {CurrentDamageMultiplier}x");
    }

    //连招进阶（在窗口开启时由缓冲输入触发）
    private void AdvanceCombo()
    {
        if (currentComboIndex >= maxComboCount)
        {
            //已达最大段数
            return;
        }

        StartCombo(currentComboIndex + 1);
    }

    //重置连招（超时或被打断时调用）
    public void ResetCombo()
    {
        currentComboIndex = 0;
        isAttacking = false;
        comboWindowOpen = false;
        inputBuffer.Clear();
    }

    //---- 动画事件回调 ----

    //由动画事件调用：开启连招窗口
    public void OpenComboWindow()
    {
        comboWindowOpen = true;
        Debug.Log("连招窗口已开启");
    }

    //由动画事件调用：关闭连招窗口
    public void CloseComboWindow()
    {
        comboWindowOpen = false;
    }

    //由动画事件调用：攻击动画结束
    public void EndAttack()
    {
        Debug.Log($"ComboSystem.EndAttack 被调用，当前段数: {currentComboIndex}");

        isAttacking = false;
        comboWindowOpen = false;
        lastAttackTime = Time.time;

        if (currentComboIndex >= maxComboCount)
        {
            ResetCombo();
        }
    }

    //由动画事件调用：造成伤害
    public void DealDamage()
    {
        if (playerController != null)
        {
            playerController.ApplyDamageWithMultiplier(CurrentDamageMultiplier);
        }
    }

    //取消当前攻击
    //<param name="resetCombo">是否重置连招段数
    public void CancelAttack(bool resetCombo = false)
    {
        isAttacking = false;
        comboWindowOpen = false;
        inputBuffer.Clear();

        if (resetCombo)
        {
            currentComboIndex = 0;
        }
        // 否则保留currentComboIndex，允许闪避取消后继续连招
    }
}