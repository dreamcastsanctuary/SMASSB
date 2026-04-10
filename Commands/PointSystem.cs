using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace SMASSB.Commands;

public class PointSystem {
    
    private DatabaseService _db;
    
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
        
        var points = _db.GetPoints(member.Id);

        Embed embed = (new EmbedBuilder()
            .WithAuthor("|| " + member.Nickname, member.GetGuildAvatarUrl() ?? member.GetAvatarUrl())
            .WithTitle("❖﹒Points . .")
            .WithDescription("This member has earned ***" + points + "*** points.")
            .WithColor(0xBFA55F)).Build();
        
        await command.RespondAsync(embed: embed);
    }

    [DefaultMemberPermissions(GuildPermission.ManageRoles)]
    public async Task EditPoints(SocketSlashCommand command, bool add) {
        
        SocketGuildUser member = (SocketGuildUser)command.User;
        int points = 0;
        
        foreach (var option in command.Data.Options)
        {
            switch (option.Name) {
                
                case "member":
                    member = ((SocketGuildUser)option.Value);
                    break;
                case "amount":
                    points = (int)(long) option.Value;
                    break;
                default:
                    await command.RespondAsync("Unrecognized command.", ephemeral: true);
                    break;
            }
        }
        
        EmbedBuilder embedBuilder = new EmbedBuilder();
        if (add) {
            await _db.AddPoints(member.Id, points);
            var current = await _db.GetPoints(member.Id);
            
            var s = "s";
            if (current == 1) { s = ""; }
            
            embedBuilder.WithDescription("This member has been given ***" + points + "*** points,\nand now has ***" + current + "*** point" + s + ".");
        } else {
            await _db.RemovePoints(member.Id, points);
            var current = await _db.GetPoints(member.Id);
            
            var s = "s";
            if (current == 1) { s = ""; }
            
            embedBuilder.WithDescription("You have removed ***" + points + "*** points from this member.\nThey now have ***" + current + "*** point" + s + ".");
        }

        embedBuilder
            .WithAuthor("|| " + member.Nickname, member.GetGuildAvatarUrl() ?? member.GetAvatarUrl())
            .WithTitle("❖﹒Done and done!")
            .WithColor(0xBFA55F);
        
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
}