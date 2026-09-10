using Discord;
using Discord.WebSocket;
using SMASSB.Commands;
using SMASSB.Data;
using SMASSB.Models;

namespace SMASSB.ServiceHandlers;
public class CommandHandler {
    
    private readonly DiscordSocketClient _client;
    private readonly RewardSystem _rewardSystem;
    private readonly MeetingSystem _meetingSystem;
    private readonly RoleSystem _roleSystem;
    private readonly IdSystem _idSystem;
    private readonly PointSystem _pointSystem;
    private readonly GeneralSystem _generalSystem;
    private readonly CellSystem _cellSystem;
    private readonly ShopSystem _shopSystem;
    private readonly LogHandler _logHandler;
    private readonly TaskSystem _taskSystem;
    private readonly ulong? _guildId;

    public CommandHandler(DiscordSocketClient client,
                          LogHandler logHandler,
                          RewardSystem rewardSystem,
                          MeetingSystem meetingSystem,
                          RoleSystem roleSystem,
                          IdSystem idSystem,
                          PointSystem pointSystem,
                          GeneralSystem generalSystem,
                          CellSystem cellSystem,
                          ShopSystem shopSystem,
                          TaskSystem taskSystem,
                          GuildConfiguration guildConfig) {
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
        _logHandler = logHandler;
        _taskSystem = taskSystem;
        _guildId = guildConfig.GuildId;
    }
    
    /// <summary>
    /// Makes it so that all commands actually run asynchronously and don't make the important threads wait.
    /// </summary>
    private Task SlashCommandHandler(SocketSlashCommand command) {
        
        _ = Task.Run(async () => {
            try {
                await HandleSlashCommand(command);
            } catch (Exception ex) {
                var guild = _client.GetGuild((ulong)_guildId!);
                await _logHandler.LogExceptionWatch(guild.Id, exception: ex);
                if (command.HasResponded)
                    await command.FollowupAsync(ex.Message, ephemeral: true);
                else
                    await command.RespondAsync(ex.Message, ephemeral: true);
            }
        });
        return Task.CompletedTask;
    }
    
