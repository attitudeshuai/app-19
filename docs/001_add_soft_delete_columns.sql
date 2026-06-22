-- ============================================================================
-- 脚本名称: 001_add_soft_delete_columns.sql
-- 脚本说明: 为主干迁移脚本 - 从物理删除迁移到逻辑删除的第一步
-- 适用数据库: MySQL 5.7+ / MySQL 8.0+
-- 创建日期: 2026-06-22
-- 执行顺序: 第1步（共5步）
--
-- 功能描述:
--   为 SharePosts、Reservations、PickupCodes、KarmaPoints 四个业务表
--   添加软删除（逻辑删除）相关字段和复合索引，所有操作使用 IF NOT EXISTS
--   保护，确保脚本幂等性（可重复执行）。
--
-- 新增字段说明:
--   1. IsDeleted      TINYINT(1)    软删除标记，0=未删除，1=已删除
--   2. DeletedAt      DATETIME      软删除时间，NULL表示未删除
--   3. DeletedBy      INT           软删除操作者用户ID，NULL表示系统自动删除
--   4. DeletionReason VARCHAR(500)  删除原因说明，便于后续审计追溯
--
-- 新增索引说明:
--   为每个表创建 (IsDeleted, DeletedAt) 复合索引，用于优化软删除过滤查询
--   EF Core 全局查询过滤器会自动使用该索引提升查询性能
--
-- 使用说明:
--   1. 在执行前请确保已对数据库进行完整备份
--   2. 建议在业务低峰期执行，避免锁表现象
--   3. 使用具有 ALTER TABLE 权限的数据库账号执行
--   4. 执行命令: mysql -u用户名 -p 数据库名 < 001_add_soft_delete_columns.sql
--   5. 执行完成后，请运行 004_verify_migration.sql 验证迁移结果
--
-- 注意事项:
--   - 本脚本仅添加字段和索引，不修改现有数据
--   - 历史数据回填请执行 003_backfill_soft_delete_for_existing_data.sql
--   - 如需回滚本脚本，请执行 005_rollback_all_migrations.sql 的对应部分
-- ============================================================================

-- 设置字符集，防止中文乱码
SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS = 0;

-- ============================================================================
-- 一、为 SharePosts 表添加软删除字段
-- ============================================================================

-- 添加 IsDeleted 字段：软删除标记
ALTER TABLE SharePosts
    ADD COLUMN IF NOT EXISTS IsDeleted TINYINT(1) NOT NULL DEFAULT 0
    COMMENT '软删除标记：0=未删除，1=已删除';

-- 添加 DeletedAt 字段：软删除时间
ALTER TABLE SharePosts
    ADD COLUMN IF NOT EXISTS DeletedAt DATETIME NULL
    COMMENT '软删除时间，NULL表示未删除';

-- 添加 DeletedBy 字段：软删除操作者ID
ALTER TABLE SharePosts
    ADD COLUMN IF NOT EXISTS DeletedBy INT NULL
    COMMENT '软删除操作者用户ID，NULL表示系统自动删除';

-- 添加 DeletionReason 字段：删除原因
ALTER TABLE SharePosts
    ADD COLUMN IF NOT EXISTS DeletionReason VARCHAR(500) NULL
    COMMENT '删除原因说明，便于审计追溯';

-- 创建软删除复合索引
-- 使用存储过程判断索引是否存在，因为 MySQL 没有 CREATE INDEX IF NOT EXISTS
DROP PROCEDURE IF EXISTS create_index_shareposts;
DELIMITER //
CREATE PROCEDURE create_index_shareposts()
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.STATISTICS
        WHERE TABLE_SCHEMA = DATABASE()
          AND TABLE_NAME = 'SharePosts'
          AND INDEX_NAME = 'IX_SharePosts_IsDeleted_DeletedAt'
    ) THEN
        CREATE INDEX IX_SharePosts_IsDeleted_DeletedAt
            ON SharePosts (IsDeleted, DeletedAt);
    END IF;
END //
DELIMITER ;
CALL create_index_shareposts();
DROP PROCEDURE IF EXISTS create_index_shareposts;

-- ============================================================================
-- 二、为 Reservations 表添加软删除字段
-- ============================================================================

-- 添加 IsDeleted 字段：软删除标记
ALTER TABLE Reservations
    ADD COLUMN IF NOT EXISTS IsDeleted TINYINT(1) NOT NULL DEFAULT 0
    COMMENT '软删除标记：0=未删除，1=已删除';

-- 添加 DeletedAt 字段：软删除时间
ALTER TABLE Reservations
    ADD COLUMN IF NOT EXISTS DeletedAt DATETIME NULL
    COMMENT '软删除时间，NULL表示未删除';

-- 添加 DeletedBy 字段：软删除操作者ID
ALTER TABLE Reservations
    ADD COLUMN IF NOT EXISTS DeletedBy INT NULL
    COMMENT '软删除操作者用户ID，NULL表示系统自动删除';

