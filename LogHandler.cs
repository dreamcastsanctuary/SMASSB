using System.Collections.Concurrent;
using System.Drawing.Printing;
using Discord;
using Discord.WebSocket;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Color = Discord.Color;
using Image = SixLabors.ImageSharp.Image;

namespace SMASSB;

public class LogHandler {
    
    private readonly DiscordSocketClient _client;
    private DatabaseService _db;
    private static readonly HttpClient _httpClient = new HttpClient();

    public LogHandler(DiscordSocketClient client,
                      DatabaseService db) {
        _client = client;
        _db = db;
    }

    public async Task LogMemberUpdate(Cacheable<SocketGuildUser, ulong> before, SocketGuildUser after, SocketGuild guild) {
        try {
            EmbedBuilder embedBuilder = new EmbedBuilder()
                .WithAuthor("|| " + after.Username, after.GetGuildAvatarUrl() ?? after.GetAvatarUrl())
                .WithFooter(after.Id.ToString())
                .WithColor(0xBFA55F);
            
            var beforeUser = await before.GetOrDownloadAsync();
            var channel = guild.GetChannel(1482805129613938860) as ISocketMessageChannel;
            
            if (beforeUser.Nickname != after.Nickname) {
                embedBuilder.WithTitle("❖﹒Nickname change . .");
                embedBuilder.WithDescription("### BEFORE : \n" + beforeUser.Nickname + "\n### AFTER : \n" + after.Nickname);
                embedBuilder.WithCurrentTimestamp();
                await channel.SendMessageAsync(embed: embedBuilder.Build());
                return;
            } 
            
            if (beforeUser.Username != after.Username) {
                embedBuilder.WithTitle("❖﹒Username change . .");
                embedBuilder.WithDescription("### BEFORE : \n" + beforeUser.Username + "\n### AFTER : \n" + after.Username);
                embedBuilder.WithCurrentTimestamp();
                await channel.SendMessageAsync(embed: embedBuilder.Build());
                return;
            } 
            
            var beforeAvatarUrl = beforeUser.GetGuildAvatarUrl() ?? beforeUser.GetAvatarUrl();
            var afterAvatarUrl  = after.GetGuildAvatarUrl() ?? after.GetAvatarUrl();

            if (beforeAvatarUrl != afterAvatarUrl) {

                await _db.SetAvatarUrl(after.Id, afterAvatarUrl ?? after.GetDefaultAvatarUrl());

                var leftBytes  = await _httpClient.GetByteArrayAsync(beforeAvatarUrl ?? beforeUser.GetDefaultAvatarUrl());
                var rightBytes = await _httpClient.GetByteArrayAsync(afterAvatarUrl ?? after.GetDefaultAvatarUrl());

                using var left  = Image.Load(leftBytes);
                using var right = Image.Load(rightBytes);

                int totalWidth  = left.Width + right.Width;
                int totalHeight = Math.Max(left.Height, right.Height);

                using var combined = new Image<Rgba32>(totalWidth, totalHeight);
                combined.Mutate(ctx => ctx
                    .DrawImage(left, new Point(0, 0), 1f)
                    .DrawImage(right, new Point(left.Width, 0), 1f)
                );

                using var stream = new MemoryStream();
                combined.SaveAsPng(stream);
                stream.Position = 0;

                embedBuilder
                    .WithTitle("❖﹒Avatar change . .")
                    .WithImageUrl("attachment://combined.png")
                    .WithCurrentTimestamp();

                await channel.SendFileAsync(stream, "combined.png", embed: embedBuilder.Build());
            }

            var addedRoles = after.Roles.Except(beforeUser.Roles);
            var removedRoles = beforeUser.Roles.Except(after.Roles);
            embedBuilder.WithTitle("❖﹒Roles changed . .");
            string content = "";

            foreach (var role in addedRoles) {
                content += "- Added " + role.Name + "\n";
            }

            foreach (var role in removedRoles) {
                content += "- Removed " + role.Name + "\n";
            }
            
            if (string.IsNullOrEmpty(content)) return;
            embedBuilder.WithDescription(content);
            embedBuilder.WithCurrentTimestamp();
            await channel.SendMessageAsync(embed: embedBuilder.Build()); 
        } catch (Exception e) {
            Console.WriteLine(e);
        }
    }
    
