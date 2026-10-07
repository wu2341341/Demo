using System;

namespace BehaviorTree
{
    ///<summary>
    ///条件节点：执行一个返回 bool 的委托
    ///- true -> Success
    ///- false -> Failure
    ///</summary>
    public class ConditionNode : BTNode
    {
        private Func<bool> condition;

        public ConditionNode(Func<bool> condition, string name = "Condition") : base(name)
        {
            this.condition = condition;
        }

        public override BTStatus Execute()
        {
            try
            {
                return condition() ? BTStatus.Success : BTStatus.Failure;
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"条件节点 {nodeName} 执行异常: {e.Message}");
                return BTStatus.Failure;
            }
        }
    }
}