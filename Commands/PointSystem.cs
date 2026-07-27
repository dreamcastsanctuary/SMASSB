using System.Linq;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using SMASSB.Exceptions;
using SMASSB.Models;

namespace SMASSB.Commands;

public class PointSystem {
    
    private DatabaseService _db;
    private static readonly HttpClient _internalClient = new HttpClient {
        BaseAddress = new Uri(Environment.GetEnvironmentVariable("BOT_B_API_URL") ?? throw new Exception("BOT_B_API_URL environment variable not set.")),
        DefaultRequestHeaders = { { "X-Internal-Key", Environment.GetEnvironmentVariable("INTERNAL_API_KEY") } }
    };
    
    public PointSystem(DatabaseService db) {
        _db = db;
    }

    public async Task ShowPoints(SocketSlashCommand command) {
        
        SocketGuildUser member = (SocketGuildUser)command.User;
        
        foreach (var option in command.Data.Options)
        {
            switch (option.Name) {
                
                case "member":
                    member = ((SocketGuildUser)option.Value);
                    break;
                default:
                    await command.RespondAsync("Unrecognized command.", ephemeral: true);
                    break;
            }
        }

        int points;
        try { points = await _db.GetPoints(member.Id); } catch { await command.RespondAsync("Forgot to enlist someone?"); return; }

        Embed embed = (new EmbedBuilder()
            .WithAuthor("|| " + member.Nickname, member.GetGuildAvatarUrl() ?? member.GetAvatarUrl())
            .WithTitle("❖﹒Points . .")
            .WithDescription("This member has earned ***" + points + "*** points.")
            .WithColor(0xBFA55F)
            .WithFooter("Why not use /showid to look at your points? It's a lot cooler, we promise.")).Build();
        
        await command.RespondAsync(embed: embed);
    }

