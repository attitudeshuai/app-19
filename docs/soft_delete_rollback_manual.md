# 软删除功能回滚操作手册

> **文档版本**：v1.0  
> **更新日期**：2026-06-22  
> **适用对象**：运维团队、DBA、紧急响应负责人  
> **执行等级**：P0 级操作，执行前请仔细阅读全文并确认每一步

---

## 一、回滚触发条件（何时需要回滚）

### 1.1 必须立即紧急回滚的场景（P0）

满足以下**任一条件**时，应立即启动紧急回滚流程：

| 编号 | 触发条件 | 风险等级 | 判断标准 |
|------|----------|----------|----------|
| R-01 | 软删除导致核心业务数据查询异常 | P0 | 列表查询返回空、详情 404、统计数据严重失真，且影响用户数 ≥ 10% |
| R-02 | 删除操作后数据未按预期隐藏 | P0 | 用户删除分享帖后，列表中仍可见，造成用户困惑或投诉 |
| R-03 | SaveChanges 拦截器引发数据库死锁 | P0 | 慢查询日志中出现大量死锁（Deadlock），或数据库 CPU 持续 ≥ 80% 超过 5 分钟 |
| R-04 | 级联软删除异常导致数据不一致 | P0 | 删除 SharePost 后 Reservation 未同时标记删除、或出现孤立数据 |
| R-05 | 全局查询过滤器导致 EF Core 查询生成错误 SQL | P0 | 应用日志中出现大量 EF Core `InvalidOperationException` 或数据库语法错误 |
| R-06 | 6个月物理清理任务误删了未过期数据 | P0 | ScheduledTaskLogs 中清理数量异常偏大，或用户报告保留期内数据丢失 |

### 1.2 建议渐进式回滚的场景（P1 / P2）

满足以下条件时，优先考虑**渐进式回滚**（禁用物理清理开关 + 代码回滚），而非紧急回滚：

| 编号 | 触发条件 | 风险等级 | 建议 |
|------|----------|----------|------|
| R-07 | 回收站 API 存在非阻塞性 Bug | P2 | 如分页排序错误、预览显示异常，不影响核心业务，走正常修复迭代 |
| R-08 | 快照表膨胀速度超出预期 | P1 | 监控到 DeletedEntitySnapshots 表日增 > 1GB，先禁用清理开关 + 紧急优化 |
| R-09 | 恢复功能有小概率失败 | P1 | 特定边界条件下恢复失败，但失败率 < 1%，走修复流程 |
| R-10 | 性能测试发现带过滤的查询变慢 | P2 | 慢查询比例 < 0.1%，先检查索引是否正确创建，无需立即回滚代码 |

### 1.3 回滚前的快速确认清单

在决定回滚前，请执行以下快速检查（**预计耗时 ≤ 5 分钟**）：

- [ ] 确认问题是否可复现（提供具体步骤、请求ID、错误日志）
- [ ] 确认不是客户端缓存、CDN 缓存导致的假象
- [ ] 确认数据库 `IsDeleted` / `DeletedAt` 字段是否已正确添加
- [ ] 确认 `IX_*_IsDeleted_DeletedAt` 复合索引是否已创建
- [ ] 确认不是代码 Merge 错误或配置文件缺失导致的问题
- [ ] 确认问题发生在软删除相关代码部署之后（关联版本号 / Commit Hash）
- [ ] 通知**研发负责人**确认是否触发回滚（双人确认原则）

---

## 二、紧急回滚流程

> **警告**：紧急回滚会停止服务，预计造成 **5~15 分钟**的业务中断。请在执行前通过站内通知/前端公告告知用户。

### 总体流程概览

```
步骤1：停止应用服务（防止新的软删除写入）
    ↓
步骤2：执行 SQL 回滚脚本（清除软删除标记，将数据恢复为物理删除状态）
    ↓
步骤3：使用 Git 回滚代码（切换到软删除部署前的稳定版本）
    ↓
步骤4：重新部署并启动服务
    ↓
步骤5：冒烟测试验证（核心场景全绿后恢复对外服务）
```

---

### 步骤1：停止应用服务

**目标**：防止回滚过程中继续有新的软删除写入，造成数据不一致。

#### 1.1 Docker Compose 部署方式（推荐）

