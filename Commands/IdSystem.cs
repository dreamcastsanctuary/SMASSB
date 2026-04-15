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
                                     string avatarUrlParam,
                                     string accIdParam,
                                     DateTimeOffset dateParam,
                                     string rankParam,
                                     int pointsParam,
                                     string bloodtypeParam,
                                     string catchphraseParam,
                                     string usernameParam) {
        
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36"
        );
        
        var fontCollection = new FontCollection();
        var fontPath = Path.Combine(AppContext.BaseDirectory, "Fonts", "MonaspaceArgon-Bold.otf");
        var fontFamily = fontCollection.Add(fontPath);
        var font = fontFamily.CreateFont(50);
        var fontId = fontFamily.CreateFont(35);
        var fontRank = fontFamily.CreateFont(35);
        var imgPath = Path.Combine(AppContext.BaseDirectory, "Images", "id-template.png");
        var idImg = Image.Load(imgPath);
        
        string sizedAvatarUrl = avatarUrlParam + "?size=4096";
        var avatarBytes = await _httpClient.GetByteArrayAsync(sizedAvatarUrl);
        using var avatarStream = new MemoryStream(avatarBytes);
        var avatar = Image.Load(avatarStream);
        avatar.Mutate(x => x.Resize(new ResizeOptions {
            Size = new Size(250, 250),
            Mode = ResizeMode.Crop,
            Sampler = KnownResamplers.Lanczos3
        }));
        
        var badge1 = await _httpClient.GetByteArrayAsync("https://64.media.tumblr.com/f2694484a6a988155f4eaaead393cb1e/1a0075520b2ed1f2-be/s2048x3072/d41162201408b15a0e3156e11d4437a985db7acc.pnj");
        var badge2 = await _httpClient.GetByteArrayAsync("https://64.media.tumblr.com/5b3c5faa19f08b6c74c6138cedf36ad3/1a0075520b2ed1f2-4f/s2048x3072/fe51341a79a92987dbed3809d9342539c614dd29.pnj");
        var badge3 = await _httpClient.GetByteArrayAsync("https://64.media.tumblr.com/0901b17238e2ff04018b728884056777/1a0075520b2ed1f2-4c/s2048x3072/b4147b46cfa2600f8bae36b879f9c0201009dfed.pnj");
        var badge4 = await _httpClient.GetByteArrayAsync("https://64.media.tumblr.com/efc24b5cd36da99c8c40a518003941f5/1a0075520b2ed1f2-0a/s2048x3072/91fe3b5806e46fd1b5115dd8f578b97da435f140.pnj");
        var badge5 = await _httpClient.GetByteArrayAsync("https://64.media.tumblr.com/45abb2ba5f882bda33def1df3b2ab445/1a0075520b2ed1f2-a7/s2048x3072/8cb36fd581e73111d7b3a99f837a15f3edefea78.pnj");
        var badge6 = await _httpClient.GetByteArrayAsync("https://64.media.tumblr.com/441b822d5961524c1a11d3a053b9af95/1a0075520b2ed1f2-97/s2048x3072/c99191454f3b9e55eb927b86e263d9f74694a3b9.pnj");
        var badge7 = await _httpClient.GetByteArrayAsync("https://64.media.tumblr.com/389151ebc620c390160c58c06fc7b1e9/1a0075520b2ed1f2-7b/s2048x3072/b7520d32000572c95167e1f85373cbb9a56d590a.pnj");
        var badge8 = await _httpClient.GetByteArrayAsync("https://64.media.tumblr.com/66404be7766ce8b64cd71a430672b50f/1a0075520b2ed1f2-7c/s2048x3072/895f6838f60a56498215b9c3198a88635506ba97.pnj");
        var badge9 = await _httpClient.GetByteArrayAsync("https://64.media.tumblr.com/744e75e8e6e02c8e569bc9419df5f1d2/1a0075520b2ed1f2-21/s2048x3072/2a418c1cb7d8b3c476ad436e6a3f7f2fd3dc1a2f.pnj");

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
        var avatarPos = new Point(93,372);
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
        var redBarcode = new Image<Rgba32>(barcodeImg.Width, barcodeImg.Height, new Rgba32(255, 49, 44, 255));
        redBarcode.Mutate(ctx => ctx.DrawImage(barcodeImg, new Point(0, 0), PixelColorBlendingMode.Multiply, PixelAlphaCompositionMode.SrcOver, 1f));
        redBarcode.Mutate(x => x.Resize(new ResizeOptions {
            Size = new Size(250, 50),
            Mode = ResizeMode.Crop,
            Sampler = KnownResamplers.Lanczos3
        }));
        
        var clone = idImg.Clone(ipc => {
            
            ipc.DrawImage(avatar, avatarPos, 1);
            ipc.DrawImage(redBarcode, barcodePos, 1);
            ipc.DrawText($"{name}", font, golden, namePos);
            ipc.DrawText($"{points}", font, golden, pointsPos);
            ipc.DrawText($"{bloodtype}", font, golden, bloodtypePos);
            ipc.DrawText($"{catchphrase}", font, golden, catchphrasePos);
            ipc.DrawText($"{date:M/d/yyyy}", font, golden, datePos);
            if (rank.Contains("taru")) {
                rank = "Bakuryōchō\ntaru Onchō";
                ipc.DrawText($"{rank}", fontRank, golden, rankPos);
            } else {
                ipc.DrawText($"{rank}", font, golden, rankPos);
            }
            ipc.DrawText($"{accId}", fontId, golden, idPos);
            
                foreach (IRole role in member.Roles) {
                    switch (role.Id) {
                        
                        case 1473371574710046840:
                            var badge1Stream = new MemoryStream(badge1);
                            var badgeFin1 = Image.Load(badge1Stream);
                            badgeFin1.Mutate(x => x.Resize(new ResizeOptions {
                                Size = new Size(150, 50),
                                Mode = ResizeMode.Crop,
                                Sampler = KnownResamplers.Lanczos3
                            }));
                            ipc.DrawImage(badgeFin1, new Point(1344,317), 1);
                            break;
                        
                        case 1475889357629161523:
                            var badge2Stream = new MemoryStream(badge2);
                            var badgeFin2 = Image.Load(badge2Stream);
                            badgeFin2.Mutate(x => x.Resize(new ResizeOptions {
                                Size = new Size(150, 50),
                                Mode = ResizeMode.Crop,
                                Sampler = KnownResamplers.Lanczos3
                            }));
                            ipc.DrawImage(badgeFin2, new Point(1510,317), 1);
                            break;
                        
                        case 1475898897174892769:
                            var badge3Stream = new MemoryStream(badge3);
                            var badgeFin3 = Image.Load(badge3Stream);
                            badgeFin3.Mutate(x => x.Resize(new ResizeOptions {
                                Size = new Size(150, 50),
                                Mode = ResizeMode.Crop,
                                Sampler = KnownResamplers.Lanczos3
                            }));
                            ipc.DrawImage(badgeFin3, new Point(1344,395), 1);
                            break;
                        
                        case 1475899025851945081:
                            var badge4Stream = new MemoryStream(badge4);
                            var badgeFin4 = Image.Load(badge4Stream);
                            badgeFin4.Mutate(x => x.Resize(new ResizeOptions {
                                Size = new Size(150, 50),
                                Mode = ResizeMode.Crop,
                                Sampler = KnownResamplers.Lanczos3
                            }));
                            ipc.DrawImage(badgeFin4, new Point(1510,395), 1);
                            break;
                        
                        case 1475899134337617980:
                            var badge5Stream = new MemoryStream(badge5);
                            var badgeFin5 = Image.Load(badge5Stream);
                            badgeFin5.Mutate(x => x.Resize(new ResizeOptions {
                                Size = new Size(150, 150),
                                Mode = ResizeMode.Crop,
                                Sampler = KnownResamplers.Lanczos3
                            }));
                            ipc.DrawImage(badgeFin5, new Point(1344,460), 1);
                            break;
                        
                        case 1475899268593225829:
                            var badge6Stream = new MemoryStream(badge6);
                            var badgeFin6 = Image.Load(badge6Stream);
                            badgeFin6.Mutate(x => x.Resize(new ResizeOptions {
                                Size = new Size(150, 150),
                                Mode = ResizeMode.Crop,
                                Sampler = KnownResamplers.Lanczos3
                            }));
                            ipc.DrawImage(badgeFin6, new Point(1510,460), 1);
                            break;
                        
                        case 1475961765433970880:
                            var badge7Stream = new MemoryStream(badge7);
                            var badgeFin7 = Image.Load(badge7Stream);
                            badgeFin7.Mutate(x => x.Resize(new ResizeOptions {
                                Size = new Size(135, 135),
                                Mode = ResizeMode.Crop,
                                Sampler = KnownResamplers.Lanczos3
                            }));
                            ipc.DrawImage(badgeFin7, new Point(1334,600), 1);
                            break;
                        
                        case 1475899269335744564:
                            var badge8Stream = new MemoryStream(badge8);
                            var badgeFin8 = Image.Load(badge8Stream);
                            badgeFin8.Mutate(x => x.Resize(new ResizeOptions {
                                Size = new Size(135, 135),
                                Mode = ResizeMode.Crop,
                                Sampler = KnownResamplers.Lanczos3
                            }));
                            ipc.DrawImage(badgeFin8, new Point(1427,626), 1);
                            break;
                        
                        case 1477926845184872531:
                            var badge9Stream = new MemoryStream(badge9);
                            var badgeFin9 = Image.Load(badge9Stream);
                            badgeFin9.Mutate(x => x.Resize(new ResizeOptions {
                                Size = new Size(135, 135),
                                Mode = ResizeMode.Crop,
                                Sampler = KnownResamplers.Lanczos3
                            }));
                            ipc.DrawImage(badgeFin9, new Point(1520,600), 1);
                            break;
                    }
                }
        });

        var output = Path.Combine(Path.GetTempPath(), $"id_{accId}.png");
        var channel = command.Channel;
        
        clone.Save(output);
        
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
                    break;
            }
        }
        
        await _db.SetClaim(enlisted.Id, claim);
        await _db.SetAvatarUrl(enlisted.Id, avatarUrl);
        await _db.SetBloodtype(enlisted.Id, bloodtype);
        
        string claimParam = await _db.GetClaim(enlisted.Id);
        string avatarUrlParam = await _db.GetAvatarUrl(enlisted.Id);
        string accIdParam = enlisted.Id.ToString();
        DateTimeOffset dateParam = enlisted.JoinedAt ?? enlisted.CreatedAt;
        string rankParam = await _db.GetRank(enlisted.Id);
        int pointsParam = await _db.GetPoints(enlisted.Id);
        string bloodtypeParam = await _db.GetBloodtype(enlisted.Id);
        string usernameParam = await _db.GetUsername(enlisted.Id);
        
        await command.RespondAsync("Loading Idol ID . .", ephemeral: true);
        await BuildId(command, enlisted, claimParam, avatarUrlParam, accIdParam, dateParam, rankParam, pointsParam, bloodtypeParam, "Go Strike!", usernameParam);

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
        
        await command.RespondAsync("Loading Idol ID . .", ephemeral: true);
        await BuildId(command, enlisted, claimParam, avatarUrlParam, accIdParam, dateParam, rankParam, pointsParam, bloodtypeParam, "Go Strike!", usernameParam);
    }
}