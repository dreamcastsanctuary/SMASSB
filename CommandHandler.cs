using Discord;
using Discord.Net;
using Discord.WebSocket;
using SMASSB.Commands;
using Newtonsoft.Json;

namespace SMASSB;
public class CommandHandler {
    
    private readonly DiscordSocketClient _client;
    private RewardSystem _rewardSystem;
    private MeetingSystem _meetingSystem;
    private RoleSystem _roleSystem;
    private IdSystem _idSystem;
    private PointSystem _pointSystem;
    private GeneralSystem _generalSystem;
    private DatabaseService _db;

    public CommandHandler(DiscordSocketClient client,
                          RewardSystem rewardSystem,
                          MeetingSystem meetingSystem,
                          RoleSystem roleSystem,
                          IdSystem idSystem,
                          PointSystem pointSystem,
                          GeneralSystem generalSystem,
                          DatabaseService db) {
        
        _client = client;
        _client.SlashCommandExecuted += SlashCommandHandler;
        _rewardSystem = rewardSystem;
        _meetingSystem = meetingSystem;
        _roleSystem = roleSystem;
        _idSystem = idSystem;
        _pointSystem = pointSystem;
        _generalSystem = generalSystem;
        _db = db;
    }
    public async Task RegisterCommands(SocketGuild guild) {

        List<SlashCommandBuilder> commands = new List<SlashCommandBuilder>();
        
        // REWARDSYSTEM.
        
        commands.Add(new SlashCommandBuilder()
            .WithName("rewardko")
            .WithDescription("Rewards a kohosei a specific item / document.")
            .AddOption("enlisted1", ApplicationCommandOptionType.User, "The @ of the enlisted.", isRequired: true).AddOption("enlisted2", ApplicationCommandOptionType.User, "The @ of the enlisted.", isRequired: false).AddOption("enlisted3", ApplicationCommandOptionType.User, "The @ of the enlisted.", isRequired: false).AddOption("enlisted4", ApplicationCommandOptionType.User, "The @ of the enlisted.", isRequired: false).AddOption("enlisted5", ApplicationCommandOptionType.User, "The @ of the enlisted.", isRequired: false).AddOption("enlisted6", ApplicationCommandOptionType.User, "The @ of the enlisted.", isRequired: false).AddOption("enlisted7", ApplicationCommandOptionType.User, "The @ of the enlisted.", isRequired: false).AddOption("enlisted8", ApplicationCommandOptionType.User, "The @ of the enlisted.", isRequired: false).AddOption("enlisted9", ApplicationCommandOptionType.User, "The @ of the enlisted.", isRequired: false).AddOption("enlisted10", ApplicationCommandOptionType.User, "The @ of the enlisted.", isRequired: false)
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("item").WithDescription("The specific item / document that is to be rewarded.")
                .WithRequired(true)
                .AddChoice("Headphones", 1).AddChoice("Sword", 2).AddChoice("Uniform", 3)
                .WithType(ApplicationCommandOptionType.Integer))
            .WithDefaultMemberPermissions(GuildPermission.ManageRoles));

