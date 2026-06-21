namespace LeftoverShare.Tests.Helpers;

public class PickupCodeGeneratorTests
{
    [Fact]
    public void Generate_ReturnsString()
    {
        var result = PickupCodeGenerator.Generate();

        result.Should().NotBeNull();
        result.Should().BeOfType<string>();
    }

    [Fact]
    public void Generate_ReturnsExactlySixCharacters()
    {
        var result = PickupCodeGenerator.Generate();

        result.Length.Should().Be(6);
    }

    [Fact]
    public void Generate_ReturnsOnlyDigits()
    {
        var result = PickupCodeGenerator.Generate();

        result.Should().MatchRegex(@"^\d{6}$");
    }

    [Fact]
    public void Generate_ReturnsValueInRange()
    {
        var result = PickupCodeGenerator.Generate();
        var numericValue = int.Parse(result);

        numericValue.Should().BeInRange(100000, 999999);
    }

    [Fact]
    public void Generate_DoesNotStartWithZero()
    {
        var result = PickupCodeGenerator.Generate();

        result[0].Should().NotBe('0');
    }

    [Fact]
    public void Generate_MultipleCalls_ReturnDifferentValues()
    {
        var results = new HashSet<string>();

        for (int i = 0; i < 100; i++)
        {
            results.Add(PickupCodeGenerator.Generate());
        }

        results.Count.Should().BeGreaterThan(1);
    }

    [Fact]
    public void Generate_1000Generations_AllAreValid()
    {
        for (int i = 0; i < 1000; i++)
        {
            var code = PickupCodeGenerator.Generate();

            code.Length.Should().Be(6, $"第 {i} 个生成的取餐码长度不正确: {code}");
            code.Should().MatchRegex(@"^\d{6}$", $"第 {i} 个生成的取餐码格式不正确: {code}");

            var numericValue = int.Parse(code);
            numericValue.Should().BeInRange(100000, 999999, $"第 {i} 个生成的取餐码超出范围: {code}");
        }
    }

    [Theory]
    [InlineData(100)]
    [InlineData(500)]
    [InlineData(1000)]
    public void Generate_MultipleGenerations_HaveLowCollisionRate(int count)
    {
        var results = new List<string>();

        for (int i = 0; i < count; i++)
        {
            results.Add(PickupCodeGenerator.Generate());
        }

        var uniqueResults = new HashSet<string>(results);
        var collisionRate = (double)(count - uniqueResults.Count) / count;

        collisionRate.Should().BeLessThan(0.01, $"碰撞率过高: {collisionRate:P2}");
    }

    [Fact]
    public void Generate_FormatIsD6_PadsWithZerosWhenNeeded()
    {
        var foundSmallNumber = false;
        var attempts = 0;
        const int maxAttempts = 10000;

        while (!foundSmallNumber && attempts < maxAttempts)
        {
            var code = PickupCodeGenerator.Generate();
            var numericValue = int.Parse(code);

            if (numericValue < 1000000)
            {
                foundSmallNumber = true;
                code.Length.Should().Be(6);
                code.Should().MatchRegex(@"^\d{6}$");
            }

            attempts++;
        }

        foundSmallNumber.Should().BeTrue("未能在合理次数内生成可以验证格式的取餐码");
    }

    [Fact]
    public void Generate_ValuesAreUniformlyDistributed()
    {
        var sampleSize = 10000;
        var digitCounts = new int[10];
        var firstDigitCounts = new int[10];

        for (int i = 0; i < sampleSize; i++)
        {
            var code = PickupCodeGenerator.Generate();

            for (int j = 0; j < code.Length; j++)
            {
                int digit = int.Parse(code[j].ToString());
                digitCounts[digit]++;

                if (j == 0)
                {
                    firstDigitCounts[digit]++;
                }
            }
        }

        firstDigitCounts[0].Should().Be(0, "首位不应该为0");

        var expectedPerDigit = (sampleSize * 6) / 10.0;
        var tolerance = expectedPerDigit * 0.2;

        for (int i = 0; i < 10; i++)
        {
            if (i == 0)
            {
                digitCounts[i].Should().BeInRange(
                    (int)(expectedPerDigit * 0.8 - tolerance),
                    (int)(expectedPerDigit * 1.2 + tolerance),
                    $"数字 {i} 的出现频率异常");
            }
            else
            {
                digitCounts[i].Should().BeInRange(
                    (int)(expectedPerDigit - tolerance),
                    (int)(expectedPerDigit + tolerance),
                    $"数字 {i} 的出现频率异常");
            }
        }
    }

    [Fact]
    public void Generate_IsThreadSafe()
    {
        var threads = new Thread[10];
        var results = new ConcurrentBag<string>();

        for (int i = 0; i < threads.Length; i++)
        {
            threads[i] = new Thread(() =>
            {
                for (int j = 0; j < 100; j++)
                {
                    results.Add(PickupCodeGenerator.Generate());
                }
            });
        }

        foreach (var thread in threads)
        {
            thread.Start();
        }

        foreach (var thread in threads)
        {
            thread.Join();
        }

        results.Count.Should().Be(1000);

        foreach (var code in results)
        {
            code.Length.Should().Be(6);
            code.Should().MatchRegex(@"^\d{6}$");
        }
    }

    [Fact]
    public void Generate_StatisticalRandomnessTest()
    {
        var sampleSize = 1000;
        var codes = new List<string>();

        for (int i = 0; i < sampleSize; i++)
        {
            codes.Add(PickupCodeGenerator.Generate());
        }

        var mean = codes.Average(c => int.Parse(c));
        var expectedMean = (100000 + 999999) / 2.0;

        mean.Should().BeInRange(expectedMean * 0.9, expectedMean * 1.1);

        var sortedCodes = codes.Select(c => int.Parse(c)).OrderBy(x => x).ToList();
        var median = sampleSize % 2 == 0
            ? (sortedCodes[sampleSize / 2 - 1] + sortedCodes[sampleSize / 2]) / 2.0
            : sortedCodes[sampleSize / 2];

        median.Should().BeInRange(expectedMean * 0.85, expectedMean * 1.15);
    }
}
