-- ============================================================================
-- 脚本名称: 004_verify_migration.sql
-- 脚本说明: 迁移验证脚本 - 全面检查软删除迁移是否成功完成
-- 适用数据库: MySQL 5.7+ / MySQL 8.0+
-- 创建日期: 2026-06-22
-- 执行顺序: 第4步（共5步）
-- 依赖脚本: 001, 002, 003 均已执行完成
--
-- 功能描述:
--   本脚本对整个软删除迁移进行全面的自动化验证，包括：
--   1. 字段存在性检查：验证四个业务表的4个软删除字段均已正确添加
--   2. 索引完整性检查：验证4个表的复合索引 + 快照表的3个索引均已创建
--   3. 表存在性检查：验证 DeletedEntitySnapshots 审计快照表已创建
--   4. 数据正确性检查：统计各表软删除标记为1的行数（迁移后预期应为0）
--   5. 外键约束检查：验证快照表的 DeletedBy 外键已正确配置
--
-- 验证结果判定标准:
--   - 所有字段检查记录的「存在状态」列均应为 ✓ 存在
--   - 所有索引检查记录的「存在状态」列均应为 ✓ 存在
--   - 快照表检查的「存在状态」应为 ✓ 存在
--   - 所有表的「软删除标记=1的行数」均应为 0
--   - 外键约束的「存在状态」应为 ✓ 存在
--
-- 使用说明:
--   1. 请先执行完 001~003 三个迁移脚本
--   2. 使用任何数据库账号均可执行（仅需 SELECT 权限）
--   3. 执行命令: mysql -u用户名 -p 数据库名 < 004_verify_migration.sql
--   4. 逐条检查输出结果，对照上述判定标准
--   5. 如所有检查均通过，说明迁移成功；如有任何检查未通过，
--      请检查对应迁移脚本是否正确执行，或参考 005 脚本回滚后重试
--
-- 注意事项:
--   - 本脚本为纯查询脚本，不会修改任何数据或结构，可放心重复执行
--   - 如某表无数据，「软删除标记=1的行数」自然为 0 属正常
--   - INFORMATION_SCHEMA 查询可能受 MySQL query cache 影响，
--     如刚执行完迁移建议等待数秒或 FLUSH TABLES 后再验证
-- ============================================================================

-- 设置字符集，防止中文乱码
SET NAMES utf8mb4;

-- ============================================================================
-- 一、四个业务表的软删除字段存在性检查
-- ============================================================================

SELECT '================================================================' AS '';
SELECT '  第一部分：四个业务表软删除字段存在性检查' AS '';
SELECT '================================================================' AS '';

-- ----------------------------------------------------------------------------
-- 1.1 SharePosts 表字段检查
-- ----------------------------------------------------------------------------
SELECT '---- SharePosts 表字段检查 ----' AS '';

SELECT
    'IsDeleted' AS 字段名,
    CASE WHEN COUNT(*) = 1 THEN '✓ 存在' ELSE '✗ 不存在' END AS 存在状态,
    COLUMN_TYPE AS 字段类型,
    IS_NULLABLE AS 是否可空,
    COLUMN_DEFAULT AS 默认值
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME = 'SharePosts'
  AND COLUMN_NAME = 'IsDeleted'
UNION ALL
SELECT
    'DeletedAt' AS 字段名,
    CASE WHEN COUNT(*) = 1 THEN '✓ 存在' ELSE '✗ 不存在' END AS 存在状态,
    COLUMN_TYPE AS 字段类型,
    IS_NULLABLE AS 是否可空,
    COLUMN_DEFAULT AS 默认值
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME = 'SharePosts'
  AND COLUMN_NAME = 'DeletedAt'
UNION ALL
SELECT
    'DeletedBy' AS 字段名,
    CASE WHEN COUNT(*) = 1 THEN '✓ 存在' ELSE '✗ 不存在' END AS 存在状态,
    COLUMN_TYPE AS 字段类型,
    IS_NULLABLE AS 是否可空,
    COLUMN_DEFAULT AS 默认值
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME = 'SharePosts'
  AND COLUMN_NAME = 'DeletedBy'
UNION ALL
SELECT
    'DeletionReason' AS 字段名,
    CASE WHEN COUNT(*) = 1 THEN '✓ 存在' ELSE '✗ 不存在' END AS 存在状态,
    COLUMN_TYPE AS 字段类型,
    IS_NULLABLE AS 是否可空,
    COLUMN_DEFAULT AS 默认值
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME = 'SharePosts'
  AND COLUMN_NAME = 'DeletionReason';

-- ----------------------------------------------------------------------------
-- 1.2 Reservations 表字段检查
-- ----------------------------------------------------------------------------
SELECT '---- Reservations 表字段检查 ----' AS '';

