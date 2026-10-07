using System.Collections.Generic;
using BehaviorTree;

//构建 Boss 的行为树
public static class BossBTBuilder
{

    //Boss 行为树
    ///<param name="phaseProvider">获取当前阶段的委托</param>
    ///<param name="onPhase1">Phase1 行为</param>
    ///<param name="onPhase2">Phase2 行为</param>
    ///<param name="onPhase3">Phase3 行为</param>
    ///<param name="blackboard">黑板</param>
    public static BTNode Build(
        System.Func<BossPhase> phaseProvider,
        System.Func<BTStatus> onPhase1,
        System.Func<BTStatus> onPhase2,
        System.Func<BTStatus> onPhase3,
        Blackboard blackboard)
    {
        BTNode tree = new Selector(new List<BTNode>
        {
            new Sequence(new List<BTNode>
            {
                new ConditionNode(() => phaseProvider() == BossPhase.Phase3, "IsPhase3"),
                new ActionNode(onPhase3, "Phase3Behavior")
            }, "Phase3Sequence"),

            new Sequence(new List<BTNode>
            {
                new ConditionNode(() => phaseProvider() == BossPhase.Phase2, "IsPhase2"),
                new ActionNode(onPhase2, "Phase2Behavior")
            }, "Phase2Sequence"),

            new Sequence(new List<BTNode>
            {
                new ConditionNode(() => phaseProvider() == BossPhase.Phase1, "IsPhase1"),
                new ActionNode(onPhase1, "Phase1Behavior")
            }, "Phase1Sequence")
        }, "BossAI");

        tree.SetBlackboard(blackboard);
        return tree;
    }
}

//Boss 阶段枚举（移到命名空间外，让 BTBuilder 也能用）
public enum BossPhase
{
    Phase1,
    Phase2,
    Phase3
}