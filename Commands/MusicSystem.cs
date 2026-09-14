using Discord;
using Discord.WebSocket;
using Lavalink4NET;
using Lavalink4NET.Players;
using Lavalink4NET.Players.Queued;
using Lavalink4NET.Rest.Entities.Tracks;
using Microsoft.Extensions.Options;
using SMASSB.Models;
using SMASSB.ServiceHandlers;

namespace SMASSB.Commands;

public class MusicSystem {

    private readonly DiscordSocketClient _client;
    private readonly IAudioService _audio;
    private readonly InactivityService _inactivity;
    private readonly LogHandler _logHandler;
    private readonly ulong? _guildId;

    private static readonly Dictionary<ulong, bool> LoopState = new();
    private static readonly Dictionary<ulong, int> VolumeState = new();
    
    public MusicSystem(DiscordSocketClient client,
                        IAudioService audio,
                        InactivityService inactivity,
                        LogHandler logHandler,
                        GuildConfiguration guildConfig) {

        _client = client;
        _audio = audio;
        _inactivity = inactivity;
        _logHandler = logHandler;
        _guildId = guildConfig.GuildId;
    }
    
    private static string FormatDuration(TimeSpan? duration) {
        if (duration is null) return "Unknown";
        return duration.Value.TotalHours >= 1 ? duration.Value.ToString(@"hh\:mm\:ss") : duration.Value.ToString(@"mm\:ss");
    }

    private async ValueTask<QueuedLavalinkPlayer?> GetPlayerAsync(SocketSlashCommand command, bool join = true) {

        var user = command.User as SocketGuildUser;
        var voiceChannelId = user?.VoiceChannel?.Id;

        if (join && voiceChannelId is null) {
            await command.RespondAsync("You must be in a voice channel!", ephemeral: true);
            return null;
        }

        var behavior = join ? PlayerChannelBehavior.Join : PlayerChannelBehavior.None;
        var retrieveOptions = new PlayerRetrieveOptions(ChannelBehavior: behavior);
        var result = await _audio.Players.RetrieveAsync((ulong)_guildId!, voiceChannelId, PlayerFactory.Queued, Options.Create(new QueuedLavalinkPlayerOptions()), retrieveOptions);

        if (result.IsSuccess) return result.Player;
        
        string msg;
        switch (result.Status) {
            case PlayerRetrieveStatus.UserNotInVoiceChannel:
                msg = "You must be in a voice channel!";
                break;
            case PlayerRetrieveStatus.BotNotConnected:
                msg = "I'm not connected to a voice channel.";
                break;
            default:
                msg = "Something went wrong retrieving the player.";
                break;
        }
            
        await _logHandler.LogExceptionWatch((ulong)_guildId!, text: $"Getting the player failed.\n{msg}");
            
        await command.RespondAsync(msg, ephemeral: true);
        return null;
    }

    private static Embed BuildEmbed(string title, string description, Color color) {
        return new EmbedBuilder().WithTitle(title).WithDescription(description).WithColor(color).WithFooter("三五FM Radio Tsu! ♪").Build();
    }
    
    public async Task HandleJoinCommand(SocketSlashCommand command) {

        await command.DeferAsync();

        try {
            var player = await GetPlayerAsync(command, join: true);
            if (player is null) return;

            if (command.Channel is ITextChannel textChannel) _inactivity.ResetTimer(textChannel);
            await command.FollowupAsync("Joined VC.");
        } catch {
            await command.FollowupAsync("I can't see your VC, though I know you're in one.\nHave one of the staff check the permissions for your VC.");
        }
    }

