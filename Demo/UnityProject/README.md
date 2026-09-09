# 红海二笔 Unity C# Demo

这是按《红海一笔》架构设计线计划实现的 Unity 2022.3 LTS 离线三消原型。

## 启动

场景默认绑定 Assets/Config/LevelConfig_001.asset；棋盘尺寸、颜色、seed、目标、道具和安全上限从该资产转换到 Core。

1. 使用 Unity 2022.3 LTS 打开 `Demo/UnityProject`。
2. 打开 `Assets/Scenes/GameScene.unity` 并运行。
3. 点击相邻糖果提交交换；无效交换会回滚且不扣步数。
4. 右侧可使用 5×5 范围道具、重开和固定操作回放。

表现或状态机错误快照导出到 `Application.persistentDataPath/ErrorSnapshots`。文件名包含 UTC 时间、回合编号和错误类型，JSON 内包含规则状态、seed、三路随机游标、事件队列长度和逻辑棋盘快照；导出失败时仍会通过 Unity Error 日志输出同一诊断上下文。

每个已提交回合在 Unity Console 输出一条 `[Performance]` 日志，分别记录总逻辑、匹配扫描、特殊规则、消除、下落补充、洗牌和 FIFO 表现播放耗时。MVP 关闭特殊糖果时 `specialMs` 固定为 0；性能数据不写入 `ResolveSummary` 或回放一致性数据。

## 代码边界

RuleContracts.cs 定义 IRulePipeline 契约和默认适配器；TurnResolver 通过该边界依赖规则，不直接绑定静态实现。

- `Assets/Scripts/Core/Match3Core.cs`：纯 C# 数据模型、匹配、交换验证、事务快照、消除、障碍物、下落、补充和摘要。
- `Assets/Scripts/Core/Rules/MvpRulePipeline.cs`：MVP 唯一规则结算入口，编排交换/范围道具、消除、障碍物伤害、下落、补充、连锁和 `ResolveSummary`。
- `Assets/Scripts/Core/Match3Core.cs` 中的 `ResolveSystem`：兼容遗留入口，已标记 `Obsolete`；新代码不得直接调用。
- `Assets/Scripts/Flow/TurnStateMachine.cs`：Idle → Selecting → Resolving → Animating → Refilling → CheckingChain → Idle，统一输入锁。
- `Assets/Scripts/Flow/InputController.cs`：纯 C# 选择状态与输入命令协调器；拥有回合编号，通过 `TurnResolver` 提交交换/道具，终态直接拒绝输入。
- `Assets/Scripts/Presentation/Match3DemoController.cs`：把鼠标命中坐标交给 InputController、启动表现播放并更新 UI，不直接提交规则命令、消费事件或推导规则。
- `Assets/Scripts/Presentation/EffectPlayer.cs`：表现事件队列的唯一消费者，按 FIFO 和配置时长顺序播放，并在超时后产生诊断回调。
- `Assets/Scripts/Presentation/BoardView.cs`：只读取 BoardModel，将逻辑格映射到资源表现，并应用播放器发出的事件提示。
- `Assets/Scripts/Config/LevelConfigAsset.cs`：ScriptableObject 关卡配置入口。

## 规则状态

规则主流程固定为：Input → TurnStateMachine → TurnResolver → SwapValidator → MvpRulePipeline → ShuffleSystem（无合法移动时）→ EventQueue → EffectPlayer（FIFO）→ BoardView → Goal/Score → Idle/Win/Lose。

ArchitectureTests 还验证了 TurnResolver 的有效/非法交换提交、FIFO 事件入队与消费、回放摘要记录，以及范围道具不消耗步数。

MVP 开启普通三连、确定性补充、障碍物和 5×5 道具；Rocket/Bomb/FlyingBomb/ColorBomb/SpecialCombo 仅保留枚举和配置开关，默认关闭。

规则主流程固定为：`Input → TurnStateMachine → TurnResolver → SwapValidator → MvpRulePipeline → EventQueue → Presentation → Goal/Score → Idle/Win/Lose`。

## 独立核心测试

在仓库根目录运行：

```powershell
dotnet run --project Demo/CoreTests/CoreTests.csproj
```

测试不依赖 Unity 场景，覆盖匹配、有效交换、无效交换完整回滚、障碍物交换限制和 5×5 道具边界。
