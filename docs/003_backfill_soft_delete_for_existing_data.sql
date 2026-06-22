-- ============================================================================
-- 脚本名称: 003_backfill_soft_delete_for_existing_data.sql
-- 脚本说明: 历史数据回填脚本 - 为现有数据设置软删除初始值
-- 适用数据库: MySQL 5.7+ / MySQL 8.0+
-- 创建日期: 2026-06-22
-- 执行顺序: 第3步（共5步）
-- 依赖脚本: 001_add_soft_delete_columns.sql, 002_create_deleted_entity_snapshots_table.sql
--
-- 功能描述:
--   由于 001 脚本为新增字段设置了 DEFAULT 0，新插入的数据会自动使用默认值。
--   但对于 ALTER TABLE 之前已存在的数据行，MySQL 的行为取决于 SQL 模式：
--   - 某些配置下已存在的行会自动使用 DEFAULT 值
--   - 某些配置下已存在的行可能为 NULL 或未正确设置
--   本脚本通过显式 UPDATE 语句确保所有历史数据的软删除字段值正确。
--
-- 回填策略:
--   现有业务数据全部视为"未删除"状态：
--     IsDeleted = 0（未删除）
--     DeletedAt = NULL（无删除时间）
--     DeletedBy = NULL（无删除操作者）
--     DeletionReason = NULL（无删除原因）
--
-- 本脚本包含三部分内容:
--   A. 【推荐执行】安全检查SQL - 统计各表现有行数，用于回填前后对比
--   B. 【必须执行】历史数据回填SQL - 显式更新四个表的软删除字段
--   C. 【可选参考】回滚语句 - 如需撤销，移除四个表的字段和索引
--
-- 使用说明:
--   1. 请先执行 001 和 002 脚本完成前两步迁移
--   2. 强烈建议先在测试环境验证本脚本
--   3. 在执行前请确保已对数据库进行完整备份
--   4. 建议在业务低峰期执行，大批量数据可能需要较长时间
--   5. 按顺序执行 A（安全检查）→ B（数据回填）
--   6. 执行命令: mysql -u用户名 -p 数据库名 < 003_backfill_soft_delete_for_existing_data.sql
--   7. 执行完成后，请运行 004_verify_migration.sql 验证迁移结果
--
-- 注意事项:
--   - 本脚本使用 LOW_PRIORITY 和 IGNORE，减少锁竞争并忽略重复更新
--   - 对于大数据量表（>10万行），建议分批执行或使用 pt-online-schema-change
--   - 回填操作不会产生审计快照（因为并未删除任何数据）
--   - 如需回滚，使用脚本 C 部分的语句，或执行 005_rollback_all_migrations.sql
-- ============================================================================

-- 设置字符集，防止中文乱码
SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS = 0;

-- ============================================================================
--  A. 安全检查SQL：执行前数据统计（可选，建议执行）
-- ============================================================================
-- 说明：执行前请先运行以下 SELECT 语句，记录各表行数，
--       以便回填后对比确认数据未丢失。
--       此部分仅查询，不修改任何数据。

-- 可取消下方注释执行安全检查
/*
SELECT '========== 迁移前数据行数统计 ==========' AS '';

SELECT 'SharePosts 表' AS 表名, COUNT(*) AS 总行数
FROM SharePosts;

SELECT 'Reservations 表' AS 表名, COUNT(*) AS 总行数
FROM Reservations;

SELECT 'PickupCodes 表' AS 表名, COUNT(*) AS 总行数
FROM PickupCodes;

SELECT 'KarmaPoints 表' AS 表名, COUNT(*) AS 总行数
FROM KarmaPoints;

SELECT '========== 统计完成 ==========' AS '';
*/

