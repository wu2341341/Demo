using UnityEngine;
using System.Collections.Generic;

public class AStarPathfinding : MonoBehaviour
{
    private GridManager gridManager;

    void Start()
    {
        gridManager = GetComponent<GridManager>();
    }

    public List<Vector3> FindPath(Vector3 startPos, Vector3 targetPos)
    {
        Node startNode = gridManager.GetNodeFromWorld(startPos);
        Node targetNode = gridManager.GetNodeFromWorld(targetPos);

        if (startNode == null || targetNode == null) return null;
        if (!gridManager.IsWalkable(startNode) || !gridManager.IsWalkable(targetNode)) return null;

        //用 MinHeap 替代 List
        MinHeap openSet = new MinHeap();
        HashSet<Node> closedSet = new HashSet<Node>();

        openSet.Push(startNode);

        while (openSet.Count > 0)
        {
            //O(log n) 取出 fCost 最小的节点
            Node currentNode = openSet.Pop();
            closedSet.Add(currentNode);

            if (currentNode == targetNode)
                return RetracePath(startNode, targetNode);

            foreach (Node neighbor in gridManager.GetNeighbors(currentNode))
            {
                if (!gridManager.IsWalkable(neighbor) || closedSet.Contains(neighbor))
                    continue;

                int newCostToNeighbor = currentNode.gCost + GetDistance(currentNode, neighbor);

                if (newCostToNeighbor < neighbor.gCost || !openSet.Contains(neighbor))
                {
                    neighbor.gCost = newCostToNeighbor;
                    neighbor.hCost = GetDistance(neighbor, targetNode);
                    neighbor.parent = currentNode;

                    if (!openSet.Contains(neighbor))
                        openSet.Push(neighbor);
                    else
                        openSet.UpdateNode(neighbor);
                }
            }
        }

        return null;
    }

    List<Vector3> RetracePath(Node startNode, Node endNode)
    {
        List<Vector3> path = new List<Vector3>();
        Node currentNode = endNode;

        while (currentNode != startNode)
        {
            path.Add(currentNode.worldPos);
            currentNode = currentNode.parent;
        }
        path.Reverse();

        //简化路径（可选）
        //path = SimplifyPath(path);

        return path;
    }

    int GetDistance(Node a, Node b)
    {
        int dx = Mathf.Abs(a.gridX - b.gridX);
        int dy = Mathf.Abs(a.gridY - b.gridY);

        //八方向移动：对角线距离 = 14，直线 = 10
        if (dx > dy)
            return 14 * dy + 10 * (dx - dy);
        return 14 * dx + 10 * (dy - dx);
    }

    //可选：简化路径，去除冗余节点
    List<Vector3> SimplifyPath(List<Vector3> path)
    {
        if (path.Count < 3) return path;

        List<Vector3> simplified = new List<Vector3> { path[0] };
        for (int i = 1; i < path.Count - 1; i++)
        {
            Vector3 prev = path[i - 1];
            Vector3 curr = path[i];
            Vector3 next = path[i + 1];

            Vector3 dir1 = (curr - prev).normalized;
            Vector3 dir2 = (next - curr).normalized;

            if (Vector3.Distance(dir1, dir2) > 0.1f) //方向变化较大时保留节点
            {
                simplified.Add(curr);
            }
        }
        simplified.Add(path[path.Count - 1]);
        return simplified;
    }
}