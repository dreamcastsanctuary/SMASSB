using System.Text.RegularExpressions;
using Discord;
using Discord.WebSocket;
using GenCode128;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SMASSB.Exceptions;
using SMASSB.Models;
using Color = SixLabors.ImageSharp.Color;
using Image = SixLabors.ImageSharp.Image;

namespace SMASSB.Commands;

public class IdSystem {
    
    private readonly DiscordSocketClient _client;
    private readonly DatabaseService _db;
    private readonly RoleSystem _roleSystem;
    private readonly LogHandler _logHandler;
    private SocketGuild _guild;
    private static readonly HttpClient HttpClient = new HttpClient();
    
    public IdSystem(DiscordSocketClient client, 
                    LogHandler logHandler, 
                    DatabaseService db,
                    RoleSystem roleSystem, 
                    GuildConfiguration guildConfig) {
        
        _client = client;
        _logHandler = logHandler;
        _db = db;
        _roleSystem = roleSystem;
        var guildId = guildConfig.GuildId;
        _guild = client.GetGuild(guildId);
    }

    public static async Task BuildId(SocketSlashCommand command,
                                     SocketGuildUser member,
                                     string claimParam, 
                                     byte[]? avatarImageParam,
                                     string avatarUrlParam,
                                     string accIdParam,
                                     DateTimeOffset dateParam,
                                     string rankParam,
                                     int pointsParam,
                                     int recruitsParam,
                                     string bloodtypeParam,
                                     string catchphraseParam,
                                     string usernameParam,
                                     string idType) {
        
        var fontCollection = new FontCollection();
        var fontPath = Path.Combine(AppContext.BaseDirectory, "Fonts", "MonaspaceArgon-Bold.otf");
        var fontFamily = fontCollection.Add(fontPath);
        var font = fontFamily.CreateFont(50);
        var fontId = fontFamily.CreateFont(35);
        var fontSmall = fontFamily.CreateFont(35);

        Image idImg;
        try { idImg = LoadId(idType); } catch { await command.FollowupAsync("Did you forget to pre/enlist this person? ;)", ephemeral: true); return; }
        Image avatar;
        
        if (avatarImageParam != null) {
            try {
                using var avatarStream = new MemoryStream(avatarImageParam);
                avatar = await Image.LoadAsync(avatarStream);
            } catch (UnknownImageFormatException) {
                await command.FollowupAsync("It seems like your avatar is corrupted. Please run /editid to fix it, or contact a staff member for assistance.", ephemeral: true);
                return;
            }
        } else {
            var sizedAvatarUrl = avatarUrlParam.Contains('?') ? avatarUrlParam + "&size=4096" : avatarUrlParam + "?size=4096";

            try {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                var avatarBytes = await HttpClient.GetByteArrayAsync(sizedAvatarUrl, cts.Token);
                using var avatarStream = new MemoryStream(avatarBytes);
                avatar = await Image.LoadAsync(avatarStream, cts.Token);
                
            } catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException) {
                await command.FollowupAsync("Couldn't load the avatar image. Try again in a moment, or contact a staff member for assistance.", ephemeral: true);
                return;
                
            } catch (UnknownImageFormatException) {
                await command.FollowupAsync("That avatar link doesn't point to a supported image format.", ephemeral: true);
                return;
            }
        }
        
        avatar.Mutate(x => x.Resize(new ResizeOptions {
            Size = new Size(250, 250),
            Mode = ResizeMode.Crop,
            Sampler = KnownResamplers.Lanczos3
        }));
        
        var namePos = new Point(827, 452);
        var avatarPos = new Point(93,369);
        var idPos = new Point(1219,154);
        var datePos = new Point(827,531);
        var rankPos = new Point(827,609);
        var pointsPos = new Point(827,690);
        var recruitsPos = new Point(827,769);
        var bloodtypePos = new Point(827,848);
        var catchphrasePos = new Point(70,953);
        var barcodePos = new Point(95,688);
        
        var colors = LoadColors(idType);
        var barcode = Code128Rendering.MakeBarcodeImage(usernameParam, 1, true);
        
        var stream = new MemoryStream();
        barcode.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
        stream.Position = 0;
        
        var barcodeImg = await Image.LoadAsync(stream);
        var coloredBarcode = new Image<Rgba32>(barcodeImg.Width, barcodeImg.Height, colors[3]);
        
