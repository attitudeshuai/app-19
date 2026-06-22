# 软删除功能回归测试计划

> **文档版本**：v1.0  
> **更新日期**：2026-06-22  
> **适用对象**：测试团队、质量保障（QA）负责人  
> **预计执行时间**：功能测试 4 人日 / 性能测试 2 人日 / 合计 6 人日

---

## 一、测试范围

### 1.1 功能测试范围

| 模块 | 测试重点 | 优先级 |
|------|----------|--------|
| **软删除核心机制** | SaveChanges 拦截器是否正确将物理删除转换为软删除 | P0 |
| | 全局查询过滤器是否正确过滤已软删除记录 | P0 |
| | 绕过过滤器（IgnoreQueryFilters）的方法是否正常工作 | P1 |
| **四张业务表 CRUD** | SharePosts 删除/查询/恢复 | P0 |
| | Reservations 删除/查询/恢复 | P0 |
| | PickupCodes 删除/查询/恢复 | P1 |
| | KarmaPoints 删除/查询/恢复 | P1 |
| **级联软删除** | 删除 SharePost 是否级联删除关联的 Reservation 和 PickupCode | P0 |
| | 删除 Reservation 是否级联删除关联的 PickupCode | P0 |
| | 级联记录的 DeletionReason 是否正确填充 | P1 |
| **审计快照** | 每次软删除是否生成快照记录 | P0 |
| | 快照数据（SnapshotData JSON）是否完整、正确 | P0 |
| | EntityDisplayName / OriginalOwnerId 是否按规则生成 | P1 |
| **回收站 API** | `GET /api/recyclebin` 列表查询（分页/筛选/权限） | P0 |
| | `GET /api/recyclebin/snapshots/{id}` 快照详情 | P1 |
| | `POST /api/recyclebin/restore` 恢复功能 | P0 |
| | `DELETE /api/recyclebin/snapshots/{id}` 管理员永久删除 | P2 |
| **实体恢复** | 恢复后 IsDeleted / DeletedAt 等字段是否正确重置 | P0 |
| | 恢复后数据是否立即出现在正常查询中 | P0 |
| | 恢复成功后快照记录是否被清理 | P1 |
| **六个月物理清理** | HardCleanupScheduler 是否按计划时间触发 | P1 |
| | HardCleanupService 清理顺序是否正确（避免外键错误） | P0 |
| | 清理条件是否正确（只清理超过 6 个月的） | P0 |
| | Repository.HardDelete 是否真正执行物理删除（不被拦截） | P1 |
| | ScheduledTaskLogs 是否正确记录每次清理 | P1 |
| **权限控制** | 用户只能看到自己删除/拥有的回收站记录 | P0 |
| | 非删除者 / 非所有者无法恢复他人记录 | P0 |
| | 非管理员无法调用永久删除接口（userId ≠ 1） | P1 |
| **兼容性** | 原有 DELETE 接口契约（路由/参数/状态码/响应格式）是否不变 | P0 |
| | 原有 GET 列表/详情接口结果是否与物理删除时代码语义一致 | P0 |
| | 前端是否无需任何修改即可工作 | P0 |

### 1.2 非功能测试范围

| 类别 | 测试项目 | 优先级 |
|------|----------|--------|
| **性能** | 带全局查询过滤器的查询性能（对比未启用过滤器的基线） | P0 |
| | 删除操作的性能开销（快照生成 + 级联处理耗时） | P1 |
| | 6个月清理任务处理大数据量时的性能与锁影响 | P1 |
| | 复合索引 `(IsDeleted, DeletedAt)` 是否被查询优化器正确选用 | P0 |
| **可靠性** | 事务中部分失败是否会回滚软删除和快照（原子性） | P1 |
| | 应用重启后 HardCleanupScheduler 是否能正确补跑 | P2 |
| | 并发删除同一实体时是否有竞态条件 | P2 |
| **安全** | SnapshotData 中是否可能泄漏敏感字段（如密码哈希） | P1 |
| | 是否存在越权查看/恢复他人数据的可能 | P0 |

### 1.3 不在本次测试范围的内容

- 用户表（Users）、通知表（Notifications）、任务日志表（ScheduledTaskLogs）的软删除（未启用）
- 数据库层面的物理备份/恢复流程（属于 DBA 运维范围）
- 前端 UI 交互测试（前端对接阶段进行）
- 移动端适配（非本次范围）

---

## 二、功能测试用例矩阵

### 2.1 测试环境准备

#### 测试账号

| 账号类型 | 用户名 / ID | 用途 |
|----------|-------------|------|
| 管理员 | `admin` / userId=1 | 测试管理员专属接口（永久删除） |
| 普通用户 A | `userA` / userId=2 | 创建、删除、恢复自己的数据 |
| 普通用户 B | `userB` / userId=3 | 验证权限隔离（无法操作 userA 的数据） |
| 普通用户 C | `userC` / userId=4 | 预订 userA 发布的帖子，验证级联删除 |

#### 前置数据脚本

```sql
-- 测试开始前，确保测试库有以下基础数据
INSERT INTO Users (Id, Username, Email, PasswordHash, CreatedAt) VALUES
(1, 'admin',   'admin@test.com',   'HASHED_ADMIN_PWD',   NOW()),
(2, 'userA',   'usera@test.com',   'HASHED_USERA_PWD',   NOW()),
(3, 'userB',   'userb@test.com',   'HASHED_USERB_PWD',   NOW()),
(4, 'userC',   'userc@test.com',   'HASHED_USERC_PWD',   NOW());
```

---

### 2.2 P0 级核心用例（必须 100% 通过）