```bash
# 进入项目根目录
cd d:\charles\program\ai\apps\02.work session\solo-0601\source code\app-19

# 停止 API 服务容器（数据库容器保持运行）
docker-compose stop leftover-share-api

# 确认服务已停止
docker-compose ps
# 预期输出：leftover-share-api 状态为 Exit 0
```

#### 1.2 原生 IIS / Windows Service 部署方式

```powershell
# IIS 方式：停止应用池
Stop-WebAppPool -Name "LeftoverShareAppPool"

# 或 Windows Service 方式
Stop-Service -Name "LeftoverShareAPI"

# 确认端口 5000/5001 已不再监听
netstat -ano | findstr ":5000"
# 预期无输出
```

#### 1.3 Kubernetes 部署方式（如使用）

```bash
# 将 API 副本数调整为 0
kubectl scale deployment leftover-share-api --replicas=0 -n production

# 确认所有 Pod 已终止
kubectl get pods -n production -l app=leftover-share-api
# 预期输出：No resources found
```

> **操作验证**：确认 `http(s)://<域名>/health` 返回连接超时或 502/503 错误。

---

### 步骤2：执行 SQL 回滚脚本

**目标**：
1. 将所有软删除的业务数据（`IsDeleted = true`）**物理删除**，恢复为迁移前的物理删除语义
2. 删除审计快照表 `DeletedEntitySnapshots` 中的所有记录（可选，也可以 DROP 整个表）
3. 移除数据库层面的软删除字段（可选，激进模式；保守模式保留字段和索引）

> **前置操作**：**必须先对数据库做完整备份**！
> ```sql
> -- MySQL 备份示例（在数据库服务器上执行）
> mysqldump -u <用户名> -p<密码> --single-transaction --routines --triggers \
>   leftover_share_db > leftover_share_db_backup_YYYYMMDD_HHMM.sql
> ```

#### 2.1 保守模式（推荐，风险低）

保留软删除字段和索引，仅将 `IsDeleted = true` 的记录物理删除。适用于短期可能重新启用软删除的场景。

```sql
-- =============================================
-- 保守模式 SQL 回滚脚本（MySQL 8.0+）
-- 执行前请确认已做完整数据库备份！
-- =============================================

-- 切换到目标数据库
USE leftover_share_db;

-- 开启事务（回滚脚本本身也需要可回退）
START TRANSACTION;

-- 1. 记录待清理的数量（便于回滚后核对）
SELECT '== 清理前统计 ==' AS info;
SELECT COUNT(*) AS shareposts_soft_deleted_count FROM SharePosts WHERE IsDeleted = TRUE;
SELECT COUNT(*) AS reservations_soft_deleted_count FROM Reservations WHERE IsDeleted = TRUE;
SELECT COUNT(*) AS pickupcodes_soft_deleted_count FROM PickupCodes WHERE IsDeleted = TRUE;
SELECT COUNT(*) AS karmapoints_soft_deleted_count FROM KarmaPoints WHERE IsDeleted = TRUE;
SELECT COUNT(*) AS snapshots_total_count FROM DeletedEntitySnapshots;

-- 2. 物理删除软删除的业务数据
--    严格按外键依赖顺序：PickupCode → Reservation → SharePost → KarmaPoint
--    （与自动清理顺序相反，因为 PickupCode 有外键依赖 Reservation）

-- 第1步：删除 PickupCodes 中已软删除的
DELETE FROM PickupCodes WHERE IsDeleted = TRUE;

-- 第2步：删除 Reservations 中已软删除的
DELETE FROM Reservations WHERE IsDeleted = TRUE;

-- 第3步：删除 SharePosts 中已软删除的
DELETE FROM SharePosts WHERE IsDeleted = TRUE;

-- 第4步：删除 KarmaPoints 中已软删除的
DELETE FROM KarmaPoints WHERE IsDeleted = TRUE;

-- 3. 清理快照表（保守模式可以 TRUNCATE，激进模式直接 DROP）
TRUNCATE TABLE DeletedEntitySnapshots;
-- 如果后续确定不再使用软删除，可改为：
-- DROP TABLE DeletedEntitySnapshots;

-- 4. 清理定时任务日志中的 MonthlyHardCleanup 记录（可选）
-- DELETE FROM ScheduledTaskLogs WHERE TaskName = 'MonthlyHardCleanup';

-- 5. 清理后统计
SELECT '== 清理后统计 ==' AS info;
SELECT COUNT(*) AS shareposts_remaining FROM SharePosts;
SELECT COUNT(*) AS reservations_remaining FROM Reservations;
SELECT COUNT(*) AS pickupcodes_remaining FROM PickupCodes;
SELECT COUNT(*) AS karmapoints_remaining FROM KarmaPoints;
SELECT COUNT(*) AS snapshots_remaining FROM DeletedEntitySnapshots;

-- 提交事务（核对数量无误后再 COMMIT；有问题就 ROLLBACK）
-- COMMIT;
-- ROLLBACK;
```

