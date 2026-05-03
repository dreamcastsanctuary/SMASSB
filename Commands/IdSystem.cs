using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using GenCode128;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Color = SixLabors.ImageSharp.Color;
using Image = SixLabors.ImageSharp.Image;

namespace SMASSB.Commands;

public class IdSystem {
    
    private DatabaseService _db;
    private static readonly HttpClient _httpClient = new HttpClient();
    
    public IdSystem(DatabaseService db) {
        _db = db;
        
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
                                     string bloodtypeParam,
                                     string catchphraseParam,
                                     string usernameParam) {
        
        var fontCollection = new FontCollection();
        var fontPath = Path.Combine(AppContext.BaseDirectory, "Fonts", "MonaspaceArgon-Bold.otf");
        var fontFamily = fontCollection.Add(fontPath);
        var font = fontFamily.CreateFont(50);
        var fontId = fontFamily.CreateFont(35);
        var fontSmall = fontFamily.CreateFont(35);
        
        var imgPath = Path.Combine(AppContext.BaseDirectory, "Images", "id-template.png");
        var idImg = Image.Load(imgPath);
        
        Image avatar;
        if (avatarImageParam != null) {
            using var avatarStream = new MemoryStream(avatarImageParam);
            avatar = Image.Load(avatarStream);
        } else {
            
            string sizedAvatarUrl = avatarUrlParam.Contains('?')
                ? avatarUrlParam + "&size=4096"
                : avatarUrlParam + "?size=4096";
            var avatarBytes = await _httpClient.GetByteArrayAsync(sizedAvatarUrl);
            using var avatarStream = new MemoryStream(avatarBytes);
            avatar = Image.Load(avatarStream);
        }

        avatar.Mutate(x => x.Resize(new ResizeOptions {
            Size = new Size(250, 250),
            Mode = ResizeMode.Crop,
            Sampler = KnownResamplers.Lanczos3
        }));
        
        var name = claimParam;
        var accId = accIdParam;
        var date = dateParam;
        var rank = rankParam;
        var points = pointsParam;
        var bloodtype = bloodtypeParam;
        var catchphrase = catchphraseParam;
        Color golden = Color.FromRgba(190, 164, 95, 255);
        System.Drawing.Image barcode = Code128Rendering.MakeBarcodeImage(usernameParam, 1, true);

        var namePos = new Point(827, 452);
        var avatarPos = new Point(93,373);
        var idPos = new Point(1219,154);
        var datePos = new Point(827,529);
        var rankPos = new Point(827,602);
        var pointsPos = new Point(827,687);
        var bloodtypePos = new Point(827,764);
        var catchphrasePos = new Point(70,953);
        var barcodePos = new Point(95,688);
        
        var stream = new MemoryStream();
        barcode.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
        stream.Position = 0;
        var barcodeImg = Image.Load(stream);
        using var redBarcode = new Image<Rgba32>(barcodeImg.Width, barcodeImg.Height, new Rgba32(255, 49, 44, 255));
        redBarcode.Mutate(ctx => ctx.DrawImage(barcodeImg, new Point(0, 0), PixelColorBlendingMode.Multiply, PixelAlphaCompositionMode.SrcOver, 1f));
        redBarcode.Mutate(x => x.Resize(new ResizeOptions {
            Size = new Size(250, 50),
            Mode = ResizeMode.Crop,
            Sampler = KnownResamplers.Lanczos3
        }));
        
        var roleIds = member.Roles.Select(r => r.Id).ToHashSet();
        var badgesToDraw = new List<(Image img, Point pos)>();

        if (roleIds.Contains(1473371574710046840)) badgesToDraw.Add((LoadBadges("badge1.png", 150, 50),  new Point(1344, 317)));
        if (roleIds.Contains(1475889357629161523)) badgesToDraw.Add((LoadBadges("badge2.png", 150, 50),  new Point(1510, 317)));
        if (roleIds.Contains(1475898897174892769)) badgesToDraw.Add((LoadBadges("badge3.png", 150, 50),  new Point(1344, 395)));
        if (roleIds.Contains(1475899025851945081)) badgesToDraw.Add((LoadBadges("badge4.png", 150, 50),  new Point(1510, 395)));
        if (roleIds.Contains(1475899134337617980)) badgesToDraw.Add((LoadBadges("badge5.png", 150, 150), new Point(1344, 460)));
        if (roleIds.Contains(1475899268593225829)) badgesToDraw.Add((LoadBadges("badge6.png", 150, 150), new Point(1510, 460)));
        if (roleIds.Contains(1475961765433970880)) badgesToDraw.Add((LoadBadges("badge7.png", 135, 135), new Point(1334, 600)));
        if (roleIds.Contains(1475899269335744564)) badgesToDraw.Add((LoadBadges("badge8.png", 135, 135), new Point(1427, 626)));
        if (roleIds.Contains(1477926845184872531)) badgesToDraw.Add((LoadBadges("badge9.png", 135, 135), new Point(1520, 600)));
        
        var clone = idImg.Clone(ipc => {
            
            ipc.DrawImage(avatar, avatarPos, 1);
            ipc.DrawImage(redBarcode, barcodePos, 1);
            ipc.DrawText($"{points}", font, golden, pointsPos);
            ipc.DrawText($"{bloodtype}", font, golden, bloodtypePos);
            ipc.DrawText($"{catchphrase}", font, golden, catchphrasePos);
            ipc.DrawText($"{date:M/d/yyyy}", font, golden, datePos);
            ipc.DrawText($"{accId}", fontId, golden, idPos);

            if (name.Length > 15) {
                
                var spaceIndex = name.IndexOf(' ');
                if (spaceIndex > 0) {
                    var firstName = name.Substring(0, spaceIndex);
                    var lastName = name.Substring(spaceIndex + 1);
                    name = $"{firstName}\n{lastName}";
                }
                
                ipc.DrawText($"{name}", fontSmall, golden, new Point(namePos.X, namePos.Y + 5));
            } else ipc.DrawText($"{name}", font, golden, namePos);
            
            if (rank.Contains("taru")) {
                rank = "Bakuryōchō\ntaru Onchō";
                ipc.DrawText($"{rank}", fontSmall, golden, rankPos);
                
            } else ipc.DrawText($"{rank}", font, golden, rankPos);
            
            foreach (var (img, pos) in badgesToDraw)
                ipc.DrawImage(img, pos, 1);
            
        });

        var output = Path.Combine(Path.GetTempPath(), $"id_{accId}.png");
        var channel = command.Channel;
        
        clone.Save(output);
        foreach (var (img, _) in badgesToDraw) img.Dispose();
        
        if (member != command.User) {
            await UserExtensions.SendFileAsync(member, output, "Here you are, your brand new Idol ID!");
        } else {
            await channel.SendFileAsync(output, "Loaded Idol ID . . !"); 
        }
        
        File.Delete(output);
    }
    
