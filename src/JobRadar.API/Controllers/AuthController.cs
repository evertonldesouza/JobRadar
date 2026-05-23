using JobRadar.Application.DTOs;
using JobRadar.Application.UseCases.Auth;
using JobRadar.Domain.Entities;
using JobRadar.Domain.Interfaces;
using JobRadar.Application.Abstractions;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;

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

    public AuthController(IMediator mediator, IUserRepository userRepository, ITokenService tokenService, ILogger<AuthController> logger)
    {
        _mediator = mediator;
        _userRepository = userRepository;
        _tokenService = tokenService;
        _logger = logger;
    }

    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var result = await _mediator.Send(new RegisterCommand(request.Name, request.Email, request.Password));
        _logger.LogInformation("Novo usuário registrado: {Email}", request.Email);
        return Ok(result);
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await _mediator.Send(new LoginCommand(request.Email, request.Password));
        _logger.LogInformation("Login realizado: {Email}", request.Email);
        return Ok(result);
    }

    [HttpGet("google")]
    public IActionResult GoogleLogin([FromQuery] string? returnUrl = null)
    {
        var properties = new AuthenticationProperties
        {
            RedirectUri = Url.Action(nameof(GoogleCallback)),
            Items = { { "returnUrl", returnUrl } }
        };
        return Challenge(properties, GoogleDefaults.AuthenticationScheme);
    }

    [HttpGet("google/callback")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GoogleCallback()
    {
        var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        if (!result.Succeeded)
        {
            _logger.LogWarning("Falha na autenticação Google");
            return Unauthorized();
        }

        var googleId = result.Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
        var email = result.Principal.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty;
        var name = result.Principal.FindFirst(ClaimTypes.Name)?.Value ?? string.Empty;

        var user = await _userRepository.GetByGoogleIdAsync(googleId)
                ?? await _userRepository.GetByEmailAsync(email);

        if (user is null)
        {
            var newUser = Domain.Entities.User.CreateWithGoogle(name, email, googleId);
            await _userRepository.AddAsync(newUser);
            user = newUser;
            _logger.LogInformation("Novo usuário via Google: {Email}", email);
        }

        var token = _tokenService.GenerateToken(user);
        return Ok(new AuthResponse(token, user.Name, user.Email));
    }
}