    public async Task HandlePlayCommand(SocketSlashCommand command) {

        await command.DeferAsync();
        var query = command.Data.Options.FirstOrDefault()?.Value as string ?? "";

        try {
            var player = await GetPlayerAsync(command, join: true);
            if (player is null) return;

            var cleanQuery = query.Trim('_', '<', '>');

            var isUrl = Uri.IsWellFormedUriString(cleanQuery, UriKind.Absolute);
            var isSpotify = isUrl && cleanQuery.Contains("open.spotify.com", StringComparison.OrdinalIgnoreCase);
            var searchMode = isSpotify ? TrackSearchMode.Spotify : isUrl ? TrackSearchMode.None : TrackSearchMode.YouTube;

            var result = await _audio.Tracks
                .LoadTracksAsync(cleanQuery, searchMode)
                ;

            if (!result.HasMatches) {
                await command.FollowupAsync("I can't find any results for that track. Is it a Soundcloud link?", ephemeral: true);
                return;
            }

            if (result.IsPlaylist) {

                var tracks = result.Tracks.ToList();
                var playlist = result.Playlist!;

                await player.PlayAsync(tracks[0]);
                foreach (var t in tracks.Skip(1)) await player.Queue.AddAsync(new TrackQueueItem(t));

                await command.FollowupAsync(embed: BuildEmbed("Queued Playlist", $"**{playlist.Name}**\n`{tracks.Count}` tracks added.", new Color(0x44786F)));

                if (command.Channel is ITextChannel textChannel) _inactivity.ResetTimer(textChannel);
                return;
            }

            var track = result.Track!;
            if (player.State is PlayerState.Playing or PlayerState.Paused) {

                await player.Queue.AddAsync(new TrackQueueItem(track));
                await command.FollowupAsync(embed: BuildEmbed("Queued", $"**{track.Title}**\n`{FormatDuration(track.Duration)}`", new Color(0x44786F)));
            } else {
                
                await player.PlayAsync(track);
                await command.FollowupAsync(embed: BuildEmbed("Now Playing . . <:sango_emblem_mono:1492222638980989138>", $"**{track.Title}**\n`{FormatDuration(track.Duration)}`", new Color(0xBFA55F)));
            }

            if (command.Channel is ITextChannel textChannelPlay) _inactivity.ResetTimer(textChannelPlay);
        }
        catch (Exception ex) {
            await _logHandler.LogExceptionWatch((ulong)_guildId!, exception: ex);
            await command.FollowupAsync("An unexpected error occurred. Please try again.", ephemeral: true);
        }
    }
    
    public async Task HandleStopCommand(SocketSlashCommand command) {

        await command.DeferAsync();

        try {
            var player = await GetPlayerAsync(command, join: false);
            if (player is null) {

                await command.FollowupAsync("I'm not playing anything.", ephemeral: true);
                return;
            }

            LoopState[(ulong)_guildId!] = false;
            await player.StopAsync();
            await player.DisconnectAsync();
            await command.FollowupAsync("Be well.");
            
            _inactivity.RemoveTimer();
        }
        catch (Exception ex) {
            await _logHandler.LogExceptionWatch((ulong)_guildId!, exception: ex);
            await command.FollowupAsync("An unexpected error occurred.", ephemeral: true);
        }
    }
    
    public async Task HandleSkipCommand(SocketSlashCommand command) {

        await command.DeferAsync();

        try {
            var player = await GetPlayerAsync(command, join: false);
            if (player is null) {

                await command.FollowupAsync("I'm not playing anything.", ephemeral: true);
                return;
            }

            var current = player.CurrentTrack?.Title ?? "Unknown";

            if (player.Queue.Count == 0) {

                await player.StopAsync();
                await command.FollowupAsync($"Skipped **{current}**. Queue is now empty.");
                if (command.Channel is ITextChannel textChannel) _inactivity.ResetTimer(textChannel);
                return;
            }

            await player.SkipAsync();
            var next = player.CurrentTrack;

            await command.FollowupAsync(embed: BuildEmbed("Skipped", $"**{current}**\n\nNow playing: **{next?.Title ?? "Unknown"}**", new Color(0x44786F)));
            if (command.Channel is ITextChannel textChannelSkip) _inactivity.ResetTimer(textChannelSkip);
            
        } catch (Exception ex) {

            await _logHandler.LogExceptionWatch((ulong)_guildId!, exception: ex);
            await command.FollowupAsync("An unexpected error occurred.", ephemeral: true);
        }
    }
    
    public async Task HandleQueueCommand(SocketSlashCommand command) {

        await command.DeferAsync();

        try {
            var player = await GetPlayerAsync(command, join: false);
            if (player is null) {

                await command.FollowupAsync("Nothing's queued.", ephemeral: true);
                return;
            }

            if (player.Queue.Count == 0) {

                await command.FollowupAsync("Nothing's queued.");
                return;
            }

            var tracks = player.Queue.Take(10).ToList();
            var list = string.Join("\n", tracks.Select((t, i) => $"`{i + 1}.` **{t.Track?.Title ?? "Unknown"}** : `{FormatDuration(t.Track?.Duration)}`"));
            var more = player.Queue.Count > 10 ? $"\n*…and {player.Queue.Count - 10} more*" : "";

            await command.FollowupAsync(embed: BuildEmbed("Queue", list + more, new Color(0xBFA55F)));
        }
        catch (Exception ex) {

            await _logHandler.LogExceptionWatch((ulong)_guildId!, exception: ex);
            await command.FollowupAsync("An unexpected error occurred.", ephemeral: true);
        }
    }
    
