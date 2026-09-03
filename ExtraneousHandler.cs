using Discord;
using Discord.Net;
using Discord.WebSocket;
using SMASSB.Commands;
using SMASSB.Exceptions;
using SMASSB.Models;

namespace SMASSB;

public class ExtraneousHandler {

    private readonly DiscordSocketClient _client;
    private readonly RoleSystem _roleSystem;
    private readonly PointSystem _pointSystem;
    private readonly CellSystem _cellSystem;
    private readonly ShopSystem _shopSystem;
    private readonly DatabaseService _db;
    private readonly LogHandler _logHandler;
    private readonly ulong? _guildId;

    public ExtraneousHandler(DiscordSocketClient client,
        LogHandler logHandler,
        RoleSystem roleSystem,
        PointSystem pointSystem,
        CellSystem cellSystem,
        ShopSystem shopSystem,
        DatabaseService db,
        GuildConfiguration guildConfig) {

        _client = client;
        _roleSystem = roleSystem;
        _pointSystem = pointSystem;
        _cellSystem = cellSystem;
        _shopSystem = shopSystem;
        _db = db;
        _logHandler = logHandler;
        _guildId = guildConfig.GuildId;
    }

    public async Task ReactionAddedHandler(SocketGuild guild, Cacheable<IUserMessage, ulong> cache, Cacheable<IMessageChannel, ulong> channel, SocketReaction reaction) {

        var user = guild.GetUser(reaction.UserId);
        if (user is null) {
            return;
        }

        if (reaction.Emote.Name == "⭐") {

            var message = await cache.GetOrDownloadAsync();
            if (message.Channel.Id == 1473214696109903883) return;

            var starEmote = new Emoji("⭐");

            var freshMessage = await message.Channel.GetMessageAsync(message.Id) as IUserMessage;
            var starCount = freshMessage?.Reactions.TryGetValue(starEmote, out var meta) == true ? meta.ReactionCount : 1;

            if (starCount < 3) return;

            var author = message.Author as SocketGuildUser;
            var builder = new EmbedBuilder()
                .WithAuthor("|| " + author?.Nickname, author?.GetGuildAvatarUrl() ?? author?.GetAvatarUrl())
                .WithTitle($"⭐ {starCount} stars! ﹒ https://discordapp.com/channels/{guild.Id}/{message.Channel.Id}/{message.Id}")
                .WithDescription(message.Content)
                .WithFooter($"{message.Timestamp:M/d/yyyy HH:mm:ss tt}")
                .WithColor(0xBFA55F);

            if (message.Attachments.Count > 0) {

                var attachment = message.Attachments.First();
                var isVideo = attachment.ContentType?.StartsWith("video/") == true || IsVideoExtension(attachment.Filename);

                if (attachment.IsSpoiler()) {
                    var label = isVideo ? "Spoilered video." : "Spoilered image.";
                    builder.WithDescription(message.Content + $"\n\n**{label}**");
                } else if (isVideo) {
                    builder.WithDescription(message.Content + $"\n\n[Video attachment]({attachment.Url})");
                } else {
                    builder.WithImageUrl(attachment.Url);
                }
            }

            var starboard = guild.GetChannel(1473214696109903883) as ITextChannel;
            if (starboard == null) return;

            var existingId = _db.GetStarboardMessageId(message.Id);

            if (existingId is null) {

                var sent = await starboard.SendMessageAsync(embed: builder.Build());
                _db.SaveStarboardMessageId(message.Id, sent.Id);
            } else {
                if (ulong.TryParse(existingId, out var existingUlong) &&
                    await starboard.GetMessageAsync(existingUlong) is IUserMessage existing) {
                    await existing.ModifyAsync(m => m.Embed = builder.Build());
                }
            }
        }

        switch (reaction.MessageId) {
            case 1495182123924197396: { // roe
                if (reaction.Emote is Emote emote) {
                    switch (emote.Id) {

                        case 1481753776745611505:
                            await user.AddRoleAsync(1473369383471677461);
                            break;
                        case 1481753799071633499:
                            await user.AddRoleAsync(1475720710910382310);
                            break;
                    }
                }
                break;
            } case 1515073043184226555: { // roles, personal
                if (reaction.Emote is Emote emote) {
                    switch (emote.Id) {

                        case 1481753776745611505:
                            await user.AddRoleAsync(1473370170826428626);
                            break;
                        case 1481753799071633499:
                            await user.AddRoleAsync(1473370195010785464);
                            break;
                        case 1481753821637251112:
                            await user.AddRoleAsync(1473370218259546192);
                            break;
                        case 1481753839668564029:
                            await user.AddRoleAsync(1473370251872698530);
                            break;
                        case 1481753863194284032:
                            await user.AddRoleAsync(1473370274073153708);
                            break;
                        case 1481753878834970654:
                            await user.AddRoleAsync(1473370375739015259);
                            break;
                    }
                }
                break;
            } case 1515073044773736528: { // roles, pings
                if (reaction.Emote is Emote emote) {
                    switch (emote.Id) {

                        case 1481753776745611505:
                            await user.AddRoleAsync(1473370497524699382);
                            break;
                        case 1481753799071633499:
                            await user.AddRoleAsync(1473370613992394864);
                            break;
                        case 1481753821637251112:
                            await user.AddRoleAsync(1481786695254016020);
                            break;
                        case 1481753839668564029:
                            await user.AddRoleAsync(1477868383524618270);
                            break;
                        case 1481753863194284032:
                            await user.AddRoleAsync(1473370861422772349);
                            break;
                        case 1481753878834970654:
                            await user.AddRoleAsync(1473370973448310980);
                            break;
                        case 1481753919624446063:
                            await user.AddRoleAsync(1473371104604061768);
                            break;
                    }
                }
                break;
            } case 1544439498115383429: { // tanabata
                if (reaction.Emote is Emote emote) {
                    switch (emote.Id) {

                        case 1492222638980989138:
                            await user.AddRoleAsync(1544434735173083207);
                            break;
                    }
                }
                break;
            }
        }
    }

