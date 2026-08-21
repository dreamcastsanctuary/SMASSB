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
    private ExtraneousHandler _extraneousHandler;
    private LogHandler _logHandler;
    private MeetingSystem _meetingSystem;
    
    private static IServiceProvider _serviceProvider;
    private ConcurrentDictionary<string, int> _inviteCache = new();
    private static readonly HashSet<ulong> _startedLoops = new();
    private static readonly object _startedLoopsLock = new();

    /// <summary>
    /// Where the magic starts.
    /// </summary>
    public static async Task Main() => await new Program().RunAsync();

    /// <summary>
    /// Where the magic runs. This is where the bot logs onto its client.
    /// </summary>
    private async Task RunAsync() {
        
        _serviceProvider = CreateProvider();
        _client = _serviceProvider.GetRequiredService<DiscordSocketClient>();
        _commandHandler = _serviceProvider.GetRequiredService<CommandHandler>();
        _logHandler = _serviceProvider.GetRequiredService<LogHandler>();
        _meetingSystem = _serviceProvider.GetRequiredService<MeetingSystem>();
        
        var token = Environment.GetEnvironmentVariable("BOT_TOKEN") ?? throw new Exception("BOT_TOKEN environment variable not set.");
        _guildId = ulong.Parse(Environment.GetEnvironmentVariable("GUILD_ID") ?? throw new Exception("GUILD_ID environment variable not set."));
        
        _client.Log += Log;
        _client.ButtonExecuted += _extraneousHandler.ButtonHandler;
        _client.MessageReceived += _meetingSystem.HandleMeetingMessage;
        
        _client.ReactionAdded += (cache, channel, reaction) => { _ = Task.Run(async () => await _extraneousHandler.ReactionAddedHandler(_client.GetGuild(_guildId), cache, channel, reaction)); return Task.CompletedTask; };
        _client.ReactionRemoved += (cache, channel, reaction) => { _ = Task.Run(async () => await _extraneousHandler.ReactionRemovedHandler(_client.GetGuild(_guildId), cache, channel, reaction)); return Task.CompletedTask; };
        _client.UserVoiceStateUpdated += (user, before, after) => { _ = Task.Run(async () => await _extraneousHandler.VoiceStateUpdatedAsync(user, before, after, _client.GetGuild(_guildId))); return Task.CompletedTask; };

        _client.GuildMemberUpdated += (before, after) => { _ = Task.Run(async () => await _logHandler.LogMemberUpdate(before, after, _client.GetGuild(_guildId))); return Task.CompletedTask; };
        _client.InviteCreated += (invite) => { _ = Task.Run(async () => { await _logHandler.LogInvite(invite, _client.GetGuild(_guildId)); }); return Task.CompletedTask; };
        _client.UserJoined += (user) => { _ = Task.Run(async () => await _logHandler.LogUserJoined(user, _client.GetGuild(_guildId))); return Task.CompletedTask; };
        _client.UserLeft += (userGuild, user) => { _ = Task.Run(async () => await _logHandler.LogMemberLeft(userGuild, user)); return Task.CompletedTask; };
        _client.UserBanned += (user, userGuild) => { _ = Task.Run(async () => await _logHandler.LogMemberBanned(user, userGuild)); return Task.CompletedTask; };
        _client.MessageDeleted += (message, messageChannel) => { _ = Task.Run(async () => await _logHandler.LogMessageDelete(message, messageChannel, _client.GetGuild(_guildId))); return Task.CompletedTask; };
        _client.MessageUpdated += (beforemessage, aftermessage, messageChannel) => { _ = Task.Run(async () => await _logHandler.LogMessageUpdate(beforemessage, aftermessage, messageChannel, _client.GetGuild(_guildId))); return Task.CompletedTask; };
        _client.WebhooksUpdated += (userGuild, channel) => { _ = Task.Run(async () => await _logHandler.LogWebhookUpdate(userGuild, channel)); return Task.CompletedTask; };
        
        _client.AutocompleteExecuted += async (interaction) => {

            if (interaction.Data.Current.Name == "id_type") { await _extraneousHandler.IdAutocompleteHandler(interaction); }
            if (interaction.Data.Current.Name == "add_apps") { await _extraneousHandler.CollectedAppAutocompleteHandler(interaction); }
            if (interaction.Data.Current.Name == "remove_apps") { await _extraneousHandler.AppAutocompleteHandler(interaction); }
            if (interaction.Data.Current.Name == "case_type") { await _extraneousHandler.CaseAutocompleteHandler(interaction); }
            if (interaction.Data.Current.Name == "charm_type") { await _extraneousHandler.CharmAutocompleteHandler(interaction); }
            if (interaction.Data.Current.Name == "wallpaper_type") { await _extraneousHandler.WallpaperAutocompleteHandler(interaction); }
        };
        
        _client.Ready += async () => {
    
        var guild = _client.GetGuild(_guildId); 
        
        _ = Task.Run(async () => {
            await _logHandler.CreateOrUpdateStatChannel(guild);
            await _commandHandler.RegisterCommands(guild);
            
            bool shouldStartLoops;
            lock (_startedLoopsLock) {
                shouldStartLoops = _startedLoops.Add(guild.Id);
            }

            if (shouldStartLoops) {
                _ = _extraneousHandler.KickUnEnlisted(guild);
                _ = _extraneousHandler.AutoEnlistKohosei(guild);
                _ = _extraneousHandler.WeeklyEarningsRollover();
            }
        });
    };
        
        await _client.LoginAsync(TokenType.Bot, token);
        await _client.StartAsync();
        
        await Task.Delay(-1);
    }
    
    /// <summary>
    /// Gets sent to our backend and ExceptionWatch.
    /// </summary>
    private async Task<Task> Log(LogMessage msg) {
        
        Console.WriteLine(msg.ToString());
        _guildId = ulong.Parse(Environment.GetEnvironmentVariable("GUILD_ID") ?? throw new Exception("GUILD_ID environment variable not set."));
        await _logHandler.LogExceptionWatch(msg, _guildId);
        
        return Task.CompletedTask;
    }
    
    /// <summary>
    /// Providers help us get the things our classes need. Whenever you add a new System, please put them here.
    /// </summary>
    private static ServiceProvider CreateProvider() {
        
        var config = new DiscordSocketConfig {
            
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
            .AddSingleton<ExtraneousHandler>()
            .AddSingleton<LogHandler>()
            .AddSingleton<DatabaseService>()
            
            .AddSingleton<RewardSystem>()
            .AddSingleton<RoleSystem>()
            .AddSingleton<MeetingSystem>()
            .AddSingleton<IdSystem>()
            .AddSingleton<PointSystem>()
            .AddSingleton<GeneralSystem>()
            .AddSingleton<CellSystem>()
            .AddSingleton<ShopSystem>()
            
            .BuildServiceProvider();
    }
}
