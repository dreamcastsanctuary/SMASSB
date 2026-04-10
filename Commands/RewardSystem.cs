using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace SMASSB.Commands;

public class RewardSystem {
    
    [DefaultMemberPermissions(GuildPermission.ManageRoles)]
    public async Task HandleRewardKoCommand(SocketSlashCommand command) {
        
        SocketGuildUser assignee = (SocketGuildUser)command.User; 
        SocketGuildUser assignedTo = null;
        int item = 1;

        foreach (var option in command.Data.Options)
        {
            switch (option.Name) {
                
                case "kosohei":
                    assignedTo = ((SocketGuildUser)option.Value);
                    break;
                case "item":
                    item = (int)(long)option.Value;
                    break;
                default:
                    await command.RespondAsync("Unrecognized command.", ephemeral: true);
                    break;
            }
        }

        if (assignedTo == null) {
            await command.RespondAsync("Unrecognized account.", ephemeral: true);
            return;
        }

        var embedBuilder = new EmbedBuilder();
        
        if (item == 1) {
            embedBuilder
                .WithAuthor("Dear Prospective Student, you have been awarded . . .")
                .WithTitle("TRIAL HEADPHONES")
                .WithThumbnailUrl("https://64.media.tumblr.com/bca2cc2f79769603271b08c771604ff8/e0c197076de26732-2b/s540x810/6ba9914651ae690969cd4c896a9f566527fa90a0.pnj")
                .WithDescription("These are your first step into enlisting into our ranks.\n\n0 1  ∥  040506 | LOCKED\n0 2  ∥  222f2f | LOCKED\n0 3  ∥  0d0f12 | LOCKED")
                .WithImageUrl("https://64.media.tumblr.com/3477f610c193e36e7dec89cdd4950dc9/e0c197076de26732-d9/s1280x1920/77b23df058976d98915498920e55feb130d55470.pnj")
                .WithFooter("Send your finished uniform in BCGch. Kamikawa Hiromi's DMs to be checked and be able to move onto SCS102!\n\nSing for hope | Strike for all!")
                .WithColor(new Color(0x44786F));
        } else if (item == 2) {
            embedBuilder
                .WithAuthor("Dear Prospective Student, you have been awarded . . .")
                .WithTitle("TRIAL SWORD")
                .WithThumbnailUrl("https://64.media.tumblr.com/0af3a5ffef90e7bd73441f53fff50d8a/e0c197076de26732-f7/s540x810/0a2a680a38dd2882a33e6acd700b0849ac5b9d80.pnj")
                .WithDescription("You've made it past your mandatory lectures! Good going.\nYou can attend Downtown Patrols now that you have this sword in hand.\n\n0 1  ∥  08090a | 192126\n0 2  ∥  151618 | LOCKED\n0 3  ∥  260c0c | 08090a\n0 4  ∥  504029 | 08090a")
                .WithImageUrl("https://64.media.tumblr.com/f4e68447a8cc190693797e5563c51d1b/e0c197076de26732-c8/s1280x1920/8b27f009674e970061ce55f34d35e6c136b17d00.pnj")
                .WithFooter("Send your finished uniform in BCGch. Kamikawa Hiromi's DMs to be checked and get on out there!\n\nSing for hope | Strike for all!")
                .WithColor(new Color(0xBFA55F));
        } else if (item == 3) {
            embedBuilder
                .WithAuthor("Dear Prospective Student, you have been awarded . . .")
                .WithTitle("PARADE DRESS")
                .WithThumbnailUrl("https://64.media.tumblr.com/9c0494481ed62afa3249222b17ae5610/e0c197076de26732-f5/s2048x3072/b325d552629a22c6c61896c9fc83c03311e0e7d7.pnj")
                .WithDescription("Wow! Take a look at you, finally ready to move up in the ranks!\nYou'll have to complete this uniform and submit it in order to attend the upcoming enlistment ceremony.\nPlease be on time!\n\n[. . PRE-ENLISTED PARADE DRESS . .](<https://docs.google.com/document/d/1mQN15_Rxn1VtBBWSjVgqo8x1_zpSh0iDTtrfbcvyNNk/edit?tab=t.0>)")
                .WithImageUrl("https://64.media.tumblr.com/616bce1d6e1a6d7a2123c76d6f249404/2ecded076fd064e9-c6/s1280x1920/11d327bf242c41a07ca122757d050c6c6ce52da1.pnj")
                .WithFooter("Send your finished uniform in BCGch. Kamikawa Hiromi's DMs to be checked. We're all proud of you!\n\nSing for hope | Strike for all!")
                .WithColor(new Color(0xFF312C));
        }
        
        Embed finalEmbed = embedBuilder.Build();
        
        await command.RespondAsync(text: "This message has been sent to both you and the other member: ",embed: finalEmbed, ephemeral: true);
        await UserExtensions.SendMessageAsync(assignedTo, null, false, finalEmbed);
        await UserExtensions.SendMessageAsync(assignee, null, false, finalEmbed);
    }

