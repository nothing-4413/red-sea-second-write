# 红海二笔 Unity C# Demo

这是按《红海一笔》架构设计线计划实现的 Unity 2022.3 LTS 离线三消原型。

## 启动

场景默认绑定 Assets/Config/LevelConfig_001.asset；棋盘尺寸、颜色、seed、目标、道具和安全上限从该资产转换到 Core。

1. 使用 Unity 2022.3 LTS 打开 `Demo/UnityProject`。
2. 打开 `Assets/Scenes/GameScene.unity` 并运行。
3. 点击相邻糖果提交交换；无效交换会回滚且不扣步数。
4. 右侧可使用 5×5 范围道具、重开和固定操作回放。

## 代码边界

RuleContracts.cs 定义 IRulePipeline 契约和默认适配器；TurnResolver 通过该边界依赖规则，不直接绑定静态实现。

- `Assets/Scripts/Core/Match3Core.cs`：纯 C# 数据模型、匹配、交换验证、事务快照、消除、障碍物、下落、补充和摘要。
- `Assets/Scripts/Core/Rules/MvpRulePipeline.cs`：MVP 唯一规则结算入口，编排交换/范围道具、消除、障碍物伤害、下落、补充、连锁和 `ResolveSummary`。
- `Assets/Scripts/Core/Match3Core.cs` 中的 `ResolveSystem`：兼容遗留入口，已标记 `Obsolete`；新代码不得直接调用。
- `Assets/Scripts/Flow/TurnStateMachine.cs`：Idle → Selecting → Resolving → Animating → Refilling → CheckingChain → Idle，统一输入锁。
- `Assets/Scripts/Presentation/Match3DemoController.cs`：提交命令、消费 FIFO 事件、更新状态，不直接推导规则。
- `Assets/Scripts/Presentation/BoardView.cs`：只读取 BoardModel，并将逻辑格映射到资源表现。
- `Assets/Scripts/Config/LevelConfigAsset.cs`：ScriptableObject 关卡配置入口。

## 规则状态

规则主流程固定为：Input → TurnStateMachine → TurnResolver → SwapValidator → MvpRulePipeline → ShuffleSystem（无合法移动时）→ EventQueue → Presentation → Goal/Score → Idle/Win/Lose。

ArchitectureTests 还验证了 TurnResolver 的有效/非法交换提交、FIFO 事件入队与消费、回放摘要记录，以及范围道具不消耗步数。

MVP 开启普通三连、确定性补充、障碍物和 5×5 道具；Rocket/Bomb/FlyingBomb/ColorBomb/SpecialCombo 仅保留枚举和配置开关，默认关闭。

规则主流程固定为：`Input → TurnStateMachine → TurnResolver → SwapValidator → MvpRulePipeline → EventQueue → Presentation → Goal/Score → Idle/Win/Lose`。

## 独立核心测试

在仓库根目录运行：

```powershell
dotnet run --project Demo/CoreTests/CoreTests.csproj
```

测试不依赖 Unity 场景，覆盖匹配、有效交换、无效交换完整回滚、障碍物交换限制和 5×5 道具边界。
