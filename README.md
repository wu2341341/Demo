\# ARPG Demo



一个用于求职的 3D 动作 RPG Demo，从零实现完整的战斗系统、AI 框架和游戏循环。



> 引擎：团结引擎（基于 Unity 2022 LTS）｜ 语言：C# ｜ 无商业化，仅作技术展示



\---



\## 核心亮点



\### 1. 手写行为树 AI 框架



\- 从零实现 `Sequence` / `Selector` 组合节点、条件节点、动作节点

\- 支持 `Running` 状态跨帧恢复执行（`currentIndex` 缓存）

\- 基于 `Blackboard`（黑板系统）实现节点间数据共享

\- 支持动态重置（`Reset()`），解决阶段切换时的状态残留 Bug



\### 2. 多阶段 Boss 战



\- \*\*Phase 1\*\*：近战追击 + 单体攻击

\- \*\*Phase 2\*\*：防御减伤 50% + 召唤 5 个小兵

\- \*\*Phase 3\*\*：狂暴（移速 1.5x、攻速 2x）+ 不分敌我 AOE

\- 阶段切换时重置行为树 + 注册/取消动态障碍物



\### 3. 三段连招系统



\- \*\*输入缓冲队列\*\*：动画期间按下的攻击键会被缓存，在连招窗口自动消费

\- \*\*连招窗口\*\*：由动画事件在特定帧开启，控制输入时机

\- \*\*攻击取消\*\*：闪避可打断攻击后继续连招，受击则重置连招段数

\- \*\*超时兜底\*\*：3 秒未完成连招自动重置，防止状态卡死



\### 4. A\* 寻路 + 二叉堆优化



\- 从零实现 A\* 网格寻路，使用\*\*二叉最小堆\*\*替代 List

\- openSet 的查找/插入/删除从 O(n) 降到 \*\*O(log n)\*\*

\- 通过 `Dictionary<Node, int>` 维护节点索引，支持 `UpdateNode` 动态调整

\- 支持\*\*动态障碍物\*\*：Boss 防御时注册，小兵自动绕行

\- \*\*卡住检测兜底\*\*：位置检测 + 射线找空位，自动绕行防止物理卡死



\### 5. 战斗反馈体系



\- \*\*动画事件精准判定\*\*：伤害在攻击动画的特定帧触发

\- \*\*受击反馈\*\*：闪白、硬直、屏幕震动、镜头抖动、伤害数字

\- \*\*Ragdoll 物理死亡\*\*：禁用 Animator 后启用骨骼物理，比预录动画更自然

\- \*\*对象池管理伤害数字\*\*：单例 + 池空自动扩容，减少 GC 压力



\### 6. 完整游戏循环



\- 开始界面 → 战斗 → 胜利/失败结算 → 返回主菜单

\- `SceneManager` 场景管理 + `Time.timeScale` 暂停

\- 鼠标锁定/解锁随场景切换自动处理



\---



\## 技术栈



| 分类 | 技术 |

|------|------|

| 引擎 | 团结引擎（基于 Unity 2022 LTS） |

| 语言 | C# |

| AI | 手写行为树 + 黑板系统 |

| 寻路 | 手写 A\* + 二叉最小堆 |

| 战斗 | 动画事件 + 输入缓冲 + 状态机 |

| 物理 | CharacterController + Ragdoll |

| UI | TextMeshPro + RectMask2D 遮罩方案 |

| 字体 | 思源黑体（SIL OFL 开源许可） |



\---



\## 项目结构



```

Assets/

├── Scripts/

│   ├── BehaviorTree/          # 手写行为树框架

│   │   ├── Core/              # BTNode、BTStatus

│   │   ├── Nodes/             # Sequence、Selector、Condition、Action

│   │   ├── Blackboard.cs      # 黑板系统

│   │   └── BBKeys.cs          # 黑板键常量

│   ├── Boss/                  # Boss 三阶段 AI

│   │   ├── BossController.cs  # 阶段管理 + 行为树调度

│   │   ├── BossCombat.cs      # 伤害判定

│   │   ├── BossSummoner.cs    # 召唤小兵

│   │   └── BossBTBuilder.cs   # 行为树构建

│   ├── Combat/                # 战斗系统

│   │   ├── ComboSystem.cs     # 三段连招

│   │   ├── InputBuffer.cs     # 输入缓冲

│   │   └── IDamageable.cs     # 伤害接口

│   ├── Enemy/                 # 普通敌人 AI

│   ├── Pathfinding/           # A\* 寻路

│   │   ├── AStarPathfinding.cs

│   │   ├── GridManager.cs

│   │   ├── MinHeap.cs         # 二叉最小堆

│   │   └── Node.cs

│   ├── Player/                # 玩家控制

│   │   ├── PlayerController.cs

│   │   └── PlayerCamera.cs

│   ├── UI/                    # UI 系统

│   │   ├── BossHealthUI.cs

│   │   ├── DamageNumber.cs

│   │   ├── DamageNumberPool.cs

│   │   ├── GameOverUI.cs

│   │   └── StartMenu.cs

│   └── StuckAvoidance.cs      # 卡住检测兜底

├── Scenes/

│   ├── StartMenu.unity        # 开始界面

│   └── SampleScene.unity      # 战斗场景

├── Prefabs/

├── Animators/

├── Materials/

└── Fonts/                     # 思源黑体

```



\---



\## 如何运行



\### 环境要求



\- 团结引擎或 Unity 2022 LTS

\- 已安装 TextMeshPro 包



### 方式一：直接下载

[点击下载 ARPG Demo v1.0 (Windows)](https://github.com/wu2341341/Demo/releases/download/v1.0/ARPG_Demo_Windows.zip)

解压后双击 `Demo.exe` 即可运行。

### 方式二：从源码运行

1\. 克隆仓库

&#x20;  ```bash

&#x20;  git clone https://github.com/wu2341341/Demo.git

&#x20;  ```

2\. 用 Unity Hub 打开项目

3\. 打开 `Assets/Scenes/StartMenu.unity`

4\. 点击 Play 运行



\### 操作说明



| 按键 | 功能 |

|------|------|

| WASD | 移动 |

| 鼠标 | 视角 |

| 鼠标左键 | 攻击（连按触发三段连招） |

| 空格 | 闪避（有无敌帧） |

| 鼠标滚轮 | 缩放视角 |



\---



\## 依赖说明



本项目使用了以下\*\*第三方资源\*\*，出于版权原因\*\*未包含在仓库中\*\*：



| 资源 | 用途 | 获取方式 |

|------|------|----------|

| StarterAssets | 角色控制器基础 | Unity Asset Store 免费下载 |

| DoubleL | 角色模型和动画 | Asset Store |

| UnityTechnologies | 环境资源 | Asset Store |



如果打开场景后模型/动画丢失，请从 Asset Store 导入上述资源包，放入 `Assets/ThirdParty/` 对应目录即可。



\---



\## 关于引擎选择



本项目使用\*\*团结引擎\*\*（Unity 中国版，基于 Unity 2022 LTS）开发。核心 API 与国际版一致，所有代码都是引擎无关的 C# 实现，可平滑迁移到国际版 Unity。

