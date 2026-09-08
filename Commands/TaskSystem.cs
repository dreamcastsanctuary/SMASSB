using System.Globalization;
using Discord;
using Discord.WebSocket;
using SMASSB.Data;
using SMASSB.Models;
using SMASSB.ServiceHandlers;

namespace SMASSB.Commands;

public class TaskSystem {
    
    private readonly DiscordSocketClient _client;
    private readonly DatabaseService _db;
    private readonly ulong? _guildId;
    private readonly LogHandler _logHandler;
    
    public TaskSystem(DiscordSocketClient client, LogHandler logHandler, DatabaseService db, GuildConfiguration guildConfig) {
        
        _client = client;
        _logHandler = logHandler;
        _db = db;
        _guildId = guildConfig.GuildId;
    }
    
    public async Task HandleAssignTaskCommand(SocketSlashCommand command) {
        
        var assignee = _client.GetGuild((ulong)_guildId!).GetUser(command.User.Id);
        SocketGuildUser? assignedTo = null;
        var taskName = "";
        var description = "";
        var priority = "";
        string? deadlineStr = null;
        var deadline = new DateTime(9398, 12, 20);

        foreach (var option in command.Data.Options) {
            switch (option.Name) {
                
                case "assigned_to":
                    assignedTo = ((SocketGuildUser)option.Value);
                    break;
                case "task_name":
                    taskName = option.Value.ToString();
                    break;
                case "description":
                    description = option.Value.ToString();
                    break;
                case "priority":
                    priority = option.Value.ToString();
                    break;
                case "deadline":
                    deadlineStr = option.Value.ToString();
                    break;
                default:
                    await command.RespondAsync("Unrecognized command.", ephemeral: true);
                    break;
            }
        }

        if (deadlineStr != null) {
            if (!DateTime.TryParseExact(deadlineStr, "MM/dd/yyyy", null, DateTimeStyles.None, out deadline)) {
                await command.RespondAsync("That deadline isn't in the right format. Please use MM/DD/YYYY.", ephemeral: true);
                return;
            }
        }

        Enum.TryParse(priority, out PriorityType myPriority);

        if (assignedTo != null) {
            
            var embedBuilder = new EmbedBuilder()
                .WithAuthor((assignee.Nickname ?? assignee.Username) + " → " + (assignedTo.Nickname ?? assignedTo.Username))
                .WithTitle(taskName + "     " + myPriority.GetEnumDescription())
                .WithDescription(description)
                .WithFooter("Progress: TO-DO")
                .WithColor(myPriority.GetEnumColors());

            if (deadline.Year != 9398 && deadline < DateTime.Now) {
                await command.RespondAsync("That deadline is in the past!", ephemeral: true);
                return;
            } if (deadline.Year != 9398) {
                embedBuilder.WithFooter("Progress: TO-DO\nDeadline: " + deadline.DayOfWeek + ", " + deadline.ToString("MMMM") + " " + deadline.Day + ", " + deadline.Year);
            }
        
            await command.RespondAsync(text: "This message has been sent to both you and the other member: ",embed: embedBuilder.Build(), ephemeral: true);
            await assignedTo.SendMessageAsync("", false, embedBuilder.Build());
        } else return;

        if (taskName != null && description != null && priority != null)
            _db.CreateTask(assignedTo.Id.ToString(), assignee.Id.ToString(), DateTimeOffset.Now.ToString("MM/dd/yyyy"), deadline.ToString("MM/dd/yyyy"), taskName, description, priority, "TO-DO");
    }
    
    public async Task HandleRemoveTaskCommand(SocketSlashCommand command) {
        var assignee = _client.GetGuild((ulong)_guildId!).GetUser(command.User.Id);
        var taskName = "";

        foreach (var option in command.Data.Options) {
            switch (option.Name) {

                case "task_name":
                    taskName = option.Value.ToString();
                    break;
                default:
                    await command.RespondAsync("Unrecognized command.", ephemeral: true);
                    break;
            }
        }

        if (taskName != null && !assignee.Id.ToString().Equals(_db.GetAssigneeId(taskName))) {
            await command.RespondAsync(text: "You aren't the creator of that task.\nTrying to get your friend off the hook? : )", ephemeral: true);
            return;
        }

        _db.DeleteTask(taskName!, assignee.Id.ToString());
        await command.RespondAsync(text: "Task " + taskName + " has been deleted!", ephemeral: false);
    }
    
