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
            _client.ReactionAdded += async (cache, channel, reaction) => { await _commandHandler.ReactionAddedHandler(guild, cache, channel, reaction); };
            _client.ReactionRemoved += async (cache, channel, reaction) => { await _commandHandler.ReactionRemovedHandler(guild, cache, channel, reaction); };
            _client.UserVoiceStateUpdated += async (user, before, after) => await _commandHandler.VoiceStateUpdatedAsync(user, before, after, guild);
            _client.ButtonExecuted += _commandHandler.ButtonHandler;
            
            _client.GuildMemberUpdated += async (before, after) => await _logHandler.LogMemberUpdate(before, after, guild);
            _client.InviteCreated += async (invite) => await _logHandler.LogInvite(invite, guild);
            _client.UserJoined += async (user) => await _logHandler.LogUserJoined(user, guild);
            _client.UserLeft += async (userGuild, user) => await _logHandler.LogMemberLeft(userGuild, user);
            _client.UserBanned += async (user, userGuild) => await _logHandler.LogMemberBanned(user, userGuild);
            _client.MessageDeleted += async (message, messageChannel) => await _logHandler.LogMessageDelete(message, messageChannel, guild);
            _client.MessageUpdated += async (beforemessage, aftermessage, messageChannel) => await _logHandler.LogMessageUpdate(beforemessage, aftermessage, messageChannel, guild);
            _client.WebhooksUpdated += async (userGuild, channel) => await _logHandler.LogWebhookUpdate(userGuild, channel);
            
            _ = Task.Run(async () => {
                await _logHandler.CreateOrUpdateStatChannel(guild);
                await _commandHandler.RegisterCommands(guild);
                await _client.SetActivityAsync(new CustomStatusGame("Helping " +  guild.Users.Count(u => !u.IsBot) + " students..."));
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