SELECT
    'IsDeleted' AS 字段名,
    CASE WHEN COUNT(*) = 1 THEN '✓ 存在' ELSE '✗ 不存在' END AS 存在状态,
    COLUMN_TYPE AS 字段类型,
    IS_NULLABLE AS 是否可空,
    COLUMN_DEFAULT AS 默认值
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME = 'Reservations'
  AND COLUMN_NAME = 'IsDeleted'
UNION ALL
SELECT
    'DeletedAt' AS 字段名,
    CASE WHEN COUNT(*) = 1 THEN '✓ 存在' ELSE '✗ 不存在' END AS 存在状态,
    COLUMN_TYPE AS 字段类型,
    IS_NULLABLE AS 是否可空,
    COLUMN_DEFAULT AS 默认值
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME = 'Reservations'
  AND COLUMN_NAME = 'DeletedAt'
UNION ALL
SELECT
    'DeletedBy' AS 字段名,
    CASE WHEN COUNT(*) = 1 THEN '✓ 存在' ELSE '✗ 不存在' END AS 存在状态,
    COLUMN_TYPE AS 字段类型,
    IS_NULLABLE AS 是否可空,
    COLUMN_DEFAULT AS 默认值
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME = 'Reservations'
  AND COLUMN_NAME = 'DeletedBy'
UNION ALL
SELECT
    'DeletionReason' AS 字段名,
    CASE WHEN COUNT(*) = 1 THEN '✓ 存在' ELSE '✗ 不存在' END AS 存在状态,
    COLUMN_TYPE AS 字段类型,
    IS_NULLABLE AS 是否可空,
    COLUMN_DEFAULT AS 默认值
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME = 'Reservations'
  AND COLUMN_NAME = 'DeletionReason';

-- ----------------------------------------------------------------------------
-- 1.3 PickupCodes 表字段检查
-- ----------------------------------------------------------------------------
SELECT '---- PickupCodes 表字段检查 ----' AS '';

SELECT
    'IsDeleted' AS 字段名,
    CASE WHEN COUNT(*) = 1 THEN '✓ 存在' ELSE '✗ 不存在' END AS 存在状态,
    COLUMN_TYPE AS 字段类型,
    IS_NULLABLE AS 是否可空,
    COLUMN_DEFAULT AS 默认值
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME = 'PickupCodes'
  AND COLUMN_NAME = 'IsDeleted'
UNION ALL
SELECT
    'DeletedAt' AS 字段名,
    CASE WHEN COUNT(*) = 1 THEN '✓ 存在' ELSE '✗ 不存在' END AS 存在状态,
    COLUMN_TYPE AS 字段类型,
    IS_NULLABLE AS 是否可空,
    COLUMN_DEFAULT AS 默认值
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME = 'PickupCodes'
  AND COLUMN_NAME = 'DeletedAt'
UNION ALL
SELECT
    'DeletedBy' AS 字段名,
    CASE WHEN COUNT(*) = 1 THEN '✓ 存在' ELSE '✗ 不存在' END AS 存在状态,
    COLUMN_TYPE AS 字段类型,
    IS_NULLABLE AS 是否可空,
    COLUMN_DEFAULT AS 默认值
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME = 'PickupCodes'
  AND COLUMN_NAME = 'DeletedBy'
UNION ALL
SELECT
    'DeletionReason' AS 字段名,
    CASE WHEN COUNT(*) = 1 THEN '✓ 存在' ELSE '✗ 不存在' END AS 存在状态,
    COLUMN_TYPE AS 字段类型,
    IS_NULLABLE AS 是否可空,
    COLUMN_DEFAULT AS 默认值
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME = 'PickupCodes'
  AND COLUMN_NAME = 'DeletionReason';

-- ----------------------------------------------------------------------------
-- 1.4 KarmaPoints 表字段检查
-- ----------------------------------------------------------------------------
SELECT '---- KarmaPoints 表字段检查 ----' AS '';

SELECT
    'IsDeleted' AS 字段名,
    CASE WHEN COUNT(*) = 1 THEN '✓ 存在' ELSE '✗ 不存在' END AS 存在状态,
    COLUMN_TYPE AS 字段类型,
    IS_NULLABLE AS 是否可空,
    COLUMN_DEFAULT AS 默认值
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME = 'KarmaPoints'
  AND COLUMN_NAME = 'IsDeleted'
UNION ALL
SELECT
    'DeletedAt' AS 字段名,
    CASE WHEN COUNT(*) = 1 THEN '✓ 存在' ELSE '✗ 不存在' END AS 存在状态,
    COLUMN_TYPE AS 字段类型,
    IS_NULLABLE AS 是否可空,
    COLUMN_DEFAULT AS 默认值
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME = 'KarmaPoints'
  AND COLUMN_NAME = 'DeletedAt'