> **执行要点**：
> 1. 上述脚本**故意注释了 COMMIT**，请先执行到统计部分，确认数量无误后再手动 COMMIT
> 2. 如果数据量较大（单表 > 10 万），建议分批删除（每次 1000 条），避免长事务锁表
> 3. 执行期间密切关注数据库慢查询和锁等待情况

#### 2.2 激进模式（彻底移除软删除痕迹）

适用于确定不再使用软删除的场景，会移除所有软删除字段、索引和快照表。

```sql
-- =============================================
-- 激进模式 SQL 回滚脚本（MySQL 8.0+）
-- 执行前请确认已做完整数据库备份！
-- =============================================

USE leftover_share_db;
START TRANSACTION;

-- ---- A. 先物理删除软删除数据（同保守模式）----
DELETE FROM PickupCodes WHERE IsDeleted = TRUE;
DELETE FROM Reservations WHERE IsDeleted = TRUE;
DELETE FROM SharePosts WHERE IsDeleted = TRUE;
DELETE FROM KarmaPoints WHERE IsDeleted = TRUE;

-- ---- B. 删除软删除相关的索引 ----
-- 删除 SharePosts 表的软删除索引
ALTER TABLE SharePosts DROP INDEX IX_SharePosts_IsDeleted_DeletedAt;
-- 删除 Reservations 表的软删除索引
ALTER TABLE Reservations DROP INDEX IX_Reservations_IsDeleted_DeletedAt;
-- 删除 PickupCodes 表的软删除索引
ALTER TABLE PickupCodes DROP INDEX IX_PickupCodes_IsDeleted_DeletedAt;
-- 删除 KarmaPoints 表的软删除索引
ALTER TABLE KarmaPoints DROP INDEX IX_KarmaPoints_IsDeleted_DeletedAt;

-- ---- C. 删除四个业务表的软删除字段 ----
-- 注意：ALTER TABLE 会锁表，大表请在低峰期执行

ALTER TABLE SharePosts
  DROP COLUMN IsDeleted,
  DROP COLUMN DeletedAt,
  DROP COLUMN DeletedBy,
  DROP COLUMN DeletionReason;

ALTER TABLE Reservations
  DROP COLUMN IsDeleted,
  DROP COLUMN DeletedAt,
  DROP COLUMN DeletedBy,
  DROP COLUMN DeletionReason;

ALTER TABLE PickupCodes
  DROP COLUMN IsDeleted,
  DROP COLUMN DeletedAt,
  DROP COLUMN DeletedBy,
  DROP COLUMN DeletionReason;

ALTER TABLE KarmaPoints
  DROP COLUMN IsDeleted,
  DROP COLUMN DeletedAt,
  DROP COLUMN DeletedBy,
  DROP COLUMN DeletionReason;

-- ---- D. 删除审计快照表 ----
DROP TABLE DeletedEntitySnapshots;

-- ---- E. 清理定时任务日志 ----
DELETE FROM ScheduledTaskLogs WHERE TaskName = 'MonthlyHardCleanup';

COMMIT;
```

> **执行要点**：
> 1. `ALTER TABLE ... DROP COLUMN` 在 MySQL 中会重建表，大表（> 100 万行）可能耗时数十分钟，请预估维护窗口
> 2. 推荐使用 `pt-online-schema-change` 或 `gh-ost` 等在线 DDL 工具，避免长时间锁表

---

### 步骤3：使用 Git 回滚代码

**目标**：将应用代码切换到软删除部署前的稳定版本。

#### 3.1 查找软删除部署前的 Commit Hash

```bash
# 进入项目根目录
cd "d:\charles\program\ai\apps\02.work session\solo-0601\source code\app-19"

# 查看最近的提交历史，找到软删除功能合入之前的那个 commit
git log --oneline -30

# 预期输出示例：
# abc1234 (HEAD -> main) feat: 软删除功能上线
# def5678 chore: 更新依赖版本
# ghi9012 fix: 修复预订超时的 bug   <-- 选择这个（软删除合入之前的稳定版本）
# jkl3456 ...
```