-- 添加 DeletionReason 字段：删除原因
ALTER TABLE Reservations
    ADD COLUMN IF NOT EXISTS DeletionReason VARCHAR(500) NULL
    COMMENT '删除原因说明，便于审计追溯';

-- 创建软删除复合索引
DROP PROCEDURE IF EXISTS create_index_reservations;
DELIMITER //
CREATE PROCEDURE create_index_reservations()
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.STATISTICS
        WHERE TABLE_SCHEMA = DATABASE()
          AND TABLE_NAME = 'Reservations'
          AND INDEX_NAME = 'IX_Reservations_IsDeleted_DeletedAt'
    ) THEN
        CREATE INDEX IX_Reservations_IsDeleted_DeletedAt
            ON Reservations (IsDeleted, DeletedAt);
    END IF;
END //
DELIMITER ;
CALL create_index_reservations();
DROP PROCEDURE IF EXISTS create_index_reservations;

-- ============================================================================
-- 三、为 PickupCodes 表添加软删除字段
-- ============================================================================

-- 添加 IsDeleted 字段：软删除标记
ALTER TABLE PickupCodes
    ADD COLUMN IF NOT EXISTS IsDeleted TINYINT(1) NOT NULL DEFAULT 0
    COMMENT '软删除标记：0=未删除，1=已删除';

-- 添加 DeletedAt 字段：软删除时间
ALTER TABLE PickupCodes
    ADD COLUMN IF NOT EXISTS DeletedAt DATETIME NULL
    COMMENT '软删除时间，NULL表示未删除';

-- 添加 DeletedBy 字段：软删除操作者ID
ALTER TABLE PickupCodes
    ADD COLUMN IF NOT EXISTS DeletedBy INT NULL
    COMMENT '软删除操作者用户ID，NULL表示系统自动删除';

-- 添加 DeletionReason 字段：删除原因
ALTER TABLE PickupCodes
    ADD COLUMN IF NOT EXISTS DeletionReason VARCHAR(500) NULL
    COMMENT '删除原因说明，便于审计追溯';

-- 创建软删除复合索引
DROP PROCEDURE IF EXISTS create_index_pickupcodes;
DELIMITER //
CREATE PROCEDURE create_index_pickupcodes()
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.STATISTICS
        WHERE TABLE_SCHEMA = DATABASE()
          AND TABLE_NAME = 'PickupCodes'
          AND INDEX_NAME = 'IX_PickupCodes_IsDeleted_DeletedAt'
    ) THEN
        CREATE INDEX IX_PickupCodes_IsDeleted_DeletedAt
            ON PickupCodes (IsDeleted, DeletedAt);
    END IF;
END //
DELIMITER ;
CALL create_index_pickupcodes();
DROP PROCEDURE IF EXISTS create_index_pickupcodes;

-- ============================================================================
-- 四、为 KarmaPoints 表添加软删除字段
-- ============================================================================

-- 添加 IsDeleted 字段：软删除标记
ALTER TABLE KarmaPoints
    ADD COLUMN IF NOT EXISTS IsDeleted TINYINT(1) NOT NULL DEFAULT 0
    COMMENT '软删除标记：0=未删除，1=已删除';

-- 添加 DeletedAt 字段：软删除时间
ALTER TABLE KarmaPoints
    ADD COLUMN IF NOT EXISTS DeletedAt DATETIME NULL
    COMMENT '软删除时间，NULL表示未删除';

-- 添加 DeletedBy 字段：软删除操作者ID
ALTER TABLE KarmaPoints
    ADD COLUMN IF NOT EXISTS DeletedBy INT NULL
    COMMENT '软删除操作者用户ID，NULL表示系统自动删除';

-- 添加 DeletionReason 字段：删除原因
ALTER TABLE KarmaPoints
    ADD COLUMN IF NOT EXISTS DeletionReason VARCHAR(500) NULL
    COMMENT '删除原因说明，便于审计追溯';

-- 创建软删除复合索引
DROP PROCEDURE IF EXISTS create_index_karmapoints;
DELIMITER //
CREATE PROCEDURE create_index_karmapoints()
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.STATISTICS
        WHERE TABLE_SCHEMA = DATABASE()
          AND TABLE_NAME = 'KarmaPoints'
          AND INDEX_NAME = 'IX_KarmaPoints_IsDeleted_DeletedAt'
    ) THEN
        CREATE INDEX IX_KarmaPoints_IsDeleted_DeletedAt
            ON KarmaPoints (IsDeleted, DeletedAt);
    END IF;
END //
DELIMITER ;
CALL create_index_karmapoints();
DROP PROCEDURE IF EXISTS create_index_karmapoints;

-- 恢复外键检查
SET FOREIGN_KEY_CHECKS = 1;

-- ============================================================================
-- 脚本执行完成提示
-- ============================================================================
-- 执行成功后，请继续执行：
--   1. 002_create_deleted_entity_snapshots_table.sql - 创建审计快照表
--   2. 003_backfill_soft_delete_for_existing_data.sql - 历史数据回填
--   3. 004_verify_migration.sql - 验证迁移结果
-- ============================================================================
