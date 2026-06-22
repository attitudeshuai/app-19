# 软删除迁移方案总览

> **文档版本**：v1.0  
> **更新日期**：2026-06-22  
> **适用对象**：研发团队、运维团队、测试团队

---

## 一、背景与目标

### 1.1 为什么要从物理删除迁移到逻辑删除

在本次架构升级之前，系统对业务数据采用**物理删除**（即执行 SQL `DELETE` 语句直接从数据库中移除记录）。物理删除存在以下显著风险和痛点：

| 痛点 | 详细说明 |
|------|----------|
| **数据不可恢复** | 误操作（手滑删除、bug 触发删除）导致数据永久丢失，只能依赖数据库备份恢复，恢复成本极高 |
| **审计追踪困难** | 删除后无法追溯「谁删的、什么时间删的、为什么删」，不满足合规要求 |
| **用户体验差** | 用户删除后反悔无法撤回，客服无有效手段恢复数据 |
| **级联删除风险** | 级联物理删除可能导致关联数据意外丢失，排查困难 |
| **数据价值流失** | 删除的历史数据对运营分析、报表统计仍有价值，物理删除直接销毁这些价值 |

### 1.2 迁移目标

本次迁移的核心目标：

1. **数据可恢复**：所有删除操作支持在保留期内完整恢复
2. **操作可审计**：记录每次删除的操作者、时间、原因、完整快照
3. **零侵入前端**：现有前端代码无需任何修改即可自动适配软删除
4. **存储可控**：软删除数据不无限堆积，超过保留期后自动物理清理
5. **性能保障**：索引优化确保查询性能不受软删除影响

---

## 二、迁移范围

### 2.1 涉及的四张表

本次软删除方案覆盖以下四张核心业务表：

| 表名 | 实体类 | 说明 | 级联关系 |
|------|--------|------|----------|
| `SharePosts` | `SharePost` | 分享帖子表 | 级联删除关联的 Reservation 和 PickupCode |
| `Reservations` | `Reservation` | 预订记录表 | 级联删除关联的 PickupCode |
| `PickupCodes` | `PickupCode` | 取餐码表 | 无下游级联 |
| `KarmaPoints` | `KarmaPoint` | 积分流水表 | 无下游级联 |

### 2.2 新增的表

| 表名 | 实体类 | 说明 |
|------|--------|------|
| `DeletedEntitySnapshots` | `DeletedEntitySnapshot` | 软删除审计快照表，记录每次删除的完整数据快照 |

### 2.3 未纳入范围的表

以下表**未**启用软删除（保持物理删除或无需删除）：
- `Users` — 用户表，账号体系敏感，暂不开放
- `Notifications` — 站内通知，只读场景，支持已读/未读即可
- `ScheduledTaskLogs` — 定时任务日志，系统内部使用

---

## 三、架构设计

### 3.1 ISoftDeletable 接口设计

所有需要支持软删除的实体都必须实现 `ISoftDeletable` 接口（位于 `src/LeftoverShare.API/Entities/ISoftDeletable.cs:3`）：

```csharp
public interface ISoftDeletable
{
    bool IsDeleted { get; set; }        // 是否已删除标记
    DateTime? DeletedAt { get; set; }   // 删除时间（UTC）
    int? DeletedBy { get; set; }        // 删除操作者用户ID
    string? DeletionReason { get; set; }// 删除原因说明（可选）
}
```

**字段说明**：

| 字段 | 类型 | 约束 | 说明 |
|------|------|------|------|
| `IsDeleted` | `bool` | `DEFAULT false` | 核心标记位，`true` 表示已软删除 |
| `DeletedAt` | `DateTime?` | 可空 | 软删除执行时间，用于保留期计算 |
| `DeletedBy` | `int?` | 可空，外键 → Users | 执行删除的用户ID，用于审计和权限校验 |
| `DeletionReason` | `string?` | 最大 500 字符 | 级联删除时会自动填充原因 |

**实体实现示例**（`SharePost` 类，位于 `src/LeftoverShare.API/Entities/SharePost.cs:10`）：

```csharp
public class SharePost : ISoftDeletable
{
    // ... 其他业务字段 ...

    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public int? DeletedBy { get; set; }
    public string? DeletionReason { get; set; }
}
```

---

### 3.2 全局查询过滤器（Global Query Filter）

