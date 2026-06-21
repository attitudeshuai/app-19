using AutoMapper;
using BCrypt.Net;
using Microsoft.Extensions.Options;
using LeftoverShare.API.Helpers;

namespace LeftoverShare.Tests.Services;

public class AuthServiceTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly Mock<IOptions<JwtSettings>> _jwtSettingsMock;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _mapperMock = new Mock<IMapper>();
        _jwtSettingsMock = new Mock<IOptions<JwtSettings>>();

        _jwtSettingsMock.Setup(x => x.Value).Returns(new JwtSettings
        {
            Secret = "this_is_a_very_long_secret_key_for_testing_purposes",
            Issuer = "test",
            Audience = "test",
            ExpiryInMinutes = 60
        });

        _authService = new AuthService(_unitOfWorkMock.Object, _mapperMock.Object, _jwtSettingsMock.Object);
    }

    [Fact]
    public async Task RegisterAsync_WhenUsernameExists_ReturnsFailResponse()
    {
        var request = new RegisterRequest
        {
            Username = "existinguser",
            Email = "test@example.com",
            Password = "Password123!"
        };

        _unitOfWorkMock.Setup(x => x.Users.GetByUsernameAsync(request.Username))
            .ReturnsAsync(new User { Id = 1, Username = "existinguser" });

        var result = await _authService.RegisterAsync(request);

        result.Should().NotBeNull();
        result.Code.Should().Be(400);
        result.Message.Should().Be("用户名已存在");
        _unitOfWorkMock.Verify(x => x.Users.AddAsync(It.IsAny<User>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_WhenEmailExists_ReturnsFailResponse()
    {
        var request = new RegisterRequest
        {
            Username = "newuser",
            Email = "existing@example.com",
            Password = "Password123!"
        };

        _unitOfWorkMock.Setup(x => x.Users.GetByUsernameAsync(request.Username))
            .ReturnsAsync((User?)null);
        _unitOfWorkMock.Setup(x => x.Users.GetByEmailAsync(request.Email))
            .ReturnsAsync(new User { Id = 1, Email = "existing@example.com" });

        var result = await _authService.RegisterAsync(request);

        result.Should().NotBeNull();
        result.Code.Should().Be(400);
        result.Message.Should().Be("邮箱已被注册");
        _unitOfWorkMock.Verify(x => x.Users.AddAsync(It.IsAny<User>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_WhenValidRequest_CreatesUserWithHashedPassword()
    {
        var request = new RegisterRequest
        {
            Username = "newuser",
            Email = "new@example.com",
            Password = "Password123!"
        };

        _unitOfWorkMock.Setup(x => x.Users.GetByUsernameAsync(request.Username))
            .ReturnsAsync((User?)null);
        _unitOfWorkMock.Setup(x => x.Users.GetByEmailAsync(request.Email))
            .ReturnsAsync((User?)null);
        _unitOfWorkMock.Setup(x => x.Users.AddAsync(It.IsAny<User>()))
            .ReturnsAsync((User u) => u);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(1);
        _mapperMock.Setup(x => x.Map<UserResponse>(It.IsAny<User>()))
            .Returns(new UserResponse { Id = 1, Username = "newuser", Email = "new@example.com" });

        var result = await _authService.RegisterAsync(request);

        result.Should().NotBeNull();
        result.Code.Should().Be(200);
        result.Message.Should().Be("注册成功");

        _unitOfWorkMock.Verify(x => x.Users.AddAsync(It.Is<User>(u =>
            u.Username == request.Username &&
            u.Email == request.Email &&
            !string.IsNullOrEmpty(u.PasswordHash) &&
            u.PasswordHash != request.Password)), Times.Once);

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_PasswordIsHashedWithBCrypt()
    {
        var request = new RegisterRequest
        {
            Username = "newuser",
            Email = "new@example.com",
            Password = "Password123!"
        };

        User? createdUser = null;
        _unitOfWorkMock.Setup(x => x.Users.GetByUsernameAsync(request.Username))
            .ReturnsAsync((User?)null);
        _unitOfWorkMock.Setup(x => x.Users.GetByEmailAsync(request.Email))
            .ReturnsAsync((User?)null);
        _unitOfWorkMock.Setup(x => x.Users.AddAsync(It.IsAny<User>()))
            .Callback<User>(u => createdUser = u)
            .ReturnsAsync((User u) => u);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync())
            .ReturnsAsync(1);
        _mapperMock.Setup(x => x.Map<UserResponse>(It.IsAny<User>()))
            .Returns(new UserResponse { Id = 1 });

        await _authService.RegisterAsync(request);

        createdUser.Should().NotBeNull();
        BCrypt.Net.BCrypt.Verify(request.Password, createdUser!.PasswordHash).Should().BeTrue();
    }

    [Fact]
    public async Task LoginAsync_WhenUserNotFound_ReturnsFailResponse()
    {
        var request = new LoginRequest
        {
            UsernameOrEmail = "nonexistent",
            Password = "Password123!"
        };

        _unitOfWorkMock.Setup(x => x.Users.GetByUsernameOrEmailAsync(request.UsernameOrEmail))
            .ReturnsAsync((User?)null);

        var result = await _authService.LoginAsync(request);

        result.Should().NotBeNull();
        result.Code.Should().Be(400);
        result.Message.Should().Be("用户名或密码错误");
    }

    [Fact]
    public async Task LoginAsync_WhenPasswordIsIncorrect_ReturnsFailResponse()
    {
        var request = new LoginRequest
        {
            UsernameOrEmail = "testuser",
            Password = "WrongPassword!"
        };

        var user = new User
        {
            Id = 1,
            Username = "testuser",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPassword!")
        };

        _unitOfWorkMock.Setup(x => x.Users.GetByUsernameOrEmailAsync(request.UsernameOrEmail))
            .ReturnsAsync(user);

        var result = await _authService.LoginAsync(request);

        result.Should().NotBeNull();
        result.Code.Should().Be(400);
        result.Message.Should().Be("用户名或密码错误");
    }

    [Fact]
    public async Task LoginAsync_WhenUserIsDisabled_ReturnsForbidden()
    {
        var request = new LoginRequest
        {
            UsernameOrEmail = "testuser",
            Password = "Password123!"
        };

        var user = new User
        {
            Id = 1,
            Username = "testuser",
            IsActive = false,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)
        };

        _unitOfWorkMock.Setup(x => x.Users.GetByUsernameOrEmailAsync(request.UsernameOrEmail))
            .ReturnsAsync(user);

        var result = await _authService.LoginAsync(request);

        result.Should().NotBeNull();
        result.Code.Should().Be(403);
        result.Message.Should().Be("账户已被禁用");
    }

    [Fact]
    public async Task LoginAsync_WhenCredentialsAreValid_ReturnsSuccessWithToken()
    {
        var request = new LoginRequest
        {
            UsernameOrEmail = "testuser",
            Password = "Password123!"
        };

        var user = new User
        {
            Id = 1,
            Username = "testuser",
            Email = "test@example.com",
            IsActive = true,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)
        };

        _unitOfWorkMock.Setup(x => x.Users.GetByUsernameOrEmailAsync(request.UsernameOrEmail))
            .ReturnsAsync(user);
        _mapperMock.Setup(x => x.Map<UserResponse>(user))
            .Returns(new UserResponse { Id = 1, Username = "testuser", Email = "test@example.com" });

        var result = await _authService.LoginAsync(request);

        result.Should().NotBeNull();
        result.Code.Should().Be(200);
        result.Message.Should().Be("登录成功");
        result.Data.Should().NotBeNull();

        var data = (dynamic)result.Data!;
        ((string)data.Token).Should().NotBeNullOrEmpty();
        ((UserResponse)data.User).Username.Should().Be("testuser");
    }

    [Theory]
    [InlineData("Password123!")]
    [InlineData("MySecurePass456!")]
    [InlineData("TestPass_789")]
    public void BCryptHash_VerifyCorrectPassword_ReturnsTrue(string password)
    {
        var hash = BCrypt.Net.BCrypt.HashPassword(password);
        BCrypt.Net.BCrypt.Verify(password, hash).Should().BeTrue();
    }

    [Theory]
    [InlineData("Password123!", "WrongPassword!")]
    [InlineData("MySecurePass456!", "mysecurepass456!")]
    public void BCryptHash_VerifyWrongPassword_ReturnsFalse(string correctPassword, string wrongPassword)
    {
        var hash = BCrypt.Net.BCrypt.HashPassword(correctPassword);
        BCrypt.Net.BCrypt.Verify(wrongPassword, hash).Should().BeFalse();
    }

    [Fact]
    public void BCryptHash_SamePasswordProducesDifferentHashes()
    {
        var password = "Password123!";
        var hash1 = BCrypt.Net.BCrypt.HashPassword(password);
        var hash2 = BCrypt.Net.BCrypt.HashPassword(password);

        hash1.Should().NotBe(hash2);
        BCrypt.Net.BCrypt.Verify(password, hash1).Should().BeTrue();
        BCrypt.Net.BCrypt.Verify(password, hash2).Should().BeTrue();
    }
}
