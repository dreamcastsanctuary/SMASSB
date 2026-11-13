using System.Text.RegularExpressions;
using Discord;
using Discord.Net;
using Discord.WebSocket;
using SMASSB.Data;
using SMASSB.Models;
using SMASSB.ServiceHandlers;

namespace SMASSB.Commands;

public class RoleSystem {
    
    private readonly DiscordSocketClient _client;
    private readonly DatabaseService _db;
    private readonly LogHandler _logHandler;
    private readonly ulong? _guildId;
    
    public RoleSystem(DiscordSocketClient client, 
                        LogHandler logHandler, 
                        DatabaseService db, 
                        GuildConfiguration guildConfig) {
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
        
        try { await civilian.SendMessageAsync($"Welcome to SANGŌ, **Kō. {claim}**! We're very happy to have you.\n" + "Your first event *must* be of type **CIVT / Civilian Training**. Please be on the lookout for it."); }
        catch (HttpException ex) when (ex.DiscordCode == DiscordErrorCode.CannotSendMessageToUser) { await command.FollowupAsync($"Hey, <@{civilian.Id}>! Please turn your Server DMs on so that I can message you important information regarding your enlistment!\nThank you!"); return; }
        
        await civilian.AddRoleAsync(1473369036766052445);
        await civilian.AddRoleAsync(1475886792174604484);
        await civilian.RemoveRoleAsync(1473369383471677461);

        await civilian.ModifyAsync(x => x.Nickname = "Kō. " + claim);

        if (claim != null) {
            await _db.PreEnlist(command, civilian, claim, civilian.GetGuildAvatarUrl() ?? civilian.GetAvatarUrl() ?? civilian.GetDefaultAvatarUrl() ?? civilian.GetDisplayAvatarUrl(), civilian.Id.ToString(), civilian.JoinedAt ?? civilian.CreatedAt, "Jieikan Kōhosei", 0, 0, "N/A", "", civilian.Username, "ENLISTEDMAIN", "BLACK", "NONE", "BASIC");

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
            description += $"{await _db.GetClaim(promotable[i].Id)} :: <@{promotable[i].Id}> -> {promoteTo[i]} ∥ {await _db.GetPoints(promotable[i].Id)} pts.\n";
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
                    await Promote(enlisted, targetRole);
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

    public async Task HandleReinstateEnlistmentCommand(SocketSlashCommand command) {
        
        await command.DeferAsync();
        SocketGuildUser? civilian = null;
        
        foreach (var option in command.Data.Options) {
            switch (option.Name) {
                
                case "civilian":
                    civilian = _client.GetGuild((ulong)_guildId!).GetUser(((SocketUser)option.Value).Id);
                    break;
                default:
                    await command.FollowupAsync("Unrecognized command.", ephemeral: true);
                    break;
            }
        }

        if (civilian == null || string.IsNullOrEmpty(await _db.GetHistoryClaim(civilian.Id))) {
            await command.FollowupAsync("Unrecognized account.", ephemeral: true);
            return;
        }

        try {
            await _db.ReinstateEnlistment(civilian.Id, civilian.Id, _client);
            var (rankId, categoryId) = (await _db.GetRank(civilian.Id)).GetRoleId();
            
            await civilian.RemoveRoleAsync(1473369383471677461);
            await civilian.AddRolesAsync([rankId, categoryId]);
            
            var rank = await _client.GetGuild((ulong)_guildId!).GetRoleAsync(rankId);
            var rankName = rank.Name;
            var dotIndex = rankName.IndexOf('.');
            
            var fixedRankNick = rankName.Substring(1, dotIndex);
            await civilian.ModifyAsync(x => x.Nickname = fixedRankNick + " " + _db.GetClaim(civilian.Id));
            
        } catch (Exception ex) {
            await _logHandler.LogExceptionWatch((ulong)_guildId!, exception: ex);
            await command.FollowupAsync("Something went wrong.", ephemeral: true);
        }
    }
    
    public async Task HandleMoveEnlistmentCommand(SocketSlashCommand command) {
    
        await command.DeferAsync();
        ulong? oldId = null;
        SocketGuildUser? newAccount = null;
    
        foreach (var option in command.Data.Options) {
            switch (option.Name) {
                case "old_account":
                    oldId = ((IUser)option.Value).Id;
                    break;
                case "new_account":
                    newAccount = _client.GetGuild((ulong)_guildId!).GetUser(((SocketUser)option.Value).Id);
                    break;
                default:
                    await command.RespondAsync("Unrecognized command.", ephemeral: true);
                    return;
            }
        }

        if (oldId == null || newAccount == null || string.IsNullOrEmpty(await _db.GetHistoryClaim(oldId.Value))) {
            await command.FollowupAsync("Unrecognized account(s).", ephemeral: true);
            return;
        }

        try {
            await _db.ReinstateEnlistment(oldId.Value, newAccount.Id, _client);
            var (rankId, categoryId) = (await _db.GetRank(newAccount.Id)).GetRoleId();
        
            await newAccount.RemoveRoleAsync(1473369383471677461);
            await newAccount.AddRolesAsync([rankId, categoryId]);

            var rank = await _client.GetGuild((ulong)_guildId!).GetRoleAsync(rankId);
            var rankName = rank.Name;
            var dotIndex = rankName.IndexOf('.');
            
            var fixedRankNick = rankName.Substring(1, dotIndex);
            await newAccount.ModifyAsync(x => x.Nickname = fixedRankNick + " " + _db.GetClaim(newAccount.Id));

            await command.FollowupAsync($"Moved enlistment to {newAccount.Mention}.");
            
        } catch (Exception ex) {
            await _logHandler.LogExceptionWatch((ulong)_guildId!, exception: ex);
            await command.FollowupAsync("Something went wrong.", ephemeral: true);
        }
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

    private async Task Promote(SocketGuildUser enlisted, IRole rank, SocketSlashCommand? command = null, string? newClaim = null, string? response = null) {
        
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
        await _db.PushRankAsync(enlisted.Id, (ulong)_guildId!, fixedRankFull);

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
        await kohosei.SendMessageAsync("Congratulations! You've successfully ranked up to **NiShi. Nitō Shi**. We hope to see much more from you in the future.\n\nYou've earned your final uniforms, which you can find in the new \"ENLISTED\" uniform channel.\nYou'll need to submit these before your rank is changed.");
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

    private static readonly Regex UnitLineRegex = new Regex(@"^\s*(?<name>.+?)\s+(?<units>(?:u(?:drill[\s_-]*team|striker|researcher|journalist|recon|medic)\s*){1,2})$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex UnitTokenRegex = new Regex(@"u(?<unit>drill[\s_-]*team|striker|researcher|journalist|recon|medic)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    
    private static readonly Dictionary<string, (string Name, ulong RoleId, string UniformLink, string? GuideLink, string Thumbnail)> UnitRoles = new(StringComparer.OrdinalIgnoreCase) {

        ["striker"] = ("Striker", 1557735885192040448, "https://discord.com/channels/1471660035854569505/1473516609397063680", null, "61st-emblem.png"),
        ["drillteam"]  = ("Drill Team", 1557736055778713641, "https://sangoidoldefenseforce.vercel.app/nbsvvdokw", null, "61st-emblem.png"),
        ["researcher"] = ("Researcher", 1557735964950929558, "https://sangoidoldefenseforce.vercel.app/bocokbmrob", "https://docs.google.com/document/d/1XHVLCm9fh7x0ZYHXgSMl90srZ2H7Tags9ule6lW9a4g/edit?usp=sharing", "233rd-emblem.png"),
        ["journalist"] = ("Journalist", 1557736035495059477, "https://sangoidoldefenseforce.vercel.app/tyebxkvscd", "https://docs.google.com/document/d/18_kBgrC0wux0LhOJIhbGePnxVn2hQi3PB3jLkKDJVQE/edit?usp=sharing", "233rd-emblem.png"),
        ["recon"] = ("Recon", 1568931405134561501, "https://sangoidoldefenseforce.vercel.app/bomyx", "https://docs.google.com/document/d/1kR_UppQhhbonCBp3TsEJcRhrTAzyFknTTF3AA3v0toE/edit?tab=t.0", "233rd-emblem.png"),
        ["medic"] = ("Medic", 1557736016016576522, "https://sangoidoldefenseforce.vercel.app/wonsm", "https://docs.google.com/document/d/17cNiFP86K7Q4dq73WU6AqiBIXj3UaZ4gjhpFkUEmO74/edit?usp=sharing", "45th-emblem.png"),
    };
    
    private static IEnumerable<ulong> ExclusiveUnitRoleIds => UnitRoles.Values.Select(u => u.RoleId).Where(id => id != 1557736055778713641).Distinct();
    
    public async Task UnitAccept(SocketSlashCommand command) {
        
        string? messageLink = null;

        foreach (var option in command.Data.Options) {
            switch (option.Name) {
                case "message_link":
                    messageLink = option.Value.ToString();
                    break;
                default:
                    await command.RespondAsync("Unrecognized command.", ephemeral: true);
                    return;
            }
        }

        if (string.IsNullOrWhiteSpace(messageLink)) {
            await command.RespondAsync("You need to supply a message link.", ephemeral: true);
            return;
        }

        var linkMatch = Regex.Match(messageLink, @"channels/(\d+)/(\d+)/(\d+)");
        if (!linkMatch.Success) { 
            await command.RespondAsync("That doesn't look like a valid message link.", ephemeral: true);
            return;
        }

        await command.DeferAsync();

        var channelId = ulong.Parse(linkMatch.Groups[2].Value);
        var messageId = ulong.Parse(linkMatch.Groups[3].Value);

        var guild = _client.GetGuild((ulong)_guildId!);
        var channel = guild.GetTextChannel(channelId);
        if (channel == null) {
            await command.FollowupAsync("I couldn't find that channel! Does it... exist?");
            return;
        }

        IMessage? message;
        try {
            message = await channel.GetMessageAsync(messageId);
        } catch {
            message = null;
        }

        if (message == null) {
            await command.FollowupAsync("I couldn't find that message.");
            return;
        }

        var allClaims = _db.GetAllClaims();

        var lines = message.Content
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.Trim())
            .Where(l => l.Length > 0);

        var successes = new List<string>();
        var unparsed = new List<string>();
        var notFound = new List<string>();
        var ambiguous = new List<string>();
        var failures = new List<string>();

        foreach (var line in lines) {
            
            var lineMatch = UnitLineRegex.Match(line);
            if (!lineMatch.Success) {
                unparsed.Add($"\"{line}\"");
                continue;
            }

            var name = lineMatch.Groups["name"].Value.Trim();

            var units = new List<(string Name, ulong RoleId, string UniformLink, string? GuideLink, string Thumbnail)>();
            var duplicateFound = false;

            foreach (Match token in UnitTokenRegex.Matches(lineMatch.Groups["units"].Value)) {
                
                var key = Regex.Replace(token.Groups["unit"].Value, @"[\s_-]", "");
                var unit = UnitRoles[key];

                if (units.Any(u => u.RoleId == unit.RoleId)) {
                    duplicateFound = true;
                    break;
                }
                units.Add(unit);
            }

            if (duplicateFound) {
                unparsed.Add($"\"{line}\" (same unit listed twice!)");
                continue;
            }

            if (units.Count(u => u.RoleId != 1557736055778713641) > 1) {
                unparsed.Add($"\"{line}\" (only Drill Team can be combined with another unit, man!)");
                continue;
            }

            var matches = allClaims
                .Where(m => !string.IsNullOrWhiteSpace(m.Claim) && string.Equals(m.Claim.Trim(), name, StringComparison.OrdinalIgnoreCase))
                .ToList();
            var bestDistance = 0;

            if (matches.Count == 0) {
                var scored = allClaims
                    .Where(m => !string.IsNullOrWhiteSpace(m.Claim))
                    .Select(m => new { Claim = m, Distance = FuzzyContainsDistance(m.Claim, name) })
                    .Where(x => x.Distance <= MaxAllowedDistance(name))
                    .OrderBy(x => x.Distance)
                    .ToList();

                if (scored.Count == 0) {
                    notFound.Add(name);
                    continue;
                }
                bestDistance = scored[0].Distance;
                matches = scored.Where(x => x.Distance == bestDistance).Select(x => x.Claim).ToList();
            }

            if (matches.Count > 1) {
                ambiguous.Add($"{name} (matched: {string.Join(", ", matches.Select(m => m.Claim))})");
                continue;
            }

            var wasFuzzy = bestDistance > 0;
            
            var userId = ulong.Parse(matches[0].UserId);
            IGuildUser? guildUser = guild.GetUser(userId);

            if (guildUser == null) {
                notFound.Add($"{name} (claim found, but they're not in the server)");
                continue;
            }

            var toAdd = units.Where(u => !guildUser.RoleIds.Contains(u.RoleId)).ToList();
            var alreadyHad = units.Where(u => guildUser.RoleIds.Contains(u.RoleId)).ToList();

            var newExclusive = units.Where(u => u.RoleId != 1557736055778713641).Select(u => u.RoleId).ToList();
            var toRemove = newExclusive.Count == 0
                ? new List<ulong>()
                : ExclusiveUnitRoleIds.Where(id => !newExclusive.Contains(id) && guildUser.RoleIds.Contains(id)).ToList();

            try {
                if (toAdd.Count > 0)
                    await guildUser.AddRolesAsync(toAdd.Select(u => u.RoleId));
                if (toRemove.Count > 0)
                    await guildUser.RemoveRolesAsync(toRemove);
                
            } catch (HttpException ex) {
                failures.Add($"<@{userId}> : couldn't assign roles ({ex.Reason ?? ex.Message})");
                continue;
            }
            
            var dmFailed = false;
            if (toAdd.Count > 0) {
                var dmEmbed = BuildWelcomeEmbed(toAdd);
                var thumbnailPath = Path.Combine(AppContext.BaseDirectory, "Images", toAdd[0].Thumbnail);

                try {
                    using var thumbnail = new FileAttachment(thumbnailPath);
                    var dm = await guildUser.CreateDMChannelAsync();
                    await dm.SendFileAsync(thumbnail, embed: dmEmbed);
                    
                } catch (Exception ex) {
                    dmFailed = true;
                    await _logHandler.LogExceptionWatch((ulong)_guildId!, exception: ex);
                }
            }

            string summary;
            if (toAdd.Count > 0) {
                summary = $"<@{userId}> has been accepted into {string.Join(" and ", toAdd.Select(u => $"***{u.Name}***"))}.";
                if (alreadyHad.Count > 0)
                    summary += $" *(already in {string.Join(" and ", alreadyHad.Select(u => u.Name))})*";
            } else {
                summary = $"<@{userId}> is already in {string.Join(" and ", alreadyHad.Select(u => $"***{u.Name}***"))}.";
            }

            if (wasFuzzy)
                summary += $" *(matched \"{name}\" → \"{matches[0].Claim}\", {bestDistance} char{(bestDistance == 1 ? "" : "s")} off)*";

            if (toRemove.Count > 0) {
                var removedNames = toRemove.Select(id => UnitRoles.Values.First(u => u.RoleId == id).Name);
                summary += $" *(removed from {string.Join(" and ", removedNames)})*";
            }

            if (dmFailed) summary += " *(couldn't DM them)*";
            successes.Add(summary);
        }

        var description = "";
        if (successes.Count > 0) description += string.Join("\n", successes) + "\n";
        if (unparsed.Count > 0)  description += "\n**Couldn't parse:**\n" + string.Join("\n", unparsed) + "\n";
        if (notFound.Count > 0)  description += "\n**Couldn't find a match for:**\n" + string.Join("\n", notFound) + "\n";
        if (ambiguous.Count > 0) description += "\n**Multiple matches found for:**\n" + string.Join("\n", ambiguous) + "\n";
        if (failures.Count > 0)  description += "\n**Failed to assign roles:**\n" + string.Join("\n", failures);

        if (description.Length == 0) description = "Nothing to process! The message looks empty.";

        await SendChunkedEmbedAsync(command, "❖﹒Unit acceptance . .", description);
    }
    
    private static int MaxAllowedDistance(string name) {
        return name.Length <= 4 ? 1 : Math.Min(2, name.Length / 5 + 1);
    }

    private static int FuzzyContainsDistance(string haystack, string needle) {
        
        if (haystack.Contains(needle, StringComparison.OrdinalIgnoreCase)) return 0;
        if (haystack.Length < needle.Length - 1) return LevenshteinDistance(haystack, needle);

        var best = int.MaxValue;
        var n = needle.Length;

        for (var windowLen = Math.Max(1, n - 1); windowLen <= n + 1 && windowLen <= haystack.Length; windowLen++) {
            for (var start = 0; start <= haystack.Length - windowLen; start++) {
                var window = haystack.Substring(start, windowLen);
                var dist = LevenshteinDistance(window, needle);
                if (dist < best) best = dist;
                if (best == 0) return 0;
            }
        }
        return best;
    }

    private static int LevenshteinDistance(string a, string b) {
        
        a = a.ToLowerInvariant();
        b = b.ToLowerInvariant();
        var dp = new int[a.Length + 1, b.Length + 1];

        for (var i = 0; i <= a.Length; i++) dp[i, 0] = i;
        for (var j = 0; j <= b.Length; j++) dp[0, j] = j;

        for (var i = 1; i <= a.Length; i++) {
            for (var j = 1; j <= b.Length; j++) {
                var cost = a[i - 1] == b[j - 1] ? 0 : 1;
                dp[i, j] = Math.Min(
                    Math.Min(dp[i - 1, j] + 1, dp[i, j - 1] + 1),
                    dp[i - 1, j - 1] + cost);
            }
        }
        return dp[a.Length, b.Length];
    }
    
    private static async Task SendChunkedEmbedAsync(SocketSlashCommand command, string title, string description) {
        
        const int maxDescriptionLength = 4096;
        var chunks = new List<string>();
        var currentChunk = "";

        foreach (var line in description.Split('\n')) {
            if ((currentChunk + line + "\n").Length > maxDescriptionLength) {
                if (!string.IsNullOrEmpty(currentChunk)) chunks.Add(currentChunk.TrimEnd());
                currentChunk = line + "\n";
            } else {
                currentChunk += line + "\n";
            }
        }
        if (!string.IsNullOrEmpty(currentChunk)) chunks.Add(currentChunk.TrimEnd());

        for (var i = 0; i < chunks.Count; i++) {
            var embed = new EmbedBuilder()
                .WithTitle(chunks.Count == 1 ? title : $"{title} (Part {i + 1} / {chunks.Count})")
                .WithColor(0xBFA55F)
                .WithDescription(chunks[i])
                .Build();
            await command.FollowupAsync(embed: embed);
        }
    }
    
    private static Embed BuildWelcomeEmbed(List<(string Name, ulong RoleId, string UniformLink, string? GuideLink, string Thumbnail)> units) {
        
        var plural = units.Count > 1;
        var designation = string.Join(" and ", units.Select(u => u.Name));
        var division = plural ? "Units" : "Unit";

        string Links(IEnumerable<string> urls) {
            
            var list = urls.ToList();
            var text = $"[C__lick her__e](<{list[0]}>)";
            
            foreach (var url in list.Skip(1)) 
                text += $" and [__her__e](<{url}>)";
            
            return text;
        }

        var guides = units.Where(u => u.GuideLink != null).Select(u => u.GuideLink!).ToList();

        var guideLine = guides.Count > 0
            ? $"<:sango_a_goldarrow:1565905710875676722> {Links(guides)} to read your Basic Guide{(guides.Count > 1 ? "s" : "")}.\n"
            : "";

        var advice = guides.Count > 0
            ? "\nRead your Guide, come up with new ideas / study what you've been given, and speak with your new peers."
            : "\nCome up with new ideas and speak with your new peers.";

        var description = $"## <:sango_a_beigefourstar:1566153893635690657> ・ Welcome to the {designation} {division}, enlisted.\n_ _\n*We're honored to have you stand by us.*\n\n<:sango_a_redarrow:1565905694652243998> {Links(units.Select(u => u.UniformLink))} to access your Fall uniform{(plural ? "s.\n> -# Make only the helmet for the Drill Team uniforms" : "")}.\n{guideLine}\nYou will eventually be called to an in-person training in the <#1568904427241218069> channel, where you'll be taught how to properly do your new job.\n\nUntil then, get your uniforms in and find a feel for your new responsibilities, however you see fit. \n{advice}\n\n*Good luck, and make us proud.*\n\n> ### <:sango_emblem_mono:1492222638980989138> ⋮ For the Good of All. ⋮ <:sango_a_redfourstar:1565904661922455583>";
        
        return new EmbedBuilder()
            .WithDescription(description)
            .WithThumbnailUrl($"attachment://{units[0].Thumbnail}")
            .WithColor(0xBC3533)
            .Build();
    }

    public static string PrettyName(string enumName) => Regex.Replace(enumName, @"(?<=[a-z])(?=[A-Z0-9])|(?<=[0-9])(?=[A-Z])", " ");
    
    public async Task UnitAssignment(SocketSlashCommand command) {

        string? operationStr = null, locationStr = null, description = null;
        var unitKeys = new List<string>();

        foreach (var option in command.Data.Options) {
            
            if (option.Name.StartsWith("unit")) {
                unitKeys.Add(option.Value.ToString()!);
                
            } else switch (option.Name) {
                case "operation_type":
                    operationStr = option.Value.ToString();
                    break;
                case "location":
                    locationStr = option.Value.ToString();
                    break;
                case "description":
                    description = option.Value.ToString();
                    break;
                default:
                    await command.RespondAsync("Unrecognized command.", ephemeral: true);
                    return;
            }
        }

        if (!Enum.TryParse<OperationType>(operationStr, out var operation)) {
            await command.RespondAsync("That isn't a valid operation type.", ephemeral: true);
            return;
        } 
        if (!Enum.TryParse<LocationType>(locationStr, out var location)) {
            await command.RespondAsync("That isn't a valid location. Pick one from the list!", ephemeral: true);
            return;
        }
        if (string.IsNullOrWhiteSpace(description)) {
            await command.RespondAsync("You need to give a description.", ephemeral: true);
            return;
        }

        var units = unitKeys.Distinct().Where(UnitRoles.ContainsKey).Select(k => UnitRoles[k]).ToList();
        if (units.Count == 0) {
            await command.RespondAsync("You need to pick at least one Unit.", ephemeral: true);
            return;
        }

        await command.DeferAsync(ephemeral: true);

        var guild = _client.GetGuild((ulong)_guildId!);
        var channel = guild.GetTextChannel(1568904427241218069);
        if (channel == null) {
            await command.FollowupAsync("I couldn't find the assignment channel.", ephemeral: true);
            return;
        }

        var pings = string.Join(" | ", units.Select(u => $"<@&{u.RoleId}>"));
        var deadline = DateTimeOffset.UtcNow.AddMinutes(30).ToUnixTimeSeconds();

        var embedDescription =
            $"## <:sango_a_redarrow:1565905694652243998> ・ 『 **OPERATION**　⋮　{PrettyName(operation.ToString())} 』\n" +
            $"<:sango_a_note:1565904348066750654> **LOCATION**　⋮　{PrettyName(location.ToString())}\n" +
            $"<:sango_a_note:1565904348066750654> **TIME LIMIT**　⋮　<t:{deadline}:R>\n\n" +
            $"> \" {description} \"";

        var embedBuilder = new EmbedBuilder()
            .WithDescription(embedDescription)
            .WithColor(0xBC3533)
            .WithImageUrl("https://media.discordapp.net/attachments/1480816858998374501/1569231818098479155/image.png?ex=6af7fa84&is=6af6a904&hm=ae8206325680bc74c0423f5b497c2e843b60ed3389370cfc4707259a7802d0ca&=&format=webp&quality=lossless");
        var embed = embedBuilder.Build();

        var allowedMentions = new AllowedMentions(AllowedMentionTypes.Roles);
        await channel.SendMessageAsync($"-# {pings}", embed: embed, allowedMentions: allowedMentions);
        await command.FollowupAsync($"Assignment sent to {channel.Mention}.", ephemeral: true);
    }
}