**原理**：在 EF Core 的 `OnModelCreating` 中为每个软删除实体配置查询过滤器，确保默认所有查询都自动过滤已软删除的记录，业务层代码无需显式添加 `Where(x => !x.IsDeleted)` 条件。

**配置位置**：`src/LeftoverShare.API/Data/AppDbContext.cs` 中各实体配置：

```csharp
// SharePosts (AppDbContext.cs:164)
entity.HasQueryFilter(sp => !sp.IsDeleted);

// Reservations (AppDbContext.cs:215)
entity.HasQueryFilter(r => !r.IsDeleted);

// PickupCodes (AppDbContext.cs:260)
entity.HasQueryFilter(pc => !pc.IsDeleted);

// KarmaPoints (AppDbContext.cs:294)
entity.HasQueryFilter(kp => !kp.IsDeleted);
```

**工作流程**：

```
前端调用 GET /api/shareposts
        ↓
Repository.GetPagedAsync()
        ↓
EF Core 自动附加 WHERE IsDeleted = 0
        ↓
返回未删除的记录（对业务代码透明）
```

**绕过过滤器**：当需要查询已删除记录（如回收站、恢复操作）时，使用 `IgnoreQueryFilters()` 方法：

```csharp
// Repository.cs:27-30
public virtual async Task<T?> GetByIdIgnoreFilterAsync(int id)
{
    return await _dbSet.IgnoreQueryFilters()
        .FirstOrDefaultAsync(e => EF.Property<int>(e, "Id") == id);
}
```

---

### 3.3 SaveChanges 拦截器实现原理

**核心机制**：重写 `DbContext` 的 `SaveChanges()` 和 `SaveChangesAsync()` 方法，在 EF Core 实际执行 SQL 之前拦截所有 `EntityState.Deleted` 状态的实体，将其转换为**软删除**（`EntityState.Modified` + 设置 `IsDeleted = true`）。

**代码位置**：`src/LeftoverShare.API/Data/AppDbContext.cs:421-458`

```
调用 SaveChanges / SaveChangesAsync
        ↓
ProcessSoftDeletes() 被触发
        ↓
遍历 ChangeTracker 中所有 EntityState.Deleted 且实现 ISoftDeletable 的实体
        ↓
对每个实体执行：
  ├─ 将 State 改为 EntityState.Modified（不执行 DELETE）
  ├─ 设置 IsDeleted = true
  ├─ 设置 DeletedAt = DateTime.UtcNow
  ├─ 保留 DeletedBy（如果已设置）
  ├─ CreateAuditSnapshot() → 创建审计快照
  └─ HandleCascadeSoftDelete() → 处理级联软删除
        ↓
base.SaveChanges() → 实际执行 UPDATE SQL
```

**关键代码**（`AppDbContext.cs:433-458`）：

```csharp
private void ProcessSoftDeletes()
{
    var softDeleteEntries = ChangeTracker.Entries<ISoftDeletable>()
        .Where(e => e.State == EntityState.Deleted)
        .ToList();

    foreach (var entry in softDeleteEntries)
    {
        entry.State = EntityState.Modified;       // 改为 UPDATE
        entry.Entity.IsDeleted = true;             // 标记已删除
        entry.Entity.DeletedAt = DateTime.UtcNow;  // 记录时间
        CreateAuditSnapshot(entry);                // 生成快照
        HandleCascadeSoftDelete(entry);            // 处理级联
    }
}
```

> **设计要点**：通过拦截器模式，业务代码只需调用 `_dbSet.Remove(entity)`（和物理删除时代码完全一致），即可自动转换为软删除，实现业务代码零修改。

---

### 3.4 审计快照机制

**目的**：每次软删除时，将实体的完整原始数据序列化为 JSON 保存到 `DeletedEntitySnapshots` 表中，支持后续恢复和审计查看。

**快照表结构**（`src/LeftoverShare.API/Entities/DeletedEntitySnapshot.cs:6`）：

| 字段 | 类型 | 说明 |
|------|------|------|
| `Id` | `int` PK | 快照主键 |
| `EntityType` | `string(100)` | 实体类型名：`SharePost` / `Reservation` / `PickupCode` / `KarmaPoint` |
| `EntityId` | `int` | 原实体的主键 ID |
| `EntityDisplayName` | `string(500)` | 人类可读的显示名，如「分享帖 #123 - 免费便当」 |
| `SnapshotData` | `text` | JSON 格式的完整原始数据（OriginalValues） |
| `DeletedBy` | `int` FK | 删除操作者用户ID |
| `DeletedAt` | `DateTime` | 删除时间 |
| `DeletionReason` | `string(500)?` | 删除原因 |
| `OriginalOwnerId` | `int?` | 原数据所有者ID（用于回收站权限过滤） |

