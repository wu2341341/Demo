using System.Collections.Generic;

namespace BehaviorTree
{
    ///<summary>
    ///顺序节点：按顺序执行所有子节点
    ///- 全部 Success -> Success
    ///- 任意 Failure -> 立即返回 Failure
    ///- 任意 Running -> 立即返回 Running
    ///</summary>
    public class Sequence : CompositeNode
    {
        private int currentIndex = 0;

        public Sequence(List<BTNode> children, string name = "Sequence")
            : base(name, children) { }

        public override BTStatus Execute()
        {
            //从当前索引开始执行（支持 Running 状态的恢复）
            for (int i = currentIndex; i < children.Count; i++)
            {
                BTStatus status = children[i].Execute();

                if (status == BTStatus.Failure)
                {
                    currentIndex = 0;
                    return BTStatus.Failure;
                }
                else if (status == BTStatus.Running)
                {
                    currentIndex = i;
                    return BTStatus.Running;
                }
            }

            currentIndex = 0;
            return BTStatus.Success;
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