    public async Task HandleForceRemoveTaskCommand(SocketSlashCommand command) {
        var assignee = _client.GetGuild((ulong)_guildId!).GetUser(command.User.Id);
        var taskName = "";

        foreach (var option in command.Data.Options) {
            switch (option.Name) {

                case "task_name":
                    taskName = option.Value.ToString();
                    break;
                default:
                    await command.RespondAsync("Unrecognized command.", ephemeral: true);
                    break;
            }
        }

        _db.DeleteTask(taskName!, assignee.Id.ToString());
        await command.RespondAsync(text: "Task " + taskName + " has been deleted!", ephemeral: false);
    }
    
    public async Task HandleUpdateTaskCommand(SocketSlashCommand command) {
    
        var assignee = _client.GetGuild((ulong)_guildId!).GetUser(command.User.Id);
        var taskName = "";
        string? description = null;
        string? priority = null;
        string? deadlineStr = null;

        foreach (var option in command.Data.Options) {
            switch (option.Name) {
                case "task_name":
                    taskName = option.Value.ToString();
                    break;
                case "description":
                    description = option.Value.ToString();
                    break;
                case "priority":
                    priority = option.Value.ToString();
                    break;
                case "deadline":
                    deadlineStr = option.Value.ToString();
                    break;
            }
        }

        if (taskName != null && !assignee.Id.ToString().Equals(_db.GetAssigneeId(taskName))) {
            await command.RespondAsync("You aren't the creator of that task.\nTrying to get your friend off the hook? : )", ephemeral: true);
            return;
        }

        var existingDescription = description ?? _db.GetDescription(taskName!);
        var existingPriority     = priority ?? _db.GetPriority(taskName!);
        var existingDeadlineStr  = _db.GetDeadline(taskName!);

        DateTime deadline;
        if (deadlineStr != null) {
            if (!DateTime.TryParseExact(deadlineStr, "MM/dd/yyyy", null, DateTimeStyles.None, out deadline)) {
                await command.RespondAsync("That deadline isn't in the right format. Please use MM/DD/YYYY.", ephemeral: true);
                return;
            }
            if (deadline < DateTime.Now && deadline.Year != 9398) {
                await command.RespondAsync("That deadline is in the past!", ephemeral: true);
                return;
            }
        } else {
            deadline = DateTime.ParseExact(existingDeadlineStr, "MM/dd/yyyy", null);
        }

        if (taskName == null) return;
        
        _db.UpdateTask(taskName, assignee.Id.ToString(), deadline.ToString("MM/dd/yyyy"), existingDescription, existingPriority);

        Enum.TryParse(existingPriority, out PriorityType myPriority);

        var embedBuilder = new EmbedBuilder()
            .WithTitle(taskName + "     " + myPriority.GetEnumDescription())
            .WithDescription(existingDescription)
            .WithColor(myPriority.GetEnumColors());

        if (deadline.Year != 9398) {
            embedBuilder.WithFooter("Progress: " + _db.GetProgress(taskName) + "\nDeadline: " + deadline.DayOfWeek + ", " + deadline.ToString("MMMM") + " " + deadline.Day + ", " + deadline.Year);
        } else {
            embedBuilder.WithFooter("Progress: " + _db.GetProgress(taskName));
        }
        
        var user = _client.GetGuild((ulong)_guildId!).GetUser(ulong.Parse(_db.GetAssignedTo(taskName)));
        
        await command.RespondAsync(text: "Task updated!", embed: embedBuilder.Build(), ephemeral: true);
        if (user != null) {
            await user.SendMessageAsync("Your task has been updated!", embed: embedBuilder.Build());
        }
    }
    
