using System.Globalization;
using Discord;
using Discord.WebSocket;
using SMASSB.Models;

namespace SMASSB.ServiceHandlers;


public class ReminderService {

    private const int SentinelYear = 9398;
    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(30);

    private const string StageNone = "NONE";
    private const string StageThreeDay = "THREE_DAY";
    private const string StageOneDay = "ONE_DAY";
    private const string StageDue = "DUE";
    private const string StageOverdue = "OVERDUE";

    private readonly DiscordSocketClient _client;
    private readonly DatabaseService _db;
    private readonly LogHandler _logHandler;
    private readonly ulong? _guildId;
    private Timer? _timer;

    public ReminderService(DiscordSocketClient client, LogHandler logHandler, DatabaseService db, GuildConfiguration guildConfig) {
        _client = client;
        _logHandler = logHandler;
        _db = db;
        _guildId = guildConfig.GuildId;
    }

    public void Start() {
        _timer ??= new Timer(async void (_) => await RunCheckSafely(), null, TimeSpan.Zero, CheckInterval);
    }

    private async Task RunCheckSafely() {
        try {
            await CheckDeadlines();
        } catch (Exception ex) {
            Console.WriteLine("[ReminderService] Error while checking deadlines: " + ex);
        }
    }

    private async Task CheckDeadlines() {

        var tasks = _db.GetActiveTasksWithDeadlines();
        var today = DateTime.Now.Date;
        var todayString = today.ToString("MM/dd/yyyy");

        foreach (var task in tasks) {

            if (!DateTime.TryParseExact(task.Deadline, "MM/dd/yyyy", null, DateTimeStyles.None, out var deadline))
                continue;

            if (deadline.Year == SentinelYear)
                continue;

            if (!ulong.TryParse(task.AssignedId, out var assignedUserId))
                continue;

            var assignedUser = _client.GetUser(assignedUserId);
            if (assignedUser == null)
                continue;

            var daysUntil = (deadline.Date - today).Days;
            var stage = string.IsNullOrEmpty(task.ReminderStage) ? StageNone : task.ReminderStage;

            if (daysUntil == 3 && stage == StageNone) {
                await SendDm(assignedUser, $"Reminder: your task **{task.TaskName}** is due in 3 days.");
                _db.UpdateReminderState(task.TaskName, StageThreeDay, task.LastOverdueReminderDate);
            }
            else if (daysUntil == 1 && (stage == StageNone || stage == StageThreeDay)) {
                await SendDm(assignedUser, $"Reminder: your task **{task.TaskName}** is due tomorrow!");
                _db.UpdateReminderState(task.TaskName, StageOneDay, task.LastOverdueReminderDate);
            }
            else if (daysUntil == 0 && stage != StageDue && stage != StageOverdue) {
                await SendDm(assignedUser, $"Your task **{task.TaskName}** is due **today**!");
                _db.UpdateReminderState(task.TaskName, StageDue, todayString);
            }
            else if (daysUntil < 0 && task.LastOverdueReminderDate != todayString) {
                var daysOverdue = Math.Abs(daysUntil);
                var dayWord = daysOverdue == 1 ? "day" : "days";
                await SendDm(assignedUser, $"Your task **{task.TaskName}** is now {daysOverdue} {dayWord} overdue! Get in that server and report!");
                _db.UpdateReminderState(task.TaskName, StageOverdue, todayString);
            }
        }
    } 

    private async Task SendDm(SocketUser user, string message) {
        try {
            await user.SendMessageAsync(message);
        } catch (Exception ex) {
            await _logHandler.LogExceptionWatch((ulong)_guildId!, exception: ex);
        }
    }
}