| 用例编号 | 用例名称 | 前置条件 | 操作步骤 | 预期结果 | 优先级 |
|----------|----------|----------|----------|----------|--------|
| **F001** | 删除 SharePost 验证软删除标记 | 登录 userA，创建一条分享帖 ID=1001 | 1. `DELETE /api/shareposts/1001`<br>2. 直接查询数据库：`SELECT Id, IsDeleted, DeletedAt, DeletedBy FROM SharePosts WHERE Id = 1001` | 1. 接口返回 200 success<br>2. 数据库中该记录**未被删除**，且：<br>  - `IsDeleted = 1`<br>  - `DeletedAt` 非空（UTC 时间，误差 ≤ 1 分钟）<br>  - `DeletedBy = 2` | P0 |
| **F002** | 全局查询过滤器过滤已删除帖子 | F001 执行完毕 | 1. `GET /api/shareposts`（列表）<br>2. `GET /api/shareposts/1001`（详情） | 1. 列表中不出现 ID=1001 的帖子<br>2. 详情接口返回 404（与物理删除语义一致） | P0 |
| **F003** | 删除 SharePost 时生成审计快照 | F001 执行完毕 | `SELECT * FROM DeletedEntitySnapshots WHERE EntityType = 'SharePost' AND EntityId = 1001` | 存在 1 条快照记录：<br>  - EntityType=`SharePost`<br>  - EntityId=`1001`<br>  - DeletedBy=`2`<br>  - OriginalOwnerId=`2`<br>  - EntityDisplayName 格式为「分享帖 #1001 - xxx」<br>  - SnapshotData 为合法 JSON，包含 Title/Description 等所有字段 | P0 |
| **F004** | 回收站列表显示自己删除的记录 | F001 执行完毕，登录 userA | `GET /api/recyclebin?entityType=SharePost` | 返回 200，data.items 中包含 ID=1001 的快照，分页信息正确 | P0 |
| **F005** | 回收站权限隔离：userB 看不到 userA 的记录 | F001 执行完毕，登录 userB | `GET /api/recyclebin` | 返回 200，data.items 为空列表（或 totalCount=0），**不包含** userA 删除的帖子 | P0 |
| **F006** | 恢复已删除的 SharePost | F001 执行完毕，登录 userA | 1. 从 F004 获取快照 snapshotId<br>2. `POST /api/recyclebin/restore` body: `{"id": snapshotId, "entityType": "SharePost"}`<br>3. `GET /api/shareposts/1001` | 1. 恢复接口返回 200 + 帖子详情<br>2. 详情接口正常返回该帖子<br>3. 数据库验证：`IsDeleted=0, DeletedAt=NULL, DeletedBy=NULL`<br>4. 对应快照记录已从 DeletedEntitySnapshots 中删除 | P0 |
| **F007** | 无权限恢复他人记录 | F001 执行完毕，登录 userB | 1. （需通过 SQL 或管理员接口获取 snapshotId）<br>2. `POST /api/recyclebin/restore` body: `{"id": snapshotId, "entityType": "SharePost"}` | 返回 403 Forbidden，错误信息为「无权限恢复此记录」 | P0 |
| **F008** | 删除 SharePost 级联删除 Reservation 和 PickupCode | 1. userA 创建帖子 ID=2001<br>2. userC 预订该帖子生成 Reservation ID=3001 和 PickupCode ID=4001<br>3. 登录 userA | 1. `DELETE /api/shareposts/2001`<br>2. 查询数据库三张表的 IsDeleted 字段 | 1. 接口返回 200<br>2. SharePosts[2001].IsDeleted = 1<br>3. Reservations[3001].IsDeleted = 1，且 DeletionReason 包含「级联删除」<br>4. PickupCodes[4001].IsDeleted = 1，且 DeletionReason 包含「级联删除」 | P0 |
| **F009** | 删除 Reservation 级联删除 PickupCode | 1. userA 创建帖子 ID=2002<br>2. userC 预订该帖子生成 Reservation ID=3002 和 PickupCode ID=4002<br>3. 登录 userC | 1. `DELETE /api/reservations/3002`<br>2. 查询数据库 | 1. 接口返回 200<br>2. Reservations[3002].IsDeleted = 1<br>3. PickupCodes[4002].IsDeleted = 1，DeletionReason 包含「级联删除」<br>4. SharePosts[2002].IsDeleted = 0（帖子不受影响） | P0 |
| **F010** | 物理清理只清理超过 6 个月的数据 | 通过 SQL 预置测试数据：<br>• SP1：DeletedAt = 7 个月前，IsDeleted=1<br>• SP2：DeletedAt = 5 个月前，IsDeleted=1<br>• SP3：未删除 | 1. 调用 HardCleanupService.CleanupExpiredSoftDeletesAsync(6个月前)<br>2. 查询数据库 | 1. SP1 **被物理删除**（行数 -1）<br>2. SP2 **保留**（仍在表中，IsDeleted=1）<br>3. SP3 不受影响<br>4. 对应 SP1 的快照记录也被删除 | P0 |
| **F011** | DELETE 接口契约保持不变（兼容性） | 无 | 对比软删除部署前后的 OpenAPI 文档（Swagger.json）中 DELETE 接口定义 | DELETE 接口的：<br>• 路由不变<br>• 请求参数不变<br>• 成功状态码不变（200）<br>• 失败状态码不变（401/403/404）<br>• 响应结构（ApiResponse 包装）不变 | P0 |
| **F012** | GET 接口返回语义与物理删除一致 | 同一数据库状态下，对比：<br>A 模式：启用软删除 + 已删除 IsDeleted=1<br>B 模式：物理删除后（不存在） | 调用 GET 列表和 GET 详情 | A 模式与 B 模式返回结果**完全一致**（列表无该项，详情 404） | P0 |

---

### 2.3 P1 级重要用例（建议 100% 通过）

