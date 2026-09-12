using Discord;
using Discord.WebSocket;
using SMASSB.Data;
using SMASSB.Exceptions;
using SMASSB.Models;
using SMASSB.ServiceHandlers;

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
                    civilian = _client.GetGuild((ulong)_guildId!).GetUser(((SocketUser)option.Value).Id);
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
            try {
                await civilian.SendMessageAsync($"Welcome to SANGŌ, **Kō. {claim}**! We're very happy to have you.\n" + "Your first event *must* be of type **CIVT / Civilian Training**. Please be on the lookout for it.");
                await _db.PreEnlist(command, civilian, claim, civilian.GetGuildAvatarUrl() ?? civilian.GetAvatarUrl(), civilian.Id.ToString(), civilian.JoinedAt ?? civilian.CreatedAt, "Kōhosei", 0, 0, "N/A", "", civilian.Username, "ENLISTEDMAIN", "BLACK", "NONE", "BASIC");
            } catch (Discord.Net.HttpException ex) {
                await command.FollowupAsync($"Hey, <@{civilian.Id}>! Please turn your Server DMs on so that I can message you important information regarding your enlistment!\nThank you!");
            }
        }
    }
    
    public async Task HandleEnlistCommand(SocketSlashCommand command) {
        
        await command.DeferAsync();
        SocketGuildUser? civilian = null;
        
        foreach (var option in command.Data.Options) {
            switch (option.Name) {
                
                case "kōhosei":
                    civilian = _client.GetGuild((ulong)_guildId!).GetUser(((SocketUser)option.Value).Id);
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

        var ranks = new Dictionary<RankType, ulong> {
            { RankType.Kō, 1475886792174604484 },
            { RankType.NiShi, 1475886748268625962 },
            { RankType.ItShi, 1475886729561899212 },
            { RankType.Shi, 1475886715368509753 },
            { RankType.SaSō, 1475886697118957660 },
            { RankType.NiSō, 1475886671919579310 },
            { RankType.ItSō, 1475886657545961472 },
            { RankType.Sō, 1475886640429011125 }
        };

        var enlisteds = new List<SocketGuildUser>();
        var promotable = new List<SocketGuildUser>();
        var promoteTo = new List<string>();
        
        foreach (var userId in _db.GetEnlisted()) {
            enlisteds.Add(guild.GetUser(ulong.Parse(userId)));
        }

        foreach (var enlisted in enlisteds) {
            
            var currentRankStr = await _db.GetRank(enlisted.Id);
            var currentPoints = await _db.GetPoints(enlisted.Id);
            RankType? highestQualified = null;
    
            var currentRank = currentRankStr.ToRankType();
            if (!currentRank.HasValue || (int)currentRank.Value >= 10000) continue;
            
            foreach (var rankType in ranks.Keys.OrderByDescending(r => (int)r)) {
                if (currentPoints >= (int)rankType && (int)rankType < 10000) {
                    highestQualified = rankType;
                    break;
                }
            }
            
            if (highestQualified.HasValue && highestQualified.Value > currentRank) {
                promotable.Add(enlisted);
                promoteTo.Add(highestQualified.Value.ToString());
            }
        }
        
        if (promotable.Count == 0) {
            await command.FollowupAsync("No promotions found.");
            return;
        }

        var description = "<:sango_emblem_mono:1492222638980989138> ∥ GENERAL RANKUPs . .\n・ ・ ・ ・ ・ ・ ・ ・ ・ ・ ・ ・ ・ ・ ・ ・ ・\n";

        for (int i = 0; i < promotable.Count; i++) {
            description += $"<@{promotable[i].Id}> -> {promoteTo[i]} ∥ {await _db.GetPoints(promotable[i].Id)} pts.\n";
        }

        EmbedBuilder builder = new EmbedBuilder()
            .WithTitle("❖﹒Viable Promotions . .")
            .WithThumbnailUrl("https://media.discordapp.net/attachments/1084260632142024784/1514408846259523606/Untitled384_20260410170520.png?ex=6a2e8e65&is=6a2d3ce5&hm=c05da8c7af19869b1745e4024aa09d0ba8a119d1c0ee397c6d02d6f9a381ff9a&=&format=webp&quality=lossless&width=1265&height=1265")
            .WithDescription(description)
            .WithColor(0xBFA55F);

        if (promote) {
            foreach (var enlisted in promotable) {
                
                var currentPoints = await _db.GetPoints(enlisted.Id);
                RankType? targetRank = null;
                
                foreach (var rank in ranks.Keys.OrderByDescending(r => (int)r)) {
                    if (currentPoints < (int)rank) continue;
                    targetRank = rank;
                    break;
                }
        
                if (targetRank.HasValue) {
                    var targetRole = guild.GetRole(ranks[targetRank.Value]);
                    if (targetRole == null) continue;
                    
                    foreach (var roleId in ranks.Values.Distinct()) {
                        var role = guild.GetRole(roleId);
                        if (role != null && enlisted.Roles.Contains(role)) {
                            await enlisted.RemoveRoleAsync(role);
                        }
                    }
                    await enlisted.AddRoleAsync(targetRole);
                    await _db.SetRank(enlisted.Id, targetRank.Value.ToString());
                }
            }
        }
        await command.FollowupAsync(embed: builder.Build());
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
                enlisteds.Add(_client.GetGuild((ulong)_guildId!).GetUser(((SocketUser)option.Value).Id));
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
        IRole? rank = null;
        
        foreach (var option in command.Data.Options) {
            switch (option.Name) {
                
                case "civilian":
                    civilian = _client.GetGuild((ulong)_guildId!).GetUser(((SocketUser)option.Value).Id);
                    break;
                case "claim_name":
                    claim = option.Value.ToString();
                    break;
                case "rank_name":
                    rank = (IRole)option.Value;
                    break;
                default:
                    await command.FollowupAsync("Unrecognized command.", ephemeral: true);
                    break;
            }
        }

        if (civilian == null) {
            await command.FollowupAsync("Unrecognized account.", ephemeral: true);
            return;
        } if (rank == null || string.IsNullOrWhiteSpace(claim)) {
            await command.FollowupAsync("Unrecognized command.", ephemeral: true);
            return;
        }
        
        var rankName = rank.Name;
        var dotIndex = rankName.IndexOf('.');
            
        var fixedRankNick = rankName.Substring(1, dotIndex);
        var fixedRankFull = rankName[(dotIndex + 2)..];
            
        await civilian.ModifyAsync(x => x.Nickname = fixedRankNick + " " + claim);

        var idType = "ENLISTEDMAIN";
        if ((int)Enum.Parse<RankType>(rankName[1..dotIndex]) == 50000) { idType = "STAFFMAIN"; }
        
        await _db.PreEnlist(command, civilian, claim, civilian.GetGuildAvatarUrl() ?? civilian.GetAvatarUrl(), civilian.Id.ToString(), civilian.JoinedAt ?? civilian.CreatedAt, fixedRankFull,0,0,"N/A","", civilian.Username, idType, "BLACK", "NONE", "BASIC"); 
    }

    public async Task Promote(SocketGuildUser enlisted, IRole rank, SocketSlashCommand? command = null, string? newClaim = null, string? response = null) {
        
        var nickname = enlisted.Nickname;
        var rankName = rank.Name;

        var dotIndex = rankName.IndexOf('.');
        var fixedRankNick = rankName.Substring(1, dotIndex);
        var fixedRankFull = rankName[(dotIndex + 2)..];
        var spaceIndex = nickname.IndexOf(' ');
        string? claim;
        
        if (string.IsNullOrEmpty(newClaim)) {
            claim = spaceIndex >= 0 ? nickname[(spaceIndex + 1)..] : nickname;
        }
        else {
            claim = newClaim; 
            await _db.SetClaim(enlisted.Id, claim);
        }

        if ((int)Enum.Parse<RankType>(rankName[1..dotIndex]) == 50000) {
            await _db.GiveNewId(enlisted.Id, "STAFFMAIN");
            await _db.SetIdType(enlisted.Id, "STAFFMAIN");
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
                    civilian = _client.GetGuild((ulong)_guildId!).GetUser(((SocketUser)option.Value).Id);
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
                    member1 = _client.GetGuild((ulong)_guildId!).GetUser(((SocketUser)option.Value).Id);
                    break;
                case "member2":
                    member2 = _client.GetGuild((ulong)_guildId!).GetUser(((SocketUser)option.Value).Id);
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