    /// <summary>
    /// This actually registers the commands.
    /// It makes a List of SlashCommandBuilders, then builds them all at the same time.
    /// </summary>
    public async Task RegisterCommands() {
        
        List<SlashCommandBuilder> commands = new List<SlashCommandBuilder>();
        var guild = _client.GetGuild((ulong)_guildId!);
        
        // REWARDSYSTEM
        
        commands.Add(new SlashCommandBuilder()
            .WithName("rewardko")
            .WithDescription("Rewards a kohosei their sword and headphones.")
            .AddOption("enlisted1", ApplicationCommandOptionType.User, "The @ of the enlisted.", isRequired: true).AddOption("enlisted2", ApplicationCommandOptionType.User, "The @ of the enlisted.", isRequired: false).AddOption("enlisted3", ApplicationCommandOptionType.User, "The @ of the enlisted.", isRequired: false).AddOption("enlisted4", ApplicationCommandOptionType.User, "The @ of the enlisted.", isRequired: false).AddOption("enlisted5", ApplicationCommandOptionType.User, "The @ of the enlisted.", isRequired: false).AddOption("enlisted6", ApplicationCommandOptionType.User, "The @ of the enlisted.", isRequired: false).AddOption("enlisted7", ApplicationCommandOptionType.User, "The @ of the enlisted.", isRequired: false).AddOption("enlisted8", ApplicationCommandOptionType.User, "The @ of the enlisted.", isRequired: false).AddOption("enlisted9", ApplicationCommandOptionType.User, "The @ of the enlisted.", isRequired: false).AddOption("enlisted10", ApplicationCommandOptionType.User, "The @ of the enlisted.", isRequired: false)
            .WithDefaultMemberPermissions(GuildPermission.ManageRoles));

        commands.Add(new SlashCommandBuilder()
            .WithName("rewardaccomp")
            .WithDescription("Gives a enlisted a specific award after achieving an accomplishment.")
            .AddOption("enlisted1", ApplicationCommandOptionType.User, "The @ of the enlisted.", isRequired: true).AddOption("enlisted2", ApplicationCommandOptionType.User, "The @ of the enlisted.", isRequired: false).AddOption("enlisted3", ApplicationCommandOptionType.User, "The @ of the enlisted.", isRequired: false).AddOption("enlisted4", ApplicationCommandOptionType.User, "The @ of the enlisted.", isRequired: false).AddOption("enlisted5", ApplicationCommandOptionType.User, "The @ of the enlisted.", isRequired: false).AddOption("enlisted6", ApplicationCommandOptionType.User, "The @ of the enlisted.", isRequired: false).AddOption("enlisted7", ApplicationCommandOptionType.User, "The @ of the enlisted.", isRequired: false).AddOption("enlisted8", ApplicationCommandOptionType.User, "The @ of the enlisted.", isRequired: false).AddOption("enlisted9", ApplicationCommandOptionType.User, "The @ of the enlisted.", isRequired: false).AddOption("enlisted10", ApplicationCommandOptionType.User, "The @ of the enlisted.", isRequired: false)
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
        
        var prOption = new SlashCommandOptionBuilder()
            .WithName("pr")
            .WithDescription("Create a PR meeting")
            .WithType(ApplicationCommandOptionType.SubCommand)
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("type")
                .WithDescription("What kind of PR meeting?")
                .WithType(ApplicationCommandOptionType.String)
                .WithRequired(true)
                .AddChoice("Partnering", "Partnering")
                .AddChoice("Blacklist", "Blacklist")
                .AddChoice("Other", "Other"))
            .AddOption("person", ApplicationCommandOptionType.User, "The person for the meeting", isRequired: true)
            .AddOption("meeting_name", ApplicationCommandOptionType.String, "Name for the meeting", isRequired: true);

        var reprimandOption = new SlashCommandOptionBuilder()
            .WithName("reprimand")
            .WithDescription("Create a reprimand meeting")
            .WithType(ApplicationCommandOptionType.SubCommand)
            .AddOption("person", ApplicationCommandOptionType.User, "The person for the meeting", isRequired: true)
            .AddOption("meeting_name", ApplicationCommandOptionType.String, "Name for the meeting", isRequired: true);

        var createOption = new SlashCommandOptionBuilder()
            .WithName("create")
            .WithDescription("Create a meeting")
            .WithType(ApplicationCommandOptionType.SubCommandGroup)
            .AddOption(prOption)
            .AddOption(reprimandOption);

        commands.Add(new SlashCommandBuilder()
            .WithName("meeting")
            .WithDescription("Creates or closes a private meeting room with PR / Kamikawa and the person provided.")
            .AddOption(createOption)
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("close")
                .WithDescription("Close the current meeting room")
                .WithType(ApplicationCommandOptionType.SubCommand))
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
            .WithName("debugenlist")
            .WithDescription("Force enlists a user.")
            .AddOption("civilian", ApplicationCommandOptionType.User, "The @ of the user.", isRequired: true)
            .AddOption("claim_name", ApplicationCommandOptionType.String, "The claim name of the civilian.", isRequired: true)
            .AddOption("rank_name", ApplicationCommandOptionType.Role, "The rank to be placed in the database.", isRequired: false)
            .AddOption("is_staff", ApplicationCommandOptionType.Boolean, "Are they a staff member, or an enlisted?", isRequired: true)
            .WithDefaultMemberPermissions(GuildPermission.Administrator));

        commands.Add(new SlashCommandBuilder()
            .WithName("debugunenlist")
            .WithDescription("Force removes a user.")
            .AddOption("civilian", ApplicationCommandOptionType.User, "The @ of the user.", isRequired: true)
            .WithDefaultMemberPermissions(GuildPermission.Administrator));
        
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
        
        var showOption = new SlashCommandOptionBuilder()
            .WithName("show")
            .WithDescription("Shows your Enlisted ID.")
            .WithType(ApplicationCommandOptionType.SubCommand);

