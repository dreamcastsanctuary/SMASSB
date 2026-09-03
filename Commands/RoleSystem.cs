using Discord;
using Discord.WebSocket;
using SMASSB.Exceptions;
using SMASSB.Models;

namespace SMASSB.Commands;

public class RoleSystem {
    
    private readonly DiscordSocketClient _client;
    private readonly DatabaseService _db;
    private readonly LogHandler _logHandler;
    private readonly ulong? _guildId;
    
    public RoleSystem(DiscordSocketClient client, LogHandler logHandler, DatabaseService db, GuildConfiguration guildConfig) {
        _client = client;
        _logHandler = logHandler;
        _db = db;
        _guildId = guildConfig.GuildId;
    }

    public async Task HandlePreEnlistCommand(SocketSlashCommand command) {

        await command.DeferAsync();
        SocketGuildUser? civilian = null;
        var claim = "";

        foreach (var option in command.Data.Options) {
            switch (option.Name) {
                
                case "civilian":
                    civilian = ((SocketGuildUser)option.Value);
                    break;
                case "claim_name":
                    claim = option.Value.ToString();
                    break;
            }
        }

        if (civilian == null) {
            await command.FollowupAsync("Unrecognized account.", ephemeral: true);
            return;
        }
        
        await civilian.AddRoleAsync(1473369036766052445);
        await civilian.AddRoleAsync(1475886792174604484);
        await civilian.RemoveRoleAsync(1473369383471677461);

        await civilian.ModifyAsync(x => x.Nickname = "Kō. " + claim);

        if (claim != null) {
            await _db.PreEnlist(command, civilian, claim, civilian.GetGuildAvatarUrl() ?? civilian.GetAvatarUrl(), civilian.Id.ToString(), civilian.JoinedAt ?? civilian.CreatedAt, "Kōhosei", 0, 0, "N/A", "", civilian.Username, "ENLISTEDMAIN", "BLACK", "NONE", "BASIC");

            try {
                await civilian.SendMessageAsync($"Welcome to SANGŌ, **Kō. {claim}**! We're very happy to have you.\n" + "Your first event *must* be of type **CIVT / Civilian Training**. Please be on the lookout for it.");
            } catch (Discord.Net.HttpException ex) {
                await command.FollowupAsync(new MessageSendException(ex.Message, ex).Message);
            }
        }
    }
    