        coloredBarcode.Mutate(ctx => ctx.DrawImage(barcodeImg, new Point(0, 0), PixelColorBlendingMode.Multiply, PixelAlphaCompositionMode.SrcOver, 1f));
        coloredBarcode.Mutate(x => x.Resize(new ResizeOptions {
            Size = new Size(250, 50),
            Mode = ResizeMode.Crop,
            Sampler = KnownResamplers.Lanczos3
        }));

        var badgesToDraw = ListBadges(member.Roles.Select(r => r.Id).ToHashSet());
        
        var clone = idImg.Clone(ipc => {
            if (member.Roles.Select(r => r.Id).ToHashSet().Contains(1527907825836097556)) {
                ipc.DrawImage(Image.Load(Path.Combine(AppContext.BaseDirectory, "Images", "badge10.png")), 1);
            }
            if (member.Roles.Select(r => r.Id).ToHashSet().Contains(1527907819649630298)) {
                ipc.DrawImage(Image.Load(Path.Combine(AppContext.BaseDirectory, "Images", "badge11.png")), 1);
            }
            
            ipc.DrawImage(avatar, avatarPos, 1);
            ipc.DrawImage(coloredBarcode, barcodePos, 1);
            ipc.DrawText($"{pointsParam}", font, colors[2], pointsPos);
            ipc.DrawText($"{recruitsParam}", font, colors[2], recruitsPos);
            ipc.DrawText($"{bloodtypeParam}", font, colors[2], bloodtypePos);
            ipc.DrawText($"{catchphraseParam}", font, colors[1], catchphrasePos);
            ipc.DrawText($"{dateParam:M/d/yyyy}", font, colors[2], datePos);
            ipc.DrawText($"{accIdParam}", fontId, colors[0], idPos);

            if (claimParam.Length > 15) {
                
                var spaceIndex = claimParam.IndexOf(' ');
                if (spaceIndex > 0) {
                    var firstName = claimParam[..spaceIndex];
                    var lastName = claimParam[(spaceIndex + 1)..];
                    claimParam = $"{firstName}\n{lastName}";
                }
                
                ipc.DrawText($"{claimParam}", fontSmall, colors[2], new Point(namePos.X, namePos.Y - 5));
            } else ipc.DrawText($"{claimParam}", font, colors[2], namePos);
            
            if (rankParam.Contains("taru")) {
                rankParam = "Bakuryōchō\ntaru Onshō";
                ipc.DrawText($"{rankParam}", fontSmall, colors[2], new Point(rankPos.X, rankPos.Y - 3));
                
            } else ipc.DrawText($"{rankParam}", font, colors[2], rankPos);
            
            foreach (var (img, pos) in badgesToDraw)
                ipc.DrawImage(img, pos, 1);
            
        });

        var output = Path.Combine(Path.GetTempPath(), $"id_{accIdParam}.png");
        
        await clone.SaveAsync(output);
        foreach (var (img, _) in badgesToDraw) img.Dispose();
        
        if (member != command.User && !command.CommandName.Contains("other")) {
            try { 
                await member.SendMessageAsync("Here you are! Your new **Identification Card** and loaned **Work Cellphone**!\nKeep them safe.");
                await member.SendFileAsync(output);
            }
            catch (Discord.Net.HttpException ex) { await command.FollowupAsync(new MessageSendException(ex.Message, ex).Message); }
        } else {
            await command.FollowupWithFileAsync(output, text: "<:sango_emblem_mono:1492222638980989138> :: Loaded Identification Card!");
        }
        
