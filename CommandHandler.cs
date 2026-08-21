using Discord;
using Discord.WebSocket;
using SMASSB.Commands;
using SMASSB.Data;

namespace SMASSB;
public class CommandHandler {
    
    private readonly DiscordSocketClient _client;
    private RewardSystem _rewardSystem;
    private MeetingSystem _meetingSystem;
    private RoleSystem _roleSystem;
    private IdSystem _idSystem;
    private PointSystem _pointSystem;
    private GeneralSystem _generalSystem;
    private CellSystem _cellSystem;
    private ShopSystem _shopSystem;

    public CommandHandler(DiscordSocketClient client,
                          RewardSystem rewardSystem,
                          MeetingSystem meetingSystem,
                          RoleSystem roleSystem,
                          IdSystem idSystem,
                          PointSystem pointSystem,
                          GeneralSystem generalSystem,
                          CellSystem cellSystem,
                          ShopSystem shopSystem) {
        _client = client;
        _client.SlashCommandExecuted += SlashCommandHandler;
        _rewardSystem = rewardSystem;
        _meetingSystem = meetingSystem;
        _roleSystem = roleSystem;
        _idSystem = idSystem;
        _pointSystem = pointSystem;
        _generalSystem = generalSystem;
        _cellSystem = cellSystem;
        _shopSystem = shopSystem;
    }
    
    /// <summary>
    /// Makes it so that all commands actually run asynchronously and don't make the important threads wait.
    /// </summary>
    private Task SlashCommandHandler(SocketSlashCommand command) {
        
        _ = Task.Run(async () => {
            try {
                await HandleSlashCommand(command);
            } catch (Exception ex) {
                
                Console.WriteLine($"Unhandled exception in command '{command.Data.Name}': {ex}");
                try {
                    if (command.HasResponded)
                        await command.FollowupAsync(ex.Message, ephemeral: true);
                    else
                        await command.RespondAsync(ex.Message, ephemeral: true);
                } catch {
                }
            }
        });
        return Task.CompletedTask;
    }
    
    /// <summary>
    /// This actually registers the commands.
    /// It makes a List of SlashCommandBuilders, then builds them all at the same time.
    /// </summary>
    public async Task RegisterCommands(SocketGuild guild) {
        
        List<SlashCommandBuilder> commands = new List<SlashCommandBuilder>();
        
        
        // REWARDSYSTEM
        
        commands.Add(new SlashCommandBuilder()
            .WithName("rewardko")
            .WithDescription("Rewards a kohosei their sword and headphones.")
            .AddOption("enlisted1", ApplicationCommandOptionType.User, "The @ of the enlisted.", isRequired: true).AddOption("enlisted2", ApplicationCommandOptionType.User, "The @ of the enlisted.", isRequired: false).AddOption("enlisted3", ApplicationCommandOptionType.User, "The @ of the enlisted.", isRequired: false).AddOption("enlisted4", ApplicationCommandOptionType.User, "The @ of the enlisted.", isRequired: false).AddOption("enlisted5", ApplicationCommandOptionType.User, "The @ of the enlisted.", isRequired: false).AddOption("enlisted6", ApplicationCommandOptionType.User, "The @ of the enlisted.", isRequired: false).AddOption("enlisted7", ApplicationCommandOptionType.User, "The @ of the enlisted.", isRequired: false).AddOption("enlisted8", ApplicationCommandOptionType.User, "The @ of the enlisted.", isRequired: false).AddOption("enlisted9", ApplicationCommandOptionType.User, "The @ of the enlisted.", isRequired: false).AddOption("enlisted10", ApplicationCommandOptionType.User, "The @ of the enlisted.", isRequired: false)
            .WithDefaultMemberPermissions(GuildPermission.ManageRoles));

        commands.Add(new SlashCommandBuilder()
            .WithName("rewardaccomp")
            .WithDescription("Gives a enlisted a specific award after achieving an accomplishment.")
            .AddOption("enlisted", ApplicationCommandOptionType.User, "The name of the enlisted.", isRequired: true)
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("item").WithDescription("The specific item / document that is to be rewarded.")
                .WithRequired(true)
                .AddChoice("Transfer", 1).AddChoice("Supporter", 2).AddChoice("HighScouter", 3).AddChoice("MAXScouter",4)
                .AddChoice("PerfectPitch", 5).AddChoice("WorldClassIdol", 6)
                .AddChoice("RikugunBukoshoI", 7).AddChoice("RikugunBukoshoII", 8).AddChoice("Rebirth", 9)
                .AddChoice("ANutritiousBreakfast", 10).AddChoice("Stalemate", 11)
                .WithType(ApplicationCommandOptionType.Integer))
            .WithDefaultMemberPermissions(GuildPermission.ManageRoles));
        
        
        // MEETINGSYSTEM.
        