    public async Task ReactionRemovedHandler(SocketGuild guild, Cacheable<IUserMessage, ulong> cache, Cacheable<IMessageChannel, ulong> channel, SocketReaction reaction) {

        var user = guild.GetUser(reaction.UserId);
        if (user is null) {
            return;
        }

        if (reaction.Emote.Name == "⭐") {

            var message = await cache.GetOrDownloadAsync();
            if (message.Channel.Id == 1473214696109903883) return;

            var starEmote = new Emoji("⭐");

            var freshMessage = await message.Channel.GetMessageAsync(message.Id) as IUserMessage;
            var starCount = freshMessage?.Reactions.TryGetValue(starEmote, out var meta) == true ? meta.ReactionCount : 0;

            var starboard = guild.GetChannel(1473214696109903883) as ITextChannel;
            if (starboard == null) return;

            var existingId = _db.GetStarboardMessageId(message.Id);
            if (existingId is null) return;

            if (!ulong.TryParse(existingId, out var existingUlong)) return;

            if (starCount < 3) {

                if (await starboard.GetMessageAsync(existingUlong) is IUserMessage existing) {
                    await existing.DeleteAsync();
                }
                _db.DeleteStarboardEntry(message.Id);
            } else {

                var author = message.Author as SocketGuildUser;
                var builder = new EmbedBuilder()
                    .WithAuthor("|| " + author?.Nickname, author?.GetGuildAvatarUrl() ?? author?.GetAvatarUrl())
                    .WithTitle($"⭐ {starCount} stars! ﹒ https://discordapp.com/channels/{guild.Id}/{message.Channel.Id}/{message.Id}")
                    .WithDescription(message.Content)
                    .WithFooter($"{message.Timestamp:M/d/yyyy HH:mm:ss tt}")
                    .WithColor(0xBFA55F);

                if (message.Attachments.Count > 0) {

                    var attachment = message.Attachments.First();
                    var isVideo = attachment.ContentType?.StartsWith("video/") == true || IsVideoExtension(attachment.Filename);

                    if (attachment.IsSpoiler()) {
                        var label = isVideo ? "Spoilered video." : "Spoilered image.";
                        builder.WithDescription(message.Content + $"\n\n**{label}**");
                    } else if (isVideo) {
                        builder.WithDescription(message.Content + $"\n\n[Video attachment]({attachment.Url})");
                    } else {
                        builder.WithImageUrl(attachment.Url);
                    }
                }
                
                if (await starboard.GetMessageAsync(existingUlong) is IUserMessage existing) {
                    await existing.ModifyAsync(m => m.Embed = builder.Build());
                }
            }
        }

        switch (reaction.MessageId) {
            case 1495182123924197396: { // roe
                if (reaction.Emote is Emote emote) {
                    switch (emote.Id) {

                        case 1481753776745611505:
                            await user.RemoveRoleAsync(1473369383471677461);
                            break;
                        case 1481753799071633499:
                            await user.RemoveRoleAsync(1475720710910382310);
                            break;
                    }
                }
                break;
            } case 1515073043184226555: { // roles, personal
                if (reaction.Emote is Emote emote) {
                    switch (emote.Id) {

                        case 1481753776745611505:
                            await user.RemoveRoleAsync(1473370170826428626);
                            break;
                        case 1481753799071633499:
                            await user.RemoveRoleAsync(1473370195010785464);
                            break;
                        case 1481753821637251112:
                            await user.RemoveRoleAsync(1473370218259546192);
                            break;
                        case 1481753839668564029:
                            await user.RemoveRoleAsync(1473370251872698530);
                            break;
                        case 1481753863194284032:
                            await user.RemoveRoleAsync(1473370274073153708);
                            break;
                        case 1481753878834970654:
                            await user.RemoveRoleAsync(1473370375739015259);
                            break;
                    }
                }
                break;
            } case 1515073044773736528: { // roles, pings
                if (reaction.Emote is Emote emote) {
                    switch (emote.Id) {

                        case 1481753776745611505:
                            await user.RemoveRoleAsync(1473370497524699382);
                            break;
                        case 1481753799071633499:
                            await user.RemoveRoleAsync(1473370613992394864);
                            break;
                        case 1481753821637251112:
                            await user.RemoveRoleAsync(1481786695254016020);
                            break;
                        case 1481753839668564029:
                            await user.RemoveRoleAsync(1477868383524618270);
                            break;
                        case 1481753863194284032:
                            await user.RemoveRoleAsync(1473370861422772349);
                            break;
                        case 1481753878834970654:
                            await user.RemoveRoleAsync(1473370973448310980);
                            break;
                        case 1481753919624446063:
                            await user.RemoveRoleAsync(1473371104604061768);
                            break;
                    }
                }
                break;
            } case 1544439498115383429: { // tanabata
                if (reaction.Emote is Emote emote) {
                    switch (emote.Id) {

                        case 1492222638980989138:
                            await user.RemoveRoleAsync(1544434735173083207);
                            break;
                    }
                }
                break;
            }
        }
    }

