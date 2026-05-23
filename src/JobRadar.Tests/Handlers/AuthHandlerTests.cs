using JobRadar.Application.Abstractions;
using JobRadar.Application.UseCases.Auth;
using JobRadar.Domain.Entities;
using JobRadar.Domain.Interfaces;
using Moq;

namespace JobRadar.Tests.Handlers;

public class AuthHandlerTests
{
    private readonly Mock<IUserRepository> _userRepo;
    private readonly Mock<ITokenService> _tokenService;
    private readonly RegisterCommandHandler _registerHandler;
    private readonly LoginCommandHandler _loginHandler;

    public AuthHandlerTests()
    {
        _userRepo = new Mock<IUserRepository>();
        _tokenService = new Mock<ITokenService>();
        _registerHandler = new RegisterCommandHandler(_userRepo.Object, _tokenService.Object);
        _loginHandler = new LoginCommandHandler(_userRepo.Object, _tokenService.Object);
    }

    [Fact]
    public async Task Register_WithNewEmail_ReturnsAuthResponse()
    {
        _userRepo.Setup(r => r.GetByEmailAsync("test@email.com")).ReturnsAsync((User?)null);
        _tokenService.Setup(t => t.GenerateToken(It.IsAny<User>())).Returns("fake-token");

        var result = await _registerHandler.Handle(new RegisterCommand("Test", "test@email.com", "123456"), default);

        Assert.Equal("Test", result.Name);
        Assert.Equal("test@email.com", result.Email);
        Assert.Equal("fake-token", result.Token);
    }

    [Fact]
    public async Task Register_WithExistingEmail_ThrowsInvalidOperation()
    {
        var existing = User.CreateWithPassword("Existing", "test@email.com", "hash");
        _userRepo.Setup(r => r.GetByEmailAsync("test@email.com")).ReturnsAsync(existing);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _registerHandler.Handle(new RegisterCommand("Test", "test@email.com", "123456"), default));
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsAuthResponse()
    {
        var passwordHash = BCrypt.Net.BCrypt.HashPassword("123456");
        var user = User.CreateWithPassword("Test", "test@email.com", passwordHash);
        _userRepo.Setup(r => r.GetByEmailAsync("test@email.com")).ReturnsAsync(user);
        _tokenService.Setup(t => t.GenerateToken(user)).Returns("fake-token");

        var result = await _loginHandler.Handle(new LoginCommand("test@email.com", "123456"), default);

        Assert.Equal("Test", result.Name);
        Assert.Equal("fake-token", result.Token);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ThrowsUnauthorizedAccess()
    {
        var passwordHash = BCrypt.Net.BCrypt.HashPassword("correct");
        var user = User.CreateWithPassword("Test", "test@email.com", passwordHash);
        _userRepo.Setup(r => r.GetByEmailAsync("test@email.com")).ReturnsAsync(user);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _loginHandler.Handle(new LoginCommand("test@email.com", "wrong"), default));
    }

    [Fact]
    public async Task Login_WithNonExistentEmail_ThrowsUnauthorizedAccess()
    {
        _userRepo.Setup(r => r.GetByEmailAsync("notfound@email.com")).ReturnsAsync((User?)null);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _loginHandler.Handle(new LoginCommand("notfound@email.com", "123456"), default));
    }
}
