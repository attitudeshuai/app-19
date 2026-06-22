-- =====================================================
-- 迁移名称: 添加预约并发控制字段
-- 描述: 为 SharePosts 表添加已预约数量和行版本号字段，
--       支持高并发场景下的库存扣减和乐观并发控制
-- 迁移版本: 007
-- 应用日期: 2026-06-22
-- =====================================================

-- 1. 添加 ReservedQuantity 字段
ALTER TABLE SharePosts 
ADD COLUMN ReservedQuantity INT NOT NULL DEFAULT 0 
COMMENT '已预约数量' 
AFTER Quantity;

-- 2. 添加 RowVersion 字段用于乐观并发控制
ALTER TABLE SharePosts 
ADD COLUMN RowVersion TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP 
ON UPDATE CURRENT_TIMESTAMP 
COMMENT '行版本号，用于乐观并发控制' 
AFTER ReservedQuantity;

-- 3. 添加复合索引用于优化库存查询
ALTER TABLE SharePosts 
ADD INDEX IX_SharePosts_Status_Quantity_Reserved (Status, Quantity, ReservedQuantity);

-- 4. 数据回填：计算现有分享帖的已预约数量
UPDATE SharePosts sp
SET sp.ReservedQuantity = (
    SELECT COUNT(*) 
    FROM Reservations r 
    WHERE r.PostId = sp.Id 
      AND r.IsDeleted = FALSE 
      AND r.Status IN ('Pending', 'Confirmed', 'Completed')
);

-- =====================================================
-- 验证迁移
-- =====================================================
-- 检查表结构
SELECT 
    COLUMN_NAME, 
    DATA_TYPE, 
    IS_NULLABLE, 
    COLUMN_DEFAULT,
    COLUMN_COMMENT
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_SCHEMA = DATABASE() 
  AND TABLE_NAME = 'SharePosts' 
  AND COLUMN_NAME IN ('ReservedQuantity', 'RowVersion')
ORDER BY ORDINAL_POSITION;

-- 检查索引
SELECT 
    INDEX_NAME, 
    COLUMN_NAME, 
    SEQ_IN_INDEX
FROM INFORMATION_SCHEMA.STATISTICS 
WHERE TABLE_SCHEMA = DATABASE() 
  AND TABLE_NAME = 'SharePosts' 
  AND INDEX_NAME = 'IX_SharePosts_Status_Quantity_Reserved';

-- 检查数据回填结果（应该没有 ReservedQuantity > Quantity 的情况）
SELECT 
    Id, 
    Title, 
    Quantity, 
    ReservedQuantity,
    CASE 
        WHEN ReservedQuantity > Quantity THEN 'WARNING: 超卖'
        ELSE 'OK'
    END AS Status
FROM SharePosts
WHERE ReservedQuantity > Quantity;
