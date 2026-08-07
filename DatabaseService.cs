using System.Text.Json;
using Discord.WebSocket;
using SMASSB.Commands;

namespace SMASSB;

using Microsoft.Data.Sqlite;

public class DatabaseService
{
    private readonly string _connectionString;

    public DatabaseService(string? dbPath = null) {
        dbPath ??= Environment.GetEnvironmentVariable("DB_PATH") ?? "bot.db";
        _connectionString = $"Data Source={dbPath}";
        InitializeDatabase();
    }

    private void InitializeDatabase() {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS Enrolled (
                UserId TEXT PRIMARY KEY,
                Claim TEXT,
                AvatarUrl TEXT NOT NULL,
                AvatarImage BLOB,
                Rank TEXT NOT NULL,
                Points INTEGER DEFAULT 0,
                Bloodtype TEXT NOT NULL,
                Catchphrase TEXT NOT NULL,
                Username TEXT NOT NULL,
                IDType TEXT NOT NULL,
                KoNotes TEXT,
                Recruits INTEGER NOT NULL DEFAULT 0
            );

            CREATE TABLE IF NOT EXISTS Id (
                UserId TEXT PRIMARY KEY,
                Collected TEXT NOT NULL,
                Frames TEXT
            );

            CREATE TABLE IF NOT EXISTS WorkCell (
                UserId TEXT PRIMARY KEY,
                Yen INTEGER NOT NULL DEFAULT 0,
                Cases TEXT NOT NULL,
                Wallpapers TEXT NOT NULL,
                Charms TEXT,
                Apps TEXT,
                Collected TEXT
            );

            CREATE TABLE IF NOT EXISTS Shop (
                Item TEXT PRIMARY KEY,
                Cost INTEGER NOT NULL DEFAULT 0
            );

            CREATE TABLE IF NOT EXISTS Addons (
                UserId TEXT PRIMARY KEY,
                IsProspect TINYINT NOT NULL DEFAULT 0,
                IsEnlisted TINYINT NOT NULL DEFAULT 0,
                IsPartner TINYINT NOT NULL DEFAULT 0,
                IsCivilian TINYINT NOT NULL DEFAULT 0,
                IsFan TINYINT NOT NULL DEFAULT 0
            );

            CREATE TABLE IF NOT EXISTS EnrolledHistory (
                UserId TEXT PRIMARY KEY,
                Claim TEXT,
                Rank TEXT NOT NULL,
                Points INTEGER DEFAULT 0,
                Recruits INTEGER NOT NULL DEFAULT 0,
                IDType TEXT NOT NULL,
                Username TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS Starboard (
                OriginalId TEXT PRIMARY KEY,
                StarboardId TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS StatChannel (
                GuildId TEXT PRIMARY KEY,
                ChannelId TEXT NOT NULL
            );
        ";
        
        command.ExecuteNonQuery();
    }

    public async Task PreEnlist(SocketSlashCommand command,
                          SocketGuildUser member,
                          string claimParam, 
                          string avatarUrlParam,
                          string accIdParam,
                          DateTimeOffset dateParam,
                          string rankParam,
                          int pointsParam,
                          int recruitsParam,
                          string bloodtypeParam,
                          string catchphraseParam,
                          string usernameParam,
                          string idTypeParam,
                          string caseParam,
                          string charmParam,
                          string wallpaperParam) {
        
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        
        var cmd = connection.CreateCommand();
        cmd.CommandText = "INSERT INTO Enrolled (UserId, Claim, AvatarUrl, Rank, Points, Recruits, Bloodtype, Catchphrase, Username, IDType) VALUES ($accIdParam, $claimParam, $avatarUrlParam, $rankParam, $pointsParam, $recruitsParam, $bloodtypeParam, $catchphraseParam, $usernameParam, $idTypeParam);";
        cmd.Parameters.AddWithValue("$accIdParam", accIdParam);
        cmd.Parameters.AddWithValue("$claimParam", claimParam);
        cmd.Parameters.AddWithValue("$avatarUrlParam", avatarUrlParam);
        cmd.Parameters.AddWithValue("$rankParam", rankParam);
        cmd.Parameters.AddWithValue("$pointsParam", pointsParam);
        cmd.Parameters.AddWithValue("$recruitsParam", recruitsParam);
        cmd.Parameters.AddWithValue("$bloodtypeParam", bloodtypeParam);
        cmd.Parameters.AddWithValue("$catchphraseParam", catchphraseParam);
        cmd.Parameters.AddWithValue("$usernameParam", usernameParam);
        cmd.Parameters.AddWithValue("$idTypeParam", idTypeParam);
        
        cmd.ExecuteNonQuery();
        
        await command.FollowupAsync("Processed Prospect into Database.");

        await GiveNewId(ulong.Parse(accIdParam), idTypeParam);
        await GiveNewCase(ulong.Parse(accIdParam), caseParam);
        await GiveNewCharm(ulong.Parse(accIdParam), charmParam);
        await GiveNewWallpaper(ulong.Parse(accIdParam), wallpaperParam);
        await IdSystem.BuildId(command, member, claimParam, null, avatarUrlParam, accIdParam, dateParam, rankParam, pointsParam, recruitsParam, bloodtypeParam, catchphraseParam, usernameParam, idTypeParam);
        }

    public async Task GiveCell(SocketSlashCommand command) {
        
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        
        var cmd = connection.CreateCommand();
        
        cmd.ExecuteNonQuery();
        
        await command.FollowupAsync("Probably done.");

        foreach (var enlisted in GetEnlisted()) {
            
            await GiveNewCase(ulong.Parse(enlisted), "BLACK");
            await GiveNewCharm(ulong.Parse(enlisted), "NONE");
            await GiveNewWallpaper(ulong.Parse(enlisted), "BASIC");
        }
    }
    
// UNENROLL CHECK COMMANDS.
    
    /**
     * For use when checking if someone was previously enrolled.
     */
    
    public async Task<string> UnenrolledExists(ulong userId, DiscordSocketClient client) {
    
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(1) FROM Unenrolled WHERE UserId = $id;";
        command.Parameters.AddWithValue("$id", userId.ToString());

        var result = await command.ExecuteScalarAsync();
        var ans = Convert.ToInt32(result) > 0;

        if (ans) {
            var user = client.GetUser(userId) as SocketGuildUser;
            if (user != null) {
                await user.AddRoleAsync(1473369036766052445);
                await TransferFromUnenrolledToEnrolled(userId, client);

                return "This user was " + await GetUClaim(userId) + "before, had ***" + await GetUPoints(userId) +
                       "*** points, and was ranked ***" + await GetURank(userId) +
                       "***.\nRanks, points, automatically, anything else can be done by the enlistee / staff.";
            }
        }
        return "This user was not enlisted fully!";
    }

    private async Task<int> GetUPoints(ulong userId) {
        
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT Points FROM Unenrolled WHERE UserId = $id;";
        command.Parameters.AddWithValue("$id", userId.ToString());

        var result = await command.ExecuteScalarAsync();
        return result != null ? Convert.ToInt32(result) : 0;
    }

    public async Task TransferFromEnrolledToUnenrolled(ulong userId) {

        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO Unenrolled (UserId, Claim, Rank, Points, Username) SELECT UserId, Claim, Rank, Points, Username FROM Enrolled WHERE UserId = $id;";
        command.Parameters.AddWithValue("$id", userId.ToString());
        await command.ExecuteNonQueryAsync();
        
        var command2 = connection.CreateCommand();
        command2.CommandText = "DELETE FROM Enrolled WHERE UserId = $id;";
        command2.Parameters.AddWithValue("$id", userId.ToString());
        await command2.ExecuteNonQueryAsync();
    }

    private async Task TransferFromUnenrolledToEnrolled(ulong userId, DiscordSocketClient client) {
        
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO Enrolled (UserId, Claim, Rank, Points, Username) SELECT UserId, Claim, Rank, Points, Username FROM Unenrolled WHERE UserId = $id;";
        command.Parameters.AddWithValue("$id", userId.ToString());
        await command.ExecuteNonQueryAsync();

        await TransferURankToERank(userId, await GetURank(userId), client);
    }

    private async Task TransferURankToERank(ulong userId, string uRank, DiscordSocketClient client) {
        
        var student = client.GetUser(userId) as SocketGuildUser;

        if (student != null) {
            if (uRank.Equals("Nitō Shi"))
                await student.AddRoleAsync(1475886748268625962);
            else if (uRank.Equals("Ittō Shi"))
                await student.AddRoleAsync(1475886729561899212);
            else if (uRank.Equals("Shichō"))
                await student.AddRoleAsync(1475886715368509753);
            else if (uRank.Equals("Santō Sō"))
                await student.AddRoleAsync(1475886697118957660);
            else if (uRank.Equals("Nitō Sō"))
                await student.AddRoleAsync(1475886671919579310);
            else if (uRank.Equals("Ittō Sō"))
                await student.AddRoleAsync(1475886657545961472);
            else
                await student.AddRoleAsync(1475886640429011125);
        }
    }

    public async Task Remove(ulong userId) {
        
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        
        var command = connection.CreateCommand();
        command.CommandText = @"DELETE FROM Enrolled WHERE UserId = $id;
                                DELETE FROM Id WHERE UserId = $id;";
        command.Parameters.AddWithValue("$id", userId.ToString());
        await command.ExecuteNonQueryAsync();
    }

    public async Task<int> GetPoints(ulong userId) {
        
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT Points FROM Enrolled WHERE UserId = $id;";
        command.Parameters.AddWithValue("$id", userId.ToString());

        var result = await command.ExecuteScalarAsync();
        return result != null ? Convert.ToInt32(result) : 0;
    }

    public async Task<int> AddPoints(ulong userId, int points) {
        
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        
        var command = connection.CreateCommand();
        command.CommandText = "UPDATE Enrolled SET Points = Points + $points WHERE UserId = $id;";
        command.Parameters.AddWithValue("$id", userId.ToString());
        command.Parameters.AddWithValue("$points", points);
        
        return await command.ExecuteNonQueryAsync();
    }

    public async Task<int> RemovePoints(ulong userId, int points) {
        
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        
        var command = connection.CreateCommand();
        command.CommandText = "UPDATE Enrolled SET Points = Points - $points WHERE UserId = $id;";
        command.Parameters.AddWithValue("$id", userId.ToString());
        command.Parameters.AddWithValue("$points", points);
        await command.ExecuteNonQueryAsync();

        return await Underflow(userId);
    }
    
    public async Task<int> GetRecruits(ulong userId) {
        
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT Recruits FROM Enrolled WHERE UserId = $id;";
        command.Parameters.AddWithValue("$id", userId.ToString());

        var result = await command.ExecuteScalarAsync();
        return result != null ? Convert.ToInt32(result) : 0;
    }

    public async Task<int> AddRecruits(ulong userId, int recruits) {
        
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        
        var command = connection.CreateCommand();
        command.CommandText = "UPDATE Enrolled SET Recruits = Recruits + $recruits WHERE UserId = $id;";
        command.Parameters.AddWithValue("$id", userId.ToString());
        command.Parameters.AddWithValue("$recruits", recruits);
        
        return await command.ExecuteNonQueryAsync();
    }
    
    public async Task<int> RemoveRecruits(ulong userId, int recruits) {
        
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        
        var command = connection.CreateCommand();
        command.CommandText = "UPDATE Enrolled SET Recruits = Recruits - $recruits WHERE UserId = $id;";
        command.Parameters.AddWithValue("$id", userId.ToString());
        command.Parameters.AddWithValue("$recruits", recruits);
        await command.ExecuteNonQueryAsync();

        return await UnderflowRecruits(userId);
    }

    private async Task<int> Underflow(ulong userId) {
        
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(1) FROM Enrolled WHERE UserId = $id AND Points < 0;";
        command.Parameters.AddWithValue("$id", userId.ToString());
        
        var result = await command.ExecuteScalarAsync();
        var ans = Convert.ToInt32(result) > 0;

        if (ans) {
            var command2 = connection.CreateCommand();
            command2.CommandText = "UPDATE Enrolled SET Points = 0 WHERE UserId = $id;";
            command2.Parameters.AddWithValue("$id", userId.ToString());
            return await command2.ExecuteNonQueryAsync();
        }
        
        return -1;
    }
    
    private async Task<int> UnderflowRecruits(ulong userId) {
        
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(1) FROM Enrolled WHERE UserId = $id AND Recruits < 0;";
        command.Parameters.AddWithValue("$id", userId.ToString());
        
        var result = await command.ExecuteScalarAsync();
        var ans = Convert.ToInt32(result) > 0;

        if (ans) {
            var command2 = connection.CreateCommand();
            command2.CommandText = "UPDATE Enrolled SET Recruits = 0 WHERE UserId = $id;";
            command2.Parameters.AddWithValue("$id", userId.ToString());
            return await command2.ExecuteNonQueryAsync();
        }
        
        return -1;
    }
    
    private async Task<int> UnderflowYen(ulong userId) {
        
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(1) FROM WorkCell WHERE UserId = $id AND Yen < 0;";
        command.Parameters.AddWithValue("$id", userId.ToString());
        
        var result = await command.ExecuteScalarAsync();
        var ans = Convert.ToInt32(result) > 0;

        if (ans) {
            var command2 = connection.CreateCommand();
            command2.CommandText = "UPDATE WorkCell SET Yen = 0 WHERE UserId = $id;";
            command2.Parameters.AddWithValue("$id", userId.ToString());
            return await command2.ExecuteNonQueryAsync();
        }
        
        return -1;
    }

    public async Task<string> GetClaim(ulong userId) {
        
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT Claim FROM Enrolled WHERE UserId = $id;";
        command.Parameters.AddWithValue("$id", userId.ToString());
        
        var result = await command.ExecuteScalarAsync();
        return Convert.ToString(result) ?? "";
    }

    public async Task<int> SetClaim(ulong userId, string claim) {
        
        if (String.IsNullOrEmpty(claim)) return -1;
        
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = "UPDATE Enrolled SET Claim = $claim WHERE UserId = $id;";
        command.Parameters.AddWithValue("$id", userId.ToString());
        command.Parameters.AddWithValue("$claim", claim);
        
        return await command.ExecuteNonQueryAsync();
    }

    public async Task<string> GetAvatarUrl(ulong userId) {
        
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT AvatarUrl FROM Enrolled WHERE UserId = $id;";
        command.Parameters.AddWithValue("$id", userId.ToString());
        
        var result = await command.ExecuteScalarAsync();
        return Convert.ToString(result) ?? "";
    }

    public async Task<int> SetAvatarUrl(ulong userId, string avatarUrl) {
        
        if (String.IsNullOrEmpty(avatarUrl)) return -1;
        
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = "UPDATE Enrolled SET AvatarUrl = $avatarUrl WHERE UserId = $id;";
        command.Parameters.AddWithValue("$id", userId.ToString());
        command.Parameters.AddWithValue("$avatarUrl", avatarUrl);
        
        return await command.ExecuteNonQueryAsync();
    }

    public async Task<string> GetRank(ulong userId) {
        
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT Rank FROM Enrolled WHERE UserId = $id;";
        command.Parameters.AddWithValue("$id", userId.ToString());
        
        var result = await command.ExecuteScalarAsync();
        return Convert.ToString(result) ?? "";
    }

    public async Task<int> SetRank(ulong userId, string rank) {
        
        if (String.IsNullOrEmpty(rank)) return -1;
        
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = "UPDATE Enrolled SET Rank = $rank WHERE UserId = $id;";
        command.Parameters.AddWithValue("$id", userId.ToString());
        command.Parameters.AddWithValue("$rank", rank);
        
        return await command.ExecuteNonQueryAsync();
    }

    public async Task<string> GetBloodtype(ulong userId) {
        
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT Bloodtype FROM Enrolled WHERE UserId = $id;";
        command.Parameters.AddWithValue("$id", userId.ToString());
        
        var result = await command.ExecuteScalarAsync();
        return Convert.ToString(result) ?? "";
    }

    public async Task<int> SetBloodtype(ulong userId, string bloodtype) {
        
        if (String.IsNullOrEmpty(bloodtype)) return -1;
        
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = "UPDATE Enrolled SET Bloodtype = $bloodtype WHERE UserId = $id;";
        command.Parameters.AddWithValue("$id", userId.ToString());
        command.Parameters.AddWithValue("$bloodtype", bloodtype);
        
        return await command.ExecuteNonQueryAsync();
    }
    
    public async Task<string> GetIdType(ulong userId) {
        
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT IDType FROM Enrolled WHERE UserId = $id;";
        command.Parameters.AddWithValue("$id", userId.ToString());
        
        var result = await command.ExecuteScalarAsync();
        return Convert.ToString(result) ?? "";
    }

    public async Task<int> SetIdType(ulong userId, string idType) {
        
        if (String.IsNullOrEmpty(idType)) return -1;
        
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = "UPDATE Enrolled SET IDType = $idType WHERE UserId = $id;";
        command.Parameters.AddWithValue("$id", userId.ToString());
        command.Parameters.AddWithValue("$idType", idType);
        
        return await command.ExecuteNonQueryAsync();
    }
    
    public async Task<string> GetKoNotes(ulong userId) {
        
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT KoNotes FROM Enrolled WHERE UserId = $id;";
        command.Parameters.AddWithValue("$id", userId.ToString());
        
        var result = await command.ExecuteScalarAsync();
        return Convert.ToString(result) ?? "";
    }

    public async Task<int> SetKoNotes(ulong userId, string koNotes) {
        
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = "UPDATE Enrolled SET KoNotes = $koNotes WHERE UserId = $id;";
        command.Parameters.AddWithValue("$id", userId.ToString());
        command.Parameters.AddWithValue("koNotes", koNotes);
        
        return await command.ExecuteNonQueryAsync();
    }

    public async Task<string> GetUsername(ulong userId) {
        
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT Username FROM Enrolled WHERE UserId = $id;";
        command.Parameters.AddWithValue("$id", userId.ToString());
        
        var result = await command.ExecuteScalarAsync();
        return Convert.ToString(result) ?? "";
    }

    public async Task<int> SetUsername(ulong userId, string username) {
        
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = "UPDATE Enrolled SET Username = $username WHERE UserId = $id;";
        command.Parameters.AddWithValue("$id", userId.ToString());
        command.Parameters.AddWithValue("$username", username);
        
        return await command.ExecuteNonQueryAsync();
    }
    
    public List<string> GetEnlisted() {
        using var connection = new SqliteConnection(_connectionString);
        connection.OpenAsync();

        var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT UserId FROM Enrolled ORDER BY Points DESC;";

        var userIds = new List<string>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read()) {
            userIds.Add(reader.GetString(0));
        }

        return userIds;
    }
    
    public List<(string UserId, string Claim)> GetAllClaims() {
        using var connection = new SqliteConnection(_connectionString);
        connection.OpenAsync();

        var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT UserId, Claim FROM Enrolled;";

        var results = new List<(string UserId, string Claim)>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read()) {
            results.Add((reader.GetString(0), reader.IsDBNull(1) ? "" : reader.GetString(1)));
        }

        return results;
    }