    public async Task HandleEnlistCommand(SocketSlashCommand command) {
        
        await command.DeferAsync();
        SocketGuildUser? civilian = null;
        
        foreach (var option in command.Data.Options) {
            switch (option.Name) {
                
                case "kōhosei":
                    civilian = ((SocketGuildUser)option.Value);
                    break;
                default:
                    await command.FollowupAsync("Unrecognized command.", ephemeral: true);
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
        await civilian.RemoveRoleAsync(1537202109336920096);

        var guild = _client.GetGuild((ulong)_guildId!);
        IRole niShi = guild.GetRole(1475886748268625962);
        await Promote(civilian, niShi, command);
    }

    public async Task HandleCheckPromosCommand(SocketSlashCommand command) {
        
        await command.DeferAsync();
        var promote = false;
        var guild = _client.GetGuild((ulong)_guildId!);
        
        foreach (var option in command.Data.Options) {
            switch (option.Name) {
                
                case "auto_promote":
                    promote = option.Value.ToString() == "True";
                    break;
                default:
                    await command.FollowupAsync("Unrecognized command.", ephemeral: true);
                    break;
            }
        }

        var ranks = new List<(ulong RoleId, int Threshold)> {
            (1475886748268625962, 250),
            (1475886729561899212, 500),
            (1475886715368509753, 750),
            (1475886697118957660, 1000),
            (1475886671919579310, 1250),
            (1475886657545961472, 1500),
        };

        var enlisteds = new List<SocketGuildUser>();
        var promotable = new List<SocketGuildUser>();
        
        foreach (var userId in _db.GetEnlisted()) {
            enlisteds.Add(guild.GetUser(ulong.Parse(userId)));
        }

        foreach (var enlisted in enlisteds) {
            
            foreach (var rank in ranks) {
                var role = guild.GetRole(rank.RoleId);
                if (role == null) continue;

                if (role.Name.Contains(await _db.GetRank(enlisted.Id)) && await _db.GetPoints(enlisted.Id) >= rank.Threshold) {
                    promotable.Add(enlisted);
                }
            }
        }

        if (promotable.Count == 0) {
            await command.FollowupAsync("No promotions found.");
            return;
        }

        var description = "<:sango_emblem_mono:1492222638980989138> ∥ GENERAL RANKUPs . .\n・ ・ ・ ・ ・ ・ ・ ・ ・ ・ ・ ・ ・ ・ ・ ・ ・\n";

        foreach (var promo in promotable) {
            description += "<@" + promo.Id + "> ∥ " + await _db.GetPoints(promo.Id) + "pts.\n";
        }

        EmbedBuilder builder = new EmbedBuilder()
            .WithTitle("❖﹒Viable Promotions . .")
            .WithThumbnailUrl("https://media.discordapp.net/attachments/1084260632142024784/1514408846259523606/Untitled384_20260410170520.png?ex=6a2e8e65&is=6a2d3ce5&hm=c05da8c7af19869b1745e4024aa09d0ba8a119d1c0ee397c6d02d6f9a381ff9a&=&format=webp&quality=lossless&width=1265&height=1265")
            .WithDescription(description)
            .WithColor(0xBFA55F);

        if (promote) {
            foreach (var enlisted in promotable) {

                for (var i = 0; i < ranks.Count; i++) {
                    var role = guild.GetRole(ranks[i].RoleId);
                    if (role == null) continue;

                    if (role.Name.Contains(await _db.GetRank(enlisted.Id))) {

                        if (i + 1 < ranks.Count) {
                            await Promote(enlisted, guild.GetRole(ranks[i + 1].RoleId));

                            for (var j = i; j >= Math.Max(0, i - 3); j--) {
                                var oldRole = guild.GetRole(ranks[j].RoleId);
                                if (oldRole != null) {
                                    await enlisted.RemoveRoleAsync(oldRole);
                                }
                            }

                            await enlisted.AddRoleAsync(guild.GetRole(ranks[i + 1].RoleId));
                        }
                        break;
                    }
                }
            }
            builder.WithTitle("Viable Promotions Completed!");
        }

        if (command.Channel is ITextChannel channel) await channel.SendMessageAsync(embed: builder.Build());
    }

    public async Task HandlePromoteCommand(SocketSlashCommand command) {

        await command.DeferAsync();
        
        var enlisteds = new List<SocketGuildUser>();
        IRole? addedRank = null; 
        IRole? addedRankCategory = null; 
        IRole? removedRank = null; 
        IRole? removedRankCategory = null;
        
        foreach (var option in command.Data.Options) {
            
            if (option.Name.StartsWith("enlisted")) {
                enlisteds.Add((SocketGuildUser)option.Value);
            } else switch (option.Name) {
                case "add_rank":
                    addedRank = (IRole)option.Value;
                    break;
                case "remove_rank":
                    addedRankCategory = (IRole)option.Value;
                    break;
                case "add_rank_category":
                    removedRank = (IRole)option.Value;
                    break;
                case "remove_rank_category":
                    removedRankCategory = (IRole)option.Value;
                    break;
                default:
                    await command.RespondAsync("Unrecognized command.", ephemeral: true);
                    return;
            }
        }

        if (addedRank != null) {
            foreach (var enlisted in enlisteds) {
                
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
        await command.FollowupAsync("Completed task.", ephemeral: true);
    }
    
    public async Task HandleForceEnlistCommand(SocketSlashCommand command) {
        await command.DeferAsync();
        
        SocketGuildUser? civilian = null;
        var claim = "";
        var rank = "";
        var isStaff = false;
        
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
                    await command.FollowupAsync("Unrecognized command.", ephemeral: true);
                    break;
            }
        }

        if (civilian == null) {
            await command.FollowupAsync("Unrecognized account.", ephemeral: true);
            return;
        }

        var idType = "ENLISTEDMAIN";
        if (isStaff) { idType = "STAFFMAIN"; }
        
        if (claim != null && rank != null) await _db.PreEnlist(command, civilian, claim, civilian.GetGuildAvatarUrl() ?? civilian.GetAvatarUrl(), civilian.Id.ToString(), civilian.JoinedAt ?? civilian.CreatedAt, rank,0,0,"N/A","", civilian.Username, idType, "BLACK", "NONE", "BASIC"); 
    }

    public async Task Promote(SocketGuildUser enlisted, IRole rank, SocketSlashCommand? command = null, string? newClaim = null, string? response = null) {
        
        var nickname = enlisted.Nickname;
        var rankName = rank.Name;

        var dotIndex = rankName.IndexOf('.');
        var fixedRankNick = rankName.Substring(1, dotIndex);
        var fixedRankFull = rankName[(dotIndex + 2)..];
        var spaceIndex = nickname.IndexOf(' ');
        string? claim;
        
        if (String.IsNullOrEmpty(newClaim)) {
            claim = spaceIndex >= 0 ? nickname[(spaceIndex + 1)..] : nickname;
        }
        else {
            claim = newClaim; 
            await _db.SetClaim(enlisted.Id, claim);
        }

        await enlisted.ModifyAsync(x => x.Nickname = fixedRankNick + " " + claim);
        await _db.SetRank(enlisted.Id, fixedRankFull);

        var message = string.IsNullOrEmpty(response) ? "Welcome to your new life as an enlisted, <@" + enlisted.Id + ">!" : response;
        
        if (command != null) { await command.FollowupAsync(message); }
    }

    public async Task HandleForceRemoveCommand(SocketSlashCommand command) {
        
        SocketGuildUser? civilian = null;
        
        foreach (var option in command.Data.Options) {
            switch (option.Name) {
                
                case "civilian":
                    civilian = ((SocketGuildUser)option.Value);
                    break;
                default:
                    await command.RespondAsync("Unrecognized command.", ephemeral: true);
                    break;
            }
        }

        if (civilian != null) await _db.Remove(civilian.Id);
        await command.RespondAsync("Completed task.");
    }
    
    public async Task HandleFinishKo(IGuildUser kohosei, ITextChannel channel) {
        
        await channel.AddPermissionOverwriteAsync(kohosei, new OverwritePermissions(viewChannel: PermValue.Allow));
        await kohosei.SendMessageAsync("Congratulations! You've successfully ranked up to **NiShi. Nitō Shi**. We hope to see much more from you in the future.\n\nYou've earned your final uniforms, which you can find in the new \"ENLISTED\" uniform channel.");
        await _db.SetRank(kohosei.Id, "Nitō Shi");
    }

    public async Task HandleDuoCommand(SocketSlashCommand command) {

        SocketGuildUser? member1 = null;
        SocketGuildUser? member2 = null;

        foreach (var option in command.Data.Options) {
            switch (option.Name) {

                case "member1":
                    member1 = (SocketGuildUser)option.Value;
                    break;
                case "member2":
                    member2 = (SocketGuildUser)option.Value;
                    break;
                default:
                    await command.RespondAsync("Unrecognized command.", ephemeral: true);
                    break;
            }
        }

        if (member1 == null || member2 == null) {
            await command.RespondAsync("Couldn't find one of the members!", ephemeral: true);
            return;
        }
        
        await member1.AddRoleAsync(1473369962788950248);
        await member2.AddRoleAsync(1473369962788950248);

        await command.RespondAsync("Completed pairing request.");
    }
}
