using AutoMapper;
using LeftoverShare.API.Data;
using LeftoverShare.API.DTOs.Stats;
using LeftoverShare.API.Helpers;
using LeftoverShare.API.Repositories;
using Microsoft.EntityFrameworkCore;

namespace LeftoverShare.API.Services.Impl;

/// <summary>
/// 统计服务实现
/// </summary>
public class StatsService : IStatsService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly AppDbContext _context;

    public StatsService(IUnitOfWork unitOfWork, IMapper mapper, AppDbContext context)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _context = context;
    }

    /// <summary>
    /// 获取概览统计：用户总数、帖子总数、预订总数、积分总数
    /// </summary>
    public async Task<ApiResponse> GetOverviewAsync()
    {
        var totalUsers = await _context.Users.CountAsync();
        var totalPosts = await _context.SharePosts.CountAsync();
        var totalReservations = await _context.Reservations.CountAsync();
        var totalKarmaPoints = await _context.KarmaPoints.SumAsync(kp => kp.Points);

        var overviewStats = new OverviewStatsResponse
        {
            TotalUsers = totalUsers,
            TotalPosts = totalPosts,
            TotalReservations = totalReservations,
            TotalKarmaPoints = totalKarmaPoints
        };

        return ApiResponse.Success(overviewStats);
    }

    /// <summary>
    /// 获取趋势统计：按日期统计帖子和预订数量变化
    /// </summary>
    public async Task<ApiResponse> GetTrendAsync(DateTime startDate, DateTime endDate)
    {
        if (startDate > endDate)
        {
            return ApiResponse.Fail("开始日期不能大于结束日期");
        }

        startDate = startDate.Date;
        endDate = endDate.Date.AddDays(1).AddTicks(-1);

        var postTrend = await _context.SharePosts
            .Where(sp => sp.CreatedAt >= startDate && sp.CreatedAt <= endDate)
            .GroupBy(sp => sp.CreatedAt.Date)
            .Select(g => new
            {
                Date = g.Key,
                PostCount = g.Count()
            })
            .ToListAsync();

        var reservationTrend = await _context.Reservations
            .Where(r => r.ReservedAt >= startDate && r.ReservedAt <= endDate)
            .GroupBy(r => r.ReservedAt.Date)
            .Select(g => new
            {
                Date = g.Key,
                ReservationCount = g.Count()
            })
            .ToListAsync();

        var trendStats = new List<TrendStatsResponse>();
        for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
        {
            var postCount = postTrend.FirstOrDefault(p => p.Date == date)?.PostCount ?? 0;
            var reservationCount = reservationTrend.FirstOrDefault(r => r.Date == date)?.ReservationCount ?? 0;

            trendStats.Add(new TrendStatsResponse
            {
                Date = date,
                PostCount = postCount,
                ReservationCount = reservationCount
            });
        }

        return ApiResponse.Success(trendStats);
    }
}
