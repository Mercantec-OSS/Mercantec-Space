namespace Backend.DiscordBot.Commands;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Backend.Data;
using Backend.DiscordServices.Services;
using Backend.Models;
using Discord;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

public partial class Commands
{
    private static IServiceProvider _serviceProvider;

    // Sæt service provider
    public static void SetServiceProvider(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    // Dictionary til at holde alle kommandoer
    private static readonly Dictionary<
        string,
        Func<SocketMessage, DiscordSocketClient, Task>
    > _commands =
        new()
        {
            { "ping", PingCommand },
            { "hello", HejCommand },
            { "help", HjælpCommand },
            { "info", InfoCommand },
            { "rank", RankCommand },
            { "givexp", GiveXPCommand },
            { "testxp", TestXPCommand },
            { "daily", DailyCommand },
            { "leaderboard", LeaderboardCommand }
        };

    // Metode til at hente alle kommandoer
    public static Dictionary<string, Func<SocketMessage, DiscordSocketClient, Task>> GetCommands()
    {
        return _commands;
    }

    // Metode til at tjekke om en kommando eksisterer
    public static bool CommandExists(string command)
    {
        return _commands.ContainsKey(command.ToLower());
    }

    // Metode til at udføre en kommando
    public static async Task ExecuteCommand(
        string command,
        SocketMessage message,
        DiscordSocketClient client
    )
    {
        if (CommandExists(command))
        {
            await _commands[command.ToLower()](message, client);
        }
    }

    // Ping kommando - !ping
    private static async Task PingCommand(SocketMessage message, DiscordSocketClient client)
    {
        await message.Channel.SendMessageAsync("Pong! 🏓");
    }

    // Hej kommando
    private static async Task HejCommand(SocketMessage message, DiscordSocketClient client)
    {
        await message.Channel.SendMessageAsync($"Hej {message.Author.Mention}! 👋");
    }

    // Hjælp kommando - !help
    private static async Task HjælpCommand(SocketMessage message, DiscordSocketClient client)
    {
        string prefix = "!";

        var embedBuilder = new EmbedBuilder()
            .WithTitle("Kommando Hjælp")
            .WithDescription("Her er en liste over alle tilgængelige kommandoer:")
            .WithColor(Color.Blue)
            .AddField($"{prefix}ping", "Tjek om botten er online")
            .AddField($"{prefix}hello", "Få en hilsen fra botten")
            .AddField($"{prefix}help", "Vis denne hjælpebesked")
            .AddField($"{prefix}info", "Vis information om botten")
            .AddField($"{prefix}rank", "Vis din rang og XP")
            .AddField($"{prefix}givexp", "Giv XP til en bruger")
            .AddField($"{prefix}testxp", "Test XP-systemet")
            .AddField($"{prefix}daily", "Få daglig XP-bonus")
            .AddField($"{prefix}leaderboard", "Vis top 5 brugere og din placering")
            .WithFooter(footer => footer.Text = "Brug præfiks ! før hver kommando");

        await message.Channel.SendMessageAsync(embed: embedBuilder.Build());
    }

    // Info kommando - !info
    private static async Task InfoCommand(SocketMessage message, DiscordSocketClient client)
    {
        var embed = new EmbedBuilder()
            .WithTitle("Bot Information")
            .WithDescription("En simpel Discord bot lavet med Discord.NET")
            .WithColor(Color.Blue)
            .WithCurrentTimestamp()
            .WithFooter(footer => footer.Text = "Lavet af Mercantec Space Teamet")
            .AddField("Servere", client.Guilds.Count, true)
            .AddField("Ping", $"{client.Latency} ms", true)
            .Build();

        await message.Channel.SendMessageAsync(embed: embed);
    }

    // Rank kommando - !rank
    private static async Task RankCommand(SocketMessage message, DiscordSocketClient client)
    {
        if (_serviceProvider == null)
        {
            await message.Channel.SendMessageAsync("Fejl: Service provider er ikke konfigureret.");
            return;
        }

        using (var scope = _serviceProvider.CreateScope())
        {
            var xpService = scope.ServiceProvider.GetRequiredService<XPService>();
            var progress = await xpService.GetUserProgressAsync(message.Author.Id.ToString());
            var stats = await xpService.GetUserActivityStatsAsync(message.Author.Id.ToString());

            var embed = new EmbedBuilder()
                .WithTitle($"{message.Author.Username}'s Rank")
                .WithDescription(
                    $"Level: {progress.Level}\nXP: {progress.XP}/{progress.RequiredXP}"
                )
                .WithColor(Color.Gold)
                .WithThumbnailUrl(
                    message.Author.GetAvatarUrl() ?? message.Author.GetDefaultAvatarUrl()
                )
                .WithCurrentTimestamp();

            // Tilføj daglige aktiviteter
            var activityField = "";
            foreach (var stat in stats)
            {
                activityField += $"{stat.Key}: {stat.Value} i dag\n";
            }

            if (!string.IsNullOrEmpty(activityField))
            {
                embed.AddField("Dagens aktiviteter", activityField);
            }

            await message.Channel.SendMessageAsync(embed: embed.Build());
        }
    }

    // GiveXP kommando - !givexp
    private static async Task GiveXPCommand(SocketMessage message, DiscordSocketClient client)
    {
        if (_serviceProvider == null)
        {
            await message.Channel.SendMessageAsync("Fejl: Service provider er ikke konfigureret.");
            return;
        }

        using (var scope = _serviceProvider.CreateScope())
        {
            var xpService = scope.ServiceProvider.GetRequiredService<XPService>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Commands>>();

            try
            {
                // Giv XP for en besked
                bool success = await xpService.AddXPAsync(
                    message.Author.Id.ToString(),
                    XPActivityType.Message
                );

                if (success)
                {
                    var progress = await xpService.GetUserProgressAsync(
                        message.Author.Id.ToString()
                    );
                    await message.Channel.SendMessageAsync(
                        $"Du fik XP! Du er nu level {progress.Level} med {progress.XP}/{progress.RequiredXP} XP."
                    );
                }
                else
                {
                    await message.Channel.SendMessageAsync(
                        "Kunne ikke give XP. Tjek logs for detaljer."
                    );
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Fejl ved givexp kommando");
                await message.Channel.SendMessageAsync($"Der opstod en fejl: {ex.Message}");
            }
        }
    }

    // TestXP kommando - !testxp
    private static async Task TestXPCommand(SocketMessage message, DiscordSocketClient client)
    {
        if (_serviceProvider == null)
        {
            await message.Channel.SendMessageAsync("Fejl: Service provider er ikke konfigureret.");
            return;
        }

        using (var scope = _serviceProvider.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Commands>>();
            var userService = scope.ServiceProvider.GetRequiredService<UserService>();

            try
            {
                // Find brugeren
                var user = await userService.GetUserByDiscordIdAsync(message.Author.Id.ToString());
                if (user == null)
                {
                    await message.Channel.SendMessageAsync(
                        "Du er ikke registreret. Brug !register først."
                    );
                    return;
                }

                // Tilføj XP direkte til brugeren
                user.Experience += 10;
                dbContext.Users.Update(user);

                // Opret en aktivitetspost direkte
                var dailyActivity = new UserDailyActivity
                {
                    UserId = user.Id,
                    ActivityType = "TestXP",
                    Date = DateTime.UtcNow.Date,
                    Count = 1,
                    TotalXPAwarded = 10,
                    LastActivity = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow
                };

                dbContext.Set<UserDailyActivity>().Add(dailyActivity);

                // Gem ændringerne
                await dbContext.SaveChangesAsync();

                // Bekræft at det virkede
                await message.Channel.SendMessageAsync(
                    $"Test XP tilføjet! Du har nu {user.Experience} XP. Tjek !rank for at se om aktiviteten blev registreret."
                );
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Fejl ved testxp kommando");
                await message.Channel.SendMessageAsync($"Der opstod en fejl: {ex.Message}");
            }
        }
    }

    // Daily kommando - !daily (opdateret)
    private static async Task DailyCommand(SocketMessage message, DiscordSocketClient client)
    {
        if (_serviceProvider == null)
        {
            await message.Channel.SendMessageAsync("Fejl: Service provider er ikke konfigureret.");
            return;
        }

        using (var scope = _serviceProvider.CreateScope())
        {
            var xpService = scope.ServiceProvider.GetRequiredService<XPService>();

            try
            {
                bool success = await xpService.AddXPAsync(
                    message.Author.Id.ToString(),
                    XPActivityType.DailyLogin
                );

                if (success)
                {
                    var progress = await xpService.GetUserProgressAsync(
                        message.Author.Id.ToString()
                    );
                    await message.Channel.SendMessageAsync(
                        $"Du har fået din daglige XP bonus! Du er nu level {progress.Level} med {progress.XP}/{progress.RequiredXP} XP."
                    );
                }
                else
                {
                    await message.Channel.SendMessageAsync(
                        "Du har allerede fået din daglige XP bonus i dag. Kom tilbage i morgen!"
                    );
                }
            }
            catch (Exception ex)
            {
                await message.Channel.SendMessageAsync($"Der opstod en fejl: {ex.Message}");
            }
        }
    }

    // Leaderboard kommando - !leaderboard
    private static async Task LeaderboardCommand(SocketMessage message, DiscordSocketClient client)
    {
        if (_serviceProvider == null)
        {
            await message.Channel.SendMessageAsync("Fejl: Service provider er ikke konfigureret.");
            return;
        }

        using (var scope = _serviceProvider.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Commands>>();
            var levelSystem = scope.ServiceProvider.GetRequiredService<LevelSystem>();

            try
            {
                // Hent top 5 brugere sorteret efter XP - ændret for at undgå GetValueOrDefault
                var topUsers = await dbContext
                    .Users.Where(u => u.IsBot == null || u.IsBot == false) // Ændret fra GetValueOrDefault
                    .OrderByDescending(u => u.Experience)
                    .Take(5)
                    .Select(u => new
                    {
                        u.Id,
                        u.Username,
                        u.GlobalName,
                        u.DiscordId,
                        u.Experience,
                        u.Level,
                        RequiredXP = levelSystem.CalculateRequiredXP(u.Level)
                    })
                    .ToListAsync();

                // Find brugerens data
                var userData = await dbContext
                    .Users.Where(u => u.DiscordId == message.Author.Id.ToString())
                    .Select(u => new
                    {
                        u.Username,
                        u.GlobalName,
                        u.Experience,
                        u.Level,
                        RequiredXP = levelSystem.CalculateRequiredXP(u.Level)
                    })
                    .FirstOrDefaultAsync();

                // Hvis brugeren ikke findes, vis en fejlmeddelelse
                if (userData == null)
                {
                    await message.Channel.SendMessageAsync(
                        "Du er ikke registreret. Brug !register først."
                    );
                    return;
                }

                // Find brugerens position - ændret for at undgå GetValueOrDefault
                var userPosition = await dbContext
                    .Users.Where(u =>
                        (u.IsBot == null || u.IsBot == false) && u.Experience >= userData.Experience
                    )
                    .CountAsync();

                // Byg embed besked
                var embed = new EmbedBuilder()
                    .WithTitle("🏆 XP Leaderboard 🏆")
                    .WithDescription($"Top {topUsers.Count} brugere med mest XP")
                    .WithColor(Color.Gold)
                    .WithCurrentTimestamp();

                // Tilføj top brugere
                for (int i = 0; i < topUsers.Count; i++)
                {
                    var user = topUsers[i];
                    string displayName = !string.IsNullOrEmpty(user.GlobalName)
                        ? user.GlobalName
                        : user.Username;
                    string medal =
                        i == 0
                            ? "🥇"
                            : i == 1
                                ? "🥈"
                                : i == 2
                                    ? "🥉"
                                    : "🏅";

                    // Marker hvis det er brugeren selv
                    string userIndicator =
                        user.DiscordId == message.Author.Id.ToString() ? " (Dig)" : "";

                    embed.AddField(
                        $"{medal} #{i + 1} {displayName}{userIndicator}",
                        $"Level: {user.Level} | XP: {user.Experience}/{user.RequiredXP}"
                    );
                }

                // Tilføj en separator
                if (userPosition > 5 && userData != null)
                {
                    embed.AddField("⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯", "⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯");

                    // Tilføj brugerens position
                    string displayName = !string.IsNullOrEmpty(userData.GlobalName)
                        ? userData.GlobalName
                        : userData.Username;
                    embed.AddField(
                        $"#{userPosition} {displayName} (Dig)",
                        $"Level: {userData.Level} | XP: {userData.Experience}/{userData.RequiredXP}"
                    );
                }

                await message.Channel.SendMessageAsync(embed: embed.Build());
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Fejl ved leaderboard kommando");
                await message.Channel.SendMessageAsync($"Der opstod en fejl: {ex.Message}");
            }
        }
    }
}
