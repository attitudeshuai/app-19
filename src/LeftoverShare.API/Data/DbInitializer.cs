using LeftoverShare.API.Entities;
using LeftoverShare.API.Entities.Enums;
using Microsoft.EntityFrameworkCore;

namespace LeftoverShare.API.Data;

// 数据库初始化器，负责迁移和种子数据
public static class DbInitializer
{
    // 初始化数据库（执行迁移 + 种子数据）
    public static async Task Initialize(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;
        var context = services.GetRequiredService<AppDbContext>();
        var logger = services.GetRequiredService<ILogger<AppDbContext>>();

        var maxRetries = 30;
        var delay = TimeSpan.FromSeconds(5);

        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                await context.Database.EnsureCreatedAsync();
                await SeedData(context);
                logger.LogInformation("数据库初始化成功");
                return;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "数据库初始化失败，第 {Attempt}/{Max} 次重试...", i + 1, maxRetries);
                if (i == maxRetries - 1)
                {
                    logger.LogError(ex, "数据库初始化最终失败");
                    throw;
                }
                await Task.Delay(delay);
            }
        }
    }

    // 种子数据：用户、分享帖子、预订、取餐码、积分
    private static async Task SeedData(AppDbContext context)
    {
        // 如果已有用户数据，则跳过种子
        if (await context.Users.AnyAsync())
        {
            return;
        }

        // ===== 种子用户 =====
        var users = new List<User>
        {
            new User
            {
                Username = "zhangsan",
                Email = "zhangsan@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
                Avatar = null,
                CreatedAt = DateTime.UtcNow.AddDays(-30),
                UpdatedAt = DateTime.UtcNow.AddDays(-30)
            },
            new User
            {
                Username = "lisi",
                Email = "lisi@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
                Avatar = null,
                CreatedAt = DateTime.UtcNow.AddDays(-25),
                UpdatedAt = DateTime.UtcNow.AddDays(-25)
            },
            new User
            {
                Username = "wangwu",
                Email = "wangwu@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
                Avatar = null,
                CreatedAt = DateTime.UtcNow.AddDays(-20),
                UpdatedAt = DateTime.UtcNow.AddDays(-20)
            },
            new User
            {
                Username = "zhaoliu",
                Email = "zhaoliu@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
                Avatar = null,
                CreatedAt = DateTime.UtcNow.AddDays(-15),
                UpdatedAt = DateTime.UtcNow.AddDays(-15)
            },
            new User
            {
                Username = "sunqi",
                Email = "sunqi@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
                Avatar = null,
                CreatedAt = DateTime.UtcNow.AddDays(-10),
                UpdatedAt = DateTime.UtcNow.AddDays(-10)
            }
        };

        await context.Users.AddRangeAsync(users);
        await context.SaveChangesAsync();

        // ===== 种子分享帖子 =====
        var sharePosts = new List<SharePost>
        {
            new SharePost
            {
                PosterId = users[0].Id,
                Title = "家庭聚餐剩余的红烧肉",
                Description = "今天家庭聚餐做的红烧肉，还剩大约2斤，味道很好，希望有人能来取走。",
                FoodType = "肉类",
                Quantity = 2,
                PickupAddress = "北京市朝阳区建国路88号小区门口",
                Latitude = (decimal)39.9042,
                Longitude = (decimal)116.4074,
                Photos = "https://example.com/photos/hongshaorou.jpg",
                AvailableUntil = DateTime.UtcNow.AddHours(8),
                CreatedAt = DateTime.UtcNow.AddHours(-2),
                Status = SharePostStatus.Available
            },
            new SharePost
            {
                PosterId = users[1].Id,
                Title = "公司下午茶剩余蛋糕",
                Description = "公司下午茶买多了，有3个慕斯蛋糕和2个提拉米苏，都是今天新鲜的。",
                FoodType = "甜点",
                Quantity = 5,
                PickupAddress = "上海市浦东新区陆家嘴环路1000号大厦大堂",
                Latitude = (decimal)31.2304,
                Longitude = (decimal)121.4737,
                Photos = "https://example.com/photos/cakes.jpg",
                AvailableUntil = DateTime.UtcNow.AddHours(12),
                CreatedAt = DateTime.UtcNow.AddHours(-1),
                Status = SharePostStatus.Available
            },
            new SharePost
            {
                PosterId = users[2].Id,
                Title = "朋友送的新鲜水果篮",
                Description = "朋友送的水果篮，有苹果、橙子、葡萄等，大概5斤，吃不完分享。",
                FoodType = "水果",
                Quantity = 5,
                PickupAddress = "广州市天河区珠江新城华夏路8号公寓楼下",
                Latitude = (decimal)23.1291,
                Longitude = (decimal)113.2644,
                Photos = "https://example.com/photos/fruits.jpg",
                AvailableUntil = DateTime.UtcNow.AddDays(3),
                CreatedAt = DateTime.UtcNow.AddHours(-3),
                Status = SharePostStatus.Available
            },
            new SharePost
            {
                PosterId = users[3].Id,
                Title = "外卖点多了的便当",
                Description = "中午外卖点多了，一份日式照烧鸡腿饭，还没开封，需要的尽快来取。",
                FoodType = "便当",
                Quantity = 1,
                PickupAddress = "深圳市南山区科技园南区高新南一道星巴克门口",
                Latitude = (decimal)22.5431,
                Longitude = (decimal)114.0579,
                Photos = "https://example.com/photos/bento.jpg",
                AvailableUntil = DateTime.UtcNow.AddHours(4),
                CreatedAt = DateTime.UtcNow.AddMinutes(-30),
                Status = SharePostStatus.Available
            },
            new SharePost
            {
                PosterId = users[0].Id,
                Title = "自制手工面包",
                Description = "周末在家做的全麦面包，烤了6个，自己吃不完，分享给需要的朋友。",
                FoodType = "面包",
                Quantity = 4,
                PickupAddress = "北京市朝阳区建国路88号3号楼2单元",
                Latitude = (decimal)39.9042,
                Longitude = (decimal)116.4074,
                Photos = "https://example.com/photos/bread.jpg",
                AvailableUntil = DateTime.UtcNow.AddDays(2),
                CreatedAt = DateTime.UtcNow.AddHours(-5),
                Status = SharePostStatus.Available
            },
            new SharePost
            {
                PosterId = users[1].Id,
                Title = "生日派对剩余披萨",
                Description = "昨晚生日派对剩下的披萨，还有5块，加热即可食用。",
                FoodType = "披萨",
                Quantity = 5,
                PickupAddress = "上海市浦东新区陆家嘴环路1000号28楼茶水间",
                Latitude = (decimal)31.2304,
                Longitude = (decimal)121.4737,
                Photos = "https://example.com/photos/pizza.jpg",
                AvailableUntil = DateTime.UtcNow.AddHours(6),
                CreatedAt = DateTime.UtcNow.AddHours(-12),
                Status = SharePostStatus.Reserved
            },
            new SharePost
            {
                PosterId = users[2].Id,
                Title = "超市买的牛奶和酸奶",
                Description = "超市促销买多了，3盒纯牛奶和2盒酸奶，都是近期生产的。",
                FoodType = "乳制品",
                Quantity = 5,
                PickupAddress = "广州市天河区珠江新城华夏路8号便利店门口",
                Latitude = (decimal)23.1291,
                Longitude = (decimal)113.2644,
                Photos = "https://example.com/photos/milk.jpg",
                AvailableUntil = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow.AddHours(-8),
                Status = SharePostStatus.Available
            },
            new SharePost
            {
                PosterId = users[3].Id,
                Title = "团建剩余的零食大礼包",
                Description = "公司团建剩下的零食，有薯片、饼干、巧克力等，满满一大袋。",
                FoodType = "零食",
                Quantity = 10,
                PickupAddress = "深圳市南山区科技园南区高新南一道公司前台",
                Latitude = (decimal)22.5431,
                Longitude = (decimal)114.0579,
                Photos = "https://example.com/photos/snacks.jpg",
                AvailableUntil = DateTime.UtcNow.AddDays(14),
                CreatedAt = DateTime.UtcNow.AddHours(-6),
                Status = SharePostStatus.Available
            },
            new SharePost
            {
                PosterId = users[4].Id,
                Title = "妈妈做的家常菜",
                Description = "妈妈来看我做了太多菜，有红烧排骨、清炒时蔬、番茄炒蛋各一份。",
                FoodType = "家常菜",
                Quantity = 3,
                PickupAddress = "杭州市西湖区文三路478号小区北门",
                Latitude = (decimal)30.2741,
                Longitude = (decimal)120.1551,
                Photos = "https://example.com/photos/homefood.jpg",
                AvailableUntil = DateTime.UtcNow.AddHours(5),
                CreatedAt = DateTime.UtcNow.AddMinutes(-45),
                Status = SharePostStatus.Available
            },
            new SharePost
            {
                PosterId = users[4].Id,
                Title = "咖啡店买多了的三明治",
                Description = "早上咖啡店买多了，2个火腿三明治，新鲜可口。",
                FoodType = "三明治",
                Quantity = 2,
                PickupAddress = "杭州市西湖区文三路478号星巴克门口",
                Latitude = (decimal)30.2741,
                Longitude = (decimal)120.1551,
                Photos = "https://example.com/photos/sandwich.jpg",
                AvailableUntil = DateTime.UtcNow.AddHours(3),
                CreatedAt = DateTime.UtcNow.AddMinutes(-20),
                Status = SharePostStatus.PickedUp
            }
        };

        await context.SharePosts.AddRangeAsync(sharePosts);
        await context.SaveChangesAsync();

        // ===== 种子预订（使用 PostId，非 SharePostId） =====
        var reservations = new List<Reservation>
        {
            new Reservation
            {
                PostId = sharePosts[5].Id,
                ClaimerId = users[3].Id,
                Status = ReservationStatus.Confirmed,
                ReservedAt = DateTime.UtcNow.AddHours(-10),
                PickupCode = "A1B2C3"
            },
            new Reservation
            {
                PostId = sharePosts[0].Id,
                ClaimerId = users[2].Id,
                Status = ReservationStatus.Pending,
                ReservedAt = DateTime.UtcNow.AddMinutes(-15),
                PickupCode = "B2C3D4"
            },
            new Reservation
            {
                PostId = sharePosts[1].Id,
                ClaimerId = users[4].Id,
                Status = ReservationStatus.Pending,
                ReservedAt = DateTime.UtcNow.AddMinutes(-10),
                PickupCode = "C3D4E5"
            },
            new Reservation
            {
                PostId = sharePosts[9].Id,
                ClaimerId = users[0].Id,
                Status = ReservationStatus.Completed,
                ReservedAt = DateTime.UtcNow.AddDays(-1),
                PickupCode = "D4E5F6"
            },
            new Reservation
            {
                PostId = sharePosts[3].Id,
                ClaimerId = users[1].Id,
                Status = ReservationStatus.Confirmed,
                ReservedAt = DateTime.UtcNow.AddMinutes(-25),
                PickupCode = "E5F6G7"
            }
        };

        await context.Reservations.AddRangeAsync(reservations);
        await context.SaveChangesAsync();

        // ===== 种子取餐码 =====
        var pickupCodes = new List<PickupCode>
        {
            new PickupCode
            {
                ReservationId = reservations[0].Id,
                Code = "A1B2C3",
                ExpiresAt = DateTime.UtcNow.AddHours(2),
                IsUsed = false,
                UsedAt = null
            },
            new PickupCode
            {
                ReservationId = reservations[3].Id,
                Code = "X9Y8Z7",
                ExpiresAt = DateTime.UtcNow.AddDays(-1).AddHours(4),
                IsUsed = true,
                UsedAt = DateTime.UtcNow.AddDays(-1).AddHours(2)
            },
            new PickupCode
            {
                ReservationId = reservations[4].Id,
                Code = "D4E5F6",
                ExpiresAt = DateTime.UtcNow.AddHours(3),
                IsUsed = false,
                UsedAt = null
            }
        };

        await context.PickupCodes.AddRangeAsync(pickupCodes);
        await context.SaveChangesAsync();

        // ===== 种子积分 =====
        var karmaPoints = new List<KarmaPoint>
        {
            new KarmaPoint
            {
                UserId = users[0].Id,
                Points = 50,
                Reason = "分享食物 - 家庭聚餐剩余的红烧肉",
                CreatedAt = DateTime.UtcNow.AddHours(-2),
                RelatedId = sharePosts[0].Id
            },
            new KarmaPoint
            {
                UserId = users[0].Id,
                Points = 30,
                Reason = "分享食物 - 自制手工面包",
                CreatedAt = DateTime.UtcNow.AddHours(-5),
                RelatedId = sharePosts[4].Id
            },
            new KarmaPoint
            {
                UserId = users[0].Id,
                Points = 70,
                Reason = "历史分享奖励",
                CreatedAt = DateTime.UtcNow.AddDays(-15),
                RelatedId = null
            },
            new KarmaPoint
            {
                UserId = users[1].Id,
                Points = 50,
                Reason = "分享食物 - 公司下午茶剩余蛋糕",
                CreatedAt = DateTime.UtcNow.AddHours(-1),
                RelatedId = sharePosts[1].Id
            },
            new KarmaPoint
            {
                UserId = users[1].Id,
                Points = 80,
                Reason = "分享食物 - 生日派对剩余披萨",
                CreatedAt = DateTime.UtcNow.AddHours(-12),
                RelatedId = sharePosts[5].Id
            },
            new KarmaPoint
            {
                UserId = users[1].Id,
                Points = 100,
                Reason = "月度活跃用户奖励",
                CreatedAt = DateTime.UtcNow.AddDays(-5),
                RelatedId = null
            },
            new KarmaPoint
            {
                UserId = users[2].Id,
                Points = 50,
                Reason = "分享食物 - 朋友送的新鲜水果篮",
                CreatedAt = DateTime.UtcNow.AddHours(-3),
                RelatedId = sharePosts[2].Id
            },
            new KarmaPoint
            {
                UserId = users[2].Id,
                Points = 30,
                Reason = "分享食物 - 超市买的牛奶和酸奶",
                CreatedAt = DateTime.UtcNow.AddHours(-8),
                RelatedId = sharePosts[6].Id
            },
            new KarmaPoint
            {
                UserId = users[3].Id,
                Points = 50,
                Reason = "分享食物 - 外卖点多了的便当",
                CreatedAt = DateTime.UtcNow.AddMinutes(-30),
                RelatedId = sharePosts[3].Id
            },
            new KarmaPoint
            {
                UserId = users[3].Id,
                Points = 60,
                Reason = "分享食物 - 团建剩余的零食大礼包",
                CreatedAt = DateTime.UtcNow.AddHours(-6),
                RelatedId = sharePosts[7].Id
            },
            new KarmaPoint
            {
                UserId = users[3].Id,
                Points = 200,
                Reason = "累计分享10次特别奖励",
                CreatedAt = DateTime.UtcNow.AddDays(-3),
                RelatedId = null
            },
            new KarmaPoint
            {
                UserId = users[4].Id,
                Points = 50,
                Reason = "分享食物 - 妈妈做的家常菜",
                CreatedAt = DateTime.UtcNow.AddMinutes(-45),
                RelatedId = sharePosts[8].Id
            },
            new KarmaPoint
            {
                UserId = users[4].Id,
                Points = 50,
                Reason = "分享食物 - 咖啡店买多了的三明治",
                CreatedAt = DateTime.UtcNow.AddMinutes(-20),
                RelatedId = sharePosts[9].Id
            },
            new KarmaPoint
            {
                UserId = users[4].Id,
                Points = -55,
                Reason = "预约后未按时领取扣除",
                CreatedAt = DateTime.UtcNow.AddDays(-2),
                RelatedId = null
            },
            new KarmaPoint
            {
                UserId = users[2].Id,
                Points = 10,
                Reason = "预约分享帖",
                CreatedAt = DateTime.UtcNow.AddMinutes(-15),
                RelatedId = sharePosts[0].Id
            },
            new KarmaPoint
            {
                UserId = users[4].Id,
                Points = 10,
                Reason = "预约分享帖",
                CreatedAt = DateTime.UtcNow.AddMinutes(-10),
                RelatedId = sharePosts[1].Id
            }
        };

        await context.KarmaPoints.AddRangeAsync(karmaPoints);
        await context.SaveChangesAsync();
    }
}
