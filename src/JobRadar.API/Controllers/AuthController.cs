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

namespace JobRadar.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IUserRepository _userRepository;
    private readonly ITokenService _tokenService;

    public AuthController(IMediator mediator, IUserRepository userRepository, ITokenService tokenService)
    {
        _mediator = mediator;
        _userRepository = userRepository;
        _tokenService = tokenService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        try
        {
            var result = await _mediator.Send(new RegisterCommand(request.Name, request.Email, request.Password));
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            var result = await _mediator.Send(new LoginCommand(request.Email, request.Password));
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
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
    public async Task<IActionResult> GoogleCallback()
    {
        var result = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);
        if (!result.Succeeded) return Unauthorized();

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
        }

        var token = _tokenService.GenerateToken(user);
        return Ok(new AuthResponse(token, user.Name, user.Email));
    }
}