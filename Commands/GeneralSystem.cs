using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace SMASSB.Commands;

public class GeneralSystem {
    
    private LogHandler _logHandler;
    
    public GeneralSystem(LogHandler logHandler) {
        _logHandler = logHandler;
    }

    [DefaultMemberPermissions(GuildPermission.ManageMessages)]
    public async Task HandleMassRemoveCommand(SocketSlashCommand command) {
        
        await command.RespondAsync("Purging messages.", ephemeral: true);
        var channel = command.Channel as ITextChannel;
        int amount = 0;

        foreach (var option in command.Data.Options) {
            switch (option.Name) {
                case "amount":
                    amount = Convert.ToInt32(option.Value);
                    break;
                default:
                    await command.RespondAsync("Unrecognized option.", ephemeral: true);
                    return;
            }
        }

        if (amount < 1 || amount > 100) {
            await command.RespondAsync("Please provide a number between 1 and 100.", ephemeral: true);
            return;
        }

        if (channel == null) {
            await command.RespondAsync("This command can only be used in a text channel.", ephemeral: true);
            return;
        }

        var messages = await channel.GetMessagesAsync(amount).FlattenAsync();
        var validMessages = messages.Where(m => DateTimeOffset.UtcNow - m.Timestamp < TimeSpan.FromDays(14)).ToList();
        var tooOld = messages.Count() - validMessages.Count;

        if (!validMessages.Any()) {
            await command.ModifyOriginalResponseAsync(m => m.Content = "No deletable messages found. Messages older than 14 days cannot be bulk deleted.");
            return;
        }

        await channel.DeleteMessagesAsync(validMessages);

        string response = $"Deleted {validMessages.Count} message(s).";
        if (tooOld > 0) response += $" {tooOld} message(s) were skipped as they are older than 14 days.";

        await command.ModifyOriginalResponseAsync(m => m.Content = response);
        await _logHandler.LogMassRemove(command, validMessages.Count);
    }
}