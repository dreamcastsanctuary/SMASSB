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
    
    public async Task LogUserJoined(SocketGuildUser user, SocketGuild guild) {
        try {
            var welcomeChannel = _client.GetChannel(1473208226278408275) as ISocketMessageChannel;
            var logChannel = guild.GetChannel(1482805129613938860) as ISocketMessageChannel;
                
            Random rnd = new Random();
            int random = rnd.Next(0, 3);
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

            if (welcomeChannel != null) {
                
                // string ordinal;
                // var memberCount = guild.MemberCount - 2;
                
                // if (memberCount % 10 == 1 && memberCount != 11) { ordinal = "st"; }
                // else if  (memberCount % 10 == 2 && memberCount != 12) { ordinal = "nd"; }
                // else if ( memberCount % 10 == 3) { ordinal = "rd"; }
                // else { ordinal = "th"; }
                
                Embed embed = (new EmbedBuilder()
                    .WithAuthor("Welcome to the Sangō Military-Idol Academy!")
                    .WithThumbnailUrl(user.GetAvatarUrl())
                    .WithDescription("Step right in, we've been waiting for you. || <:sango_emblem_mono:1492222638980989138>\n\n✦ Grab your https://discord.com/channels/1471660035854569505/1473208251100299337, read it *f__ront to bac__k*!\n✦ Tell us more about yourself in https://discord.com/channels/1471660035854569505/1473208770216591422.")
                    .WithColor(color)
                    .WithImageUrl("https://64.media.tumblr.com/51b15f41ee5f58c722ebac09ae3d165e/6a794ae0ea17c706-cc/s2048x3072/39b7a663a13e95d68c46239534bea85f9e008f26.pnj")
                    .WithFooter("『 太陽はまた昇る！GO STRIKE! 』")).Build();
                
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
        } catch (Exception e) {
            Console.WriteLine(e);
        }
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
                if (await _db.GetRank(user.Id) != "Kōsohei") {
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
        
        if (msg.Author.IsBot) return;
        
        if (msg == null) {
            Console.WriteLine("Deleted message was not cached.");
            return;
        }
        
        Embed embed = (new EmbedBuilder()
            .WithAuthor("|| " + msg.Author.Username , msg.Author.GetAvatarUrl())
            .WithTitle("❖﹒Message removed in #" + chnl.Name + " . .")
            .WithDescription(msg.Content)
            .WithFooter(msg.Author.Id.ToString())
            .WithCurrentTimestamp()
            .WithColor(0xFF312C)).Build();
        
        await logChannel.SendMessageAsync(embed: embed);
    }
    
    public async Task LogMessageUpdate(Cacheable<IMessage, ulong> beforeMessage, SocketMessage afterMessage, ISocketMessageChannel messageChannel, SocketGuild guild) {
        
        var channel = guild.GetChannel(1482805129613938860) as ISocketMessageChannel;
        var before = beforeMessage.Value;
        var after = afterMessage;

        if (before.Author.IsBot) return;
        
        if (before == null) {
            Console.WriteLine("Changed message was not cached.");
            return;
        }
        
        if (before.Author.Id == 1477898638410911835) return;
        
        Embed embed = (new EmbedBuilder()
            .WithAuthor("|| " + after.Author.Username , after.Author.GetAvatarUrl())
            .WithTitle("❖﹒Message edited in #" + messageChannel.Name + " . .")
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
            var user = command.User;
            var chnl = command.Channel;
            
            var channel = guild.GetChannel(1482805129613938860) as ISocketMessageChannel;
     
            Embed embed = (new EmbedBuilder()
                .WithAuthor("|| " + user.Username, user.GetAvatarUrl())
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

        var channelId = _db.GetStatChannel(guild.Id);
        if (channelId != null)
            channel = guild.GetChannel(channelId.Value) as SocketVoiceChannel;

        if (channel == null) {
            var created = await guild.CreateVoiceChannelAsync("✦ idols : 0", props => {
                props.CategoryId = 1473208155210252381;
            });
            _db.SetStatChannel(guild.Id, created.Id);
            channel = guild.GetChannel(created.Id) as SocketVoiceChannel;
        }

        await UpdateStatChannel(guild);
    }

    private async Task UpdateStatChannel(SocketGuild guild) {
        var channelId = _db.GetStatChannel(guild.Id);
        if (channelId == null) return;

        if (guild.GetChannel(channelId.Value) is SocketVoiceChannel channel) {
            var memberCount = guild.Users.Count(u => !u.IsBot);
            await channel.ModifyAsync((VoiceChannelProperties props) => {
                props.Name = $"✦ idols : {memberCount}";
            });
        }
    }
}