#### 3.2 方式一：Git Reset（简单直接，会丢弃后续提交）

> **仅适用于**：确认软删除之后没有其他需要保留的提交。

```bash
# 强制回退到指定 commit（--hard 会丢弃所有后续修改）
git reset --hard ghi9012

# 推送到远程仓库（注意：需要 --force，因为重写了历史）
git push origin main --force
```

#### 3.3 方式二：Git Revert（推荐，保留历史，更安全）

> **适用于**：软删除之后还有其他需要保留的功能提交，或多人协作分支。

```bash
# 方式 A：仅 revert 软删除的那个 commit
git revert abc1234
# 解决可能的冲突后：
git commit

# 方式 B：如果软删除涉及多个连续 commit，可 revert 一个范围
# 假设 abc1230~abc1234 都是软删除相关的提交：
git revert abc1230^..abc1234

# 推送到远程（不需要 --force，因为没有重写历史）
git push origin main
```

#### 3.4 方式三：直接检出指定 Commit（最快，无需修改 Git 历史）

> **适用于**：紧急情况下快速恢复服务，Git 历史可后续整理。

```bash
# 检出指定 commit（进入 detached HEAD 状态）
git checkout ghi9012

# 如果使用 Docker 部署，直接用这个 commit 构建镜像即可
# 后续需要重新绑定分支时：
git checkout -b rollback-temp ghi9012
git push origin rollback-temp
# 在部署系统中将生产分支切换为 rollback-temp
```

---

### 步骤4：重新部署并启动服务

#### 4.1 Docker Compose 部署

```bash
cd "d:\charles\program\ai\apps\02.work session\solo-0601\source code\app-19"

# 如果代码已回滚，重新构建镜像
docker-compose build --no-cache leftover-share-api

# 启动服务
docker-compose up -d leftover-share-api

# 查看启动日志，确认无异常
docker-compose logs -f leftover-share-api
```

#### 4.2 原生 IIS / Windows Service 部署

```powershell
# IIS 方式
# 1. 用回滚后的代码重新发布到 IIS 目录
# 2. 启动应用池
Start-WebAppPool -Name "LeftoverShareAppPool"

# Windows Service 方式
# 1. 替换服务目录下的 DLL 文件
# 2. 启动服务
Start-Service -Name "LeftoverShareAPI"
```

#### 4.3 Kubernetes 部署

```bash
# 触发重新部署（如果使用 GitOps 会自动同步）
kubectl rollout restart deployment leftover-share-api -n production

# 查看滚动部署状态
kubectl rollout status deployment leftover-share-api -n production

# 确认 Pod 正常启动
kubectl get pods -n production -l app=leftover-share-api
```

> **启动验证**：
> - 确认容器 / 服务日志中无 `Error` 级别异常
> - 确认 `GET /health` 接口返回 200（如已配置健康检查）
> - 确认数据库连接正常（应用日志中无 `SqlException`）

---

### 步骤5：冒烟测试验证

执行以下核心场景测试，确认业务恢复正常（**预计耗时 5~10 分钟**）：

| 编号 | 测试场景 | 操作步骤 | 预期结果 | 状态 |
|------|----------|----------|----------|------|
| S-01 | 用户登录 | 使用测试账号调用登录接口 | 返回 200 + JWT Token | ☐ |
| S-02 | 分享帖列表 | `GET /api/shareposts` | 返回 200，列表非空，且**不包含**任何已删除的帖子 | ☐ |
| S-03 | 分享帖详情 | 从列表取一个有效 ID 调 `GET /api/shareposts/{id}` | 返回 200 + 详情数据 | ☐ |
| S-04 | 创建分享帖 | `POST /api/shareposts` 创建新帖子 | 返回 201，数据库 `SharePosts` 表新增 1 行 | ☐ |
| S-05 | 删除分享帖 | `DELETE /api/shareposts/{id}` 删除刚创建的帖子 | 返回 200，数据库中该记录**被物理删除**（行数 -1） | ☐ |
| S-06 | 删除后列表 | 再次 `GET /api/shareposts` | 已删除的帖子不再出现 | ☐ |
| S-07 | 删除后详情 | `GET /api/shareposts/{id}` 查已删除 ID | 返回 404 | ☐ |
| S-08 | 创建预订 | 对存在的帖子创建预订 | 返回 201，数据库 `Reservations` +1 | ☐ |
| S-09 | 删除预订 | `DELETE /api/reservations/{id}` | 返回 200，记录**被物理删除** | ☐ |
| S-10 | 积分查询 | `GET /api/karmapoints`（如有该接口） | 列表正常，无异常数据 | ☐ |
| S-11 | 统计接口 | `GET /api/stats/overview` | 指标值合理，无报错 | ☐ |

