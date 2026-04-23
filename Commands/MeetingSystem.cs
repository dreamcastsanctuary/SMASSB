using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace SMASSB.Commands;

public class MeetingSystem {
    
    private DatabaseService _db;
    
    public MeetingSystem (DatabaseService db) {
        _db = db;
    }

    [DefaultMemberPermissions(GuildPermission.ManageRoles)]
    public async Task HandleMeetingCommand(SocketSlashCommand command, DiscordSocketClient client) {
        
        await command.RespondAsync("Creating meeting room.", ephemeral: true);
        var guild = client.GetGuild(command.GuildId.Value);
        var channel = guild.GetChannel(1482455836776333322) as SocketTextChannel;
        SocketGuildUser person = null;
        var meeting_name = "";
        
        foreach (var option in command.Data.Options) {
            switch (option.Name) {
                
                case "person":
                    person = ((SocketGuildUser)option.Value);
                    break;
                case "meeting_name":
                    meeting_name = option.Value.ToString();
                    break;
                default:
                    await command.RespondAsync("Unrecognized command.", ephemeral: true);
                    break;
            }
        }
        
        if (person == null) {
            await command.RespondAsync("Unrecognized account.", ephemeral: true);
            return;
        }

        await person.AddRoleAsync(1492674198345224293);
        
        var name = "meeting-" + meeting_name;
        
        var thread = await channel.CreateThreadAsync(name, type: ThreadType.PrivateThread, autoArchiveDuration: ThreadArchiveDuration.OneHour);
        await Task.Delay(500);
        await thread.SendMessageAsync("Welcome to Meeting Room " + meeting_name +".\nPlease wait here and be patient as our <@&1473508563887329447> prepare to assist you, <@" + person.Id + ">.");
    }
    
    [DefaultMemberPermissions(GuildPermission.Administrator)]
    public async Task HandleMeetingBListCommand(SocketSlashCommand command, DiscordSocketClient client) {
        
        await command.RespondAsync("Creating blacklist meeting room.", ephemeral: true);
        var guild = client.GetGuild(command.GuildId.Value);
        var channel = guild.GetChannel(1482455836776333322) as SocketTextChannel;
        SocketGuildUser person = null;
        var meeting_name = "";
        
        foreach (var option in command.Data.Options) {
            switch (option.Name) {
                
                case "person":
                    person = ((SocketGuildUser)option.Value);
                    break;
                case "meeting_name":
                    meeting_name = option.Value.ToString();
                    break;
                default:
                    await command.RespondAsync("Unrecognized command.", ephemeral: true);
                    break;
            }
        }
        
        if (person == null) {
            await command.RespondAsync("Unrecognized account.", ephemeral: true);
            return;
        }
        
        await person.AddRoleAsync(1492674198345224293);
        
        var name = "meeting-blist-" + meeting_name;
        
        var thread = await channel.CreateThreadAsync(name, type: ThreadType.PrivateThread, autoArchiveDuration: ThreadArchiveDuration.OneHour);
        await Task.Delay(500);
        await thread.SendMessageAsync("Welcome to Meeting Room " + meeting_name + ".\nPlease wait here and be patient as <@" + command.User.Id + "> and <@1436617424379318282> prepare to assist you, <@" + person.Id + ">.");
    }
    
    [DefaultMemberPermissions(GuildPermission.ManageRoles)]
    public async Task HandleMeetingReprimandCommand(SocketSlashCommand command, DiscordSocketClient client) {
        
        await command.RespondAsync("Creating reprimand meeting room.", ephemeral: true);
        var guild = client.GetGuild(command.GuildId.Value);
        var channel = guild.GetChannel(1482455836776333322) as SocketTextChannel;
        SocketGuildUser person = null;
        var meeting_name = "";
        
        foreach (var option in command.Data.Options) {
            switch (option.Name) {
                
                case "person":
                    person = ((SocketGuildUser)option.Value);
                    break;
                case "meeting_name":
                    meeting_name = option.Value.ToString();
                    break;
                default:
                    await command.RespondAsync("Unrecognized command.", ephemeral: true);
                    break;
            }
        }
        
        if (person == null) {
            await command.RespondAsync("Unrecognized account.", ephemeral: true);
            return;
        }
        
        await person.AddRoleAsync(1492674198345224293);
        await person.AddRoleAsync(1492678150025379860);
        
        var freshPerson = guild.GetUser(person.Id);

        await _db.SetIsCivilian(freshPerson.Id, freshPerson.Roles.Contains(guild.GetRole(1473369383471677461)));
        await _db.SetIsEnlisted(freshPerson.Id, freshPerson.Roles.Contains(guild.GetRole(1473368797023961139)));
        await _db.SetIsFan(freshPerson.Id, person.Roles.Contains(guild.GetRole(1475720710910382310)));
        await _db.SetIsPartner(freshPerson.Id, freshPerson.Roles.Contains(guild.GetRole(1473514553240322148)));
        await _db.SetIsProspect(freshPerson.Id, freshPerson.Roles.Contains(guild.GetRole(1473369036766052445)));

        Console.WriteLine(await _db.GetIsCivilian(freshPerson.Id));
        
        if (await _db.GetIsCivilian(freshPerson.Id)) {
            await freshPerson.RemoveRoleAsync(1473369383471677461);
        }
        
        if (await _db.GetIsEnlisted(freshPerson.Id)) {
            await freshPerson.RemoveRoleAsync(1473368797023961139);
        }
        
        if (await _db.GetIsFan(freshPerson.Id)) {
            await freshPerson.RemoveRoleAsync(1475720710910382310);
        }
        
        if (await _db.GetIsPartner(freshPerson.Id)) {
            await freshPerson.RemoveRoleAsync(1473514553240322148);
        }
        
        if (await _db.GetIsProspect(freshPerson.Id)) {
            await freshPerson.RemoveRoleAsync(1473369036766052445);
        }
        
        var name = "meeting-repri-" + meeting_name;
        
        var thread = await channel.CreateThreadAsync(name, type: ThreadType.PrivateThread, autoArchiveDuration: ThreadArchiveDuration.OneHour);
        await Task.Delay(500);
        await thread.SendMessageAsync("Welcome to Meeting Room " + meeting_name +".\nPlease wait here and be patient as <@" + command.User.Id + "> prepares to speak to you, <@" + freshPerson.Id + ">.");
    }
    
