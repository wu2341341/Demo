using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    [Header("目标")]
    public Transform target;               //跟随的角色
    public float targetHeight = 1.5f;      //看向角色的高度（胸部）

    [Header("距离")]
    public float distance = 5f;
    public float minDistance = 2f;
    public float maxDistance = 10f;

    [Header("灵敏度")]
    public float yawSpeed = 3f;            //水平旋转速度
    public float pitchSpeed = 2f;          //垂直旋转速度
    public float zoomSpeed = 2f;           //滚轮缩放速度

    [Header("角度限制")]
    public float pitchMin = -30f;          //最低俯仰角（向下看）
    public float pitchMax = 60f;           //最高俯仰角（向上看）

    //内部状态
    private float yaw = 0f;                //水平角度（度）
    private float pitch = 20f;             //垂直角度（度）

    [Header("震动")]
    public float shakeDuration = 0.2f;      //震动持续时间
    public float shakeMagnitude = 0.3f;     //震动幅度
    private float currentShakeDuration;     //当前剩余震动时间
    private Vector3 originalPos;            //相机原始位置

    private float currentShakeDurationMax;

    void Start()
    {
        //锁定鼠标光标
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        originalPos = transform.position;
    }

    void LateUpdate()
    {
        if (target == null) return;

        //---- 1. 输入 ----
        //鼠标水平移动 -> 左右旋转（偏航）
        yaw += Input.GetAxis("Mouse X") * yawSpeed;

        //鼠标垂直移动 -> 上下旋转（俯仰），鼠标向上移动 pitch 减小（往下看）
        pitch -= Input.GetAxis("Mouse Y") * pitchSpeed;
        pitch = Mathf.Clamp(pitch, pitchMin, pitchMax);

        //滚轮 -> 缩放
        distance -= Input.GetAxis("Mouse ScrollWheel") * zoomSpeed;
        distance = Mathf.Clamp(distance, minDistance, maxDistance);

        //---- 2. 球坐标计算相机位置 ----
        float yawRad = yaw * Mathf.Deg2Rad;
        float pitchRad = pitch * Mathf.Deg2Rad;

        Vector3 offset = new Vector3(
            distance * Mathf.Cos(pitchRad) * Mathf.Sin(yawRad),
            distance * Mathf.Sin(pitchRad),
            distance * Mathf.Cos(pitchRad) * Mathf.Cos(yawRad)
        );

        Vector3 lookTarget = target.position + Vector3.up * targetHeight;
        Vector3 desiredPos = lookTarget + offset;

        //---- 3. 平滑跟随 ----
        transform.position = Vector3.Lerp(transform.position, desiredPos, Time.deltaTime * 10f);

        //---- 4. 始终看向目标 ----
        transform.LookAt(lookTarget);

        if (currentShakeDuration > 0)
        {
            float decay = currentShakeDuration / currentShakeDurationMax;
            float decayedMagnitude = shakeMagnitude * decay;

            float x = Random.Range(-1f, 1f) * decayedMagnitude;
            float y = Random.Range(-1f, 1f) * decayedMagnitude;
            transform.position += new Vector3(x, y, 0);

            currentShakeDuration -= Time.deltaTime;
        }
    }

    public void Shake(float duration, float magnitude)
    {
        currentShakeDuration = duration;
        currentShakeDurationMax = duration;
        shakeMagnitude = magnitude;
    }
}