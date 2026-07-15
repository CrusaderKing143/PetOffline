# Pet Offline Test Report

更新日期：2026-07-15  
报告状态：Milestone 0 Foundation 已执行真实 Unity 测试。  
结论：Foundation 通过；Day 1、Day 2、完整 UI、P0 全量与 Build 尚未通过。

## 环境确认

| 项目 | 结果 | 证据 |
| --- | --- | --- |
| ProjectVersion | 已确认 | 6000.3.14f1 (d68c3f99a318) |
| 本机 Unity Editor | 已确认存在 | C:\Program Files\Unity 6000.3.14f1\Editor\Unity.exe |
| Unity License/batchmode 可用性 | PASS | EditMode/PlayMode 日志均 code 0；Personal license 成功解析 |
| MCP Project Root | PASS（专属 relay） | `D:/UGit/PetOffline`；多实例时禁用全局 Console 结论 |

## 当前执行结果

| Gate | 状态 | 结果文件 |
| --- | --- | --- |
| Unity Import/Compile | PASS | EditMode.log、PlayMode.log；PetOffline 程序集成功编译 |
| Setup/Scenes | PASS | Setup.log；四个 Scene、Input Actions、Build Settings、稳定 Layer ID 已由 Unity 创建并回读 |
| ValidateBatch/Menu | PASS | Validation.log、ValidationReport.txt；2026-07-15 再次执行菜单 PASS |
| EditMode | PASS 1/1 | EditMode.xml、EditMode.log；exit code 0 |
| PlayMode | PASS 3/3 | PlayMode.xml、PlayMode.log；Bootstrap、UIRoot Mock、单 World 并发不变量；exit code 0 |
| Full title-to-Restore smoke | NOT RUN | 无 |
| Full title-to-Quiet smoke | NOT RUN | 无 |
| Windows x64 Development Build | NOT RUN | 无 |
| Windows x64 Release Build | NOT RUN | Builds/Windows/PetOffline.exe 尚不存在 |
| Standalone launch | NOT RUN | 无 Player.log/Smoke 结果 |
| Screenshots | NOT RUN | Artifacts/Screenshots 尚无验收证据 |

## P0 测试清单

以下全部待实现并实际执行：

- [x] Architecture boundary test（Foundation 范围）
- [ ] UIRoot disabled gameplay test
- [x] UIRoot mock preview test（Foundation 绑定范围）
- [ ] Day 1 shoe completion test
- [ ] Day 1 detection reset test
- [ ] Day 1 previous-task preservation test
- [ ] Day 1 pillow and robot interaction test
- [ ] Day 1 final report transition test
- [ ] Day 2 first 10-second confirmation test
- [ ] Day 2 feeder return resets progress test
- [ ] Day 2 ignored confirmation pauses progress test
- [ ] Day 2 feeder-camera disable test
- [ ] Day 2 backup-camera activation test
- [ ] Day 2 wrong-route confirmation test
- [ ] Day 2 correct-route 20-second completion test
- [ ] Restore Connection ending test
- [ ] Keep Quiet ending test
- [ ] Save/unlock test
- [ ] Full title-to-ending smoke test：Restore Connection
- [ ] Full title-to-ending smoke test：Keep Quiet

当前通过的 4 个测试只覆盖 Foundation；不得据此勾选 Day 1、Day 2、UIRoot 断电或完整流程。

## 最终验收记录要求

每次运行后在本文件追加：

- 完整命令、Unity 版本、Git commit、开始/结束时间和退出码。
- XML/log/截图/Build 的相对路径。
- Passed、Failed、Skipped 数量。
- 失败原因、修复 commit 和重跑结果。
- Standalone 实际启动路径及 Player.log 结论。

测试只有在真实命令退出成功且结果文件可读时才标记通过；脚本存在、Console 无可见红字或手工推测都不是通过证据。