        commands.Add(new SlashCommandBuilder()
            .WithName("meetingpr")
            .WithDescription("Creates a private meeting room with our PR Officer and the person provided.")
            .AddOption("person", ApplicationCommandOptionType.User, "The @ of the person.", isRequired: true)
            .AddOption("meeting_name", ApplicationCommandOptionType.String, "What you want to call this meeting; add - instead of spaces.", isRequired: true)
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("type")
                .WithDescription("What kind of PR Meeting will this be?")
                .WithType(ApplicationCommandOptionType.String)
                .WithRequired(true)
                .AddChoice("Partnering", "Partnering").AddChoice("Blacklist", "Blacklist").AddChoice("Other", "Other"))
            .WithDefaultMemberPermissions(GuildPermission.ManageRoles));
        
        commands.Add(new SlashCommandBuilder()
            .WithName("meetingreprimand")
            .WithDescription("Creates a private meeting room with only the person provided, for use with discipline.")
            .AddOption("person", ApplicationCommandOptionType.User, "The @ of the person.", isRequired: true)
            .AddOption("meeting_name", ApplicationCommandOptionType.String, "What you want to call this meeting; add - instead of spaces.", isRequired: true)
            .WithDefaultMemberPermissions(GuildPermission.ManageRoles));
        
        commands.Add(new SlashCommandBuilder()
            .WithName("meetingclose")
            .WithDescription("Closes the current thread if it is a meeting room.")
            .WithDefaultMemberPermissions(GuildPermission.ManageRoles));
        
        
        // ROLESYSTEM.
        
        commands.Add(new SlashCommandBuilder()
            .WithName("preenlist")
            .WithDescription("Pre-enlists a civilian into a prospect; to be used during in-server uniform check.")
            .AddOption("civilian", ApplicationCommandOptionType.User, "The @ of the civilian.", isRequired: true)
            .AddOption("claim_name", ApplicationCommandOptionType.String, "The claim name of the civilian.", isRequired: true)
            .WithDefaultMemberPermissions(GuildPermission.ManageRoles));
        
        commands.Add(new SlashCommandBuilder()
            .WithName("enlist")
            .WithDescription("Enlists a kōhosei into a enlisted.")
            .AddOption("kōhosei", ApplicationCommandOptionType.User, "The @ of the enlisted.", isRequired: true)
            .WithDefaultMemberPermissions(GuildPermission.ManageRoles));
        
        commands.Add(new SlashCommandBuilder()
            .WithName("forceenlist")
            .WithDescription("Force enlists a user.")
            .AddOption("civilian", ApplicationCommandOptionType.User, "The @ of the user.", isRequired: true)
            .AddOption("claim_name", ApplicationCommandOptionType.String, "The claim name of the civilian.", isRequired: true)
            .AddOption("rank_name", ApplicationCommandOptionType.String, "The rank to be placed in the database.", isRequired: true)
            .AddOption("is_staff", ApplicationCommandOptionType.Boolean, "Are they a staff member, or an enlisted?", isRequired: true)
            .WithDefaultMemberPermissions(GuildPermission.Administrator));

        commands.Add(new SlashCommandBuilder()
            .WithName("forceremove")
            .WithDescription("Force removes a user.")
            .AddOption("civilian", ApplicationCommandOptionType.User, "The @ of the user.", isRequired: true)
            .WithDefaultMemberPermissions(GuildPermission.Administrator));
        
        commands.Add(new SlashCommandBuilder()
            .WithName("addrecruits")
            .WithDescription("Add to a member's recruit counter.")
            .AddOption("member", ApplicationCommandOptionType.User, "The member this applies to.", isRequired: true)
            .AddOption("recruitpoints", ApplicationCommandOptionType.Integer, "How many recruits did this person get? (If applicable.)", isRequired: true)
            .WithDefaultMemberPermissions(GuildPermission.ManageRoles));
        
        commands.Add(new SlashCommandBuilder()
            .WithName("removerecruits")
            .WithDescription("Subtract from a member's recruit counter.")
            .AddOption("member", ApplicationCommandOptionType.User, "The member this applies to.", isRequired: true)
            .AddOption("recruitpoints", ApplicationCommandOptionType.Integer, "Messed up with the count? www", isRequired: true)
            .WithDefaultMemberPermissions(GuildPermission.ManageRoles));
        
        commands.Add(new SlashCommandBuilder()
            .WithName("duo")
            .WithDescription("Gives two people the Duo Group role.")
            .AddOption("member1", ApplicationCommandOptionType.User, "The first member this applies to.", isRequired: true)
            .AddOption("member2", ApplicationCommandOptionType.User, "The second member this applies to.", isRequired: true)
            .WithDefaultMemberPermissions(GuildPermission.ManageRoles));
        
        commands.Add(new SlashCommandBuilder()
            .WithName("checkpromotions")
            .WithDescription("Checks if we have any promotions.")
            .AddOption("auto_promote", ApplicationCommandOptionType.Boolean, "Automatically promote everyone here to the next rank.", isRequired: true)
            .WithDefaultMemberPermissions(GuildPermission.ManageRoles));
        
        
        // IDSYSTEM.
        
        commands.Add(new SlashCommandBuilder()
            .WithName("showid")
            .WithDescription("Shows your Idol ID."));
        
        commands.Add(new SlashCommandBuilder()
            .WithName("showotherid")
            .WithDescription("Shows another member's Idol ID.")
            .AddOption("member", ApplicationCommandOptionType.User, "The member this applies to.", isRequired: true)
            .WithDefaultMemberPermissions(GuildPermission.ManageRoles));

        commands.Add(new SlashCommandBuilder()
            .WithName("editid")
            .WithDescription("Edits your Idol ID and displays it.")
            .AddOption("avatar_url", ApplicationCommandOptionType.String, "The profile of the member / character.", isRequired: false)
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("bloodtype")
                .WithDescription("The bloodtype of the member / character")
                .WithRequired(false)
                .AddChoice("O (Optimistic)", "O (Optimistic)").AddChoice("A (Patient)", "A (Patient)").AddChoice("B (Active)", "B (Active)").AddChoice("AB (Rational)", "AB (Rational)")
                .WithType(ApplicationCommandOptionType.String))
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("id_type")
                .WithDescription("The ID to display.")
                .WithRequired(false)
                .WithType(ApplicationCommandOptionType.String)
                .WithAutocomplete(true)));
        
        var idOption = new SlashCommandOptionBuilder()
            .WithName("id")
            .WithDescription("The ID to give.")
            .WithType(ApplicationCommandOptionType.String)
            .WithRequired(true);

        foreach (var name in Enum.GetNames<IdType>())
            idOption.AddChoice(name, name);

        commands.Add(new SlashCommandBuilder()
            .WithName("giveidskin")
            .WithDescription("Give a member an ID skin.")
            .AddOption("member", ApplicationCommandOptionType.User, "The member the ID will go to.", isRequired: true)
            .AddOption(idOption)
            .WithDefaultMemberPermissions(GuildPermission.ManageRoles)
        );
        
        commands.Add(new SlashCommandBuilder()
            .WithName("removeidskin")
            .WithDescription("Remove a member's ID skin.")
            .AddOption("member", ApplicationCommandOptionType.User, "The member.", isRequired: true)
            .AddOption(idOption)
            .WithDefaultMemberPermissions(GuildPermission.ManageRoles)
        );
        
        var frameOption = new SlashCommandOptionBuilder()
            .WithName("frame")
            .WithDescription("The frame to give.")
            .WithType(ApplicationCommandOptionType.String)
            .WithRequired(true);

        foreach (var name in Enum.GetNames<FrameType>())
            frameOption.AddChoice(name, name);
        
        commands.Add(new SlashCommandBuilder()
            .WithName("giveidframe")
            .WithDescription("Give a member an ID frame.")
            .AddOption("member", ApplicationCommandOptionType.User, "The member the frame will go to.", isRequired: true)
            .AddOption(frameOption)
            .WithDefaultMemberPermissions(GuildPermission.ManageRoles)
        );
        
        commands.Add(new SlashCommandBuilder()
            .WithName("removeidframe")
            .WithDescription("Remove a member's ID frame.")
            .AddOption("member", ApplicationCommandOptionType.User, "The member.", isRequired: true)
            .AddOption(frameOption)
            .WithDefaultMemberPermissions(GuildPermission.ManageRoles)
        );
        
        commands.Add(new SlashCommandBuilder()
            .WithName("changeclaim")
            .WithDescription("Updates a user's DB entries. Use when changing someone's claim or for ID fixes.")
            .AddOption("member", ApplicationCommandOptionType.User, "The @ of the user.", isRequired: true)
            .AddOption("claim_name", ApplicationCommandOptionType.String, "The claim name of the user.", isRequired: false)
            .AddOption("rank_name", ApplicationCommandOptionType.Role, "The rank to be placed in the database.", isRequired: false)
            .AddOption("avatar_fix", ApplicationCommandOptionType.Boolean, "Got a borked avatar in the database? No you don't.")
            .WithDefaultMemberPermissions(GuildPermission.ManageRoles));

        
        // POINTSYSTEM.
        
        commands.Add(new SlashCommandBuilder()
            .WithName("showpoints")
            .WithDescription("Shows the points of a member.")
            .AddOption("member", ApplicationCommandOptionType.User, "The aforementioned member.", isRequired: false));

        commands.Add(new SlashCommandBuilder()
            .WithName("leaderboard")
            .WithDescription("Shows the point leaderboard.")
            .WithDefaultMemberPermissions(GuildPermission.ManageRoles));

        commands.Add(new SlashCommandBuilder()
            .WithName("addpoints")
            .WithDescription("Adds points to a member.")
            .AddOption("enlisted1", ApplicationCommandOptionType.User, "The @ of the enlisted.", isRequired: true).AddOption("enlisted2", ApplicationCommandOptionType.User, "The @ of the enlisted.", isRequired: false).AddOption("enlisted3", ApplicationCommandOptionType.User, "The @ of the enlisted.", isRequired: false).AddOption("enlisted4", ApplicationCommandOptionType.User, "The @ of the enlisted.", isRequired: false).AddOption("enlisted5", ApplicationCommandOptionType.User, "The @ of the enlisted.", isRequired: false).AddOption("enlisted6", ApplicationCommandOptionType.User, "The @ of the enlisted.", isRequired: false).AddOption("enlisted7", ApplicationCommandOptionType.User, "The @ of the enlisted.", isRequired: false).AddOption("enlisted8", ApplicationCommandOptionType.User, "The @ of the enlisted.", isRequired: false).AddOption("enlisted9", ApplicationCommandOptionType.User, "The @ of the enlisted.", isRequired: false).AddOption("enlisted10", ApplicationCommandOptionType.User, "The @ of the enlisted.", isRequired: false)
            .AddOption("amount", ApplicationCommandOptionType.Integer, "The amount of points to add.", isRequired: true)
            .AddOption("recruitpoints", ApplicationCommandOptionType.Integer, "How many recruits did this person get? (If applicable.)")
            .AddOption("currency", ApplicationCommandOptionType.Integer, "How many star pieces did this person get? (If applicable.)")
            .WithDefaultMemberPermissions(GuildPermission.ManageRoles));
        
        commands.Add(new SlashCommandBuilder()
            .WithName("addbatchpoints")
            .WithDescription("Reads a message link full of 'Name pN rN' lines and applies points / recruits to matching members.")
            .AddOption("message_link", ApplicationCommandOptionType.String, "The link to the message with the point list.", isRequired: true)
            .WithDefaultMemberPermissions(GuildPermission.ManageRoles));
        
        commands.Add(new SlashCommandBuilder()
            .WithName("parsebatchrecruits")
            .WithDescription("Parses the recruits channel and gives points to all. Can fail.")
            .WithDefaultMemberPermissions(GuildPermission.ManageRoles));

        commands.Add(new SlashCommandBuilder()
            .WithName("removepoints")
            .WithDescription("Removes points from a member.")
            .AddOption("enlisted1", ApplicationCommandOptionType.User, "The @ of the enlisted.", isRequired: true).AddOption("enlisted2", ApplicationCommandOptionType.User, "The @ of the enlisted.", isRequired: false).AddOption("enlisted3", ApplicationCommandOptionType.User, "The @ of the enlisted.", isRequired: false).AddOption("enlisted4", ApplicationCommandOptionType.User, "The @ of the enlisted.", isRequired: false).AddOption("enlisted5", ApplicationCommandOptionType.User, "The @ of the enlisted.", isRequired: false).AddOption("enlisted6", ApplicationCommandOptionType.User, "The @ of the enlisted.", isRequired: false).AddOption("enlisted7", ApplicationCommandOptionType.User, "The @ of the enlisted.", isRequired: false).AddOption("enlisted8", ApplicationCommandOptionType.User, "The @ of the enlisted.", isRequired: false).AddOption("enlisted9", ApplicationCommandOptionType.User, "The @ of the enlisted.", isRequired: false).AddOption("enlisted10", ApplicationCommandOptionType.User, "The @ of the enlisted.", isRequired: false)
            .AddOption("amount", ApplicationCommandOptionType.Integer, "The amount of points to remove.", isRequired: true)
            .WithDefaultMemberPermissions(GuildPermission.ManageRoles));
        
        
        // GENERAL SYSTEM
        
        commands.Add(new SlashCommandBuilder()
            .WithName("purgemessages")
            .WithDescription("Deletes a specified number of messages from this channel.")
            .AddOption("amount", ApplicationCommandOptionType.Integer, "Number of messages to delete (1-100).", isRequired: true)
            .WithDefaultMemberPermissions(GuildPermission.ManageRoles));
        
        commands.Add(new SlashCommandBuilder()
            .WithName("checkclaimed")
            .WithDescription("Checks if inputted name has been claimed before.")
            .AddOption("name", ApplicationCommandOptionType.String, "The name to check.", isRequired:true)
            .WithDefaultMemberPermissions(GuildPermission.ManageRoles));
        
        commands.Add(new SlashCommandBuilder()
            .WithName("postlore")
            .WithDescription("Posts... posts lore.")
            .WithDefaultMemberPermissions(GuildPermission.Administrator));
        
        // CELLSYSTEM
        
        commands.Add(new SlashCommandBuilder()
            .WithName("showworkcell")
            .WithDescription("Shows your Work Cellphone."));
        
        commands.Add(new SlashCommandBuilder()
            .WithName("editworkcell")
            .WithDescription("Edits your Work Cellphone.")
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("add_apps")
                .WithDescription("Any apps need adding?")
                .WithRequired(false)
                .WithType(ApplicationCommandOptionType.String)
                .WithAutocomplete(true))
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("remove_apps")
                .WithDescription("Any apps need removing?")
                .WithRequired(false)
                .WithType(ApplicationCommandOptionType.String)
                .WithAutocomplete(true))
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("case_type")
                .WithDescription("The Case to display.")
                .WithRequired(false)
                .WithType(ApplicationCommandOptionType.String)
                .WithAutocomplete(true))
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("charm_type")
                .WithDescription("The Charm to display.")
                .WithRequired(false)
                .WithType(ApplicationCommandOptionType.String)
                .WithAutocomplete(true))
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("wallpaper_type")
                .WithDescription("The Wallpaper to display.")
                .WithRequired(false)
                .WithType(ApplicationCommandOptionType.String)
                .WithAutocomplete(true)));
        
        commands.Add(new SlashCommandBuilder()
            .WithName("showotherworkcell")
            .WithDescription("Shows another member's Work Cellphone.")
            .AddOption("member", ApplicationCommandOptionType.User, "The member this applies to.", isRequired: true)
            .WithDefaultMemberPermissions(GuildPermission.ManageRoles));
        
        commands.Add(new SlashCommandBuilder()
            .WithName("addyen")
            .WithDescription("Adds yen to a member.")
            .AddOption("enlisted1", ApplicationCommandOptionType.User, "The @ of the enlisted.", isRequired: true).AddOption("enlisted2", ApplicationCommandOptionType.User, "The @ of the enlisted.", isRequired: false).AddOption("enlisted3", ApplicationCommandOptionType.User, "The @ of the enlisted.", isRequired: false).AddOption("enlisted4", ApplicationCommandOptionType.User, "The @ of the enlisted.", isRequired: false).AddOption("enlisted5", ApplicationCommandOptionType.User, "The @ of the enlisted.", isRequired: false).AddOption("enlisted6", ApplicationCommandOptionType.User, "The @ of the enlisted.", isRequired: false).AddOption("enlisted7", ApplicationCommandOptionType.User, "The @ of the enlisted.", isRequired: false).AddOption("enlisted8", ApplicationCommandOptionType.User, "The @ of the enlisted.", isRequired: false).AddOption("enlisted9", ApplicationCommandOptionType.User, "The @ of the enlisted.", isRequired: false).AddOption("enlisted10", ApplicationCommandOptionType.User, "The @ of the enlisted.", isRequired: false)
            .AddOption("amount", ApplicationCommandOptionType.Integer, "The amount of points to add.", isRequired: true)
            .WithDefaultMemberPermissions(GuildPermission.ManageRoles));
        
        commands.Add(new SlashCommandBuilder()
            .WithName("removeyen")
            .WithDescription("Removes yen from a member.")
            .AddOption("enlisted1", ApplicationCommandOptionType.User, "The @ of the enlisted.", isRequired: true).AddOption("enlisted2", ApplicationCommandOptionType.User, "The @ of the enlisted.", isRequired: false).AddOption("enlisted3", ApplicationCommandOptionType.User, "The @ of the enlisted.", isRequired: false).AddOption("enlisted4", ApplicationCommandOptionType.User, "The @ of the enlisted.", isRequired: false).AddOption("enlisted5", ApplicationCommandOptionType.User, "The @ of the enlisted.", isRequired: false).AddOption("enlisted6", ApplicationCommandOptionType.User, "The @ of the enlisted.", isRequired: false).AddOption("enlisted7", ApplicationCommandOptionType.User, "The @ of the enlisted.", isRequired: false).AddOption("enlisted8", ApplicationCommandOptionType.User, "The @ of the enlisted.", isRequired: false).AddOption("enlisted9", ApplicationCommandOptionType.User, "The @ of the enlisted.", isRequired: false).AddOption("enlisted10", ApplicationCommandOptionType.User, "The @ of the enlisted.", isRequired: false)
            .AddOption("amount", ApplicationCommandOptionType.Integer, "The amount of points to remove.", isRequired: true)
            .WithDefaultMemberPermissions(GuildPermission.ManageRoles));
        
        var appOption = new SlashCommandOptionBuilder()
            .WithName("app")
            .WithDescription("The App to give.")
            .WithType(ApplicationCommandOptionType.String);

        foreach (var name in Enum.GetNames<AppType>())
            appOption.AddChoice(name, name);
        
        var caseOption = new SlashCommandOptionBuilder()
            .WithName("case")
            .WithDescription("The Case to give.")
            .WithType(ApplicationCommandOptionType.String);

        foreach (var name in Enum.GetNames<CaseType>())
            caseOption.AddChoice(name, name);
        
        var charmOption = new SlashCommandOptionBuilder()
            .WithName("charm")
            .WithDescription("The Charm to give.")
            .WithType(ApplicationCommandOptionType.String);

        foreach (var name in Enum.GetNames<CharmType>())
            charmOption.AddChoice(name, name);

        var wallpaperOption = new SlashCommandOptionBuilder()
            .WithName("wallpaper")
            .WithDescription("The Wallpaper to give.")
            .WithType(ApplicationCommandOptionType.String);

        foreach (var name in Enum.GetNames<WallpaperType>())
            wallpaperOption.AddChoice(name, name);

        commands.Add(new SlashCommandBuilder()
            .WithName("givecelladdons")
            .WithDescription("Give a member a WorkCell addon.")
            .AddOption("member", ApplicationCommandOptionType.User, "The member the addon will go to.", isRequired: true)
            .AddOption(appOption)
            .AddOption(caseOption)
            .AddOption(wallpaperOption)
            .AddOption(charmOption)
            .WithDefaultMemberPermissions(GuildPermission.ManageRoles)
        );
        
        commands.Add(new SlashCommandBuilder()
            .WithName("removecelladdons")
            .WithDescription("Remove an app from a member's WorkCell.")
            .AddOption("member", ApplicationCommandOptionType.User, "The member.", isRequired: true)
            .AddOption(appOption)
            .AddOption(caseOption)
            .AddOption(wallpaperOption)
            .AddOption(charmOption)
            .WithDefaultMemberPermissions(GuildPermission.ManageRoles)
        );

        // SHOPSYSTEM
        
        commands.Add(new SlashCommandBuilder()
            .WithName("initweeklybaselines")
            .WithDescription("One-time setup: seeds everyone's weekly earnings baseline.")
            .WithDefaultMemberPermissions(GuildPermission.Administrator));

        commands.Add(new SlashCommandBuilder()
            .WithName("shoppost")
            .WithDescription("Posts the current shop items.")
            .WithDefaultMemberPermissions(GuildPermission.Administrator)
        );
        
        try {
            var builtCommands = commands.Select(c => (ApplicationCommandProperties)c.Build()).ToArray();
            await ((IGuild)guild).BulkOverwriteApplicationCommandsAsync(builtCommands);
            
        } catch (Exception ex) {
            Console.WriteLine($"Command registration failed: {ex}");
        }
    }
    
    /// <summary>
    /// Handles every single slash command that is run.
    /// </summary>
    private async Task HandleSlashCommand(SocketSlashCommand command) {
        
        switch(command.Data.Name) {
            
            case "rewardko":
                await _rewardSystem.HandleRewardKoCommand(command);
                break;
            case "rewardaccomp":
                await _rewardSystem.HandleRewardAccompCommand(command, _client);
                break;
            
            case "meetingpr":
                await _meetingSystem.HandleMeetingPRCommand(command, _client);
                break;
            case "meetingreprimand":
                await _meetingSystem.HandleMeetingReprimandCommand(command, _client);
                break;
            case "meetingclose":
                await _meetingSystem.HandleMeetingCloseCommand(command, _client);
                break;
            
            case "preenlist":
                await _roleSystem.HandlePreEnlistCommand(command);
                break;
            case "enlist":
                await _roleSystem.HandleEnlistCommand(command, _client);
                break;
            case "forceenlist":
                await _roleSystem.HandleForceEnlistCommand(command);
                break;
            case "forceremove":
                await _roleSystem.HandleForceRemoveCommand(command);
                break;
            case "checkpromotions":
                await _roleSystem.HandleCheckPromosCommand(command, _client);
                break;
            case "duo":
                await _roleSystem.HandleDuoCommand(command);
                break;
            
            case "showid":
                await _idSystem.ShowId(command, _client);
                break;
            case "showotherid":
                await _idSystem.ShowId(command, _client);
                break;
            case "editid":
                await _idSystem.EditId(command, _client);
                break;
            case "giveidskin":
                await _idSystem.GainId(command, _client);
                break;
            case "removeidskin":
                await _idSystem.RemoveId(command, _client);
                break;
            case "changeclaim":
                await _idSystem.HandleForceUpdateCommand(command);
                break;
            
            case "showpoints":
                await _pointSystem.ShowPoints(command);
                break;
            case "addpoints":
                await _pointSystem.EditPoints(command, true);
                break;
            case "removepoints":
                await _pointSystem.EditPoints(command, false);
                break;
            case "addbatchpoints":
                await _pointSystem.HandleBatchPoints(command, _client);
                break;
            case "leaderboard":
                await _pointSystem.Leaderboard(command);
                break;
            case "addrecruits":
                await _pointSystem.EditRecruits(command, true);
                break;
            case "removerecruits":
                await _pointSystem.EditRecruits(command, false);
                break;
            case "parsebatchrecruits":
                await _pointSystem.HandleBatchRecruits(command);
                break;
                
            case "purgemessages":
                await _generalSystem.HandleMassRemoveCommand(command);
                break;
            case "checkclaimed":
                await _generalSystem.HandleCheckClaimedCommand(command);
                break;
            case "parsenontrained":
                await _generalSystem.HandleParseNonTrainedKohoseiCommand(command, _client);
                break;
            case "postlore":
                await _generalSystem.PostLore(command);
                break;
            
            case "showworkcell":
                await _cellSystem.ShowWorkCell(command);
                break;
            case "editworkcell":
                await _cellSystem.EditWorkCell(command);
                break;
            case "showotherworkcell":
                await _cellSystem.ShowWorkCell(command);
                break;
            case "addyen":
                await _cellSystem.EditYen(command, true);
                break;
            case "removeyen":
                await _cellSystem.EditYen(command, false);
                break;
            case "givecelladdons":
                await _cellSystem.EditAddons(command, true);
                break;
            case "removecelladdons":
                await _cellSystem.EditAddons(command, false);
                break;
            
            case "shoppost":
                await _shopSystem.PostShopContents(command);
                break;
            
            default:
                await command.RespondAsync("Unrecognized command.", ephemeral: true);
                break;
        }
    }
}