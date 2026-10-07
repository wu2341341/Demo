using UnityEngine;

//卡住检测 + 自动绕行
//当角色想移动但位置几乎不变时，自动向侧面移动一小段

public class StuckAvoidance : MonoBehaviour
{
    [Header("检测参数")]
    public float checkInterval = 0.25f;      // 检测间隔（秒）
    public float stuckThreshold = 0.05f;     // 移动距离小于此值视为卡住
    public float dodgeDistance = 0.6f;       // 绕行距离
    public float dodgeDuration = 0.3f;       // 绕行持续时间
    public float dodgeCooldown = 1.0f;       // 绕行冷却
    public float obstacleCheckDistance = 1.5f; // 检测侧面障碍的距离

    private CharacterController controller;
    private Vector3 lastPosition;
    private float lastCheckTime;
    private float lastDodgeTime;
    private Vector3 dodgeDirection;
    private float dodgeEndTime;
    private bool isDodging;

    public bool IsDodging => isDodging;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (controller == null)
            Debug.LogError($"{name} 缺少 CharacterController，StuckAvoidance 无法工作");
        lastPosition = transform.position;
    }

    //每帧调用（当角色"想移动"时）。
    //如果在绕行中，会自动执行绕行移动。

    public void Tick()
    {
        //正在绕行
        if (isDodging)
        {
            if (Time.time < dodgeEndTime)
            {
                float speed = dodgeDistance / dodgeDuration;
                controller.Move(dodgeDirection * speed * Time.deltaTime);
            }
            else
            {
                isDodging = false;
                lastPosition = transform.position;
            }
            return;
        }

        //定期检测
        if (Time.time < lastCheckTime + checkInterval) return;
        lastCheckTime = Time.time;

        float moved = Vector3.Distance(transform.position, lastPosition);
        lastPosition = transform.position;

        if (moved < stuckThreshold && Time.time > lastDodgeTime + dodgeCooldown)
        {
            StartDodge();
        }
    }

    void StartDodge()
    {
        Vector3 left = -transform.right;
        Vector3 right = transform.right;
        Vector3 origin = transform.position + Vector3.up * 0.5f;

        //检测左右哪个方向更空
        bool leftBlocked = Physics.Raycast(origin, left, obstacleCheckDistance);
        bool rightBlocked = Physics.Raycast(origin, right, obstacleCheckDistance);

        Vector3 dodgeDir;
        if (leftBlocked && !rightBlocked)
            dodgeDir = right;
        else if (rightBlocked && !leftBlocked)
            dodgeDir = left;
        else
            dodgeDir = Random.value > 0.5f ? left : right;  //两边都堵或都空->随机

        dodgeDirection = dodgeDir.normalized;
        dodgeEndTime = Time.time + dodgeDuration;
        lastDodgeTime = Time.time;
        isDodging = true;

        Debug.Log($"{name} 检测到卡住，向 {(leftBlocked ? "右" : "左")} 绕行");
    }
}