        commands.Add(new SlashCommandBuilder()
            .WithName("rewardaccomp")
            .WithDescription("Gives a enlisted a specific award after achieving an accomplishment.")
            .AddOption("enlisted", ApplicationCommandOptionType.User, "The name of the enlisted.", isRequired: true)
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("item").WithDescription("The specific item / document that is to be rewarded.")
                .WithRequired(true)
                .AddChoice("Transfer", 1).AddChoice("Supporter", 2).AddChoice("HighScouter", 3).AddChoice("MAXScouter",4)
                .AddChoice("PerfectPitch", 5).AddChoice("WorldClassIdol", 6)
                .AddChoice("HonorsCollegeI", 7).AddChoice("HonorsCollegeII", 8).AddChoice("Rebirth", 9)
                .WithType(ApplicationCommandOptionType.Integer))
            .WithDefaultMemberPermissions(GuildPermission.ManageRoles));
        
        // MEETINGSYSTEM.

        commands.Add(new SlashCommandBuilder()
            .WithName("meeting")
            .WithDescription("Creates a private meeting room with the staff and person provided.")
            .AddOption("person", ApplicationCommandOptionType.User, "The @ of the person.", isRequired: true)
            .AddOption("meeting_name", ApplicationCommandOptionType.String, "What you want to call this meeting; add - instead of spaces.", isRequired: true)
            .WithDefaultMemberPermissions(GuildPermission.ManageRoles));
        
        commands.Add(new SlashCommandBuilder()
            .WithName("meetingblist")
            .WithDescription("Creates a private meeting room with our PR Liaison Officer and the person provided.")
            .AddOption("person", ApplicationCommandOptionType.User, "The @ of the person.", isRequired: true)
            .AddOption("meeting_name", ApplicationCommandOptionType.String, "What you want to call this meeting; add - instead of spaces.", isRequired: true)
            .WithDefaultMemberPermissions(GuildPermission.Administrator));
        
        commands.Add(new SlashCommandBuilder()
            .WithName("meetingreprimand")
            .WithDescription("Creates a private meeting room with only the person provided, for use with discipline.")
            .AddOption("person", ApplicationCommandOptionType.User, "The @ of the person.", isRequired: true)
            .AddOption("meeting_name", ApplicationCommandOptionType.String, "What you want to call this meeting; add - instead of spaces.", isRequired: true)
            .WithDefaultMemberPermissions(GuildPermission.ManageRoles));
        
        commands.Add(new SlashCommandBuilder()
            .WithName("meetingclose")
            .WithDescription("Closes the current thread if it is a meeting room.")
            .WithDefaultMemberPermissions(GuildPermission.ManageRoles));
        
        // ROLESYSTEM.

        commands.Add(new SlashCommandBuilder()
            .WithName("preenlist")
            .WithDescription("Pre-enlists a civilian into a prospect; to be used during in-server uniform check.")
            .AddOption("civilian", ApplicationCommandOptionType.User, "The @ of the civilian.", isRequired: true)
            .AddOption("claim_name", ApplicationCommandOptionType.String, "The claim name of the civilian.", isRequired: true)
            .WithDefaultMemberPermissions(GuildPermission.ManageRoles));
        
        commands.Add(new SlashCommandBuilder()
            .WithName("enlist")
            .WithDescription("Enlists a kōhosei into a enlisted.")
            .AddOption("kōhosei", ApplicationCommandOptionType.User, "The @ of the enlisted.", isRequired: true)
            .WithDefaultMemberPermissions(GuildPermission.Administrator));
        
        commands.Add(new SlashCommandBuilder()
            .WithName("forceenlist")
            .WithDescription("Force enlists a user.")
            .AddOption("civilian", ApplicationCommandOptionType.User, "The @ of the user.", isRequired: true)
            .AddOption("claim_name", ApplicationCommandOptionType.String, "The claim name of the civilian.", isRequired: true)
            .AddOption("rank_name", ApplicationCommandOptionType.String, "The rank to be placed in the database.", isRequired: true)
            .WithDefaultMemberPermissions(GuildPermission.Administrator));

        commands.Add(new SlashCommandBuilder()
            .WithName("forceremove")
            .WithDescription("Force removes a user.")
            .AddOption("civilian", ApplicationCommandOptionType.User, "The @ of the user.", isRequired: true)
            .WithDefaultMemberPermissions(GuildPermission.Administrator));
        
        commands.Add(new SlashCommandBuilder()
            .WithName("promote")
            .WithDescription("Promotes the given list of enlisted to the specific rank.")
            .AddOption("add_rank", ApplicationCommandOptionType.Role, "The role to be given.", isRequired: true).AddOption("add_rank_category", ApplicationCommandOptionType.Role, "If needed, the next rank category (IE: Enlisted, Non-Commissioned Officer, etc.", isRequired: false)
            .AddOption("remove_rank", ApplicationCommandOptionType.Role, "The role to be taken away.", isRequired: true).AddOption("remove_rank_category", ApplicationCommandOptionType.Role, "If needed, the previous rank category (IE: Enlisted, Non-Commissioned Officer, etc.", isRequired: false)
            .AddOption("enlisted1", ApplicationCommandOptionType.User, "The @ of the enlisted.", isRequired: true).AddOption("enlisted2", ApplicationCommandOptionType.User, "The @ of the enlisted.", isRequired: false).AddOption("enlisted3", ApplicationCommandOptionType.User, "The @ of the enlisted.", isRequired: false).AddOption("enlisted4", ApplicationCommandOptionType.User, "The @ of the enlisted.", isRequired: false).AddOption("enlisted5", ApplicationCommandOptionType.User, "The @ of the enlisted.", isRequired: false).AddOption("enlisted6", ApplicationCommandOptionType.User, "The @ of the enlisted.", isRequired: false).AddOption("enlisted7", ApplicationCommandOptionType.User, "The @ of the enlisted.", isRequired: false).AddOption("enlisted8", ApplicationCommandOptionType.User, "The @ of the enlisted.", isRequired: false).AddOption("enlisted9", ApplicationCommandOptionType.User, "The @ of the enlisted.", isRequired: false).AddOption("enlisted10", ApplicationCommandOptionType.User, "The @ of the enlisted.", isRequired: false)
            .WithDefaultMemberPermissions(GuildPermission.ManageRoles));
        
        // IDSYSTEM.
        
        commands.Add(new SlashCommandBuilder()
            .WithName("showid")
            .WithDescription("Shows your Idol ID."));

        commands.Add(new SlashCommandBuilder()
            .WithName("editid")
            .WithDescription("Edits your Idol ID and displays it.")
            .AddOption("claim", ApplicationCommandOptionType.String, "The name of the claim / character.", isRequired: false)
            .AddOption("avatar_url", ApplicationCommandOptionType.String, "The profile of the member / character.", isRequired: false)
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("bloodtype")
                .WithDescription("The bloodtype of the member / character")
                .WithRequired(false)
                .AddChoice("O (Optimistic)", "O (Optimistic)")
                .AddChoice("A (Patient)", "A (Patient)")
                .AddChoice("B (Active)", "B (Active)")
                .AddChoice("AB (Rational)", "AB (Rational)")
                .WithType(ApplicationCommandOptionType.String)));
        
        // POINTSYSTEM.

        commands.Add(new SlashCommandBuilder()
            .WithName("showpoints")
            .WithDescription("Shows the points of a member.")
            .AddOption("member", ApplicationCommandOptionType.User, "The aforementioned member.", isRequired: false));

        commands.Add(new SlashCommandBuilder()
            .WithName("leaderboard")
            .WithDescription("Shows the point leaderboard."));

        commands.Add(new SlashCommandBuilder()
            .WithName("addpoints")
            .WithDescription("Adds points to a member.")
            .AddOption("member", ApplicationCommandOptionType.User, "The aforementioned member.", isRequired: false)
            .AddOption("amount", ApplicationCommandOptionType.Integer, "The amount of points to add.", isRequired: true)
            .WithDefaultMemberPermissions(GuildPermission.ManageRoles));
        
        commands.Add(new SlashCommandBuilder()
            .WithName("removepoints")
            .WithDescription("Removes points from a member.")
            .AddOption("member", ApplicationCommandOptionType.User, "The aforementioned member.", isRequired: false)
            .AddOption("amount", ApplicationCommandOptionType.Integer, "The amount of points to remove.", isRequired: true)
            .WithDefaultMemberPermissions(GuildPermission.ManageRoles));

        commands.Add(new SlashCommandBuilder()
            .WithName("restoreprogress")
            .WithDescription("Restores the progress of a previous member.")
            .AddOption("member", ApplicationCommandOptionType.User, "The member.", isRequired: true)
            .WithDefaultMemberPermissions(GuildPermission.Administrator));
        
        commands.Add(new SlashCommandBuilder()
            .WithName("massremove")
            .WithDescription("Deletes a specified number of messages from this channel.")
            .AddOption("amount", ApplicationCommandOptionType.Integer, "Number of messages to delete (1-100).", isRequired: true)
            .WithDefaultMemberPermissions(GuildPermission.ManageRoles));

        try {
            var builtCommands = commands.Select(c => (ApplicationCommandProperties)c.Build()).ToArray();
            await ((IGuild)guild).BulkOverwriteApplicationCommandsAsync(builtCommands);
            
        } catch (ApplicationCommandException exception) {
            var json = JsonConvert.SerializeObject(exception.Errors, Formatting.Indented);
            Console.WriteLine(json);
        }
    }
    private async Task SlashCommandHandler(SocketSlashCommand command) {
        switch(command.Data.Name) {
            case "rewardko":
                await _rewardSystem.HandleRewardKoCommand(command);
                break;
            case "rewardaccomp":
                await _rewardSystem.HandleRewardAccompCommand(command, _client);
                break;
            
            case "meeting":
                await _meetingSystem.HandleMeetingCommand(command, _client);
                break;
            case "meetingblist":
                await _meetingSystem.HandleMeetingBListCommand(command, _client);
                break;
            case "meetingreprimand":
                await _meetingSystem.HandleMeetingReprimandCommand(command, _client);
                break;
            case "meetingclose":
                await _meetingSystem.HandleMeetingCloseCommand(command, _client);
                break;
            
            case "preenlist":
                await _roleSystem.HandlePreEnlistCommand(command);
                break;
            case "enlist":
                await _roleSystem.HandleEnlistCommand(command, _client);
                break;
            case "forceenlist":
                await _roleSystem.HandleForceEnlistCommand(command);
                break;
            case "forceremove":
                await _roleSystem.HandleForceRemoveCommand(command);
                break;
            case "promote":
                await _roleSystem.HandlePromoteCommand(command);
                break;
            
            case "showid":
                await _idSystem.ShowId(command, _client);
                break;
            case "editid":
                await _idSystem.EditId(command, _client);
                break;
            
            case "showpoints":
                await _pointSystem.ShowPoints(command);
                break;
            case "addpoints":
                await _pointSystem.EditPoints(command, true);
                break;
            case "removepoints":
                await _pointSystem.EditPoints(command, false);
                break;
            case "leaderboard":
                await _pointSystem.Leaderboard(command);
                break;
            case "restoreprogress":
                await _pointSystem.RestoreProgress(command, _client);
                break;
            case "massremove":
                await _generalSystem.HandleMassRemoveCommand(command);
                break;
            
            default:
                await command.RespondAsync("Unrecognized command.", ephemeral: true);
                break;
        }
    }
    
    
    
    
    
    

    public async Task ReactionAddedHandler(SocketGuild guild, Cacheable<IUserMessage, ulong> cache, Cacheable<IMessageChannel, ulong> channel, SocketReaction reaction) {
        
        var user = guild.GetUser(reaction.UserId);
        if (user is null) {
            return;
        }
        
        if (reaction.Emote.Name == "⭐") {

            var message = await cache.GetOrDownloadAsync();
            if (message.Channel.Id == 1473214696109909883) return;
            
            var starEmote = new Emoji("⭐");

            var freshMessage = await message.Channel.GetMessageAsync(message.Id) as IUserMessage;
            int starCount = freshMessage?.Reactions.TryGetValue(starEmote, out var meta) == true
                ? meta.ReactionCount
                : 1;

            if (starCount < 3) return;
            
            var author = message.Author as SocketGuildUser;
            
            Embed embed = (new EmbedBuilder()
                .WithAuthor(author.Nickname + " || ", author.GetGuildAvatarUrl() ?? author.GetAvatarUrl())
                .WithTitle($"⭐ {starCount} star{(starCount != 1 ? "s" : "")}! ﹒ <#" + message.Channel.Id + ">")
                .WithDescription(message.Content)
                .WithColor(0xBFA55F)).Build();

            var starboard = guild.GetChannel(1473214696109903883) as ITextChannel;
            if (starboard == null) return;

            string? existingId = _db.GetStarboardMessageId(message.Id);

            if (existingId is null) {

                var sent = await starboard.SendMessageAsync(embed: embed);
                _db.SaveStarboardMessageId(message.Id, sent.Id);
            } else {

                if (ulong.TryParse(existingId, out var existingUlong) &&
                    await starboard.GetMessageAsync(existingUlong) is IUserMessage existing) {
                    await existing.ModifyAsync(m => m.Embed = embed);
                }
            }
        }
        
        if (reaction.MessageId is 1494398646035156992) { // roe
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
            
        } else if (reaction.MessageId is 1493088668557115403) { // roles, personal
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
            
        } else if (reaction.MessageId is 1493088669924462592) { // roles, pings
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
        }
    }
    
    public async Task ReactionRemovedHandler(SocketGuild guild, Cacheable<IUserMessage, ulong> cache, Cacheable<IMessageChannel, ulong> channel, SocketReaction reaction) {
        
        var user = guild.GetUser(reaction.UserId);
        if (user is null) {
            return;
        }

        if (reaction.Emote.Name == "⭐") {
    
            var message = await cache.GetOrDownloadAsync();
            if (message.Channel.Id == 1473214696109909883) return;
    
            var starEmote = new Emoji("⭐");
    
            var freshMessage = await message.Channel.GetMessageAsync(message.Id) as IUserMessage;
            int starCount = freshMessage?.Reactions.TryGetValue(starEmote, out var meta) == true
                ? meta.ReactionCount
                : 0;

            var starboard = guild.GetChannel(1473214696109909883) as ITextChannel;
            if (starboard == null) return;

            string? existingId = _db.GetStarboardMessageId(message.Id);
            if (existingId is null) return;

            if (!ulong.TryParse(existingId, out var existingUlong)) return;

            if (starCount == 0) {
        
                if (await starboard.GetMessageAsync(existingUlong) is IUserMessage existing)
                {
                    await existing.DeleteAsync();
                }

                _db.DeleteStarboardEntry(message.Id);
            } else {
        
                var author = message.Author as SocketGuildUser;
                
                Embed embed = (new EmbedBuilder()
                    .WithAuthor(author.Nickname + " || ", author.GetGuildAvatarUrl() ?? author.GetAvatarUrl())
                    .WithTitle($"⭐ {starCount} star{(starCount != 1 ? "s" : "")}! ﹒ <#" + message.Channel.Id + ">")
                    .WithDescription(message.Content)
                    .WithColor(0xBFA55F)).Build();

                if (await starboard.GetMessageAsync(existingUlong) is IUserMessage existing)
                {
                    await existing.ModifyAsync(m => m.Embed = embed);
                }
            }
        }

        if (reaction.MessageId is 1494398646035156992) { // roe
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
            
        } else if (reaction.MessageId is 1493088668557115403) { // roles, personal
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
        } else if (reaction.MessageId is 1493088669924462592) { // roles, pings
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
        }
    }
    
    public async Task VoiceStateUpdatedAsync(SocketUser user, SocketVoiceState before, SocketVoiceState after, SocketGuild guild) {
        
        if (after.VoiceChannel?.Id == 1473221413749129367) {
            var category = guild.GetCategoryChannel(1473221350826184734);

            int voiceChannelCount = category.Channels.OfType<SocketVoiceChannel>().Count() - 1;
            var nameCount = voiceChannelCount switch {
                0 => "i", 1 => "ii", 2 => "iii", 3 => "iv", 4 => "v",
                5 => "vi", 6 => "vii", 7 => "iix", 8 => "ix", 9 => "x",
                _ => voiceChannelCount.ToString()
            };

            var name = "∥﹒mic﹒ready﹒" + nameCount;

            var vc = await guild.CreateVoiceChannelAsync(name, props => {
                props.CategoryId = category.Id;
                props.PermissionOverwrites = category.PermissionOverwrites.ToList();
            });

            if (user is SocketGuildUser guildUser) {
                await guildUser.ModifyAsync(x => x.Channel = vc);
            }

            await vc.SetStatusAsync("Change the status to the topic of the VC!");
        }
        
        if (before.VoiceChannel != null && before.VoiceChannel.Id != 1473221413749129367) {
            await Task.Delay(500);
            var vc = before.VoiceChannel;
            if (vc.ConnectedUsers.Count == 0) {
                await vc.DeleteAsync();
            }
        }
    }
    
    public async Task KickUnEnlisted(SocketGuild guild) {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(6)); // FromDays(3)
    
        while (await timer.WaitForNextTickAsync()) {
            IAsyncEnumerable<IReadOnlyCollection<IGuildUser>> collection = guild.GetUsersAsync();
        
            await foreach (var members in collection) {
                foreach (var user in members) {
                
                    var guildUser = user as SocketGuildUser;
                    
                    bool isUnenlistedProspect = user.RoleIds.Contains((ulong)1473369383471677461) && !user.RoleIds.Contains((ulong)1475720710910382310);
                    bool isCivilian = user.RoleIds.Contains((ulong)1473369036766052445);
                    bool isInactive = user.JoinedAt < DateTimeOffset.Now.AddMinutes(-5);
                
                    ulong[] unverifiedRoles = [1473369716792885402, 1473370059950002318, 1473370439526125599, 1473371454790832304];

                    bool isUnverified = guildUser.Roles
                        .Where(r => !r.IsEveryone)
                        .All(r => unverifiedRoles.Contains(r.Id));
                    
                    if ((isUnenlistedProspect || isCivilian || isUnverified) && isInactive) {
                        try {
                            
                            await UserExtensions.SendMessageAsync(user, "Hello! This is the *Automatic Messaging System* at the Sangō Idol-Defense Force.\n\n... \n\nhttps://64.media.tumblr.com/51b15f41ee5f58c722ebac09ae3d165e/6a794ae0ea17c706-cc/s2048x3072/39b7a663a13e95d68c46239534bea85f9e008f26.pnj");
                            await Task.Delay(1500); 
                            await user.KickAsync("Inactive for 2+ months");
                            await Task.Delay(1000); 
                        }
                        catch (Exception ex) {
                            Console.WriteLine($"Failed to kick {user.Username}: {ex.Message}");
                        }
                    }
                }
            }
        }
    }
    
    public async Task ButtonHandler(SocketMessageComponent component) {
    
        var id = component.Data.CustomId;
    
        if (id.StartsWith("leaderboard_back_") || id.StartsWith("leaderboard_next_")) {
        
            var parts = id.Split('_');
            int currentPage = int.Parse(parts[2]);
            int newPage = id.StartsWith("leaderboard_back_") ? currentPage - 1 : currentPage + 1;
        
            var entries = _db.GetLeaderboard();
            var embed = _pointSystem.BuildLeaderboardEmbed(entries, newPage);
            var components = _pointSystem.BuildLeaderboardComponents(newPage, entries.Count);
        
            await component.UpdateAsync(x => {
                x.Embed = embed;
                x.Components = components;
            });
        }
    }
}