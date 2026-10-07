namespace BehaviorTree
{
    public abstract class BTNode
    {
        protected string nodeName;
        protected Blackboard blackboard;

        public BTNode(string name = "Node")
        {
            nodeName = name;
        }

        //设置黑板引用（由外部注入）
        public virtual void SetBlackboard(Blackboard bb)
        {
            blackboard = bb;
        }

        //核心执行方法（子类必须实现）
        public abstract BTStatus Execute();

        //可选：重置节点状态（用于 Running 状态的清理）
        public virtual void Reset() { }

        //调试用：获取节点名称
        public string GetName() => nodeName;
    }
}