using TMPro;
using UnityEngine;

public class DamageNumber : MonoBehaviour
{
    [Header("浮动参数")]
    public float floatSpeed = 2f;
    public float lifeTime = 1.2f;
    public float randomOffsetRange = 0.5f;

    private TextMeshProUGUI tmp;        //UGUI 版本的 TMP
    private RectTransform rectTransform;
    private Vector3 startPos;
    private float currentLife;

    [Header("显示")]
    public float defaultScale = 0.3f;  //在 Inspector 中可调

    private Camera mainCamera;

    void Awake()
    {
        //在子物体中查找 TextMeshProUGUI（UI文本）
        tmp = GetComponentInChildren<TextMeshProUGUI>();
        if (tmp == null)
        {
            //如果没找到，尝试查找普通 Text（兼容旧版）
            UnityEngine.UI.Text legacyText = GetComponentInChildren<UnityEngine.UI.Text>();
            if (legacyText != null)
            {
                Debug.LogWarning($"{name} 使用的是旧版 Text 组件，建议替换为 TextMeshPro");
                //可以把 legacyText 转为 TMP 吗？这里我们提示错误，因为最好用 TMP
                //但我们也可以自己创建一个 TMP 并赋值，或者直接显示错误。
                Debug.LogError("请将 DamageText 替换为 TextMeshProUGUI 组件！");
            }
            else
            {
                Debug.LogError($"{name} 找不到 TextMeshProUGUI 组件！请确保子物体有 TMP 文本。");
            }
        }

        rectTransform = GetComponent<RectTransform>();
        if (rectTransform == null)
            rectTransform = gameObject.AddComponent<RectTransform>();

        mainCamera = Camera.main;
    }

    public void Show(int damage, Vector3 worldPos)
    {
        if (tmp == null)
        {
            Debug.LogError("tmp 为空，无法显示伤害数字");
            return;
        }

        //设置文本
        tmp.text = damage.ToString();

        //随机偏移
        float randX = Random.Range(-randomOffsetRange, randomOffsetRange);
        float randZ = Random.Range(-randomOffsetRange, randomOffsetRange);
        startPos = worldPos + new Vector3(randX, 1.8f, randZ);
        transform.position = startPos;

        //---- 强制设置大小 ----
        //固定大小为 0.3 单位（可根据需要调整）
        float randScale = Random.Range(0.85f, 1.15f);
        transform.localScale = Vector3.one * defaultScale * randScale;

        //---- 强制朝向摄像机（始终面向玩家） ----
        if (mainCamera != null)
        {
            //让数字始终正面朝向摄像机
            transform.LookAt(mainCamera.transform);
            //因为 LookAt 会让 Z 轴指向摄像机，但 UI 的正面是 Z 轴负方向，所以需要旋转 180 度
            transform.Rotate(0, 180, 0);
        }

        //重置颜色
        Color color = tmp.color;
        color.a = 1f;
        tmp.color = color;

        currentLife = lifeTime;
        gameObject.SetActive(true);
    }

    void Update()
    {
        if (currentLife <= 0) return;

        //---- 持续面向摄像机 ----
        if (mainCamera != null)
        {
            transform.LookAt(mainCamera.transform);
            transform.Rotate(0, 180, 0);
        }

        //上浮
        transform.position += Vector3.up * floatSpeed * Time.deltaTime;

        //渐出
        currentLife -= Time.deltaTime;
        float alpha = Mathf.Clamp01(currentLife / lifeTime);
        Color color = tmp.color;
        color.a = alpha;
        tmp.color = color;

        if (currentLife <= 0)
        {
            gameObject.SetActive(false);
            if (DamageNumberPool.Instance != null)
                DamageNumberPool.Instance.Return(gameObject);
        }
    }
}