| 用例编号 | 用例名称 | 操作步骤 | 预期结果 | 优先级 |
|----------|----------|----------|----------|--------|
| F101 | 删除 Reservation 验证软删除标记 | 创建预订后 DELETE，查库 | Reservation 表 IsDeleted=1，DeletedAt/DeletedBy 正确 | P1 |
| F102 | 删除 PickupCode 验证软删除标记 | 删除取餐码后查库 | PickupCodes IsDeleted=1，其余字段正确 | P1 |
| F103 | 删除 KarmaPoint 验证软删除标记 | 删除积分流水后查库 | KarmaPoints IsDeleted=1，其余字段正确 | P1 |
| F104 | 回收站四种实体类型筛选分别生效 | 分别为每种实体各造 1 条删除数据，逐类型查询 | 每次查询只返回指定 EntityType 的记录 | P1 |
| F105 | 回收站分页正确 | 造 25 条删除数据，pageSize=10 | page1 返回 10 条，page2 返回 10 条，page3 返回 5 条，TotalCount=25 | P1 |
| F106 | 回收站默认按删除时间倒序 | 按不同时间造 3 条记录 | 返回顺序按 DeletedAt DESC，最新的在前 | P1 |
| F107 | 快照详情字段完整 | `GET /api/recyclebin/snapshots/{id}` | 返回的 snapshotData 包含原实体所有字段，值与删除前一致 | P1 |
| F108 | 快照预览 SnapshotDataPreview 正确 | 从列表接口获取 item | 能正确截取 Title/Status/Reason 等关键字段 | P1 |
| F109 | 恢复 Reservation 正确 | 删除预订 → 恢复 | 恢复后 IsDeleted=0，可正常出现在列表和详情中 | P1 |
| F110 | 恢复 PickupCode 正确 | 删除取餐码 → 恢复 | 恢复后 IsDeleted=0，可正常查询 | P1 |
| F111 | 恢复 KarmaPoint 正确 | 删除积分 → 恢复 | 恢复后 IsDeleted=0，可正常查询 | P1 |
| F112 | 重复恢复幂等 | 对同一条已恢复记录再次调恢复接口 | 返回 400/200（语义正确），不会报错或重复操作 | P1 |
| F113 | 恢复不存在的实体 | 使用已物理删除的 entityId 调恢复 | 返回 404「分享帖子不存在或已被物理删除」 | P1 |
| F114 | 非法 entityType 恢复 | `POST /api/recyclebin/restore` entityType=`User` | 返回 400「无效的实体类型」 | P1 |
| F115 | Repository.GetByIdIgnoreFilterAsync 可获取已删除 | 软删除一条帖子后，内部调忽略过滤接口 | 能获取到该帖子（IsDeleted=1），而普通 GetByIdAsync 返回 null | P1 |
| F116 | HardCleanupScheduler 启动日志正确 | 正常启动应用（HardCleanupEnabled=true） | 应用日志包含「每月物理清理定时任务调度器已启动」 | P1 |
| F117 | 禁用物理清理后调度器不启动 | 修改配置 HardCleanupEnabled=false，重启 | 应用日志包含「每月物理清理定时任务已被禁用」 | P1 |
| F118 | ScheduledTaskLogs 记录清理结果 | 执行一次清理任务 | ScheduledTaskLogs 表新增一条 TaskName=MonthlyHardCleanup 的记录，状态为 Success/PartialSuccess/Failed | P1 |
| F119 | 清理任务 ScheduledTaskLogs 统计字段正确 | 清理了若干条数据后 | ExpiredSharePostsCount 等字段值与实际删除数量一致 | P1 |
| F120 | 非管理员无法永久删除快照 | 登录 userA，调用 `DELETE /api/recyclebin/snapshots/{id}` | 返回 403「无权限执行此操作，仅管理员可永久删除」 | P1 |

---

### 2.4 P2 级边缘用例（尽量覆盖）

| 用例编号 | 用例名称 | 操作步骤 | 预期结果 | 优先级 |
|----------|----------|----------|----------|--------|
| F201 | 回收站搜索关键词匹配 EntityDisplayName | 造多条数据，用标题关键词搜索 | 能正确返回匹配项 | P2 |
| F202 | 回收站搜索关键词匹配 DeletionReason | 对级联删除的记录用「级联删除」搜索 | 能正确返回匹配项 | P2 |
| F203 | 回收站按 entityType 升序排序 | 请求 sortBy=entityType&sortDirection=asc | 按字母顺序排列 | P2 |
| F204 | 快照显示名四种格式均正确 | 分别删除四种实体 | EntityDisplayName 符合规则，无异常格式 | P2 |
| F205 | OriginalOwnerId 四种实体均正确 | 删除 SharePost/Reservation/KarmaPoint（PickupCode 为 null） | OriginalOwnerId 值与业务逻辑一致（PosterId/ClaimerId/UserId） | P2 |
| F206 | 管理员永久删除快照成功 | 登录 admin 调用永久删除 | 快照记录从表中删除，返回 200 | P2 |
| F207 | 永久删除不存在的快照 ID | admin 调用删除不存在的 id | 返回 404 | P2 |
| F208 | 并发删除同一实体（乐观锁测试） | 两个线程同时 DELETE 同一条记录 | 其中一个成功（200），另一个返回 404（或 200 幂等），数据库最终状态一致 | P2 |
| F209 | 事务中软删除后抛异常，全部回滚 | 使用 TransactionScope，删除后抛 Exception | 业务表和快照表均无新增/修改（事务原子性） | P2 |
| F210 | DeletionReason 字段超长（500+字符） | 删除时传入超长 DeletionReason | 不抛异常，自动截断或数据库正确报错（取决于实现） | P2 |
| F211 | 级联恢复测试（删除 SharePost 后只恢复 SharePost） | 见迁移方案 3.7 节「级联恢复说明」 | 只恢复 SharePost，Reservation 和 PickupCode 仍保持已删除（符合预期） | P2 |

---

## 三、接口兼容性测试

### 3.1 兼容性验证思路