    [DefaultMemberPermissions(GuildPermission.CreatePublicThreads)]
    public async Task EditId(SocketSlashCommand command, DiscordSocketClient client) {

        SocketGuildUser enlisted = (SocketGuildUser)command.User;
        string claim = null;
        string avatarUrl = null;
        string catchphrase = null;
        string bloodtype = null;
        
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
                default:
                    await command.RespondAsync("Unrecognized command.", ephemeral: true);
                    return;
            }
        }
        
        if (!string.IsNullOrEmpty(avatarUrl)) {
            await _db.SetAvatarUrl(enlisted.Id, avatarUrl);
    
            var bytes = await _httpClient.GetByteArrayAsync(avatarUrl);
            await _db.SetAvatarImage(enlisted.Id, bytes);
        }

        if (!string.IsNullOrEmpty(claim)) {
            await _db.SetClaim(enlisted.Id, claim);
        }
        
        if (!string.IsNullOrEmpty(bloodtype)) {
            await _db.SetBloodtype(enlisted.Id, bloodtype);
        }
        
        string claimParam = await _db.GetClaim(enlisted.Id);
        string avatarUrlParam = await _db.GetAvatarUrl(enlisted.Id);
        string accIdParam = enlisted.Id.ToString();
        DateTimeOffset dateParam = enlisted.JoinedAt ?? enlisted.CreatedAt;
        string rankParam = await _db.GetRank(enlisted.Id);
        int pointsParam = await _db.GetPoints(enlisted.Id);
        string bloodtypeParam = await _db.GetBloodtype(enlisted.Id);
        string usernameParam = await _db.GetUsername(enlisted.Id);
        byte[]? avatarImageParam = await _db.GetAvatarImage(enlisted.Id);
        
        await command.RespondAsync("Loading Idol ID . .", ephemeral: true);
        await BuildId(command, enlisted, claimParam, avatarImageParam, avatarUrlParam, accIdParam, dateParam, rankParam, pointsParam, bloodtypeParam, "Go Strike!", usernameParam);

    }
    
    [DefaultMemberPermissions(GuildPermission.CreatePublicThreads)]
    public async Task ShowId(SocketSlashCommand command, DiscordSocketClient client) {
        
        SocketGuildUser enlisted = (SocketGuildUser)command.User;
        
        string claimParam = await _db.GetClaim(enlisted.Id);
        string avatarUrlParam = await _db.GetAvatarUrl(enlisted.Id);
        string accIdParam = enlisted.Id.ToString();
        DateTimeOffset dateParam = enlisted.JoinedAt ?? enlisted.CreatedAt;
        string rankParam = await _db.GetRank(enlisted.Id);
        int pointsParam = await _db.GetPoints(enlisted.Id);
        string bloodtypeParam = await _db.GetBloodtype(enlisted.Id);
        string usernameParam = await _db.GetUsername(enlisted.Id);
        byte[]? avatarImageParam = await _db.GetAvatarImage(enlisted.Id);
        
        await command.RespondAsync("Loading Idol ID . .", ephemeral: true);
        await BuildId(command, enlisted, claimParam, avatarImageParam, avatarUrlParam, accIdParam, dateParam, rankParam, pointsParam, bloodtypeParam, "Go Strike!", usernameParam);
    }
    
    static Image LoadBadges(string filename, int w, int h) {
        var path = Path.Combine(AppContext.BaseDirectory, "Images", filename);
        var img = Image.Load(path);
        img.Mutate(x => x.Resize(new ResizeOptions {
            Size = new Size(w, h),
            Mode = ResizeMode.Crop,
            Sampler = KnownResamplers.Lanczos3
        }));
        return img;
    }
}