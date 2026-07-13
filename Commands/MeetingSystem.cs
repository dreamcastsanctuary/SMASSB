using System.Text;
using System.Text.Json;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace SMASSB.Commands;

public class MeetingSystem {

    private DatabaseService _db;
    private static readonly HttpClient _httpClient = new HttpClient();
    private const string SiteBaseUrl = "https://sangoidoldefenseforce.vercel.app";
    private const string MeetingApiSecret = "Ba11erySama!";
    private const long MaxEmbeddedAttachmentBytes = 3 * 1024 * 1024;

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
    public async Task HandleMeetingPRCommand(SocketSlashCommand command, DiscordSocketClient client) {
        
        await command.RespondAsync("Creating blacklist meeting room.", ephemeral: true);
        var guild = client.GetGuild(command.GuildId.Value);
        var channel = guild.GetChannel(1482455836776333322) as SocketTextChannel;
        SocketGuildUser person = null;
        var meeting_name = "";
        var type = "";
        
        foreach (var option in command.Data.Options) {
            switch (option.Name) {
                
                case "person":
                    person = ((SocketGuildUser)option.Value);
                    break;
                case "meeting_name":
                    meeting_name = option.Value.ToString();
                    break;
                case "type":
                    type = option.Value.ToString();
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

        switch (type) {
            case "Partnering":
                type = "partner";
                break;
            case "Blacklist":
                type = "blist";
                break;
            case "Other":
                type = "pr-gen";
                break;
            default:
                await command.RespondAsync("Unrecognized command.", ephemeral: true);
                return;
        }
        
        var name = "meeting-" + type + "-" + meeting_name;
        
        var thread = await channel.CreateThreadAsync(name, type: ThreadType.PrivateThread, autoArchiveDuration: ThreadArchiveDuration.OneHour);
        await Task.Delay(500);
        await thread.SendMessageAsync("Welcome to Meeting Room " + meeting_name + ".\nPlease wait here and be patient as <@274990117163368448> and <@&1473371232060702781> prepare to assist you, <@" + person.Id + ">.");
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
        await thread.SendMessageAsync("Welcome to Meeting Room " + meeting_name +".\nPlease wait here and be patient as <@274990117163368448> prepares to speak to you, <@" + freshPerson.Id + ">.");
    }

    /// <summary>
    /// Fires on every message the bot can see (wire this up in your bot startup with
    /// `client.MessageReceived += meetingSystem.HandleMeetingMessage;`). Live-logs
    /// anything posted in a "meeting-" thread to the website as it happens, so a bot
    /// restart mid-meeting doesn't lose what was said before it went down.
    /// </summary>
    public async Task HandleMeetingMessage(SocketMessage rawMessage) {
        if (rawMessage.Author.IsBot) return;
        if (rawMessage.Channel is not SocketThreadChannel thread) return;
        if (!thread.Name.Contains("meeting-")) return;

        await PostMessageToMeetingLog(thread.Name, rawMessage);
    }

    /// <summary>
    /// Sends a single Discord message (text + attachments) to /api/meeting.
    /// Safe to call more than once for the same message — the server keys storage by
    /// Discord message ID, so repeats just overwrite instead of duplicating.
    /// </summary>
    private async Task PostMessageToMeetingLog(string meetingName, IMessage message) {
        if (string.IsNullOrWhiteSpace(message.Content) && message.Attachments.Count == 0)
            return;

        var user = message.Author as IGuildUser;
        var attachments = new List<object>();

        foreach (var attachment in message.Attachments) {
            if (attachment.Size > MaxEmbeddedAttachmentBytes) {
                attachments.Add(new {
                    url = attachment.Url,
                    filename = attachment.Filename,
                    contentType = attachment.ContentType
                });
                continue;
            }

            try {
                var bytes = await _httpClient.GetByteArrayAsync(attachment.Url);
                attachments.Add(new {
                    filename = attachment.Filename,
                    contentType = attachment.ContentType,
                    dataBase64 = Convert.ToBase64String(bytes)
                });
            } catch (Exception ex) {
                attachments.Add(new {
                    url = (string)null,
                    filename = attachment.Filename,
                    error = ex.Message
                });
            }
        }

        var payload = new {
            name = meetingName,
            secret = MeetingApiSecret,
            action = "message",
            message = new {
                id = message.Id.ToString(),
                author = user?.Nickname ?? message.Author.Username,
                avatarUrl = message.Author.GetAvatarUrl() ?? message.Author.GetDefaultAvatarUrl(),
                timestamp = message.Timestamp.ToUnixTimeMilliseconds(),
                content = message.Content,
                attachments
            }
        };

        await PostToMeetingApi(payload, $"message {message.Id}");
    }

    private async Task CloseMeetingLog(string meetingName) {
        var payload = new { name = meetingName, secret = MeetingApiSecret, action = "close" };
        await PostToMeetingApi(payload, $"close {meetingName}");
    }

    private async Task PostToMeetingApi(object payload, string label) {
        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        try {
            var response = await _httpClient.PostAsync($"{SiteBaseUrl}/api/meeting", content);
            if (!response.IsSuccessStatusCode) {
                Console.WriteLine($"[MeetingLog] Failed to post {label}: {response.StatusCode}");
            }
        } catch (Exception ex) {
            Console.WriteLine($"[MeetingLog] Error posting {label}: {ex.Message}");
        }
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

            foreach (var message in messages) {
                await PostMessageToMeetingLog(thread.Name, message);
                await Task.Delay(250);
            }

            await CloseMeetingLog(thread.Name);

            try {
                await command.FollowupAsync(
                    $"Log saved: {SiteBaseUrl}/meeting/{thread.Name}",
                    ephemeral: true
                );
            } catch (Exception ex) {
                Console.WriteLine($"[MeetingLog] Could not send followup: {ex.Message}");
            }

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