-- ============================================================================
--  B. 历史数据回填SQL：显式设置软删除字段（必须执行）
-- ============================================================================
-- 说明：显式 UPDATE 确保所有现有数据的 IsDeleted=0。
--       使用 LOW_PRIORITY 降低对业务的影响（MyISAM有效，InnoDB可忽略）
--       使用 IGNORE 跳过可能的约束冲突（此处应不会有冲突）

-- 关闭自动提交，使用事务确保回填操作原子性
START TRANSACTION;

-- ----------------------------------------------------------------------------
-- B-1. 回填 SharePosts 表
-- ----------------------------------------------------------------------------
UPDATE LOW_PRIORITY IGNORE SharePosts
SET
    IsDeleted = 0,
    DeletedAt = NULL,
    DeletedBy = NULL,
    DeletionReason = NULL
WHERE
    IsDeleted IS NULL
    OR IsDeleted != 0
    OR DeletedAt IS NOT NULL
    OR DeletedBy IS NOT NULL
    OR DeletionReason IS NOT NULL;

-- 记录 SharePosts 回填影响的行数（仅调试用，生产环境可注释）
SELECT 'SharePosts 表回填完成，影响行数：' AS '', ROW_COUNT() AS 影响行数;

-- ----------------------------------------------------------------------------
-- B-2. 回填 Reservations 表
-- ----------------------------------------------------------------------------
UPDATE LOW_PRIORITY IGNORE Reservations
SET
    IsDeleted = 0,
    DeletedAt = NULL,
    DeletedBy = NULL,
    DeletionReason = NULL
WHERE
    IsDeleted IS NULL
    OR IsDeleted != 0
    OR DeletedAt IS NOT NULL
    OR DeletedBy IS NOT NULL
    OR DeletionReason IS NOT NULL;

SELECT 'Reservations 表回填完成，影响行数：' AS '', ROW_COUNT() AS 影响行数;

-- ----------------------------------------------------------------------------
-- B-3. 回填 PickupCodes 表
-- ----------------------------------------------------------------------------
UPDATE LOW_PRIORITY IGNORE PickupCodes
SET
    IsDeleted = 0,
    DeletedAt = NULL,
    DeletedBy = NULL,
    DeletionReason = NULL
WHERE
    IsDeleted IS NULL
    OR IsDeleted != 0
    OR DeletedAt IS NOT NULL
    OR DeletedBy IS NOT NULL
    OR DeletionReason IS NOT NULL;

SELECT 'PickupCodes 表回填完成，影响行数：' AS '', ROW_COUNT() AS 影响行数;

-- ----------------------------------------------------------------------------
-- B-4. 回填 KarmaPoints 表
-- ----------------------------------------------------------------------------
UPDATE LOW_PRIORITY IGNORE KarmaPoints
SET
    IsDeleted = 0,
    DeletedAt = NULL,
    DeletedBy = NULL,
    DeletionReason = NULL
WHERE
    IsDeleted IS NULL
    OR IsDeleted != 0
    OR DeletedAt IS NOT NULL
    OR DeletedBy IS NOT NULL
    OR DeletionReason IS NOT NULL;

SELECT 'KarmaPoints 表回填完成，影响行数：' AS '', ROW_COUNT() AS 影响行数;

-- ----------------------------------------------------------------------------
-- B-5. 回填后二次校验（确认 IsDeleted 非零数据的数量，预期为0）
-- ----------------------------------------------------------------------------
SELECT '========== 回填后数据校验（预期软删除行数均为0） ==========' AS '';

SELECT 'SharePosts' AS 表名, COUNT(*) AS 软删除标记为1的行数
FROM SharePosts
WHERE IsDeleted = 1;

SELECT 'Reservations' AS 表名, COUNT(*) AS 软删除标记为1的行数
FROM Reservations
WHERE IsDeleted = 1;

SELECT 'PickupCodes' AS 表名, COUNT(*) AS 软删除标记为1的行数
FROM PickupCodes
WHERE IsDeleted = 1;

SELECT 'KarmaPoints' AS 表名, COUNT(*) AS 软删除标记为1的行数
FROM KarmaPoints
WHERE IsDeleted = 1;

