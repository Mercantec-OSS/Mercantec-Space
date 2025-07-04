namespace Backend.Services;

using Backend.Data;
using Backend.DBAccess;
using Backend.Models;
using Backend.Models.DTOs;
using BCrypt.Net;
using Microsoft.EntityFrameworkCore;

public class AuthService
{

    private readonly AuthDBAccess _authDBAccess;
    private readonly JwtService _jwtService;

    public AuthService(JwtService jwtService, AuthDBAccess authDBAccess)
    {
        _authDBAccess = authDBAccess;
        _jwtService = jwtService;
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request)
    {
        // Find bruger via email eller username
        var user = await _authDBAccess.Login(request);

        if (user == null || !BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return null;
        }

        if (!user.IsActive)
        {
            throw new UnauthorizedAccessException("Brugerkonto er deaktiveret");
        }

        // Generer tokens
        var accessToken = _jwtService.GenerateAccessToken(user);
        var refreshToken = await _jwtService.GenerateRefreshTokenAsync(user.Id);

        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(60), // Dette skal matche JWT config
            User = MapToUserDto(user)
        };
    }

    public async Task<AuthResponse?> RegisterAsync(RegisterRequest request)
    {
        // Tjek om email eller username allerede eksisterer
        if (await _authDBAccess.CheckForExistingUser(request))
        {
            throw new InvalidOperationException("Email eller brugernavn er allerede i brug");
        }

        // Tjek om Discord ID allerede er linket
        if (!string.IsNullOrEmpty(request.DiscordId))
        {
            
            if (await _authDBAccess.CheckForExistingDiscordIdLink(request))
            {
                throw new InvalidOperationException("Discord konto er allerede linket til en anden bruger");
            }
        }

        // Hash password
        var passwordHash = BCrypt.HashPassword(request.Password);

        User user;

        // Hvis Discord ID er angivet, prøv at finde og opdatere eksisterende Discord bruger
        if (!string.IsNullOrEmpty(request.DiscordId))
        {
            var discordUser = await _authDBAccess.GetDiscordUser(request.DiscordId);

            if (discordUser != null)
            {
                // Opdater eksisterende Discord bruger med login info
                discordUser.Email = request.Email;
                discordUser.Username = request.Username;
                discordUser.PasswordHash = passwordHash;
                discordUser.EmailConfirmed = false;
                discordUser.LastUpdated = DateTime.UtcNow;

                user = discordUser;

                await _authDBAccess.UpdateUser(discordUser);
            }
            else
            {
                // Opret ny bruger med Discord ID
                user = new User
                {
                    Email = request.Email,
                    Username = request.Username,
                    PasswordHash = passwordHash,
                    DiscordId = request.DiscordId,
                    EmailConfirmed = false,
                    CreatedAt = DateTime.UtcNow,
                    LastUpdated = DateTime.UtcNow,
                    IsActive = true,
                    Experience = 0,
                    Level = 1,
                    Roles = new List<string> { UserRole.Student.ToString() }
                };

                await _authDBAccess.AddUser(user);
            }
        }
        else
        {
            // Opret ny bruger uden Discord
            user = new User
            {
                Email = request.Email,
                Username = request.Username,
                PasswordHash = passwordHash,
                EmailConfirmed = false,
                CreatedAt = DateTime.UtcNow,
                LastUpdated = DateTime.UtcNow,
                IsActive = true,
                Experience = 0,
                Level = 1,
                Roles = new List<string> { UserRole.Student.ToString() }
            };

            await _authDBAccess.AddUser(user);
        }


        // Generer tokens
        var accessToken = _jwtService.GenerateAccessToken(user);
        var refreshToken = await _jwtService.GenerateRefreshTokenAsync(user.Id);

        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(60),
            User = MapToUserDto(user)
        };
    }

    public async Task<bool> LinkDiscordAsync(string userId, string discordId)
    {
        var user = await _authDBAccess.GetUser(userId);
        if (user == null)
            return false;

        // Tjek om Discord ID allerede er i brug
        var existingDiscordUser = await _authDBAccess.GetDiscordUser(discordId);

        if (existingDiscordUser != null && existingDiscordUser.Id != userId)
        {
            // Hvis der eksisterer en Discord-only bruger (uden email/password), merge med den nuværende bruger
            if (string.IsNullOrEmpty(existingDiscordUser.Email) && string.IsNullOrEmpty(existingDiscordUser.PasswordHash))
            {
                // Kopier Discord data til den nuværende bruger
                user.DiscordId = existingDiscordUser.DiscordId;
                user.GlobalName = existingDiscordUser.GlobalName;
                user.Discriminator = existingDiscordUser.Discriminator;
                user.AvatarUrl = existingDiscordUser.AvatarUrl;
                user.Nickname = existingDiscordUser.Nickname;
                user.IsBot = existingDiscordUser.IsBot;
                user.PublicFlags = existingDiscordUser.PublicFlags;
                user.JoinedAt = existingDiscordUser.JoinedAt;
                user.IsBoosting = existingDiscordUser.IsBoosting;
                user.Experience = Math.Max(user.Experience, existingDiscordUser.Experience);
                user.Level = Math.Max(user.Level, existingDiscordUser.Level);
                user.LastUpdated = DateTime.UtcNow;

                // Slet den gamle Discord-only bruger
                await _authDBAccess.DeleteUser(existingDiscordUser);
            }
            else
            {
                return false; // Discord konto er allerede linket til en anden fuld bruger
            }
        }
        else
        {
            user.DiscordId = discordId;
            user.LastUpdated = DateTime.UtcNow;
        }

        await _authDBAccess.UpdateUser(user);
        return true;
    }

    public async Task<AuthResponse?> RefreshTokenAsync(string refreshToken)
    {
        var user = await _jwtService.ValidateRefreshTokenAsync(refreshToken);
        if (user == null)
            return null;

        // Revoke den gamle token
        await _jwtService.RevokeRefreshTokenAsync(refreshToken);

        // Generer nye tokens
        var newAccessToken = _jwtService.GenerateAccessToken(user);
        var newRefreshToken = await _jwtService.GenerateRefreshTokenAsync(user.Id);

        return new AuthResponse
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(60),
            User = MapToUserDto(user)
        };
    }

    public async Task<bool> LogoutAsync(string refreshToken)
    {
        await _jwtService.RevokeRefreshTokenAsync(refreshToken);
        return true;
    }

    public async Task<bool> UnlinkDiscordAsync(string userId)
    {
        var user = await _authDBAccess.GetUser(userId);
        if (user == null)
            return false;

        if (string.IsNullOrEmpty(user.DiscordId))
            return false;

        // Fjern Discord felter
        user.DiscordId = null;
        user.GlobalName = null;
        user.Discriminator = null;
        user.AvatarUrl = null;
        user.Nickname = null;
        user.IsBot = null;
        user.PublicFlags = null;
        user.JoinedAt = null;
        user.IsBoosting = null;
        user.LastUpdated = DateTime.UtcNow;

        await _authDBAccess.UpdateUser(user);
        return true;
    }

    private static UserDto MapToUserDto(User user)    {        return new UserDto        {            Id = user.Id,            Email = user.Email,            Username = user.Username,            DiscordId = user.DiscordId,            GlobalName = user.GlobalName,            AvatarUrl = user.AvatarUrl,            Experience = user.Experience,            Level = user.Level,            Roles = user.Roles,            IsActive = user.IsActive,            CreatedAt = user.CreatedAt        };    }
} 