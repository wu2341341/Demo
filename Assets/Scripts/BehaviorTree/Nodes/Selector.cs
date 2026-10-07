using System.Collections.Generic;

namespace BehaviorTree
{
    ///<summary>
    ///选择节点：按优先级执行子节点
    ///- 任意 Success -> 立即返回 Success
    ///- 任意 Running -> 立即返回 Running
    ///- 全部 Failure -> 返回 Failure
    ///</summary>
    public class Selector : CompositeNode
    {
        private int currentIndex = 0;

        public Selector(List<BTNode> children, string name = "Selector")
            : base(name, children) { }

        public override BTStatus Execute()
        {
            for (int i = currentIndex; i < children.Count; i++)
            {
                BTStatus status = children[i].Execute();

                if (status == BTStatus.Success)
                {
                    currentIndex = 0;
                    return BTStatus.Success;
                }
                else if (status == BTStatus.Running)
                {
                    currentIndex = i;
                    return BTStatus.Running;
                }
            }

            currentIndex = 0;
            return BTStatus.Failure;
        }

        public override void Reset()
        {
            currentIndex = 0;
            foreach (var child in children)
            {
                child.Reset();
            }
        }
    }
}