UNION ALL
SELECT
    'DeletedBy' AS 字段名,
    CASE WHEN COUNT(*) = 1 THEN '✓ 存在' ELSE '✗ 不存在' END AS 存在状态,
    COLUMN_TYPE AS 字段类型,
    IS_NULLABLE AS 是否可空,
    COLUMN_DEFAULT AS 默认值
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME = 'KarmaPoints'
  AND COLUMN_NAME = 'DeletedBy'
UNION ALL
SELECT
    'DeletionReason' AS 字段名,
    CASE WHEN COUNT(*) = 1 THEN '✓ 存在' ELSE '✗ 不存在' END AS 存在状态,
    COLUMN_TYPE AS 字段类型,
    IS_NULLABLE AS 是否可空,
    COLUMN_DEFAULT AS 默认值
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME = 'KarmaPoints'
  AND COLUMN_NAME = 'DeletionReason';

-- ============================================================================
-- 二、索引存在性检查（4个业务表 + 快照表共7个索引）
-- ============================================================================

SELECT '================================================================' AS '';
SELECT '  第二部分：索引存在性检查（共7个索引）' AS '';
SELECT '================================================================' AS '';

SELECT
    表名,
    索引名,
    索引字段,
    CASE WHEN 存在标记 = 1 THEN '✓ 存在' ELSE '✗ 不存在' END AS 存在状态
FROM (
    SELECT
        'SharePosts' AS 表名,
        'IX_SharePosts_IsDeleted_DeletedAt' AS 索引名,
        'IsDeleted, DeletedAt' AS 索引字段,
        COUNT(*) AS 存在标记
    FROM INFORMATION_SCHEMA.STATISTICS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'SharePosts'
      AND INDEX_NAME = 'IX_SharePosts_IsDeleted_DeletedAt'

    UNION ALL

    SELECT
        'Reservations' AS 表名,
        'IX_Reservations_IsDeleted_DeletedAt' AS 索引名,
        'IsDeleted, DeletedAt' AS 索引字段,
        COUNT(*) AS 存在标记
    FROM INFORMATION_SCHEMA.STATISTICS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'Reservations'
      AND INDEX_NAME = 'IX_Reservations_IsDeleted_DeletedAt'

    UNION ALL

    SELECT
        'PickupCodes' AS 表名,
        'IX_PickupCodes_IsDeleted_DeletedAt' AS 索引名,
        'IsDeleted, DeletedAt' AS 索引字段,
        COUNT(*) AS 存在标记
    FROM INFORMATION_SCHEMA.STATISTICS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'PickupCodes'
      AND INDEX_NAME = 'IX_PickupCodes_IsDeleted_DeletedAt'

    UNION ALL

    SELECT
        'KarmaPoints' AS 表名,
        'IX_KarmaPoints_IsDeleted_DeletedAt' AS 索引名,
        'IsDeleted, DeletedAt' AS 索引字段,
        COUNT(*) AS 存在标记
    FROM INFORMATION_SCHEMA.STATISTICS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'KarmaPoints'
      AND INDEX_NAME = 'IX_KarmaPoints_IsDeleted_DeletedAt'

    UNION ALL

    SELECT
        'DeletedEntitySnapshots' AS 表名,
        'IX_DeletedEntitySnapshots_EntityType_EntityId' AS 索引名,
        'EntityType, EntityId' AS 索引字段,
        COUNT(*) AS 存在标记
    FROM INFORMATION_SCHEMA.STATISTICS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'DeletedEntitySnapshots'
      AND INDEX_NAME = 'IX_DeletedEntitySnapshots_EntityType_EntityId'

    UNION ALL

    SELECT
        'DeletedEntitySnapshots' AS 表名,
        'IX_DeletedEntitySnapshots_DeletedBy_DeletedAt' AS 索引名,
        'DeletedBy, DeletedAt' AS 索引字段,
        COUNT(*) AS 存在标记
    FROM INFORMATION_SCHEMA.STATISTICS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'DeletedEntitySnapshots'
      AND INDEX_NAME = 'IX_DeletedEntitySnapshots_DeletedBy_DeletedAt'

    UNION ALL

    SELECT
        'DeletedEntitySnapshots' AS 表名,
        'IX_DeletedEntitySnapshots_DeletedAt' AS 索引名,
        'DeletedAt' AS 索引字段,
        COUNT(*) AS 存在标记
    FROM INFORMATION_SCHEMA.STATISTICS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'DeletedEntitySnapshots'
      AND INDEX_NAME = 'IX_DeletedEntitySnapshots_DeletedAt'
) AS idx_check;

-- ============================================================================
-- 三、DeletedEntitySnapshots 表存在性及结构检查
-- ============================================================================

SELECT '================================================================' AS '';
SELECT '  第三部分：DeletedEntitySnapshots 快照表检查' AS '';
SELECT '================================================================' AS '';

