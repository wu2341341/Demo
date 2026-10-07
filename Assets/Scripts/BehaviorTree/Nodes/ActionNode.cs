using System;

namespace BehaviorTree
{
    ///<summary>
    ///动作节点：执行一个返回 BTStatus 的委托
    ///</summary>
    public class ActionNode : BTNode
    {
        private Func<BTStatus> action;

        public ActionNode(Func<BTStatus> action, string name = "Action") : base(name)
        {
            this.action = action;
        }

        public override BTStatus Execute()
        {
            try
            {
                return action();
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"动作节点 {nodeName} 执行异常: {e.Message}");
                return BTStatus.Failure;
            }
        }
    }
}