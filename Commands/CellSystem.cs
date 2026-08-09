using System.Text;
using System.Text.Json;
using Discord;
using Discord.WebSocket;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing;
using SMASSB.Data;
using Image = SixLabors.ImageSharp.Image;
using Color = SixLabors.ImageSharp.Color;

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
                                         List<string> appsParam,
                                         int yen, 
                                         int currentWeekEarnings, 
                                         double percentChange, 
                                         bool isIncrease) {

        var ownerId = command.User.Id; 
        var hasEmulatorApp = appsParam.Contains(AppType.RHYTHMTENGOKU.ToString());
        var flipCustomId = $"flip_over:{ownerId}|front|{caseType}|{charmType}|{wallpaperType}|{hasEmulatorApp}";

        var (cellAttachment, cellImageUrl) = GetCellImage(caseType, charmType, wallpaperType, isFront: true, yen: yen, userId: ownerId, currentWeekEarnings: currentWeekEarnings, percentChange: percentChange, isIncrease: isIncrease);

        var container = new ContainerBuilder()
            .AddComponent(new MediaGalleryBuilder().AddItem(new MediaGalleryItemProperties(cellImageUrl)))
            .AddComponent(new ActionRowBuilder().WithButton("Flip Cellphone Over", customId: flipCustomId, style: ButtonStyle.Secondary));

        if (hasEmulatorApp) {
            var actionRow = new ActionRowBuilder()
                .WithButton("Play Rhythm Tengoku", customId: $"launch_emulatorjs:{ownerId}", style: ButtonStyle.Success);
            container.AddComponent(actionRow);
        }

        var components = new ComponentBuilderV2()
            .WithTextDisplay(new TextDisplayBuilder().WithContent("<:sango_emblem_mono:1492222638980989138> :: Loaded WorkCell!"))
            .WithSeparator(new SeparatorBuilder().WithIsDivider(false))
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
            await component.RespondAsync("This isn't your cell! You like touching things that don't belong to you?", ephemeral: true);
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
            await component.RespondAsync("Couldn't launch the app... Ask for help!", ephemeral: true);
        }
    }

    public async Task HandleFlipOver(SocketMessageComponent component, 
                                         string state, 
                                         int yen, 
                                         int currentWeekEarnings, 
                                         double percentChange, 
                                         bool isIncrease) {

        try {
            var parts = state.Split('|');
            var ownerId = ulong.Parse(parts[0]);

            if (component.User.Id != ownerId) {
                await component.RespondAsync("This isn't your cell! You like touching things that don't belong to you?", ephemeral: true);
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

            var (cellAttachment, cellImageUrl) = GetCellImage(caseType, charmType, wallpaperType, isFront: isFrontNext, yen: yen, userId: ownerId, currentWeekEarnings: currentWeekEarnings, percentChange: percentChange, isIncrease: isIncrease);

            var container = new ContainerBuilder()
                .AddComponent(new MediaGalleryBuilder().AddItem(new MediaGalleryItemProperties(cellImageUrl)))
                .AddComponent(new ActionRowBuilder().WithButton("Flip Cell Over", customId: nextCustomId, style: ButtonStyle.Secondary));

            if (hasEmulatorApp) {
                var actionRow = new ActionRowBuilder()
                    .WithButton("Play Rhythm Tengoku", customId: $"launch_emulatorjs:{ownerId}", style: ButtonStyle.Success);
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

    private static (FileAttachment Attachment, string Url) GetCellImage(string caseType, 
                                                                          string charmType, 
                                                                          string wallpaperType, 
                                                                          bool isFront, 
                                                                          int yen, 
                                                                          ulong userId, 
                                                                          int currentWeekEarnings, 
                                                                          double percentChange, 
                                                                          bool isIncrease) {

        var fontCollection = new FontCollection();
        var fontPath = Path.Combine(AppContext.BaseDirectory, "Fonts", "MonaspaceArgon-Bold.otf");
        var fontFamily = fontCollection.Add(fontPath);
        var fontReg = fontFamily.CreateFont(200);
        var fontTiny = fontFamily.CreateFont(50);
        var fontSmall = fontFamily.CreateFont(65);
        var fontBal = fontFamily.CreateFont(80);
        
        var caseFile = "";
        var charmFile = "";
        var wallpaperFile = "";
        
        switch (caseType) {
            case "BLACK":
                caseFile = isFront ? "black-case-front.png" : "black-case-back.png";
                break;
            case "SAKURA":
                caseFile = isFront ? "sakura-case-front.png" : "sakura-case-back.png";
                break;
            case "SANGO":
                caseFile = isFront ? "sango-case-front.png" : "sango-case-back.png";
                break;
        }

        switch (charmType) {
            case "SAKURA":
                charmFile = "sakura-charm.png";
                break;
            case "SANGO":
                charmFile = "sango-charm.png";
                break;
        }

        switch (wallpaperType) {
            case "BASIC":
                wallpaperFile = "basic-wallpaper.png";
                break;
            case "SAKURA":
                wallpaperFile = "sakura-wallpaper.png";
                break;
            case "SANGO":
                wallpaperFile = "sango-wallpaper.png";
                break;
        }
        
        var casePath = Path.Combine(AppContext.BaseDirectory, "Images", caseFile);
        var charmPath = Path.Combine(AppContext.BaseDirectory, "Images", wallpaperFile);
        var wallpaperPath = Path.Combine(AppContext.BaseDirectory, "Images", wallpaperFile);
        
        using var cellCase = Image.Load(casePath);
        using var charm = Image.Load(charmPath);
        using var wallpaper = Image.Load(wallpaperPath);

        if (isFront) {
            
            var color = GetFontColor(wallpaperType);
            
            using var clone = cellCase.Clone(ipc => {
                    
                    ipc.DrawImage(wallpaper, new Point(0, 0), 1);
                    ipc.DrawImage(charm, new Point(0, 0), 1);
                    ipc.DrawText("¥" + yen.ToString("N0"), fontReg, color, new Point(757, 739));
                    ipc.DrawText("**** **** **** " + (userId % 10000).ToString("D4"), fontSmall, color, new Point(762, 473));
                    ipc.DrawText("Balance", fontBal, color, new Point(740, 640));
                    ipc.DrawText(BuildEarningsSummary(currentWeekEarnings, percentChange, isIncrease), fontTiny, color, new Point(807, 953));
            });
            var outputStream = new MemoryStream();
            clone.Save(outputStream, new PngEncoder());
            outputStream.Position = 0;

            var composedFileName = "composed-cell.png";
            var composedAttachment = new FileAttachment(outputStream, composedFileName);
        
            var composedUrl = $"attachment://{composedAttachment.FileName}";

            return (composedAttachment, composedUrl);
        }
        
        var caseAttachment = new FileAttachment(casePath, caseFile);
        var caseUrl = $"attachment://{caseAttachment.FileName}";
        
        return (caseAttachment, caseUrl);
    }

    private static Color GetFontColor(string wallpaperType) {
        
        switch (wallpaperType) {
            case "BASIC":
                return Color.FromRgba(15, 15, 15, 255);
            case "SAKURA":
                return Color.FromRgba(154, 100, 114, 255);
            case "SANGO":
                return Color.FromRgba(28, 39, 41, 255);
        }
        return new Color();
    }
    
    private static string BuildEarningsSummary(int currentWeekEarnings, double percentChange, bool isIncrease) {
        var arrow = isIncrease ? "▲" : "▼";
        return $"¥{currentWeekEarnings:N0} this week ({arrow} {Math.Abs(percentChange):F1}%)";
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
        var (currentWeekEarnings, previousWeekEarnings, percentChange, isIncrease) = await _db.GetEarningsSummary(enlisted.Id);

        await BuildCell(command, caseParam, charmParam, wallpaperParam, appsParam, await _db.GetYen(enlisted.Id), currentWeekEarnings, percentChange, isIncrease);
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
        var (currentWeekEarnings, previousWeekEarnings, percentChange, isIncrease) = await _db.GetEarningsSummary(enlisted.Id);

        await BuildCell(command, caseParam, charmParam, wallpaperParam, appsParam, await _db.GetYen(enlisted.Id), currentWeekEarnings, percentChange, isIncrease);
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

    public async Task EditAddons(SocketSlashCommand command, bool add) {

        await command.DeferAsync();

        SocketGuildUser member = null;
        string app = null;
        string caseType = null;
        string charmType = null;
        string wallpaperType = null;

        foreach (var option in command.Data.Options) {
            switch (option.Name) {

                case "member":
                    member = (SocketGuildUser)option.Value;
                    break;
                case "app":
                    app = option.Value.ToString();
                    break;
                case "case":
                    caseType = option.Value.ToString();
                    break;
                case "charm":
                    charmType = option.Value.ToString();
                    break;
                case "wallpaper":
                    wallpaperType = option.Value.ToString();
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

        if (string.IsNullOrEmpty(app) && string.IsNullOrEmpty(caseType) &&
            string.IsNullOrEmpty(charmType) && string.IsNullOrEmpty(wallpaperType)) {
            await command.FollowupAsync("You must specify at least one addon to give or remove!", ephemeral: true);
            return;
        }

        if (!string.IsNullOrEmpty(app)) {
            if (add)
                await _db.GiveNewApp(member.Id, app);
            else
                await _db.RemoveApp(member.Id, app);
        }
        if (!string.IsNullOrEmpty(caseType)) {
            if (add) {
                await _db.GiveNewCase(member.Id, caseType);
            } else {
                await _db.RemoveCase(member.Id, caseType);
                var equipped = await _db.GetCaseType(member.Id);
                if (equipped == caseType)
                    await _db.SetCaseType(member.Id, "BLACK");
            }
        }
        if (!string.IsNullOrEmpty(charmType)) {
            if (add) {
                await _db.GiveNewCharm(member.Id, charmType);
            } else {
                await _db.RemoveCharm(member.Id, charmType);
                var equipped = await _db.GetCharmType(member.Id);
                if (equipped == charmType)
                    await _db.SetCharmType(member.Id, "NONE");
            }
        }
        if (!string.IsNullOrEmpty(wallpaperType)) {
            if (add) {
                await _db.GiveNewWallpaper(member.Id, wallpaperType);
            } else {
                await _db.RemoveWallpaper(member.Id, wallpaperType);
                var equipped = await _db.GetWallpaperType(member.Id);
                if (equipped == wallpaperType)
                    await _db.SetWallpaperType(member.Id, "BASIC");
            }
        }

        await command.FollowupAsync("Done!");
    }
}