使用**对比测试法**：在两个版本的代码（软删除前 vs 软删除后）上，使用相同请求输入集合，对比输出结果是否语义一致。

### 3.2 接口清单及测试要点

| 接口 | 方法 | 兼容性等级 | 测试要点 |
|------|------|------------|----------|
| `/api/shareposts` | GET | **完全兼容** | 列表结构、分页、排序、过滤、总数完全一致 |
| `/api/shareposts/{id}` | GET | **完全兼容** | 已删除的 id 在两个版本均返回 404 |
| `/api/shareposts` | POST | **完全兼容** | 创建成功状态码、响应结构完全一致 |
| `/api/shareposts/{id}` | PUT | **完全兼容** | 修改后的字段值完全一致 |
| `/api/shareposts/{id}` | DELETE | **行为兼容（内部不同）** | 接口契约（状态码/响应）相同，但数据库层从 DELETE → UPDATE |
| `/api/shareposts/{id}/status` | PATCH | **完全兼容** | 状态修改逻辑无变化 |
| `/api/reservations` | GET | **完全兼容** | 同上 |
| `/api/reservations/{id}` | GET | **完全兼容** | 同上 |
| `/api/reservations` | POST | **完全兼容** | 同上 |
| `/api/reservations/{id}` | DELETE | **行为兼容（内部不同）** | 同上 |
| `/api/pickupcodes` | 各方法 | **完全兼容/行为兼容** | 按上述原则逐一验证 |
| `/api/karmapoints` | 各方法 | **完全兼容/行为兼容** | 按上述原则逐一验证 |
| `/api/stats/*` | GET | **完全兼容** | 统计指标计算方式不变（应排除已删除记录） |

### 3.3 自动化兼容性测试脚本（示例）

可编写 Postman Collection Runner 或 Newman 脚本批量执行：

```javascript
// Postman Test Script 示例：DELETE 接口契约验证
pm.test("DELETE /api/shareposts/{id} 状态码为 200", function () {
    pm.response.to.have.status(200);
});

pm.test("响应体包含 ApiResponse 标准结构", function () {
    var jsonData = pm.response.json();
    pm.expect(jsonData).to.have.property('code');
    pm.expect(jsonData).to.have.property('message');
    pm.expect(jsonData).to.have.property('data');
});

pm.test("GET 已删除 ID 返回 404", function () {
    // 可在 DELETE 后立即跟进 GET 验证
    pm.sendRequest({
        url: pm.environment.get("baseUrl") + "/api/shareposts/" + pm.environment.get("deletedPostId"),
        method: 'GET'
    }, function (err, res) {
        pm.expect(res.code).to.eql(404);
    });
});
```

### 3.4 前端回归测试要点

| 前端场景 | 预期表现 |
|----------|----------|
| 分享帖列表页 | 删除帖子后，刷新页面后列表中不再出现该帖子 |
| 分享帖详情页 | 通过已删除帖子的 URL 直接访问，显示「帖子不存在或已被删除」的 404 页面 |
| 预订管理页 | 删除预订后，该预订不再出现在「我的预订」列表 |
| 积分流水页 | （如支持删除积分）删除后积分流水中不再显示 |
| 数据统计卡片 | 删除帖子后，相关统计数字（如帖子总数）减少 |
| 通知中心 | 通知仍可点击跳转到已删除帖子的 404 页（与物理删除一致，不会报错） |

---

## 四、性能测试关注点（索引是否生效）

### 4.1 性能基线建立

在启用软删除**之前**（或在关闭过滤器的对照环境中）建立以下基线数据，用于对比：

| 基准查询 | 目标 | 基线标准（示例） |
|----------|------|-----------------|
| `SELECT COUNT(*) FROM SharePosts` | 全表扫描性能 | < 100ms（10 万行级别） |
| `SELECT * FROM SharePosts ORDER BY CreatedAt DESC LIMIT 10` | 列表查询 P95 响应时间 | < 50ms |
| `DELETE FROM SharePosts WHERE Id = X` | 删除操作耗时 | < 30ms |

### 4.2 软删除引入后的性能对比测试

| 测试项 | SQL / 操作 | 验证重点 | 合格标准 |
|--------|-----------|----------|----------|
| **带过滤器的列表查询** | `SELECT * FROM SharePosts ORDER BY CreatedAt DESC LIMIT 10`（EF Core 会自动附加 IsDeleted=0） | 1. `EXPLAIN` 输出中是否使用了 `IX_SharePosts_IsDeleted_DeletedAt` 或主索引<br>2. 响应时间对比基线增幅 ≤ 20% | P95 < 60ms，增幅 ≤ 20% |
| **带过滤器的详情查询** | `SELECT * FROM SharePosts WHERE Id = X AND IsDeleted = 0` | 主键查询仍使用 PRIMARY 索引（预期不会退化） | P95 < 10ms |
| **带过滤器的统计查询** | `SELECT COUNT(*) FROM SharePosts WHERE IsDeleted = 0` | 是否使用了 `(IsDeleted, DeletedAt)` 复合索引 | P95 < 150ms |
| **按时间范围扫描已删除记录**（回收站场景） | `SELECT * FROM DeletedEntitySnapshots WHERE DeletedBy=Y ORDER BY DeletedAt DESC LIMIT 20` | `IX_DeletedEntitySnapshots_DeletedBy_DeletedAt` 是否命中 | P95 < 50ms |
| **按时间范围清理**（6 个月任务） | `DELETE FROM SharePosts WHERE IsDeleted=1 AND DeletedAt < '6个月前' LIMIT 1000` | 1. 是否使用 `(IsDeleted, DeletedAt)` 索引<br>2. 锁范围是否合理（不会锁全表）<br>3. 单次 1000 条清理耗时 | 单次 < 2s，无死锁 |
| **软删除操作开销** | `DELETE /api/shareposts/{id}`（即 UPDATE + INSERT 快照） | 对比原物理删除：多了快照 INSERT，耗时增幅应 ≤ 100% | 平均 < 60ms |
| **级联删除性能** | 删除含 10 个 Reservation + 10 个 PickupCode 的 SharePost | 验证 O(N) 级联处理的线性可扩展性 | 总耗时 < 500ms |
| **已删除数据比例较高时的性能** | 构造 30% 记录为 IsDeleted=1 的测试数据集，重复执行上述核心查询 | 性能不应出现非线性退化 | 增幅 ≤ 50% |