**快照生成流程**（`AppDbContext.cs:460-494`）：

```
实体被标记为 Deleted
        ↓
遍历 entry.OriginalValues.Properties
        ↓
将所有原始值序列化为 Dictionary
        ↓
System.Text.Json 序列化为紧凑 JSON（WriteIndented=false）
        ↓
构造 DeletedEntitySnapshot 并添加到 DbContext
        ↓
SaveChanges 时一并 INSERT 到快照表
```

**显示名生成规则**（`AppDbContext.cs:496-507`）：

| 实体类型 | 显示名格式 | 示例 |
|----------|-----------|------|
| SharePost | `分享帖 #{Id} - {Title}` | `分享帖 #123 - 免费午餐便当` |
| Reservation | `预约 #{Id} - 帖子#{PostId} by 用户#{ClaimerId}` | `预约 #45 - 帖子#123 by 用户#8` |
| PickupCode | `取餐码 #{Id} - {Code}` | `取餐码 #78 - ABC123XYZ` |
| KarmaPoint | `积分流水 #{Id} - 用户#{UserId} {Points}分` | `积分流水 #9 - 用户#5 10分` |

**数据库索引**（`AppDbContext.cs:387-394`）：

- `IX_DeletedEntitySnapshots_EntityType_EntityId`：按实体类型+ID查询
- `IX_DeletedEntitySnapshots_DeletedBy_DeletedAt`：按操作者+时间查询
- `IX_DeletedEntitySnapshots_DeletedAt`：用于过期快照清理（按时间范围）

---

### 3.5 级联软删除处理

**问题**：EF Core 的配置中，`SharePost → Reservation` 和 `Reservation → PickupCode` 配置了 `DeleteBehavior.Cascade` 的级联物理删除（`AppDbContext.cs:161` 和 `:212`）。当改为软删除后，EF Core 的级联删除不再适用，需要自行处理级联软删除。

**级联关系图**：

```
SharePost (分享帖)
    │
    ├── OnDelete(DeleteBehavior.Cascade) → 改为代码级级联软删除
    │
    └──→ Reservation (预订记录)
            │
            ├── OnDelete(DeleteBehavior.Cascade) → 改为代码级级联软删除
            │
            └──→ PickupCode (取餐码)
```

**实现代码**（`AppDbContext.cs:521-578`）：

```csharp
private void HandleCascadeSoftDelete(EntityEntry<ISoftDeletable> entry)
{
    // 场景1：删除 SharePost → 级联删除关联的 Reservation 和 PickupCode
    if (entry.Entity is SharePost post)
    {
        // 1. 查找 DbContext Local 中的关联 Reservation（尚未 Load 的需要先 Include）
        var relatedReservations = Reservations.Local
            .Where(r => r.PostId == post.Id && !r.IsDeleted).ToList();

        foreach (var res in relatedReservations)
        {
            res.IsDeleted = true;
            res.DeletedAt = DateTime.UtcNow;
            res.DeletedBy = post.DeletedBy;
            res.DeletionReason = $"级联删除：关联分享帖#{post.Id}已删除";
            resEntry.State = EntityState.Modified;
        }

        // 2. 进一步级联：这些 Reservation 关联的 PickupCode
        var relatedPickupCodes = PickupCodes.Local
            .Where(pc => relatedReservations.Select(r => r.Id).Contains(pc.ReservationId))
            .ToList();
        // ... 同样设置软删除标记
    }

    // 场景2：删除 Reservation → 级联删除关联的 PickupCode
    if (entry.Entity is Reservation reservationEntry)
    {
        var relatedPickupCode = PickupCodes.Local
            .FirstOrDefault(pc => pc.ReservationId == reservationEntry.Id);
        // ... 设置软删除标记，原因填「级联删除：关联预约#{Id}已删除」
    }
}
```

> **重要说明**：级联软删除基于 DbContext 的 `Local` 缓存工作。如果业务代码在删除 SharePost 前没有 `Include(sp => sp.Reservations).ThenInclude(r => r.PickupCodeNavigation)` 加载关联数据，则级联软删除不会生效。Service 层负责在删除前确保关联数据已加载。

