using UnityEngine;
using System.Collections.Generic;

public class GridManager : MonoBehaviour
{
    [Header("网格设置")]
    public Vector2 gridSize = new Vector2(20, 20);
    public float cellSize = 1f;
    public LayerMask obstacleLayer;

    private Node[,] grid;
    private float halfCellSize;

    [Header("障碍物检测优化")]
    [Range(0.5f, 1f)]
    public float obstacleCheckRadiusMultiplier = 0.8f;

    [Header("角色半径膨胀")]
    public float agentRadius = 0.3f;  //角色的碰撞半径

    private List<Transform> dynamicObstacles = new List<Transform>();

    [Header("动态障碍物")]
    public float dynamicObstacleRadius = 0.7f;

    void Start()
    {
        InitializeGrid();
    }

    void InitializeGrid()
    {
        halfCellSize = cellSize * 0.5f;
        grid = new Node[(int)gridSize.x, (int)gridSize.y];

        //计算实际检测半径
        float checkRadius = halfCellSize * obstacleCheckRadiusMultiplier + agentRadius;

        //计算网格的总宽度和高度
        float totalWidth = (gridSize.x - 1) * cellSize;
        float totalHeight = (gridSize.y - 1) * cellSize;

        //中心偏移：让网格左下角在 (-totalWidth/2, 0, -totalHeight/2)
        Vector3 centerOffset = new Vector3(-totalWidth * 0.5f, 0, -totalHeight * 0.5f);

        for (int x = 0; x < gridSize.x; x++)
        {
            for (int y = 0; y < gridSize.y; y++)
            {
                Vector3 worldPos = new Vector3(x * cellSize, 0, y * cellSize) + centerOffset;
                bool walkable = !Physics.CheckSphere(worldPos, checkRadius, obstacleLayer);
                grid[x, y] = new Node(walkable, worldPos, x, y);
            }
        }
    }

    public Node GetNodeFromWorld(Vector3 worldPos)
    {
        float totalWidth = (gridSize.x - 1) * cellSize;
        float totalHeight = (gridSize.y - 1) * cellSize;
        Vector3 centerOffset = new Vector3(-totalWidth * 0.5f, 0, -totalHeight * 0.5f);

        float x = (worldPos.x - centerOffset.x) / cellSize;
        float y = (worldPos.z - centerOffset.z) / cellSize;

        int gridX = Mathf.RoundToInt(x);
        int gridY = Mathf.RoundToInt(y);

        if (gridX < 0 || gridX >= gridSize.x || gridY < 0 || gridY >= gridSize.y)
            return null;

        return grid[gridX, gridY];
    }

    public List<Node> GetNeighbors(Node node)
    {
        List<Node> neighbors = new List<Node>();

        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                if (x == 0 && y == 0) continue;

                int checkX = node.gridX + x;
                int checkY = node.gridY + y;

                if (checkX >= 0 && checkX < gridSize.x && checkY >= 0 && checkY < gridSize.y)
                {
                    neighbors.Add(grid[checkX, checkY]);
                }
            }
        }

        return neighbors;
    }

    //注册动态障碍物（Boss 进入 Phase2 时调用）
    public void RegisterDynamicObstacle(Transform obstacle)
    {
        if (!dynamicObstacles.Contains(obstacle))
            dynamicObstacles.Add(obstacle);
    }

    //取消动态障碍物（Boss 进入 Phase3 时调用）
    public void UnregisterDynamicObstacle(Transform obstacle)
    {
        dynamicObstacles.Remove(obstacle);
    }

    //判断节点是否可通行（综合静态网格 + 动态障碍物）
    public bool IsWalkable(Node node)
    {
        if (!node.walkable) return false;

        foreach (var obs in dynamicObstacles)
        {
            if (obs == null) continue;

            //只比较水平距离（忽略 Y 轴）
            Vector2 a = new Vector2(node.worldPos.x, node.worldPos.z);
            Vector2 b = new Vector2(obs.position.x, obs.position.z);

            if (Vector2.Distance(a, b) < dynamicObstacleRadius)
                return false;
        }
        return true;
    }

    //---- 可视化调试 ----
    void OnDrawGizmos()
    {
        if (grid == null) InitializeGrid();
        if (grid == null) return;

        int drawStep = Mathf.Max(1, (int)gridSize.x / 50);  //每50格绘制1个

        for (int x = 0; x < gridSize.x; x += drawStep)
        {
            for (int y = 0; y < gridSize.y; y += drawStep)
            {
                Node node = grid[x, y];
                Gizmos.color = node.walkable ? Color.white : Color.red;
                Gizmos.DrawWireCube(node.worldPos, Vector3.one * (cellSize * 0.9f) * drawStep);
            }
        }
    }
}