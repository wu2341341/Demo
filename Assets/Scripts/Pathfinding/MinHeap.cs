using System.Collections.Generic;

//二叉最小堆：按 Node.fCost 排序
//支持：Push O(log n)、Pop O(log n)、UpdateNode O(log n)、Contains O(1)
public class MinHeap
{
    private List<Node> heap = new List<Node>();
    private Dictionary<Node, int> nodeIndices = new Dictionary<Node, int>();  //节点 -> 堆索引

    public int Count => heap.Count;

    public bool Contains(Node node) => nodeIndices.ContainsKey(node);

    public void Push(Node node)
    {
        if (nodeIndices.ContainsKey(node)) return;

        heap.Add(node);
        nodeIndices[node] = heap.Count - 1;
        HeapifyUp(heap.Count - 1);
    }

    public Node Pop()
    {
        if (heap.Count == 0) return null;

        Node root = heap[0];
        nodeIndices.Remove(root);

        int lastIndex = heap.Count - 1;
        heap[0] = heap[lastIndex];
        heap.RemoveAt(lastIndex);

        if (heap.Count > 0)
        {
            nodeIndices[heap[0]] = 0;
            HeapifyDown(0);
        }

        return root;
    }

    //节点 gCost 变化后调用，重新调整其在堆中的位置
    public void UpdateNode(Node node)
    {
        if (!nodeIndices.TryGetValue(node, out int index)) return;

        //fCost 变小 -> 上浮；fCost 变大 -> 下沉
        HeapifyUp(index);
        HeapifyDown(index);
    }

    public void Clear()
    {
        heap.Clear();
        nodeIndices.Clear();
    }

    private void HeapifyUp(int index)
    {
        while (index > 0)
        {
            int parent = (index - 1) / 2;
            if (heap[index].fCost >= heap[parent].fCost) break;

            Swap(index, parent);
            index = parent;
        }
    }

    private void HeapifyDown(int index)
    {
        int count = heap.Count;

        while (true)
        {
            int left = 2 * index + 1;
            int right = 2 * index + 2;
            int smallest = index;

            if (left < count && heap[left].fCost < heap[smallest].fCost)
                smallest = left;
            if (right < count && heap[right].fCost < heap[smallest].fCost)
                smallest = right;

            if (smallest == index) break;

            Swap(index, smallest);
            index = smallest;
        }
    }

    private void Swap(int a, int b)
    {
        (heap[a], heap[b]) = (heap[b], heap[a]);
        //同步更新索引映射
        nodeIndices[heap[a]] = a;
        nodeIndices[heap[b]] = b;
    }
}