---

### 3.6 回收站查询流程

**API 端点**：`GET /api/recyclebin`（`RecycleBinController.cs:40-60`）

**权限规则**：用户只能看到**自己删除的**或**自己是原所有者的**记录：

```sql
-- RecycleBinService.cs:44 对应的 SQL 条件
WHERE DeletedBy = @userId OR OriginalOwnerId = @userId
```

**查询流程**：

```
用户请求 GET /api/recyclebin?entityType=SharePost&pageNumber=1&pageSize=10
        ↓
权限校验（已登录）
        ↓
从 DeletedEntitySnapshots 查询：
  ├─ 过滤：DeletedBy == userId 或 OriginalOwnerId == userId
  ├─ 可选过滤：EntityType == 指定值
  ├─ 可选搜索：EntityDisplayName 或 DeletionReason 包含关键词
  ├─ 排序：默认按 DeletedAt DESC
  ├─ 分页：Skip / Take
  └─ 统计：TotalCount
        ↓
为每条记录生成 SnapshotDataPreview（截取 Title/Reason/Status 等关键字段）
        ↓
返回分页结果 PagedResponse<RecycleBinItemResponse>
```

**响应字段**（`RecycleBinItemResponse`）：

| 字段 | 说明 |
|------|------|
| `Id` | 快照ID（恢复时用这个ID，不是实体ID） |
| `EntityType` | 实体类型 |
| `EntityId` | 原实体ID |
| `EntityDisplayName` | 显示名 |
| `SnapshotDataPreview` | 快照预览（前几个关键属性） |
| `DeletedBy` | 删除者用户ID |
| `DeletedAt` | 删除时间 |
| `DeletionReason` | 删除原因（级联会有文字） |
| `OriginalOwnerId` | 原所有者ID |

---

### 3.7 实体恢复流程

**API 端点**：`POST /api/recyclebin/restore`（`RecycleBinController.cs:68-79`）

**请求体**（`RestoreRequest`）：

```json
{
  "id": 123,          // 快照ID（从回收站列表获取）
  "entityType": "SharePost"  // 实体类型
}
```

**恢复流程**（`RecycleBinService.cs:105-141`）：

```
用户请求恢复
        ↓
参数校验：entityType 必须是四个有效值之一
        ↓
根据 快照ID + 实体类型 查询 DeletedEntitySnapshots
        ↓
权限校验：当前用户 == DeletedBy 或 当前用户 == OriginalOwnerId
        ↓  不通过
        └─→ 返回 403 无权限
        ↓  通过
根据 EntityType 分发给对应恢复方法
  ├─ RestoreSharePostAsync(entityId)
  ├─ RestoreReservationAsync(entityId)
  ├─ RestorePickupCodeAsync(entityId)
  └─ RestoreKarmaPointAsync(entityId)
        ↓
恢复方法内部：
  1. GetByIdIgnoreFilterAsync(entityId) → 绕过全局过滤器
  2. 校验实体存在且 IsDeleted == true
  3. 重置四个软删除字段：
        IsDeleted = false
        DeletedAt = null
        DeletedBy = null
        DeletionReason = null
  4. Update 并 SaveChanges
        ↓
恢复成功 → DELETE 对应的 DeletedEntitySnapshot 记录
        ↓
返回恢复后的实体详情
```

> **设计决策**：恢复成功后删除快照记录，避免快照表累积。如需保留恢复审计，可后续扩展为标记快照已恢复而非物理删除。

**级联恢复说明**：当前版本**不自动级联恢复**。即：
- 删除 SharePost 时级联删除的 Reservation / PickupCode，恢复 SharePost **不会**自动恢复关联的 Reservation / PickupCode
- 用户需要手动逐条恢复（或后续扩展批量恢复功能）

---

### 3.8 六个月自动物理清理策略

**目标**：软删除数据不无限堆积，超过保留期（默认6个月）后自动物理清理，释放存储空间。

#### 3.8.1 配置参数

配置位置：`appsettings.json` → `DailyCleanupSettings`（`Helpers/DailyCleanupSettings.cs:6`）

```json
{
  "DailyCleanupSettings": {
    "HardCleanupEnabled": true,                // 开关：是否启用物理清理
    "HardCleanupExecuteTime": "03:00",         // 每月执行时间（本地时间，HH:mm）
    "SoftDeleteRetentionMonths": 6             // 软删除保留月数
  }
}
```