    public async Task HandlePauseCommand(SocketSlashCommand command) {

        await command.DeferAsync();

        try {
            var player = await GetPlayerAsync(command, join: false);
            
            if (player is null) {
                await command.RespondAsync("Nothing's playing.", ephemeral: true);
                return;
            }

            if (player.State == PlayerState.Paused) {

                await player.ResumeAsync();
                await command.FollowupAsync("Resumed.");
            } else {

                await player.PauseAsync();
                await command.FollowupAsync("Paused.");
            }

            if (command.Channel is ITextChannel textChannel) _inactivity.ResetTimer(textChannel);
            
        } catch (Exception ex) {

            await _logHandler.LogExceptionWatch((ulong)_guildId!, exception: ex);
            await command.FollowupAsync("An unexpected error occurred.", ephemeral: true);
        }
    }
    
    public async Task HandleNowPlayingCommand(SocketSlashCommand command) {

        await command.DeferAsync();

        try {
            var player = await GetPlayerAsync(command, join: false);
            if (player is null) {

                await command.FollowupAsync("Now Playing: Silence.", ephemeral: true);
                return;
            }

            var track = player.CurrentTrack;
            if (track is null) {

                await command.FollowupAsync("Now Playing: Silence.", ephemeral: true);
                return;
            }

            var looping = LoopState.TryGetValue((ulong)_guildId!, out var l) && l;
            var vol = VolumeState.GetValueOrDefault((ulong)_guildId!, 100);

            var desc = $"**{track.Title}**\n by *{track.Author}*\n\nDuration: `{FormatDuration(track.Duration)}`\nVolume: `{vol}%`\nLoop: `{(looping ? "On" : "Off")}`\nState: `{player.State}`\n\n[Open Link]({track.Uri})";
            await command.FollowupAsync(embed: BuildEmbed("Now Playing ♪", desc, new Color(0xBFA55F)));
            
        } catch (Exception ex) {

            await _logHandler.LogExceptionWatch((ulong)_guildId!, exception: ex);
            await command.FollowupAsync("An unexpected error occurred.", ephemeral: true);
        }
    }
    
    public async Task HandleVolumeCommand(SocketSlashCommand command) {

        await command.DeferAsync();

        var levelObj = command.Data.Options.FirstOrDefault()?.Value;
        var level = levelObj switch { int i => i, long l => (int)l, _ => 100 };

        try {

            if (level is < 0 or > 100) {
                await command.FollowupAsync("Volume must be between 0 and 100!", ephemeral: true);
                return;
            }

            var player = await GetPlayerAsync(command, join: false);
            if (player is null) return;

            await player.SetVolumeAsync(level / 100f);
            VolumeState[(ulong)_guildId!] = level;

            await command.FollowupAsync($"Volume set to `{level}%`.");
        }
        catch (Exception ex) {

            await _logHandler.LogExceptionWatch((ulong)_guildId!, exception: ex);
            await command.FollowupAsync("An unexpected error occurred.", ephemeral: true);
        }
    }
    
    public async Task HandleLoopCommand(SocketSlashCommand command) {

        await command.DeferAsync();

        try {
            var player = await GetPlayerAsync(command, join: false);
            if (player is null) {

                await command.FollowupAsync("Not playing anything.", ephemeral: true);
                return;
            }

            var current = LoopState.TryGetValue((ulong)_guildId!, out var l) && l;
            LoopState[(ulong)_guildId!] = !current;

            player.RepeatMode = LoopState[(ulong)_guildId!] ? TrackRepeatMode.Track : TrackRepeatMode.None;

            await command.FollowupAsync(LoopState[(ulong)_guildId!] ? "Loop **enabled**. Current track will repeat." : "Loop **disabled**.");
        } catch (Exception ex) {

            await _logHandler.LogExceptionWatch(command.Id, exception: ex);
            await command.FollowupAsync("An unexpected error occurred.", ephemeral: true);
        }
    }
}