    public async Task HandleForceUpdateTaskCommand(SocketSlashCommand command) {
    
        var assignee = _client.GetGuild((ulong)_guildId!).GetUser(command.User.Id);
        var taskName = "";
        string? description = null;
        string? priority = null;
        string? deadlineStr = null;

        foreach (var option in command.Data.Options) {
            switch (option.Name) {
                case "task_name":
                    taskName = option.Value.ToString();
                    break;
                case "description":
                    description = option.Value.ToString();
                    break;
                case "priority":
                    priority = option.Value.ToString();
                    break;
                case "deadline":
                    deadlineStr = option.Value.ToString();
                    break;
            }
        }

        if (taskName == null) return;

        var existingDescription = description ?? _db.GetDescription(taskName);
        var existingPriority     = priority ?? _db.GetPriority(taskName);
        var existingDeadlineStr  = _db.GetDeadline(taskName);

        DateTime deadline;
        if (deadlineStr != null) {
            if (!DateTime.TryParseExact(deadlineStr, "MM/dd/yyyy", null, DateTimeStyles.None, out deadline)) {
                await command.RespondAsync("That deadline isn't in the right format. Please use MM/DD/YYYY.", ephemeral: true);
                return;
            }
            if (deadline < DateTime.Now && deadline.Year != 9398) {
                await command.RespondAsync("That deadline is in the past!", ephemeral: true);
                return;
            }
        } else {
            deadline = DateTime.ParseExact(existingDeadlineStr, "MM/dd/yyyy", null);
        }

        _db.UpdateTask(taskName, assignee.Id.ToString(), deadline.ToString("MM/dd/yyyy"), existingDescription, existingPriority);

        Enum.TryParse(existingPriority, out PriorityType myPriority);

        var embedBuilder = new EmbedBuilder()
            .WithTitle(taskName + "     " + myPriority.GetEnumDescription())
            .WithDescription(existingDescription)
            .WithColor(myPriority.GetEnumColors());

        if (deadline.Year != 9398) {
            embedBuilder.WithFooter("Progress: " + _db.GetProgress(taskName) + "\nDeadline: " + deadline.DayOfWeek + ", " + deadline.ToString("MMMM") + " " + deadline.Day + ", " + deadline.Year);
        } else {
            embedBuilder.WithFooter("Progress: " + _db.GetProgress(taskName));
        }
        
        var user = _client.GetGuild((ulong)_guildId!).GetUser(ulong.Parse(_db.GetAssignedTo(taskName)));
        
        await command.RespondAsync(text: "Task updated!", embed: embedBuilder.Build(), ephemeral: true);
        if (user != null) {
            await user.SendMessageAsync("Your task has been updated!", embed: embedBuilder.Build());
        }
    }
    
    public async Task HandleUpdateProgressCommand(SocketSlashCommand command) {
        
        var taskName = "";
        var progress = "";
        
        foreach (var option in command.Data.Options) {
            switch (option.Name) {

                case "task_name":
                    taskName = option.Value.ToString();
                    break;
                case "progress":
                    progress = option.Value.ToString();
                    break;
                default:
                    await command.RespondAsync("Unrecognized command.", ephemeral: true);
                    break;
            }
        }

        if (taskName != null && progress != null) {
            var (taskFound, isNowCompleted) = _db.SetProgress(taskName, progress);

            if (!taskFound) {
                await command.RespondAsync($"No task found with the name **{taskName}**. Nothing was updated.",
                    ephemeral: true);
                return;
            }
            await command.RespondAsync("Updated progress for " + taskName + "!", ephemeral: true);
           
            if (isNowCompleted) {
                var assigner = _client.GetGuild((ulong)_guildId!).GetUser(ulong.Parse(_db.GetAssigneeId(taskName)));
                var assigned = _client.GetGuild((ulong)_guildId!).GetUser(ulong.Parse(_db.GetAssignedTo(taskName)));
                
                if (assigner != null && assigned != null) {
                    var assignedName = assigned.Nickname ?? assigned.Username;
                    await assigner.SendMessageAsync(assignedName + " has marked their task, " + taskName + ", completed!\nGo check in with them.");
                }
            }
        }
    }
    
