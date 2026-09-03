using Discord;
using Discord.WebSocket;
using SMASSB.Exceptions;
using SMASSB.Models;

namespace SMASSB.Commands;

public class GeneralSystem {

    private readonly DiscordSocketClient _client;
    private readonly LogHandler _logHandler;
    private readonly DatabaseService _db;
    private readonly ulong? _guildId;

    public GeneralSystem(DiscordSocketClient client, LogHandler logHandler, DatabaseService db, GuildConfiguration guildConfig) {
        
        _client = client;
        _logHandler = logHandler;
        _db = db;
        _guildId = guildConfig.GuildId;
    }

    public async Task HandleMassRemoveCommand(SocketSlashCommand command) {

        await command.RespondAsync("Purging messages.", ephemeral: true);
        var channel = command.Channel as ITextChannel;
        var amount = 0;

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
        var enumerable = messages.ToList();
        var validMessages = enumerable.Where(m => DateTimeOffset.UtcNow - m.Timestamp < TimeSpan.FromDays(14)).ToList();
        var tooOld = enumerable.Count - validMessages.Count;

        if (!validMessages.Any()) {
            await command.ModifyOriginalResponseAsync(m =>
                m.Content = "No deletable messages found. Messages older than 14 days cannot be bulk deleted.");
            return;
        }

        await channel.DeleteMessagesAsync(validMessages);

        var response = $"Deleted {validMessages.Count} message(s).";
        if (tooOld > 0) response += $" {tooOld} message(s) were skipped as they are older than 14 days.";

        await command.ModifyOriginalResponseAsync(m => m.Content = response);
        await _logHandler.LogMassRemove(command, validMessages.Count);
    }

    public async Task HandleParseCivtCommand(SocketSlashCommand command) {

        await command.DeferAsync();
        
        var preCivt = new List<SocketGuildUser>();
        var enlisted = new List<SocketGuildUser>();
        var guild = _client.GetGuild((ulong)_guildId!);
        
        await guild.DownloadUsersAsync();
                    
        var failures = new List<MessageSendException>();
        var failures2 = new List<DmParseException>();

        foreach (var userId in _db.GetEnlisted()) { enlisted.Add(guild.GetUser(ulong.Parse(userId))); }
        
        foreach (var user in enlisted) {
            
            try { if (user.IsBot) continue; } catch (Exception e) { await command.FollowupAsync(e.ToString()); }
            
            try {
                var dmChannel = await user.CreateDMChannelAsync();
                var batch = await dmChannel.GetMessagesAsync().FlattenAsync();
                var messages = batch.ToList();
                
                try {
                    if (messages.Count == 0) continue;
                    if (messages.Any(m => m.Author.Id == _client.CurrentUser.Id && m.Embeds.Any(e => e.Description != null && e.Description.Contains("You've made it past your mandatory lectures!", StringComparison.OrdinalIgnoreCase)))) {
                        
                        preCivt.Add(user);
                    }
                } catch {
                    failures2.Add(new DmParseException(user.Nickname ?? user.Username));
                }
            } catch (Exception ex) { failures.Add(new MessageSendException(user.Username, ex)); }
            await Task.Delay(250);
        }

        var embed = new EmbedBuilder()
            .WithAuthor("Dear Enlisted, you have been given . . .")
            .WithTitle("【 PRE-CIVT PARADE DRESS 】")
            .WithDescription("As was announced a few days ago, we've undergone changes made to the *Jieikan Kōhosei* role, and made it easier to enlist.\n\nWe figured that this was all well and good for those who hadn't enlisted yet, but felt it was unfair to those who went through the trouble of the original Kōhosei roadmap.\n\nThus, we've given you a complimentary uniform and ID skin, as we felt your hard work shouldn't go unnoticed!\n\n[. . PRE-CIVT PARADE DRESS . .](<https://sangoidoldefenseforce.vercel.app/precivt>)\n\n-# Your new ID skin can be found with the /editid command.")
            .WithImageUrl("https://64.media.tumblr.com/616bce1d6e1a6d7a2123c76d6f249404/2ecded076fd064e9-c6/s1280x1920/11d327bf242c41a07ca122757d050c6c6ce52da1.pnj")
            .WithFooter("Thank you for your support thus far! We love you!!\n\n 恐れも、惨めさも、怒りも無く！・❖")
            .WithColor(new Color(0xFF312C)).Build();
        
        foreach (var user in preCivt) {
            await user.SendMessageAsync(embed: embed);
            await _db.GiveNewId(user.Id, "ENLISTEDPRECIVT");
        }

        await command.FollowupAsync("Completed task.");
        await command.FollowupAsync(string.Join("FAILURES : \n", failures.Select(f => f.ToString())));
        await command.FollowupAsync(string.Join("FAILURES : \n", failures2.Select(f => f.ToString())));
    }

    public async Task HandleCheckClaimedCommand(SocketSlashCommand command) {
        
        var name = "";

        foreach (var option in command.Data.Options) {
            switch (option.Name) {
                
                case "name":
                    name = option.Value.ToString();
                    break;
                default:
                    await command.RespondAsync("Unrecognized option.", ephemeral: true);
                    return;
            }
        }
        
        var allClaims = _db.GetAllClaims();
        
        var matches = allClaims
            .Where(m => !string.IsNullOrWhiteSpace(m.Claim) && name != null && m.Claim.Contains(name, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (matches.Count == 0) {
            await command.RespondAsync("No claims found!", ephemeral: true);
        } else {
            var matchDesc = "";
            
            foreach (var match in matches) {
                matchDesc += match.Claim + "\n";
            }
            await command.RespondAsync(matchDesc, ephemeral: true);
        }
    }
    
    public async Task HandleParseNonTrainedKohoseiCommand(SocketSlashCommand command) {

        await command.DeferAsync();
        
        var preCivt = new List<SocketGuildUser>();
        var enlisted = new List<SocketGuildUser>();
        var guild = _client.GetGuild((ulong)_guildId!);
        
        await guild.DownloadUsersAsync();

        foreach (var userId in _db.GetEnlisted()) { enlisted.Add(guild.GetUser(ulong.Parse(userId))); }
        foreach (var user in enlisted) {
            
            var dmChannel = await user.CreateDMChannelAsync();
            var batch = await dmChannel.GetMessagesAsync().FlattenAsync();
            var messages = batch.ToList();
            
            if (messages.Count == 0) continue;

            var hasLockedMessage = messages.Any(m => m.Author.Id == _client.CurrentUser.Id && m.Embeds.Any(e => e.Description != null && e.Description.Contains("151618 | LOCKED", StringComparison.OrdinalIgnoreCase)));
            var hasKoName = (user.Nickname ?? user.Username).Contains("Kō", StringComparison.OrdinalIgnoreCase);
            var hasNoPoints = await _db.GetPoints(user.Id) == 0;
                    
            if (hasNoPoints && !hasLockedMessage && hasKoName) {
                preCivt.Add(user);
            }

            await Task.Delay(250);
        }

        foreach (var user in preCivt) {
            await user.AddRoleAsync(1537202109336920096);
        }

        await command.FollowupAsync("Done!");
    }
}