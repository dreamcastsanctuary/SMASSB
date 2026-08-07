using System.Text;
using System.Text.Json;
using Discord;
using Discord.WebSocket;
using SMASSB.Data;

namespace SMASSB.Commands;

public class CellSystem {

    private DatabaseService _db;
    private static readonly HttpClient _httpClient = new HttpClient();

    public CellSystem(DatabaseService db) {
        _db = db;
    }

    private static async Task BuildCell(SocketSlashCommand command,
        string caseType,
        string charmType,
        string wallpaperType,
        List<string> appsParam) {

        var ownerId = command.User.Id; // the person who ran the command
        var hasEmulatorApp = appsParam.Contains(AppType.RHYTHMTENGOKU.ToString());
        var flipCustomId = $"flip_over:{ownerId}|front|{caseType}|{charmType}|{wallpaperType}|{hasEmulatorApp}";

        var (cellAttachment, cellImageUrl) = GetCellImage(caseType, charmType, wallpaperType, isFront: true);

        var container = new ContainerBuilder()
            .AddComponent(new MediaGalleryBuilder().AddItem(new MediaGalleryItemProperties(cellImageUrl)))
            .AddComponent(new ActionRowBuilder().WithButton("Flip Cell Over", customId: flipCustomId, style: ButtonStyle.Primary));

        if (hasEmulatorApp) {
            var actionRow = new ActionRowBuilder()
                .WithButton("Play Rhythm Tengoku", customId: $"launch_emulatorjs:{ownerId}", style: ButtonStyle.Secondary);
            container.AddComponent(actionRow);
        }

        var components = new ComponentBuilderV2()
            .AddComponent(container)
            .Build();

        await command.FollowupWithFilesAsync(
            attachments: new[] { cellAttachment },
            components: components,
            flags: MessageFlags.ComponentsV2
        );
    }

