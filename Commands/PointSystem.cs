using System.Net.Http.Json;
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
}