-- ============================================================================
-- 脚本名称: 002_create_deleted_entity_snapshots_table.sql
-- 脚本说明: 创建软删除审计快照表
-- 适用数据库: MySQL 5.7+ / MySQL 8.0+
-- 创建日期: 2026-06-22
-- 执行顺序: 第2步（共5步）
-- 依赖脚本: 001_add_soft_delete_columns.sql
--
-- 功能描述:
--   创建 DeletedEntitySnapshots 表，用于存储所有软删除实体的完整数据快照。
--   当业务实体被软删除时，系统会自动将该实体删除前的完整状态序列化为 JSON
--   并存入此表，支持后续的数据恢复、审计追溯和合规检查。
--
-- 表结构说明（与 DeletedEntitySnapshot C# 实体对应）:
--   字段名              类型            约束        说明
--   Id                  INT             PK, AUTO    快照记录唯一标识
--   EntityType          VARCHAR(100)    NOT NULL    实体类型名称（如 SharePost）
--   EntityId            INT             NOT NULL    被删除实体的原始主键ID
--   EntityDisplayName   VARCHAR(500)    NOT NULL    实体友好显示名称（便于人工识别）
--   SnapshotData        TEXT            NOT NULL    删除前的完整数据JSON快照
--   DeletedBy           INT             NOT NULL    执行删除操作的用户ID
--   DeletedAt           TIMESTAMP       NOT NULL    删除操作时间（默认当前时间）
--   DeletionReason      VARCHAR(500)    NULL        删除原因说明
--   OriginalOwnerId     INT             NULL        数据原始拥有者用户ID
--
-- 索引说明:
--   1. IX_DeletedEntitySnapshots_EntityType_EntityId
--      (EntityType, EntityId) - 按实体类型+实体ID快速查询历史快照
--   2. IX_DeletedEntitySnapshots_DeletedBy_DeletedAt
--      (DeletedBy, DeletedAt) - 按操作者+时间范围审计查询
--   3. IX_DeletedEntitySnapshots_DeletedAt
--      (DeletedAt) - 按时间范围查询回收站列表
--
-- 外键约束:
--   DeletedBy -> Users(Id) ON DELETE RESTRICT
--   说明：禁止删除有删除操作记录的用户账号，确保审计链完整
--
-- 使用说明:
--   1. 请先执行 001_add_soft_delete_columns.sql 完成第一步迁移
--   2. 在执行前请确保已对数据库进行完整备份
--   3. 使用具有 CREATE TABLE 权限的数据库账号执行
--   4. 执行命令: mysql -u用户名 -p 数据库名 < 002_create_deleted_entity_snapshots_table.sql
--   5. 执行完成后，请运行 004_verify_migration.sql 验证迁移结果
--
-- 注意事项:
--   - 本脚本使用 CREATE TABLE IF NOT EXISTS，具备幂等性
--   - 快照表数据量会随删除操作持续增长，建议定期归档
--   - SnapshotData 字段使用 TEXT 类型，单条最大支持 64KB
--   - 如需回滚本脚本，请执行 005_rollback_all_migrations.sql
-- ============================================================================

-- 设置字符集，防止中文乱码
SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS = 0;

-- ============================================================================
-- 创建 DeletedEntitySnapshots 审计快照表
-- ============================================================================

CREATE TABLE IF NOT EXISTS DeletedEntitySnapshots (
    -- ========================================================================
    -- 主键字段
    -- ========================================================================
    Id INT NOT NULL AUTO_INCREMENT
        COMMENT '快照记录唯一标识，自增主键',

    -- ========================================================================
    -- 实体标识字段
    -- ========================================================================
    EntityType VARCHAR(100) NOT NULL
        COMMENT '实体类型名称，对应C#类名（如 SharePost、Reservation 等）',

    EntityId INT NOT NULL
        COMMENT '被删除实体的原始主键ID，与EntityType组合唯一定位一条业务数据',

    EntityDisplayName VARCHAR(500) NOT NULL
        COMMENT '实体友好显示名称，用于回收站列表快速识别（如"分享帖#123 - 蛋糕一批"）',

    -- ========================================================================
    -- 数据快照字段
    -- ========================================================================
    SnapshotData TEXT NOT NULL
        COMMENT '删除前的完整数据JSON快照，包含实体所有属性的键值对，用于数据恢复',

    -- ========================================================================
    -- 删除审计字段
    -- ========================================================================
    DeletedBy INT NOT NULL
        COMMENT '执行删除操作的用户ID，关联Users表，0表示系统自动删除',

    DeletedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
        COMMENT '删除操作执行时间，默认使用数据库当前时间',

    DeletionReason VARCHAR(500) NULL
        COMMENT '删除原因说明，可为用户手动输入或系统自动生成（如级联删除说明）',

    OriginalOwnerId INT NULL
        COMMENT '数据原始拥有者用户ID，用于按原所有者筛选回收站数据',

    -- ========================================================================
    -- 约束定义
    -- ========================================================================
    PRIMARY KEY (Id),

    -- 索引1：按实体类型+实体ID查询历史快照
    INDEX IX_DeletedEntitySnapshots_EntityType_EntityId (EntityType, EntityId),

    -- 索引2：按操作者+删除时间审计查询
    INDEX IX_DeletedEntitySnapshots_DeletedBy_DeletedAt (DeletedBy, DeletedAt),

    -- 索引3：按删除时间范围查询回收站列表
    INDEX IX_DeletedEntitySnapshots_DeletedAt (DeletedAt),

    -- 外键：删除操作者关联用户表，禁止删除有操作记录的用户
    CONSTRAINT FK_DeletedEntitySnapshots_Users_DeletedBy
        FOREIGN KEY (DeletedBy) REFERENCES Users(Id)
        ON DELETE RESTRICT
        ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
    COMMENT='软删除实体审计快照表 - 存储所有被软删除实体的完整数据备份，支持数据恢复和审计追溯';

-- 恢复外键检查
SET FOREIGN_KEY_CHECKS = 1;

-- ============================================================================
-- 脚本执行完成提示
-- ============================================================================
-- 执行成功后，请继续执行：
--   1. 003_backfill_soft_delete_for_existing_data.sql - 历史数据回填
--   2. 004_verify_migration.sql - 验证迁移结果
--
-- 表创建成功后可使用以下SQL查看表结构:
--   DESC DeletedEntitySnapshots;
--   SHOW CREATE TABLE DeletedEntitySnapshots;
-- ============================================================================
