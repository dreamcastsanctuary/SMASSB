using Discord;
using Discord.WebSocket;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SMASSB.Models;
using Color = Discord.Color;
using Image = SixLabors.ImageSharp.Image;

namespace SMASSB;

public class LogHandler {
    
    private readonly DiscordSocketClient _client;
    private readonly DatabaseService _db;
    private readonly ulong? _guildId;
    private static readonly HttpClient HttpClient = new HttpClient();

    public LogHandler(DiscordSocketClient client,
                      DatabaseService db,
                      GuildConfiguration guildConfig) {
        
        _client = client;
        _db = db;
        _guildId = guildConfig.GuildId;
    }

    public async Task LogMemberUpdate(Cacheable<SocketGuildUser, ulong> before, SocketGuildUser after, SocketGuild guild) {
        
        try {
            var embedBuilder = new EmbedBuilder()
                .WithAuthor("|| " + after.Username, after.GetGuildAvatarUrl() ?? after.GetAvatarUrl())
                .WithFooter(after.Id.ToString())
                .WithColor(0xBFA55F);
            
            var beforeUser = await before.GetOrDownloadAsync();
            var channel = guild.GetChannel(1482805129613938860) as ISocketMessageChannel;
            
            if (beforeUser.Nickname != after.Nickname) {
                var name = after.Nickname ?? "@" + after.Username;
                
                embedBuilder.WithTitle("❖﹒Nickname change . .");
                embedBuilder.WithDescription("### BEFORE : \n" + beforeUser.Nickname + "\n### AFTER : \n" + name);
                embedBuilder.WithCurrentTimestamp();
                if (channel != null) await channel.SendMessageAsync(embed: embedBuilder.Build());
                return;
            } 
            
            if (beforeUser.Username != after.Username) {
                embedBuilder.WithTitle("❖﹒Username change . .");
                embedBuilder.WithDescription("### BEFORE : \n" + beforeUser.Username + "\n### AFTER : \n" + after.Username);
                embedBuilder.WithCurrentTimestamp();
                if (channel != null) await channel.SendMessageAsync(embed: embedBuilder.Build());
                return;
            }

            try {
                var beforeAvatarUrl = beforeUser.GetGuildAvatarUrl() ?? beforeUser.GetAvatarUrl();
                var afterAvatarUrl = after.GetGuildAvatarUrl() ?? after.GetAvatarUrl();

                if (beforeAvatarUrl != afterAvatarUrl) {

                    await _db.SetAvatarUrl(after.Id, afterAvatarUrl ?? after.GetDefaultAvatarUrl());

                    var leftBytes =
                        await HttpClient.GetByteArrayAsync(beforeAvatarUrl ?? beforeUser.GetDefaultAvatarUrl());
                    var rightBytes = await HttpClient.GetByteArrayAsync(afterAvatarUrl ?? after.GetDefaultAvatarUrl());

                    var left = Image.Load(leftBytes);
                    var right = Image.Load(rightBytes);

                    var totalWidth = left.Width + right.Width;
                    var totalHeight = Math.Max(left.Height, right.Height);

                    using var combined = new Image<Rgba32>(totalWidth, totalHeight);
                    combined.Mutate(ctx => ctx
                        .DrawImage(left, new Point(0, 0), 1f)
                        .DrawImage(right, new Point(left.Width, 0), 1f)
                    );

                    await using var stream = new MemoryStream();
                    await combined.SaveAsPngAsync(stream);
                    stream.Position = 0;

                    embedBuilder
                        .WithTitle("❖﹒Avatar change . .")
                        .WithImageUrl("attachment://combined.png")
                        .WithCurrentTimestamp();

                    if (channel != null)
                        await channel.SendFileAsync(stream, "combined.png", embed: embedBuilder.Build());
                }
            } catch {
                await LogExceptionWatch(guild.Id, text: $"{beforeUser.Nickname}'s avatar wasn't cached!");
            }

            var addedRoles = after.Roles.Except(beforeUser.Roles);
            var removedRoles = beforeUser.Roles.Except(after.Roles);
            embedBuilder.WithTitle("❖﹒Roles changed . .");
            var content = "";

            foreach (var role in addedRoles) {
                content += "- Added <@&" + role.Id + ">\n";
            }

            foreach (var role in removedRoles) {
                content += "- Removed <@&" + role.Id + ">\n";
            }
            
            if (string.IsNullOrEmpty(content)) return;
            embedBuilder.WithDescription(content);
            embedBuilder.WithCurrentTimestamp();
            if (channel != null) await channel.SendMessageAsync(embed: embedBuilder.Build());
        } catch (Exception e) {
            await LogExceptionWatch(guild.Id, exception: e);
        }
    }
    