    [DefaultMemberPermissions(GuildPermission.ManageRoles)]
    public async Task HandleMeetingCloseCommand(SocketSlashCommand command, DiscordSocketClient client) {

        var guild = client.GetGuild(command.GuildId.Value);
        SocketChannel channel = client.GetChannel(command.ChannelId.Value);
        
        if (((SocketTextChannel)channel).Name.Contains("meeting-") && channel is IThreadChannel) {
            await command.RespondAsync("Closing meeting room.", ephemeral: true);
            var thread = guild.GetThreadChannel(channel.Id);
            IReadOnlyCollection<SocketThreadUser> users = await thread.GetUsersAsync();
            
            var messages = (await thread.GetMessagesAsync(500).FlattenAsync())
                .OrderBy(m => m.Timestamp)
                .ToList();

            var logChannel = guild.GetTextChannel(1482805129613938860);
            var logThread = await logChannel.CreateThreadAsync(
                name: thread.Name + "-log",
                type: ThreadType.PrivateThread,
                autoArchiveDuration: ThreadArchiveDuration.OneWeek
            );

            var webhook = await logChannel.CreateWebhookAsync("MeetingLogger");
            var webhookClient = new Discord.Webhook.DiscordWebhookClient(webhook);

            using var httpClient = new HttpClient();
            try {
                foreach (var message in messages) {
                    var user = message.Author as IGuildUser;
                    var guildUser = guild.GetUser(message.Author.Id);
                    var displayName = guildUser?.Nickname ?? message.Author.Username;
                    var avatarUrl = message.Author.GetAvatarUrl() ?? message.Author.GetDefaultAvatarUrl();

                    if (!string.IsNullOrWhiteSpace(message.Content)) {
                        await webhookClient.SendMessageAsync(
                            text: message.Content,
                            username: displayName,
                            avatarUrl: avatarUrl,
                            threadId: logThread.Id
                        );
                        await Task.Delay(500);
                    }

                    foreach (var attachment in message.Attachments) {
                        try {
                            var bytes = await httpClient.GetByteArrayAsync(attachment.Url);
                            using var stream = new MemoryStream(bytes);
                            var fileAttachment = new FileAttachment(stream, attachment.Filename);
                            await webhookClient.SendFilesAsync(
                                [fileAttachment],
                                text: null,
                                isTTS: false,
                                embeds: null,
                                username: displayName,
                                avatarUrl: avatarUrl,
                                flags: MessageFlags.None,
                                threadId: logThread.Id
                            );
                        } catch (Exception ex) {
                            await logThread.SendMessageAsync(
                                $"Could not re-upload `{attachment.Filename}` from **{displayName}** — {ex.Message}"
                            );
                        }
                        await Task.Delay(500);
                    }
                }
            } finally {
                await webhook.DeleteAsync();
            }
            
            await logThread.ModifyAsync(t => {
                t.Archived = true;
                t.Locked = true;
            });
            
            if (((SocketTextChannel)channel).Name.Contains("meeting-repri-")) {
                foreach (SocketThreadUser user in users) {
                    SocketGuildUser guildUser = (SocketGuildUser)user;
                    if (guildUser.Roles.Any(r => r.Id == 1492678150025379860)) {
                        
                        await guildUser.RemoveRoleAsync(1492678150025379860);
                        
                        if (await _db.GetIsCivilian(guildUser.Id)) {
                            await guildUser.AddRoleAsync(1473369383471677461);
                        }
        
                        if (await _db.GetIsEnlisted(guildUser.Id)) {
                            await guildUser.AddRoleAsync(1473368797023961139);
                        }
        
                        if (await _db.GetIsFan(guildUser.Id)) {
                            await guildUser.AddRoleAsync(1475720710910382310);
                        }
        
                        if (await _db.GetIsPartner(guildUser.Id)) {
                            await guildUser.AddRoleAsync(1473514553240322148);
                        }
        
                        if (await _db.GetIsProspect(guildUser.Id)) {
                            await guildUser.AddRoleAsync(1473369036766052445);
                        }
                    }
                }
            }
            
            foreach (SocketThreadUser user in users) {
                SocketGuildUser guildUser = (SocketGuildUser)user;
                if (guildUser.Roles.Any(r => r.Id == 1492674198345224293)) {
                    await guildUser.RemoveRoleAsync(1492674198345224293);
                }
            }

            await thread.DeleteAsync();

        } else {
            await command.RespondAsync("This channel wasn't made by the Assistant!", ephemeral: true);
        }
    }
}