| 参数 | 默认值 | 说明 |
|------|--------|------|
| `HardCleanupEnabled` | `true` | 设为 `false` 可禁用物理清理（保留所有软删除数据） |
| `HardCleanupExecuteTime` | `"03:00"` | 每月1号凌晨3点执行（低峰时段） |
| `SoftDeleteRetentionMonths` | `6` | 软删除数据保留6个月，超过则物理删除 |

#### 3.8.2 调度器实现

**组件**：`HardCleanupScheduler`（`BackgroundServices/HardCleanupScheduler.cs:20`），继承自 ASP.NET Core `BackgroundService`，应用启动后自动运行。

**调度策略**：

```
应用启动
    ↓
检查 HardCleanupEnabled == false → 直接退出不启动
    ↓  true
等待 StartupDelaySeconds（默认60秒，给系统初始化留时间）
    ↓
进入主循环：
    1. 读取本月是否已成功执行（查 ScheduledTaskLogs 表）
    2. 若本月已执行 → 睡到下月1号
    3. 若本月未执行且当前时间 ≥ 本月1号 03:00 → 立即触发（支持补跑）
    4. 否则 → 睡到计划时间
    ↓ 重复
```

**防重复执行**：每次执行前查询 `ScheduledTaskLogs` 表，只要本月有 `Success` 或 `PartialSuccess` 状态的记录就跳过。支持应用重启后的「补跑」（例如维护导致1号停机，2号启动时会自动补跑）。

**并发保护**：使用 `SemaphoreSlim(1, 1)` 信号量，确保同一时刻只有一个清理任务在执行。

#### 3.8.3 清理服务实现

**组件**：`HardCleanupService`（`Services/Impl/HardCleanupService.cs:14`）

**清理顺序**（严格按外键依赖顺序，避免外键约束错误）：

```
第1步：清理 SharePosts  →  条件：IsDeleted=true AND DeletedAt < (今天 - 6个月)
第2步：清理 Reservations  →  同上
第3步：清理 PickupCodes  →  同上
第4步：清理 KarmaPoints  →  同上
第5步：清理 DeletedEntitySnapshots  →  条件：DeletedAt < (今天 - 6个月)
```

**物理删除绕过软删除**：使用 `Repository.HardDelete()` / `HardDeleteRange()` 方法（`Repository.cs:132-159`），先将实体标记为 `Modified` 并抑制软删除字段修改，再重新改为 `Deleted`，从而绕过 `ProcessSoftDeletes()` 拦截器，执行真正的 SQL `DELETE`。

**清理结果记录**：每次执行都会在 `ScheduledTaskLogs` 表中记录一条日志：

| 字段 | 说明 |
|------|------|
| `TaskName` | `"MonthlyHardCleanup"` |
| `Status` | `Running` / `Success` / `PartialSuccess` / `Failed` |
| `ExpiredSharePostsCount` | 物理删除的 SharePost 数量 |
| `ExpiredPickupCodesCount` | 物理删除的 PickupCode 数量 |
| `NotificationsSentCount` | 存放 Reservation + KarmaPoint 清理数量 |
| `DurationMs` | 执行耗时（毫秒） |
| `ErrorMessage` | 错误信息（如有） |
| `Details` | JSON 格式的详细统计 |

---

## 四、API 变更说明

### 4.1 DELETE 接口行为变化（现在是软删除，不是真删除）

以下接口的**路由、请求参数、响应格式完全不变**，仅内部行为从物理删除变为软删除：

| 原接口 | 行为变化 | 幂等性说明 |
|--------|----------|-----------|
| `DELETE /api/shareposts/{id}` | 从物理删除 → 软删除 | 404 的场景从「不存在」变为「不存在或已软删除」 |
| `DELETE /api/reservations/{id}` | 从物理删除 → 软删除 | 同上 |
| `DELETE /api/pickupcodes/{id}` | 从物理删除 → 软删除 | 同上 |
| `DELETE /api/karmapoints/{id}` | 从物理删除 → 软删除 | 同上 |

**对前端的影响**：**零影响**。前端调用 DELETE 接口后，数据从列表中消失（被全局查询过滤器过滤），用户体验与之前完全一致。

**对后端内部查询的影响**：任何使用 LINQ 查询的代码，只要不调用 `IgnoreQueryFilters()`，就会自动排除已软删除记录，结果与物理删除时代码完全一致。

