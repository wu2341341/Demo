namespace BehaviorTree
{
    public enum BTStatus
    {
        Success,    //节点执行成功
        Failure,    //节点执行失败
        Running     //节点正在执行中（需要下一帧继续）
    }
}