    public async Task LogInvite(SocketInvite invite, SocketGuild guild) {
        try {
            var channel = guild.GetChannel(1482805129613938860) as ISocketMessageChannel;
            
            Embed embed = (new EmbedBuilder()
                .WithAuthor(invite.Code)
                .WithTitle("❖﹒Invite Created . .")
                .WithDescription("- Created by : " + invite.Inviter.Username + "\n- Expires at : " + invite.ExpiresAt)
                .WithFooter(invite.Inviter.Id.ToString())
                .WithCurrentTimestamp()
                .WithColor(0xBFA55F)).Build();
            
            await channel.SendMessageAsync(embed: embed);
        } catch (Exception e) {
            Console.WriteLine(e);
        }
    }
    
    public async Task LogUserJoined(SocketGuildUser user, SocketGuild guild, ConcurrentDictionary<string, int> inviteCache, IReadOnlyCollection<IInviteMetadata> newInvites) {
        
        try {
            var welcomeChannel = _client.GetChannel(1473208226278408275) as ISocketMessageChannel;
            var logChannel = guild.GetChannel(1482805129613938860) as ISocketMessageChannel;
            var usedInvite = newInvites.FirstOrDefault(newInv =>
                inviteCache.TryGetValue(newInv.Code, out int oldUses) &&
                newInv.Uses > oldUses
            );
            
            Random rnd = new Random();
            int random = rnd.Next(0, 3);
            Color color;
            
            var embedBuilder = new EmbedBuilder();
            
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
            
            if (usedInvite != null) {
                Console.WriteLine(">>>>>>>>>>>>>>>> INVITE CACHE SUCCEED.");
                if (usedInvite.Code == "SjtaFZDqWp") {
                    
                    embedBuilder = new EmbedBuilder()
                        .WithAuthor("Welcome to the Sangō Idol-Defense Force!")
                        .WithThumbnailUrl(user.GetAvatarUrl())
                        .WithDescription("So, you're here to watch us, right? || <:sango_emblem_mono:1492222638980989138>\n\n✦ Head down to our voice channel and join! There's no time to waste!\n✦ After you're done, read https://discord.com/channels/1471660035854569505/1473208251100299337 and grab yourself some roles here! -> https://discord.com/channels/1471660035854569505/1473208770216591422.")
                        .WithColor(color)
                        .WithImageUrl("https://64.media.tumblr.com/21c82a6e53d59335955b70197c129b12/c6b43c8a326634f0-7c/s2048x3072/7d052288ea61a62bca28f4d79b760240779a44e1.pnj")
                        .WithFooter("『 太陽はまた昇る！GO STRIKE! 』");

                    await user.AddRoleAsync(1475720710910382310);
                }
                else {
                    embedBuilder = new EmbedBuilder()
                        .WithAuthor("Welcome to the Sangō Idol-Defense Force!")
                        .WithThumbnailUrl(user.GetAvatarUrl())
                        .WithDescription("Step right in, we've been waiting for you. || <:sango_emblem_mono:1492222638980989138>\n\n✦ Grab your https://discord.com/channels/1471660035854569505/1473208251100299337, read it *f__ront to bac__k*!\n✦ Tell us more about yourself in https://discord.com/channels/1471660035854569505/1473208770216591422.")
                        .WithColor(color)
                        .WithImageUrl("https://64.media.tumblr.com/384045d1eed5c0aa490e00aa98456239/c6b43c8a326634f0-7e/s2048x3072/8ae54d651ee2b0f75768d902e80ff1ec77417d08.pnj")
                        .WithFooter("『 太陽はまた昇る！GO STRIKE! 』");
                }
            } else {
                Console.WriteLine(">>>>>>>>>>>>>>>> INVITE CACHE FAIL.");
                
                embedBuilder = new EmbedBuilder()
                    .WithAuthor("Welcome to the Sangō Idol-Defense Force!")
                    .WithThumbnailUrl(user.GetAvatarUrl())
                    .WithDescription("Step right in, we've been waiting for you. || <:sango_emblem_mono:1492222638980989138>\n\n✦ Grab your https://discord.com/channels/1471660035854569505/1473208251100299337, read it *f__ront to bac__k*!\n✦ Tell us more about yourself in https://discord.com/channels/1471660035854569505/1473208770216591422.")
                    .WithColor(color)
                    .WithImageUrl("https://64.media.tumblr.com/384045d1eed5c0aa490e00aa98456239/c6b43c8a326634f0-7e/s2048x3072/8ae54d651ee2b0f75768d902e80ff1ec77417d08.pnj")
                    .WithFooter("『 太陽はまた昇る！GO STRIKE! 』");
            }

            if (welcomeChannel != null) {
                
                // string ordinal;
                // var memberCount = guild.MemberCount - 2;
                
                // if (memberCount % 10 == 1 && memberCount != 11) { ordinal = "st"; }
                // else if  (memberCount % 10 == 2 && memberCount != 12) { ordinal = "nd"; }
                // else if ( memberCount % 10 == 3) { ordinal = "rd"; }
                // else { ordinal = "th"; }
                
                Embed embed = embedBuilder.Build();
                
                await welcomeChannel.SendMessageAsync(embed: embed);
            }
            
            Embed logEmbed = (new EmbedBuilder()
                .WithAuthor("|| " + user.DisplayName, user.GetAvatarUrl())
                .WithTitle("❖﹒Prospect Approaches . .")
                .WithDescription(user.Mention)
                .WithFooter(user.Id.ToString())
                .WithCurrentTimestamp()
                .WithColor(0x44786F)).Build();
            
            await logChannel.SendMessageAsync(embed: logEmbed);
            await UpdateStatChannel(guild);
            foreach (var inv in newInvites)
                Console.WriteLine($">>>>>>>>>>>>>>>>> New invite snapshot: {inv.Code} ({inv.Uses} uses)");
            foreach (var kv in inviteCache)
                Console.WriteLine($"\">>>>>>>>>>>>>>>>> Cached: {kv.Key} ({kv.Value} uses)");
        } catch (Exception e) {
            Console.WriteLine(e);
        }
        
        await user.AddRolesAsync([1473369716792885402, 1473370059950002318, 1473370439526125599, 1473371454790832304]);
    }

