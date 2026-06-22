using System.Linq.Expressions;
using System.Text.Json;
using LeftoverShare.API.Entities;
using LeftoverShare.API.Entities.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace LeftoverShare.API.Data;

// 数据库上下文，使用 MySQL，配置实体映射
public class AppDbContext : DbContext
{
    private readonly HashSet<object> _hardDeleteEntities = new();

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    internal void MarkForHardDelete(object entity)
    {
        _hardDeleteEntities.Add(entity);
    }

    // 用户表
    public DbSet<User> Users => Set<User>();
    // 分享帖子表
    public DbSet<SharePost> SharePosts => Set<SharePost>();
    // 预订表
    public DbSet<Reservation> Reservations => Set<Reservation>();
    // 取餐码表
    public DbSet<PickupCode> PickupCodes => Set<PickupCode>();
    // 积分表
    public DbSet<KarmaPoint> KarmaPoints => Set<KarmaPoint>();
    // 站内通知表
    public DbSet<Notification> Notifications => Set<Notification>();
    // 定时任务日志表
    public DbSet<ScheduledTaskLog> ScheduledTaskLogs => Set<ScheduledTaskLog>();
    // 软删除审计快照表
    public DbSet<DeletedEntitySnapshot> DeletedEntitySnapshots => Set<DeletedEntitySnapshot>();
    // 食物分类表
    public DbSet<FoodCategory> FoodCategories => Set<FoodCategory>();
    // 过敏原标签表
    public DbSet<AllergenTag> AllergenTags => Set<AllergenTag>();
    // 帖子标签表
    public DbSet<PostTag> PostTags => Set<PostTag>();
    // 分享帖-过敏原标签关联表
    public DbSet<SharePostAllergenTag> SharePostAllergenTags => Set<SharePostAllergenTag>();
    // 分享帖-帖子标签关联表
    public DbSet<SharePostPostTag> SharePostPostTags => Set<SharePostPostTag>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<PublisherReputation> PublisherReputations => Set<PublisherReputation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ===== 用户实体配置 =====
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);

            // 用户名唯一索引
            entity.HasIndex(u => u.Username)
                  .IsUnique()
                  .HasDatabaseName("IX_Users_Username");

            // 邮箱唯一索引
            entity.HasIndex(u => u.Email)
                  .IsUnique()
                  .HasDatabaseName("IX_Users_Email");

            entity.Property(u => u.Username)
                  .IsRequired()
                  .HasMaxLength(50);

            entity.Property(u => u.Email)
                  .IsRequired()
                  .HasMaxLength(100);

            entity.Property(u => u.PasswordHash)
                  .IsRequired()
                  .HasMaxLength(255);

            entity.Property(u => u.Avatar)
                  .HasMaxLength(200);

            entity.Property(u => u.Role)
                  .IsRequired()
                  .HasConversion<string>()
                  .HasMaxLength(20)
                  .HasDefaultValue(Entities.Enums.UserRole.User);

            // 使用 MySQL 的 NOW() 函数作为默认值
            entity.Property(u => u.CreatedAt)
                  .HasColumnType("timestamp").HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(u => u.UpdatedAt)
                  .HasColumnType("timestamp").HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(u => u.ReputationScore)
                  .HasColumnType("decimal(5,2)")
                  .HasDefaultValue(50m);

            entity.Property(u => u.ReceivedReviewCount)
                  .HasDefaultValue(0);

            // 用户与分享帖子：一对多
            entity.HasMany(u => u.SharePosts)
                  .WithOne(sp => sp.Poster)
                  .HasForeignKey(sp => sp.PosterId)
                  .OnDelete(DeleteBehavior.Restrict);

            // 用户与预订：一对多（作为领取者）
            entity.HasMany(u => u.Reservations)
                  .WithOne(r => r.Claimer)
                  .HasForeignKey(r => r.ClaimerId)
                  .OnDelete(DeleteBehavior.Restrict);

            // 用户与积分：一对多
            entity.HasMany(u => u.KarmaPoints)
                  .WithOne(kp => kp.User)
                  .HasForeignKey(kp => kp.UserId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ===== 分享帖子实体配置 =====
        modelBuilder.Entity<SharePost>(entity =>
        {
            entity.HasKey(sp => sp.Id);

            // 复合索引：发布者+状态+创建时间
            entity.HasIndex(sp => new { sp.PosterId, sp.Status, sp.CreatedAt })
                  .HasDatabaseName("IX_SharePosts_PosterId_Status_CreatedAt");

            // 位置索引
            entity.HasIndex(sp => new { sp.Latitude, sp.Longitude })
                  .HasDatabaseName("IX_SharePosts_Location");

            // 食物分类索引
            entity.HasIndex(sp => sp.FoodCategoryId)
                  .HasDatabaseName("IX_SharePosts_FoodCategoryId");

            // 软删除索引
            entity.HasIndex(sp => new { sp.IsDeleted, sp.DeletedAt })
                  .HasDatabaseName("IX_SharePosts_IsDeleted_DeletedAt");

            entity.Property(sp => sp.Title)
                  .IsRequired()
                  .HasMaxLength(100);

            entity.Property(sp => sp.Description)
                  .HasMaxLength(1000);

            entity.Property(sp => sp.FoodType)
                  .IsRequired()
                  .HasMaxLength(50);

            entity.Property(sp => sp.Quantity)
                  .IsRequired()
                  .HasDefaultValue(1);

            entity.Property(sp => sp.PickupAddress)
                  .IsRequired()
                  .HasMaxLength(200);

            entity.Property(sp => sp.Status)
                  .IsRequired()
                  .HasConversion<string>()
                  .HasMaxLength(20)
                  .HasDefaultValue(SharePostStatus.Available);

            entity.Property(sp => sp.Latitude)
                  .HasColumnType("decimal(9,6)");

            entity.Property(sp => sp.Longitude)
                  .HasColumnType("decimal(9,6)");

            entity.Property(sp => sp.Photos)
                  .HasMaxLength(2000);

            entity.Property(sp => sp.ExpirationReason)
                  .HasConversion<string>()
                  .HasMaxLength(30);

            entity.Property(sp => sp.IsDeleted)
                  .HasDefaultValue(false);

            entity.Property(sp => sp.DeletionReason)
                  .HasMaxLength(500);

            // 使用 MySQL 的 NOW() 函数作为默认值
            entity.Property(sp => sp.CreatedAt)
                  .HasColumnType("timestamp").HasDefaultValueSql("CURRENT_TIMESTAMP");

            // 帖子与预订：一对多
            entity.HasMany(sp => sp.Reservations)
                  .WithOne(r => r.Post)
                  .HasForeignKey(r => r.PostId)
                  .OnDelete(DeleteBehavior.Cascade);

            // 帖子与食物分类：多对一
            entity.HasOne(sp => sp.FoodCategory)
                  .WithMany(fc => fc.SharePosts)
                  .HasForeignKey(sp => sp.FoodCategoryId)
                  .OnDelete(DeleteBehavior.SetNull);

            // 全局查询过滤器：自动过滤已软删除的记录
            entity.HasQueryFilter(sp => !sp.IsDeleted);
        });

        // ===== 预订实体配置 =====
        modelBuilder.Entity<Reservation>(entity =>
        {
            entity.HasKey(r => r.Id);

            // 复合唯一索引：帖子+领取者
            entity.HasIndex(r => new { r.PostId, r.ClaimerId })
                  .IsUnique()
                  .HasDatabaseName("IX_Reservations_PostId_ClaimerId");

            // 复合索引：领取者+状态+预订时间
            entity.HasIndex(r => new { r.ClaimerId, r.Status, r.ReservedAt })
                  .HasDatabaseName("IX_Reservations_ClaimerId_Status_ReservedAt");

            // 软删除索引
            entity.HasIndex(r => new { r.IsDeleted, r.DeletedAt })
                  .HasDatabaseName("IX_Reservations_IsDeleted_DeletedAt");

            entity.Property(r => r.Status)
                  .IsRequired()
                  .HasConversion<string>()
                  .HasMaxLength(20)
                  .HasDefaultValue(ReservationStatus.Pending);

            entity.Property(r => r.PickupCode)
                  .IsRequired()
                  .HasMaxLength(10);

            entity.Property(r => r.Note)
                  .HasMaxLength(500);

            entity.Property(r => r.IsDeleted)
                  .HasDefaultValue(false);

            entity.Property(r => r.DeletionReason)
                  .HasMaxLength(500);

            // 使用 MySQL 的 NOW() 函数作为默认值
            entity.Property(r => r.ReservedAt)
                  .HasColumnType("timestamp").HasDefaultValueSql("CURRENT_TIMESTAMP");

            // 预订与取餐码：一对一
            entity.HasOne(r => r.PickupCodeNavigation)
                  .WithOne(pc => pc.Reservation)
                  .HasForeignKey<PickupCode>(pc => pc.ReservationId)
                  .OnDelete(DeleteBehavior.Cascade);

            // 全局查询过滤器：自动过滤已软删除的记录
            entity.HasQueryFilter(r => !r.IsDeleted);
        });

        // ===== 取餐码实体配置 =====
        modelBuilder.Entity<PickupCode>(entity =>
        {
            entity.HasKey(pc => pc.Id);

            // 取餐码唯一索引
            entity.HasIndex(pc => pc.Code)
                  .IsUnique()
                  .HasDatabaseName("IX_PickupCodes_Code");

            // 复合索引：预订+使用状态+过期时间
            entity.HasIndex(pc => new { pc.ReservationId, pc.IsUsed, pc.ExpiresAt })
                  .HasDatabaseName("IX_PickupCodes_ReservationId_IsUsed_ExpiresAt");

            // 软删除索引
            entity.HasIndex(pc => new { pc.IsDeleted, pc.DeletedAt })
                  .HasDatabaseName("IX_PickupCodes_IsDeleted_DeletedAt");

            entity.Property(pc => pc.Code)
                  .IsRequired()
                  .HasMaxLength(10);

            entity.Property(pc => pc.ExpiresAt)
                  .IsRequired();

            entity.Property(pc => pc.IsUsed)
                  .HasDefaultValue(false);

            entity.Property(pc => pc.IsExpired)
                  .HasDefaultValue(false);

            entity.Property(pc => pc.ExpirationReason)
                  .HasConversion<string>()
                  .HasMaxLength(30);

            entity.Property(pc => pc.IsDeleted)
                  .HasDefaultValue(false);

            entity.Property(pc => pc.DeletionReason)
                  .HasMaxLength(500);

            // 全局查询过滤器：自动过滤已软删除的记录
            entity.HasQueryFilter(pc => !pc.IsDeleted);
        });

        // ===== 积分实体配置 =====
        modelBuilder.Entity<KarmaPoint>(entity =>
        {
            entity.HasKey(kp => kp.Id);

            // 复合索引：用户+创建时间
            entity.HasIndex(kp => new { kp.UserId, kp.CreatedAt })
                  .HasDatabaseName("IX_KarmaPoints_UserId_CreatedAt");

            // 软删除索引
            entity.HasIndex(kp => new { kp.IsDeleted, kp.DeletedAt })
                  .HasDatabaseName("IX_KarmaPoints_IsDeleted_DeletedAt");

            entity.Property(kp => kp.Points)
                  .IsRequired();

            entity.Property(kp => kp.Reason)
                  .IsRequired()
                  .HasMaxLength(200);

            entity.Property(kp => kp.IsDeleted)
                  .HasDefaultValue(false);

            entity.Property(kp => kp.DeletionReason)
                  .HasMaxLength(500);

            // 使用 MySQL 的 NOW() 函数作为默认值
            entity.Property(kp => kp.CreatedAt)
                  .HasColumnType("timestamp").HasDefaultValueSql("CURRENT_TIMESTAMP");

            // 全局查询过滤器：自动过滤已软删除的记录
            entity.HasQueryFilter(kp => !kp.IsDeleted);
        });

        // ===== 站内通知实体配置 =====
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(n => n.Id);

            entity.HasIndex(n => new { n.UserId, n.IsRead, n.CreatedAt })
                  .HasDatabaseName("IX_Notifications_UserId_IsRead_CreatedAt");

            entity.HasIndex(n => new { n.Type, n.CreatedAt })
                  .HasDatabaseName("IX_Notifications_Type_CreatedAt");

            entity.Property(n => n.Title)
                  .IsRequired()
                  .HasMaxLength(200);

            entity.Property(n => n.Content)
                  .IsRequired()
                  .HasMaxLength(2000);

            entity.Property(n => n.Type)
                  .IsRequired()
                  .HasConversion<string>()
                  .HasMaxLength(30);

            entity.Property(n => n.IsRead)
                  .HasDefaultValue(false);

            entity.Property(n => n.CreatedAt)
                  .HasColumnType("timestamp").HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(n => n.User)
                  .WithMany()
                  .HasForeignKey(n => n.UserId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(n => n.SharePost)
                  .WithMany()
                  .HasForeignKey(n => n.SharePostId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(n => n.Reservation)
                  .WithMany()
                  .HasForeignKey(n => n.ReservationId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // ===== 定时任务执行日志实体配置 =====
        modelBuilder.Entity<ScheduledTaskLog>(entity =>
        {
            entity.HasKey(stl => stl.Id);

            entity.HasIndex(stl => new { stl.TaskName, stl.StartedAt })
                  .HasDatabaseName("IX_ScheduledTaskLogs_TaskName_StartedAt");

            entity.HasIndex(stl => new { stl.Status, stl.StartedAt })
                  .HasDatabaseName("IX_ScheduledTaskLogs_Status_StartedAt");

            entity.Property(stl => stl.TaskName)
                  .IsRequired()
                  .HasMaxLength(100);

            entity.Property(stl => stl.Status)
                  .IsRequired()
                  .HasConversion<string>()
                  .HasMaxLength(20);

            entity.Property(stl => stl.ExpiredSharePostsCount)
                  .HasDefaultValue(0);

            entity.Property(stl => stl.ExpiredPickupCodesCount)
                  .HasDefaultValue(0);

            entity.Property(stl => stl.NotificationsSentCount)
                  .HasDefaultValue(0);

            entity.Property(stl => stl.StartedAt)
                  .HasColumnType("timestamp").HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(stl => stl.ErrorMessage)
                  .HasMaxLength(4000);

            entity.Property(stl => stl.Details)
                  .HasMaxLength(8000);
        });

        // ===== 软删除审计快照实体配置 =====
        modelBuilder.Entity<DeletedEntitySnapshot>(entity =>
        {
            entity.HasKey(des => des.Id);

            entity.HasIndex(des => new { des.EntityType, des.EntityId })
                  .HasDatabaseName("IX_DeletedEntitySnapshots_EntityType_EntityId");

            entity.HasIndex(des => new { des.DeletedBy, des.DeletedAt })
                  .HasDatabaseName("IX_DeletedEntitySnapshots_DeletedBy_DeletedAt");

            entity.HasIndex(des => des.DeletedAt)
                  .HasDatabaseName("IX_DeletedEntitySnapshots_DeletedAt");

            entity.Property(des => des.EntityType)
                  .IsRequired()
                  .HasMaxLength(100);

            entity.Property(des => des.EntityDisplayName)
                  .IsRequired()
                  .HasMaxLength(500);

            entity.Property(des => des.SnapshotData)
                  .IsRequired()
                  .HasColumnType("text");

            entity.Property(des => des.DeletionReason)
                  .HasMaxLength(500);

            entity.Property(des => des.DeletedAt)
                  .HasColumnType("timestamp").HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(des => des.DeletedByUser)
                  .WithMany()
                  .HasForeignKey(des => des.DeletedBy)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ===== 食物分类实体配置 =====
        modelBuilder.Entity<FoodCategory>(entity =>
        {
            entity.HasKey(fc => fc.Id);

            entity.HasIndex(fc => fc.Code)
                  .IsUnique()
                  .HasDatabaseName("IX_FoodCategories_Code");

            entity.HasIndex(fc => new { fc.ParentId, fc.SortOrder })
                  .HasDatabaseName("IX_FoodCategories_ParentId_SortOrder");

            entity.HasIndex(fc => new { fc.IsDeleted, fc.IsActive })
                  .HasDatabaseName("IX_FoodCategories_IsDeleted_IsActive");

            entity.Property(fc => fc.Name)
                  .IsRequired()
                  .HasMaxLength(50);

            entity.Property(fc => fc.Code)
                  .IsRequired()
                  .HasMaxLength(50);

            entity.Property(fc => fc.IconUrl)
                  .HasMaxLength(500);

            entity.Property(fc => fc.SortOrder)
                  .HasDefaultValue(0);

            entity.Property(fc => fc.Description)
                  .HasMaxLength(200);

            entity.Property(fc => fc.IsActive)
                  .HasDefaultValue(true);

            entity.Property(fc => fc.IsDeleted)
                  .HasDefaultValue(false);

            entity.Property(fc => fc.DeletionReason)
                  .HasMaxLength(500);

            entity.Property(fc => fc.CreatedAt)
                  .HasColumnType("timestamp").HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(fc => fc.UpdatedAt)
                  .HasColumnType("timestamp").HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(fc => fc.Parent)
                  .WithMany(fc => fc.Children)
                  .HasForeignKey(fc => fc.ParentId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasQueryFilter(fc => !fc.IsDeleted);
        });

        // ===== 过敏原标签实体配置 =====
        modelBuilder.Entity<AllergenTag>(entity =>
        {
            entity.HasKey(at => at.Id);

            entity.HasIndex(at => at.Code)
                  .IsUnique()
                  .HasDatabaseName("IX_AllergenTags_Code");

            entity.HasIndex(at => new { at.IsDeleted, at.IsActive })
                  .HasDatabaseName("IX_AllergenTags_IsDeleted_IsActive");

            entity.Property(at => at.Name)
                  .IsRequired()
                  .HasMaxLength(50);

            entity.Property(at => at.Code)
                  .IsRequired()
                  .HasMaxLength(50);

            entity.Property(at => at.IconUrl)
                  .HasMaxLength(500);

            entity.Property(at => at.SeverityLevel)
                  .IsRequired()
                  .HasDefaultValue(2);

            entity.Property(at => at.Description)
                  .HasMaxLength(500);

            entity.Property(at => at.SortOrder)
                  .HasDefaultValue(0);

            entity.Property(at => at.IsActive)
                  .HasDefaultValue(true);

            entity.Property(at => at.IsDeleted)
                  .HasDefaultValue(false);

            entity.Property(at => at.DeletionReason)
                  .HasMaxLength(500);

            entity.Property(at => at.CreatedAt)
                  .HasColumnType("timestamp").HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(at => at.UpdatedAt)
                  .HasColumnType("timestamp").HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasQueryFilter(at => !at.IsDeleted);
        });

        // ===== 帖子标签实体配置 =====
        modelBuilder.Entity<PostTag>(entity =>
        {
            entity.HasKey(pt => pt.Id);

            entity.HasIndex(pt => pt.Code)
                  .IsUnique()
                  .HasDatabaseName("IX_PostTags_Code");

            entity.HasIndex(pt => new { pt.IsDeleted, pt.IsActive })
                  .HasDatabaseName("IX_PostTags_IsDeleted_IsActive");

            entity.HasIndex(pt => pt.UsageCount)
                  .HasDatabaseName("IX_PostTags_UsageCount");

            entity.Property(pt => pt.Name)
                  .IsRequired()
                  .HasMaxLength(30);

            entity.Property(pt => pt.Code)
                  .IsRequired()
                  .HasMaxLength(30);

            entity.Property(pt => pt.Color)
                  .HasMaxLength(20)
                  .HasDefaultValue("#3B82F6");

            entity.Property(pt => pt.IconUrl)
                  .HasMaxLength(500);

            entity.Property(pt => pt.Description)
                  .HasMaxLength(200);

            entity.Property(pt => pt.UsageCount)
                  .HasDefaultValue(0);

            entity.Property(pt => pt.IsSystemDefined)
                  .HasDefaultValue(false);

            entity.Property(pt => pt.SortOrder)
                  .HasDefaultValue(0);

            entity.Property(pt => pt.IsActive)
                  .HasDefaultValue(true);

            entity.Property(pt => pt.IsDeleted)
                  .HasDefaultValue(false);

            entity.Property(pt => pt.DeletionReason)
                  .HasMaxLength(500);

            entity.Property(pt => pt.CreatedAt)
                  .HasColumnType("timestamp").HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(pt => pt.UpdatedAt)
                  .HasColumnType("timestamp").HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(pt => pt.CreatedByUser)
                  .WithMany(u => u.CreatedPostTags)
                  .HasForeignKey(pt => pt.CreatedBy)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasQueryFilter(pt => !pt.IsDeleted);
        });

        // ===== 分享帖-过敏原标签关联实体配置 =====
        modelBuilder.Entity<SharePostAllergenTag>(entity =>
        {
            entity.HasKey(spat => new { spat.SharePostId, spat.AllergenTagId });

            entity.HasIndex(spat => spat.AllergenTagId)
                  .HasDatabaseName("IX_SharePostAllergenTags_AllergenTagId");

            entity.Property(spat => spat.CreatedAt)
                  .HasColumnType("timestamp").HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(spat => spat.SharePost)
                  .WithMany(sp => sp.SharePostAllergenTags)
                  .HasForeignKey(spat => spat.SharePostId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(spat => spat.AllergenTag)
                  .WithMany(at => at.SharePostAllergenTags)
                  .HasForeignKey(spat => spat.AllergenTagId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ===== 分享帖-帖子标签关联实体配置 =====
        modelBuilder.Entity<SharePostPostTag>(entity =>
        {
            entity.HasKey(sppt => new { sppt.SharePostId, sppt.PostTagId });

            entity.HasIndex(sppt => sppt.PostTagId)
                  .HasDatabaseName("IX_SharePostPostTags_PostTagId");

            entity.Property(sppt => sppt.CreatedAt)
                  .HasColumnType("timestamp").HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(sppt => sppt.SharePost)
                  .WithMany(sp => sp.SharePostPostTags)
                  .HasForeignKey(sppt => sppt.SharePostId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(sppt => sppt.PostTag)
                  .WithMany(pt => pt.SharePostPostTags)
                  .HasForeignKey(sppt => sppt.PostTagId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Review>(entity =>
        {
            entity.HasKey(r => r.Id);

            entity.HasIndex(r => new { r.ReservationId, r.ReviewerId })
                  .IsUnique()
                  .HasDatabaseName("IX_Reviews_ReservationId_ReviewerId");

            entity.HasIndex(r => new { r.PublisherId, r.Status, r.CreatedAt })
                  .HasDatabaseName("IX_Reviews_PublisherId_Status_CreatedAt");

            entity.HasIndex(r => new { r.SharePostId, r.CreatedAt })
                  .HasDatabaseName("IX_Reviews_SharePostId_CreatedAt");

            entity.HasIndex(r => new { r.ReviewerId, r.CreatedAt })
                  .HasDatabaseName("IX_Reviews_ReviewerId_CreatedAt");

            entity.HasIndex(r => new { r.ReviewerIp, r.CreatedAt })
                  .HasDatabaseName("IX_Reviews_ReviewerIp_CreatedAt");

            entity.HasIndex(r => new { r.IsDeleted, r.DeletedAt })
                  .HasDatabaseName("IX_Reviews_IsDeleted_DeletedAt");

            entity.HasIndex(r => r.Rating)
                  .HasDatabaseName("IX_Reviews_Rating");

            entity.Property(r => r.Rating)
                  .IsRequired();

            entity.Property(r => r.Comment)
                  .HasMaxLength(500);

            entity.Property(r => r.Status)
                  .IsRequired()
                  .HasConversion<string>()
                  .HasMaxLength(20)
                  .HasDefaultValue(Entities.Enums.ReviewStatus.Normal);

            entity.Property(r => r.ReviewerIp)
                  .HasMaxLength(45);

            entity.Property(r => r.IsFirstReview)
                  .HasDefaultValue(false);

            entity.Property(r => r.IsDeleted)
                  .HasDefaultValue(false);

            entity.Property(r => r.DeletionReason)
                  .HasMaxLength(500);

            entity.Property(r => r.FlagDetail)
                  .HasMaxLength(500);

            entity.Property(r => r.CreatedAt)
                  .HasColumnType("timestamp").HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(r => r.UpdatedAt)
                  .HasColumnType("timestamp").HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(r => r.Reservation)
                  .WithMany()
                  .HasForeignKey(r => r.ReservationId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(r => r.Reviewer)
                  .WithMany()
                  .HasForeignKey(r => r.ReviewerId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(r => r.Publisher)
                  .WithMany()
                  .HasForeignKey(r => r.PublisherId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(r => r.SharePost)
                  .WithMany()
                  .HasForeignKey(r => r.SharePostId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasQueryFilter(r => !r.IsDeleted);
        });

        modelBuilder.Entity<PublisherReputation>(entity =>
        {
            entity.HasKey(pr => pr.Id);

            entity.HasIndex(pr => pr.PublisherId)
                  .IsUnique()
                  .HasDatabaseName("IX_PublisherReputations_PublisherId");

            entity.HasIndex(pr => pr.ReputationScore)
                  .HasDatabaseName("IX_PublisherReputations_ReputationScore");

            entity.HasIndex(pr => pr.AverageRating)
                  .HasDatabaseName("IX_PublisherReputations_AverageRating");

            entity.HasIndex(pr => new { pr.ReputationScore, pr.TotalReviewCount })
                  .HasDatabaseName("IX_PublisherReputations_Score_Count");

            entity.Property(pr => pr.AverageRating)
                  .IsRequired()
                  .HasColumnType("decimal(3,2)");

            entity.Property(pr => pr.ReputationScore)
                  .IsRequired()
                  .HasColumnType("decimal(5,2)");

            entity.Property(pr => pr.LastReviewAt)
                  .HasColumnType("timestamp").HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(pr => pr.CreatedAt)
                  .HasColumnType("timestamp").HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(pr => pr.UpdatedAt)
                  .HasColumnType("timestamp").HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(pr => pr.Publisher)
                  .WithMany()
                  .HasForeignKey(pr => pr.PublisherId)
                  .OnDelete(DeleteBehavior.Restrict);
        });
    }

    public override int SaveChanges()
    {
        ProcessSoftDeletes();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ProcessSoftDeletes();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void ProcessSoftDeletes()
    {
        var softDeleteEntries = ChangeTracker.Entries<ISoftDeletable>()
            .Where(e => e.State == EntityState.Deleted && !_hardDeleteEntities.Contains(e.Entity))
            .ToList();

        foreach (var entry in softDeleteEntries)
        {
            entry.State = EntityState.Modified;
            entry.Entity.IsDeleted = true;
            entry.Entity.DeletedAt = DateTime.UtcNow;

            if (entry.Entity.DeletedBy == null)
            {
                var deletedByProp = entry.Property("DeletedBy");
                if (deletedByProp.CurrentValue == null || (int)deletedByProp.CurrentValue == 0)
                {
                    deletedByProp.CurrentValue = null;
                }
            }

            CreateAuditSnapshot(entry);

            HandleCascadeSoftDelete(entry);
        }

        _hardDeleteEntities.Clear();
    }

    private void CreateAuditSnapshot(EntityEntry<ISoftDeletable> entry)
    {
        var entityType = entry.Entity.GetType().Name;
        var entityId = (int)entry.Property("Id").CurrentValue!;
        var entityDisplayName = GetEntityDisplayName(entry);
        var originalOwnerId = GetOriginalOwnerId(entry);
        var deletionReason = entry.Entity.DeletionReason;
        var deletedBy = entry.Entity.DeletedBy ?? 0;
        var deletedAt = entry.Entity.DeletedAt ?? DateTime.UtcNow;

        var snapshotData = new Dictionary<string, object?>();
        foreach (var prop in entry.OriginalValues.Properties)
        {
            var propName = prop.Name;
            var propValue = entry.OriginalValues[prop];
            snapshotData[propName] = propValue;
        }

        var snapshot = new DeletedEntitySnapshot
        {
            EntityType = entityType,
            EntityId = entityId,
            EntityDisplayName = entityDisplayName,
            SnapshotData = JsonSerializer.Serialize(snapshotData, new JsonSerializerOptions
            {
                WriteIndented = false
            }),
            DeletedBy = deletedBy,
            DeletedAt = deletedAt,
            DeletionReason = deletionReason,
            OriginalOwnerId = originalOwnerId
        };

        DeletedEntitySnapshots.Add(snapshot);
    }

    private static string GetEntityDisplayName(EntityEntry<ISoftDeletable> entry)
    {
        var entity = entry.Entity;
        return entity switch
        {
            SharePost sp => $"分享帖 #{sp.Id} - {sp.Title}",
            Reservation r => $"预约 #{r.Id} - 帖子#{r.PostId} by 用户#{r.ClaimerId}",
            PickupCode pc => $"取餐码 #{pc.Id} - {pc.Code}",
            KarmaPoint kp => $"积分流水 #{kp.Id} - 用户#{kp.UserId} {kp.Points}分",
            FoodCategory fc => $"食物分类 #{fc.Id} - {fc.Name}",
            AllergenTag at => $"过敏原标签 #{at.Id} - {at.Name}",
            PostTag pt => $"帖子标签 #{pt.Id} - {pt.Name}",
            Review r => $"评价 #{r.Id} - 用户#{r.ReviewerId}评发布者#{r.PublisherId}",
            _ => $"{entity.GetType().Name} #{entry.Property("Id").CurrentValue}"
        };
    }

    private static int? GetOriginalOwnerId(EntityEntry<ISoftDeletable> entry)
    {
        return entry.Entity switch
        {
            SharePost sp => sp.PosterId,
            Reservation r => r.ClaimerId,
            PickupCode pc => null,
            KarmaPoint kp => kp.UserId,
            FoodCategory fc => null,
            AllergenTag at => null,
            PostTag pt => pt.CreatedBy,
            Review r => r.ReviewerId,
            _ => null
        };
    }

    private void HandleCascadeSoftDelete(EntityEntry<ISoftDeletable> entry)
    {
        if (entry.Entity is SharePost post)
        {
            var relatedReservations = Reservations.Local
                .Where(r => r.PostId == post.Id && !r.IsDeleted)
                .ToList();

            foreach (var res in relatedReservations)
            {
                var resEntry = Entry(res);
                if (resEntry.State != EntityState.Deleted && resEntry.State != EntityState.Modified)
                {
                    res.IsDeleted = true;
                    res.DeletedAt = DateTime.UtcNow;
                    res.DeletedBy = post.DeletedBy;
                    res.DeletionReason = $"级联删除：关联分享帖#{post.Id}已删除";
                    resEntry.State = EntityState.Modified;
                }
            }

            var relatedPickupCodes = PickupCodes.Local
                .Where(pc => relatedReservations.Select(r => r.Id).Contains(pc.ReservationId) && !pc.IsDeleted)
                .ToList();

            foreach (var pickupCode in relatedPickupCodes)
            {
                var pcEntry = Entry(pickupCode);
                if (pcEntry.State != EntityState.Deleted && pcEntry.State != EntityState.Modified)
                {
                    pickupCode.IsDeleted = true;
                    pickupCode.DeletedAt = DateTime.UtcNow;
                    pickupCode.DeletedBy = post.DeletedBy;
                    pickupCode.DeletionReason = $"级联删除：关联分享帖#{post.Id}已删除";
                    pcEntry.State = EntityState.Modified;
                }
            }
        }

        if (entry.Entity is Reservation reservationEntry)
        {
            var relatedPickupCode = PickupCodes.Local
                .FirstOrDefault(pc => pc.ReservationId == reservationEntry.Id && !pc.IsDeleted);

            if (relatedPickupCode != null)
            {
                var pcEntry = Entry(relatedPickupCode);
                if (pcEntry.State != EntityState.Deleted && pcEntry.State != EntityState.Modified)
                {
                    relatedPickupCode.IsDeleted = true;
                    relatedPickupCode.DeletedAt = DateTime.UtcNow;
                    relatedPickupCode.DeletedBy = reservationEntry.DeletedBy;
                    relatedPickupCode.DeletionReason = $"级联删除：关联预约#{reservationEntry.Id}已删除";
                    pcEntry.State = EntityState.Modified;
                }
            }
        }
    }
}
