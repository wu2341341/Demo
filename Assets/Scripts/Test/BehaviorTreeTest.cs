using UnityEngine;
using BehaviorTree;

public class BehaviorTreeTest : MonoBehaviour
{
    private BTNode tree;
    private Blackboard blackboard;

    void Start()
    {
        blackboard = new Blackboard();
        blackboard.Set("testValue", 10);

        //构建一个简单的行为树：
        //Selector
        // ├─ Condition: testValue > 5
        // │   └─ Action: 打印 "条件成立"
        // └─ Action: 打印 "条件不成立"

        tree = new Selector(new System.Collections.Generic.List<BTNode>
        {
            new Sequence(new System.Collections.Generic.List<BTNode>
            {
                new ConditionNode(() => blackboard.Get<int>("testValue") > 5),
                new ActionNode(() => {
                    Debug.Log("? 条件成立！testValue > 5");
                    return BTStatus.Success;
                })
            }),
            new ActionNode(() => {
                Debug.Log("? 条件不成立！testValue <= 5");
                return BTStatus.Success;
            })
        });

        tree.SetBlackboard(blackboard);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("=== 执行行为树 ===");
            tree.Execute();
            blackboard.DebugPrint();
        }

        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            int val = blackboard.Get<int>("testValue");
            blackboard.Set("testValue", val + 1);
            Debug.Log($"testValue 增加到 {val + 1}");
        }

        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            int val = blackboard.Get<int>("testValue");
            blackboard.Set("testValue", val - 1);
            Debug.Log($"testValue 减少到 {val - 1}");
        }
    }
}