    public async Task<string> GetUClaim(ulong userId) {
        
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT Claim FROM Unenrolled WHERE UserId = $id;";
        command.Parameters.AddWithValue("$id", userId.ToString());
        
        var result = await command.ExecuteScalarAsync();
        return Convert.ToString(result) ?? "";
    }

    public async Task<string> GetURank(ulong userId) {
        
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT Rank FROM Unenrolled WHERE UserId = $id;";
        command.Parameters.AddWithValue("$id", userId.ToString());
        
        var result = await command.ExecuteScalarAsync();
        return Convert.ToString(result) ?? "";
    }

    public async Task<string> GetUUsername(ulong userId) {
        
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT Username FROM Unenrolled WHERE UserId = $id;";
        command.Parameters.AddWithValue("$id", userId.ToString());
        
        var result = await command.ExecuteScalarAsync();
        return Convert.ToString(result) ?? "";
    }
    
// STARBOARD.

    public string? GetStarboardMessageId(ulong originalMessageId) {
        using var connection = new SqliteConnection(_connectionString);
        connection.OpenAsync();
    
        var command = connection.CreateCommand();
        command.CommandText = "SELECT StarboardId FROM Starboard WHERE OriginalId = $id";
        command.Parameters.AddWithValue("$id", originalMessageId.ToString());
    
        var result = command.ExecuteScalar();
        return result is not null ? (string)result : null;
    }

