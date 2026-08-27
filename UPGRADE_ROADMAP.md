# LockStepSystem 升级路线

> 目标：从"单球 lockstep demo"升级为"确定性帧同步对抗 demo"——
> 双端同内核、服务器权威 + 帧哈希、KCP 可靠传输、预测回滚、追帧回放。
> 参照：心动 UE_game 战斗架构（网络分层 / 双端共用确定性世界 / 帧哈希 / ServerFirst）。
> **两线并行**：网络线（自己手写）/ 逻辑线（AI 按合同写），在"帧协议合同"处汇合。

## 架构三支柱

1. **帧协议合同**（先定，网络与逻辑的分界）：`FrameInput` / `FrameCmdSet{frameId, seed, preFrameHash, cmds}`——逻辑层定义内容，网络层只做信封与可靠运送。
2. **确定性世界 LockStepCore**（纯 C# 类库，零 Unity 依赖）：种子随机 + 固定数学 + 指令输入 + 帧哈希 + 序列化。
3. **同步模型**：服务器权威（服务器也跑 Core）+ 客户端本地预测 + 帧哈希校验 + KCP 可靠传输。

## 里程碑

| # | 里程碑 | 内容 | 验收标准 |
|---|---|---|---|
| M1 | **C# 服务端** | 独立 .NET Console(`LockStepServer`)，移植现有 C++ 服务端逻辑（socket / 帧存储 / Join 下发历史 / 超时补位），协议沿用现有 struct | `dotnet run` 可跑，Unity 客户端能连 |
| M2 | **确定性世界** | `LockStepCore`：种子随机 / 固定步长 / 指令输入 / 帧哈希 / 序列化；客户端表现层改为"读世界状态→渲染插值"（本地也跑世界） | 本地逻辑帧稳定 30fps 不被网络卡；同指令+同种子→哈希一致 |
| M3 | **协议重定义 + 权威连接** | 合同落地（下行含 `seed/frameId/preFrameHash`）；服务器跑 Core 算权威帧哈希；客户端校验不匹配即报错 | 无丢包时帧哈希全程一致；不一致能定位 |
| M4 | **KCP 传输** | 战斗通道接 KCP（C# 移植或引用现成 `kcp-csharp`），保留裸 UDP 开关 | 10% 丢包下帧同步不中断（对照裸 UDP 卡死） |
| M5 | **模拟实验室** | 延迟/丢包/乱序模拟开关 + 重传/吞吐/逻辑帧率统计 | 可演示各级网络劣化下的表现对比 |
| M6 | **预测回滚** | 客户端超前 N 帧预测；服务器帧到达→过期帧重算修正 | 丢帧时手感平滑，回滚后状态仍与哈希一致 |
| M7 | **回放 + 追帧 + 检验** | 录指令→离线重演（倍速）；Join 下发历史/快照→客户端加速模拟追上接实时；哈希离线回归测试 + Bot 压测 | 任意局可回放；后加入者能追上；回归脚本通过 |

**M2 成败点**：客户端逻辑帧修到 30fps（现在是个位数）；做偏成"客户端纯表现"= 退化成快照同步。

## 心动对照速查

- 网络分层：`SocketMsg.h`（UDP 8 字节头 / TCP 20 字节头）、`NetSingleton.h`（通道划分）
- 帧指令流：`BattleMgr.lua:596`（上行 PushCommand）、`BattleMgr.lua:673`（下行 RecvPushCommand）、`FightMsg.cpp:307`（AddServerMsg）
- 权威+预测：`FightRunLogic.cpp:124`（World_Prepare）、`BattleMgr.lua:748`（IsHashError 帧哈希校验）
- 确定性随机：`CDamageUtils.cpp:12`（SGT(Math)->Random）
- 回放/追帧：`ReplayHelper.h`、`FightRunLogic.cpp:463`（RunInitWorldAsReplay）
