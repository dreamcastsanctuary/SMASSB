using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace SMASSB.Commands;

public class RoleSystem {
    
    private DatabaseService _db;
    
    public RoleSystem(DatabaseService db) {
        _db = db;
    }

    [DefaultMemberPermissions(GuildPermission.ManageRoles)]
    public async Task HandlePreEnlistCommand(SocketSlashCommand command) {
        
        SocketGuildUser civilian = null;
        var channel = command.Channel;
        var claim = "";
        
        foreach (var option in command.Data.Options)
        {
            switch (option.Name) {
                
                case "civilian":
                    civilian = ((SocketGuildUser)option.Value);
                    break;
                case "claim_name":
                    claim = option.Value.ToString();
                    break;
                default:
                    await command.RespondAsync("Unrecognized command.", ephemeral: true);
                    break;
            }
        }

        if (civilian == null) {
            await command.RespondAsync("Unrecognized account.", ephemeral: true);
            return;
        }

        await civilian.AddRoleAsync(1473369036766052445);
        await civilian.AddRoleAsync(1475886792174604484);
        await civilian.RemoveRoleAsync(1473369383471677461);
        
        await civilian.ModifyAsync(x => x.Nickname = "Kō. " + claim);

        await command.RespondAsync("Processing Student into Database . . .");
        await _db.PreEnlist(command, civilian, claim, civilian.GetGuildAvatarUrl() ?? civilian.GetAvatarUrl(), civilian.Id.ToString(), civilian.JoinedAt ?? civilian.CreatedAt, "Kōsohei",0,"N/A","Go Strike!", civilian.Username);
        await UserExtensions.SendMessageAsync(civilian, "Welcome to SMA, **Kō. " + claim + "**! We're very happy to have you.\nYour first event *must* be of type **SCS101**. Please be on the lookout for it.");
    }
    
    [DefaultMemberPermissions(GuildPermission.ManageRoles)]
    public async Task HandleEnlistCommand(SocketSlashCommand command, DiscordSocketClient client) {
        
        SocketGuildUser civilian = null;
        var channel = command.Channel;
        var guild = client.GetGuild((ulong)command.GuildId);
        
        foreach (var option in command.Data.Options)
        {
            switch (option.Name) {
                
                case "kōsohei":
                    civilian = ((SocketGuildUser)option.Value);
                    break;
                default:
                    await command.RespondAsync("Unrecognized command.", ephemeral: true);
                    break;
            }
        }

        if (civilian == null) {
            return;
        }

        await civilian.AddRoleAsync(1473368797023961139);
        await civilian.AddRoleAsync(1475886748268625962);
        await civilian.RemoveRoleAsync(1473369036766052445);
        await civilian.RemoveRoleAsync(1475886792174604484);

        IRole niShi = guild.GetRole(1475886748268625962);
        await Promote(civilian, niShi);
        
        var updated = guild.GetUser(civilian.Id);
        await command.RespondAsync("Sent welcome message to new enlisted.");
        await UserExtensions.SendMessageAsync(civilian, "Welcome to your new life as an enlisted, " + updated.Nickname + "! Let's make bright memories together!");
    }

    [DefaultMemberPermissions(GuildPermission.ManageRoles)]
    public async Task HandlePromoteCommand(SocketSlashCommand command) {

        List<SocketGuildUser> students = new List<SocketGuildUser>();
        IRole addedRank = null; IRole addedRankCategory = null; IRole removedRank = null; IRole removedRankCategory = null;
        
        foreach (var option in command.Data.Options)
        {
            switch (option.Name) {
                
                case "student1":
                    students.Add(((SocketGuildUser)option.Value));
                    break;
                case "student2":
                    students.Add(((SocketGuildUser)option.Value));
                    break;
                case "student3":
                    students.Add(((SocketGuildUser)option.Value));
                    break;
                case "student4":
                    students.Add(((SocketGuildUser)option.Value));
                    break;
                case "student5":
                    students.Add(((SocketGuildUser)option.Value));
                    break;
                case "student6":
                    students.Add(((SocketGuildUser)option.Value));
                    break;
                case "student7":
                    students.Add(((SocketGuildUser)option.Value));
                    break;
                case "student8":
                    students.Add(((SocketGuildUser)option.Value));
                    break;
                case "student9":
                    students.Add(((SocketGuildUser)option.Value));
                    break;
                case "student10":
                    students.Add(((SocketGuildUser)option.Value));
                    break;
                case "add_rank":
                    addedRank = (IRole)option.Value;
                    break;
                case "remove_rank":
                    removedRank = (IRole)option.Value;
                    break;
                case "add_rank_category":
                    addedRankCategory = (IRole)option.Value;
                    break;
                case "remove_rank_category":
                    addedRankCategory = (IRole)option.Value;
                    break;
                default:
                    await command.RespondAsync("Unrecognized command.", ephemeral: true);
                    break;
            }
        }

        if (addedRank != null) {
            foreach (SocketGuildUser student in students) {
                
                await student.AddRoleAsync(addedRank);
                if (addedRankCategory != null) {
                    await student.AddRoleAsync(addedRankCategory);
                } if (removedRank != null) {
                    await student.RemoveRoleAsync(removedRank);
                } if (removedRankCategory != null) {
                    await student.RemoveRoleAsync(removedRankCategory);
                }

                await Promote(student, addedRank);
            }
        }
    }
    
    [DefaultMemberPermissions(GuildPermission.ManageRoles)]
    public async Task HandleForceEnlistCommand(SocketSlashCommand command) {
        
        SocketGuildUser civilian = null;
        var claim = "";
        var rank = "";
        
        foreach (var option in command.Data.Options)
        {
            switch (option.Name) {
                
                case "civilian":
                    civilian = ((SocketGuildUser)option.Value);
                    break;
                case "claim_name":
                    claim = option.Value.ToString();
                    break;
                case "rank_name":
                    rank = option.Value.ToString();
                    break;
                default:
                    await command.RespondAsync("Unrecognized command.", ephemeral: true);
                    break;
            }
        }

        if (civilian == null) {
            await command.RespondAsync("Unrecognized account.", ephemeral: true);
            return;
        }

        await command.RespondAsync("Processing Student into Database . . .");
        await _db.PreEnlist(command, civilian, claim, civilian.GetGuildAvatarUrl() ?? civilian.GetAvatarUrl(), civilian.Id.ToString(), civilian.JoinedAt ?? civilian.CreatedAt, rank,0,"N/A","Go Strike!", civilian.Username); 
    }

    private async Task Promote(SocketGuildUser student, IRole rank) {
    
        string nickname = student.Nickname;
    
        int cutRank = rank.ToString().IndexOf('.');
        int cutNick = nickname.IndexOf(' ');
    
        string claim = nickname.Remove(0, cutNick);
        string fixedRank = rank.ToString().Remove(1, cutRank - 1);
        string fixedNick = rank.ToString().Substring(1, cutRank - 1);
    
        await student.ModifyAsync(x => x.Nickname = fixedNick + ". " + claim);
        await _db.SetRank(student.Id, fixedRank);
    }

    [DefaultMemberPermissions(GuildPermission.ManageRoles)]
    public async Task HandleForceRemoveCommand(SocketSlashCommand command) {
        
        SocketGuildUser civilian = null;
        
        foreach (var option in command.Data.Options)
        {
            switch (option.Name) {
                
                case "civilian":
                    civilian = ((SocketGuildUser)option.Value);
                    break;
                default:
                    await command.RespondAsync("Unrecognized command.", ephemeral: true);
                    break;
            }
        }
        await _db.Remove(civilian.Id);
        await command.RespondAsync("Completed task.");
    }
}