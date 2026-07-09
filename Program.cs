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
    private static readonly HashSet<ulong> _startedLoops = new();
    private static readonly object _startedLoopsLock = new();

    public static async Task Main()
        => await new Program().RunAsync();

    private async Task RunAsync() {
        
        _serviceProvider = CreateProvider();
        _client = _serviceProvider.GetRequiredService<DiscordSocketClient>();
        _commandHandler = _serviceProvider.GetRequiredService<CommandHandler>();
        _logHandler = _serviceProvider.GetRequiredService<LogHandler>();
        
        var token = Environment.GetEnvironmentVariable("BOT_TOKEN") ?? throw new Exception("BOT_TOKEN environment variable not set.");

        _guildId = ulong.Parse(Environment.GetEnvironmentVariable("GUILD_ID") ?? throw new Exception("GUILD_ID environment variable not set."));
        _client.Log += Log;
        
        _client.ButtonExecuted += _commandHandler.ButtonHandler;
        _client.ReactionAdded += (cache, channel, reaction) => { _ = Task.Run(async () => await _commandHandler.ReactionAddedHandler(_client.GetGuild(_guildId), cache, channel, reaction)); return Task.CompletedTask; };
        _client.ReactionRemoved += (cache, channel, reaction) => { _ = Task.Run(async () => await _commandHandler.ReactionRemovedHandler(_client.GetGuild(_guildId), cache, channel, reaction)); return Task.CompletedTask; };
        _client.UserVoiceStateUpdated += (user, before, after) => { _ = Task.Run(async () => await _commandHandler.VoiceStateUpdatedAsync(user, before, after, _client.GetGuild(_guildId))); return Task.CompletedTask; };

        _client.GuildMemberUpdated += (before, after) => { _ = Task.Run(async () => await _logHandler.LogMemberUpdate(before, after, _client.GetGuild(_guildId))); return Task.CompletedTask; };
        _client.InviteCreated += (invite) => { _ = Task.Run(async () => { await _logHandler.LogInvite(invite, _client.GetGuild(_guildId)); }); return Task.CompletedTask; };
        _client.UserJoined += (user) => { _ = Task.Run(async () => await _logHandler.LogUserJoined(user, _client.GetGuild(_guildId))); return Task.CompletedTask; };
        _client.UserLeft += (userGuild, user) => { _ = Task.Run(async () => await _logHandler.LogMemberLeft(userGuild, user)); return Task.CompletedTask; };
        _client.UserBanned += (user, userGuild) => { _ = Task.Run(async () => await _logHandler.LogMemberBanned(user, userGuild)); return Task.CompletedTask; };
        _client.MessageDeleted += (message, messageChannel) => { _ = Task.Run(async () => await _logHandler.LogMessageDelete(message, messageChannel, _client.GetGuild(_guildId))); return Task.CompletedTask; };
        _client.MessageUpdated += (beforemessage, aftermessage, messageChannel) => { _ = Task.Run(async () => await _logHandler.LogMessageUpdate(beforemessage, aftermessage, messageChannel, _client.GetGuild(_guildId))); return Task.CompletedTask; };
        _client.WebhooksUpdated += (userGuild, channel) => { _ = Task.Run(async () => await _logHandler.LogWebhookUpdate(userGuild, channel)); return Task.CompletedTask; };
        
        _client.AutocompleteExecuted += async (interaction) => {
            
            if (interaction.Data.CommandName != "editid") return;
            if (interaction.Data.Current.Name != "id_type") return;
            
            await _commandHandler.IdAutocompleteHandler(interaction);
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
                _ = _commandHandler.KickUnEnlisted(guild);
                _ = _commandHandler.AutoEnlistKohosei(guild);
            }
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
    
    private static ServiceProvider CreateProvider() {
        
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