    public async Task LogInvite(SocketInvite invite, SocketGuild guild) {
        
        try {
            var channel = guild.GetChannel(1482805129613938860) as ISocketMessageChannel;
            
            var embed = (new EmbedBuilder()
                .WithAuthor(invite.Code)
                .WithTitle("❖﹒Invite Created . .")
                .WithDescription("- Created by : " + invite.Inviter.Username + "\n- Expires at : " + invite.ExpiresAt)
                .WithFooter(invite.Inviter.Id.ToString())
                .WithCurrentTimestamp()
                .WithColor(0xBFA55F)).Build();

            if (channel != null) await channel.SendMessageAsync(embed: embed);
        } catch (Exception e) {
            await LogExceptionWatch(guild.Id, exception: e);
        }
    }
    
    public async Task LogUserJoined(SocketGuildUser user, SocketGuild guild) {
        
        try {
            var welcomeChannel = _client.GetChannel(1473208226278408275) as ISocketMessageChannel;
            var logChannel = guild.GetChannel(1482805129613938860) as ISocketMessageChannel;
                        
            var rnd = new Random();
            var random = rnd.Next(0, 3);
            Color color;
            
            switch (random) {
                
                case 0:
                    color = 0xFF312C;
                    break;
                case 1:
                    color = 0x44786F;
                    break;
                case 2:
                    color = 0xBFA55F;
                    break;
                default:
                    color = 0xFF312C;
                    break;
            }
            
                var embedBuilder = new EmbedBuilder()
                    .WithAuthor("Welcome to the Sangō Idol-Defense Force!")
                    .WithThumbnailUrl(user.GetAvatarUrl())
                    .WithDescription("Step right in, we've been waiting for you. || <:sango_emblem_mono:1492222638980989138>\n\n✦ Grab your https://discord.com/channels/1471660035854569505/1473208251100299337, \nㅤㅤㅤread it *f__ront to bac__k*!\n✦ Tell us more about yourself in https://discord.com/channels/1471660035854569505/1473208770216591422.\n✦ If you're here because of a l__ive even__t,\nㅤ skip all of that and just j__oin the populated V__C!")
                    .WithColor(color)
                    .WithImageUrl("https://64.media.tumblr.com/384045d1eed5c0aa490e00aa98456239/c6b43c8a326634f0-7e/s2048x3072/8ae54d651ee2b0f75768d902e80ff1ec77417d08.pnj")
                    .WithFooter("『 恐れも、惨めさも、怒りも無く！GO STRIKE! 』");

            if (welcomeChannel != null) {
                var embed = embedBuilder.Build();
                await welcomeChannel.SendMessageAsync(embed: embed);
            }
            
            var logEmbed = (new EmbedBuilder()
                .WithAuthor("|| " + user.DisplayName, user.GetAvatarUrl())
                .WithTitle("❖﹒Prospect Approaches . .")
                .WithDescription(user.Mention)
                .WithFooter(user.Id.ToString())
                .WithCurrentTimestamp()
                .WithColor(0x44786F)).Build();

            if (logChannel != null) await logChannel.SendMessageAsync(embed: logEmbed);
            await UpdateStatChannel(guild);
        } catch (Exception e) {
            await LogExceptionWatch(guild.Id, exception: e);
        }
        
        await user.AddRolesAsync([1473369716792885402, 1473370059950002318, 1473370439526125599, 1473371454790832304]);
    }

    public async Task LogMemberLeft(SocketGuild guild, SocketUser user) {
        
        try {
            var channel = guild.GetChannel(1482805129613938860) as ISocketMessageChannel;

            var embed = (new EmbedBuilder()
                .WithAuthor("|| " + user.Username, user.GetAvatarUrl())
                .WithTitle("❖﹒Prospect Left . .")
                .WithDescription(user.Mention)
                .WithFooter(user.Id.ToString())
                .WithCurrentTimestamp()
                .WithColor(0xFF312C)).Build();

            if (channel != null) await channel.SendMessageAsync(embed: embed);

            var storedUsername = await _db.GetUsername(user.Id);
            if (!string.IsNullOrEmpty(storedUsername)) {
                await _db.Remove(user.Id);
            }
            await UpdateStatChannel(guild);
        } catch (Exception e) {
            await LogExceptionWatch(guild.Id, exception: e);
        }
    }

