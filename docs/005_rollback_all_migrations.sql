-- ============================================================================
-- 脚本名称: 005_rollback_all_migrations.sql
-- 脚本说明: 完整回滚脚本 - 撤销所有软删除相关迁移
-- 适用数据库: MySQL 5.7+ / MySQL 8.0+
-- 创建日期: 2026-06-22
-- 执行顺序: 单独执行（用于回滚 001~003 所有迁移变更）
--
-- 功能描述:
--   本脚本用于完整撤销软删除迁移方案的所有数据库变更，包括：
--   1. 删除 DeletedEntitySnapshots 审计快照表（含其数据、索引和外键约束）
--   2. 删除四个业务表的软删除相关复合索引
--   3. 删除四个业务表的四个软删除字段（IsDeleted/DeletedAt/DeletedBy/DeletionReason）
--
-- 回滚影响评估:
--   - 【数据丢失风险】DeletedEntitySnapshots 表中的所有审计快照将永久删除！
--     如需保留，执行前请先对该表进行数据导出备份。
--   - 【业务代码影响】回滚后应用层的软删除功能将失效。请务必先停用或回滚
--     依赖软删除的应用代码版本，否则应用将因找不到字段而抛出异常。
--   - 【删除策略变更】回滚后系统恢复为物理删除模式，后续所有删除操作
--     将直接从数据库中移除数据，无法通过回收站恢复。
--
-- 回滚执行步骤（严格按顺序）:
--   1. 【强烈建议】对整个数据库进行完整备份：
--      mysqldump -u用户名 -p 数据库名 > backup_before_rollback.sql
--   2. 【如需要保留快照数据】单独备份 DeletedEntitySnapshots 表：
--      mysqldump -u用户名 -p 数据库名 DeletedEntitySnapshots > snapshots_backup.sql
--   3. 停止所有应用服务，确保无新的数据库写入
--   4. 使用具有 DROP TABLE / ALTER TABLE 权限的数据库账号执行本脚本
--   5. 执行命令: mysql -u用户名 -p 数据库名 < 005_rollback_all_migrations.sql
--   6. （可选）执行 004_verify_migration.sql，应看到所有检查项均为「不存在」
--   7. 部署回滚后的应用代码版本（不依赖软删除的版本）
--   8. 启动应用服务，验证系统功能正常
--
-- 脚本幂等性说明:
--   所有 DROP 操作均使用 IF EXISTS 保护，因此即使部分对象已不存在，
--   脚本仍可正常执行而不会报错，可安全重复运行。
--
-- 注意事项:
--   - 【高危操作】本脚本涉及 DROP TABLE 和 DROP COLUMN，请务必在测试环境
--     充分验证后再在生产环境执行！
--   - 对于大表，ALTER TABLE DROP COLUMN 操作可能耗时较长并锁表，
--     建议在业务低峰期执行。
--   - InnoDB 引擎下，删除字段可能不会立即释放磁盘空间，如需回收空间
--     可后续执行 OPTIMIZE TABLE 或 pt-online-schema-change。
--   - 删除顺序严格为：先删外键约束 -> 先删索引 -> 再删字段 -> 最后删表，
--     避免因对象依赖导致删除失败。
-- ============================================================================

-- 设置字符集，防止中文乱码
SET NAMES utf8mb4;

-- 【重要】关闭外键检查，避免外键依赖导致删除失败
-- 执行完成后脚本末尾会重新开启
SET FOREIGN_KEY_CHECKS = 0;

-- ============================================================================
-- 回滚前确认提示（可选，交互模式下可取消注释）
-- ============================================================================
-- 说明：以下语句仅用于人工确认，非交互模式（命令行导入）下不会暂停。
--       生产环境执行前请确保团队已确认回滚决策。
/*
SELECT '****************************************************************' AS '';
SELECT '  【警告】您即将执行完整回滚操作！' AS '';
SELECT '  本操作将：' AS '';
SELECT '    1. 永久删除 DeletedEntitySnapshots 表及其中的所有审计数据';
SELECT '    2. 移除四个业务表的所有软删除字段和索引';
SELECT '  请确认已完成：' AS '';
SELECT '    - 数据库完整备份';
SELECT '    - 应用服务已停止';
SELECT '    - 团队决策确认';
SELECT '****************************************************************' AS '';
*/