    public async Task HandleLaunchEmulatorJs(SocketMessageComponent component, ulong ownerId) {

        if (component.User.Id != ownerId) {
            await component.RespondAsync("This isn't your cell — you can't use this button.", ephemeral: true);
            return;
        }

        var payload = new { type = 12 };
        var json = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(
            $"https://discord.com/api/v10/interactions/{component.Id}/{component.Token}/callback",
            json
        );

        if (!response.IsSuccessStatusCode) {
            var errorBody = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"LaunchActivity failed: {response.StatusCode} - {errorBody}");
            await component.RespondAsync("Couldn't launch the Activity. Make sure it's registered and enabled for this app.", ephemeral: true);
        }
    }

    public async Task EditWorkCell(SocketSlashCommand command) {

        await command.DeferAsync();

        SocketGuildUser enlisted = (SocketGuildUser)command.User;
        string addedApp = null;
        string removedApp = null;
        string cellCase = null;
        string charm = null;
        string wallpaper = null;

        foreach (var option in command.Data.Options) {
            switch (option.Name) {

                case "add_apps":
                    addedApp = option.Value.ToString();
                    break;
                case "remove_apps":
                    removedApp = option.Value.ToString();
                    break;
                case "case_type":
                    cellCase = option.Value.ToString();
                    break;
                case "charm_type":
                    charm = option.Value.ToString();
                    break;
                case "wallpaper_type":
                    wallpaper = option.Value.ToString();
                    break;
                default:
                    await command.FollowupAsync("Unrecognized command.", ephemeral: true);
                    return;
            }
        }

        if (!string.IsNullOrEmpty(addedApp)) {
            await _db.AddAppsToHome(enlisted.Id, addedApp);
        }

        if (!string.IsNullOrEmpty(removedApp)) {
            await _db.RemoveAppsFromHome(enlisted.Id, removedApp);
        }

        if (!string.IsNullOrEmpty(cellCase)) {
            await _db.SetCaseType(enlisted.Id, cellCase);
        }

        if (!string.IsNullOrEmpty(charm)) {
            await _db.SetCharmType(enlisted.Id, charm);
        }

        if (!string.IsNullOrEmpty(wallpaper)) {
            await _db.SetWallpaperType(enlisted.Id, wallpaper);
        }

        var caseParam = await _db.GetCaseType(enlisted.Id);
        var charmParam = await _db.GetCharmType(enlisted.Id);
        var wallpaperParam = await _db.GetWallpaperType(enlisted.Id);
        var appsParam = await _db.GetApps(enlisted.Id);

        await BuildCell(command, caseParam, charmParam, wallpaperParam, appsParam);
    }

    public async Task ShowWorkCell(SocketSlashCommand command) {

        await command.DeferAsync();
        SocketGuildUser enlisted = (SocketGuildUser)command.User;

        foreach (var option in command.Data.Options) {
            switch (option.Name) {

                case "member":
                    enlisted = (SocketGuildUser)option.Value;
                    break;
                default:
                    await command.FollowupAsync("Unrecognized command.", ephemeral: true);
                    return;
            }
        }

        var caseParam = await _db.GetCaseType(enlisted.Id);
        var charmParam = await _db.GetCharmType(enlisted.Id);
        var wallpaperParam = await _db.GetWallpaperType(enlisted.Id);
        var appsParam = await _db.GetApps(enlisted.Id);

        await BuildCell(command, caseParam, charmParam, wallpaperParam, appsParam);
    }

    public async Task EditYen(SocketSlashCommand command, bool add) {

        await command.DeferAsync();

        SocketGuildUser member = null;
        var yen = 0;

        foreach (var option in command.Data.Options) {
            switch (option.Name) {

                case "member":
                    member = (SocketGuildUser)option.Value;
                    break;
                case "amount":
                    yen = (int)(ulong)option.Value;
                    break;
                default:
                    await command.FollowupAsync("Unrecognized command.", ephemeral: true);
                    return;
            }
        }

        if (member == null) {
            await command.FollowupAsync("Unrecognized user.", ephemeral: true);
            return;
        }

        if (add)
            await _db.AddYen(member.Id, yen);
        else
            await _db.RemoveYen(member.Id, yen);
    }

    public async Task EditApp(SocketSlashCommand command, bool add) {

        await command.DeferAsync();

        SocketGuildUser member = null;
        var app = "";

        foreach (var option in command.Data.Options) {
            switch (option.Name) {

                case "member":
                    member = (SocketGuildUser)option.Value;
                    break;
                case "app":
                    app = option.Value.ToString();
                    break;
                default:
                    await command.FollowupAsync("Unrecognized command.", ephemeral: true);
                    return;
            }
        }

        if (member == null) {
            await command.FollowupAsync("Unrecognized user.", ephemeral: true);
            return;
        }

        if (add)
            await _db.GiveNewApp(member.Id, app);
        else
            await _db.RemoveApp(member.Id, app);

        await command.FollowupAsync("Done!");
    }

    public async Task HandleFlipOver(SocketMessageComponent component, string state) {

        try {
            var parts = state.Split('|');
            var ownerId = ulong.Parse(parts[0]);

            if (component.User.Id != ownerId) {
                await component.RespondAsync("This isn't your cell — you can't use this button.", ephemeral: true);
                return;
            }

            var currentSide = parts[1];
            var caseType = parts[2];
            var charmType = parts[3];
            var wallpaperType = parts[4];
            var hasEmulatorApp = bool.Parse(parts[5]);

            var nextSide = currentSide == "front" ? "back" : "front";
            var isFrontNext = nextSide == "front";

            var nextCustomId = $"flip_over:{ownerId}|{nextSide}|{caseType}|{charmType}|{wallpaperType}|{hasEmulatorApp}";

            var (cellAttachment, cellImageUrl) = GetCellImage(caseType, charmType, wallpaperType, isFront: isFrontNext);

            var container = new ContainerBuilder()
                .AddComponent(new MediaGalleryBuilder().AddItem(new MediaGalleryItemProperties(cellImageUrl)))
                .AddComponent(new ActionRowBuilder().WithButton("Flip Cell Over", customId: nextCustomId, style: ButtonStyle.Primary));

            if (hasEmulatorApp) {
                var actionRow = new ActionRowBuilder()
                    .WithButton("Play Rhythm Tengoku", customId: $"launch_emulatorjs:{ownerId}", style: ButtonStyle.Secondary);
                container.AddComponent(actionRow);
            }

            var components = new ComponentBuilderV2()
                .AddComponent(container)
                .Build();

            await component.UpdateAsync(msg => {
                msg.Attachments = new List<FileAttachment> { cellAttachment };
                msg.Components = components;
                msg.Flags = MessageFlags.ComponentsV2;
            });
        } catch (Exception ex) {
            Console.WriteLine($"HandleFlipOver failed: {ex}");

            if (!component.HasResponded) {
                await component.RespondAsync("Something went wrong flipping the cell.", ephemeral: true);
            }
        }
    }

    private static (FileAttachment Attachment, string Url) GetCellImage(string caseType, string charmType, string wallpaperType, bool isFront) {

        switch (caseType) { }
        switch (charmType) { }
        switch (wallpaperType) { }

        var fileName = isFront ? "placeholder-cell-front.png" : "placeholder-cell-back.png";
        var path = Path.Combine(AppContext.BaseDirectory, "Images", fileName);

        var attachment = new FileAttachment(path, fileName);
        var url = $"attachment://{attachment.FileName}";

        return (attachment, url);
    }
}