    public async Task HandleViewTaskCommand(SocketSlashCommand command) {
        
        var taskName = command.Data.Options.FirstOrDefault(o => o.Name == "task_name")?.Value?.ToString();
        
        if (string.IsNullOrEmpty(taskName)) {
            await command.RespondAsync("Please provide a task name.", ephemeral: true);
            return;
        }

        var task = _db.ViewTask(taskName);

        if (task == null) {
            await command.RespondAsync($"No task found with the name **{taskName}**.", ephemeral: true);
            return;
        }

        Enum.TryParse(task.Priority, out PriorityType myPriority);

        var assigned = _client.GetGuild((ulong)_guildId!).GetUser(ulong.Parse(task.AssignedId)) as IUser;
        var assignee = _client.GetGuild((ulong)_guildId!).GetUser(ulong.Parse(task.AssigneeId)) as IUser;
        var deadline = DateTime.ParseExact(task.Deadline, "MM/dd/yyyy", null);
        
        var embedBuilder = new EmbedBuilder()
            .WithAuthor(assignee?.Username + " → " + assigned?.Username)
            .WithTitle(taskName + "     " + myPriority.GetEnumDescription())
            .WithDescription(task.Description ?? "No description.")
            .WithColor(myPriority.GetEnumColors());

        if (deadline.Year.Equals(9398)) {
            embedBuilder.WithFooter("Progress: " + task.Progress);
        } else {
            embedBuilder.WithFooter("Progress: " + task.Progress + "\nDeadline: " + deadline.DayOfWeek + ", " + deadline.ToString("MMMM") + " " + deadline.Day + ", " + deadline.Year);
        }

        await command.DeferAsync(ephemeral: true);
        await command.FollowupAsync(embed: embedBuilder.Build(), ephemeral: true);
    }
    
    public async Task HandleViewAllCommand(SocketSlashCommand command) {
        
        var tasks = _db.ViewAll(command.User.Id.ToString());

        if (tasks.Count == 0) {
            await command.RespondAsync("No tasks found.", ephemeral: true);
            return;
        }

        await command.DeferAsync(ephemeral: false);

        foreach (var task in tasks) {
            
            if (task == null) continue;
            
            Enum.TryParse(task.Priority, out PriorityType myPriority);

            var assigned = _client.GetGuild((ulong)_guildId!).GetUser(ulong.Parse(task.AssignedId)) as IUser;
            var assignee = _client.GetGuild((ulong)_guildId!).GetUser(ulong.Parse(task.AssigneeId)) as IUser;
            var deadline = DateTime.ParseExact(task.Deadline, "MM/dd/yyyy", null);

            var embedBuilder = new EmbedBuilder()
                .WithAuthor(assignee?.Username + " → " + assigned?.Username)
                .WithTitle(task.TaskName + "     " + myPriority.GetEnumDescription())
                .WithDescription(task.Description ?? "No description.")
                .WithColor(myPriority.GetEnumColors());

            if (deadline.Year.Equals(9398)) {
                embedBuilder.WithFooter("Progress: " + task.Progress);
            } else {
                embedBuilder.WithFooter("Progress: " + task.Progress + "\nDeadline: " + deadline.DayOfWeek + ", " + deadline.ToString("MMMM") + " " + deadline.Day + ", " + deadline.Year);
            }

            await command.FollowupAsync(embed: embedBuilder.Build(), ephemeral: false);
        }
    }

    public async Task HandleViewEveryoneCommand(SocketSlashCommand command) {
        
        var tasks = _db.ViewEveryone();

        if (tasks.Count == 0) {
            await command.RespondAsync("No tasks found.");
            return;
        }

        await command.DeferAsync(ephemeral: true);

        foreach (var task in tasks) {

            if (task == null) return;
            
            Enum.TryParse(task.Priority, out PriorityType myPriority);

            var assigned = _client.GetGuild((ulong)_guildId!).GetUser(ulong.Parse(task.AssignedId));
            var assignee = _client.GetGuild((ulong)_guildId!).GetUser(ulong.Parse(task.AssigneeId));
            var deadline = DateTime.ParseExact(task.Deadline, "MM/dd/yyyy", null);

            var embedBuilder = new EmbedBuilder()
                .WithAuthor(assignee?.Username + " → " + assigned?.Username)
                .WithTitle(task.TaskName + "     " + myPriority.GetEnumDescription())
                .WithDescription(task.Description ?? "No description.")
                .WithColor(myPriority.GetEnumColors());

            embedBuilder.WithFooter(deadline.Year.Equals(9398) ? $"Progress: {task.Progress}" : $"Progress: {task.Progress}\nDeadline: {deadline.DayOfWeek}, {deadline:MMMM} {deadline.Day}, {deadline.Year}");
            await command.FollowupAsync(embed: embedBuilder.Build(), ephemeral: true);
        }
    }
}