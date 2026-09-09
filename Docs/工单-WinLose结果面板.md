# 工单：Win/Lose 结果面板

| 字段 | 内容 |
| --- | --- |
| 任务名称 | 终态结果面板与数据链 |
| 所属模块 | Presentation / Flow |
| 输入数据 | `BoardModel`、`ResolveSummary`、`GameState.Win/Lose` |
| 实现内容 | `ResultPanelModel` 保存终态、分数、目标进度和剩余步数；`ResultPanel` 负责绘制；Controller 监听状态并绑定数据 |
| 验收标准 | Win/Lose 显示面板；非终态隐藏；终态输入仍锁定；重开清空面板并生成新回合 |
| 测试场景 | 纯 C# 结果模型 Win/Idle 状态切换；既有 Win/Lose 20 次点击回归 |
| 人工校验 | Unity 2022.3 LTS 中完成胜负操作，核对面板字段、重开行为和布局 |
| 已知风险 | 当前无 Unity 编辑器，真实 GUI 布局、按钮覆盖和终态截图尚未 PlayMode 验收 |
