using System.Collections.Generic;

namespace BehaviorTree
{
    public abstract class CompositeNode : BTNode
    {
        protected List<BTNode> children;

        public CompositeNode(string name, List<BTNode> children) : base(name)
        {
            this.children = children;
            //将黑板传递给所有子节点
            //foreach (var child in children)
            //{
            //  child.SetBlackboard(blackboard);
            //}
        }

        //设置黑板时，同时传递给所有子节点
        public override void SetBlackboard(Blackboard bb)
        {
            base.SetBlackboard(bb);
            foreach (var child in children)
            {
                child.SetBlackboard(bb);
            }
        }
    }
}