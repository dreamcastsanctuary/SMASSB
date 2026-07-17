using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using SMASSB.Exceptions;

namespace SMASSB.Commands;

public class GeneralSystem {

    private LogHandler _logHandler;
    private DatabaseService _db;

    public GeneralSystem(LogHandler logHandler, DatabaseService db) {
        
        _logHandler = logHandler;
        _db = db;
    }

    [DefaultMemberPermissions(GuildPermission.ManageMessages)]
    public async Task HandleMassRemoveCommand(SocketSlashCommand command)
    {

        await command.RespondAsync("Purging messages.", ephemeral: true);
        var channel = command.Channel as ITextChannel;
        int amount = 0;

        foreach (var option in command.Data.Options)
        {
            switch (option.Name)
            {
                case "amount":
                    amount = Convert.ToInt32(option.Value);
                    break;
                default:
                    await command.RespondAsync("Unrecognized option.", ephemeral: true);
                    return;
            }
        }

        if (amount < 1 || amount > 100)
        {
            await command.RespondAsync("Please provide a number between 1 and 100.", ephemeral: true);
            return;
        }

        if (channel == null)
        {
            await command.RespondAsync("This command can only be used in a text channel.", ephemeral: true);
            return;
        }

        var messages = await channel.GetMessagesAsync(amount).FlattenAsync();
        var validMessages = messages.Where(m => DateTimeOffset.UtcNow - m.Timestamp < TimeSpan.FromDays(14)).ToList();
        var tooOld = messages.Count() - validMessages.Count;

        if (!validMessages.Any())
        {
            await command.ModifyOriginalResponseAsync(m =>
                m.Content = "No deletable messages found. Messages older than 14 days cannot be bulk deleted.");
            return;
        }

        await channel.DeleteMessagesAsync(validMessages);

        string response = $"Deleted {validMessages.Count} message(s).";
        if (tooOld > 0) response += $" {tooOld} message(s) were skipped as they are older than 14 days.";

        await command.ModifyOriginalResponseAsync(m => m.Content = response);
        await _logHandler.LogMassRemove(command, validMessages.Count);
    }

    public async Task HandleParseCivtCommand(SocketSlashCommand command, DiscordSocketClient client) {

        await command.DeferAsync();
        
        List<SocketGuildUser> preCivt = new List<SocketGuildUser>();
        List<SocketGuildUser> enlisted = new List<SocketGuildUser>();

        var guild = client.GetGuild((ulong)command.GuildId);
        await guild.DownloadUsersAsync();
                    
        var failures = new List<MessageSendException>();
        var failures2 = new List<DmParseException>();

        foreach (var userId in _db.GetEnlisted()) { enlisted.Add(guild.GetUser(ulong.Parse(userId))); }
        
        foreach (var user in enlisted) {
            
            try { if (user == null || user.IsBot) continue; } catch (Exception e) { await command.FollowupAsync(e.ToString()); }
            
            try {
                var dmChannel = await user.CreateDMChannelAsync();
                var found = false;
                var batch = await dmChannel.GetMessagesAsync(100).FlattenAsync();
                var messages = batch.ToList();
                
                try {
                    if (messages.Count == 0) continue;
                    if (messages.Any(m => m.Author.Id == client.CurrentUser.Id && m.Embeds.Any(e => e.Description != null && e.Description.Contains("You've made it past your mandatory lectures!", StringComparison.OrdinalIgnoreCase)))) {
                        
                        preCivt.Add(user);
                    }
                } catch (Exception e) {
                    failures2.Add(new DmParseException(user.Nickname ?? user.Username));
                }
            } catch (Exception ex) { failures.Add(new MessageSendException(user.Username, ex)); }
            await Task.Delay(250);
        }

        var embed = new EmbedBuilder()
            .WithAuthor("Dear Enlisted, you have been given . . .")
            .WithTitle("【 PRE-CIVT PARADE DRESS 】")
            .WithDescription("As was announced a few days ago, we've undergone changes made to the *Jieikan Kōhosei* role, and made it easier to enlist.\n\nWe figured that this was all well and good for those who hadn't enlisted yet, but felt it was unfair to those who went through the trouble of the original Kōhosei roadmap.\n\nThus, we've given you a complimentary uniform and ID skin, as we felt your hard work shouldn't go unnoticed!\n\n[. . PRE-CIVT PARADE DRESS . .](<https://sangoidoldefenseforce.vercel.app/precivt>)\n-# Your new ID skin can be found with the /editid command.")
            .WithImageUrl("https://64.media.tumblr.com/616bce1d6e1a6d7a2123c76d6f249404/2ecded076fd064e9-c6/s1280x1920/11d327bf242c41a07ca122757d050c6c6ce52da1.pnj")
            .WithFooter("Thank you for your support thus far! We love you!! -The Staff Team at SANGŌ\n\n 太陽はまた昇る！・❖")
            .WithColor(new Color(0xFF312C)).Build();
        
        foreach (var user in preCivt) {
            if (user.Username.Equals("fastestthingalive"))
            {
                await UserExtensions.SendMessageAsync(user, embed: embed);
            }
        }
        
        await command.FollowupAsync(string.Join("FAILURES : \n", failures.Select(f => f.ToString())));
        await command.FollowupAsync(string.Join("FAILURES : \n", failures2.Select(f => f.ToString())));
    }
}