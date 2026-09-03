using System.Text.RegularExpressions;
using Discord;
using Discord.WebSocket;
using SMASSB.Exceptions;
using SMASSB.Models;

namespace SMASSB.Commands;

public class PointSystem {
    
    private readonly DiscordSocketClient _client;
    private readonly DatabaseService _db;
    private readonly SocketGuild _guild;
    private readonly LogHandler _logHandler;
    private static readonly HttpClient HttpClient = new HttpClient {
        BaseAddress = new Uri(Environment.GetEnvironmentVariable("BOT_B_API_URL") ?? throw new Exception("BOT_B_API_URL environment variable not set.")),
        DefaultRequestHeaders = { { "X-Internal-Key", Environment.GetEnvironmentVariable("INTERNAL_API_KEY") } }
    };
    
    public PointSystem(DiscordSocketClient client, LogHandler logHandler, DatabaseService db, GuildConfiguration guildConfig) {
        
        _client = client;
        _logHandler = logHandler;
        _db = db;
        var guildId = guildConfig.GuildId;
        _guild = client.GetGuild(guildId);
    }

    public async Task ShowPoints(SocketSlashCommand command) {
        
        SocketGuildUser member = (SocketGuildUser)command.User;
        
        foreach (var option in command.Data.Options) {
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

        var embed = (new EmbedBuilder()
            .WithAuthor("|| " + member.Nickname, member.GetGuildAvatarUrl() ?? member.GetAvatarUrl())
            .WithTitle("❖﹒Points . .")
            .WithDescription("This member has earned ***" + points + "*** points.")
            .WithColor(0xBFA55F)
            .WithFooter("Why not use /showid to look at your points? It's a lot cooler, we promise.")).Build();
        
        await command.RespondAsync(embed: embed);
    }

    public async Task EditPoints(SocketSlashCommand command, bool add) {
        
        var enlisteds = new List<SocketGuildUser>();
        var points = 0;
        var recruits = 0;
        var currency = 0;
        
        foreach (var option in command.Data.Options) {
            
            if (option.Name.StartsWith("enlisted")) {
                enlisteds.Add((SocketGuildUser)option.Value);
            } else switch (option.Name) {
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
        
        foreach (var member in enlisteds) {
            var embedBuilder = new EmbedBuilder();
            
            if (add) {
                await _db.AddPoints(member.Id, points);
                await _db.AddRecruits(member.Id, recruits);
                var current = await _db.GetPoints(member.Id);
                var currentR = await _db.GetRecruits(member.Id);

                if (recruits == 0) {
                    embedBuilder.WithDescription("This member has been given ***" + points + "*** point" + (points == 1 ? "" : "s") + ", and now has ***" + current + "*** point" + (current == 1 ? "" : "s") + ".");
                } else {
                    embedBuilder.WithDescription("This member has been given ***" + points + "*** point" + (points == 1 ? "" : "s") +",\nand now has ***" + current + "*** point" + (current == 1 ? "" : "s") + ".\n\nThey've also scouted ***" + recruits + "*** recruit" + (recruits == 1 ? "" : "s") + ", and now has scouted ***" + currentR + "*** recruit" + (currentR == 1 ? "" : "s") + " in total!");
                }
                
            } else {
                await _db.RemovePoints(member.Id, points);
                var current = await _db.GetPoints(member.Id);
            
                embedBuilder.WithDescription("You have removed ***" + points + "*** point" + (points == 1 ? "" : "s") + " from this member.\nThey now have ***" + current + "*** point" + (current == 1 ? "" : "s") + ".");
            }

            embedBuilder
                .WithAuthor("|| " + member.Nickname, member.GetGuildAvatarUrl() ?? member.GetAvatarUrl())
                .WithTitle("❖﹒Done and done!")
                .WithColor(0xBFA55F);
        
            await command.FollowupAsync(embed: embedBuilder.Build());

            if (currency == 0) continue;
            
            try {
                var response = await HttpClient.PostAsJsonAsync("/internal/currency",
                    new CurrencyModels.CurrencyRequest(member.Id, currency));

                response.EnsureSuccessStatusCode();
                var result = await response.Content.ReadFromJsonAsync<CurrencyModels.CurrencyResult>();

                if (result != null) {
                    var currencyEmbed = new EmbedBuilder()
                        .WithAuthor("|| " + member.Nickname, member.GetGuildAvatarUrl() ?? member.GetAvatarUrl())
                        .WithTitle("★﹒I wish, and wish, and wish . .")
                        .WithDescription($"This member has been given ***{currency}*** Star Piece{(currency == 1 ? "" : "s")},\nand now has ***{result.NewBalance}*** Star Piece{(result.NewBalance == 1 ? "" : "s")}.")
                        .WithColor(0xBFA55F)
                        .Build();

                    await command.FollowupAsync(embed: currencyEmbed);
                }
            } catch (HttpRequestException ex) {
                currencyFailures.Add(new CurrencySyncException(member.Username, $"Failed to sync currency for '{member.Username}'.", ex));
            }
        }

        if (currencyFailures.Count > 0) {
            foreach (var e in currencyFailures) {
                await command.FollowupAsync(e.ToString());
            }
        }
    }

    public async Task EditRecruits(SocketSlashCommand command, bool add) {
        
        SocketGuildUser? member = null;
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
        
        var embedBuilder = new EmbedBuilder();
        
        if (add) {
            await _db.AddRecruits(member.Id, recruits);
            var current = await _db.GetRecruits(member.Id);
            
            embedBuilder.WithDescription("This member has scouted ***" + recruits + "*** recruit" + (recruits == 1 ? "" : "s") + ", and now has scouted ***" + current + "*** recruit" + (current == 1 ? "" : "s") + " in total!");
        } else {
            
            await _db.RemoveRecruits(member.Id, recruits);
            var current = await _db.GetRecruits(member.Id);
            
            embedBuilder.WithDescription("You have removed ***" + recruits + "*** recruitpoint" + (recruits == 1 ? "" : "s") + " from this member.\nThey now have ***" + current + "*** recruitpoint" + (current == 1 ? "" : "s") + ".");
        }

        embedBuilder
            .WithAuthor("|| " + member.Nickname, member.GetGuildAvatarUrl() ?? member.GetAvatarUrl())
            .WithTitle("❖﹒Done and done!")
            .WithColor(0x44786F);
        
        await command.RespondAsync(embed: embedBuilder.Build());
    }
    
    private static readonly Regex BatchLineRegex = new Regex(@"^\s*(?<name>.+?)\s+(?<tokens>(?:[pcry]\d+\s*)+)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex TokenRegex = new Regex(@"(?<type>[pcry])(?<value>\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public async Task HandleBatchPoints(SocketSlashCommand command) {

        string? messageLink = null;
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
        
        var channelId = ulong.Parse(linkMatch.Groups[2].Value);
        var messageId = ulong.Parse(linkMatch.Groups[3].Value);
        
        var channel = _guild.GetTextChannel(channelId);
        if (channel == null) {
            await command.FollowupAsync("I couldn't find that channel! Does it... exist?");
            return;
        }

        IMessage? message;
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

            int points = 0, currency = 0, recruits = 0, yen = 0;
            var seenTypes = new HashSet<char>();
            var duplicateFound = false;

            foreach (Match token in TokenRegex.Matches(lineMatch.Groups["tokens"].Value)) {
                var type = char.ToLowerInvariant(token.Groups["type"].Value[0]);

                if (!seenTypes.Add(type)) {
                    duplicateFound = true;
                    break;
                }

                var value = int.Parse(token.Groups["value"].Value);
                switch (type) {
                    case 'p': points = value; break;
                    case 'c': currency = value; break;
                    case 'r': recruits = value; break;
                    case 'y': yen = value; break;
                }
            }

            if (duplicateFound) {
                notFound.Add($"Couldn't parse: \"{line}\"");
                continue;
            }

            var scored = allClaims
                .Where(m => !string.IsNullOrWhiteSpace(m.Claim))
                .Select(m => new { Claim = m, Distance = FuzzyContainsDistance(m.Claim, name) })
                .Where(x => x.Distance <= MaxAllowedDistance(name))
                .OrderBy(x => x.Distance)
                .ToList();

            if (scored.Count == 0) {
                notFound.Add(name);
                continue;
            }

            var bestDistance = scored[0].Distance;
            var matches = scored.Where(x => x.Distance == bestDistance).Select(x => x.Claim).ToList();

            if (matches.Count > 1) {
                ambiguous.Add($"{name} (matched: {string.Join(", ", matches.Select(m => m.Claim))})");
                continue;
            }

            var wasFuzzy = bestDistance > 0;
            var userId = ulong.Parse(matches[0].UserId);
            
            if (points > 0) await _db.AddPoints(userId, points);
            if (recruits > 0) await _db.AddRecruits(userId, recruits);
            if (yen > 0) await _db.AddYen(userId, yen);
            
            var currentPoints = await _db.GetPoints(userId);
            var currentRecruits = await _db.GetRecruits(userId);
            var currentYen = await _db.GetYen(userId);

            var summary = $"<@{userId}> has been given ***{points}*** point{(points == 1 ? "" : "s")}";
            if (recruits > 0) summary += $", ***{recruits}*** recruit{(recruits == 1 ? "" : "s")}";
            if (yen > 0) summary += $", ***{yen}*** yen";
            summary += $". They now have ***{currentPoints}*** point{(currentPoints == 1 ? "" : "s")}";
            
            if (recruits > 0) summary += $", have scouted ***{currentRecruits}*** recruit{(currentRecruits == 1 ? "" : "s")} in total";
            if (yen > 0) summary += $", and have cashed out ***{currentYen}*** yen in total";
            summary += ".";
            if (wasFuzzy) summary += $" *(matched \"{name}\" → \"{matches[0].Claim}\", {bestDistance} char{(bestDistance == 1 ? "" : "s")} off)*";
            summary += "\n";
            
            if (currency != 0) {
                if (_guild != null) {
                    var member = _guild.GetUser(userId);
                    try {
                        var response = await HttpClient.PostAsJsonAsync("/internal/currency",
                            new CurrencyModels.CurrencyRequest(userId, currency));
            
                        response.EnsureSuccessStatusCode();
                        var result = await response.Content.ReadFromJsonAsync<CurrencyModels.CurrencyResult>();

                        if (result != null) {
                            var currencyEmbed = new EmbedBuilder()
                                .WithAuthor("|| " + member.Nickname, member.GetGuildAvatarUrl() ?? member.GetAvatarUrl())
                                .WithTitle("★﹒I wish, and wish, and wish . .")
                                .WithDescription($"This member has been given ***{currency}*** Star Piece{(currency == 1 ? "" : "s")},\nand now has ***{result.NewBalance}*** Star Piece{(result.NewBalance == 1 ? "" : "s")}.")
                                .WithColor(0xBFA55F)
                                .Build();
            
                            await command.FollowupAsync(embed: currencyEmbed);
                        }
                    } catch (HttpRequestException ex) {
                        currencyFailures.Add(new CurrencySyncException(member.Username, $"Failed to sync currency for '{member.Username}'.", ex));
                    }
                }
            }
            successes.Add(summary);
        }

        if (currencyFailures.Count > 0) {
            foreach (var e in currencyFailures) {
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

        const int maxDescriptionLength = 4096;

        if (description.Length <= maxDescriptionLength) {
            embedBuilder.WithDescription(description);
            await command.FollowupAsync(embed: embedBuilder.Build());
        } else {
            
            var chunks = new List<string>();
            var descLines = description.Split('\n');
            var currentChunk = "";
    
            foreach (var line in descLines) {
                if ((currentChunk + line + "\n").Length > maxDescriptionLength) {
                    if (!string.IsNullOrEmpty(currentChunk)) chunks.Add(currentChunk.TrimEnd());
                    currentChunk = line + "\n";
                } else {
                    currentChunk += line + "\n";
                }
            }
    
            if (!string.IsNullOrEmpty(currentChunk)) chunks.Add(currentChunk.TrimEnd());
    
            foreach (var (chunk, index) in chunks.Select((c, i) => (c, i))) {
                var embed = new EmbedBuilder()
                    .WithTitle($"❖﹒Batch points . . (Part {index + 1}/{chunks.Count})")
                    .WithColor(0xBFA55F)
                    .WithDescription(chunk)
                    .Build();
                await command.FollowupAsync(embed: embed);
            }
        }
    }

    public async Task HandleBatchRecruits(SocketSlashCommand command) {

        await command.DeferAsync();

        var channel = command.Channel;
        if (channel.Id != 1475729416264093787) {
            await command.FollowupAsync("This isn't the Prospects channel!");
            return;
        }
        var messagesAsync = channel.GetMessagesAsync();

        var cutoff = DateTimeOffset.UtcNow.AddDays(-3);
        var stopped = false;
        var desc = "";

        await foreach (var batch in messagesAsync) {
            foreach (var message in batch) {

                if (message.Timestamp < cutoff) {
                    stopped = true;
                    break;
                }

                var user = message.Author;
                if (user == null || user.IsBot) {
                    desc += $"Skipped a message from **{message.Author?.Username ?? "an unknown/departed user"}** (not a current member).\n";
                    continue;
                }

                try {
                    await _db.AddPoints(user.Id, 1);
                    await _db.AddRecruits(user.Id, 1);
                    await _db.AddYen(user.Id, 800);
                    desc += $"Parsed **{user.Username}**'s message successfully.\n";
                } catch {
                    desc += $"Failed to parse message sent by **{user.Username}**. Run /addpoints for them instead.\n";
                }
            }
            if (stopped) break;
        }

        await command.FollowupAsync(desc + "\n\nFeel free to use /purgemessages to remove the above messages.");
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
    
        var totalPages = (int)Math.Ceiling(entries.Count / (double)PageSize);
        var start = page * PageSize;
        var end = Math.Min(start + PageSize, entries.Count);
    
        var description = "";
        for (var i = start; i < end; i++) {
            var (userId, _, points) = entries[i];
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

    public async Task ReinstateEnlistment(SocketSlashCommand command) {

        SocketGuildUser? member = null;
        
        foreach (var option in command.Data.Options) {
            switch (option.Name) {
                
                case "member":
                    member = ((SocketGuildUser)option.Value);
                    break;
                default:
                    await command.RespondAsync("Unrecognized command.", ephemeral: true);
                    break;
            }
        }

        if (member == null)
            return;
        
        await _db.ReinstateEnlistment(member.Id, _client);
    }

    public async Task FestivalRewards(SocketSlashCommand command) {

        var enlisted = new List<SocketGuildUser>();
        var currencyFailures = new List<CurrencySyncException>();
        var results = new List<(SocketGuildUser User, long Balance)>();
        
        await command.DeferAsync();
        
        foreach (var userId in _db.GetEnlisted()) {
            var member = _guild.GetUser(ulong.Parse(userId));
            if (member != null) enlisted.Add(member);
        }
        
        foreach (var user in enlisted) {
            try {
                var response = await HttpClient.PostAsJsonAsync("/internal/currency",
                    new CurrencyModels.CurrencyRequest(user.Id, 0));
        
                response.EnsureSuccessStatusCode();
                var result = await response.Content.ReadFromJsonAsync<CurrencyModels.CurrencyResult>();

                if (result != null) results.Add((user, result.NewBalance));
                
            } catch (HttpRequestException ex) {
                currencyFailures.Add(new CurrencySyncException(user.Username, $"Failed to find AND / OR sync currency for '{user.Username}'.", ex));
            }
        }
        
        foreach (var (user, balance) in results) {
            if (balance < 10) continue;
            
            var points = (int)(balance / 3);
            await _db.AddPoints(user.Id, points);
            await command.FollowupAsync($"Rewarded <@{user.Id}> {points} points.");
            await user.AddRoleAsync(1527906014060609586);
        }
        
        var ranked = results.OrderByDescending(r => r.Balance).ToList();
        var limit = Math.Min(7, ranked.Count);
        
        for (var i = 0; i < limit; i++) {
            
            var user = ranked[i].User;
                await _db.GiveNewId(user.Id, "ENLISTEDTANABATA");
                await user.RemoveRoleAsync(1527906014060609586);
                string place;
        
                if (i == 3) {
                    place = ToOrdinal(i);
                } else if (i > 3) {
                    place = ToOrdinal(i - 1);
                } else place = ToOrdinal(i + 1);
            
                var embedBuilder = new EmbedBuilder()
                    .WithImageUrl("https://media.discordapp.net/attachments/1473209020285452360/1520884908397035590/IMG_2415.gif?ex=6a6efc32&is=6a6daab2&hm=48177297d77f74c40998c69a7c4269c22abe15b0222798240f23870560d1e546&=")
                    .WithColor(new Color(0x829cff));
                
                if (i == 0) {
                    await user.AddRoleAsync(1527905937329881158);
                    embedBuilder.WithDescription($"## Congratulations, {await _db.GetClaim(user.Id)}!\n\nYou've done a fantastic job with the special largescale event over the course of the past month.\n\nFor this reason alone, you have placed **1ST** in the Festival event and gained the following rewards:\n\n- The Tier 1 Badge : The Golden Tanzaku\n- A headshot of your character drawn by the main server artist, KAPS\n- A third of your currency transformed into points (nice rank skip!)\n- The limited Starry Night ID skin (use /editid to check it out!)\n- The ability to help design the special Parade Dress for our debut event!\n\nMake sure to message Mikage Makina for more information about the headshot from KAPS!\nMessage Kamikawa Hiromi to discuss more about the future Parade Dress you'll be helping with!\n\nGood job, you did wonderfully. We are honored to have had you here with us!\n### _ _                                                         — The Staff at Sangō Idol-Defense Force");
                } else if (i is 1 or 2 or 3) {
                    embedBuilder.WithDescription($"## Congratulations, {await _db.GetClaim(user.Id)}!\n\nYou've done a fantastic job with the special largescale event over the course of the past month.\n\nFor this reason alone, you have placed **{place}** in the Festival event and gained the following rewards:\n\n- The Tier 2 Badge : The Silver Tanzaku\n- An art piece of your character drawn by the main server artist, Tokiwa Cho\n- A third of your currency transformed into points\n- The limited Starry Night ID skin (use /editid to check it out!)\n\nMake sure to message Tokiwa Cho for more information about the art they'll draw for you!\n\nGood job, you did wonderfully. We are honored to have had you here with us!\n### _ _                                                         — The Staff at Sangō Idol-Defense Force");
                } else {
                    await user.AddRoleAsync(1527905990329110669);
                    embedBuilder.WithDescription($"## Congratulations, {await _db.GetClaim(user.Id)}!\n\nYou've done a fantastic job with the special largescale event over the course of the past month.\n\nFor this reason alone, you have placed **{place}** in the Festival event and gained the following rewards:\n\n- The Tier 2 Badge : The Silver Tanzaku\n- A third of your currency transformed into points\n- The limited Starry Night ID skin (use /editid to check it out!)\n\nGood job, you did wonderfully. We are honored to have had you here with us!\n### _ _                                                         — The Staff at Sangō Idol-Defense Force");
                }
                await user.SendMessageAsync(embed: embedBuilder.Build());
        }
        
        if (currencyFailures.Count > 0) {
            var failureText = string.Join("\n", currencyFailures.Select(f => f.Message));
            await command.FollowupAsync(failureText, ephemeral: true);
        }
    }
    
    private static string ToOrdinal(int number) {
        if (number <= 0) return number.ToString();

        switch (number % 100) {
            case 11:
            case 12:
            case 13:
                return number + "TH";
        }

        switch (number % 10) {
            case 1: return number + "ST";
            case 2: return number + "ND";
            case 3: return number + "RD";
            default: return number + "TH";
        }
    }
    
    private static int MaxAllowedDistance(string name) {
        return name.Length <= 4 ? 1 : Math.Min(2, name.Length / 5 + 1);
    }

    private static int FuzzyContainsDistance(string haystack, string needle) {
        
        if (haystack.Contains(needle, StringComparison.OrdinalIgnoreCase)) return 0;
        if (haystack.Length < needle.Length - 1) return LevenshteinDistance(haystack, needle);

        var best = int.MaxValue;
        var n = needle.Length;

        for (var windowLen = Math.Max(1, n - 1); windowLen <= n + 1 && windowLen <= haystack.Length; windowLen++) {
            for (var start = 0; start <= haystack.Length - windowLen; start++) {
                var window = haystack.Substring(start, windowLen);
                var dist = LevenshteinDistance(window, needle);
                if (dist < best) best = dist;
                if (best == 0) return 0;
            }
        }
        return best;
    }

    private static int LevenshteinDistance(string a, string b) {
        
        a = a.ToLowerInvariant();
        b = b.ToLowerInvariant();
        var dp = new int[a.Length + 1, b.Length + 1];

        for (var i = 0; i <= a.Length; i++) dp[i, 0] = i;
        for (var j = 0; j <= b.Length; j++) dp[0, j] = j;

        for (var i = 1; i <= a.Length; i++) {
            for (var j = 1; j <= b.Length; j++) {
                var cost = a[i - 1] == b[j - 1] ? 0 : 1;
                dp[i, j] = Math.Min(
                    Math.Min(dp[i - 1, j] + 1, dp[i, j - 1] + 1),
                    dp[i - 1, j - 1] + cost);
            }
        }
        return dp[a.Length, b.Length];
    }
}