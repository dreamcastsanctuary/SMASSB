using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace SMASSB.Commands;

public class MeetingSystem {

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
        
        var thread = await channel.CreateThreadAsync(name, autoArchiveDuration: ThreadArchiveDuration.OneHour);
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
        
        var thread = await channel.CreateThreadAsync(name, autoArchiveDuration: ThreadArchiveDuration.OneHour);
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
        await person.RemoveRoleAsync(1473368797023961139);
        
        var name = "meeting-repri-" + meeting_name;
        
        var thread = await channel.CreateThreadAsync(name, autoArchiveDuration: ThreadArchiveDuration.OneHour);
        await thread.SendMessageAsync("Welcome to Meeting Room " + meeting_name +".\nPlease wait here and be patient as <@" + command.User.Id + "> prepares to speak to you, <@" + person.Id + ">.");
    }
    
    [DefaultMemberPermissions(GuildPermission.ManageRoles)]
    public async Task HandleMeetingCloseCommand(SocketSlashCommand command, DiscordSocketClient client) {

        var guild = client.GetGuild(command.GuildId.Value);
        SocketChannel channel = client.GetChannel(command.ChannelId.Value);
        
        if (((SocketTextChannel)channel).Name.Contains("meeting-") & channel is IThreadChannel) {
            await command.RespondAsync("Closing meeting room.", ephemeral: true);
            var thread = guild.GetThreadChannel(channel.Id);
            IReadOnlyCollection<SocketThreadUser> users = await thread.GetUsersAsync();

            if (((SocketTextChannel)channel).Name.Contains("meeting-repri-")) {
                foreach (SocketThreadUser user in users) {
                    
                    SocketGuildUser guildUser = (SocketGuildUser) user;
                    if (guildUser.Roles.Any(r => r.Id == 1492678150025379860)) {
                        await guildUser.RemoveRoleAsync(1492678150025379860);
                        await guildUser.AddRoleAsync(1473368797023961139);
                    }
                }
            }
            foreach (SocketThreadUser user in users) {
                SocketGuildUser guildUser = (SocketGuildUser) user;
                if (guildUser.Roles.Any(r => r.Id == 1492674198345224293)) {
                    await user.RemoveRoleAsync(1492674198345224293);
                }
                await thread.RemoveUserAsync(user);
            }
            await thread.LeaveAsync();
            
        } else {
            await command.RespondAsync("This channel wasn't made by the SSB!", ephemeral: true);
        }
    }
}