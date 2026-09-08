using System.Text.Json;
using Discord.WebSocket;
using Microsoft.Data.Sqlite;
using SMASSB.Commands;
using SMASSB.Models;

namespace SMASSB.ServiceHandlers;

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

            CREATE TABLE IF NOT EXISTS EnlistedHistory (
                UserId TEXT PRIMARY KEY,
                Claim TEXT,
                AvatarUrl TEXT NOT NULL,
                AvatarImage BLOB,
                Rank TEXT NOT NULL,
                Points INTEGER DEFAULT 0,
                Bloodtype TEXT NOT NULL,
                IDType TEXT NOT NULL,
                Yen INTEGER NOT NULL DEFAULT 0,
                Recruits INTEGER NOT NULL DEFAULT 0,
                Cases TEXT,
                CaseType TEXT,
                Wallpapers TEXT,
                WallpaperType TEXT,
                Charms TEXT,
                CharmType TEXT,
                Apps TEXT,
                AppsCollected TEXT,
                IdsCollected TEXT NOT NULL,
                Frames TEXT,
                FrameType TEXT
            );

            CREATE TABLE IF NOT EXISTS Id (
                UserId TEXT PRIMARY KEY,
                Collected TEXT NOT NULL,
                Frames TEXT
            );

            CREATE TABLE IF NOT EXISTS WorkCell (
                UserId TEXT PRIMARY KEY,
                Yen INTEGER NOT NULL DEFAULT 0,
                Cases TEXT,
                CaseType TEXT,
                Wallpapers TEXT,
                WallpaperType TEXT,
                Charms TEXT,
                CharmType TEXT,
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

            CREATE TABLE IF NOT EXISTS Starboard (
                OriginalId TEXT PRIMARY KEY,
                StarboardId TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS StatChannel (
                GuildId TEXT PRIMARY KEY,
                ChannelId TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS SystemState (
                Key TEXT PRIMARY KEY,
                Value TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS PendingGame (
                UserId TEXT PRIMARY KEY,
                Game TEXT NOT NULL,
                SetAt INTEGER NOT NULL
            );

            CREATE TABLE IF NOT EXISTS Task (
                TaskName TEXT PRIMARY KEY,
                AssignedId TEXT NOT NULL,
                AssigneeId TEXT NOT NULL,
                DateAssigned TEXT NOT NULL,
                Deadline TEXT NOT NULL,
                Description TEXT,
                Priority TEXT NOT NULL,
                Progress TEXT NOT NULL,
                ReminderStage TEXT NOT NULL DEFAULT 'NONE',
                LastOverdueReminderDate TEXT
            );
        ";
        
        command.ExecuteNonQuery();
        MigrateWorkCellTable(connection);
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
        
        await using var connection = new SqliteConnection(_connectionString);
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
        await SetCaseType(ulong.Parse(accIdParam), caseParam);
        await GiveNewCharm(ulong.Parse(accIdParam), charmParam);
        await SetCharmType(ulong.Parse(accIdParam), charmParam);
        await GiveNewWallpaper(ulong.Parse(accIdParam), wallpaperParam);
        await SetWallpaperType(ulong.Parse(accIdParam), wallpaperParam);
        var appsParam = await GetApps(ulong.Parse(accIdParam));
        var (currentWeekEarnings, _, percentChange, isIncrease) = await GetEarningsSummary(ulong.Parse(accIdParam));
        
        await IdSystem.BuildId(command, member, claimParam, null, avatarUrlParam, accIdParam, dateParam, rankParam, pointsParam, recruitsParam, bloodtypeParam, catchphraseParam, usernameParam, idTypeParam);
        await CellSystem.BuildCell(command, member, caseParam, charmParam, wallpaperParam, appsParam, await GetYen(ulong.Parse(accIdParam)), currentWeekEarnings, percentChange, isIncrease);
        await member.AddRoleAsync(1537202109336920096);
    }

    public async Task Remove(ulong userId) {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        
        var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO EnlistedHistory (UserId, Claim, AvatarUrl, AvatarImage, Rank, Points, Bloodtype, IDType, Yen, Recruits, Cases, CaseType, Wallpapers, WallpaperType, Charms, CharmType, Apps, AppsCollected, IdsCollected, Frames) 
            VALUES ($id, $claim, $avatarUrl, $avatarImage, $rank, $points, $bloodtype, $idtype, $yen, $recruits, $cases, $caseType, $wallpapers, $wallpaperType, $charms, $charmType, $apps, $appsCollected, $idsCollected, $frames)
            ON CONFLICT(UserId) DO UPDATE SET Claim = excluded.Claim, AvatarUrl = excluded.AvatarUrl, AvatarImage = excluded.AvatarImage,Rank = excluded.Rank,Points = excluded.Points,Bloodtype = excluded.Bloodtype,IDType = excluded.IDType,Yen = excluded.Yen,Recruits = excluded.Recruits,Cases = excluded.Cases,CaseType = excluded.CaseType,Wallpapers = excluded.Wallpapers,WallpaperType = excluded.WallpaperType, Charms = excluded.Charms, CharmType = excluded.CharmType, Apps = excluded.Apps, AppsCollected = excluded.AppsCollected, IdsCollected = excluded.IdsCollected, Frames = excluded.Frames";
        
        object ToDbValue(object? value) => value ?? DBNull.Value;
        
        command.Parameters.AddWithValue("$id", userId.ToString());
        command.Parameters.AddWithValue("$claim", ToDbValue(await GetClaim(userId)));
        command.Parameters.AddWithValue("$avatarUrl", ToDbValue(await GetAvatarUrl(userId)));
        command.Parameters.AddWithValue("$avatarImage", ToDbValue(await GetAvatarImage(userId)));
        command.Parameters.AddWithValue("$rank", ToDbValue(await GetRank(userId)));
        command.Parameters.AddWithValue("$points", ToDbValue(await GetPoints(userId)));
        command.Parameters.AddWithValue("$bloodtype", ToDbValue(await GetBloodtype(userId)));
        command.Parameters.AddWithValue("$idtype", ToDbValue(await GetIdType(userId)));
        command.Parameters.AddWithValue("$yen", ToDbValue(await GetYen(userId)));
        command.Parameters.AddWithValue("$recruits", ToDbValue(await GetRecruits(userId)));
        command.Parameters.AddWithValue("$cases", ToDbValue(JsonSerializer.Serialize(await GetCases(userId))));
        command.Parameters.AddWithValue("$caseType", ToDbValue(await GetCaseType(userId)));
        command.Parameters.AddWithValue("$wallpapers", ToDbValue(JsonSerializer.Serialize(await GetWallpapers(userId))));
        command.Parameters.AddWithValue("$wallpaperType", ToDbValue(await GetWallpaperType(userId)));
        command.Parameters.AddWithValue("$charms", ToDbValue(JsonSerializer.Serialize(await GetCharms(userId))));
        command.Parameters.AddWithValue("$charmType", ToDbValue(await GetCharmType(userId)));
        command.Parameters.AddWithValue("$apps", ToDbValue(JsonSerializer.Serialize(await GetApps(userId))));
        command.Parameters.AddWithValue("$appsCollected", ToDbValue(JsonSerializer.Serialize(await GetCollectedApps(userId))));
        command.Parameters.AddWithValue("$idsCollected", ToDbValue(JsonSerializer.Serialize(await GetIds(userId))));
        command.Parameters.AddWithValue("$frames", ToDbValue(JsonSerializer.Serialize(await GetFrames(userId))));
        
        await command.ExecuteNonQueryAsync();
        
        var tables = new[] { "Enrolled", "Id", "WorkCell", "Addons", "PendingGame" };
    
        foreach (var table in tables) {
            var deleteCmd = connection.CreateCommand();
            deleteCmd.CommandText = $"DELETE FROM {table} WHERE UserId = $id;";
            deleteCmd.Parameters.AddWithValue("$id", userId.ToString());
            
            await deleteCmd.ExecuteNonQueryAsync();
        }
    }

    public async Task ReinstateEnlistment(ulong userId, DiscordSocketClient client) {
        // add everything into the correct database shit.
        // perform what preenlist does lol.
        // correct roles and shit lol
        // dm the person
    }

    public async Task<int> GetPoints(ulong userId) {
        
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT Points FROM Enrolled WHERE UserId = $id;";
        command.Parameters.AddWithValue("$id", userId.ToString());

        var result = await command.ExecuteScalarAsync();
        return result != null ? Convert.ToInt32(result) : 0;
    }

    public async Task<int> AddPoints(ulong userId, int points) {
        
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        
        var command = connection.CreateCommand();
        command.CommandText = "UPDATE Enrolled SET Points = Points + $points WHERE UserId = $id;";
        command.Parameters.AddWithValue("$id", userId.ToString());
        command.Parameters.AddWithValue("$points", points);
        
        return await command.ExecuteNonQueryAsync();
    }

    public async Task<int> RemovePoints(ulong userId, int points) {
        
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        
        var command = connection.CreateCommand();
        command.CommandText = "UPDATE Enrolled SET Points = Points - $points WHERE UserId = $id;";
        command.Parameters.AddWithValue("$id", userId.ToString());
        command.Parameters.AddWithValue("$points", points);
        await command.ExecuteNonQueryAsync();

        return await Underflow(userId);
    }
    
    public async Task<int> GetRecruits(ulong userId) {
        
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT Recruits FROM Enrolled WHERE UserId = $id;";
        command.Parameters.AddWithValue("$id", userId.ToString());

        var result = await command.ExecuteScalarAsync();
        return result != null ? Convert.ToInt32(result) : 0;
    }

    public async Task<int> AddRecruits(ulong userId, int recruits) {
        
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        
        var command = connection.CreateCommand();
        command.CommandText = "UPDATE Enrolled SET Recruits = Recruits + $recruits WHERE UserId = $id;";
        command.Parameters.AddWithValue("$id", userId.ToString());
        command.Parameters.AddWithValue("$recruits", recruits);
        
        return await command.ExecuteNonQueryAsync();
    }
    
    public async Task<int> RemoveRecruits(ulong userId, int recruits) {
        
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        
        var command = connection.CreateCommand();
        command.CommandText = "UPDATE Enrolled SET Recruits = Recruits - $recruits WHERE UserId = $id;";
        command.Parameters.AddWithValue("$id", userId.ToString());
        command.Parameters.AddWithValue("$recruits", recruits);
        await command.ExecuteNonQueryAsync();

        return await UnderflowRecruits(userId);
    }

    private async Task<int> Underflow(ulong userId) {
        
        await using var connection = new SqliteConnection(_connectionString);
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
        
        await using var connection = new SqliteConnection(_connectionString);
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
        
        await using var connection = new SqliteConnection(_connectionString);
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
        
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT Claim FROM Enrolled WHERE UserId = $id;";
        command.Parameters.AddWithValue("$id", userId.ToString());
        
        var result = await command.ExecuteScalarAsync();
        return Convert.ToString(result) ?? "";
    }

    public async Task<int> SetClaim(ulong userId, string claim) {
        
        if (String.IsNullOrEmpty(claim)) return -1;
        
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = "UPDATE Enrolled SET Claim = $claim WHERE UserId = $id;";
        command.Parameters.AddWithValue("$id", userId.ToString());
        command.Parameters.AddWithValue("$claim", claim);
        
        return await command.ExecuteNonQueryAsync();
    }

    public async Task<string> GetAvatarUrl(ulong userId) {
        
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT AvatarUrl FROM Enrolled WHERE UserId = $id;";
        command.Parameters.AddWithValue("$id", userId.ToString());
        
        var result = await command.ExecuteScalarAsync();
        return Convert.ToString(result) ?? "";
    }

    public async Task<int> SetAvatarUrl(ulong userId, string avatarUrl) {
        
        if (String.IsNullOrEmpty(avatarUrl)) return -1;
        
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = "UPDATE Enrolled SET AvatarUrl = $avatarUrl WHERE UserId = $id;";
        command.Parameters.AddWithValue("$id", userId.ToString());
        command.Parameters.AddWithValue("$avatarUrl", avatarUrl);
        
        return await command.ExecuteNonQueryAsync();
    }

    public async Task<string> GetRank(ulong userId) {
        
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT Rank FROM Enrolled WHERE UserId = $id;";
        command.Parameters.AddWithValue("$id", userId.ToString());
        
        var result = await command.ExecuteScalarAsync();
        return Convert.ToString(result) ?? "";
    }

    public async Task<int> SetRank(ulong userId, string rank) {
        
        if (String.IsNullOrEmpty(rank)) return -1;
        
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = "UPDATE Enrolled SET Rank = $rank WHERE UserId = $id;";
        command.Parameters.AddWithValue("$id", userId.ToString());
        command.Parameters.AddWithValue("$rank", rank);
        
        return await command.ExecuteNonQueryAsync();
    }

    public async Task<string> GetBloodtype(ulong userId) {
        
        await using var connection = new SqliteConnection(_connectionString);
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
        
        await using var connection = new SqliteConnection(_connectionString);
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

    public string GetStarboardMessageId(ulong originalMessageId) {
        using var connection = new SqliteConnection(_connectionString);
        connection.OpenAsync();
    
        var command = connection.CreateCommand();
        command.CommandText = "SELECT StarboardId FROM Starboard WHERE OriginalId = $id";
        command.Parameters.AddWithValue("$id", originalMessageId.ToString());
    
        var result = command.ExecuteScalar();
        return (string)result!;
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
        await connection.OpenAsync();
        
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
        await connection.OpenAsync();
        
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
        await connection.OpenAsync();
        
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
        await connection.OpenAsync();
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
        await connection.OpenAsync();
        
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
        await connection.OpenAsync();
        
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
        
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        
        var command = connection.CreateCommand();
        command.CommandText = @"DELETE FROM Shop WHERE Item = $item;";
        command.Parameters.AddWithValue("$item", item);
        await command.ExecuteNonQueryAsync();
    }

    public async Task<int> GetYen(ulong userId) {
        
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT Yen FROM WorkCell WHERE UserId = $id;";
        command.Parameters.AddWithValue("$id", userId.ToString());

        var result = await command.ExecuteScalarAsync();
        return result != null ? Convert.ToInt32(result) : 0;
    }

    public async Task<int> AddYen(ulong userId, int yen) {
        
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        
        var command = connection.CreateCommand();
        command.CommandText = "UPDATE WorkCell SET Yen = Yen + $yen WHERE UserId = $id;";
        command.Parameters.AddWithValue("$id", userId.ToString());
        command.Parameters.AddWithValue("$yen", yen);
        
        return await command.ExecuteNonQueryAsync();
    }

    public async Task<int> RemoveYen(ulong userId, int yen) {
        
        await using var connection = new SqliteConnection(_connectionString);
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
        
        await using var connection = new SqliteConnection(_connectionString);
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
        
        await using var connection = new SqliteConnection(_connectionString);
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
        
        await using var connection = new SqliteConnection(_connectionString);
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
        command.CommandText = "SELECT Collected FROM WorkCell WHERE UserId = $id;";
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
    
    public async Task RemoveAllApps(ulong userId) {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var json = JsonSerializer.Serialize(new List<string>());

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
    
    public async Task<int> GetWeekStartYen(ulong userId) {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT WeekStartYen FROM WorkCell WHERE UserId = $id;";
        command.Parameters.AddWithValue("$id", userId.ToString());

        var result = await command.ExecuteScalarAsync();
        return result != null && result != DBNull.Value ? Convert.ToInt32(result) : 0;
    }

    public async Task SetWeekStartYen(ulong userId, int value) {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = """
                              INSERT INTO WorkCell (UserId, WeekStartYen)
                              VALUES ($id, $value)
                              ON CONFLICT(UserId) DO UPDATE SET WeekStartYen = $value;
                              """;
        command.Parameters.AddWithValue("$id", userId.ToString());
        command.Parameters.AddWithValue("$value", value);

        await command.ExecuteNonQueryAsync();
    }

    public async Task<int> GetPreviousEarnings(ulong userId) {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT PreviousEarnings FROM WorkCell WHERE UserId = $id;";
        command.Parameters.AddWithValue("$id", userId.ToString());

        var result = await command.ExecuteScalarAsync();
        return result != null && result != DBNull.Value ? Convert.ToInt32(result) : 0;
    }

    public async Task SetPreviousEarnings(ulong userId, int value) {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = """
                              INSERT INTO WorkCell (UserId, PreviousEarnings)
                              VALUES ($id, $value)
                              ON CONFLICT(UserId) DO UPDATE SET PreviousEarnings = $value;
                              """;
        command.Parameters.AddWithValue("$id", userId.ToString());
        command.Parameters.AddWithValue("$value", value);

        await command.ExecuteNonQueryAsync();
    }

    public List<string> GetAllWorkCellUserIds() {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT UserId FROM WorkCell;";

        var userIds = new List<string>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read()) {
            userIds.Add(reader.GetString(0));
        }

        return userIds;
    }

    public DateTimeOffset? GetLastRolloverTime() {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT Value FROM SystemState WHERE Key = 'LastEarningsRollover';";

        var result = cmd.ExecuteScalar();
        return result is string s && DateTimeOffset.TryParse(s, out var dt) ? dt : null;
    }

    public void SetLastRolloverTime(DateTimeOffset time) {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO SystemState (Key, Value) VALUES ('LastEarningsRollover', $value)
            ON CONFLICT(Key) DO UPDATE SET Value = $value;";
        cmd.Parameters.AddWithValue("$value", time.ToString("O"));

        cmd.ExecuteNonQuery();
    }

    public async Task RolloverWeeklyEarnings() {
        foreach (var userIdStr in GetAllWorkCellUserIds()) {
            var userId = ulong.Parse(userIdStr);

            var currentYen = await GetYen(userId);
            var weekStartYen = await GetWeekStartYen(userId);
            var earningsThisWeek = currentYen - weekStartYen;

            await SetPreviousEarnings(userId, earningsThisWeek);
            await SetWeekStartYen(userId, currentYen);
        }
    }

    public async Task<(int CurrentWeekEarnings, int PreviousWeekEarnings, double PercentChange, bool IsIncrease)>
        GetEarningsSummary(ulong userId) {

        var currentYen = await GetYen(userId);
        var weekStartYen = await GetWeekStartYen(userId);
        var currentWeekEarnings = currentYen - weekStartYen;

        var previousWeekEarnings = await GetPreviousEarnings(userId);

        double percentChange;
        if (previousWeekEarnings == 0) {
            percentChange = currentWeekEarnings > 0 ? 100.0 : 0.0;
        } else {
            percentChange = ((double)(currentWeekEarnings - previousWeekEarnings) / Math.Abs(previousWeekEarnings)) * 100.0;
        }

        return (currentWeekEarnings, previousWeekEarnings, percentChange, percentChange >= 0);
    }
    
    private void MigrateWorkCellTable(SqliteConnection connection) {

        var columnInfo = new Dictionary<string, bool>();

        var pragmaCmd = connection.CreateCommand();
        pragmaCmd.CommandText = "PRAGMA table_info(WorkCell);";

        using (var reader = pragmaCmd.ExecuteReader()) {
            while (reader.Read()) {
                var name = reader.GetString(reader.GetOrdinal("name"));
                var notNull = reader.GetInt32(reader.GetOrdinal("notnull")) == 1;
                columnInfo[name] = notNull;
            }
        }

        void AddColumnIfMissing(string columnName) {
            if (columnInfo.ContainsKey(columnName)) return;

            var alterCmd = connection.CreateCommand();
            alterCmd.CommandText = $"ALTER TABLE WorkCell ADD COLUMN {columnName} TEXT;";
            alterCmd.ExecuteNonQuery();
            columnInfo[columnName] = false;
        }

        AddColumnIfMissing("CaseType");
        AddColumnIfMissing("WallpaperType");
        AddColumnIfMissing("CharmType");
        AddColumnIfMissing("WeekStartYen");
        AddColumnIfMissing("PreviousEarnings");
        
        var needsRebuild = (columnInfo.TryGetValue("Cases", out var casesNotNull) && casesNotNull)
                         || (columnInfo.TryGetValue("Wallpapers", out var wallpapersNotNull) && wallpapersNotNull);

        if (!needsRebuild) return;

        using var transaction = connection.BeginTransaction();

        var rebuildCmd = connection.CreateCommand();
        rebuildCmd.Transaction = transaction;
        rebuildCmd.CommandText = @"
            CREATE TABLE WorkCell_new (
                UserId TEXT PRIMARY KEY,
                Yen INTEGER NOT NULL DEFAULT 0,
                Cases TEXT,
                CaseType TEXT,
                Wallpapers TEXT,
                WallpaperType TEXT,
                Charms TEXT,
                CharmType TEXT,
                Apps TEXT,
                Collected TEXT
            );

            INSERT INTO WorkCell_new (UserId, Yen, Cases, CaseType, Wallpapers, WallpaperType, Charms, CharmType, Apps, Collected)
            SELECT UserId, Yen, Cases, CaseType, Wallpapers, WallpaperType, Charms, CharmType, Apps, Collected
            FROM WorkCell;

            DROP TABLE WorkCell;

            ALTER TABLE WorkCell_new RENAME TO WorkCell;
        ";
        rebuildCmd.ExecuteNonQuery();

        transaction.Commit();
    }

    public async Task InitializeWeeklyBaselines() {
        foreach (var userIdStr in GetAllWorkCellUserIds()) {
            var userId = ulong.Parse(userIdStr);

            var existing = await GetWeekStartYen(userId);
            if (existing == 0) {
                var currentYen = await GetYen(userId);
                await SetWeekStartYen(userId, currentYen);
            }
        }
    }
    
    public void SetPendingGame(string userId, string game) {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO PendingGame (UserId, Game, SetAt)
            VALUES ($id, $game, $now)
            ON CONFLICT(UserId) DO UPDATE SET Game = $game, SetAt = $now;
        ";
        command.Parameters.AddWithValue("$id", userId);
        command.Parameters.AddWithValue("$game", game);
        command.Parameters.AddWithValue("$now", DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        command.ExecuteNonQuery();
    }

    public string? GetPendingGame(string userId) {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT Game FROM PendingGame WHERE UserId = $id AND SetAt > $cutoff;";
        command.Parameters.AddWithValue("$id", userId);
        command.Parameters.AddWithValue("$cutoff", DateTimeOffset.UtcNow.AddSeconds(-60).ToUnixTimeSeconds());

        return command.ExecuteScalar() as string;
    }
    
    public void CreateTask(string assignedTo, string assignee, 
        string dateAssigned, string deadline, string taskName,
        string description, string priority, string progress) {
        
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        var command = connection.CreateCommand();

        command.CommandText =
            @"INSERT INTO Task (TaskName, AssignedId, AssigneeId, DateAssigned, Deadline, Description, Priority, Progress, ReminderStage, LastOverdueReminderDate)
              VALUES ($taskName, $assignedTo, $assignee, $dateAssigned, $deadline, $description, $priority, $progress, 'NONE', NULL);";
        
        command.Parameters.AddWithValue("$taskName", taskName);
        command.Parameters.AddWithValue("$assignedTo", assignedTo);
        command.Parameters.AddWithValue("$assignee", assignee);
        command.Parameters.AddWithValue("$dateAssigned", dateAssigned);
        command.Parameters.AddWithValue("$deadline", deadline);
        command.Parameters.AddWithValue("$description", description);
        command.Parameters.AddWithValue("$priority", priority);
        command.Parameters.AddWithValue("$progress", progress);
        
        command.ExecuteNonQuery();
    }

    public void DeleteTask(string taskName, string assignee) {
        
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        var command = connection.CreateCommand();
        
        command.CommandText =
            @"DELETE FROM Task WHERE TaskName = $taskName COLLATE NOCASE AND AssigneeId = $assigneeId;";
        
        command.Parameters.AddWithValue("$taskName", taskName);
        command.Parameters.AddWithValue("$assigneeId", assignee);
        
        command.ExecuteNonQuery();
    }
    
    public void UpdateTask(string taskName, 
                           string assignee,
                           string? deadline = null,
                           string? description = null,
                           string? priority = null) {

        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        bool deadlineChanged = false;
        if (deadline != null) {
            var currentDeadlineCommand = connection.CreateCommand();
            currentDeadlineCommand.CommandText = "SELECT Deadline FROM Task WHERE TaskName = $taskName COLLATE NOCASE;";
            currentDeadlineCommand.Parameters.AddWithValue("$taskName", taskName);
            var currentDeadline = Convert.ToString(currentDeadlineCommand.ExecuteScalar()) ?? "";
            deadlineChanged = !string.Equals(currentDeadline, deadline, StringComparison.Ordinal);
        }

        var command = connection.CreateCommand();
        var setClauses = new List<string>();

        if (deadline != null) {
            setClauses.Add("Deadline = $deadline");
            command.Parameters.AddWithValue("$deadline", deadline);
        }
        if (description != null) {
            setClauses.Add("Description = $description");
            command.Parameters.AddWithValue("$description", description);
        }
        if (priority != null) {
            setClauses.Add("Priority = $priority");
            command.Parameters.AddWithValue("$priority", priority);
        }
        if (deadlineChanged) {
            setClauses.Add("ReminderStage = $reminderStage");
            command.Parameters.AddWithValue("$reminderStage", "NONE");
            setClauses.Add("LastOverdueReminderDate = $lastOverdueReminderDate");
            command.Parameters.AddWithValue("$lastOverdueReminderDate", DBNull.Value);
        }

        if (setClauses.Count == 0) return;

        command.CommandText = $@"
        UPDATE Task
        SET {string.Join(", ", setClauses)}
        WHERE TaskName = $taskName COLLATE NOCASE";

        command.Parameters.AddWithValue("$taskName", taskName);

        command.ExecuteNonQuery();
    }

    private List<string> GetTaskNames(string? ownerColumn, string? ownerId, string? filter, int maxResults) {

        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var command = connection.CreateCommand();
        var whereClauses = new List<string> { "TaskName LIKE $likeFilter ESCAPE '\\' COLLATE NOCASE" };
        command.Parameters.AddWithValue("$likeFilter", "%" + EscapeLikePattern(filter ?? "") + "%");

        if (ownerColumn != null) {
            whereClauses.Add($"{ownerColumn} = $ownerId");
            command.Parameters.AddWithValue("$ownerId", ownerId);
        }

        command.CommandText = $@"
            SELECT TaskName FROM Task
            WHERE {string.Join(" AND ", whereClauses)}
            ORDER BY TaskName
            LIMIT $limit;";

        command.Parameters.AddWithValue("$limit", maxResults);

        using var reader = command.ExecuteReader();

        var names = new List<string>();
        while (reader.Read()) {
            names.Add(reader.GetString(0));
        }
        return names;
    }

    public List<string> GetTaskNamesForAssignedUser(string assignedId, string? filter = null, int maxResults = 25)
        => GetTaskNames("AssignedId", assignedId, filter, maxResults);

    public List<string> GetTaskNamesForAssigneeUser(string assigneeId, string? filter = null, int maxResults = 25)
        => GetTaskNames("AssigneeId", assigneeId, filter, maxResults);

    public List<string> GetAllTaskNames(string? filter = null, int maxResults = 25)
        => GetTaskNames(null, null, filter, maxResults);

    private static string EscapeLikePattern(string input) {
        return input.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
    }

    public string GetAssigneeId(string taskName) {
        
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT AssigneeId FROM Task WHERE TaskName = $taskName COLLATE NOCASE;";
        command.Parameters.AddWithValue("$taskName", taskName);
        
        var result = command.ExecuteScalar();
        return Convert.ToString(result) ?? "";
    }
    
    public (bool taskFound, bool isNowCompleted) SetProgress(string taskName, string progress) {
        
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = "UPDATE Task SET Progress = $progress WHERE TaskName = $taskName COLLATE NOCASE;";
        command.Parameters.AddWithValue("$taskName", taskName);
        command.Parameters.AddWithValue("$progress", progress);

        var rowsAffected = command.ExecuteNonQuery();
        var taskFound = rowsAffected > 0;

        return (taskFound, taskFound && progress.Equals("COMPLETED", StringComparison.OrdinalIgnoreCase));
    }

    public string GetProgress(string taskName) {
        
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT Progress FROM Task WHERE TaskName = $taskName COLLATE NOCASE;";
        command.Parameters.AddWithValue("$taskName", taskName);
        
        var result = command.ExecuteScalar();
        return Convert.ToString(result) ?? "";
    }
    
    public string GetDescription(string taskName) {
        
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT Description FROM Task WHERE TaskName = $taskName COLLATE NOCASE;";
        command.Parameters.AddWithValue("$taskName", taskName);
        
        var result = command.ExecuteScalar();
        return Convert.ToString(result) ?? "";
    }

    public string GetPriority(string taskName) {
        
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT Priority FROM Task WHERE TaskName = $taskName COLLATE NOCASE;";
        command.Parameters.AddWithValue("$taskName", taskName);
        
        var result = command.ExecuteScalar();
        return Convert.ToString(result) ?? "";
    }
    
    public string GetDeadline(string taskName) {
        
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT Deadline FROM Task WHERE TaskName = $taskName COLLATE NOCASE;";
        command.Parameters.AddWithValue("$taskName", taskName);
        
        var result = command.ExecuteScalar();
        return Convert.ToString(result) ?? "";
    }

    public string GetAssignedTo(string taskName) {
        
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT AssignedId FROM Task WHERE TaskName = $taskName COLLATE NOCASE;";
        command.Parameters.AddWithValue("$taskName", taskName);
        
        var result = command.ExecuteScalar();
        return Convert.ToString(result) ?? "";
    }

    public void UpdateReminderState(string taskName, string reminderStage, string? lastOverdueReminderDate) {

        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = "UPDATE Task SET ReminderStage = $stage, LastOverdueReminderDate = $lastOverdue WHERE TaskName = $taskName COLLATE NOCASE;";
        command.Parameters.AddWithValue("$stage", reminderStage);
        command.Parameters.AddWithValue("$lastOverdue", (object?)lastOverdueReminderDate ?? DBNull.Value);
        command.Parameters.AddWithValue("$taskName", taskName);

        command.ExecuteNonQuery();
    }

    public List<TaskModel> GetActiveTasksWithDeadlines() {

        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM Task WHERE Progress != 'COMPLETED';";

        using var reader = command.ExecuteReader();

        var tasks = new List<TaskModel>();
        while (reader.Read()) {
            tasks.Add(ReadTask(reader));
        }
        return tasks;
    }
    
    public TaskModel? ViewTask(string taskName) {
        
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = @"SELECT * FROM Task WHERE TaskName = $taskName COLLATE NOCASE;";
        command.Parameters.AddWithValue("$taskName", taskName);

        using var reader = command.ExecuteReader();
        return !reader.Read() ? null : ReadTask(reader);
    }
    
    public List<TaskModel?> ViewAll(string assignedId) {
        
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        
        var command = connection.CreateCommand();
        command.CommandText = @"SELECT * FROM Task WHERE AssignedId = $assignedId;";
        command.Parameters.AddWithValue("$assignedId", assignedId);

        using var reader = command.ExecuteReader();
        
        if (!reader.HasRows) return [];
        
        var tasks = new List<TaskModel?>();
        while (reader.Read()) {
            tasks.Add(ReadTask(reader));
        }
        return tasks;
    }
    
    public List<TaskModel?> ViewEveryone() {
        
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();
        
        var command = connection.CreateCommand();
        command.CommandText = @"SELECT * FROM Task;";

        using var reader = command.ExecuteReader();
        
        if (!reader.HasRows) return [];
        
        var tasks = new List<TaskModel?>();
        while (reader.Read()) {
            tasks.Add(ReadTask(reader));
        }
        return tasks;
        
    }

    private static TaskModel ReadTask(SqliteDataReader reader) {
        return new TaskModel {
            TaskName = reader.GetString(reader.GetOrdinal("TaskName")),
            AssignedId = reader.GetString(reader.GetOrdinal("AssignedId")),
            AssigneeId = reader.GetString(reader.GetOrdinal("AssigneeId")),
            DateAssigned = reader.GetString(reader.GetOrdinal("DateAssigned")),
            Deadline = reader.GetString(reader.GetOrdinal("Deadline")),
            Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString(reader.GetOrdinal("Description")),
            Priority = reader.GetString(reader.GetOrdinal("Priority")),
            Progress = reader.GetString(reader.GetOrdinal("Progress")),
            ReminderStage = reader.IsDBNull(reader.GetOrdinal("ReminderStage")) ? "NONE" : reader.GetString(reader.GetOrdinal("ReminderStage")),
            LastOverdueReminderDate = reader.IsDBNull(reader.GetOrdinal("LastOverdueReminderDate")) ? null : reader.GetString(reader.GetOrdinal("LastOverdueReminderDate"))
        };
    }
}