### 4.3 EXPLAIN 验证索引生效方法

以 SharePosts 表为例，验证全局查询过滤器是否正确使用索引：

```sql
-- 模拟 EF Core 带过滤器的列表查询（EXPLAIN ANALYZE 可看实际执行耗时）
EXPLAIN
SELECT * FROM SharePosts
WHERE IsDeleted = FALSE
ORDER BY CreatedAt DESC
LIMIT 10;

-- 预期输出（type 至少为 range 或 ref，key 列包含索引名）：
-- id  select_type  table       type  key                                rows  Extra
-- 1   SIMPLE       SharePosts  ref   IX_SharePosts_IsDeleted_DeletedAt  XXXX  Using where; Using filesort

-- 如果 type 为 ALL（全表扫描），说明索引未生效，需要排查
```

**回收站场景索引验证**：

```sql
EXPLAIN
SELECT * FROM DeletedEntitySnapshots
WHERE DeletedBy = 2 OR OriginalOwnerId = 2
ORDER BY DeletedAt DESC
LIMIT 10;

-- 注意：OR 条件可能导致索引失效，必要时改为 UNION ALL：
EXPLAIN
SELECT * FROM (
    SELECT * FROM DeletedEntitySnapshots WHERE DeletedBy = 2
    UNION ALL
    SELECT * FROM DeletedEntitySnapshots WHERE OriginalOwnerId = 2 AND DeletedBy <> 2
) t
ORDER BY DeletedAt DESC
LIMIT 10;
```

### 4.4 性能监控告警项（上线后持续观察）

| 监控项 | 阈值 | 告警级别 |
|--------|------|----------|
| DELETE 接口 P95 响应时间 | > 200ms 持续 5 分钟 | Warning |
| 核心查询慢查询占比（耗时 > 500ms） | > 0.5% | Warning |
| 数据库死锁次数 | > 10 次 / 小时 | Critical |
| HardCleanup 单次执行时长 | > 30 分钟 | Warning |
| DeletedEntitySnapshots 表日增长 | > 10GB | Warning |

---

## 五、数据一致性验证脚本

### 5.1 软删除字段完整性检查

**目的**：验证已软删除的记录，四个软删除字段的值都处于合法状态。

```sql
-- =============================================
-- 脚本 1：软删除字段完整性检查
-- 适用场景：上线前每日巡检 / 回滚后验证
-- =============================================

SELECT '== SharePosts 软删除字段异常 ==' AS check_item;
SELECT Id, IsDeleted, DeletedAt, DeletedBy, DeletionReason
FROM SharePosts
WHERE
  -- 异常1：IsDeleted=true 但 DeletedAt 为空（缺少删除时间）
  (IsDeleted = TRUE AND DeletedAt IS NULL)
  -- 异常2：IsDeleted=false 但 DeletedAt 不为空（幽灵标记）
  OR (IsDeleted = FALSE AND DeletedAt IS NOT NULL)
  -- 异常3：IsDeleted=true 但 DeletedBy 为 0（空值应为 NULL 而非 0）
  OR (IsDeleted = TRUE AND DeletedBy = 0);

SELECT '== Reservations 软删除字段异常 ==' AS check_item;
SELECT Id, IsDeleted, DeletedAt, DeletedBy
FROM Reservations
WHERE
  (IsDeleted = TRUE AND DeletedAt IS NULL)
  OR (IsDeleted = FALSE AND DeletedAt IS NOT NULL)
  OR (IsDeleted = TRUE AND DeletedBy = 0);

SELECT '== PickupCodes 软删除字段异常 ==' AS check_item;
SELECT Id, IsDeleted, DeletedAt, DeletedBy
FROM PickupCodes
WHERE
  (IsDeleted = TRUE AND DeletedAt IS NULL)
  OR (IsDeleted = FALSE AND DeletedAt IS NOT NULL)
  OR (IsDeleted = TRUE AND DeletedBy = 0);

SELECT '== KarmaPoints 软删除字段异常 ==' AS check_item;
SELECT Id, IsDeleted, DeletedAt, DeletedBy
FROM KarmaPoints
WHERE
  (IsDeleted = TRUE AND DeletedAt IS NULL)
  OR (IsDeleted = FALSE AND DeletedAt IS NOT NULL)
  OR (IsDeleted = TRUE AND DeletedBy = 0);

-- 如果以上查询均返回 0 行，说明字段完整性检查通过
```

### 5.2 级联软删除一致性检查

**目的**：验证删除 SharePost 后，关联的 Reservation 和 PickupCode 是否也被级联软删除。

```sql
-- =============================================
-- 脚本 2：级联软删除一致性检查
-- 适用场景：删除操作后的即时验证
-- =============================================

SELECT '== 异常：SharePost 已删除，但仍有未软删除的 Reservation ==' AS check_item;
SELECT
  sp.Id AS PostId,
  sp.Title AS PostTitle,
  sp.DeletedAt AS PostDeletedAt,
  r.Id AS ReservationId,
  r.Status AS ReservationStatus
FROM SharePosts sp
JOIN Reservations r ON r.PostId = sp.Id
WHERE
  sp.IsDeleted = TRUE
  AND r.IsDeleted = FALSE;

SELECT '== 异常：Reservation 已删除，但仍有未软删除的 PickupCode ==' AS check_item;
SELECT
  r.Id AS ReservationId,
  r.DeletedAt AS ReservationDeletedAt,
  pc.Id AS PickupCodeId,
  pc.Code AS PickupCodeCode
FROM Reservations r
JOIN PickupCodes pc ON pc.ReservationId = r.Id
WHERE
  r.IsDeleted = TRUE
  AND pc.IsDeleted = FALSE;

-- 如果以上查询均返回 0 行，说明级联一致性检查通过
```