    public async Task VoiceStateUpdatedAsync(SocketUser user, SocketVoiceState before, SocketVoiceState after, SocketGuild guild) {

        if (after.VoiceChannel?.Id == 1473221413749129367) {
            var category = guild.GetCategoryChannel(1473221350826184734);

            var voiceChannelCount = category.Channels.OfType<SocketVoiceChannel>().Count() - 1;
            var nameCount = voiceChannelCount switch {
                0 => "i", 1 => "ii", 2 => "iii", 3 => "iv", 4 => "v", 5 => "vi", 6 => "vii", 7 => "iix", 8 => "ix", 9 => "x", _ => voiceChannelCount.ToString()
            };

            var name = "∥・mic・ready・" + nameCount;

            var vc = await guild.CreateVoiceChannelAsync(name, props => {
                props.CategoryId = category.Id;
                props.PermissionOverwrites = category.PermissionOverwrites.ToList();
            });

            if (user is SocketGuildUser guildUser) {
                await guildUser.ModifyAsync(x => x.Channel = vc);
            }

            await vc.SetStatusAsync("✦ Change the status to the topic of the VC!");
        }

        if (before.VoiceChannel != null && before.VoiceChannel.Id != 1473221413749129367) {
            await Task.Delay(500);
            var vc = before.VoiceChannel;
            if (vc.ConnectedUsers.Count == 0) {
                await vc.DeleteAsync();
            }
        }
    }

