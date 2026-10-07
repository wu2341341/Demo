using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BossHealthUI : MonoBehaviour
{
    [Header("UI 引用")]
    public RectTransform healthFillMask;
    public TextMeshProUGUI bossNameText;
    public TextMeshProUGUI healthText;
    public GameObject healthBarRoot;

    [Header("颜色配置")]
    public Image healthFillImage;   // Fill 的 Image（用于改颜色）
    public Color phase1Color = new Color(0.8f, 0.1f, 0.1f);
    public Color phase2Color = new Color(0.9f, 0.6f, 0.1f);
    public Color phase3Color = new Color(0.6f, 0.0f, 0.8f);

    [Header("显示控制")]
    public float showDistance = 15f;

    private BossController boss;
    private Transform player;
    private float healthBarFullWidth;

    void Start()
    {
        boss = FindObjectOfType<BossController>();
        if (boss == null)
        {
            if (healthBarRoot != null) healthBarRoot.SetActive(false);
            return;
        }

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;

        if (bossNameText != null) bossNameText.text = "Boss";

        if (healthFillMask != null)
            healthBarFullWidth = healthFillMask.sizeDelta.x;

        if (healthBarRoot != null) healthBarRoot.SetActive(false);
    }

    void Update()
    {
        if (boss == null || player == null) return;

        float distance = Vector3.Distance(boss.transform.position, player.position);
        bool shouldShow = distance <= showDistance && !boss.IsDead;

        if (healthBarRoot != null && healthBarRoot.activeSelf != shouldShow)
            healthBarRoot.SetActive(shouldShow);

        if (shouldShow)
            UpdateHealthBar();
    }

    void UpdateHealthBar()
    {
        if (boss == null) return;

        float percent = (float)boss.CurrentHealth / boss.MaxHealth;

        //用宽度控制填充
        if (healthFillMask != null)
        {
            Vector2 size = healthFillMask.sizeDelta;
            size.x = healthBarFullWidth * percent;
            healthFillMask.sizeDelta = size;
        }

        //颜色随阶段变化
        if (healthFillImage != null)
        {
            if (percent <= 0.3f) healthFillImage.color = phase3Color;
            else if (percent <= 0.6f) healthFillImage.color = phase2Color;
            else healthFillImage.color = phase1Color;
        }

        if (healthText != null)
            healthText.text = $"{boss.CurrentHealth} / {boss.MaxHealth}";
    }

    public void HideHealthBar()
    {
        if (healthBarRoot != null) healthBarRoot.SetActive(false);
    }
}