### 5.3 审计快照完整性检查

**目的**：验证每次软删除都生成了对应的快照记录，且快照内容可反序列化。

```sql
-- =============================================
-- 脚本 3：审计快照完整性检查
-- =============================================

-- 3.1 检查业务表中已删除记录是否有对应快照
SELECT '== 异常：SharePost 已删除但缺少快照 ==' AS check_item;
SELECT sp.Id, sp.Title, sp.DeletedAt
FROM SharePosts sp
LEFT JOIN DeletedEntitySnapshots s
  ON s.EntityType = 'SharePost' AND s.EntityId = sp.Id
WHERE
  sp.IsDeleted = TRUE
  AND s.Id IS NULL;

SELECT '== 异常：Reservation 已删除但缺少快照 ==' AS check_item;
SELECT r.Id, r.PostId, r.DeletedAt
FROM Reservations r
LEFT JOIN DeletedEntitySnapshots s
  ON s.EntityType = 'Reservation' AND s.EntityId = r.Id
WHERE
  r.IsDeleted = TRUE
  AND s.Id IS NULL;

-- PickupCodes / KarmaPoints 同上，略。

-- 3.2 检查快照数量是否匹配（不应出现快照比业务记录多的情况，除非已恢复或已清理）
-- 注意：恢复成功后快照会被删除，所以快照数 ≤ 已删除业务记录数 为正常

-- 3.3 检查 SnapshotData 是否为合法 JSON（MySQL 8.0+ 支持 JSON_VALID）
SELECT '== 异常：SnapshotData 不是合法 JSON ==' AS check_item;
SELECT Id, EntityType, EntityId, LEFT(SnapshotData, 100) AS SnapshotPreview
FROM DeletedEntitySnapshots
WHERE JSON_VALID(SnapshotData) = 0;

-- 3.4 检查 OriginalOwnerId 是否为合法值（SharePost 的 PosterId 是否匹配等）
SELECT '== 异常：SharePost 快照 OriginalOwnerId 与实际 PosterId 不匹配 ==' AS check_item;
SELECT
  s.Id AS SnapshotId,
  s.EntityId,
  s.OriginalOwnerId AS SnapshotOwner,
  sp.PosterId AS ActualOwner
FROM DeletedEntitySnapshots s
JOIN SharePosts sp ON sp.Id = s.EntityId
WHERE
  s.EntityType = 'SharePost'
  AND s.OriginalOwnerId <> sp.PosterId;
```

### 5.4 回收站权限边界检查

**目的**：验证回收站查询不会越权返回其他用户的数据。

```sql
-- =============================================
-- 脚本 4：回收站权限边界检查
-- 以 userId=2 (userA) 为例，模拟 API 查询并验证
-- =============================================

SET @currentUserId = 2;

SELECT '== 以下为 userId=2 应该能看到的回收站数据 ==' AS expected_visible;
SELECT
  s.Id, s.EntityType, s.EntityId, s.EntityDisplayName,
  s.DeletedBy, s.OriginalOwnerId, s.DeletedAt
FROM DeletedEntitySnapshots s
WHERE
  s.DeletedBy = @currentUserId
  OR s.OriginalOwnerId = @currentUserId
ORDER BY s.DeletedAt DESC;

-- 用这个结果和 API 返回值对比，应完全一致

SELECT '== 以下为 userId=2 不应该看到的数据（越权检查）==' AS should_not_see;
SELECT
  s.Id, s.EntityType, s.EntityId, s.EntityDisplayName,
  s.DeletedBy, s.OriginalOwnerId
FROM DeletedEntitySnapshots s
WHERE
  s.DeletedBy <> @currentUserId
  AND (s.OriginalOwnerId IS NULL OR s.OriginalOwnerId <> @currentUserId);

-- 如果 API 返回中出现了「should_not_see」中的记录，说明存在越权漏洞
```

### 5.5 物理清理前后数据对账

**目的**：在 HardCleanupService 执行前后，验证数据清理量的一致性。