    public async Task LogMemberBanned(SocketUser user, SocketGuild guild) {
        
        try {
            var channel = guild.GetChannel(1482805129613938860) as ISocketMessageChannel;
     
            var embed = (new EmbedBuilder()
                .WithAuthor("|| " + user.Username, user.GetAvatarUrl())
                .WithTitle("❖﹒Member Dishonorably Discharged . .")
                .WithDescription(user.Mention)
                .WithFooter(user.Id.ToString())
                .WithCurrentTimestamp()
                .WithColor(0xFF312C)).Build();

            if (channel != null) await channel.SendMessageAsync(embed: embed);

            var storedUsername = await _db.GetUsername(user.Id);
            if (!string.IsNullOrEmpty(storedUsername)) {
                await _db.Remove(user.Id);
            }
        } catch (Exception e) {
            await LogExceptionWatch(guild.Id, exception: e);
        }
    }

    public async Task LogMessageDelete(Cacheable<IMessage, ulong> message, Cacheable<IMessageChannel, ulong> messageChannel, SocketGuild guild) {

        var logChannel = guild.GetChannel(1482805129613938860) as ISocketMessageChannel;
        var msg = message.Value;
        var channel = messageChannel.Value;
        
        if (msg.Author is SocketGuildUser author) {
            var authorName = author.Nickname ?? author.Username;

            if (msg.Author.IsBot) return;

            if (channel == null) {
                Console.WriteLine("Channel was not cached.");
                await LogExceptionWatch(guild.Id, text: "Channel was not cached.");
                return;
            }

            var embedBuilder = new EmbedBuilder()
                .WithAuthor("|| " + authorName, author.GetGuildAvatarUrl() ?? msg.Author.GetAvatarUrl())
                .WithTitle("❖﹒Message removed in <#" + channel.Id + "> . .")
                .WithDescription(string.IsNullOrEmpty(msg.Content) ? "*No text content*" : msg.Content)
                .WithFooter(msg.Author.Id.ToString())
                .WithCurrentTimestamp()
                .WithColor(0xFF312C);

            if (logChannel != null) await logChannel.SendMessageAsync(embed: embedBuilder.Build());
        }

        if (msg.Attachments.Count == 0) return;

        var fileAttachments = new List<FileAttachment>();
        var attachmentUrls = new List<string>();
        var seenNames = new HashSet<string>();

        foreach (var attachment in msg.Attachments) {
            try {
                
                if (attachment.Size > 8 * 1024 * 1024) {
                    if (logChannel != null)
                        await logChannel.SendMessageAsync(
                            $"Attachment too large to log! : `{attachment.Filename}` ({attachment.Size / 1024 / 1024}MB)\n[※ Link . .](<{attachment.Url}>)");
                    continue;
                }

                var filename = attachment.Filename;
                if (!seenNames.Add(filename)) {
                    var ext = Path.GetExtension(filename);
                    var name = Path.GetFileNameWithoutExtension(filename);
                    filename = $"{name}_{seenNames.Count}{ext}";
                    seenNames.Add(filename);
                }

                var bytes = await HttpClient.GetByteArrayAsync(attachment.Url);
                var fa = new FileAttachment(new MemoryStream(bytes), filename);
                fileAttachments.Add(fa);
                attachmentUrls.Add($"attachment://{filename}");
            } catch (Exception ex) {
                Console.WriteLine(ex);
                await LogExceptionWatch(guild.Id, exception: ex);
            }
        }

        if (fileAttachments.Count == 0) return;

        var container = new ContainerBuilder()
            .WithSpoiler(true)
            .WithMediaGallery(attachmentUrls);

        var components = new ComponentBuilderV2()
            .AddComponent(container)
            .Build();

        try {
            if (logChannel != null)
                await logChannel.SendFilesAsync(
                    fileAttachments,
                    components: components,
                    flags: MessageFlags.ComponentsV2
                );
        } finally {
            foreach (var f in fileAttachments) f.Dispose();
        }
    }
    
    public async Task LogMessageUpdate(Cacheable<IMessage, ulong> beforeMessage, SocketMessage afterMessage, ISocketMessageChannel messageChannel, SocketGuild guild) {
        
        var channel = guild.GetChannel(1482805129613938860) as ISocketMessageChannel;
        var before = beforeMessage.Value;
        var author = before.Author as SocketGuildUser;
        var authorName = author?.Nickname ?? author?.Username;
        
        if (before.Author.IsBot) return;
        
        if (before.Author.Id == 1477898638410911835) return;
        
        if (before.Content.Trim().Equals(afterMessage.Content.Trim())) return;
        
        var embed = (new EmbedBuilder()
            .WithAuthor("|| " + authorName, author?.GetGuildAvatarUrl() ?? afterMessage.Author.GetAvatarUrl())
            .WithTitle("❖﹒Message edited in <#" + messageChannel.Id + "> . .")
            .WithDescription("### BEFORE : \n" + before.Content + "\n\n### AFTER : \n" + afterMessage.Content)
            .WithFooter(afterMessage.Author.Id.ToString())
            .WithCurrentTimestamp()
            .WithColor(0xBFA55F)).Build();

        if (channel != null) await channel.SendMessageAsync(embed: embed);
    }

