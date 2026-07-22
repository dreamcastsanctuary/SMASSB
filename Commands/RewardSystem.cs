using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using SMASSB.Exceptions;

namespace SMASSB.Commands;

public class RewardSystem {
    
    [DefaultMemberPermissions(GuildPermission.ManageRoles)]
    public async Task HandleRewardKoCommand(SocketSlashCommand command) {

        await command.DeferAsync();
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
                    await command.FollowupAsync("Unrecognized command.", ephemeral: true);
                    break;
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
            .WithFooter("Send your finished uniform in Onshō. Kamikawa Hiromi's DMs to be checked and get on out there!\n\n 陽がまた輝きますように！・❖")
            .WithColor(new Color(0xBFA55F)).Build());
        
        await command.FollowupAsync("Headphones and Sword have been sent to the other member(s).");
        var failures = new List<MessageSendException>();
        
        foreach (SocketGuildUser enlisted in enlisteds) {
            try {
                await UserExtensions.SendMessageAsync(enlisted, null, false, embedHeadphones);
                await UserExtensions.SendMessageAsync(enlisted, null, false, embedSword);
            } catch (Discord.Net.HttpException ex) {
                failures.Add(new MessageSendException(enlisted.Username, ex));
            }
        }
        
        if (failures.Count > 0) {
            foreach (var e in failures) { await command.FollowupAsync(e.Message); }
        }
    }

    [DefaultMemberPermissions(GuildPermission.ManageRoles)]
    public async Task HandleRewardAccompCommand(SocketSlashCommand command, DiscordSocketClient client) {
        
        await command.DeferAsync();
        
        string[] accompName = {"TRANSFER", "SUPPORTER", "HIGH SCOUTER", "MAX SCOUTER", "PERFECT PITCH", "WORLD-CLASS IDOL", "RIKUGUN BUKŌSHŌ I", "RIKUGUN BUKŌSHŌ II", "REBIRTH"};
        string[] paradeLocation = {"Left Wing, Main Color 3", "Right Wing, Main Color 3", "Left Wing, Outline 2", "Right Wing, Outline 2", "Left Sleeve, Outline 1", "Left Sleeve, Main Color 5", "Right Sleeve, Outline 1", "Right Sleeve, Main Color 5", "Chest Acc., Color 5"};
        string[] paradeHex = {"#5d6866", "#5d6866","#839390", "#839390", "#5d6866", "#839390","#5d6866", "#839390", "#839390"};
        string[] itemPackTrack1 = {"", " and Custom Itempack", "", " and Custom Itempack", "", " and Custom Itempack", " and Custom Itempack", " and Custom Itempack", " and Custom Itempack"};
        string[] itemPackTrack2 = {"", "\n   - You may change or add ONE item to your tracksuit.", "", "\n   - You may change or add ONE item to your tracksuit.", "", "\n   - You may change or add ONE item to your tracksuit.", "\n   - You may change or add ONE item to your tracksuit.", "\n   - You may change or add ONE item to your tracksuit.", "\n   - You may change or add ONE item to your tracksuit."};
        string[] itemPackIdol1 = {"Thin Scarf", "Neck Headphones", "Neck Ribbon", "Cravat", "Sleeveless Shirt", "Longer Sleeves", "Heart Headphones", "Flowy Sleeves", "Skirt / Pants Change, Chest Acc. Change,"};
        string[] itemPackIdol2 = {"Circle Headphones", "No Mask Perms OR Chest Acc. Change", "No Headphones Perms", "Ear Acc. Change (Cannot Remove Them)", "Foreleg Pattern Change", "Spiked Collar", "Heart Front Socks", "No Mask Perms OR Chest Acc. Change", "Hindleg Acc. Change"};
        
        SocketGuildUser assignedTo = null;
        int item = 0;
        int value = 0;

        foreach (var option in command.Data.Options)
        {
            switch (option.Name)
            {

                case "enlisted":
                    assignedTo = ((SocketGuildUser)option.Value);
                    break;
                case "item":
                    item = (int)(long)option.Value;
                    break;
                default:
                    await command.RespondAsync("Unrecognized command.", ephemeral: true);
                    return;
            }
        }
        
        if (assignedTo == null)
        {
            await command.RespondAsync("Unrecognized account.", ephemeral: true);
            return;
        }

        switch (item) {
            
            case 1:
                await assignedTo.AddRoleAsync(1473371574710046840);
                break;
            case 3:
                await assignedTo.AddRoleAsync(1475898897174892769);
                break;
            case 4:
                await assignedTo.AddRoleAsync(1475899025851945081);
                break;
            case 5:
                await assignedTo.AddRoleAsync(1475899134337617980);
                break;
            case 6:
                await assignedTo.AddRoleAsync(1475899268593225829);
                break;
            case 7:
                await assignedTo.AddRoleAsync(1475961765433970880);
                break;
            case 8:
                await assignedTo.AddRoleAsync(1475899269335744564);
                break;
            case 9:
                await assignedTo.AddRoleAsync(1477926845184872531);
                break;
            default:
                await command.FollowupAsync("Unrecognized command.", ephemeral: true);
                return;
        }
        
        value = item;
        List<EmbedBuilder> embeds = new List<EmbedBuilder>();
        
        embeds.Add(new EmbedBuilder()
            .WithAuthor("Dear Enlistee, you have completed the . . .")
            .WithTitle(accompName[value - 1] + " ACCOMPLISHMENT!")
            .WithColor(0xBFA55F)
            .WithDescription(". . And have been awarded with the following :")
            .WithThumbnailUrl("https://64.media.tumblr.com/7d47f90161168afdb17720c3e645e120/b35d6053bfb9fc2a-03/s400x600/5ed0e321f4aeac2aa3542a520f1df49cbeff1752.pnj"));
        
        embeds.Add(new EmbedBuilder()
            .WithTitle("Wearable for Parade Dress!")
            .WithColor(0xBFA55F)
            .WithDescription("Please place this code on your dress on the *" + paradeLocation[value - 1] + "*. \n✦ **" + paradeHex[value - 1] + "**"));

        embeds.Add(new EmbedBuilder()
            .WithTitle("Custom Colorpack" + itemPackTrack1[value - 1] + " for Tracksuit!")
            .WithColor(0xBFA55F)
            .WithDescription("You may change *two* colors on your tracksuit." + itemPackTrack2[value - 1] + "\n\n✦ **181615** and **070a0c** are the same code when recoloring.\n✦ **888375** and **6b6051** are the same code when recoloring.\n✦ Colors and outlines are treated as two separate values, so don't change a main color and it's outline and say that it is one color!"));

        embeds.Add(new EmbedBuilder()
            .WithTitle("New Itempack for Idol Outfit!")
            .WithColor(0xBFA55F)
            .WithDescription("You may use **" + itemPackIdol1[value - 1] + "** and **" + itemPackIdol2[value - 1] + "** rather than the required items!")
            .WithFooter("Please send your updated uniforms in the typical uniform checks."));
        
        await command.FollowupAsync(text: "Rewarded member with accomplishment " + accompName[value - 1] + ".", ephemeral: true);

        foreach (var embed in embeds) {
            try { await UserExtensions.SendMessageAsync(assignedTo, "", false, embed.Build()); }
            catch (Discord.Net.HttpException ex) { await command.FollowupAsync(new MessageSendException(ex.Message, ex).Message); }
        }
        
        var channel = client.GetChannel(1473209020285452360) as ITextChannel;
        
        await channel.SendMessageAsync("## Au__tomatic Messaging Syst__em . . \nPlease congratulate <@" + assignedTo.Id + "> in <#1473211757269876831> \nfor achieving the accomplishment **__" + accompName[value - 1]+ "__**!\n\n<@&1473370613992394864>");
    }
}