> **冒烟测试通过标准**：S-01 ~ S-09 全部通过。出现任何异常立即暂停上线，返回步骤 2 排查。

---

## 三、渐进式回滚（降级为软删除可选）

**适用场景**：R-07 ~ R-10 类问题，无需立即停止服务，可通过配置开关快速降级。

### 3.1 通过配置开关禁用物理清理

如果问题出在**6个月自动物理清理**（如误删、性能问题），只需禁用该任务即可，无需回滚整个软删除功能。

#### 修改 `appsettings.json`

```json
{
  "DailyCleanupSettings": {
    "Enabled": true,                              // 保留每日过期处理（与软删除无关）
    "HardCleanupEnabled": false,                  // ← 设为 false：禁用每月物理清理
    "HardCleanupExecuteTime": "03:00",
    "SoftDeleteRetentionMonths": 6
  }
}
```

#### 环境变量方式（容器部署推荐）

无需重建镜像，重启容器即可生效：

```bash
# Docker Compose 环境变量（在 docker-compose.yml 的 environment 节添加）
environment:
  - DailyCleanupSettings__HardCleanupEnabled=false

# Kubernetes ConfigMap / Secret 方式
kubectl set env deployment leftover-share-api \
  DailyCleanupSettings__HardCleanupEnabled=false \
  -n production

# 触发滚动重启使配置生效
kubectl rollout restart deployment leftover-share-api -n production
```

> **生效验证**：重启后查看应用启动日志，应包含以下信息：
> ```
> info: LeftoverShare.API.BackgroundServices.HardCleanupScheduler[0]
>       每月物理清理定时任务已被禁用，调度器未启动
> ```

### 3.2 配置降级的优缺点对比

| 方案 | 优点 | 缺点 | 适用场景 |
|------|------|------|----------|
| 仅禁用物理清理 | 不停止服务，即时生效，软删除/恢复功能继续可用 | 软删除数据会无限堆积，需后续手动清理 | R-06（物理清理误删）、R-08（快照膨胀） |
| 代码回滚 + 保守 SQL | 服务中断时间短，保留字段便于后续重新启用 | 需停止服务，需要 Git 操作 | R-07、R-09、R-10 |
| 紧急回滚 + 激进 SQL | 彻底恢复迁移前状态，零残留 | 服务中断时间长，ALTER TABLE 大表可能耗时 | R-01 ~ R-05 严重故障 |

---

## 四、数据恢复应急预案

### 4.1 误删数据如何从快照恢复

**场景**：用户误操作删除了重要的分享帖/预订，且尚未超过6个月保留期（或尚未被物理清理任务处理）。

**恢复流程**：

```
用户联系客服说明误删情况（提供 ID / 时间 / 内容关键词）
    ↓
DBA / 管理员查询 DeletedEntitySnapshots 表定位记录
    ↓
SELECT * FROM DeletedEntitySnapshots
WHERE EntityType = 'SharePost'
  AND EntityDisplayName LIKE '%关键词%'
  AND DeletedAt BETWEEN '开始时间' AND '结束时间'
ORDER BY DeletedAt DESC;
    ↓
找到对应快照，记录其 Id 和 EntityId
    ↓
方式 A（推荐）：让用户或管理员通过回收站 API 自助恢复
  POST /api/recyclebin/restore
  Body: { "id": <快照Id>, "entityType": "SharePost" }
    ↓
方式 B（紧急情况下，直接 SQL 恢复）：
  1. 解析 SnapshotData JSON，确认数据完整性
  2. 执行 UPDATE 将 IsDeleted 置回 false
     UPDATE SharePosts
     SET IsDeleted = FALSE,
         DeletedAt = NULL,
         DeletedBy = NULL,
         DeletionReason = NULL
     WHERE Id = <EntityId>;
  3. （可选）删除对应快照记录
     DELETE FROM DeletedEntitySnapshots WHERE Id = <快照Id>;
    ↓
验证：GET /api/shareposts/<EntityId> 返回 200 且内容正确
```

