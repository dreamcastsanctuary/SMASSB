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

        await command.RespondAsync("Processing Prospect into Database . . .");
        await _db.PreEnlist(command, civilian, claim, civilian.GetGuildAvatarUrl() ?? civilian.GetAvatarUrl(), civilian.Id.ToString(), civilian.JoinedAt ?? civilian.CreatedAt, "Kōhosei",0,"N/A","Go Strike!", civilian.Username);
        await UserExtensions.SendMessageAsync(civilian, "Welcome to SANGŌ, **Kō. " + claim + "**! We're very happy to have you.\nYour first event *must* be of type **CIVT101**. Please be on the lookout for it.");
    }
    
    [DefaultMemberPermissions(GuildPermission.ManageRoles)]
    public async Task HandleEnlistCommand(SocketSlashCommand command, DiscordSocketClient client) {
        
        SocketGuildUser civilian = null;
        var channel = command.Channel;
        var guild = client.GetGuild((ulong)command.GuildId);
        
        foreach (var option in command.Data.Options)
        {
            switch (option.Name) {
                
                case "kōhosei":
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
        await UserExtensions.SendMessageAsync(civilian, "Welcome to your new life as an enlisted, **" + updated.Nickname + "**!\nYour first order of business is to check out your new uniform channel, and make the other two!\nThey're necessary for most of our events, so get to it soon!");
    }

    [DefaultMemberPermissions(GuildPermission.ManageRoles)]
    public async Task HandlePromoteCommand(SocketSlashCommand command) {

        List<SocketGuildUser> enlisteds = new List<SocketGuildUser>();
        IRole addedRank = null; IRole addedRankCategory = null; IRole removedRank = null; IRole removedRankCategory = null;
        
        foreach (var option in command.Data.Options)
        {
            switch (option.Name) {
                
                case "enlisted1":
                    enlisteds.Add(((SocketGuildUser)option.Value));
                    break;
                case "enlisted2":
                    enlisteds.Add(((SocketGuildUser)option.Value));
                    break;
                case "enlisted3":
                    enlisteds.Add(((SocketGuildUser)option.Value));
                    break;
                case "enlisted4":
                    enlisteds.Add(((SocketGuildUser)option.Value));
                    break;
                case "enlisted5":
                    enlisteds.Add(((SocketGuildUser)option.Value));
                    break;
                case "enlisted6":
                    enlisteds.Add(((SocketGuildUser)option.Value));
                    break;
                case "enlisted7":
                    enlisteds.Add(((SocketGuildUser)option.Value));
                    break;
                case "enlisted8":
                    enlisteds.Add(((SocketGuildUser)option.Value));
                    break;
                case "enlisted9":
                    enlisteds.Add(((SocketGuildUser)option.Value));
                    break;
                case "enlisted10":
                    enlisteds.Add(((SocketGuildUser)option.Value));
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
            foreach (SocketGuildUser enlisted in enlisteds) {
                
                await enlisted.AddRoleAsync(addedRank);
                if (addedRankCategory != null) {
                    await enlisted.AddRoleAsync(addedRankCategory);
                } if (removedRank != null) {
                    await enlisted.RemoveRoleAsync(removedRank);
                } if (removedRankCategory != null) {
                    await enlisted.RemoveRoleAsync(removedRankCategory);
                }

                await Promote(enlisted, addedRank);
            }
        }
        command.RespondAsync("Completed task.", ephemeral: true);
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

        await command.RespondAsync("Processing Prospect into Database . . .");
        await _db.PreEnlist(command, civilian, claim, civilian.GetGuildAvatarUrl() ?? civilian.GetAvatarUrl(), civilian.Id.ToString(), civilian.JoinedAt ?? civilian.CreatedAt, rank,0,"N/A","Go Strike!", civilian.Username); 
    }

    private async Task Promote(SocketGuildUser enlisted, IRole rank) {
        string nickname = enlisted.Nickname;
        string rankName = rank.Name;

        int dotIndex = rankName.IndexOf('.');
        string fixedNick = rankName.Substring(1, dotIndex);
        string fixedRank = rankName.Substring(dotIndex + 2);
        int spaceIndex = nickname.IndexOf(' ');
        string claim = spaceIndex >= 0 ? nickname.Substring(spaceIndex + 1) : nickname;

        await enlisted.ModifyAsync(x => x.Nickname = fixedNick + " " + claim);
        await _db.SetRank(enlisted.Id, fixedRank);
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
