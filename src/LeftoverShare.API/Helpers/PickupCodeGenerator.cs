namespace LeftoverShare.API.Helpers;

/// <summary>
/// 取餐码生成器
/// 业务意图：生成 6 位随机数字取餐码，用于用户取餐时验证身份
/// </summary>
public static class PickupCodeGenerator
{
    private static readonly Random _random = new();

    /// <summary>
    /// 生成 6 位随机数字取餐码
    /// 业务意图：生成一个 6 位的随机数字字符串，作为取餐时的验证凭证
    /// </summary>
    /// <returns>6 位数字取餐码字符串</returns>
    public static string Generate()
    {
        return _random.Next(100000, 1000000).ToString("D6");
    }
}