### 4.2 新增的回收站 API：/api/recyclebin 系列接口

所有回收站接口均需要登录（`[Authorize]`），返回统一的 `ApiResponse` 包装格式。

#### 接口1：查询回收站列表

```
GET /api/recyclebin?entityType=SharePost&pageNumber=1&pageSize=10
```

| 查询参数 | 类型 | 必填 | 说明 |
|---------|------|------|------|
| `entityType` | string | 否 | 过滤实体类型：`SharePost` / `Reservation` / `PickupCode` / `KarmaPoint`，不传则查询全部 |
| `pageNumber` | int | 否 | 页码，默认 1 |
| `pageSize` | int | 否 | 每页数量，默认 10 |

**成功响应**（200）：

```json
{
  "code": 200,
  "message": "success",
  "data": {
    "items": [
      {
        "id": 1,
        "entityType": "SharePost",
        "entityId": 123,
        "entityDisplayName": "分享帖 #123 - 免费午餐",
        "snapshotDataPreview": "Title: 免费午餐, FoodType: 便当, Quantity: 2",
        "deletedBy": 5,
        "deletedAt": "2026-06-20T10:30:00Z",
        "deletionReason": null,
        "originalOwnerId": 5
      }
    ],
    "totalCount": 1,
    "pageNumber": 1,
    "pageSize": 10,
    "totalPages": 1
  }
}
```

#### 接口2：查看快照详情

```
GET /api/recyclebin/snapshots/{id}
```

| 路由参数 | 类型 | 说明 |
|---------|------|------|
| `id` | int | 快照ID（从列表接口获取的 `items[].id`） |

**成功响应**（200）：

```json
{
  "code": 200,
  "message": "success",
  "data": {
    "id": 1,
    "entityType": "SharePost",
    "entityId": 123,
    "entityDisplayName": "分享帖 #123 - 免费午餐",
    "snapshotData": {
      "Id": 123,
      "PosterId": 5,
      "Title": "免费午餐",
      "Description": "公司多订的便当",
      "FoodType": "便当",
      "IsDeleted": false,
      "DeletedAt": null,
      "...": "所有原始字段"
    },
    "deletedBy": 5,
    "deletedAt": "2026-06-20T10:30:00Z",
    "deletionReason": null,
    "originalOwnerId": 5
  }
}
```

#### 接口3：恢复已删除实体

```
POST /api/recyclebin/restore
Content-Type: application/json

{
  "id": 1,
  "entityType": "SharePost"
}
```

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `id` | int | 是 | 快照ID |
| `entityType` | string | 是 | 实体类型 |

**成功响应**（200）：返回恢复后的实体详情（结构与对应 GET 详情接口一致）。

**失败响应**：
- `400`：entityType 无效
- `403`：无权限恢复（非删除者也非原所有者）
- `404`：快照不存在或实体已被物理删除

#### 接口4：永久删除快照（仅管理员）

```
DELETE /api/recyclebin/snapshots/{id}
```

> **权限**：仅管理员（当前实现为 `userId == 1` 的用户）可调用。

**作用**：从 `DeletedEntitySnapshots` 表物理删除快照记录。**注意**：此接口仅删除快照记录，不会自动清理 `IsDeleted=true` 的业务记录（这些记录仍需等到6个月后由自动清理任务处理，或手动执行 SQL 清理）。

---

## 五、兼容性保证：说明为什么现有前端无需修改即可工作

### 5.1 兼容性设计的核心原则

软删除方案的设计原则是：**对业务代码和前端完全透明**。即，软删除是一个「底层架构增强」，而非「功能破坏性变更」。

### 5.2 兼容性保证的五个层面

| 层面 | 保证手段 | 效果 |
|------|----------|------|
| **API 契约不变** | DELETE 接口的路由、参数、状态码、响应结构与迁移前完全一致 | 前端无需调整任何 API 调用代码 |
| **查询结果不变** | 全局查询过滤器（Global Query Filter）自动附加 `WHERE IsDeleted = 0`，所有 GET 查询返回的数据与物理删除时代码结果一致 | 列表、详情、搜索结果完全一致 |
| **唯一约束不变** | UNIQUE 索引不包含 `IsDeleted`，如果业务需要「软删除后相同唯一键可重新创建」，需手动调整索引（当前项目的唯一约束：用户名、邮箱、取餐码、PostId+ClaimerId，这些场景软删除后不允许重复创建，符合业务逻辑） | 业务规则一致性 |
| **级联关系语义不变** | SharePost 删除时，关联的 Reservation 和 PickupCode 也会被标记删除（与物理删除时「删除帖子则预订消失」的用户感知一致） | 用户体验一致 |
| **ID 引用完整性** | 其他表的外键（如 Notifications.SharePostId、Notifications.ReservationId）配置了 `DeleteBehavior.SetNull`，物理删除时会置空；改为软删除后，外键实体未被删除，SetNull 不会触发，通知仍然能关联到原帖子ID（这是**增强**而非破坏） | 关联数据更完整 |