    public async Task LogMemberLeft(SocketGuild guild, SocketUser user) {
        try {
            var channel = guild.GetChannel(1482805129613938860) as ISocketMessageChannel;

            Embed embed = (new EmbedBuilder()
                .WithAuthor("|| " + user.Username, user.GetAvatarUrl())
                .WithTitle("❖﹒Prospect Unenrolled . .")
                .WithDescription(user.Mention)
                .WithFooter(user.Id.ToString())
                .WithCurrentTimestamp()
                .WithColor(0xFF312C)).Build();
            
            await channel.SendMessageAsync(embed: embed);

            var storedUsername = await _db.GetUsername(user.Id);
            
            if (!string.IsNullOrEmpty(storedUsername)) {
                if (await _db.GetRank(user.Id) != "Kōhosei") {
                    await _db.TransferFromEnrolledToUnenrolled(user.Id);
                } else {
                    await _db.Remove(user.Id);
                }
            }
            await UpdateStatChannel(guild);
        } catch (Exception e) {
            Console.WriteLine(e);
        }
    }

    public async Task LogMemberBanned(SocketUser user, SocketGuild guild) {
        
        try {
            var channel = guild.GetChannel(1482805129613938860) as ISocketMessageChannel;
     
            Embed embed = (new EmbedBuilder()
                .WithAuthor("|| " + user.Username, user.GetAvatarUrl())
                .WithTitle("❖﹒Member Dishonorably Discharged . .")
                .WithDescription(user.Mention)
                .WithFooter(user.Id.ToString())
                .WithCurrentTimestamp()
                .WithColor(0xFF312C)).Build();
            
            await channel.SendMessageAsync(embed: embed);
            
            var storedUsername = await _db.GetUsername(user.Id);
            if (!string.IsNullOrEmpty(storedUsername))
            {
                await _db.Remove(user.Id);
            }
        } catch (Exception e) {
            Console.WriteLine(e);
        }
    }

