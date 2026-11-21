using Discord;
using Discord.Net;
using Discord.WebSocket;
using SMASSB.Exceptions;
using SMASSB.Models;
using SMASSB.ServiceHandlers;

namespace SMASSB.Commands;

public class RewardSystem {

    private readonly DiscordSocketClient _client;
    private readonly DatabaseService _db;
    private readonly LogHandler _logHandler;
    private readonly ulong? _guildId;
    
    public RewardSystem(DiscordSocketClient client, LogHandler logHandler, DatabaseService db, GuildConfiguration guildConfig) {
        
        _client = client;
        _logHandler = logHandler;
        _db = db;
        _guildId = guildConfig.GuildId;
    }
    
    public async Task HandleRewardKoCommand(SocketSlashCommand command) {

        await command.DeferAsync();
        var enlisteds = new List<SocketGuildUser>();

        foreach (var option in command.Data.Options) {
            
            if (option.Name.StartsWith("enlisted")) {
                enlisteds.Add(_client.GetGuild((ulong)_guildId!).GetUser(((SocketUser)option.Value).Id));
            } else {
                await command.RespondAsync("Unrecognized command.", ephemeral: true);
                return;
            }
        }

        var embedHeadphones = (new EmbedBuilder()
            .WithAuthor("Dear Prospect, you have been awarded . . .")
            .WithTitle("【 TRIAL HEADPHONES 】")
            .WithThumbnailUrl("https://64.media.tumblr.com/bca2cc2f79769603271b08c771604ff8/e0c197076de26732-2b/s540x810/6ba9914651ae690969cd4c896a9f566527fa90a0.pnj")
            .WithDescription("0 1  ∥  040506 | LOCKED\n0 2  ∥  222f2f | LOCKED\n0 3  ∥  0d0f12 | LOCKED")
            .WithImageUrl("https://64.media.tumblr.com/3477f610c193e36e7dec89cdd4950dc9/e0c197076de26732-d9/s1280x1920/77b23df058976d98915498920e55feb130d55470.pnj")
            .WithColor(new Color(0x44786F)).Build());
        
        var embedSword = (new EmbedBuilder()
            .WithAuthor("As well as . . .")
            .WithTitle("【 TRIAL SWORD 】")
            .WithThumbnailUrl("https://64.media.tumblr.com/0af3a5ffef90e7bd73441f53fff50d8a/e0c197076de26732-f7/s540x810/0a2a680a38dd2882a33e6acd700b0849ac5b9d80.pnj")
            .WithDescription("0 1  ∥  08090a | 192126\n0 2  ∥  151618 | LOCKED\n0 3  ∥  260c0c | 08090a\n0 4  ∥  504029 | 08090a")
            .WithImageUrl("https://64.media.tumblr.com/f4e68447a8cc190693797e5563c51d1b/e0c197076de26732-c8/s1280x1920/8b27f009674e970061ce55f34d35e6c136b17d00.pnj")
            .WithFooter("Send your finished uniform in Onshō. Kamikawa Hiromi's DMs to be checked and get on out there!\n\n 恐れも、惨めさも、怒りも無く！・❖")
            .WithColor(new Color(0xBFA55F)).Build());
        
        await command.FollowupAsync("Headphones and Sword have been sent to the other member(s).");
        var failures = new List<MessageSendException>();
        
        foreach (SocketGuildUser enlisted in enlisteds) {
            try {
                
                await enlisted.SendMessageAsync(null, false, embedHeadphones);
                await enlisted.SendMessageAsync(null, false, embedSword);
                await enlisted.RemoveRoleAsync(1537202109336920096);
            } catch (Discord.Net.HttpException ex) {
                failures.Add(new MessageSendException(enlisted.Username, ex));
            }
        }
        
        if (failures.Count > 0) {
            foreach (var e in failures) { await command.FollowupAsync(e.Message); }
        }
    }

    internal record Accomplishment(string Name, ulong? RoleId, Embed[] Embeds, int Points = 0, int Yen = 0, string? NewId = null);