    public async Task AutoEnlistKohosei(SocketGuild guild) {
        
        using var timer = new PeriodicTimer(TimeSpan.FromHours(1));
        var channel = guild.GetTextChannel(1473516609397063680);

        while (await timer.WaitForNextTickAsync()) {
            IAsyncEnumerable<IReadOnlyCollection<IGuildUser>> collection = guild.GetUsersAsync();

            await foreach (var members in collection) {
                foreach (var member in members) {

                    var isUnenlisted = member.RoleIds.Contains((ulong)1473369036766052445);
                    var isEligible = (await _db.GetPoints(member.Id) >= 15);
                    var overwrite = channel.GetPermissionOverwrite(member);

                    if (!isUnenlisted || !isEligible || overwrite?.ViewChannel == PermValue.Allow) continue;
                    
                    await Task.Delay(1500);
                    await _roleSystem.HandleFinishKo(member, channel);
                }
            }
        }
    }

    public async Task KickUnEnlisted(SocketGuild guild) {

        using var timer = new PeriodicTimer(TimeSpan.FromDays(3));

        while (await timer.WaitForNextTickAsync()) {
            IAsyncEnumerable<IReadOnlyCollection<IGuildUser>> collection = guild.GetUsersAsync();

            await foreach (var members in collection) {
                foreach (var user in members) {

                    var guildUser = user as SocketGuildUser;

                    bool isCivilian = user.RoleIds.Contains((ulong)1473369383471677461) && !user.RoleIds.Contains((ulong)1475720710910382310);
                    bool isProspect = user.RoleIds.Contains((ulong)1473369036766052445);
                    bool isInactive = user.JoinedAt < DateTimeOffset.Now.AddMonths(-2);

                    ulong[] unverifiedRoles = [1473369716792885402, 1473370059950002318, 1473370439526125599, 1473371454790832304];

                    var isUnverified = guildUser != null && guildUser.Roles
                        .Where(r => !r.IsEveryone)
                        .All(r => unverifiedRoles.Contains(r.Id));

                    if ((!isCivilian && !isProspect && !isUnverified) || !isInactive) continue;
                    
                    var channel = guild.GetChannel(1486431270941622363) as ITextChannel;

                    try {
                        await user.SendMessageAsync("Hello! This is the *Automatic Messaging System* at the Sangō Idol-Defense Force.\n\nWe are messaging you in regards to your activity. As outlined in our syllabus, prospects and civilians (who are NOT fans) are to be kicked from the server in the case that they are inactive for more than 2 months in order to keep member counts accurate.\n\nWe thank you for attempting to experience Sangō!\n\nIf you feel this is a mistake, please friend request and send a message to *@fastestthingalive* in order to regain access to the server. If it is not, yet you still wish to join back, please give yourself __a week or so__ and do as previously instructed. Just to make sure you *really* want to!\n\nPlease have a good day, " + user.Username + "!\n### _ _                                                         — The Staff at Sangō Idol-Defense Force");
                        await user.SendMessageAsync("https://64.media.tumblr.com/384045d1eed5c0aa490e00aa98456239/c6b43c8a326634f0-7e/s2048x3072/8ae54d651ee2b0f75768d902e80ff1ec77417d08.pnj");
                    } catch (HttpException ex) {
                        if (channel != null)
                            await channel.SendMessageAsync(new MessageSendException(ex.Message, ex).Message);
                    }

                    try {
                        await Task.Delay(1500);
                        await user.KickAsync("Inactive for 2+ months");
                        await Task.Delay(1000);
                    } catch (Exception ex) {
                        if (channel != null) await channel.SendMessageAsync(ex.Message);
                    }
                }
            }
        }
    }