    public async Task LogMessageDelete(Cacheable<IMessage, ulong> message, Cacheable<IMessageChannel, ulong> messageChannel, SocketGuild guild) {

        var logChannel = guild.GetChannel(1482805129613938860) as ISocketMessageChannel;
        var msg = message.Value;
        var chnl = messageChannel.Value;
        var author = msg.Author as SocketGuildUser;
        var authorName = author.Nickname ?? author.Username;

        if (msg == null) {
            Console.WriteLine("Deleted message was not cached.");
            return;
        }

        if (msg.Author.IsBot) return;

        if (chnl == null) {
            Console.WriteLine("Channel was not cached.");
            return;
        }

        var embedBuilder = new EmbedBuilder()
            .WithAuthor("|| " + authorName, author.GetGuildAvatarUrl() ?? msg.Author.GetAvatarUrl())
            .WithTitle("❖﹒Message removed in <#" + chnl.Id + "> . .")
            .WithDescription(string.IsNullOrEmpty(msg.Content) ? "*No text content*" : msg.Content)
            .WithFooter(msg.Author.Id.ToString())
            .WithCurrentTimestamp()
            .WithColor(0xFF312C);

        await logChannel.SendMessageAsync(embed: embedBuilder.Build());

        if (msg.Attachments.Count == 0) return;

        var fileAttachments = new List<FileAttachment>();
        var attachmentUrls = new List<string>();
        var seenNames = new HashSet<string>();

        using var httpClient = new HttpClient();

        foreach (var attachment in msg.Attachments) {
            try {
                if (attachment.Size > 8 * 1024 * 1024) {
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

                var bytes = await httpClient.GetByteArrayAsync(attachment.Url);
                var fa = new FileAttachment(new MemoryStream(bytes), filename);
                fileAttachments.Add(fa);
                attachmentUrls.Add($"attachment://{filename}");
            } catch (Exception ex) {
                Console.WriteLine(ex);
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
        var after = afterMessage;
        var author = before.Author as SocketGuildUser;
        var authorName = author.Nickname ?? author.Username;
        
        if (before == null) {
            Console.WriteLine("Changed message was not cached.");
            return;
        }
        
        if (before.Author.IsBot) return;
        
        if (before.Author.Id == 1477898638410911835) return;
        
        if (before.Content.Trim().Equals(after.Content.Trim())) return;
        
        Embed embed = (new EmbedBuilder()
            .WithAuthor("|| " + authorName, author.GetGuildAvatarUrl() ?? after.Author.GetAvatarUrl())
            .WithTitle("❖﹒Message edited in <#" + messageChannel.Id + "> . .")
            .WithDescription("### BEFORE : \n" + before.Content + "\n\n### AFTER : \n" + after.Content)
            .WithFooter(after.Author.Id.ToString())
            .WithCurrentTimestamp()
            .WithColor(0xBFA55F)).Build();
        
        await channel.SendMessageAsync(embed: embed);
    }

    public async Task LogWebhookUpdate(SocketGuild guild, SocketChannel destinationChannel) {
        try {
            var channel = guild.GetChannel(1482805129613938860) as ISocketMessageChannel;

            Embed embed = (new EmbedBuilder()
                .WithTitle("❖﹒Webhook Updated ! !")
                .WithDescription("A webhook has been updated for #" + destinationChannel + "!")
                .WithCurrentTimestamp()
                .WithColor(0xBFA55F)).Build();

            await channel.SendMessageAsync("Staff, make sure this Webhook change is legitimate.", embed: embed);
        } catch (Exception e) {
            Console.WriteLine(e);
        }
    }

    public async Task LogMassRemove(SocketSlashCommand command, int amount) {
        try {
            var guild = _client.GetGuild(command.GuildId.Value);
            var user = command.User as  SocketGuildUser;
            var chnl = command.Channel;
            
            var channel = guild.GetChannel(1482805129613938860) as ISocketMessageChannel;
     
            Embed embed = (new EmbedBuilder()
                .WithAuthor("|| " + user.Nickname, user.GetGuildAvatarUrl() ?? user.GetAvatarUrl())
                .WithTitle("❖﹒Mass Message Removal")
                .WithDescription(user.Mention + " removed **" + amount + "** messages in <#" + chnl.Id + ">.")
                .WithFooter(user.Id.ToString())
                .WithCurrentTimestamp()
                .WithColor(0xFF312C)).Build();
            
            await channel.SendMessageAsync(embed: embed);
        } catch (Exception e) {
            Console.WriteLine(e);
        }
    }
    
    public async Task CreateOrUpdateStatChannel(SocketGuild guild) {
        SocketVoiceChannel? channel = null;
        
        await guild.DownloadUsersAsync();
        var memberCount = guild.Users.Count(u => !u.IsBot); // count once, after download
        
        var channelId = _db.GetStatChannel(guild.Id);
        if (channelId != null)
            channel = guild.GetChannel(channelId.Value) as SocketVoiceChannel;
        if (channel == null) {
            var created = await guild.CreateVoiceChannelAsync($"✦ idols : {memberCount}", props => {
                props.CategoryId = 1473208155210252381;
            });
            _db.SetStatChannel(guild.Id, created.Id);
            channel = guild.GetChannel(created.Id) as SocketVoiceChannel;
        }
        await UpdateStatChannel(guild, memberCount);
    }
    
    private async Task UpdateStatChannel(SocketGuild guild, int? memberCount = null) {
        var channelId = _db.GetStatChannel(guild.Id);
        if (channelId == null) return;
        if (guild.GetChannel(channelId.Value) is SocketVoiceChannel channel) {
            memberCount ??= guild.Users.Count(u => !u.IsBot);
            var expectedName = $"✦ idols : {memberCount} !";
            
            if (channel.Name == expectedName) return;
            
            await channel.ModifyAsync((VoiceChannelProperties props) => {
                props.Name = expectedName;
            });
        }
    }
}
