using System.Collections.Concurrent;
using Discord;
using Discord.WebSocket;
using SMASSB.Commands;
using Microsoft.Extensions.DependencyInjection;

namespace SMASSB;
public class Program {
    
    private DiscordSocketClient _client;
    private ulong _guildId;
    private CommandHandler _commandHandler;
    private LogHandler _logHandler;
    private static IServiceProvider _serviceProvider;
    private ConcurrentDictionary<string, int> _inviteCache = new();

    public static async Task Main()
        => await new Program().RunAsync();

    public async Task RunAsync() {
        
        _serviceProvider = CreateProvider();
        _client = _serviceProvider.GetRequiredService<DiscordSocketClient>();
        _commandHandler = _serviceProvider.GetRequiredService<CommandHandler>();
        _logHandler = _serviceProvider.GetRequiredService<LogHandler>();
        
        var token = Environment.GetEnvironmentVariable("BOT_TOKEN") ?? throw new Exception("BOT_TOKEN environment variable not set.");

        _guildId = ulong.Parse(Environment.GetEnvironmentVariable("GUILD_ID") ?? throw new Exception("GUILD_ID environment variable not set."));
        _client.Log += Log;
        
        _client.Ready += async () => {
    
        var guild = _client.GetGuild(_guildId); 
        var invites = await guild.GetInvitesAsync();
        _inviteCache = new ConcurrentDictionary<string, int>(
            invites.ToDictionary(i => i.Code, i => i.Uses ?? 0)
        );
        
        _client.ButtonExecuted += _commandHandler.ButtonHandler;
        _client.ReactionAdded += (cache, channel, reaction) => { _ = Task.Run(async () => await _commandHandler.ReactionAddedHandler(guild, cache, channel, reaction)); return Task.CompletedTask; };
        _client.ReactionRemoved += (cache, channel, reaction) => { _ = Task.Run(async () => await _commandHandler.ReactionRemovedHandler(guild, cache, channel, reaction)); return Task.CompletedTask; };
        _client.UserVoiceStateUpdated += (user, before, after) => { _ = Task.Run(async () => await _commandHandler.VoiceStateUpdatedAsync(user, before, after, guild)); return Task.CompletedTask; };

        _client.GuildMemberUpdated += (before, after) => { _ = Task.Run(async () => await _logHandler.LogMemberUpdate(before, after, guild)); return Task.CompletedTask; };
        _client.InviteCreated += (invite) => { _ = Task.Run(async () =>
        {
            // _inviteCache[invite.Code] = invite.Uses;
            await _logHandler.LogInvite(invite, guild);
        }); return Task.CompletedTask; };
        _client.UserJoined += (user) => {
            _ = Task.Run(async () => {
                var newInvites = await guild.GetInvitesAsync();
                await _logHandler.LogUserJoined(user, guild, _inviteCache, newInvites);
                // _inviteCache = new ConcurrentDictionary<string, int>(
                //    newInvites.ToDictionary(i => i.Code, i => i.Uses ?? 0)
                //);
            });
            return Task.CompletedTask;
        };
        _client.UserLeft += (userGuild, user) => { _ = Task.Run(async () => await _logHandler.LogMemberLeft(userGuild, user)); return Task.CompletedTask; };
        _client.UserBanned += (user, userGuild) => { _ = Task.Run(async () => await _logHandler.LogMemberBanned(user, userGuild)); return Task.CompletedTask; };
        _client.MessageDeleted += (message, messageChannel) => { _ = Task.Run(async () => await _logHandler.LogMessageDelete(message, messageChannel, guild)); return Task.CompletedTask; };
        _client.MessageUpdated += (beforemessage, aftermessage, messageChannel) => { _ = Task.Run(async () => await _logHandler.LogMessageUpdate(beforemessage, aftermessage, messageChannel, guild)); return Task.CompletedTask; };
        _client.WebhooksUpdated += (userGuild, channel) => { _ = Task.Run(async () => await _logHandler.LogWebhookUpdate(userGuild, channel)); return Task.CompletedTask; };

        _ = Task.Run(async () => {
            await _logHandler.CreateOrUpdateStatChannel(guild);
            await _commandHandler.RegisterCommands(guild);
            await _client.SetActivityAsync(new CustomStatusGame("Helping " + guild.Users.Count(u => !u.IsBot) + " students..."));
            _ = _commandHandler.KickUnEnlisted(guild);
        });
        
    };
        
        await _client.LoginAsync(TokenType.Bot, token);
        await _client.StartAsync();
        
        await Task.Delay(-1);
    }
    
    private static Task Log(LogMessage msg) {
        Console.WriteLine(msg.ToString());
        return Task.CompletedTask;
    }
    
    static IServiceProvider CreateProvider() {
        
        var config = new DiscordSocketConfig
        {
            MessageCacheSize = 100,
            GatewayIntents = GatewayIntents.Guilds
                             | GatewayIntents.GuildMembers
                             | GatewayIntents.GuildMessages
                             | GatewayIntents.GuildMessageReactions
                             | GatewayIntents.GuildVoiceStates
                             | GatewayIntents.GuildInvites
                             | GatewayIntents.MessageContent,
                             AlwaysDownloadUsers = true
        };
        
        return new ServiceCollection()
            .AddSingleton(config)
            .AddSingleton<DiscordSocketClient>()
            .AddSingleton<CommandHandler>()
            .AddSingleton<LogHandler>()
            .AddSingleton<DatabaseService>()
            
            .AddSingleton<RewardSystem>()
            .AddSingleton<RoleSystem>()
            .AddSingleton<MeetingSystem>()
            .AddSingleton<IdSystem>()
            .AddSingleton<PointSystem>()
            .AddSingleton<GeneralSystem>()
            
            .BuildServiceProvider();
    }
}
