using AutoMapper;
using LeftoverShare.API.DTOs.Auth;
using LeftoverShare.API.DTOs.KarmaPoints;
using LeftoverShare.API.DTOs.PickupCodes;
using LeftoverShare.API.DTOs.Reservations;
using LeftoverShare.API.DTOs.SharePosts;
using LeftoverShare.API.Entities;
using LeftoverShare.API.Helpers;
using System.Text.Json;

namespace LeftoverShare.API.Mappings;

/// <summary>
/// AutoMapper 映射配置文件
/// 业务意图：配置实体与 DTO 之间的映射关系，使用 LeftoverShare.API.Entities 命名空间
/// 注意：状态使用枚举映射，Reservation 外键为 PostId，KarmaPoint 属性为 RelatedId
/// </summary>
public class MappingProfile : Profile
{
    /// <summary>
    /// 初始化映射配置
    /// 业务意图：配置所有实体与 DTO 的映射规则，不使用可选参数在表达式树中
    /// </summary>
    public MappingProfile()
    {
        // 用户实体 → 用户响应DTO
        CreateMap<User, UserResponse>()
            .ForMember(dest => dest.TotalKarmaPoints, opt => opt.MapFrom(src => src.KarmaPoints.Sum(kp => kp.Points)));

        // 分享帖实体 → 分享帖详情响应DTO
        CreateMap<SharePost, SharePostResponse>()
            .ForMember(dest => dest.Latitude, opt => opt.MapFrom(src => DecimalToDouble(src.Latitude)))
            .ForMember(dest => dest.Longitude, opt => opt.MapFrom(src => DecimalToDouble(src.Longitude)))
            .ForMember(dest => dest.Photos, opt => opt.MapFrom(src => DeserializePhotos(src.Photos)))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => src.CreatedAt));

        // 分享帖实体 → 分享帖列表响应DTO
        CreateMap<SharePost, SharePostListResponse>()
            .ForMember(dest => dest.Latitude, opt => opt.MapFrom(src => DecimalToDouble(src.Latitude)))
            .ForMember(dest => dest.Longitude, opt => opt.MapFrom(src => DecimalToDouble(src.Longitude)))
            .ForMember(dest => dest.FirstPhoto, opt => opt.MapFrom(src => GetFirstPhoto(src.Photos)))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.PosterUsername, opt => opt.MapFrom(src => src.Poster.Username));

        // 创建分享帖请求DTO → 分享帖实体
        CreateMap<CreateSharePostRequest, SharePost>()
            .ForMember(dest => dest.Latitude, opt => opt.MapFrom(src => (decimal?)src.Latitude))
            .ForMember(dest => dest.Longitude, opt => opt.MapFrom(src => (decimal?)src.Longitude))
            .ForMember(dest => dest.Photos, opt => opt.MapFrom(src => SerializePhotos(src.Photos)))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.Now));

        // 更新分享帖请求DTO → 分享帖实体
        CreateMap<UpdateSharePostRequest, SharePost>()
            .ForMember(dest => dest.Latitude, opt => opt.MapFrom(src => (decimal?)src.Latitude))
            .ForMember(dest => dest.Longitude, opt => opt.MapFrom(src => (decimal?)src.Longitude))
            .ForMember(dest => dest.Photos, opt => opt.MapFrom(src => SerializePhotos(src.Photos)));

        // 预约实体 → 预约响应DTO（外键使用 PostId，NOT SharePostId）
        CreateMap<Reservation, ReservationResponse>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.ReservedAt))
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => src.ReservedAt));

        // 创建预约请求DTO → 预约实体（外键使用 PostId）
        CreateMap<CreateReservationRequest, Reservation>()
            .ForMember(dest => dest.ReservedAt, opt => opt.MapFrom(src => DateTime.Now))
            .ForMember(dest => dest.PickupCode, opt => opt.MapFrom(src => PickupCodeGenerator.Generate()));

        // 更新预约请求DTO → 预约实体
        CreateMap<UpdateReservationRequest, Reservation>();

        // 取餐码实体 → 取餐码响应DTO
        CreateMap<PickupCode, PickupCodeResponse>()
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.ExpiresAt.AddHours(-1)));

        // 积分实体 → 积分响应DTO（属性使用 RelatedId，NOT RelatedSharePostId）
        CreateMap<KarmaPoint, KarmaPointResponse>();

        // 创建积分请求DTO → 积分实体（属性使用 RelatedId）
        CreateMap<CreateKarmaPointRequest, KarmaPoint>()
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.Now));

        // 更新积分请求DTO → 积分实体
        CreateMap<UpdateKarmaPointRequest, KarmaPoint>();
    }

    /// <summary>
    /// 将可空 decimal 转换为 double
    /// 业务意图：避免在表达式树中使用可选参数，通过静态方法处理 nullable 转换
    /// </summary>
    private static double DecimalToDouble(decimal? value)
    {
        return value.HasValue ? (double)value.Value : 0;
    }

    /// <summary>
    /// 反序列化照片 JSON 字符串为列表
    /// 业务意图：将数据库中存储的 JSON 字符串转换为前端可用的字符串列表
    /// </summary>
    private static List<string>? DeserializePhotos(string? photosJson)
    {
        if (string.IsNullOrEmpty(photosJson))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(photosJson);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 从照片 JSON 字符串中提取第一张照片
    /// 业务意图：用于列表展示时只显示第一张照片，提升加载性能
    /// </summary>
    private static string? GetFirstPhoto(string? photosJson)
    {
        if (string.IsNullOrEmpty(photosJson))
        {
            return null;
        }

        try
        {
            var photos = JsonSerializer.Deserialize<List<string>>(photosJson);
            return photos != null && photos.Count > 0 ? photos[0] : null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 序列化照片列表为 JSON 字符串
    /// 业务意图：将前端传入的照片列表序列化为数据库存储的 JSON 字符串
    /// </summary>
    private static string? SerializePhotos(List<string>? photos)
    {
        if (photos == null || photos.Count == 0)
        {
            return null;
        }

        return JsonSerializer.Serialize(photos);
    }
}