### 4.2 如何从备份恢复物理删除的数据

**场景**：数据已被自动清理任务物理删除（超过6个月），或回滚脚本执行了 `DELETE`，需要从数据库备份中恢复。

**详细步骤**：

#### 步骤1：确定需要恢复的时间点

- 查询 `ScheduledTaskLogs` 表，确定物理清理任务的执行时间（`MonthlyHardCleanup` 任务的 `StartedAt`）
- 选择该时间点**之前**的数据库备份文件

#### 步骤2：备份当前生产库（防止恢复操作误伤）

```bash
mysqldump -u <user> -p<pwd> leftover_share_db > before_recovery_$(date +%Y%m%d_%H%M).sql
```

#### 步骤3：将历史备份恢复到临时实例

```bash
# 创建临时数据库
mysql -u root -p -e "CREATE DATABASE leftover_share_restore_temp;"

# 导入历史备份
mysql -u root -p leftover_share_restore_temp < backup_before_cleanup.sql
```

#### 步骤4：从临时库提取需要恢复的数据

```sql
-- 示例：恢复特定 ID 的 SharePost 及其关联数据
USE leftover_share_restore_temp;

-- 1. 提取目标 SharePost
SELECT * FROM SharePosts WHERE Id = <目标ID>;

-- 2. 提取关联的 Reservation
SELECT * FROM Reservations WHERE PostId = <目标ID>;

-- 3. 提取关联的 PickupCode
SELECT * FROM PickupCodes WHERE ReservationId IN (SELECT Id FROM Reservations WHERE PostId = <目标ID>);
```

#### 步骤5：将数据写回生产库

```sql
USE leftover_share_db;

-- 注意：如果原记录已物理删除，直接 INSERT；如果仍有软删除标记的记录，先 DELETE 再 INSERT
-- 操作前请确认目标 ID 在生产库中不存在，避免主键冲突
```

> **重要**：建议使用 `pt-table-sync` 等工具进行基于主键的精确同步，减少手动 SQL 出错概率。

---

## 五、回滚验证清单（Checklist）

### 5.1 回滚完成后必须执行的验证项

请逐项勾选并签字确认，未完成不得恢复对外服务。

| 类别 | 编号 | 验证项 | 验证方法 | 结果 | 验证人 |
|------|------|--------|----------|------|--------|
| **服务状态** | V-01 | API 服务正常启动 | 应用日志无 Error，/health 接口返回 200 | ☐ 通过 ☐ 失败 | |
| | V-02 | 数据库连接正常 | 连续请求 10 次列表接口，无数据库连接异常 | ☐ 通过 ☐ 失败 | |
| | V-03 | 所有定时任务日志正常 | 应用启动日志中 HardCleanupScheduler 按预期（启用/禁用） | ☐ 通过 ☐ 失败 | |
| **核心功能** | V-04 | 用户注册 / 登录正常 | 新用户注册 → 登录 → 获取 Token 全流程 | ☐ 通过 ☐ 失败 | |
| | V-05 | 分享帖 CRUD 正常 | 创建 → 查询 → 修改 → 删除 → 验证已物理删除 | ☐ 通过 ☐ 失败 | |
| | V-06 | 预订 CRUD 正常 | 创建预订 → 查询 → 删除 → 验证已物理删除 | ☐ 通过 ☐ 失败 | |
| | V-07 | 取餐码生成和核销正常 | 创建预订 → 生成取餐码 → 核销流程 | ☐ 通过 ☐ 失败 | |
| | V-08 | 积分流水正常 | 创建帖子 / 领取 → 查询积分 → 删除积分记录（如支持） | ☐ 通过 ☐ 失败 | |
| **数据一致性** | V-09 | 四张业务表无 IsDeleted=true 记录 | `SELECT COUNT(*) FROM 表名 WHERE IsDeleted = TRUE` 均返回 0 | ☐ 通过 ☐ 失败 | |
| | V-10 | 级联删除正确 | 删除帖子后，关联的 Reservation 和 PickupCode 也物理删除 | ☐ 通过 ☐ 失败 | |
| | V-11 | 外键约束无违反 | `SHOW ENGINE INNODB STATUS` 中无外键错误 | ☐ 通过 ☐ 失败 | |
| | V-12 | 业务统计数据合理 | `GET /api/stats/overview` 各指标与回滚前趋势一致（±5%） | ☐ 通过 ☐ 失败 | |
| **性能验证** | V-13 | 核心查询无慢查询 | 压力测试 5 分钟，慢查询比例 < 0.1% | ☐ 通过 ☐ 失败 | |
| | V-14 | 数据库 CPU / 连接数正常 | CloudDBA / 监控后台观察 5 分钟，指标在正常区间 | ☐ 通过 ☐ 失败 | |
| **清理工作** | V-15 | 回滚脚本文件已妥善保存 | 存放到运维文档目录，便于事故复盘 | ☐ 通过 ☐ 失败 | |
| | V-16 | 临时数据库已清理 | 删除 leftover_share_restore_temp 等临时库 | ☐ 通过 ☐ 失败 | |
| | V-17 | 已通知相关团队恢复服务 | 通知产品、客服、前端团队故障已解决 | ☐ 通过 ☐ 失败 | |

