using JobRadar.Application.Abstractions;
using JobRadar.Application.Common;
using JobRadar.Application.UseCases.Auth;
using JobRadar.Domain.Entities;
using JobRadar.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace JobRadar.Tests.Handlers;

public class SessionHandlerTests
{
    private readonly Mock<IRefreshTokenRepository> _refreshRepo = new();
    private readonly Mock<IUserRepository> _userRepo = new();
    private readonly Mock<ITokenService> _tokenService = new();

    public SessionHandlerTests()
    {
        _tokenService.Setup(t => t.GenerateToken(It.IsAny<User>())).Returns("access");
        _tokenService.Setup(t => t.GenerateRefreshToken()).Returns("raw-new");
    }

    [Fact]
    public async Task Refresh_WithActiveToken_RotatesAndReturnsNewPair()
    {
        var user = User.CreateWithPassword("Ana", "ana@exemplo.com", "hash");
        var stored = RefreshToken.Create(user.Id, TokenHasher.Hash("raw-old"), DateTime.UtcNow.AddDays(1));
        _refreshRepo.Setup(r => r.GetByHashAsync(TokenHasher.Hash("raw-old"))).ReturnsAsync(stored);
        _userRepo.Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync(user);

        var handler = new RefreshTokenCommandHandler(_refreshRepo.Object, _userRepo.Object, _tokenService.Object);
        var result = await handler.Handle(new RefreshTokenCommand("raw-old"), default);

        Assert.Equal("access", result.Token);
        Assert.Equal("raw-new", result.RefreshToken);
        Assert.True(stored.WasRevoked);
        Assert.Equal(TokenHasher.Hash("raw-new"), stored.ReplacedByTokenHash);
    }

    [Fact]
    public async Task Refresh_WithRevokedToken_RevokesAllSessions()
    {
        var user = User.CreateWithPassword("Ana", "ana@exemplo.com", "hash");
        var stored = RefreshToken.Create(user.Id, TokenHasher.Hash("raw-old"), DateTime.UtcNow.AddDays(1));
        stored.Revoke();
        _refreshRepo.Setup(r => r.GetByHashAsync(TokenHasher.Hash("raw-old"))).ReturnsAsync(stored);

        var handler = new RefreshTokenCommandHandler(_refreshRepo.Object, _userRepo.Object, _tokenService.Object);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            handler.Handle(new RefreshTokenCommand("raw-old"), default));
        _refreshRepo.Verify(r => r.RevokeAllForUserAsync(user.Id), Times.Once);
    }

    [Fact]
    public async Task ResetPassword_WithValidToken_ChangesPasswordAndRevokesSessions()
    {
        var user = User.CreateWithPassword("Ana", "ana@exemplo.com", BCrypt.Net.BCrypt.HashPassword("old-pass-123"));
        user.RequestPasswordReset(TokenHasher.Hash("reset-tok"), DateTime.UtcNow.AddHours(1));
        _userRepo.Setup(r => r.GetByEmailAsync("ana@exemplo.com")).ReturnsAsync(user);

        var handler = new ResetPasswordCommandHandler(_userRepo.Object, _refreshRepo.Object);
        await handler.Handle(new ResetPasswordCommand("ana@exemplo.com", "reset-tok", "new-pass-456"), default);

        Assert.True(BCrypt.Net.BCrypt.Verify("new-pass-456", user.PasswordHash));
        Assert.False(user.HasValidPasswordResetToken);
        _refreshRepo.Verify(r => r.RevokeAllForUserAsync(user.Id), Times.Once);
    }

    [Fact]
    public async Task ForgotPassword_WithUnknownEmail_DoesNotSendEmail()
    {
        _userRepo.Setup(r => r.GetByEmailAsync("ninguem@exemplo.com")).ReturnsAsync((User?)null);
        var emailService = new Mock<IEmailService>();
        var config = new Mock<IConfiguration>();
        config.Setup(c => c["Frontend:BaseUrl"]).Returns("https://example.com/");

        var handler = new ForgotPasswordCommandHandler(
            _userRepo.Object, _tokenService.Object, emailService.Object, config.Object,
            NullLogger<ForgotPasswordCommandHandler>.Instance);

        await handler.Handle(new ForgotPasswordCommand("ninguem@exemplo.com"), default);

        emailService.Verify(e => e.SendPasswordResetAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task GetMe_ReturnsProfileFlags()
    {
        var user = User.CreateWithGoogle("Ana", "ana@exemplo.com", "google-1");
        _userRepo.Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync(user);

        var handler = new GetMeQueryHandler(_userRepo.Object);
        var result = await handler.Handle(new GetMeQuery(user.Id), default);

        Assert.Equal("ana@exemplo.com", result.Email);
        Assert.False(result.HasPassword);
        Assert.True(result.HasGoogleAccount);
    }
}