```sql
-- =============================================
-- 脚本 5：物理清理前后对账
-- 使用场景：每次月度清理任务执行后，人工或自动对账
-- =============================================

-- Step A：清理前执行（记录基准）
CREATE TEMPORARY TABLE IF NOT EXISTS cleanup_before_counts (
  table_name VARCHAR(50) PRIMARY KEY,
  soft_deleted_count INT,
  total_count INT
);

INSERT INTO cleanup_before_counts VALUES
('SharePosts',
  (SELECT COUNT(*) FROM SharePosts WHERE IsDeleted = TRUE),
  (SELECT COUNT(*) FROM SharePosts)),
('Reservations',
  (SELECT COUNT(*) FROM Reservations WHERE IsDeleted = TRUE),
  (SELECT COUNT(*) FROM Reservations)),
('PickupCodes',
  (SELECT COUNT(*) FROM PickupCodes WHERE IsDeleted = TRUE),
  (SELECT COUNT(*) FROM PickupCodes)),
('KarmaPoints',
  (SELECT COUNT(*) FROM KarmaPoints WHERE IsDeleted = TRUE),
  (SELECT COUNT(*) FROM KarmaPoints)),
('DeletedEntitySnapshots',
  NULL,
  (SELECT COUNT(*) FROM DeletedEntitySnapshots));

-- Step B：执行清理任务（调用 HardCleanupService）

-- Step C：清理后执行，对账
SELECT
  before.table_name,
  before.soft_deleted_count                    AS before_soft_deleted,
  (CASE
    WHEN before.table_name = 'DeletedEntitySnapshots' THEN NULL
    ELSE (SELECT COUNT(*)
          FROM information_schema.COLUMNS
          WHERE TABLE_SCHEMA = DATABASE()
            AND TABLE_NAME = before.table_name
            AND COLUMN_NAME = 'IsDeleted')
  END)                                          AS has_soft_delete_col,
  after.after_total                             AS after_total_count,
  (before.total_count - after.after_total)      AS rows_removed,
  task_log.expired_count_from_log               AS log_reported_count,
  CASE
    WHEN (before.total_count - after.after_total) = task_log.expired_count_from_log
         OR task_log.expired_count_from_log IS NULL
      THEN '一致'
    ELSE '不一致'
  END                                           AS reconciliation_status
FROM cleanup_before_counts before
JOIN (
  SELECT 'SharePosts' AS tbl, COUNT(*) AS after_total FROM SharePosts UNION ALL
  SELECT 'Reservations', COUNT(*) FROM Reservations UNION ALL
  SELECT 'PickupCodes',  COUNT(*) FROM PickupCodes UNION ALL
  SELECT 'KarmaPoints',  COUNT(*) FROM KarmaPoints UNION ALL
  SELECT 'DeletedEntitySnapshots', COUNT(*) FROM DeletedEntitySnapshots
) after ON after.tbl = before.table_name
LEFT JOIN (
  SELECT
    CASE TaskNameField.tbl
      WHEN 'SharePosts'  THEN ExpiredSharePostsCount
      WHEN 'PickupCodes' THEN ExpiredPickupCodesCount
      WHEN 'Reservations' THEN NotificationsSentCount
      -- KarmaPoints 单独关联需扩展字段
      ELSE NULL
    END AS expired_count_from_log,
    TaskNameField.tbl
  FROM ScheduledTaskLogs
  CROSS JOIN (
    SELECT 'SharePosts' AS tbl UNION ALL
    SELECT 'PickupCodes' UNION ALL
    SELECT 'Reservations' UNION ALL
    SELECT 'KarmaPoints' UNION ALL
    SELECT 'DeletedEntitySnapshots'
  ) TaskNameField
  WHERE TaskName = 'MonthlyHardCleanup'
  ORDER BY StartedAt DESC
  LIMIT 1
) task_log ON task_log.tbl = before.table_name
ORDER BY before.table_name;
```

### 5.6 一键检查汇总脚本

```sql
-- =============================================
-- 脚本 6：一键检查（输出 JSON 汇总报告）
-- 适用于：上线前一键生成检查报告
-- =============================================

SET @report_generated_at = NOW(3);

SELECT JSON_OBJECT(
  'generatedAt', @report_generated_at,
  'checks', JSON_OBJECT(
    'soft_delete_field_integrity', JSON_OBJECT(
      'SharePosts_abnormal', (SELECT COUNT(*) FROM SharePosts WHERE
        (IsDeleted = TRUE AND DeletedAt IS NULL) OR
        (IsDeleted = FALSE AND DeletedAt IS NOT NULL) OR
        (IsDeleted = TRUE AND DeletedBy = 0)),
      'Reservations_abnormal', (SELECT COUNT(*) FROM Reservations WHERE
        (IsDeleted = TRUE AND DeletedAt IS NULL) OR
        (IsDeleted = FALSE AND DeletedAt IS NOT NULL) OR
        (IsDeleted = TRUE AND DeletedBy = 0)),
      'PickupCodes_abnormal', (SELECT COUNT(*) FROM PickupCodes WHERE
        (IsDeleted = TRUE AND DeletedAt IS NULL) OR
        (IsDeleted = FALSE AND DeletedAt IS NOT NULL) OR
        (IsDeleted = TRUE AND DeletedBy = 0)),
      'KarmaPoints_abnormal', (SELECT COUNT(*) FROM KarmaPoints WHERE
        (IsDeleted = TRUE AND DeletedAt IS NULL) OR
        (IsDeleted = FALSE AND DeletedAt IS NOT NULL) OR
        (IsDeleted = TRUE AND DeletedBy = 0))
    ),
    'cascade_consistency', JSON_OBJECT(
      'orphan_reservations_undeleted', (SELECT COUNT(*)
        FROM SharePosts sp JOIN Reservations r ON r.PostId = sp.Id
        WHERE sp.IsDeleted = TRUE AND r.IsDeleted = FALSE),
      'orphan_pickup_codes_undeleted', (SELECT COUNT(*)
        FROM Reservations r JOIN PickupCodes pc ON pc.ReservationId = r.Id
        WHERE r.IsDeleted = TRUE AND pc.IsDeleted = FALSE)
    ),
    'snapshot_integrity', JSON_OBJECT(
      'shareposts_missing_snapshot', (SELECT COUNT(*)
        FROM SharePosts sp LEFT JOIN DeletedEntitySnapshots s
          ON s.EntityType = 'SharePost' AND s.EntityId = sp.Id
        WHERE sp.IsDeleted = TRUE AND s.Id IS NULL),
      'reservations_missing_snapshot', (SELECT COUNT(*)
        FROM Reservations r LEFT JOIN DeletedEntitySnapshots s
          ON s.EntityType = 'Reservation' AND s.EntityId = r.Id
        WHERE r.IsDeleted = TRUE AND s.Id IS NULL),
      'invalid_json_snapshots', (SELECT COUNT(*)
        FROM DeletedEntitySnapshots WHERE JSON_VALID(SnapshotData) = 0)
    ),
    'totals', JSON_OBJECT(
      'TotalSharePosts',         (SELECT COUNT(*) FROM SharePosts),
      'TotalSharePostsDeleted',  (SELECT COUNT(*) FROM SharePosts WHERE IsDeleted = TRUE),
      'TotalReservations',       (SELECT COUNT(*) FROM Reservations),
      'TotalReservationsDeleted',(SELECT COUNT(*) FROM Reservations WHERE IsDeleted = TRUE),
      'TotalPickupCodes',        (SELECT COUNT(*) FROM PickupCodes),
      'TotalPickupCodesDeleted', (SELECT COUNT(*) FROM PickupCodes WHERE IsDeleted = TRUE),
      'TotalKarmaPoints',        (SELECT COUNT(*) FROM KarmaPoints),
      'TotalKarmaPointsDeleted', (SELECT COUNT(*) FROM KarmaPoints WHERE IsDeleted = TRUE),
      'TotalSnapshots',          (SELECT COUNT(*) FROM DeletedEntitySnapshots)
    )
  ),
  'overall_verdict', (
    SELECT CASE WHEN
      (SELECT COUNT(*) FROM SharePosts WHERE
        (IsDeleted = TRUE AND DeletedAt IS NULL) OR
        (IsDeleted = FALSE AND DeletedAt IS NOT NULL) OR
        (IsDeleted = TRUE AND DeletedBy = 0)) = 0
      AND (SELECT COUNT(*) FROM Reservations WHERE
        (IsDeleted = TRUE AND DeletedAt IS NULL) OR
        (IsDeleted = FALSE AND DeletedAt IS NOT NULL) OR
        (IsDeleted = TRUE AND DeletedBy = 0)) = 0
      AND (SELECT COUNT(*) FROM PickupCodes WHERE
        (IsDeleted = TRUE AND DeletedAt IS NULL) OR
        (IsDeleted = FALSE AND DeletedAt IS NOT NULL) OR
        (IsDeleted = TRUE AND DeletedBy = 0)) = 0
      AND (SELECT COUNT(*) FROM KarmaPoints WHERE
        (IsDeleted = TRUE AND DeletedAt IS NULL) OR
        (IsDeleted = FALSE AND DeletedAt IS NOT NULL) OR
        (IsDeleted = TRUE AND DeletedBy = 0)) = 0
      AND (SELECT COUNT(*)
        FROM SharePosts sp JOIN Reservations r ON r.PostId = sp.Id
        WHERE sp.IsDeleted = TRUE AND r.IsDeleted = FALSE) = 0
      AND (SELECT COUNT(*)
        FROM Reservations r JOIN PickupCodes pc ON pc.ReservationId = r.Id
        WHERE r.IsDeleted = TRUE AND pc.IsDeleted = FALSE) = 0
      AND (SELECT COUNT(*)
        FROM DeletedEntitySnapshots WHERE JSON_VALID(SnapshotData) = 0) = 0
    THEN 'PASS' ELSE 'FAIL' END
  )
) AS soft_delete_consistency_report;
```

