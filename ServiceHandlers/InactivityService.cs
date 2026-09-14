using SMASSB.Models;

namespace SMASSB.ServiceHandlers;

using System.Collections.Concurrent;
using Discord;
using Discord.WebSocket;
using Lavalink4NET;
using Microsoft.Extensions.Hosting;

public class InactivityService : BackgroundService {

    private readonly IAudioService _audio;
    private readonly DiscordSocketClient _client;
    private readonly LogHandler _logHandler;
    private readonly ulong? _guildId;

    private readonly ConcurrentDictionary<ulong, (DateTime LastActive, ITextChannel Channel)> _lastActivity = new();
    private readonly ConcurrentDictionary<ulong, CancellationTokenSource> _emptyCountdowns = new();

    private static readonly TimeSpan EmptyVcDelay = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan IdleTimeout = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(30);

    public InactivityService(IAudioService audio, 
                             LogHandler logHandler,
                             DiscordSocketClient client,
                             GuildConfiguration guildConfig) {

        _audio = audio;
        _logHandler = logHandler;
        _client = client;
        _guildId = guildConfig.GuildId;
    }

    public void ResetTimer(ITextChannel channel) {
        _lastActivity[(ulong)_guildId!] = (DateTime.UtcNow, channel);
    }

    public void RemoveTimer() {

        _lastActivity.TryRemove((ulong)_guildId!, out _);
        CancelEmptyCountdown();
    }

    public void RegisterEvents() {
        _client.UserVoiceStateUpdated += OnUserVoiceStateUpdated;
    }

    private async Task OnUserVoiceStateUpdated(SocketUser user, SocketVoiceState before, SocketVoiceState after) {

        if (user is not SocketGuildUser guildUser || user.IsBot) return;
        if (before.VoiceChannel is null) return;

        if (!_lastActivity.TryGetValue((ulong)_guildId!, out var entry)) return;

        var botChannel = before.VoiceChannel.ConnectedUsers.Any(u => u.Id == _client.CurrentUser.Id) ? before.VoiceChannel: after.VoiceChannel;
        if (botChannel is null) return;

        var membersRemaining = botChannel.ConnectedUsers.Count(u => !u.IsBot);
        if (membersRemaining == 0) {

            if (!_emptyCountdowns.ContainsKey((ulong)_guildId!)) {
                StartEmptyCountdown(entry.Channel);
            }
        } else {
            if (_emptyCountdowns.ContainsKey((ulong)_guildId!)) {
                CancelEmptyCountdown();
            }
        }
    }

    private void StartEmptyCountdown(ITextChannel channel) {

        var cts = new CancellationTokenSource();
        if (!_emptyCountdowns.TryAdd((ulong)_guildId!, cts)) {
            cts.Dispose();
            return;
        }

        _ = Task.Run(async () => {

            try {
                await Task.Delay(EmptyVcDelay, cts.Token);

                var player = await _audio.Players.GetPlayerAsync((ulong)_guildId!, cts.Token);
                if (player is null) {
                    RemoveTimer();
                    return;
                }
                
                await channel.SendMessageAsync("Heading off.");
                await player.DisconnectAsync(cts.Token);
                RemoveTimer();
                
            } catch (Exception ex) {
                await _logHandler.LogExceptionWatch((ulong)_guildId!, exception: ex);
            } finally {
                _emptyCountdowns.TryRemove((ulong)_guildId!, out _);
                cts.Dispose();
            }
        }, cts.Token);
    }

    private void CancelEmptyCountdown() {

        if (_emptyCountdowns.TryRemove((ulong)_guildId!, out var cts)) {
            cts.Cancel();
            cts.Dispose();
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {

        while (!stoppingToken.IsCancellationRequested) {
            await Task.Delay(CheckInterval, stoppingToken);

            foreach (var (guildId, entry) in _lastActivity) {
                if (DateTime.UtcNow - entry.LastActive < IdleTimeout) continue;

                try {
                    var player = await _audio.Players.GetPlayerAsync(guildId, stoppingToken);

                    if (player is null) {
                        RemoveTimer();
                        continue;
                    }

                    if (player.State is not (Lavalink4NET.Players.PlayerState.Playing or Lavalink4NET.Players.PlayerState.Paused)) {

                        await entry.Channel.SendMessageAsync("Make sure to press /stop on me next time, if you meant to leave me sitting for 7 minutes.");
                        await player.DisconnectAsync(stoppingToken);
                        RemoveTimer();
                    } else {
                        ResetTimer(entry.Channel);
                    }
                }
                catch (Exception ex) {
                    await _logHandler.LogExceptionWatch((ulong)_guildId!, exception: ex);
                }
            }
        }
    }
}