### 5.2 回滚报告模板

回滚完成后 **24 小时内**，运维负责人需提交回滚报告，包含：

```
# 软删除功能回滚报告

## 基本信息
- 回滚触发时间：YYYY-MM-DD HH:MM:SS
- 回滚完成时间：YYYY-MM-DD HH:MM:SS
- 总服务中断时长：XX 分钟
- 回滚执行人：XXX
- 在场见证人：XXX

## 触发原因
- 触发回滚的具体问题现象：
- 根因初步分析（如有）：
- 影响范围（受影响用户数 / 业务功能）：

## 执行步骤
1. 停止服务：完成时间 / 是否有异常：
2. SQL 回滚：模式（保守/激进）/ 清理数据量 / 是否有异常：
3. 代码回滚：方式（Reset/Revert/Checkout）/ Commit Hash：
4. 重新部署：方式 / 是否有异常：
5. 冒烟测试：通过项 / 失败项：

## 数据处理
- 已恢复数据量（条）：
- 仍丢失的数据量（条）：
- 是否从备份恢复：是 / 否，恢复时间点：

## 后续行动（Action Items）
| 编号 | 行动项 | 责任人 | 截止日期 | 状态 |
|------|--------|--------|----------|------|
| 1 | 修复软删除 Bug | XXX | YYYY-MM-DD | 待开始 |
| 2 | 完善回归测试覆盖 | XXX | YYYY-MM-DD | 待开始 |
| 3 | 优化自动清理监控告警 | XXX | YYYY-MM-DD | 待开始 |

## 附件
- 应用错误日志截图
- 数据库慢查询截图
- 回滚脚本执行结果截图
```

---

## 六、联系人和支持渠道

| 角色 | 姓名 | 联系方式 | 职责 |
|------|------|----------|------|
| **紧急响应总负责人** | [请填写] | 手机：[请填写] / 企业微信：[请填写] | 回滚决策审批、跨部门协调 |
| **运维值班** | [请填写] | 手机：[请填写] / 钉钉：[请填写] | 执行步骤1、4、5，部署操作 |
| **DBA 值班** | [请填写] | 手机：[请填写] / 钉钉：[请填写] | 执行步骤2，数据库备份/恢复，性能调优 |
| **研发负责人** | [请填写] | 手机：[请填写] / 企业微信：[请填写] | 步骤3代码回滚、根因分析、后续修复 |
| **测试负责人** | [请填写] | 手机：[请填写] / 企业微信：[请填写] | 步骤5冒烟测试、回归测试组织 |
| **产品负责人** | [请填写] | 手机：[请填写] / 企业微信：[请填写] | 用户通知、客服话术准备、影响范围评估 |

### 升级路径

```
一线运维发现异常
    ↓
运维值班（10 分钟内无法定位）
    ↓
紧急响应总负责人 + DBA + 研发负责人（联合决策是否回滚）
    ↓
确认回滚 → 执行本手册流程
    ↓
回滚完成 → 24 小时内召开复盘会议
```

### 外部支持资源

| 资源 | 链接 / 信息 |
|------|------------|
| EF Core 官方文档（查询过滤器） | https://learn.microsoft.com/ef/core/querying/filters |
| MySQL 在线 DDL 工具 pt-online-schema-change | https://www.percona.com/doc/percona-toolkit/LATEST/pt-online-schema-change.html |
| MySQL 备份恢复最佳实践 | 参考内部《数据库运维手册 v3.0》 |