### 5.7 测试通过标准

| 检查类型 | 通过标准 |
|----------|----------|
| 功能测试（P0） | 100% 通过，0 个失败 |
| 功能测试（P1） | ≥ 95% 通过，失败项需评审确认不影响上线 |
| 功能测试（P2） | ≥ 80% 通过，失败项记录为技术债务 |
| 接口兼容性 | 所有 P0 级接口 100% 契约一致 |
| 性能对比 | 核心查询性能增幅 ≤ 20%，无慢查询 |
| 索引验证 | 所有核心查询 `EXPLAIN` 输出 type ≥ `ref`，无非预期的 `ALL`（全表扫描） |
| 数据一致性脚本 | 一键检查输出 `overall_verdict = 'PASS'` |

---

## 附录：测试报告模板

```
# 软删除功能回归测试报告

## 基本信息
- 测试开始日期：YYYY-MM-DD
- 测试结束日期：YYYY-MM-DD
- 执行总人日：X 人日
- 测试执行人：XXX / XXX
- 环境信息：
  - 数据库版本：MySQL 8.0.XX
  - 应用版本：Commit Hash = xxxxxxx
  - 数据规模：SharePosts = XX 万行，Reservations = XX 万行

## 执行统计
| 类别 | 用例总数 | 通过 | 失败 | 阻塞 | 未执行 | 通过率 |
|------|---------|------|------|------|--------|--------|
| P0 功能 | XX | XX | 0 | 0 | 0 | 100% |
| P1 功能 | XX | XX | X | 0 | 0 | XX% |
| P2 边缘 | XX | XX | X | 0 | X | XX% |
| 兼容性 | XX | XX | 0 | 0 | 0 | 100% |
| **合计** | **XXX** | **XXX** | **X** | **0** | **X** | **XX%** |

## 性能测试结果
| 查询场景 | 基线 P95 | 软删除后 P95 | 增幅 | 是否通过 |
|----------|---------|-------------|------|---------|
| 列表查询 | XX ms | XX ms | +X% | 是/否 |
| 详情查询 | XX ms | XX ms | +X% | 是/否 |
| 删除操作 | XX ms | XX ms | +X% | 是/否 |
| ... | ... | ... | ... | ... |

## 数据一致性检查结果
- 一键检查 overall_verdict：PASS / FAIL
- 详细异常项：（如有，列出每条异常及处理方式）

## 遗留问题清单
| 编号 | 问题描述 | 严重程度 | 处理建议 | 责任人 |
|------|---------|---------|---------|--------|
| 1 | XXX | Major / Minor / Trivial | 修复 / 记录为技术债务 / 接受风险 | XXX |

## 上线风险评估
- 风险等级：低 / 中 / 高
- 风险说明：
- 缓解措施：

## 最终结论
☐ 建议上线（所有 P0/P1 通过，遗留问题可控）
☐ 有条件上线（需先解决以下 X 项遗留问题）
☐ 暂不建议上线（需解决 P0 失败项后复测）

签字：XXX（测试负责人）、XXX（研发负责人）、XXX（产品负责人）
日期：YYYY-MM-DD
```
