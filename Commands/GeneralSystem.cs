using Discord;
using Discord.Net;
using Discord.WebSocket;
using SMASSB.Exceptions;
using SMASSB.Models;
using SMASSB.ServiceHandlers;

namespace SMASSB.Commands;

public class GeneralSystem {

    public static readonly HttpClient httpClient = new();
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
            await command.ModifyOriginalResponseAsync(m => m.Content = "No deletable messages found. Messages older than 14 days cannot be bulk deleted.");
            return;
        }

        try { await channel.DeleteMessagesAsync(validMessages); }
        catch { await command.FollowupAsync("Some message here is seen as null and cannot be deleted. Ignore it or delete it manually.", ephemeral: true); }

        var response = $"Deleted {validMessages.Count} message(s).";
        if (tooOld > 0) response += $" {tooOld} message(s) were skipped as they are older than 14 days.";

        await command.ModifyOriginalResponseAsync(m => m.Content = response);
        await _logHandler.LogMassRemove(command, validMessages.Count);
    }

    private static readonly Dictionary<ulong, (string Name, string Link)> CareerParadeDress = new() {
        [1557735964950929558] = ("Researcher", "https://sangoidoldefenseforce.vercel.app/fsgsofqvsfdfsqwjh"),
        [1557736035495059477] = ("Journalist", "https://sangoidoldefenseforce.vercel.app/xcifbozwghdfsqwjh"),
        [1568931405134561501] = ("Recon", "https://sangoidoldefenseforce.vercel.app/fsqcbdfsqwjh"),
        [1557736016016576522] = ("Medic", "https://sangoidoldefenseforce.vercel.app/asrwqdfsqwjh"),
    };

    public async Task HandleParseCivtCommand(SocketSlashCommand command) {

        await command.DeferAsync();
        
        var preCivt = new List<SocketGuildUser>();
        var enlisted = new List<SocketGuildUser>();
        var guild = _client.GetGuild((ulong)_guildId!);
        
        await guild.DownloadUsersAsync();
                    
        var failures = new List<MessageSendException>();
        var failures2 = new List<DmParseException>();
        var dmFailures = new List<string>();

        foreach (var userId in _db.GetEnlisted()) {
            var member = guild.GetUser(ulong.Parse(userId));
            if (member != null) enlisted.Add(member);
        }
        
        foreach (var user in enlisted) {
            
            if (user.IsBot) continue;
            
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

        var sent = 0;

        foreach (var user in preCivt) {

            var careers = CareerParadeDress
                .Where(c => user.Roles.Any(r => r.Id == c.Key))
                .Select(c => c.Value)
                .ToList();
            
            if (careers.Count == 0) continue;
            
            var links = string.Join("\n", careers.Select(c => $"[. . {c.Name.ToUpper()} PARADE DRESS . .](<{c.Link}>)"));
            
            var embed = new EmbedBuilder()
                .WithAuthor("Dear Enlisted, you have been given . . .")
                .WithTitle("【 PRE-CIVT FALL AND WINTER PARADE DRESS 】")
                .WithDescription($"Here's your new PRE-CIVT Parade Dress, just in time for the cold season!\nWe've changed our uniforms for the upcoming months, so everyone who received the PreCIVT uniform in the past will now get its updated version.\n\n{links}")
                .WithImageUrl("https://64.media.tumblr.com/616bce1d6e1a6d7a2123c76d6f249404/2ecded076fd064e9-c6/s1280x1920/11d327bf242c41a07ca122757d050c6c6ce52da1.pnj")
                .WithFooter("Thank you for your support thus far!\n\n 恐れも、惨めさも、怒りも無く！・❖")
                .WithColor(new Color(0xFF312C))
                .Build();
            
            try {
                await user.SendMessageAsync(embed: embed);
                sent++;
            } catch (HttpException) {
                dmFailures.Add(user.Nickname ?? user.Username);
            }

            await Task.Delay(250);
        }

        await command.FollowupAsync($"Completed task. Sent to {sent} member{(sent == 1 ? "" : "s")}.");

        var report = "";
        if (failures.Count > 0) report += "**Couldn't read DMs:**\n" + string.Join("\n", failures.Select(f => f.ToString())) + "\n\n";
        if (failures2.Count > 0) report += "**Couldn't parse DMs:**\n" + string.Join("\n", failures2.Select(f => f.ToString())) + "\n\n";
        if (dmFailures.Count > 0) report += "**Couldn't send the uniform to:**\n" + string.Join("\n", dmFailures);

        if (report.Length > 0)
            await command.FollowupAsync(report.Length > 2000 ? report[..1997] + "..." : report);
    }

    public async Task HandleCheckOrChangeClaimCommand(SocketSlashCommand command) {
    
        var claimNameOption = command.Data.Options.FirstOrDefault(o => o.Name == "claim_name");
        var memberOption = command.Data.Options.FirstOrDefault(o => o.Name == "member");
        var guild = _client.GetGuild((ulong)_guildId!);
        
        if (claimNameOption == null) {
            await command.RespondAsync("Claim name is required.", ephemeral: true);
            return;
        }

        await command.DeferAsync();
        var claimName = claimNameOption.Value.ToString();

        if (memberOption != null) {
            var member = guild.GetUser(((SocketUser)memberOption.Value).Id);

            if (claimName == null) {
                await command.FollowupAsync("Something's wrong with this command.", ephemeral: true);
                return;
            }

            var nickname = member.Nickname;
            var dotIndex = nickname.IndexOf('.');

            var fixedRankNick = nickname.Substring(0, dotIndex + 1);
            await member.ModifyAsync(x => x.Nickname = fixedRankNick + " " + claimName);

            await _db.SetClaim(member.Id, claimName);
            await command.FollowupAsync("Changed claim.", ephemeral: true);

        } else {
            
            var allClaims = _db.GetAllClaims();
            var matches = allClaims
                .Where(m => !string.IsNullOrWhiteSpace(m.Claim) && claimName != null && m.Claim.Contains(claimName, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (matches.Count == 0) {
                await command.FollowupAsync("No claims found!", ephemeral: true);
            } else {
                
                var matchDesc = "";
                foreach (var match in matches) {
                    matchDesc += $"{match.Claim} :: <@{match.UserId}>\n";
                }
                await command.FollowupAsync(matchDesc, ephemeral: true);
            }
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

    public async Task ShowProfile(SocketSlashCommand command) {

        try {
            using var response = await httpClient.GetAsync($"https://www.sango.asia/content/personnel/{command.User.Id}.json", HttpCompletionOption.ResponseHeadersRead);

            if (response.IsSuccessStatusCode && response.Content.Headers.ContentType?.MediaType == "application/json") {
                await command.RespondAsync($"[<:sango_a_redfourstar:1565904661922455583> ・ Here's your profile!](https://www.sango.asia/profile.html?id={command.User.Id})");
                
            } else {
                await command.RespondAsync(
                    "Hey! We can't find a profile for your ID! Log in and make one here　⋮　\n\n<:sango_a_redarrow:1565905694652243998> ・ https://www.sango.asia/roster.html");
            }
        } catch {
            await _logHandler.LogExceptionWatch((ulong)_guildId!, text:"*Something* went wrong in the ShowProfile command.");
        }
    }
}