SELECT '========== 校验完成 ==========' AS '';

-- 提交事务（全部校验通过后提交；如需测试回滚，可改为 ROLLBACK）
COMMIT;
-- ROLLBACK;

-- ============================================================================
--  C. 回滚语句参考（可选，如需撤销001脚本的字段添加）
-- ============================================================================
-- 说明：以下语句用于撤销 001_add_soft_delete_columns.sql 脚本的变更，
--       即删除四个业务表的软删除字段和相关索引。
--       【警告】执行前请确认：
--         1. 没有任何业务代码依赖这些字段
--         2. 已备份 DeletedEntitySnapshots 表中的快照数据
--         3. 已确认需要回退到物理删除模式
--
--       如果需要完整回滚所有迁移（包括删除快照表），请直接执行
--       005_rollback_all_migrations.sql 脚本。

-- 可取消下方注释执行回滚（不建议与回填语句同时执行）
/*
-- 关闭外键检查
SET FOREIGN_KEY_CHECKS = 0;

-- ========== SharePosts 表回滚 ==========
-- 先删除索引（索引必须在字段前删除）
DROP INDEX IF EXISTS IX_SharePosts_IsDeleted_DeletedAt ON SharePosts;
-- 再删除字段（注意顺序无依赖）
ALTER TABLE SharePosts DROP COLUMN IF EXISTS IsDeleted;
ALTER TABLE SharePosts DROP COLUMN IF EXISTS DeletedAt;
ALTER TABLE SharePosts DROP COLUMN IF EXISTS DeletedBy;
ALTER TABLE SharePosts DROP COLUMN IF EXISTS DeletionReason;

-- ========== Reservations 表回滚 ==========
DROP INDEX IF EXISTS IX_Reservations_IsDeleted_DeletedAt ON Reservations;
ALTER TABLE Reservations DROP COLUMN IF EXISTS IsDeleted;
ALTER TABLE Reservations DROP COLUMN IF EXISTS DeletedAt;
ALTER TABLE Reservations DROP COLUMN IF EXISTS DeletedBy;
ALTER TABLE Reservations DROP COLUMN IF EXISTS DeletionReason;

-- ========== PickupCodes 表回滚 ==========
DROP INDEX IF EXISTS IX_PickupCodes_IsDeleted_DeletedAt ON PickupCodes;
ALTER TABLE PickupCodes DROP COLUMN IF EXISTS IsDeleted;
ALTER TABLE PickupCodes DROP COLUMN IF EXISTS DeletedAt;
ALTER TABLE PickupCodes DROP COLUMN IF EXISTS DeletedBy;
ALTER TABLE PickupCodes DROP COLUMN IF EXISTS DeletionReason;

-- ========== KarmaPoints 表回滚 ==========
DROP INDEX IF EXISTS IX_KarmaPoints_IsDeleted_DeletedAt ON KarmaPoints;
ALTER TABLE KarmaPoints DROP COLUMN IF EXISTS IsDeleted;
ALTER TABLE KarmaPoints DROP COLUMN IF EXISTS DeletedAt;
ALTER TABLE KarmaPoints DROP COLUMN IF EXISTS DeletedBy;
ALTER TABLE KarmaPoints DROP COLUMN IF EXISTS DeletionReason;

-- 恢复外键检查
SET FOREIGN_KEY_CHECKS = 1;

SELECT '四个业务表的软删除字段和索引已成功回滚' AS 回滚结果;
*/

-- 恢复外键检查
SET FOREIGN_KEY_CHECKS = 1;

-- ============================================================================
-- 脚本执行完成提示
-- ============================================================================
-- 执行成功后，请继续执行：
--   004_verify_migration.sql - 验证迁移结果
--
-- 如回填后需要验证数据完整性，可对比 A 部分的行数与现行数是否一致。
-- ============================================================================
