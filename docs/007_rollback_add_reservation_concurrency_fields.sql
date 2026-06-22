-- =====================================================
-- 回滚名称: 回滚添加预约并发控制字段
-- 描述: 移除 SharePosts 表的 ReservedQuantity 和 RowVersion 字段
-- 对应迁移: 007_add_reservation_concurrency_fields.sql
-- =====================================================

-- 1. 移除索引
ALTER TABLE SharePosts 
DROP INDEX IF EXISTS IX_SharePosts_Status_Quantity_Reserved;

-- 2. 移除 RowVersion 字段
ALTER TABLE SharePosts 
DROP COLUMN IF EXISTS RowVersion;

-- 3. 移除 ReservedQuantity 字段
ALTER TABLE SharePosts 
DROP COLUMN IF EXISTS ReservedQuantity;

-- =====================================================
-- 验证回滚
-- =====================================================
-- 确认字段已移除
SELECT COLUMN_NAME 
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_SCHEMA = DATABASE() 
  AND TABLE_NAME = 'SharePosts' 
  AND COLUMN_NAME IN ('ReservedQuantity', 'RowVersion');

-- 确认索引已移除
SELECT INDEX_NAME 
FROM INFORMATION_SCHEMA.STATISTICS 
WHERE TABLE_SCHEMA = DATABASE() 
  AND TABLE_NAME = 'SharePosts' 
  AND INDEX_NAME = 'IX_SharePosts_Status_Quantity_Reserved';