    public async Task LogWebhookUpdate(SocketGuild guild, SocketChannel destinationChannel) {
        
        try {
            var channel = guild.GetChannel(1482805129613938860) as ISocketMessageChannel;

            var embed = (new EmbedBuilder()
                .WithTitle("❖﹒Webhook Updated ! !")
                .WithDescription("A webhook has been updated for #" + destinationChannel + "!")
                .WithCurrentTimestamp()
                .WithColor(0xBFA55F)).Build();

            if (channel != null) await channel.SendMessageAsync("Staff, make sure this Webhook change is legitimate.", embed: embed);
        } catch (Exception e) {
            Console.WriteLine(e);
            await LogExceptionWatch(guild.Id, exception: e);
        }
    }

    public async Task LogMassRemove(SocketSlashCommand command, int amount) {
        
        try {
            if (command.GuildId != null) {
                
                var guild = _client.GetGuild(command.GuildId.Value);
                var user = command.User as SocketGuildUser;
                var messageChannel = command.Channel;
            
                var channel = guild.GetChannel(1482805129613938860) as ISocketMessageChannel;
     
                var embed = (new EmbedBuilder()
                    .WithAuthor("|| " + user?.Nickname, user?.GetGuildAvatarUrl() ?? user?.GetAvatarUrl())
                    .WithTitle("❖﹒Mass Message Removal")
                    .WithDescription(user?.Mention + " removed **" + amount + "** messages in <#" + messageChannel.Id + ">.")
                    .WithFooter(user?.Id.ToString())
                    .WithCurrentTimestamp()
                    .WithColor(0xFF312C)).Build();

                if (channel != null) await channel.SendMessageAsync(embed: embed);
            }
        } catch (Exception e) {
            Console.WriteLine(e);
            var guild = _client.GetGuild((ulong)_guildId!);
            await LogExceptionWatch(guild.Id, exception: e);
        }
    }
    
    public async Task CreateOrUpdateStatChannel() {
        
        SocketVoiceChannel? channel = null;
        var guild = _client.GetGuild((ulong)_guildId!);
        
        await guild.DownloadUsersAsync();
        var memberCount = guild.Users.Count(u => !u.IsBot);
        
        var channelId = _db.GetStatChannel(guild.Id);
        if (channelId != null)
            channel = guild.GetChannel(channelId.Value) as SocketVoiceChannel;
        if (channel == null) {
            var created = await guild.CreateVoiceChannelAsync($"✦ idols : {memberCount}", props => {
                props.CategoryId = 1473208155210252381;
            });
            _db.SetStatChannel(guild.Id, created.Id);
        }
        
        await UpdateStatChannel(guild, memberCount);
        await _client.SetActivityAsync(new CustomStatusGame("Helping " + memberCount + " enlisted..."));
    }
    
    private async Task UpdateStatChannel(SocketGuild guild, int? memberCount = null) {
        
        var channelId = _db.GetStatChannel(guild.Id);
        if (channelId == null) return;
        if (guild.GetChannel(channelId.Value) is SocketVoiceChannel channel) {
            memberCount ??= guild.Users.Count(u => !u.IsBot);
            var expectedName = $"✦ idols : {memberCount} !";
            
            if (channel.Name == expectedName) return;
            
            await channel.ModifyAsync(props => { props.Name = expectedName; });
            await _client.SetActivityAsync(new CustomStatusGame("Helping " + memberCount + " enlisted..."));
        }
    }

    public async Task LogExceptionWatch(ulong guildId, LogMessage? msg = null, Exception? exception = null, string? text = null) {

        if (_client.GetGuild(guildId).GetChannel(1540194084633710602) is IThreadChannel thread) {
            
            if (text != null) await thread.SendMessageAsync("[ DESCRIPTION ]\n\n" + text.ToUpper());
            if (msg != null) await thread.SendMessageAsync(msg.ToString());
            if (exception != null) await thread.SendMessageAsync(exception.ToString());
        }
    }
}