        File.Delete(output);
    }
    
    public async Task EditId(SocketSlashCommand command) {

        await command.DeferAsync();
        
        var enlisted = (SocketGuildUser)command.User;
        string? claim = null;
        string? avatarUrl = null;
        string? bloodtype = null;
        string? idType = null;
        
        foreach (var option in command.Data.Options) {
            switch (option.Name)
            {

                case "claim":
                    claim = option.Value.ToString();
                    break;
                case "avatar_url":
                    avatarUrl = option.Value.ToString();
                    break;
                case "bloodtype":
                    bloodtype = option.Value.ToString();
                    break;
                case "id_type":
                    idType = option.Value.ToString();
                    break;
                default:
                    await command.FollowupAsync("Unrecognized command.", ephemeral: true);
                    return;
            }
        }
        
        if (!string.IsNullOrEmpty(avatarUrl)) {
            
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(25));
            var resolvedUrl = await ResolveImgurUrlAsync(avatarUrl, HttpClient, cts.Token);

            try {
                var bytes = await HttpClient.GetByteArrayAsync(resolvedUrl, cts.Token);

                using (var testStream = new MemoryStream(bytes)) {
                    using var _ = await Image.LoadAsync(testStream, cts.Token);
                }

                await _db.SetAvatarImage(enlisted.Id, bytes);
                await _db.SetAvatarUrl(enlisted.Id, resolvedUrl);

            } catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException) {
                await command.FollowupAsync("Couldn't download that avatar image! The host website may be slow or the URL invalid. Try again or use a different link.", ephemeral: true);
                return;
                
            } catch (UnknownImageFormatException) {
                await command.FollowupAsync("That link doesn't point to a supported image (PNG/JPEG/etc.) Make sure it's a direct image link, rather than a page containing one.", ephemeral: true);
                return;
            }
        }

        if (!string.IsNullOrEmpty(claim)) {
            await _db.SetClaim(enlisted.Id, claim);
        }
        
        if (!string.IsNullOrEmpty(bloodtype)) {
            await _db.SetBloodtype(enlisted.Id, bloodtype);
        }
        
        if (!string.IsNullOrEmpty(idType)) {
            await _db.SetIdType(enlisted.Id, idType);
        }
        
        var claimParam = await _db.GetClaim(enlisted.Id);
        var avatarUrlParam = await _db.GetAvatarUrl(enlisted.Id);
        var accIdParam = enlisted.Id.ToString();
        var dateParam = enlisted.JoinedAt ?? enlisted.CreatedAt;
        var rankParam = await _db.GetRank(enlisted.Id);
        var pointsParam = await _db.GetPoints(enlisted.Id);
        var recruitsParam = await _db.GetRecruits(enlisted.Id);
        var bloodtypeParam = await _db.GetBloodtype(enlisted.Id);
        var usernameParam = await _db.GetUsername(enlisted.Id);
        var avatarImageParam = await _db.GetAvatarImage(enlisted.Id);
        var idTypeParam = await _db.GetIdType(enlisted.Id);
        
        await BuildId(command, enlisted, claimParam, avatarImageParam, avatarUrlParam, accIdParam, dateParam, rankParam, pointsParam, recruitsParam, bloodtypeParam, "", usernameParam, idTypeParam);
    }
    
    public async Task ShowId(SocketSlashCommand command) {
        
        await command.DeferAsync();
        SocketGuildUser enlisted = (SocketGuildUser)command.User;
        
        foreach (var option in command.Data.Options) {
            switch (option.Name)
            {
                case "member":
                    enlisted = (SocketGuildUser)option.Value;
                    break;
                default:
                    await command.FollowupAsync("Unrecognized command.", ephemeral: true);
                    return;
            }
        }
        
        var claimParam = await _db.GetClaim(enlisted.Id);
        var avatarUrlParam = await _db.GetAvatarUrl(enlisted.Id);
        var accIdParam = enlisted.Id.ToString();
        var dateParam = enlisted.JoinedAt ?? enlisted.CreatedAt;
        var rankParam = await _db.GetRank(enlisted.Id);
        var pointsParam = await _db.GetPoints(enlisted.Id);
        var recruitsParam = await _db.GetRecruits(enlisted.Id);
        var bloodtypeParam = await _db.GetBloodtype(enlisted.Id);
        var usernameParam = await _db.GetUsername(enlisted.Id);
        var avatarImageParam = await _db.GetAvatarImage(enlisted.Id);
        var idTypeParam = await _db.GetIdType(enlisted.Id);
        
        await BuildId(command, enlisted, claimParam, avatarImageParam, avatarUrlParam, accIdParam, dateParam, rankParam, pointsParam, recruitsParam, bloodtypeParam, "", usernameParam, idTypeParam);
    }

    public async Task GainId(SocketSlashCommand command) {
        
        SocketGuildUser? member = null;
        var id = "";
        
        foreach (var option in command.Data.Options) {
            switch (option.Name)
            {

                case "member":
                    member = (SocketGuildUser)option.Value;
                    break;
                case "id":
                    id = option.Value.ToString();
                    break;
                default:
                    await command.RespondAsync("Unrecognized command.", ephemeral: true);
                    return;
            }
        }

        if (member != null && id != null) await _db.GiveNewId(member.Id, id);
        await command.RespondAsync("Completed task.", ephemeral: true);
    }
    
    public async Task RemoveId(SocketSlashCommand command) {
        
        SocketGuildUser? member = null;
        var id = "";
        
        foreach (var option in command.Data.Options) {
            switch (option.Name)
            {

                case "member":
                    member = (SocketGuildUser)option.Value;
                    break;
                case "id":
                    id = option.Value.ToString();
                    break;
                default:
                    await command.RespondAsync("Unrecognized command.", ephemeral: true);
                    return;
            }
        }

        if (member == null || id == null)
            return; 
        
        await _db.RemoveId(member.Id, id);

        if (id.Equals(await _db.GetIdType(member.Id))) {
            if (member.Roles.Any(r => r.Id == 1473508563887329447)) {
                await _db.SetIdType(member.Id, "STAFFMAIN");
            } else {
                await _db.SetIdType(member.Id, "ENLISTEDMAIN");
            }
        }
        
        await command.RespondAsync("Completed task.", ephemeral: true);
    }
    
    public async Task HandleForceUpdateCommand(SocketSlashCommand command) {
        
        await command.DeferAsync();
        SocketGuildUser? member = null;
        var claim = "";
        IRole? rank = null;
        var avatarFoobar = false;
        
        foreach (var option in command.Data.Options)
        {
            switch (option.Name) {
                
                case "member":
                    member = ((SocketGuildUser)option.Value);
                    break;
                case "claim_name":
                    claim = option.Value.ToString();
                    break;
                case "rank_name":
                    rank = (IRole)option.Value;
                    break;
                case "avatar_fix":
                    avatarFoobar = option.Value.ToString() == "True";
                    break;
                default:
                    await command.FollowupAsync("Unrecognized command.", ephemeral: true);
                    break;
            }
        }

        if (member == null) {
            await command.FollowupAsync("Unrecognized account.", ephemeral: true);
            return;
        }

        if (avatarFoobar) {
            var avatarUrl = member.GetGuildAvatarUrl() ??  member.GetAvatarUrl() ?? member.GetDefaultAvatarUrl();
            var bytes = await HttpClient.GetByteArrayAsync(avatarUrl);
            await _db.SetAvatarUrl(member.Id, avatarUrl);
            await _db.SetAvatarImage(member.Id, bytes);
        }

        if (!string.IsNullOrEmpty(claim) && rank != null) {
            
            await _roleSystem.Promote(member, rank, command, claim, "Changed claim.");
            
        } else if (!String.IsNullOrEmpty(claim)) {
            
            var nickname = member.Nickname;
            var dotIndex = nickname.IndexOf('.');
            
            var fixedRankNick = nickname.Substring(0, dotIndex + 1);
            await member.ModifyAsync(x => x.Nickname = fixedRankNick + " " + claim);
            
            await _db.SetClaim(member.Id, claim);
            await command.FollowupAsync("Changed claim.");
            
        } else if (rank != null) {
            
            var rankName = rank.Name;
            var dotIndex = rankName.IndexOf('.');
            
            var fixedRankNick = rankName.Substring(1, dotIndex);
            var fixedRankFull = rankName[(dotIndex + 2)..];
            var oldClaim = await _db.GetClaim(member.Id);
            
            await member.ModifyAsync(x => x.Nickname = fixedRankNick + " " + oldClaim);
            
            await _db.SetRank(member.Id, fixedRankFull);
            await command.FollowupAsync("Completed task.");
            
        } else {
            await command.FollowupAsync("Nothing needs to be set.", ephemeral: true);
        }
    }
    
    static Image LoadBadges(string filename, int w, int h) {
        var path = Path.Combine(AppContext.BaseDirectory, "Images", filename);
        var img = Image.Load(path);
        
        if (filename.Contains("tanzaku")) {
            img.Mutate(x => x.Resize(new ResizeOptions {
                Size = new Size(w, h),
                Mode = ResizeMode.Pad,
                Sampler = KnownResamplers.Lanczos3
            }));
        } else {
            img.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new Size(w, h),
                Mode = ResizeMode.Crop,
                Sampler = KnownResamplers.Lanczos3
            }));
        }

        return img;
    }

    static Image LoadId(string idType) {
        
        var imgPath = "";

        switch (idType) {
            
            case "ENLISTEDMAIN":
                imgPath = Path.Combine(AppContext.BaseDirectory, "Images", "enlisted-main-template.png");
                break;
            case "ENLISTEDPRECIVT":
                imgPath = Path.Combine(AppContext.BaseDirectory, "Images", "enlisted-precivt-template.png");
                break;
            case "STAFFMAIN":
                imgPath = Path.Combine(AppContext.BaseDirectory, "Images", "staff-main-template.png");
                break;
            case "NEWGAMEPLUSENLISTED":
                imgPath = Path.Combine(AppContext.BaseDirectory, "Images", "ngplus-template.png");
                break;
            case "NEWGAMEPLUSSTAFF":
                imgPath = Path.Combine(AppContext.BaseDirectory, "Images", "ngplus-staff-template.png");
                break;
            case "ENLISTEDTANABATA":
                imgPath = Path.Combine(AppContext.BaseDirectory, "Images", "enlisted-tanabata-template.png");
                break;
            case "STAFFTANABATA":
                imgPath = Path.Combine(AppContext.BaseDirectory, "Images", "staff-tanabata-template.png");
                break;
            case "PINK":
                imgPath = Path.Combine(AppContext.BaseDirectory, "Images", "pink-template.png");
                break;
            case "RED":
                imgPath = Path.Combine(AppContext.BaseDirectory, "Images", "red-template.png");
                break;
            case "GREEN":
                imgPath = Path.Combine(AppContext.BaseDirectory, "Images", "green-template.png");
                break;
            case "BLUE":
                imgPath = Path.Combine(AppContext.BaseDirectory, "Images", "blue-template.png");
                break;  
        }
        
        return Image.Load(imgPath);
    }
    
    static Image LoadFrames(string frameType) {
        
        var imgPath = "";
        
        return Image.Load(imgPath);
    }

     static List<Color> LoadColors(string idType) {

         var colors = new List<Color>();
        
        switch (idType) {
            
            case "ENLISTEDMAIN":
                colors.Add(Color.FromRgba(190, 164, 95, 255)); // Heading
                colors.Add(Color.FromRgba(190, 164, 95, 255)); // Catchphrase
                colors.Add(Color.FromRgba(190, 164, 95, 255)); // Details
                colors.Add(Color.FromRgba(255, 61, 54, 255)); // Barcode
                break;
            case "ENLISTEDPRECIVT":
                colors.Add(Color.FromRgba(190, 164, 95, 255)); // Heading
                colors.Add(Color.FromRgba(190, 164, 95, 255)); // Catchphrase
                colors.Add(Color.FromRgba(190, 164, 95, 255)); // Details
                colors.Add(Color.FromRgba(255, 61, 54, 255)); // Barcode
                break;
            case "STAFFMAIN":
                colors.Add(Color.FromRgba(244, 227, 194, 255));
                colors.Add(Color.FromRgba(244, 227, 194, 255));
                colors.Add(Color.FromRgba(35, 1, 0, 255));
                colors.Add(Color.FromRgba(229, 58, 59, 255));
                break;
            case "NEWGAMEPLUSENLISTED":
                colors.Add(Color.FromRgba(255, 215, 156, 255));
                colors.Add(Color.FromRgba(33, 25, 22, 255));
                colors.Add(Color.FromRgba(238, 228, 212, 255));
                colors.Add(Color.FromRgba(134, 39, 36, 255));
                break;
            case "NEWGAMEPLUSSTAFF":
                colors.Add(Color.FromRgba(255, 215, 156, 255));
                colors.Add(Color.FromRgba(33, 25, 22, 255));
                colors.Add(Color.FromRgba(238, 228, 212, 255));
                colors.Add(Color.FromRgba(134, 39, 36, 255));
                break;
            case "ENLISTEDTANABATA":
                colors.Add(Color.FromRgba(255, 255, 255, 255));
                colors.Add(Color.FromRgba(255, 255, 255, 255));
                colors.Add(Color.FromRgba(255, 255, 255, 255));
                colors.Add(Color.FromRgba(255, 255, 255, 255));
                break;
            case "STAFFTANABATA":
                colors.Add(Color.FromRgba(255, 255, 255, 255));
                colors.Add(Color.FromRgba(255, 255, 255, 255));
                colors.Add(Color.FromRgba(255, 255, 255, 255));
                colors.Add(Color.FromRgba(255, 255, 255, 255));
                break;
            case "PINK":
                colors.Add(Color.FromRgba(210, 70, 103, 255)); // Heading
                colors.Add(Color.FromRgba(210, 70, 103, 255)); // Catchphrase
                colors.Add(Color.FromRgba(255, 199, 132, 255)); // Details
                colors.Add(Color.FromRgba(194, 28, 76, 255)); // Barcode
                break;
            case "RED":
                colors.Add(Color.FromRgba(255, 192, 128, 255));
                colors.Add(Color.FromRgba(255, 97, 79, 255));
                colors.Add(Color.FromRgba(255, 199, 132, 255));
                colors.Add(Color.FromRgba(255, 97, 79, 255));
                break;
            case "GREEN":
                colors.Add(Color.FromRgba(0, 154, 103, 255));
                colors.Add(Color.FromRgba(255, 57, 39, 255));
                colors.Add(Color.FromRgba(255, 103, 89, 255));
                colors.Add(Color.FromRgba(255, 103, 89, 255));
                break;
            case "BLUE":
                colors.Add(Color.FromRgba(18, 88, 157, 255));
                colors.Add(Color.FromRgba(46, 164, 211, 255));
                colors.Add(Color.FromRgba(208, 74, 143, 255));
                colors.Add(Color.FromRgba(46, 164, 211, 255));
                break;
        }
        
        return colors;
    }
    
    private static async Task<string> ResolveImgurUrlAsync(string url, HttpClient httpClient, CancellationToken ct) {
        
        if (!url.Contains("imgur.com", StringComparison.OrdinalIgnoreCase) || url.Contains("i.imgur.com", StringComparison.OrdinalIgnoreCase))
            return url;

        try {
            var html = await httpClient.GetStringAsync(url, ct);
            var match = Regex.Match(html, @"<meta\s+property=[""']og:image[""']\s+content=[""']([^""']+)[""']");

            return match.Success ? match.Groups[1].Value : url;
            
        } catch {
            return url;
        }
    }

    private static List<(Image img, Point pos)> ListBadges(HashSet<ulong> roleIds) {
        
        var badgesToDraw = new List<(Image img, Point pos)>();
        
        if (roleIds.Contains(1473371574710046840)) badgesToDraw.Add((LoadBadges("badge1.png", 150, 50),  new Point(1350, 324)));
        if (roleIds.Contains(1475889357629161523)) badgesToDraw.Add((LoadBadges("badge2.png", 150, 50),  new Point(1520, 324)));
        if (roleIds.Contains(1475898897174892769)) badgesToDraw.Add((LoadBadges("badge3.png", 150, 50),  new Point(1350, 380)));
        if (roleIds.Contains(1475899025851945081)) badgesToDraw.Add((LoadBadges("badge4.png", 150, 50),  new Point(1520, 380)));
        if (roleIds.Contains(1475899134337617980)) badgesToDraw.Add((LoadBadges("badge5.png", 150, 150), new Point(1350, 440)));
        if (roleIds.Contains(1475899268593225829)) badgesToDraw.Add((LoadBadges("badge6.png", 150, 150), new Point(1520, 440)));
        if (roleIds.Contains(1475961765433970880)) badgesToDraw.Add((LoadBadges("badge7.png", 135, 135), new Point(1334, 570)));
        if (roleIds.Contains(1475899269335744564)) badgesToDraw.Add((LoadBadges("badge8.png", 135, 135), new Point(1427, 590)));
        if (roleIds.Contains(1477926845184872531)) badgesToDraw.Add((LoadBadges("badge9.png", 135, 135), new Point(1520, 570)));
        
        if (roleIds.Contains(1527905937329881158)) badgesToDraw.Add((LoadBadges("tanzaku_gold.png", 130, 180),  new Point(163,895)));
        if (roleIds.Contains(1527905990329110669)) badgesToDraw.Add((LoadBadges("tanzaku_silver.png", 130, 180),  new Point(163,895)));
        if (roleIds.Contains(1527906014060609586)) badgesToDraw.Add((LoadBadges("tanzaku_slip.png", 130, 180),  new Point(163,895)));
        
        return badgesToDraw;
    }
}