    public void SaveStarboardMessageId(ulong originalMessageId, ulong starboardMessageId) {
        using var connection = new SqliteConnection(_connectionString);
        connection.OpenAsync();
    
        var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO Starboard (OriginalId, StarboardId) VALUES ($originalId, $starboardId)
            ON CONFLICT(OriginalId) DO UPDATE SET StarboardId = $starboardId";
        command.Parameters.AddWithValue("$originalId", originalMessageId.ToString());
        command.Parameters.AddWithValue("$starboardId", starboardMessageId.ToString());
        command.ExecuteNonQuery();
    }
    
    public void DeleteStarboardEntry(ulong originalMessageId) {
        using var connection = new SqliteConnection(_connectionString);
        connection.OpenAsync();
    
        var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Starboard WHERE OriginalId = $id";
        command.Parameters.AddWithValue("$id", originalMessageId.ToString());
        command.ExecuteNonQuery();
    }
    
    public List<(string UserId, string Username, int Points)> GetLeaderboard() {
        using var connection = new SqliteConnection(_connectionString);
        connection.OpenAsync();
    
        var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT UserId, Username, Points FROM Enrolled ORDER BY Points DESC;";
    
        var results = new List<(string, string, int)>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read()) {
            results.Add((reader.GetString(0), reader.GetString(1), reader.GetInt32(2)));
        }
        return results;
    }
    
    