    internal static readonly Dictionary<int, Accomplishment> Accomplishments = new() {
        
        [1] = Standard("TRANSFER", 1473371574710046840, "Left Wing, Main Color 3", "#5d6866", false, "Thin Scarf", "Circle Headphones"),
        [2] = Standard("SUPPORTER", null, "Right Wing, Main Color 3", "#5d6866", true, "Neck Headphones", "No Mask Perms OR Chest Acc. Change"),
        [3] = Standard("HIGH SCOUTER", 1475898897174892769, "Left Wing, Outline 2", "#839390", false, "Neck Ribbon", "No Headphones Perms"),
        [4] = Standard("MAX SCOUTER", 1475899025851945081, "Right Wing, Outline 2", "#839390", true, "Cravat", "Ear Acc. Change (Cannot Remove Them)"),
        [5] = Standard("PERFECT PITCH", 1475899134337617980, "Left Sleeve, Outline 1", "#5d6866", false, "Sleeveless Shirt", "Foreleg Pattern Change"),
        [6] = Standard("WORLD-CLASS IDOL", 1475899268593225829, "Left Sleeve, Main Color 5", "#839390", true, "Longer Sleeves", "Spiked Collar"),
        [7] = Standard("RIKUGUN BUKŌSHŌ I", 1475961765433970880, "Right Sleeve, Outline 1", "#5d6866", true, "Heart Headphones", "Heart Front Socks"),
        [8] = Standard("RIKUGUN BUKŌSHŌ II", 1475899269335744564, "Right Sleeve, Main Color 5", "#839390", true, "Flowy Sleeves", "No Mask Perms OR Chest Acc. Change"),
        [9] = Standard("REBIRTH", 1477926845184872531, "Chest Acc., Color 5", "#839390", true, "Skirt / Pants Change, Chest Acc. Change,", "Hindleg Acc. Change") with { NewId = "NEWGAMEPLUSENLISTED" },
        [10] = Special("A NUTRITIOUS BREAKFAST", 1527907825836097556, 60, 3500, "Please place these codes on your dress on the *Back Bow*. \n✦ **#839390 | #272e2e**\nThen, place these codes on the *Tail*. \n✦ **#0f1111 | #5d6866**\n✦ **#231a16 | #LOCKED**"),
        [11] = Special("STALEMATE", 1527907819649630298, 30, 2000, "Please place this code on your dress on the *Waist Accessory, Main Color 2*. \n✦ **#5d6866**"),
        [12] = Extraneous("CHARITY DONATOR", 1569194840720408656, "Left Sleeve, Outline 6", "#5d6866")
    };

    public async Task HandleRewardAccompCommand(SocketSlashCommand command) {
        
        await command.DeferAsync(ephemeral: true);
        var members = new List<SocketGuildUser>();
        var itemId = 0;
        
        foreach (var option in command.Data.Options) {
            
            if (option.Name.StartsWith("enlisted") && option.Value is SocketGuildUser user) {
                members.Add(user);
            } else if (option.Name == "item") {
                itemId = Convert.ToInt32(option.Value);
            }
        }
        
        if (!Accomplishments.TryGetValue(itemId, out var accomp)) {
            await command.FollowupAsync("Unknown accomplishment.");
            return;
        }
        
        var header = new EmbedBuilder()
            .WithAuthor("Dear Enlistee, you have completed the . . .")
            .WithTitle(accomp.Name + " ACCOMPLISHMENT!")
            .WithColor(0xBFA55F)
            .WithDescription(". . And have been awarded with the following :")
            .WithThumbnailUrl("https://64.media.tumblr.com/7d47f90161168afdb17720c3e645e120/b35d6053bfb9fc2a-03/s400x600/5ed0e321f4aeac2aa3542a520f1df49cbeff1752.pnj")
            .Build();
        
        Embed[] dm = [header, ..accomp.Embeds];
        var rewarded = new List<SocketGuildUser>();
        var errors = new List<string>();
        
        foreach (var member in members) {
            try {
                
                if (accomp.RoleId != null) await member.AddRoleAsync(accomp.RoleId.Value);
                if (accomp.NewId != null) await _db.GiveNewId(member.Id, accomp.NewId);
                if (accomp.Points > 0) await _db.AddPoints(member.Id, accomp.Points);
                if (accomp.Yen > 0) await _db.AddYen(member.Id, accomp.Yen);
                if (accomp.Name == "REBIRTH") await ApplyRebirthRankAsync(member);
                rewarded.Add(member);
                
            } catch (Exception ex) {
                errors.Add($"Couldn't reward {member.Username}: {ex.Message}");
                continue;
            }
            
            try {
                await member.SendMessageAsync(embeds: dm);
            } catch (HttpException ex) {
                errors.Add(new MessageSendException(member.Username, ex).Message);
            }
        }
        
        if (rewarded.Count > 0 && _client.GetGuild(_guildId!.Value).GetChannel(1542227176387121182) is ITextChannel channel) {
            
            var mentions = string.Join(", ", rewarded.Select(m => m.Mention));
            await channel.SendMessageAsync("## Au__tomatic Messaging Syst__em . . \nPlease congratulate " + mentions + " in <#1473211757269876831> \nfor achieving the accomplishment **__" + accomp.Name + "__**!\n\n<@&1473370613992394864>");
        }
        var reply = $"Rewarded {rewarded.Count} member(s) with accomplishment {accomp.Name}.";
        
        if (errors.Count > 0) reply += "\n\n" + string.Join("\n", errors);
        await command.FollowupAsync(reply);
    }