    [DefaultMemberPermissions(GuildPermission.ManageRoles)]
    public async Task EditPoints(SocketSlashCommand command, bool add) {
        
        List<SocketGuildUser> enlisteds = new List<SocketGuildUser>();
        var points = 0;
        var recruits = 0;
        var currency = 0;
        
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
                case "enlisted11":
                    enlisteds.Add(((SocketGuildUser)option.Value));
                    break;
                case "enlisted12":
                    enlisteds.Add(((SocketGuildUser)option.Value));
                    break;
                case "enlisted13":
                    enlisteds.Add(((SocketGuildUser)option.Value));
                    break;
                case "enlisted14":
                    enlisteds.Add(((SocketGuildUser)option.Value));
                    break;
                case "enlisted15":
                    enlisteds.Add(((SocketGuildUser)option.Value));
                    break;
                case "enlisted16":
                    enlisteds.Add(((SocketGuildUser)option.Value));
                    break;
                case "enlisted17":
                    enlisteds.Add(((SocketGuildUser)option.Value));
                    break;
                case "enlisted18":
                    enlisteds.Add(((SocketGuildUser)option.Value));
                    break;
                case "enlisted19":
                    enlisteds.Add(((SocketGuildUser)option.Value));
                    break;
                case "enlisted20":
                    enlisteds.Add(((SocketGuildUser)option.Value));
                    break;
                case "enlisted21":
                    enlisteds.Add(((SocketGuildUser)option.Value));
                    break;
                case "enlisted22":
                    enlisteds.Add(((SocketGuildUser)option.Value));
                    break;
                
                case "amount":
                    points = (int)(long) option.Value;
                    break;
                case "recruitpoints":
                    recruits = (int)(long) option.Value;
                    break;
                case "currency":
                    currency = (int)(long) option.Value;
                    break;
                default:
                    await command.RespondAsync("Unrecognized command.", ephemeral: true);
                    return;
            }
        }

        await command.DeferAsync();
        var currencyFailures = new List<CurrencySyncException>();
        
        foreach (SocketGuildUser member in enlisteds)
        {
            EmbedBuilder embedBuilder = new EmbedBuilder();
            if (add) {
                await _db.AddPoints(member.Id, points);
                await _db.AddRecruits(member.Id, recruits);
                var current = await _db.GetPoints(member.Id);
                var currentR = await _db.GetRecruits(member.Id);
            
                var s = "s";
                if (current == 1) { s = ""; }

                var sPre = "s";
                if (points == 1) { sPre = ""; }

                var sR = "s";
                if (currentR == 1) { sR = ""; }
                
                var sPreR = "s";
                if (recruits == 1) { sPreR = ""; }

                if (recruits == 0) {
                    embedBuilder.WithDescription("This member has been given ***" + points + "*** point" + sPre + ", and now has ***" + current + "*** point" + s + ".");
                } else {
                    embedBuilder.WithDescription("This member has been given ***" + points + "*** point" + sPre +",\nand now has ***" + current + "*** point" + s + ".\n\nThey've also scouted ***" + recruits + "*** recruit" + sPreR + ", and now has scouted ***" + currentR + "*** recruit" + sR + " in total!");
                }
                
            } else {
                await _db.RemovePoints(member.Id, points);
                var current = await _db.GetPoints(member.Id);
            
                var s = "s";
                if (current == 1) { s = ""; }
                
                var sPre = "s";
                if (points == 1) { sPre = ""; }
            
                embedBuilder.WithDescription("You have removed ***" + points + "*** point" + sPre + " from this member.\nThey now have ***" + current + "*** point" + s + ".");
            }

            embedBuilder
                .WithAuthor("|| " + member.Nickname, member.GetGuildAvatarUrl() ?? member.GetAvatarUrl())
                .WithTitle("❖﹒Done and done!")
                .WithColor(0xBFA55F);
        
            await command.FollowupAsync(embed: embedBuilder.Build());
            
            if (currency != 0) {
                
                try {
                    var response = await _internalClient.PostAsJsonAsync("/internal/currency",
                        new CurrencyModels.CurrencyRequest(member.Id, currency));

                    response.EnsureSuccessStatusCode();
                    var result = await response.Content.ReadFromJsonAsync<CurrencyModels.CurrencyResult>();

                    var currencyEmbed = new EmbedBuilder()
                        .WithAuthor("|| " + member.Nickname, member.GetGuildAvatarUrl() ?? member.GetAvatarUrl())
                        .WithTitle("★﹒I wish, and wish, and wish . .")
                        .WithDescription($"This member has been given ***{currency}*** Star Piece{(currency == 1 ? "" : "s")},\nand now has ***{result.NewBalance}*** Star Piece{(result.NewBalance == 1 ? "" : "s")}.")
                        .WithColor(0xBFA55F)
                        .Build();

                    await command.FollowupAsync(embed: currencyEmbed);
                } catch (HttpRequestException ex) {
                    currencyFailures.Add(new CurrencySyncException(member.Username, $"Failed to sync currency for '{member.Username}'.", ex));
                }
            }
        }

        if (currencyFailures.Count > 0)
        {
            foreach (var e in currencyFailures)
            {
                await command.FollowupAsync(e.ToString());
            }
        }
    }

    public async Task EditRecruits(SocketSlashCommand command, bool add) {
        
        SocketGuildUser member = null;
        var recruits = 0;
        
        foreach (var option in command.Data.Options)
        {
            switch (option.Name)
            {
                case "member":
                    member = ((SocketGuildUser)option.Value);
                    break;
                case "recruitpoints":
                    recruits = (int)(long)option.Value;
                    break;
                case "amount":
                    break;
                default:
                    await command.RespondAsync("Unrecognized command.", ephemeral: true);
                    break;
            }
        }

        if (member == null) return;
        
        EmbedBuilder embedBuilder = new EmbedBuilder();
        var sPreR = "s";
        if (recruits == 1) { sPreR = ""; }
        
        if (add) {
            await _db.AddRecruits(member.Id, recruits);
            var current = await _db.GetRecruits(member.Id);
            
            var s = "s";
            if (current == 1) { s = ""; }
            
            embedBuilder.WithDescription("This member has scouted ***" + recruits + "*** recruit" + sPreR + ", and now has scouted ***" + current + "*** recruit" + s + " in total!");
        } else {
            
            await _db.RemoveRecruits(member.Id, recruits);
            var current = await _db.GetRecruits(member.Id);
            
            var s = "s";
            if (current == 1) { s = ""; }
            
            embedBuilder.WithDescription("You have removed ***" + recruits + "*** recruitpoint" + sPreR + " from this member.\nThey now have ***" + current + "*** recruitpoint" + s + ".");
        }

        embedBuilder
            .WithAuthor("|| " + member.Nickname, member.GetGuildAvatarUrl() ?? member.GetAvatarUrl())
            .WithTitle("❖﹒Done and done!")
            .WithColor(0x44786F);
        
        await command.RespondAsync(embed: embedBuilder.Build());
    }
    
    private static readonly Regex BatchLineRegex = new Regex(
        @"^\s*(?<name>.+?)\s+p(?<points>\d+)\s+c(?<currency>\d+)(?:\s+r(?<recruits>\d+))?\s*$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    [DefaultMemberPermissions(GuildPermission.ManageRoles)]
    public async Task HandleBatchPoints(SocketSlashCommand command, DiscordSocketClient client) {

        string messageLink = null;
        var currencyFailures = new List<CurrencySyncException>();
        
        foreach (var option in command.Data.Options) {
            switch (option.Name) {
                case "message_link":
                    messageLink = option.Value.ToString();
                    break;
                default:
                    await command.RespondAsync("Unrecognized command.", ephemeral: true);
                    return;
            }
        }

        if (string.IsNullOrWhiteSpace(messageLink)) {
            await command.RespondAsync("You need to supply a message link.", ephemeral: true);
            return;
        }

        var linkMatch = Regex.Match(messageLink, @"channels/(\d+)/(\d+)/(\d+)");
        if (!linkMatch.Success) {
            await command.RespondAsync("That doesn't look like a valid message link.", ephemeral: true);
            return;
        }

        await command.DeferAsync();

        ulong guildId = ulong.Parse(linkMatch.Groups[1].Value);
        ulong channelId = ulong.Parse(linkMatch.Groups[2].Value);
        ulong messageId = ulong.Parse(linkMatch.Groups[3].Value);

        var guild = client.GetGuild(guildId);
        var channel = guild?.GetTextChannel(channelId);
        if (channel == null) {
            await command.FollowupAsync("I couldn't find that channel! Does it... exist?");
            return;
        }

        IMessage message;
        try {
            message = await channel.GetMessageAsync(messageId);
        } catch {
            message = null;
        }

        if (message == null) {
            await command.FollowupAsync("I couldn't find that message.");
            return;
        }
        
        var allClaims = _db.GetAllClaims();

        var lines = message.Content
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.Trim())
            .Where(l => l.Length > 0);

        var successes = new List<string>();
        var notFound = new List<string>();
        var ambiguous = new List<string>();

        foreach (var line in lines) {

            var lineMatch = BatchLineRegex.Match(line);
            if (!lineMatch.Success) {
                notFound.Add($"Couldn't parse: \"{line}\"");
                continue;
            }

            var name = lineMatch.Groups["name"].Value.Trim();
            var points = int.Parse(lineMatch.Groups["points"].Value);
            var currency = lineMatch.Groups["currency"].Success ? int.Parse(lineMatch.Groups["currency"].Value) : 0;
            var recruits = lineMatch.Groups["recruits"].Success ? int.Parse(lineMatch.Groups["recruits"].Value) : 0;

            var matches = allClaims
                .Where(m => !string.IsNullOrWhiteSpace(m.Claim) && m.Claim.Contains(name, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (matches.Count == 0) {
                notFound.Add(name);
                continue;
            }

            if (matches.Count > 1) {
                ambiguous.Add($"{name} (matched: {string.Join(", ", matches.Select(m => m.Claim))})");
                continue;
            }

            var userId = ulong.Parse(matches[0].UserId);
            
            if (points > 0) await _db.AddPoints(userId, points);
            if (recruits > 0) await _db.AddRecruits(userId, recruits);

            var currentPoints = await _db.GetPoints(userId);
            var currentRecruits = await _db.GetRecruits(userId);

            var summary = $"<@{userId}> has been given ***{points}*** point{(points == 1 ? "" : "s")}";
            if (recruits > 0) summary += $" and ***{recruits}*** recruit{(recruits == 1 ? "" : "s")}";
            summary += $". They now have ***{currentPoints}*** point{(currentPoints == 1 ? "" : "s")}";
            if (recruits > 0) summary += $" and have scouted ***{currentRecruits}*** recruit{(currentRecruits == 1 ? "" : "s")} in total";
            summary += ".\n";
            
            if (currency != 0) {
                SocketGuildUser member = guild.GetUser(userId);
                try {
                    var response = await _internalClient.PostAsJsonAsync("/internal/currency",
                        new CurrencyModels.CurrencyRequest(userId, currency));

                    response.EnsureSuccessStatusCode();
                    var result = await response.Content.ReadFromJsonAsync<CurrencyModels.CurrencyResult>();

                    var currencyEmbed = new EmbedBuilder()
                        .WithAuthor("|| " + member.Nickname, member.GetGuildAvatarUrl() ?? member.GetAvatarUrl())
                        .WithTitle("★﹒I wish, and wish, and wish . .")
                        .WithDescription($"This member has been given ***{currency}*** Star Piece{(currency == 1 ? "" : "s")},\nand now has ***{result.NewBalance}*** Star Piece{(result.NewBalance == 1 ? "" : "s")}.")
                        .WithColor(0xBFA55F)
                        .Build();

                    await command.FollowupAsync(embed: currencyEmbed);
                } catch (HttpRequestException ex) {
                    currencyFailures.Add(new CurrencySyncException(member.Username, $"Failed to sync currency for '{member.Username}'.", ex));
                }
            }
            
            successes.Add(summary);
        }

        if (currencyFailures.Count > 0)
        {
            foreach (var e in currencyFailures)
            {
                await command.FollowupAsync(e.ToString());
            }
        }

        var embedBuilder = new EmbedBuilder()
            .WithTitle("❖﹒Batch points . .")
            .WithColor(0xBFA55F);

        var description = "";
        if (successes.Count > 0) description += string.Join("\n", successes) + "\n";
        if (notFound.Count > 0) description += "\n**Couldn't find a match for:**\n" + string.Join("\n", notFound) + "\n";
        if (ambiguous.Count > 0) description += "\n**Multiple matches found for:**\n" + string.Join("\n", ambiguous);

        if (description.Length == 0) description = "Nothing to process! The message looks empty.";

        embedBuilder.WithDescription(description);

        await command.FollowupAsync(embed: embedBuilder.Build());
    }

    public async Task Leaderboard(SocketSlashCommand command) {
    
        var entries = _db.GetLeaderboard();
    
        if (entries.Count == 0) {
            await command.RespondAsync("No enrolled members found.", ephemeral: true);
            return;
        }
    
        var embed = BuildLeaderboardEmbed(entries, 0);
        var components = BuildLeaderboardComponents(0, entries.Count);
    
        await command.RespondAsync(embed: embed, components: components);
    }
    
    private const int PageSize = 10;

    public Embed BuildLeaderboardEmbed(List<(string UserId, string Username, int Points)> entries, int page) {
    
        int totalPages = (int)Math.Ceiling(entries.Count / (double)PageSize);
        int start = page * PageSize;
        int end = Math.Min(start + PageSize, entries.Count);
    
        var description = "";
        for (int i = start; i < end; i++) {
            var (userId, username, points) = entries[i];
            var s = points == 1 ? "" : "s";
            description += $"{i + 1}) **<@{userId}>** — **{points}** point{s}\n";
        }
    
        return new EmbedBuilder()
            .WithTitle("❖﹒Leaderboard . .")
            .WithDescription(description)
            .WithFooter($"Page {page + 1}/{totalPages}")
            .WithColor(0xBFA55F)
            .Build();
    }

    public MessageComponent BuildLeaderboardComponents(int page, int totalEntries) {
    
        int totalPages = (int)Math.Ceiling(totalEntries / (double)PageSize);
    
        return new ComponentBuilder()
            .WithButton("Back", $"leaderboard_back_{page}", ButtonStyle.Secondary, disabled: page == 0)
            .WithButton("Next", $"leaderboard_next_{page}", ButtonStyle.Secondary, disabled: page >= totalPages - 1)
            .Build();
    }

    public async Task RestoreProgress(SocketSlashCommand command, DiscordSocketClient client) {

        SocketGuildUser member = null;
        
        foreach (var option in command.Data.Options)
        {
            switch (option.Name) {
                
                case "member":
                    member = ((SocketGuildUser)option.Value);
                    break;
                default:
                    await command.RespondAsync("Unrecognized command.", ephemeral: true);
                    break;
            }
        }
        
        await command.RespondAsync(await _db.UnenrolledExists(member.Id, client));
    }
    
    [DefaultMemberPermissions(GuildPermission.ManageRoles)]
    public async Task HandleKoNotes(SocketSlashCommand command, bool alreadyDeferred = false, SocketGuildUser member = null, string add = "") {
        
        if (!alreadyDeferred) await command.DeferAsync(ephemeral: true);

        if (member == null)
        {
            foreach (var option in command.Data.Options)
            {
                switch (option.Name)
                {

                    case "member":
                        member = ((SocketGuildUser)option.Value);
                        break;
                    case "writenote":
                        add = option.Value.ToString();
                        break;
                    case "amount":
                        break;
                    default:
                        await command.FollowupAsync("Unrecognized command.", ephemeral: true);
                        break;
                }
            }
        }

        if (member == null) return;
        
        var note = await _db.GetKoNotes(member.Id) + add + "\n";
        await _db.SetKoNotes(member.Id, note);
        
        await command.FollowupAsync(note, ephemeral: true);
    }

    public async Task FestivalRewards(SocketSlashCommand command, DiscordSocketClient client) {

        List<SocketGuildUser> enlisted = null;
        var guild = client.GetGuild((ulong)command.GuildId);
        var currencyFailures = new List<CurrencySyncException>();
        var desc = "";

        await command.DeferAsync();
        
        foreach (var userId in _db.GetEnlisted()) { enlisted.Add(guild.GetUser(ulong.Parse(userId))); }

        foreach (var user in enlisted) {
            try {
                var response = await _internalClient.PostAsJsonAsync("/internal/currency",
                    new CurrencyModels.CurrencyRequest(user.Id, 0));

                response.EnsureSuccessStatusCode();
                var result = await response.Content.ReadFromJsonAsync<CurrencyModels.CurrencyResult>();
                if (result.NewBalance >= 10) {
                    desc += "<@{user.Id}> :: should get the rewards.";
                }
                
            } catch (HttpRequestException ex) {
                currencyFailures.Add(new CurrencySyncException(user.Username, $"Failed to find AND / OR sync currency for '{user.Username}'.", ex));
            }
        }
        
        var currencyEmbed = new EmbedBuilder()
            .WithDescription(desc)
            .Build();

        await command.FollowupAsync(embed: currencyEmbed);
        await command.FollowupAsync(currencyFailures.ToString(), ephemeral: true);
    }
}