// SERVER STATS

    public ulong? GetStatChannel(ulong guildId) {
        using var connection = new SqliteConnection(_connectionString);
        connection.OpenAsync();

        var cmd = connection.CreateCommand();
        cmd.CommandText = @"SELECT ChannelId FROM StatChannel WHERE GuildId = $guildId;";
        cmd.Parameters.AddWithValue("$guildId", guildId.ToString());

        var result = cmd.ExecuteScalar();
        return result is string id ? ulong.Parse(id) : null;
    }

    public void SetStatChannel(ulong guildId, ulong channelId) {
        using var connection = new SqliteConnection(_connectionString);
        connection.OpenAsync();

        var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO StatChannel (GuildId, ChannelId) VALUES ($guildId, $channelId)
            ON CONFLICT(GuildId) DO UPDATE SET ChannelId = $channelId;";
        cmd.Parameters.AddWithValue("$guildId", guildId.ToString());
        cmd.Parameters.AddWithValue("$channelId", channelId.ToString());

        cmd.ExecuteNonQuery();
    }
    
// ADDONS
    
    public async Task<int> SetIsProspect(ulong userId, bool hit) {
        
        await using var connection = new SqliteConnection(_connectionString);
        connection.OpenAsync();
        var command = connection.CreateCommand();
        command.CommandText = @"INSERT INTO Addons (UserId, IsProspect) VALUES ($id, $hit)
                                ON CONFLICT(UserId) DO UPDATE SET IsProspect = $hit";
        command.Parameters.AddWithValue("$id", userId.ToString());
        command.Parameters.AddWithValue("$hit", hit ? 1 : 0);
        
        return await command.ExecuteNonQueryAsync();
    }

    public async Task<bool> GetIsProspect(ulong userId) {
        
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT IsProspect FROM Addons WHERE UserId = $id;";
        command.Parameters.AddWithValue("$id", userId.ToString());

        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt32(result) > 0;
    }
    
    public async Task SetAvatarImage(ulong userId, byte[] imageBytes) {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = "UPDATE Enrolled SET AvatarImage = $img WHERE UserId = $id;";
        command.Parameters.AddWithValue("$id", userId.ToString());
        command.Parameters.AddWithValue("$img", imageBytes);

        await command.ExecuteNonQueryAsync();
    }

    public async Task<byte[]?> GetAvatarImage(ulong userId) {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT AvatarImage FROM Enrolled WHERE UserId = $id;";
        command.Parameters.AddWithValue("$id", userId.ToString());

        var result = await command.ExecuteScalarAsync();
        return result is byte[] bytes ? bytes : null;
    }
    
    public async Task<int> SetIsEnlisted(ulong userId, bool hit) {
        
        await using var connection = new SqliteConnection(_connectionString);
        connection.OpenAsync();
        var command = connection.CreateCommand();
        command.CommandText = @"INSERT INTO Addons (UserId, IsEnlisted) VALUES ($id, $hit)
                                ON CONFLICT(UserId) DO UPDATE SET IsEnlisted = $hit";
        command.Parameters.AddWithValue("$id", userId.ToString());
        command.Parameters.AddWithValue("$hit", hit ? 1 : 0);
        
        return await command.ExecuteNonQueryAsync();
    }

    public async Task<bool> GetIsEnlisted(ulong userId) {
        
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT IsEnlisted FROM Addons WHERE UserId = $id;";
        command.Parameters.AddWithValue("$id", userId.ToString());

        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt32(result) > 0;
    }
    
    public async Task<int> SetIsPartner(ulong userId, bool hit) {
        
        await using var connection = new SqliteConnection(_connectionString);
        connection.OpenAsync();
        var command = connection.CreateCommand();
        command.CommandText = @"INSERT INTO Addons (UserId, IsPartner) VALUES ($id, $hit)
                                ON CONFLICT(UserId) DO UPDATE SET IsPartner = $hit";
        command.Parameters.AddWithValue("$id", userId.ToString());
        command.Parameters.AddWithValue("$hit", hit ? 1 : 0);
        
        return await command.ExecuteNonQueryAsync();
    }

    public async Task<bool> GetIsPartner(ulong userId) {
        
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT IsPartner FROM Addons WHERE UserId = $id;";
        command.Parameters.AddWithValue("$id", userId.ToString());

        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt32(result) > 0;
    }
    
    public async Task<int> SetIsCivilian(ulong userId, bool hit) {
        
        await using var connection = new SqliteConnection(_connectionString);
        connection.OpenAsync();
        var command = connection.CreateCommand();
        command.CommandText = @"INSERT INTO Addons (UserId, IsCivilian) VALUES ($id, $hit)
                                ON CONFLICT(UserId) DO UPDATE SET IsCivilian = $hit";
        command.Parameters.AddWithValue("$id", userId.ToString());
        command.Parameters.AddWithValue("$hit", hit ? 1 : 0);
        
        return await command.ExecuteNonQueryAsync();
    }

    public async Task<bool> GetIsCivilian(ulong userId) {
        
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT IsCivilian FROM Addons WHERE UserId = $id;";
        command.Parameters.AddWithValue("$id", userId.ToString());

        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt32(result) > 0;
    }
    
    public async Task<int> SetIsFan(ulong userId, bool hit) {
        
        await using var connection = new SqliteConnection(_connectionString);
        connection.OpenAsync();
        
        var command = connection.CreateCommand();
        command.CommandText = @"INSERT INTO Addons (UserId, IsFan) VALUES ($id, $hit)
                                ON CONFLICT(UserId) DO UPDATE SET IsFan = $hit";
        command.Parameters.AddWithValue("$id", userId.ToString());
        command.Parameters.AddWithValue("$hit", hit ? 1 : 0);
        
        return await command.ExecuteNonQueryAsync();
    }

    public async Task<bool> GetIsFan(ulong userId) {
        
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT IsFan FROM Addons WHERE UserId = $id;";
        command.Parameters.AddWithValue("$id", userId.ToString());

        var result = await command.ExecuteScalarAsync();
        return Convert.ToInt32(result) > 0;
    }
    
    // IDS

    public async Task<List<string>> GetIds(ulong userId) {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT Collected FROM Id WHERE UserId = $id;";
        command.Parameters.AddWithValue("$id", userId.ToString());

        var result = await command.ExecuteScalarAsync();
    
        if (result == null || result == DBNull.Value)
            return new List<string>();

        return JsonSerializer.Deserialize<List<string>>((string)result) ?? new List<string>();
    }
    
    public async Task<List<string>> GetFrames(ulong userId) {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT Frames FROM Id WHERE UserId = $id;";
        command.Parameters.AddWithValue("$id", userId.ToString());

        var result = await command.ExecuteScalarAsync();
    
        if (result == null || result == DBNull.Value)
            return new List<string>();

        return JsonSerializer.Deserialize<List<string>>((string)result) ?? new List<string>();
    }

    public async Task GiveNewId(ulong userId, string id) {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var existing = await GetIds(userId);
        existing.Add(id);
    
        var json = JsonSerializer.Serialize(existing);

        var command = connection.CreateCommand();
        command.CommandText = """
                              INSERT INTO Id (UserId, Collected)
                              VALUES ($id, $collected)
                              ON CONFLICT(UserId) DO UPDATE SET Collected = $collected;
                              """;
        command.Parameters.AddWithValue("$id", userId.ToString());
        command.Parameters.AddWithValue("$collected", json);

        await command.ExecuteNonQueryAsync();
    }
    
    public async Task RemoveId(ulong userId, string id) {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var existing = await GetIds(userId);
        existing.Remove(id);
    
        var json = JsonSerializer.Serialize(existing);

        var command = connection.CreateCommand();
        command.CommandText = """
                              INSERT INTO Id (UserId, Collected)
                              VALUES ($id, $collected)
                              ON CONFLICT(UserId) DO UPDATE SET Collected = $collected;
                              """;
        command.Parameters.AddWithValue("$id", userId.ToString());
        command.Parameters.AddWithValue("$collected", json);

        await command.ExecuteNonQueryAsync();
    }
    
    public async Task GiveNewFrame(ulong userId, string frame) {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var existing = await GetFrames(userId);
        existing.Add(frame);
    
        var json = JsonSerializer.Serialize(existing);

        var command = connection.CreateCommand();
        command.CommandText = """
                              INSERT INTO Id (UserId, Frames)
                              VALUES ($id, $frame)
                              ON CONFLICT(UserId) DO UPDATE SET Frames = $frame;
                              """;
        command.Parameters.AddWithValue("$id", userId.ToString());
        command.Parameters.AddWithValue("$frame", json);

        await command.ExecuteNonQueryAsync();
    }
    
    public async Task RemoveFrame(ulong userId, string frame) {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var existing = await GetFrames(userId);
        existing.Remove(frame);
    
        var json = JsonSerializer.Serialize(existing);

        var command = connection.CreateCommand();
        command.CommandText = """
                              INSERT INTO Id (UserId, Frames)
                              VALUES ($id, $frame)
                              ON CONFLICT(UserId) DO UPDATE SET Frames = $frame;
                              """;
        command.Parameters.AddWithValue("$id", userId.ToString());
        command.Parameters.AddWithValue("$frame", json);

        await command.ExecuteNonQueryAsync();
    }
    
    
    
    public async Task<int> AddItem(string item, int cost) {
        
        await using var connection = new SqliteConnection(_connectionString);
        connection.OpenAsync();
        
        var command = connection.CreateCommand();
        command.CommandText = @"INSERT INTO Shop (Item, Cost) VALUES ($item, $cost)
                                ON CONFLICT(Item) DO UPDATE SET Cost = $cost";
        command.Parameters.AddWithValue("$item", item);
        command.Parameters.AddWithValue("$cost", cost);
        
        return await command.ExecuteNonQueryAsync();
    }

    public List<(string Item, int Cost)> GetItem(string item) {
        using var connection = new SqliteConnection(_connectionString);
        connection.OpenAsync();

        var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT Item, Cost FROM Shop WHERE Item = $item;";
        cmd.Parameters.AddWithValue("$item", item);

        var results = new List<(string Item, int Cost)>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read()) {
            results.Add((reader.GetString(0), reader.IsDBNull(1) ? 0 : reader.GetInt32(1)));
        }

        return results;
    }

    public List<(string Item, int Cost)> GetAllItems() {
        using var connection = new SqliteConnection(_connectionString);
        connection.OpenAsync();

        var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT Item, Cost FROM Shop;";

        var results = new List<(string Item, int Cost)>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read()) {
            results.Add((reader.GetString(0), reader.IsDBNull(1) ? 0 : reader.GetInt32(1)));
        }

        return results;
    }
    
    public async Task RemoveItem(string item) {
        
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        
        var command = connection.CreateCommand();
        command.CommandText = @"DELETE FROM Shop WHERE Item = $item;";
        command.Parameters.AddWithValue("$item", item);
        await command.ExecuteNonQueryAsync();
    }

    public async Task<int> GetYen(ulong userId) {
        
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT Yen FROM WorkCell WHERE UserId = $id;";
        command.Parameters.AddWithValue("$id", userId.ToString());

        var result = await command.ExecuteScalarAsync();
        return result != null ? Convert.ToInt32(result) : 0;
    }

    public async Task<int> AddYen(ulong userId, int yen) {
        
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        
        var command = connection.CreateCommand();
        command.CommandText = "UPDATE WorkCell SET Yen = Yen + $yen WHERE UserId = $id;";
        command.Parameters.AddWithValue("$id", userId.ToString());
        command.Parameters.AddWithValue("$yen", yen);
        
        return await command.ExecuteNonQueryAsync();
    }

    public async Task<int> RemoveYen(ulong userId, int yen) {
        
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        
        var command = connection.CreateCommand();
        command.CommandText = "UPDATE WorkCell SET Yen = Yen - $yen WHERE UserId = $id;";
        command.Parameters.AddWithValue("$id", userId.ToString());
        command.Parameters.AddWithValue("$yen", yen);
        await command.ExecuteNonQueryAsync();

        return await UnderflowYen(userId);
    }
    
    public async Task<List<string>> GetCases(ulong userId) {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT Cases FROM WorkCell WHERE UserId = $id;";
        command.Parameters.AddWithValue("$id", userId.ToString());

        var result = await command.ExecuteScalarAsync();
    
        if (result == null || result == DBNull.Value)
            return new List<string>();

        return JsonSerializer.Deserialize<List<string>>((string)result) ?? new List<string>();
    }
    
    public async Task GiveNewCase(ulong userId, string cellCase) {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var existing = await GetCases(userId);
        existing.Add(cellCase);
    
        var json = JsonSerializer.Serialize(existing);

        var command = connection.CreateCommand();
        command.CommandText = """
                              INSERT INTO WorkCell (UserId, Cases)
                              VALUES ($id, $cases)
                              ON CONFLICT(UserId) DO UPDATE SET Cases = $cases;
                              """;
        command.Parameters.AddWithValue("$id", userId.ToString());
        command.Parameters.AddWithValue("$cases", json);

        await command.ExecuteNonQueryAsync();
    }
    
    public async Task RemoveCase(ulong userId, string cellCase) {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var existing = await GetCases(userId);
        existing.Remove(cellCase);
    
        var json = JsonSerializer.Serialize(existing);

        var command = connection.CreateCommand();
        command.CommandText = """
                              INSERT INTO WorkCell (UserId, Cases)
                              VALUES ($id, $cases)
                              ON CONFLICT(UserId) DO UPDATE SET Cases = $cases;
                              """;
        command.Parameters.AddWithValue("$id", userId.ToString());
        command.Parameters.AddWithValue("$cases", json);

        await command.ExecuteNonQueryAsync();
    }
    
    public async Task<List<string>> GetWallpapers(ulong userId) {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT Wallpapers FROM WorkCell WHERE UserId = $id;";
        command.Parameters.AddWithValue("$id", userId.ToString());

        var result = await command.ExecuteScalarAsync();
    
        if (result == null || result == DBNull.Value)
            return new List<string>();

        return JsonSerializer.Deserialize<List<string>>((string)result) ?? new List<string>();
    }
    
    public async Task GiveNewWallpaper(ulong userId, string wallpaper) {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var existing = await GetWallpapers(userId);
        existing.Add(wallpaper);
    
        var json = JsonSerializer.Serialize(existing);

        var command = connection.CreateCommand();
        command.CommandText = """
                              INSERT INTO WorkCell (UserId, Wallpapers)
                              VALUES ($id, $wallpapers)
                              ON CONFLICT(UserId) DO UPDATE SET Wallpapers = $wallpapers;
                              """;
        command.Parameters.AddWithValue("$id", userId.ToString());
        command.Parameters.AddWithValue("$wallpapers", json);

        await command.ExecuteNonQueryAsync();
    }
    
    public async Task RemoveWallpaper(ulong userId, string wallpaper) {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var existing = await GetWallpapers(userId);
        existing.Remove(wallpaper);
    
        var json = JsonSerializer.Serialize(existing);

        var command = connection.CreateCommand();
        command.CommandText = """
                              INSERT INTO WorkCell (UserId, Wallpapers)
                              VALUES ($id, $wallpapers)
                              ON CONFLICT(UserId) DO UPDATE SET Wallpapers = $wallpapers;
                              """;
        command.Parameters.AddWithValue("$id", userId.ToString());
        command.Parameters.AddWithValue("$wallpapers", json);

        await command.ExecuteNonQueryAsync();
    }
    
    public async Task<List<string>> GetCharms(ulong userId) {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT Charms FROM WorkCell WHERE UserId = $id;";
        command.Parameters.AddWithValue("$id", userId.ToString());

        var result = await command.ExecuteScalarAsync();
    
        if (result == null || result == DBNull.Value)
            return new List<string>();

        return JsonSerializer.Deserialize<List<string>>((string)result) ?? new List<string>();
    }
    
    public async Task GiveNewCharm(ulong userId, string charm) {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var existing = await GetCharms(userId);
        existing.Add(charm);
    
        var json = JsonSerializer.Serialize(existing);

        var command = connection.CreateCommand();
        command.CommandText = """
                              INSERT INTO WorkCell (UserId, Charms)
                              VALUES ($id, $charms)
                              ON CONFLICT(UserId) DO UPDATE SET Charms = $charms;
                              """;
        command.Parameters.AddWithValue("$id", userId.ToString());
        command.Parameters.AddWithValue("$charms", json);

        await command.ExecuteNonQueryAsync();
    }
    
    public async Task RemoveCharm(ulong userId, string charm) {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var existing = await GetCharms(userId);
        existing.Remove(charm);
    
        var json = JsonSerializer.Serialize(existing);

        var command = connection.CreateCommand();
        command.CommandText = """
                              INSERT INTO WorkCell (UserId, Charms)
                              VALUES ($id, $charms)
                              ON CONFLICT(UserId) DO UPDATE SET Charms = $charms;
                              """;
        command.Parameters.AddWithValue("$id", userId.ToString());
        command.Parameters.AddWithValue("$charms", json);

        await command.ExecuteNonQueryAsync();
    }
    
    public async Task<List<string>> GetApps(ulong userId) {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT Apps FROM WorkCell WHERE UserId = $id;";
        command.Parameters.AddWithValue("$id", userId.ToString());

        var result = await command.ExecuteScalarAsync();
    
        if (result == null || result == DBNull.Value)
            return new List<string>();

        return JsonSerializer.Deserialize<List<string>>((string)result) ?? new List<string>();
    }
    
    public async Task AddAppsToHome(ulong userId, string app) {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var existing = await GetApps(userId);
        existing.Add(app);
    
        var json = JsonSerializer.Serialize(existing);

        var command = connection.CreateCommand();
        command.CommandText = """
                              INSERT INTO WorkCell (UserId, Apps)
                              VALUES ($id, $apps)
                              ON CONFLICT(UserId) DO UPDATE SET Apps = $apps;
                              """;
        command.Parameters.AddWithValue("$id", userId.ToString());
        command.Parameters.AddWithValue("$apps", json);

        await command.ExecuteNonQueryAsync();
    }
    
    public async Task RemoveAppsFromHome(ulong userId, string app) {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var existing = await GetApps(userId);
        existing.Remove(app);
    
        var json = JsonSerializer.Serialize(existing);

        var command = connection.CreateCommand();
        command.CommandText = """
                              INSERT INTO WorkCell (UserId, Apps)
                              VALUES ($id, $apps)
                              ON CONFLICT(UserId) DO UPDATE SET Apps = $apps;
                              """;
        command.Parameters.AddWithValue("$id", userId.ToString());
        command.Parameters.AddWithValue("$apps", json);

        await command.ExecuteNonQueryAsync();
    }
    
    public async Task<string> GetCharmType(ulong userId) {
        
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT CharmType FROM WorkCell WHERE UserId = $id;";
        command.Parameters.AddWithValue("$id", userId.ToString());
        
        var result = await command.ExecuteScalarAsync();
        return Convert.ToString(result) ?? "";
    }

    public async Task<int> SetCharmType(ulong userId, string charmType) {
        
        if (String.IsNullOrEmpty(charmType)) return -1;
        
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = "UPDATE WorkCell SET CharmType = $charmType WHERE UserId = $id;";
        command.Parameters.AddWithValue("$id", userId.ToString());
        command.Parameters.AddWithValue("$charmType", charmType);
        
        return await command.ExecuteNonQueryAsync();
    }

    public async Task<string> GetCaseType(ulong userId) {
        
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT CaseType FROM WorkCell WHERE UserId = $id;";
        command.Parameters.AddWithValue("$id", userId.ToString());
        
        var result = await command.ExecuteScalarAsync();
        return Convert.ToString(result) ?? "";
    }

    public async Task<int> SetCaseType(ulong userId, string caseType) {
        
        if (String.IsNullOrEmpty(caseType)) return -1;
        
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = "UPDATE WorkCell SET CaseType = $caseType WHERE UserId = $id;";
        command.Parameters.AddWithValue("$id", userId.ToString());
        command.Parameters.AddWithValue("$caseType", caseType);
        
        return await command.ExecuteNonQueryAsync();
    }

    public async Task<string> GetWallpaperType(ulong userId) {
        
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT WallpaperType FROM WorkCell WHERE UserId = $id;";
        command.Parameters.AddWithValue("$id", userId.ToString());
        
        var result = await command.ExecuteScalarAsync();
        return Convert.ToString(result) ?? "";
    }

    public async Task<int> SetWallpaperType(ulong userId, string wallpaperType) {
        
        if (String.IsNullOrEmpty(wallpaperType)) return -1;
        
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = "UPDATE WorkCell SET WallpaperType = $wallpaperType WHERE UserId = $id;";
        command.Parameters.AddWithValue("$id", userId.ToString());
        command.Parameters.AddWithValue("$wallpaperType", wallpaperType);
        
        return await command.ExecuteNonQueryAsync();
    }
    
    public async Task<List<string>> GetCollectedApps(ulong userId) {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT Collected FROM Id WHERE UserId = $id;";
        command.Parameters.AddWithValue("$id", userId.ToString());

        var result = await command.ExecuteScalarAsync();
    
        if (result == null || result == DBNull.Value)
            return new List<string>();

        return JsonSerializer.Deserialize<List<string>>((string)result) ?? new List<string>();
    }
    public async Task GiveNewApp(ulong userId, string app) {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var existing = await GetCollectedApps(userId);
        existing.Add(app);
    
        var json = JsonSerializer.Serialize(existing);

        var command = connection.CreateCommand();
        command.CommandText = """
                              INSERT INTO WorkCell (UserId, Collected)
                              VALUES ($id, $apps)
                              ON CONFLICT(UserId) DO UPDATE SET Collected = $apps;
                              """;
        command.Parameters.AddWithValue("$id", userId.ToString());
        command.Parameters.AddWithValue("$apps", json);

        await command.ExecuteNonQueryAsync();
    }
    
    public async Task RemoveApp(ulong userId, string app) {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var existing = await GetCollectedApps(userId);
        existing.Remove(app);
    
        var json = JsonSerializer.Serialize(existing);

        var command = connection.CreateCommand();
        command.CommandText = """
                              INSERT INTO WorkCell (UserId, Collected)
                              VALUES ($id, $apps)
                              ON CONFLICT(UserId) DO UPDATE SET Collected = $apps;
                              """;
        command.Parameters.AddWithValue("$id", userId.ToString());
        command.Parameters.AddWithValue("$apps", json);

        await command.ExecuteNonQueryAsync();
    }
}