### 5.3 前端感知到的唯一变化（可选功能）

如果前端希望提供「回收站」「撤销删除」功能，需要对接新的 `/api/recyclebin` 接口。但**不对接也完全不影响现有功能**，用户删除后的数据表现与迁移前完全一致。

### 5.4 后端业务代码的兼容性

| 代码模式 | 迁移前（物理删除） | 迁移后（软删除） | 是否需要修改 |
|----------|-------------------|------------------|-------------|
| `_dbSet.Remove(entity)` + `SaveChanges` | 执行 DELETE SQL | 被拦截为 UPDATE 设置 IsDeleted=true | **不需要** |
| `_repository.GetByIdAsync(id)` | 查不到已删除的 | 全局过滤器自动排除，同样查不到 | **不需要** |
| `_repository.GetAllAsync()` | 不包含已删除的 | 不包含已删除的 | **不需要** |
| `Any(x => x.Id == id)` | 已删除的返回 false | 已删除的返回 false | **不需要** |
| `Count()` | 不统计已删除的 | 不统计已删除的 | **不需要** |
| 外键 Include 加载 | 不加载已删除的关联 | 不加载已删除的关联（关联实体上也有过滤器） | **不需要** |

---

## 附录A：数据库索引一览（软删除相关）

为保证软删除查询性能，已为四张业务表分别创建 `(IsDeleted, DeletedAt)` 复合索引：

| 表 | 索引名 | 字段 | 用途 |
|----|--------|------|------|
| SharePosts | `IX_SharePosts_IsDeleted_DeletedAt` | `IsDeleted, DeletedAt` | 1. 全局查询过滤；2. 6个月清理时按 DeletedAt 范围查询 |
| Reservations | `IX_Reservations_IsDeleted_DeletedAt` | `IsDeleted, DeletedAt` | 同上 |
| PickupCodes | `IX_PickupCodes_IsDeleted_DeletedAt` | `IsDeleted, DeletedAt` | 同上 |
| KarmaPoints | `IX_KarmaPoints_IsDeleted_DeletedAt` | `IsDeleted, DeletedAt` | 同上 |
| DeletedEntitySnapshots | `IX_DeletedEntitySnapshots_DeletedAt` | `DeletedAt` | 快照表按时间清理 |
| DeletedEntitySnapshots | `IX_DeletedEntitySnapshots_EntityType_EntityId` | `EntityType, EntityId` | 按实体类型+ID反查快照 |

---

## 附录B：核心代码文件速查表

| 文件路径 | 职责 |
|----------|------|
| `src/LeftoverShare.API/Entities/ISoftDeletable.cs` | 软删除接口定义 |
| `src/LeftoverShare.API/Entities/DeletedEntitySnapshot.cs` | 审计快照实体 |
| `src/LeftoverShare.API/Data/AppDbContext.cs` | 全局查询过滤器配置、SaveChanges 拦截器、级联软删除、快照生成 |
| `src/LeftoverShare.API/Repositories/Repository.cs` | `GetByIdIgnoreFilterAsync`、`HardDelete`、`GetDeletedPagedAsync` 等方法 |
| `src/LeftoverShare.API/Controllers/RecycleBinController.cs` | 回收站四个 API 端点 |
| `src/LeftoverShare.API/Services/Impl/RecycleBinService.cs` | 回收站查询、恢复、快照详情、永久删除 |
| `src/LeftoverShare.API/BackgroundServices/HardCleanupScheduler.cs` | 每月物理清理调度器（BackgroundService） |
| `src/LeftoverShare.API/Services/Impl/HardCleanupService.cs` | 物理清理具体实现（按外键顺序清理4张表+快照表） |
| `src/LeftoverShare.API/Helpers/DailyCleanupSettings.cs` | 配置类（含清理开关、执行时间、保留月数） |