    private async Task ApplyRebirthRankAsync(SocketGuildUser member) {

        var oldRankRoles = member.Roles
            .Select(r => r.Id)
            .Where(id => RoleSystem.RankRoles.ContainsValue(id) && id != RoleSystem.NiShiRoleId)
            .ToList();

        if (oldRankRoles.Count > 0) await member.RemoveRolesAsync(oldRankRoles);
        if (member.Roles.All(r => r.Id != RoleSystem.NiShiRoleId)) await member.AddRoleAsync(RoleSystem.NiShiRoleId);

        var rank = RoleSystem.RebirthMark + RoleSystem.RebirthStartRank;
        await _db.SetRank(member.Id, rank);
        await _db.PushRankAsync(member.Id, member.Guild.Id, rank);

        var claim = await _db.GetClaim(member.Id);
        await member.ModifyAsync(x => x.Nickname = RoleSystem.RebirthMark + "NiShi. " + claim);
    }

    private static Embed RewardEmbed(string title, string description, string? footer = null) {
        
        var embed = new EmbedBuilder().WithTitle(title).WithColor(0xBFA55F).WithDescription(description);
        if (footer != null) embed.WithFooter(footer);
        
        return embed.Build();
    }

    private static Accomplishment Standard(string name, ulong? roleId, string paradeLocation, string paradeHex, bool itempack, string idolItem1, string idolItem2) {
        
        var parade = RewardEmbed("Wearable for Parade Dress!", $"Please place this code on your dress on the *{paradeLocation}*. \n✦ **{paradeHex}**");
        
        var colorpackDescription = "You may change *two* colors on your tracksuit.\nNote that the color you change is changed across the entire uniform. If you are changing Color X, all instances of that must be replaced with Color Y.";
        if (itempack) 
            colorpackDescription += "\n   - You may change or add ONE item to your tracksuit.";
        
        colorpackDescription += "\n\n✦ **181615** and **070a0c** are the same code when recoloring.\n✦ **888375** and **6b6051** are the same code when recoloring.\n✦ Colors and outlines are treated as two separate values, so don't change a main color and it's outline and say that it is one color!";
        var colorpack = RewardEmbed("Custom Colorpack" + (itempack ? " and Custom Itempack" : "") + " for Tracksuit!", colorpackDescription);
        var idol = RewardEmbed("New Itempack for Idol Outfit!", $"You may use **{idolItem1}** and **{idolItem2}** rather than the required items!", "Please send your updated uniforms in the typical uniform checks.");
        
        return new Accomplishment(name, roleId, [parade, colorpack, idol]);
    }

    private static Accomplishment Special(string name, ulong roleId, int points, int yen, string paradeText) {
        
        var description = paradeText + "\n\n- You may change or add ONE item to your tracksuit.";
        var footer = $"You've also received {points} POINTs and ¥{yen}!\n✦ Please send your updated uniform in the typical uniform checks.";
        var embed = RewardEmbed("Wearable for Parade Dress!", description, footer);
        
        return new Accomplishment(name, roleId, [embed], points, yen);
    }
    
    private static Accomplishment Extraneous(string name, ulong roleId, string paradeLocation, string paradeHex) {
        
        var parade = RewardEmbed("Wearable for Parade Dress!", $"Please place this code on your dress on the *{paradeLocation}*. \n✦ **{paradeHex}**");
        var colorpack = RewardEmbed("Custom Colorpack and Itempack for Tracksuit!", "You may change *two* colors on your tracksuit.\nNote that the color you change is changed across the entire uniform. If you are changing Color X, all instances of that must be replaced with Color Y.\n   - You may change or add ONE item to your tracksuit.\n\n✦ **181615** and **070a0c** are the same code when recoloring.\n✦ **888375** and **6b6051** are the same code when recoloring.\n✦ Colors and outlines are treated as two separate values, so don't change a main color and it's outline and say that it is one color!");
        var idol = RewardEmbed("New Itempack for Idol Outfit!", $"You may change *two* required items on your idol outfit!", "Please send your updated uniforms in the typical uniform checks.");
        
        return new Accomplishment(name, roleId, [parade, colorpack, idol]);
    }
}