-- ============================================================================
-- 第一阶段：删除 DeletedEntitySnapshots 审计快照表
-- ============================================================================
-- 说明：先删除快照表可解除其对 Users 表的外键依赖。
--       由于设置了 FOREIGN_KEY_CHECKS=0，外键不会阻止表删除，
--       但先处理快照表更符合逻辑依赖顺序。

DROP TABLE IF EXISTS DeletedEntitySnapshots;
SELECT '已删除表：DeletedEntitySnapshots（如存在）' AS 回滚进度;

-- ============================================================================
-- 第二阶段：删除四个业务表的软删除相关索引
-- ============================================================================
-- 说明：MySQL 删除列时会自动删除包含该列的索引，
--       但显式先删除索引更清晰，也便于跟踪回滚进度。

-- ----------------------------------------------------------------------------
-- 2-1. SharePosts 表索引删除
-- ----------------------------------------------------------------------------
DROP INDEX IF EXISTS IX_SharePosts_IsDeleted_DeletedAt ON SharePosts;
SELECT '已删除索引：IX_SharePosts_IsDeleted_DeletedAt（如存在）' AS 回滚进度;

-- ----------------------------------------------------------------------------
-- 2-2. Reservations 表索引删除
-- ----------------------------------------------------------------------------
DROP INDEX IF EXISTS IX_Reservations_IsDeleted_DeletedAt ON Reservations;
SELECT '已删除索引：IX_Reservations_IsDeleted_DeletedAt（如存在）' AS 回滚进度;

-- ----------------------------------------------------------------------------
-- 2-3. PickupCodes 表索引删除
-- ----------------------------------------------------------------------------
DROP INDEX IF EXISTS IX_PickupCodes_IsDeleted_DeletedAt ON PickupCodes;
SELECT '已删除索引：IX_PickupCodes_IsDeleted_DeletedAt（如存在）' AS 回滚进度;

-- ----------------------------------------------------------------------------
-- 2-4. KarmaPoints 表索引删除
-- ----------------------------------------------------------------------------
DROP INDEX IF EXISTS IX_KarmaPoints_IsDeleted_DeletedAt ON KarmaPoints;
SELECT '已删除索引：IX_KarmaPoints_IsDeleted_DeletedAt（如存在）' AS 回滚进度;

-- ============================================================================
-- 第三阶段：删除四个业务表的软删除字段
-- ============================================================================
-- 说明：字段删除顺序无特殊依赖，按逻辑顺序逐个删除。
--       每个字段单独一条 ALTER 语句，便于排查问题（如某列不存在）。

-- ----------------------------------------------------------------------------
-- 3-1. SharePosts 表字段删除
-- ----------------------------------------------------------------------------
ALTER TABLE SharePosts DROP COLUMN IF EXISTS IsDeleted;
ALTER TABLE SharePosts DROP COLUMN IF EXISTS DeletedAt;
ALTER TABLE SharePosts DROP COLUMN IF EXISTS DeletedBy;
ALTER TABLE SharePosts DROP COLUMN IF EXISTS DeletionReason;
SELECT '已删除 SharePosts 表软删除字段（如存在）' AS 回滚进度;

-- ----------------------------------------------------------------------------
-- 3-2. Reservations 表字段删除
-- ----------------------------------------------------------------------------
ALTER TABLE Reservations DROP COLUMN IF EXISTS IsDeleted;
ALTER TABLE Reservations DROP COLUMN IF EXISTS DeletedAt;
ALTER TABLE Reservations DROP COLUMN IF EXISTS DeletedBy;
ALTER TABLE Reservations DROP COLUMN IF EXISTS DeletionReason;
SELECT '已删除 Reservations 表软删除字段（如存在）' AS 回滚进度;