SELECT
    CASE WHEN COUNT(*) = 1 THEN '✓ 存在' ELSE '✗ 不存在' END AS 表存在状态,
    TABLE_NAME AS 表名,
    ENGINE AS 存储引擎,
    TABLE_COLLATION AS 字符集排序规则,
    TABLE_COMMENT AS 表注释
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME = 'DeletedEntitySnapshots';

-- 检查快照表字段结构
SELECT '---- 快照表字段详情 ----' AS '';
SELECT
    ORDINAL_POSITION AS 字段序号,
    COLUMN_NAME AS 字段名,
    COLUMN_TYPE AS 字段类型,
    IS_NULLABLE AS 是否可空,
    COLUMN_KEY AS 键类型,
    COLUMN_DEFAULT AS 默认值,
    COLUMN_COMMENT AS 字段注释
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME = 'DeletedEntitySnapshots'
ORDER BY ORDINAL_POSITION;

-- ============================================================================
-- 四、外键约束检查（快照表 -> Users）
-- ============================================================================

SELECT '================================================================' AS '';
SELECT '  第四部分：外键约束检查' AS '';
SELECT '================================================================' AS '';

SELECT
    CASE WHEN COUNT(*) = 1 THEN '✓ 存在' ELSE '✗ 不存在' END AS 外键存在状态,
    CONSTRAINT_NAME AS 外键名,
    'DeletedEntitySnapshots.DeletedBy -> Users.Id' AS 约束关系,
    UPDATE_RULE AS 更新规则,
    DELETE_RULE AS 删除规则
FROM INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS
WHERE CONSTRAINT_SCHEMA = DATABASE()
  AND TABLE_NAME = 'DeletedEntitySnapshots'
  AND CONSTRAINT_NAME = 'FK_DeletedEntitySnapshots_Users_DeletedBy';

-- ============================================================================
-- 五、数据正确性检查：各表软删除标记为1的行数
-- ============================================================================

SELECT '================================================================' AS '';
SELECT '  第五部分：数据正确性检查（迁移后软删除行数预期为0）' AS '';
SELECT '================================================================' AS '';

SELECT 'SharePosts' AS 表名,
       COUNT(*) AS 总行数,
       SUM(CASE WHEN IsDeleted = 1 THEN 1 ELSE 0 END) AS 软删除标记_1的行数,
       CASE
           WHEN SUM(CASE WHEN IsDeleted = 1 THEN 1 ELSE 0 END) = 0 THEN '✓ 通过'
           ELSE '✗ 异常（迁移后不应有已删除数据）'
       END AS 检查结果
FROM SharePosts

UNION ALL

SELECT 'Reservations' AS 表名,
       COUNT(*) AS 总行数,
       SUM(CASE WHEN IsDeleted = 1 THEN 1 ELSE 0 END) AS 软删除标记_1的行数,
       CASE
           WHEN SUM(CASE WHEN IsDeleted = 1 THEN 1 ELSE 0 END) = 0 THEN '✓ 通过'
           ELSE '✗ 异常（迁移后不应有已删除数据）'
       END AS 检查结果
FROM Reservations

UNION ALL

SELECT 'PickupCodes' AS 表名,
       COUNT(*) AS 总行数,
       SUM(CASE WHEN IsDeleted = 1 THEN 1 ELSE 0 END) AS 软删除标记_1的行数,
       CASE
           WHEN SUM(CASE WHEN IsDeleted = 1 THEN 1 ELSE 0 END) = 0 THEN '✓ 通过'
           ELSE '✗ 异常（迁移后不应有已删除数据）'
       END AS 检查结果
FROM PickupCodes

UNION ALL

SELECT 'KarmaPoints' AS 表名,
       COUNT(*) AS 总行数,
       SUM(CASE WHEN IsDeleted = 1 THEN 1 ELSE 0 END) AS 软删除标记_1的行数,
       CASE
           WHEN SUM(CASE WHEN IsDeleted = 1 THEN 1 ELSE 0 END) = 0 THEN '✓ 通过'
           ELSE '✗ 异常（迁移后不应有已删除数据）'
       END AS 检查结果
FROM KarmaPoints;

-- ============================================================================
-- 六、迁移验证总结
-- ============================================================================

SELECT '================================================================' AS '';
SELECT '  第六部分：迁移验证总结' AS '';
SELECT '================================================================' AS '';

SELECT '如果以上所有检查项均显示「✓ 通过/存在」，说明迁移验证成功！' AS 验证结论
UNION ALL
SELECT '如存在「✗ 异常/不存在」项，请检查对应迁移脚本是否正确执行，' AS 处理建议
UNION ALL
SELECT '或执行 005_rollback_all_migrations.sql 回滚后重新执行迁移。' AS 处理建议;

-- ============================================================================
-- 脚本执行完成
-- ============================================================================
