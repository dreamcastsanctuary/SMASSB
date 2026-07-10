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
        
        await _db.PreEnlist(command, civilian, claim, civilian.GetGuildAvatarUrl() ?? civilian.GetAvatarUrl(), civilian.Id.ToString(), civilian.JoinedAt ?? civilian.CreatedAt, "Kōhosei",0,"N/A","", civilian.Username, "ENLISTEDMAIN");
        await UserExtensions.SendMessageAsync(civilian, "Welcome to SANGŌ, **Kō. " + claim + "**! We're very happy to have you.\nYour first event *must* be of type **CIVT / Civilian Training**. Please be on the lookout for it.");
        await command.RespondAsync("Processed Prospect into Database.");
    }
    
    [DefaultMemberPermissions(GuildPermission.ManageRoles)]
    public async Task HandleEnlistCommand(SocketSlashCommand command, DiscordSocketClient client) {
        
        SocketGuildUser civilian = null;
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
        await Promote(civilian, niShi, command);
    }

    public async Task HandleCheckPromosCommand(SocketSlashCommand command, DiscordSocketClient client) {

        bool promote = false;
        
        foreach (var option in command.Data.Options)
        {
            switch (option.Name) {
                
                case "auto_promote":
                    promote = option.Value.ToString() == "True";
                    break;
                default:
                    await command.RespondAsync("Unrecognized command.", ephemeral: true);
                    break;
            }
        }

        Dictionary<ulong, int> ranks  = new Dictionary<ulong, int>();
        
        ranks.Add(1475886748268625962, 250);
        ranks.Add(1475886729561899212, 500);
        ranks.Add(1475886715368509753, 750);
        ranks.Add(1475886697118957660, 1000);
        ranks.Add(1475886671919579310, 1250);
        ranks.Add(1475886657545961472, 1500);

        List<SocketGuildUser> enlisteds = new List<SocketGuildUser>();
        SocketGuild guild = client.GetGuild((ulong)command.GuildId);
        List<SocketGuildUser> promotable = new List<SocketGuildUser>();
        List<SocketGuildUser> kouPromo = new List<SocketGuildUser>();
        
        foreach (var userId in _db.GetEnlisted()) {
            
            enlisteds.Add(guild.GetUser(ulong.Parse(userId)));
        }

        foreach (var enlisted in enlisteds) {
            foreach (var rank in ranks) {
                if (guild.GetRole(rank.Key).Name.Contains(await _db.GetRank(enlisted.Id)) & await _db.GetPoints(enlisted.Id) >= rank.Value) {
                    promotable.Add(enlisted);
                }
            }

            var potentialKou = await _db.GetRank(enlisted.Id);
            
            if (potentialKou.Contains("hosei", StringComparison.OrdinalIgnoreCase) && await _db.GetPoints(enlisted.Id) >= 14) {
                kouPromo.Add(enlisted);
            }
        }

        if (promotable.Count == 0 && kouPromo.Count == 0) {
            await command.RespondAsync("No promotions found.");
            return;
        }

        var description = "";

        if (kouPromo.Count > 0) {
            description += "<:sango_emblem_mono:1492222638980989138> ∥ KŌHOSEI GRADUATES . .\n・ ・ ・ ・ ・ ・ ・ ・ ・ ・ ・ ・ ・ ・ ・ ・ ・\n";
            
            foreach (var kou in kouPromo)
            {
                description += "<@" + kou.Id + "> ∥ " + await _db.GetPoints(kou.Id) + "pts.\n";
            }
            description += "\n";
        }
        
        if (promotable.Count > 0) {
            description += "<:sango_emblem_mono:1492222638980989138> ∥ GENERAL RANKUPs . .\n・ ・ ・ ・ ・ ・ ・ ・ ・ ・ ・ ・ ・ ・ ・ ・ ・\n";
            
            foreach (var promo in promotable)
            {
                description += "<@" + promo.Id + "> ∥ " + await _db.GetPoints(promo.Id) + "pts.\n";
            }
        }

        EmbedBuilder builder = new EmbedBuilder()
            .WithTitle("❖﹒Viable Promotions . .")
            .WithThumbnailUrl("https://media.discordapp.net/attachments/1084260632142024784/1514408846259523606/Untitled384_20260410170520.png?ex=6a2e8e65&is=6a2d3ce5&hm=c05da8c7af19869b1745e4024aa09d0ba8a119d1c0ee397c6d02d6f9a381ff9a&=&format=webp&quality=lossless&width=1265&height=1265")
            .WithDescription(description)
            .WithColor(0xBFA55F);
        
        if (promote) {
            foreach (var enlisted in promotable) {
                var rankList = ranks.ToList();
                
                for (int i = 0; i < rankList.Count; i++) {
                    if (guild.GetRole(rankList[i].Key).Name.Contains(await _db.GetRank(enlisted.Id))) {
                        
                        if (i + 1 < rankList.Count) {
                            await Promote(enlisted, guild.GetRole(rankList[i + 1].Key));
                            
                            await enlisted.RemoveRoleAsync(guild.GetRole(rankList[i].Key));
                            if (i != 0) {
                                await enlisted.RemoveRoleAsync(guild.GetRole(rankList[i - 1].Key));
                                if ((i - 1) != 0) await enlisted.RemoveRoleAsync(guild.GetRole(rankList[i - 2].Key));
                                if ((i - 2) != 0) await enlisted.RemoveRoleAsync(guild.GetRole(rankList[i - 3].Key));
                            }
                            await enlisted.AddRoleAsync(guild.GetRole(rankList[i + 1].Key));
                        }
                        break;
                    }
                }
            }
            builder.WithTitle("Viable Promotions Completed!");
        }
        
        var channel = command.Channel as ITextChannel;
        await channel.SendMessageAsync(embed: builder.Build());
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
        await command.RespondAsync("Completed task.", ephemeral: true);
    }
    
    [DefaultMemberPermissions(GuildPermission.ManageRoles)]
    public async Task HandleForceEnlistCommand(SocketSlashCommand command) {
        
        SocketGuildUser civilian = null;
        var claim = "";
        var rank = "";
        bool isStaff = false;
        
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
                case "is_staff":
                    isStaff = option.Value.ToString() == "True";
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

        var idType = "ENLISTEDMAIN";
        if (isStaff) { idType = "STAFFMAIN"; }
        
        await _db.PreEnlist(command, civilian, claim, civilian.GetGuildAvatarUrl() ?? civilian.GetAvatarUrl(), civilian.Id.ToString(), civilian.JoinedAt ?? civilian.CreatedAt, rank,0,"N/A","", civilian.Username, idType); 
    }

    public async Task Promote(SocketGuildUser enlisted, IRole rank, SocketSlashCommand command = null, string newClaim = null, string response = null) {
        
        string nickname = enlisted.Nickname;
        string rankName = rank.Name;

        int dotIndex = rankName.IndexOf('.');
        string fixedRankNick = rankName.Substring(1, dotIndex);
        string fixedRankFull = rankName.Substring(dotIndex + 2);
        int spaceIndex = nickname.IndexOf(' ');
        string claim = "";
        
        if (String.IsNullOrEmpty(newClaim)) {
            claim = spaceIndex >= 0 ? nickname.Substring(spaceIndex + 1) : nickname;
        }
        else {
            claim = newClaim; 
            await _db.SetClaim(enlisted.Id, claim);
        }

        await enlisted.ModifyAsync(x => x.Nickname = fixedRankNick + " " + claim);
        await _db.SetRank(enlisted.Id, fixedRankFull);

        var message = String.IsNullOrEmpty(response) ? "Welcome to your new life as an enlisted, <@" + enlisted.Id + ">!" : response;
        
        if (command != null) { await command.RespondAsync(message); }
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

    [DefaultMemberPermissions(GuildPermission.ManageRoles)]
    public async Task HandleFinishCeremony(SocketSlashCommand command, DiscordSocketClient client) {
        
        List<SocketGuildUser> enlisteds = new List<SocketGuildUser>();

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
                default:
                    await command.RespondAsync("Unrecognized command.", ephemeral: true);
                    break;
            }
        }
        
        SocketGuild guild = client.GetGuild((ulong)command.GuildId);
        var channel = guild.GetTextChannel(1473516609397063680);
        
        await command.RespondAsync("Completed task.");

        foreach (SocketGuildUser enlisted in enlisteds) {
            await channel.AddPermissionOverwriteAsync(enlisted, new OverwritePermissions(viewChannel: PermValue.Allow));
            await enlisted.SendMessageAsync("Congratulations on the ceremony. We hope to see much more from you in the future.\nYou've earned your final uniforms, which you can find in the new \"ENLISTED\" uniform channel.\n\nNote that you've already got the Parade Dress uniform, so skip that unless you're making a new claim.");
            await _db.RemovePoints(enlisted.Id, 14);
        }
        
    }
    
    public async Task HandleFinishKo(IGuildUser kohosei, ITextChannel channel) {
        
        await channel.AddPermissionOverwriteAsync(kohosei, new OverwritePermissions(viewChannel: PermValue.Allow));
        await kohosei.SendMessageAsync("Congratulations! You've successfully ranked up to **NiShi. Nitō Shi**. We hope to see much more from you in the future.\n\nYou've earned your final uniforms, which you can find in the new \"ENLISTED\" uniform channel.");
        await _db.SetRank(kohosei.Id, "Nitō Shi");
    }
}
