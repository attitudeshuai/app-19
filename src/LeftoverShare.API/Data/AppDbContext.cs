using LeftoverShare.API.Entities;
using LeftoverShare.API.Entities.Enums;
using Microsoft.EntityFrameworkCore;

namespace LeftoverShare.API.Data;

// 数据库上下文，使用 MySQL，配置实体映射
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
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

            // 使用 MySQL 的 NOW() 函数作为默认值
            entity.Property(u => u.CreatedAt)
                  .HasColumnType("timestamp").HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(u => u.UpdatedAt)
                  .HasColumnType("timestamp").HasDefaultValueSql("CURRENT_TIMESTAMP");

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

            // 使用 MySQL 的 NOW() 函数作为默认值
            entity.Property(sp => sp.CreatedAt)
                  .HasColumnType("timestamp").HasDefaultValueSql("CURRENT_TIMESTAMP");

            // 帖子与预订：一对多
            entity.HasMany(sp => sp.Reservations)
                  .WithOne(r => r.Post)
                  .HasForeignKey(r => r.PostId)
                  .OnDelete(DeleteBehavior.Cascade);
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

            // 使用 MySQL 的 NOW() 函数作为默认值
            entity.Property(r => r.ReservedAt)
                  .HasColumnType("timestamp").HasDefaultValueSql("CURRENT_TIMESTAMP");

            // 预订与取餐码：一对一
            entity.HasOne(r => r.PickupCodeNavigation)
                  .WithOne(pc => pc.Reservation)
                  .HasForeignKey<PickupCode>(pc => pc.ReservationId)
                  .OnDelete(DeleteBehavior.Cascade);
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

            entity.Property(pc => pc.Code)
                  .IsRequired()
                  .HasMaxLength(10);

            entity.Property(pc => pc.ExpiresAt)
                  .IsRequired();

            entity.Property(pc => pc.IsUsed)
                  .HasDefaultValue(false);
        });

        // ===== 积分实体配置 =====
        modelBuilder.Entity<KarmaPoint>(entity =>
        {
            entity.HasKey(kp => kp.Id);

            // 复合索引：用户+创建时间
            entity.HasIndex(kp => new { kp.UserId, kp.CreatedAt })
                  .HasDatabaseName("IX_KarmaPoints_UserId_CreatedAt");

            entity.Property(kp => kp.Points)
                  .IsRequired();

            entity.Property(kp => kp.Reason)
                  .IsRequired()
                  .HasMaxLength(200);

            // 使用 MySQL 的 NOW() 函数作为默认值
            entity.Property(kp => kp.CreatedAt)
                  .HasColumnType("timestamp").HasDefaultValueSql("CURRENT_TIMESTAMP");
        });
    }
}