    public async Task WeeklyEarningsRollover() {
        
        await _db.InitializeWeeklyBaselines();
        _db.SetLastRolloverTime(DateTimeOffset.UtcNow);

        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(15));
        await RunRolloverIfDue();
        while (await timer.WaitForNextTickAsync()) {
            await RunRolloverIfDue();
        }
    }

    private async Task RunRolloverIfDue() {

        var gmt2Offset = TimeSpan.FromHours(2);
        var nowGmt2 = DateTimeOffset.UtcNow.ToOffset(gmt2Offset);

        var targetSunday = GetMostRecentSundayMidnight(nowGmt2, gmt2Offset);
        var lastRollover = _db.GetLastRolloverTime() ?? DateTimeOffset.MinValue;

        if (nowGmt2 >= targetSunday && lastRollover < targetSunday) {
            _db.SetLastRolloverTime(DateTimeOffset.UtcNow);
            await _db.RolloverWeeklyEarnings();
        }
    }

    private static DateTimeOffset GetMostRecentSundayMidnight(DateTimeOffset nowInZone, TimeSpan offset) {

        var daysSinceSunday = (int)nowInZone.DayOfWeek;
        var sundayDate = nowInZone.Date.AddDays(-daysSinceSunday);
        return new DateTimeOffset(sundayDate, offset);
    }

    public async Task ButtonHandler(SocketMessageComponent component) {

        var id = component.Data.CustomId;
        var guild = _client.GetGuild((ulong)component.GuildId!);
        
        if (id.StartsWith("buy_item_")) {
            try {
                var parts = id.Split('_');
                if (parts.Length < 4) return;

                if (!int.TryParse(parts[2], out var itemNum) || !ulong.TryParse(parts[3], out var channelId)) return;

                var buyerId = component.User.Id;

                await component.DeferAsync(ephemeral: true);

                await _shopSystem.Buy(itemNum, buyerId, channelId);
                
            } catch (Exception ex) {
                await _logHandler.LogExceptionWatch(guild.Id, exception: ex);
                Console.WriteLine($"buy_item button error: {ex.Message}");
                
                if (!component.HasResponded) {
                    await component.DeferAsync(ephemeral: true);
                }
            }
            return;
        }

        if (id.StartsWith("flip_over:")) {
            
            var statePayload = id["flip_over:".Length..];
            var stateParts = statePayload.Split('|');

            if (stateParts.Length < 1 || !ulong.TryParse(stateParts[0], out var flipOwnerId)) return;

            try {
                var yen = await _db.GetYen(flipOwnerId);
                var earningsSummary = await _db.GetEarningsSummary(flipOwnerId);

                await _cellSystem.HandleFlipOver(component, statePayload, yen, earningsSummary.Item1, earningsSummary.Item3, earningsSummary.Item4);
            } catch (Exception ex) {
                Console.WriteLine($"flip_over error: {ex}");
                await _logHandler.LogExceptionWatch(guild.Id, exception: ex, text: "flip_over error.");
            }
            return;
        }

        if (id.StartsWith("launch_emulatorjs:")) {
            
            try {
                var payload = id.Substring("launch_emulatorjs:".Length);
                var payloadParts = payload.Split(':');
                
                if (payloadParts.Length < 2) return;
                if (!ulong.TryParse(payloadParts[0], out var ownerId)) return;

                var game = payloadParts[1];
                await _cellSystem.HandleLaunchEmulatorJs(component, ownerId, game);
                
            } catch (Exception ex) {
                Console.WriteLine($"launch_emulatorjs button error: {ex.Message}");
                await _logHandler.LogExceptionWatch(guild.Id, exception: ex, text: "launch_emulatorjs button error.");
            }
            return;
        }

        if (id.StartsWith("leaderboard_back_") || id.StartsWith("leaderboard_next_")) {
            try {
                var parts = id.Split('_');
                if (parts.Length < 3 || !int.TryParse(parts[2], out var currentPage)) return;

                int newPage = id.StartsWith("leaderboard_back_") ? currentPage - 1 : currentPage + 1;

                var entries = _db.GetLeaderboard();

                var embed = _pointSystem.BuildLeaderboardEmbed(entries, newPage);
                var components = _pointSystem.BuildLeaderboardComponents(newPage, entries.Count);

                await component.UpdateAsync(x => {
                    x.Embed = embed;
                    x.Components = components;
                });
            } catch (Exception ex) {
                Console.WriteLine($"leaderboard button error: {ex}");
                await _logHandler.LogExceptionWatch(guild.Id, exception: ex, text: "leaderboard button error.");
            }
        }
    }

    public async Task IdAutocompleteHandler(SocketAutocompleteInteraction interaction) {

        var collected = await _db.GetIds(interaction.User.Id);
        var typed = (string)interaction.Data.Current.Value;

        var results = collected
            .Where(id => string.IsNullOrEmpty(typed) || id.Contains(typed, StringComparison.OrdinalIgnoreCase))
            .Select(id => new AutocompleteResult(id, id));

        await interaction.RespondAsync(results);
    }

    public async Task AppAutocompleteHandler(SocketAutocompleteInteraction interaction) {

        var collected = await _db.GetApps(interaction.User.Id);
        var typed = (string)interaction.Data.Current.Value;

        var results = collected
            .Where(app => string.IsNullOrEmpty(typed) || app.Contains(typed, StringComparison.OrdinalIgnoreCase))
            .Select(app => new AutocompleteResult(app, app));

        await interaction.RespondAsync(results);
    }

    public async Task CollectedAppAutocompleteHandler(SocketAutocompleteInteraction interaction) {

        var collected = await _db.GetCollectedApps(interaction.User.Id);
        var typed = (string)interaction.Data.Current.Value;

        var results = collected
            .Where(app => string.IsNullOrEmpty(typed) || app.Contains(typed, StringComparison.OrdinalIgnoreCase))
            .Select(app => new AutocompleteResult(app, app));

        await interaction.RespondAsync(results);
    }

    public async Task CaseAutocompleteHandler(SocketAutocompleteInteraction interaction) {

        var collected = await _db.GetCases(interaction.User.Id);
        var typed = (string)interaction.Data.Current.Value;

        var results = collected
            .Where(cellCase => string.IsNullOrEmpty(typed) || cellCase.Contains(typed, StringComparison.OrdinalIgnoreCase))
            .Select(cellCase => new AutocompleteResult(cellCase, cellCase));

        await interaction.RespondAsync(results);
    }

    public async Task CharmAutocompleteHandler(SocketAutocompleteInteraction interaction) {

        var collected = await _db.GetCharms(interaction.User.Id);
        var typed = (string)interaction.Data.Current.Value;

        var results = collected
            .Where(charm => string.IsNullOrEmpty(typed) || charm.Contains(typed, StringComparison.OrdinalIgnoreCase))
            .Select(charm => new AutocompleteResult(charm, charm));

        await interaction.RespondAsync(results);
    }

    public async Task WallpaperAutocompleteHandler(SocketAutocompleteInteraction interaction) {

        var collected = await _db.GetWallpapers(interaction.User.Id);
        var typed = (string)interaction.Data.Current.Value;

        var results = collected
            .Where(wallpaper => string.IsNullOrEmpty(typed) || wallpaper.Contains(typed, StringComparison.OrdinalIgnoreCase))
            .Select(wallpaper => new AutocompleteResult(wallpaper, wallpaper));

        await interaction.RespondAsync(results);
    }

    private bool IsVideoExtension(string filename) {
        var videoExtensions = new[] { ".mp4", ".mov", ".webm", ".mkv", ".avi", ".m4v" };
        return videoExtensions.Any(ext => filename.EndsWith(ext, StringComparison.OrdinalIgnoreCase));
    }
}