-- ----------------------------------------------------------------------------
-- 3-3. PickupCodes 表字段删除
-- ----------------------------------------------------------------------------
ALTER TABLE PickupCodes DROP COLUMN IF EXISTS IsDeleted;
ALTER TABLE PickupCodes DROP COLUMN IF EXISTS DeletedAt;
ALTER TABLE PickupCodes DROP COLUMN IF EXISTS DeletedBy;
ALTER TABLE PickupCodes DROP COLUMN IF EXISTS DeletionReason;
SELECT '已删除 PickupCodes 表软删除字段（如存在）' AS 回滚进度;

-- ----------------------------------------------------------------------------
-- 3-4. KarmaPoints 表字段删除
-- ----------------------------------------------------------------------------
ALTER TABLE KarmaPoints DROP COLUMN IF EXISTS IsDeleted;
ALTER TABLE KarmaPoints DROP COLUMN IF EXISTS DeletedAt;
ALTER TABLE KarmaPoints DROP COLUMN IF EXISTS DeletedBy;
ALTER TABLE KarmaPoints DROP COLUMN IF EXISTS DeletionReason;
SELECT '已删除 KarmaPoints 表软删除字段（如存在）' AS 回滚进度;

-- ============================================================================
-- 回滚完成验证
-- ============================================================================
-- 说明：快速验证关键对象是否已成功删除。
--       如需完整验证，可执行 004_verify_migration.sql，
--       所有检查项应显示「✗ 不存在」。

SELECT '================================================================' AS '';
SELECT '  回滚完成快速验证' AS '';
SELECT '================================================================' AS '';

SELECT
    CASE WHEN COUNT(*) = 0 THEN '✓ 已删除' ELSE '✗ 仍存在' END AS 快照表状态,
    'DeletedEntitySnapshots' AS 对象名
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME = 'DeletedEntitySnapshots'

UNION ALL

SELECT
    CASE WHEN COUNT(*) = 0 THEN '✓ 已删除' ELSE '✗ 仍存在' END AS SharePosts_IsDeleted,
    'SharePosts.IsDeleted 字段' AS 对象名
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME = 'SharePosts'
  AND COLUMN_NAME = 'IsDeleted'

UNION ALL

SELECT
    CASE WHEN COUNT(*) = 0 THEN '✓ 已删除' ELSE '✗ 仍存在' END AS Reservations_IsDeleted,
    'Reservations.IsDeleted 字段' AS 对象名
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME = 'Reservations'
  AND COLUMN_NAME = 'IsDeleted'

UNION ALL

SELECT
    CASE WHEN COUNT(*) = 0 THEN '✓ 已删除' ELSE '✗ 仍存在' END AS PickupCodes_IsDeleted,
    'PickupCodes.IsDeleted 字段' AS 对象名
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME = 'PickupCodes'
  AND COLUMN_NAME = 'IsDeleted'

UNION ALL

SELECT
    CASE WHEN COUNT(*) = 0 THEN '✓ 已删除' ELSE '✗ 仍存在' END AS KarmaPoints_IsDeleted,
    'KarmaPoints.IsDeleted 字段' AS 对象名
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME = 'KarmaPoints'
  AND COLUMN_NAME = 'IsDeleted';

-- ============================================================================
-- 恢复数据库配置
-- ============================================================================

-- 重新开启外键检查（重要！）
SET FOREIGN_KEY_CHECKS = 1;

-- ============================================================================
-- 回滚完成后续操作提示
-- ============================================================================
SELECT '================================================================' AS '';
SELECT '  完整回滚脚本执行完成！' AS '';
SELECT '================================================================' AS ''
UNION ALL
SELECT '后续操作步骤：' AS ''
UNION ALL
SELECT '1. 请运行 004_verify_migration.sql 进行完整验证' AS ''
UNION ALL
SELECT '2. 部署回滚后的应用代码版本（不依赖软删除字段）' AS ''
UNION ALL
SELECT '3. 启动应用服务，验证各项功能正常' AS ''
UNION ALL
SELECT '4. 后续删除操作将采用物理删除模式，请知悉' AS '';

-- ============================================================================
-- 脚本结束
-- ============================================================================
