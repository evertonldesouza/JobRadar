using System.Security.Claims;
using JobRadar.Application.Abstractions;
using JobRadar.Application.Common;
using JobRadar.Application.DTOs;
using JobRadar.Application.UseCases.Auth;
using JobRadar.Domain.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace JobRadar.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IUserRepository _userRepository;
    private readonly ITokenService _tokenService;
    private readonly ILogger<AuthController> _logger;
    private readonly IConfiguration _configuration;

    public AuthController(IMediator mediator, IUserRepository userRepository, ITokenService tokenService, ILogger<AuthController> logger, IConfiguration configuration)
    {
        _mediator = mediator;
        _userRepository = userRepository;
        _tokenService = tokenService;
        _logger = logger;
        _configuration = configuration;
    }

    private string? FrontendBaseUrl => _configuration["Frontend:BaseUrl"];

    [HttpPost("register")]
    [EnableRateLimiting("Auth")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var result = await _mediator.Send(new RegisterCommand(request.Name, request.Email, request.Password));
        _logger.LogInformation("Novo usuário registrado");
        return Ok(result);
    }

    [HttpPost("login")]
    [EnableRateLimiting("Auth")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await _mediator.Send(new LoginCommand(request.Email, request.Password));
        _logger.LogInformation("Login realizado");
        return Ok(result);
    }

    [HttpGet("google")]
    public IActionResult GoogleLogin()
    {
        var properties = new AuthenticationProperties
        {
            RedirectUri = Url.Action(nameof(GoogleCallback))
        };
        return Challenge(properties, GoogleDefaults.AuthenticationScheme);
    }

    [HttpGet("google/callback")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GoogleCallback()
    {
        var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        if (!result.Succeeded)
        {
            _logger.LogWarning("Falha na autenticação Google");
            var errorRedirect = FrontendRedirect.BuildAuthRedirect(FrontendBaseUrl, null, error: "google");
            return errorRedirect is not null ? Redirect(errorRedirect) : Unauthorized();
        }

        var googleId = result.Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var email = result.Principal.FindFirst(ClaimTypes.Email)?.Value;
        var name = result.Principal.FindFirst(ClaimTypes.Name)?.Value;

        if (string.IsNullOrWhiteSpace(googleId) || string.IsNullOrWhiteSpace(email))
        {
            _logger.LogWarning("Callback do Google sem identificador ou email");
            var errorRedirect = FrontendRedirect.BuildAuthRedirect(FrontendBaseUrl, null, error: "google");
            return errorRedirect is not null ? Redirect(errorRedirect) : Unauthorized();
        }

        var user = await _userRepository.GetByGoogleIdAsync(googleId)
                ?? await _userRepository.GetByEmailAsync(email);

        if (user is null)
        {
            user = Domain.Entities.User.CreateWithGoogle(
                string.IsNullOrWhiteSpace(name) ? email : name, email, googleId);
            await _userRepository.AddAsync(user);
            _logger.LogInformation("Novo usuário via Google: {UserId}", user.Id);
        }
        else if (!string.Equals(user.GoogleId, googleId, StringComparison.Ordinal))
        {
            // Conta criada por email/senha passa a aceitar login com Google.
            user.LinkGoogleAccount(googleId);
            await _userRepository.UpdateAsync(user);
            _logger.LogInformation("Conta vinculada ao Google: {UserId}", user.Id);
        }

        var token = _tokenService.GenerateToken(user);

        // Navegador (fluxo do site): volta ao frontend com o token no fragmento da URL.
        // Sem Frontend:BaseUrl configurado, mantém a resposta JSON para clientes de API.
        var redirect = FrontendRedirect.BuildAuthRedirect(FrontendBaseUrl, token);
        return redirect is not null
            ? Redirect(redirect)
            : Ok(new AuthResponse(token, user.Name, user.Email));
    }
}