        var bloodtypeOption = new SlashCommandOptionBuilder()
            .WithName("bloodtype")
            .WithDescription("The bloodtype of the member / character")
            .WithType(ApplicationCommandOptionType.String)
            .WithRequired(true)
            .AddChoice("O (Optimistic)", "O (Optimistic)")
            .AddChoice("A (Patient)", "A (Patient)")
            .AddChoice("B (Active)", "B (Active)")
            .AddChoice("AB (Rational)", "AB (Rational)");

        var idTypeOption = new SlashCommandOptionBuilder()
            .WithName("id_type")
            .WithDescription("The ID to display.")
            .WithType(ApplicationCommandOptionType.String)
            .WithRequired(true)
            .WithAutocomplete(true);

        var editOption = new SlashCommandOptionBuilder()
            .WithName("edit")
            .WithDescription("Edits your Enlisted ID, then displays it.")
            .WithType(ApplicationCommandOptionType.SubCommand)
            .AddOption("avatar_url", ApplicationCommandOptionType.String, "The profile of the member / character", isRequired: true)
            .AddOption(bloodtypeOption)
            .AddOption(idTypeOption);

        commands.Add(new SlashCommandBuilder()
            .WithName("id")
            .WithDescription("Shows or edits your Enlisted ID.")
            .AddOption(showOption)
            .AddOption(editOption)
            .WithDefaultMemberPermissions(GuildPermission.ManageRoles));
        
        commands.Add(new SlashCommandBuilder()
            .WithName("debugid")
            .WithDescription("Shows another member's Idol ID.")
            .AddOption("member", ApplicationCommandOptionType.User, "The member this applies to.", isRequired: true)
            .WithDefaultMemberPermissions(GuildPermission.ManageRoles));
        
        var idOption = new SlashCommandOptionBuilder()
            .WithName("id")
            .WithDescription("The ID to give.")
            .WithType(ApplicationCommandOptionType.String)
            .WithRequired(true);

        foreach (var name in Enum.GetNames<IdType>())
            idOption.AddChoice(name, name);
        
        var frameOption = new SlashCommandOptionBuilder()
            .WithName("frame")
            .WithDescription("The frame to give.")
            .WithType(ApplicationCommandOptionType.String)
            .WithRequired(true);

        foreach (var name in Enum.GetNames<FrameType>())
            frameOption.AddChoice(name, name);

        commands.Add(new SlashCommandBuilder()
            .WithName("addidaddons")
            .WithDescription("Give a member an ID addon.")
            .AddOption("member", ApplicationCommandOptionType.User, "The member the addon will go to.", isRequired: true)
            .AddOption(idOption)
            .AddOption(frameOption)
            .WithDefaultMemberPermissions(GuildPermission.ManageRoles)
        );
        
        commands.Add(new SlashCommandBuilder()
            .WithName("removeidaddons")
            .WithDescription("Remove an Addon from a member's ID.")
            .AddOption("member", ApplicationCommandOptionType.User, "The member.", isRequired: true)
            .AddOption(idOption)
            .AddOption(frameOption)
            .WithDefaultMemberPermissions(GuildPermission.ManageRoles)
        );
        