    [DefaultMemberPermissions(GuildPermission.ManageRoles)]
    public async Task HandleRewardAccompCommand(SocketSlashCommand command, DiscordSocketClient client) {
        
        string[] accompName = {"TRANSFER STUDENT", "SUPPORTER", "HIGH SCOUTER", "MAX SCOUTER", "PERFECT PITCH", "WORLD-CLASS IDOL", "HONORS COLLEGE I", "HONORS COLLEGE II", "REBIRTH"};
        string[] paradeLocation = {"Left Wing, Main Color 3", "Right Wing, Main Color 3", "Left Wing, Outline 2", "Right Wing, Outline 2", "Left Sleeve, Outline 1", "Left Sleeve, Main Color 5", "Right Sleeve, Outline 1", "Right Sleeve, Main Color 5", "Chest Acc., Color 5"};
        string[] paradeHex = {"#5d6866", "#5d6866","#839390", "#839390", "#5d6866", "#839390","#5d6866", "#839390", "#839390"};
        string[] itemPackTrack1 = {"", " and Custom Itempack", "", " and Custom Itempack", "", " and Custom Itempack", " and Custom Itempack", " and Custom Itempack", " and Custom Itempack"};
        string[] itemPackTrack2 = {"", "\n   - You may change or add ONE item to your tracksuit.", "", "\n   - You may change or add ONE item to your tracksuit.", "", "\n   - You may change or add ONE item to your tracksuit.", "\n   - You may change or add ONE item to your tracksuit.", "\n   - You may change or add ONE item to your tracksuit.", "\n   - You may change or add ONE item to your tracksuit."};
        string[] itemPackIdol1 = {"Thin Scarf", "Neck Headphones", "Neck Ribbon", "Cravat", "Sleeveless Shirt", "Longer Sleeves", "Heart Headphones", "Flowy Sleeves", "Skirt / Pants Change, Chest Acc. Change,"};
        string[] itemPackIdol2 = {"Circle Headphones", "Chest Acc. Pattern Change", "No Headphones Perms", "Ear Acc. Change (Cannot Remove Them)", "Foreleg Pattern Change", "Spiked Collar", "Heart Front Socks", "No Mask Perms", "Hindleg Acc. Change"};
        
        SocketGuildUser assignee = (SocketGuildUser)command.User;
        SocketGuildUser assignedTo = null;
        int item = 0;
        int value = 0;

        foreach (var option in command.Data.Options)
        {
            switch (option.Name)
            {

                case "student":
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
                value = 1;
                await assignedTo.AddRoleAsync(1473371574710046840);
                break;
            case 2:
                value = 2;
                break;
            case 3:
                value = 3;
                await assignedTo.AddRoleAsync(1475898897174892769);
                break;
            case 4:
                value = 4;
                await assignedTo.AddRoleAsync(1475899025851945081);
                break;
            case 5:
                value = 5;
                await assignedTo.AddRoleAsync(1475899134337617980);
                break;
            case 6:
                value = 6;
                await assignedTo.AddRoleAsync(1475899268593225829);
                break;
            case 7:
                value = 7;
                await assignedTo.AddRoleAsync(1475961765433970880);
                break;
            case 8:
                value = 8;
                await assignedTo.AddRoleAsync(1475899269335744564);
                break;
            case 9:
                value = 9;
                await assignedTo.AddRoleAsync(1477926845184872531);
                break;
            default:
                await command.RespondAsync("Unrecognized command.", ephemeral: true);
                return;
        }

        List<EmbedBuilder> embeds = new List<EmbedBuilder>();
        
        embeds.Add(new EmbedBuilder()
            .WithAuthor("Dear Student, you have completed the . . .")
            .WithTitle(accompName[value - 1] + " ACCOMPLISHMENT!")
            .WithColor(0xBFA55F)
            .WithDescription(". . And have been awarded with the following :")
            .WithThumbnailUrl("https://64.media.tumblr.com/7d47f90161168afdb17720c3e645e120/b35d6053bfb9fc2a-03/s400x600/5ed0e321f4aeac2aa3542a520f1df49cbeff1752.pnj"));
        
        embeds.Add(new EmbedBuilder()
            .WithTitle("Wearable for Parade Dress!")
            .WithColor(0xBFA55F)
            .WithDescription("- Please place this code on your dress on the " + paradeLocation[value - 1] + ". \n- " + paradeHex[value - 1]));

        embeds.Add(new EmbedBuilder()
            .WithTitle("Custom Colorpack" + itemPackTrack1[value - 1] + " for Tracksuit!")
            .WithColor(0xBFA55F)
            .WithDescription("You may change *two* colors on your tracksuit." + itemPackTrack2[value - 1] + "\n\n✦ 181615 and 070a0c are the same code when recoloring.\n✦ 888375 and 6b6051 are the same code when recoloring.\n✦ Colors and outlines are treated as two separate values, so don't change a main color and it's outline and say that it is one color!"));

        embeds.Add(new EmbedBuilder()
            .WithTitle("New Itempack for Idol Outfit!")
            .WithColor(0xBFA55F)
            .WithDescription("You may use " + itemPackIdol1[value - 1] + " and " + itemPackIdol2[value - 1] + " rather than the required items!")
            .WithFooter("Please send your updated uniforms in the typical uniform checks."));
        
        await command.RespondAsync(text: "This message has been sent to both you and the other member.", ephemeral: true);

        foreach (var embed in embeds) {
            await UserExtensions.SendMessageAsync(assignedTo, "", false, embed.Build());
            await UserExtensions.SendMessageAsync(assignee, "", false, embed.Build());
        }
        
        var channel = client.GetChannel(1473209020285452360) as ITextChannel;
        
        await channel.SendMessageAsync("## Au__tomatic Messaging Syst__em . . \nPlease congratulate <@" + assignedTo.Id + "> in <#1473211757269876831> \nfor achieving the accomplishment **__" + accompName[value - 1]+ "__**!\n\n<@&1473370613992394864>");
    }
}