        commands.Add(new SlashCommandBuilder()
            .WithName("checkorchangeclaim")
            .WithDescription("Claim checking or claim changing. Pick your poison.")
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("check_claim")
                .WithDescription("Check if a name has been claimed yet.")
                .WithType(ApplicationCommandOptionType.SubCommand)
                .AddOption("claim_name", ApplicationCommandOptionType.String, "The name to check.", isRequired: true))
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("change_claim")
                .WithDescription("Change someone's claim name.")
                .WithType(ApplicationCommandOptionType.SubCommand)
                .AddOption("claim_name", ApplicationCommandOptionType.String, "The claim name.", isRequired: true)
                .AddOption("member", ApplicationCommandOptionType.User, "The @ of the user.", isRequired: true))
            .WithDefaultMemberPermissions(GuildPermission.ManageRoles));
        
        commands.Add(new SlashCommandBuilder()
            .WithName("debugfix")
            .WithDescription("Updates a user's DB entries.")
            .AddOption("member", ApplicationCommandOptionType.User, "The @ of the user.", isRequired: true)
            .AddOption("rank_name", ApplicationCommandOptionType.Role, "The rank to be placed in the database.", isRequired: false)
            .AddOption("avatar_fix", ApplicationCommandOptionType.Boolean, "Got a borked avatar in the database? No you don't.")
            .WithDefaultMemberPermissions(GuildPermission.ManageRoles));

        
        // POINTSYSTEM.
        
        commands.Add(new SlashCommandBuilder()
            .WithName("leaderboard")
            .WithDescription("Shows the point leaderboard.")
            .WithDefaultMemberPermissions(GuildPermission.ManageRoles));

        commands.Add(new SlashCommandBuilder()
            .WithName("addvalues")
            .WithDescription("Adds certain values to a member.")
            .AddOption("enlisted1", ApplicationCommandOptionType.User, "The @ of the enlisted.", isRequired: true).AddOption("enlisted2", ApplicationCommandOptionType.User, "The @ of the enlisted.", isRequired: false).AddOption("enlisted3", ApplicationCommandOptionType.User, "The @ of the enlisted.", isRequired: false).AddOption("enlisted4", ApplicationCommandOptionType.User, "The @ of the enlisted.", isRequired: false).AddOption("enlisted5", ApplicationCommandOptionType.User, "The @ of the enlisted.", isRequired: false).AddOption("enlisted6", ApplicationCommandOptionType.User, "The @ of the enlisted.", isRequired: false).AddOption("enlisted7", ApplicationCommandOptionType.User, "The @ of the enlisted.", isRequired: false).AddOption("enlisted8", ApplicationCommandOptionType.User, "The @ of the enlisted.", isRequired: false).AddOption("enlisted9", ApplicationCommandOptionType.User, "The @ of the enlisted.", isRequired: false).AddOption("enlisted10", ApplicationCommandOptionType.User, "The @ of the enlisted.", isRequired: false)
            .AddOption("points", ApplicationCommandOptionType.Integer, "The amount of points to add. (If applicable.)")
            .AddOption("recruitpoints", ApplicationCommandOptionType.Integer, "How many recruits did this person get? (If applicable.)")
            .AddOption("yen", ApplicationCommandOptionType.Integer, "How much yen did this person get? (If applicable.)")
            .WithDefaultMemberPermissions(GuildPermission.ManageRoles));
        
        commands.Add(new SlashCommandBuilder()
            .WithName("batchpoints")
            .WithDescription("Reads a message link full of 'Name pN rN' lines and applies points / recruits to matching members.")
            .AddOption("message_link", ApplicationCommandOptionType.String, "The link to the message with the point list.", isRequired: true)
            .WithDefaultMemberPermissions(GuildPermission.ManageRoles));
        
        commands.Add(new SlashCommandBuilder()
            .WithName("batchrecruits")
            .WithDescription("Parses the recruits channel and gives points to all. Can fail.")
            .WithDefaultMemberPermissions(GuildPermission.ManageRoles));
        
        commands.Add(new SlashCommandBuilder()
            .WithName("batchqotd")
            .WithDescription("Parses the qotd threads and gives points to all. Can fail.")
            .WithDefaultMemberPermissions(GuildPermission.ManageRoles));

        commands.Add(new SlashCommandBuilder()
            .WithName("removevalues")
            .WithDescription("Removes certain values from a member.")
            .AddOption("enlisted1", ApplicationCommandOptionType.User, "The @ of the enlisted.", isRequired: true).AddOption("enlisted2", ApplicationCommandOptionType.User, "The @ of the enlisted.", isRequired: false).AddOption("enlisted3", ApplicationCommandOptionType.User, "The @ of the enlisted.", isRequired: false).AddOption("enlisted4", ApplicationCommandOptionType.User, "The @ of the enlisted.", isRequired: false).AddOption("enlisted5", ApplicationCommandOptionType.User, "The @ of the enlisted.", isRequired: false).AddOption("enlisted6", ApplicationCommandOptionType.User, "The @ of the enlisted.", isRequired: false).AddOption("enlisted7", ApplicationCommandOptionType.User, "The @ of the enlisted.", isRequired: false).AddOption("enlisted8", ApplicationCommandOptionType.User, "The @ of the enlisted.", isRequired: false).AddOption("enlisted9", ApplicationCommandOptionType.User, "The @ of the enlisted.", isRequired: false).AddOption("enlisted10", ApplicationCommandOptionType.User, "The @ of the enlisted.", isRequired: false)
            .AddOption("points", ApplicationCommandOptionType.Integer, "The amount of points to remove. (If applicable.)")
            .AddOption("recruitpoints", ApplicationCommandOptionType.Integer, "How many recruits did this person get? (If applicable.)")
            .AddOption("yen", ApplicationCommandOptionType.Integer, "How much yen did this person get? (If applicable.)")
            .WithDefaultMemberPermissions(GuildPermission.ManageRoles));
        
        
        // GENERAL SYSTEM
        
        commands.Add(new SlashCommandBuilder()
            .WithName("purgemessages")
            .WithDescription("Deletes a specified number of messages from this channel.")
            .AddOption("amount", ApplicationCommandOptionType.Integer, "Number of messages to delete (1-100).", isRequired: true)
            .WithDefaultMemberPermissions(GuildPermission.ManageRoles));
        
        // CELLSYSTEM
        
        var editWorkCellOption = new SlashCommandOptionBuilder()
            .WithName("edit")
            .WithDescription("Edits your Work Cellphone.")
            .WithType(ApplicationCommandOptionType.SubCommand)
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
                .WithAutocomplete(true));

        var showWorkCellOption = new SlashCommandOptionBuilder()
            .WithName("show")
            .WithDescription("Shows your Work Cellphone.")
            .WithType(ApplicationCommandOptionType.SubCommand);

        commands.Add(new SlashCommandBuilder()
            .WithName("workcell")
            .WithDescription("Shows or edits your Work Cellphone.")
            .AddOption(showWorkCellOption)
            .AddOption(editWorkCellOption));
        
        var debugShowWorkCellOption = new SlashCommandOptionBuilder()
            .WithName("show")
            .WithDescription("Shows another member's Work Cellphone.")
            .WithType(ApplicationCommandOptionType.SubCommand)
            .AddOption("member", ApplicationCommandOptionType.User, "The member whose Work Cellphone to show.", isRequired: true);

        var debugEditWorkCellOption = new SlashCommandOptionBuilder()
            .WithName("edit")
            .WithDescription("Edits another member's Work Cellphone.")
            .WithType(ApplicationCommandOptionType.SubCommand)
            .AddOption("member", ApplicationCommandOptionType.User, "The member whose Work Cellphone to edit.", isRequired: true)
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
                .WithAutocomplete(true));

        commands.Add(new SlashCommandBuilder()
            .WithName("debugworkcell")
            .WithDescription("Shows or edits another member's Work Cellphone.")
            .AddOption(debugShowWorkCellOption)
            .AddOption(debugEditWorkCellOption)
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
            .WithName("addcelladdons")
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
            .WithName("shoppost")
            .WithDescription("Posts the current shop items.")
            .WithDefaultMemberPermissions(GuildPermission.Administrator)
        );
        
        // JIRA
        
        commands.Add(new SlashCommandBuilder()
            .WithName("assigntask")
            .WithDescription("Assigns a task to the given user.")
            .AddOption("assigned_to", ApplicationCommandOptionType.User, "The member receiving the task", isRequired: true)
            .AddOption("task_name", ApplicationCommandOptionType.String, "The name of the task", isRequired: true)
            .AddOption("description", ApplicationCommandOptionType.String, "The description of the task; the task itself", isRequired: true)
            .AddOption(new SlashCommandOptionBuilder()
                        .WithName("priority").WithDescription("The priority of the task").WithRequired(true)
                        .AddChoice("Lowest", 1).AddChoice("Low", 2).AddChoice("Medium", 3).AddChoice("High", 4).AddChoice("Highest", 5)
                        .WithType(ApplicationCommandOptionType.Integer))
            .AddOption("deadline", ApplicationCommandOptionType.String, "The set deadline; must be in the format \"MM/DD/YYYY\"", isRequired: false)
            .WithDefaultMemberPermissions(GuildPermission.ManageRoles));

        commands.Add(new SlashCommandBuilder()
            .WithName("removetask")
            .WithDescription("Removes the given task from the database.")
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("task_name").WithDescription("The name of the task").WithRequired(true)
                .WithAutocomplete(true)
                .WithType(ApplicationCommandOptionType.String))
            .WithDefaultMemberPermissions(GuildPermission.ManageRoles));

        commands.Add(new SlashCommandBuilder()
            .WithName("forceremovetask")
            .WithDescription("Removes the given task from the database.")
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("task_name").WithDescription("The name of the task").WithRequired(true)
                .WithAutocomplete(true)
                .WithType(ApplicationCommandOptionType.String))
            .WithDefaultMemberPermissions(GuildPermission.Administrator));
        
        commands.Add(new SlashCommandBuilder()
            .WithName("updatetask")
            .WithDescription("Updates the given task.")
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("task_name").WithDescription("The name of the task").WithRequired(true)
                .WithAutocomplete(true)
                .WithType(ApplicationCommandOptionType.String))
            .AddOption("description", ApplicationCommandOptionType.String, "The description of the task; the task itself")
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("priority").WithDescription("The priority of the task")
                .AddChoice("Lowest", 1).AddChoice("Low", 2).AddChoice("Medium", 3).AddChoice("High", 4).AddChoice("Highest", 5)
                .WithType(ApplicationCommandOptionType.Integer))
            .AddOption("deadline", ApplicationCommandOptionType.String, "The set deadline; must be in the format \"MM/DD/YYYY\"")
            .WithDefaultMemberPermissions(GuildPermission.ManageRoles));
            
        commands.Add(new SlashCommandBuilder()
            .WithName("forceupdatetask")
            .WithDescription("Updates the given task.")
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("task_name").WithDescription("The name of the task").WithRequired(true)
                .WithAutocomplete(true)
                .WithType(ApplicationCommandOptionType.String))
            .AddOption("description", ApplicationCommandOptionType.String, "The description of the task; the task itself")
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("priority").WithDescription("The priority of the task")
                .AddChoice("Lowest", 1).AddChoice("Low", 2).AddChoice("Medium", 3).AddChoice("High", 4).AddChoice("Highest", 5)
                .WithType(ApplicationCommandOptionType.Integer))
            .AddOption("deadline", ApplicationCommandOptionType.String, "The set deadline; must be in the format \"MM/DD/YYYY\"")
            .WithDefaultMemberPermissions(GuildPermission.Administrator));

        
        commands.Add(new SlashCommandBuilder()
            .WithName("viewtask")
            .WithDescription("Views a task.")
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("task_name").WithDescription("The name of the task").WithRequired(true)
                .WithAutocomplete(true)
                .WithType(ApplicationCommandOptionType.String))
            .WithDefaultMemberPermissions(GuildPermission.ManageRoles));

        commands.Add(new SlashCommandBuilder()
            .WithName("viewall")
            .WithDescription("Views all tasks.")
            .WithDefaultMemberPermissions(GuildPermission.ManageRoles));
        
        commands.Add(new SlashCommandBuilder()
            .WithName("vieweveryone")
            .WithDescription("Views everyone's tasks.")
            .WithDefaultMemberPermissions(GuildPermission.Administrator));
        
        commands.Add(new SlashCommandBuilder()
            .WithName("updatetaskprogress")
            .WithDescription("Updates the given task's progress.")
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("task_name").WithDescription("The name of the task").WithRequired(true)
                .WithAutocomplete(true)
                .WithType(ApplicationCommandOptionType.String))
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("progress").WithDescription("The progress of the task").WithRequired(true)
                .AddChoice("TO-DO", "TO-DO").AddChoice("IN PROGRESS", "IN PROGRESS").AddChoice("COMPLETED", "COMPLETED")
                .WithType(ApplicationCommandOptionType.String))
            .WithDefaultMemberPermissions(GuildPermission.ManageRoles));
        
        try {
            var builtCommands = commands.Select(c => (ApplicationCommandProperties)c.Build()).ToArray();
            await ((IGuild)guild).BulkOverwriteApplicationCommandsAsync(builtCommands);
            
        } catch (Exception ex) {
            Console.WriteLine($"Command registration failed: {ex}");
            await _logHandler.LogExceptionWatch(guild.Id, exception: ex, text: "Command registration failed.");
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
                await _rewardSystem.HandleRewardAccompCommand(command);
                break;
            
            case "meeting":
                await _meetingSystem.HandleMeetingCommand(command);
                break;
            
            case "preenlist":
                await _roleSystem.HandlePreEnlistCommand(command);
                break;
            case "enlist":
                await _roleSystem.HandleEnlistCommand(command);
                break;
            case "debugenlist":
                await _roleSystem.HandleForceEnlistCommand(command);
                break;
            case "debugunenlist":
                await _roleSystem.HandleForceRemoveCommand(command);
                break;
            case "checkpromotions":
                await _roleSystem.HandleCheckPromosCommand(command);
                break;
            case "duo":
                await _roleSystem.HandleDuoCommand(command);
                break;
            
            case "id":
                await _idSystem.HandleIdCommand(command);
                break;
            case "addidaddons":
                await _idSystem.EditAddons(command, true);
                break;
            case "removeidaddons":
                await _idSystem.EditAddons(command, false);
                break;
            case "debugfix":
                await _idSystem.HandleForceUpdateCommand(command);
                break;
            
            case "addvalues":
                await _pointSystem.EditValues(command, true);
                break;
            case "removevalues":
                await _pointSystem.EditValues(command, false);
                break;
            case "batchpoints":
                await _pointSystem.HandleBatchPoints(command);
                break;
            case "batchrecruits":
                await _pointSystem.HandleBatchRecruits(command);
                break;
            case "batchqotd":
                await _pointSystem.HandleBatchQotd(command);
                break;
            case "leaderboard":
                await _pointSystem.Leaderboard(command);
                break;
                
            case "purgemessages":
                await _generalSystem.HandleMassRemoveCommand(command);
                break;
            case "checkorchangeclaim":
                await _generalSystem.HandleCheckOrChangeClaimCommand(command);
                break;
            
            case "workcell":
                await _cellSystem.HandleWorkCellCommand(command);
                break;
            case "debugworkcell":
                await _cellSystem.HandleWorkCellCommand(command);
                break;
            case "addcelladdons":
                await _cellSystem.EditAddons(command, true);
                break;
            case "removecelladdons":
                await _cellSystem.EditAddons(command, false);
                break;
            
            case "shoppost":
                await _shopSystem.PostShopContents(command);
                break;
            
            case "assigntask":
                await _taskSystem.HandleAssignTaskCommand(command);
                break;
            case "removetask":
                await _taskSystem.HandleRemoveTaskCommand(command);
                break;
            case "forceremovetask":
                await _taskSystem.HandleForceRemoveTaskCommand(command);
                break;
            case "updatetask":
                await _taskSystem.HandleUpdateTaskCommand(command);
                break;
            case "forceupdatetask":
                await _taskSystem.HandleForceUpdateTaskCommand(command);
                break;
            case "viewtask":
                await _taskSystem.HandleViewTaskCommand(command);
                break;
            case "viewall":
                await _taskSystem.HandleViewAllCommand(command);
                break;
            case "vieweveryone":
                await _taskSystem.HandleViewEveryoneCommand(command);
                break;
            case "updatetaskprogress":
                await _taskSystem.HandleUpdateProgressCommand(command);
                break;
            
            default:
                await command.RespondAsync("Unrecognized command.", ephemeral: true);
                break;
        }
    }
}