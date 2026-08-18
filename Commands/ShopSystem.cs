using Discord;
using Discord.WebSocket;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using Image = SixLabors.ImageSharp.Image;
using Color = Discord.Color;

namespace SMASSB.Commands;

public class ShopSystem {
    
    private DatabaseService _db;
    
    public ShopSystem(DatabaseService db) {
        _db = db;
    }
    
    public async Task PostShopContents(SocketSlashCommand command) {
        await command.DeferAsync().ConfigureAwait(false);

        var sakuraAttachment = await BuildShelfAttachment(
            "sakura-shelf.png",
            "sakura-showcase.png", "sakura-case-back.png", "sakura-charm-front.png", "sakura-wallpaper.png");

        var sangoAttachment = await BuildShelfAttachment(
            "sango-shelf.png",
            "sango-showcase.png", "sango-case-back.png", "sango-charm-front.png", "sango-wallpaper.png");

        var techAttachment = await BuildShelfAttachment(
            "tech-shelf.png",
            "tech-showcase.png", "tech-case-back.png", "tech-wallpaper.png");

        var idsAttachment = await BuildShelfAttachment(
            "ids-shelf.png",
            "pink-template.png", "red-template.png", "green-template.png", "blue-template.png");

        var items = new[] { "Bundle", "Case", "Charm", "Wallpaper", "ID" };
        
        var containerSakura = new ContainerBuilder()
            .WithAccentColor(new Color(254,201,209))
            .AddComponent(new TextDisplayBuilder().WithContent("❖・ Sakura-Themed WorkCell Addons!"))
            .AddComponent(new SeparatorBuilder().WithIsDivider(true).WithSpacing(SeparatorSpacingSize.Large))
            .AddComponent(new MediaGalleryBuilder()
                .AddItem(new MediaGalleryItemProperties(new UnfurledMediaItemProperties("attachment://sakura-shelf.png"))))
            .AddComponent(new ActionRowBuilder()
                .WithButton($"Buy {items[0]} :: ¥14k", customId: $"buy_item_1_{command.Channel.Id}", style: ButtonStyle.Secondary)
                .WithButton($"Buy {items[1]} :: ¥8k", customId: $"buy_item_2_{command.Channel.Id}", style: ButtonStyle.Secondary)
                .WithButton($"Buy {items[2]} :: ¥3k", customId: $"buy_item_3_{command.Channel.Id}", style: ButtonStyle.Secondary)
                .WithButton($"Buy {items[3]} :: ¥4k", customId: $"buy_item_4_{command.Channel.Id}", style: ButtonStyle.Secondary)
            );

        var containerSango = new ContainerBuilder()
            .WithAccentColor(new Color(160,41,39))
            .AddComponent(new TextDisplayBuilder().WithContent("❖・ SANGŌ-Themed WorkCell Addons!"))
            .AddComponent(new SeparatorBuilder().WithIsDivider(true).WithSpacing(SeparatorSpacingSize.Large))
            .AddComponent(new MediaGalleryBuilder()
                .AddItem(new MediaGalleryItemProperties(new UnfurledMediaItemProperties("attachment://sango-shelf.png"))))
            .AddComponent(new ActionRowBuilder()
                .WithButton($"Buy {items[0]} :: ¥14k", customId: $"buy_item_5_{command.Channel.Id}", style: ButtonStyle.Secondary)
                .WithButton($"Buy {items[1]} :: ¥8k", customId: $"buy_item_6_{command.Channel.Id}", style: ButtonStyle.Secondary)
                .WithButton($"Buy {items[2]} :: ¥3k", customId: $"buy_item_7_{command.Channel.Id}", style: ButtonStyle.Secondary)
                .WithButton($"Buy {items[3]} :: ¥4k", customId: $"buy_item_8_{command.Channel.Id}", style: ButtonStyle.Secondary)
            );

        var containerTech = new ContainerBuilder()
            .WithAccentColor(new Color(0,0,156))
            .AddComponent(new TextDisplayBuilder().WithContent("❖・ Tech-Themed WorkCell Addons!"))
            .AddComponent(new SeparatorBuilder().WithIsDivider(true).WithSpacing(SeparatorSpacingSize.Large))
            .AddComponent(new MediaGalleryBuilder()
                .AddItem(new MediaGalleryItemProperties(new UnfurledMediaItemProperties("attachment://tech-shelf.png"))))
            .AddComponent(new ActionRowBuilder()
                .WithButton($"Buy {items[0]} :: ¥11k", customId: $"buy_item_9_{command.Channel.Id}", style: ButtonStyle.Secondary)
                .WithButton($"Buy {items[1]} :: ¥8k", customId: $"buy_item_10_{command.Channel.Id}", style: ButtonStyle.Secondary)
                .WithButton($"Buy {items[3]} :: ¥4k", customId: $"buy_item_11_{command.Channel.Id}", style: ButtonStyle.Secondary)
            );

        var containerIds = new ContainerBuilder()
            .WithAccentColor(new Color(0,0,0))
            .AddComponent(new TextDisplayBuilder().WithContent("❖・ Custom IDs!"))
            .AddComponent(new SeparatorBuilder().WithIsDivider(true).WithSpacing(SeparatorSpacingSize.Large))
            .AddComponent(new MediaGalleryBuilder()
                .AddItem(new MediaGalleryItemProperties(new UnfurledMediaItemProperties("attachment://ids-shelf.png"))))
            .AddComponent(new ActionRowBuilder()
                .WithButton($"Buy Pink {items[4]} :: ¥10k", customId: $"buy_item_12_{command.Channel.Id}", style: ButtonStyle.Secondary)
                .WithButton($"Buy Red {items[4]} :: ¥10k", customId: $"buy_item_13_{command.Channel.Id}", style: ButtonStyle.Secondary)
                .WithButton($"Buy Green {items[4]} :: ¥10k", customId: $"buy_item_14_{command.Channel.Id}", style: ButtonStyle.Secondary)
                .WithButton($"Buy Blue {items[4]} :: ¥10k", customId: $"buy_item_15_{command.Channel.Id}", style: ButtonStyle.Secondary)
            );
        
        var containerGames = new ContainerBuilder()
            .WithAccentColor(new Color(255, 97, 79))
            .AddComponent(new TextDisplayBuilder().WithContent("❖・ Buy new apps!"))
            .AddComponent(new ActionRowBuilder()
                .WithButton($"Buy Rhythm Tengoku :: ¥10k", customId: $"buy_item_16_{command.Channel.Id}", style: ButtonStyle.Secondary)
            );

        var headerComponents = new ComponentBuilderV2()
            .WithTextDisplay("# ✦ SHOP . . . !\n\nCome spend your hard-earned yen here on brand new aesthetic additions and games !")
            .WithSeparator(new SeparatorBuilder().WithIsDivider(false))
            .AddComponent(new MediaGalleryBuilder().AddItem("https://images-ext-1.discordapp.net/external/a1WXHk8jklKgoXuWXK7nObO7inQOBXNFqt6zldi8NdE/https/64.media.tumblr.com/384045d1eed5c0aa490e00aa98456239/c6b43c8a326634f0-7e/s2048x3072/8ae54d651ee2b0f75768d902e80ff1ec77417d08.pnj?format=webp"))
            .WithTextDisplay("_ _")
            .Build();

        var channel = command.Channel;
        await channel.SendMessageAsync(components: headerComponents, flags: MessageFlags.ComponentsV2);

        async Task SendShop(ContainerBuilder container, FileAttachment attachment) { // note for the person fronting later: yes i know this is a bad idea. im sorry
            var components = new ComponentBuilderV2()
                .AddComponent(container)
                .Build();

            await channel.SendFilesAsync(
                attachments: new[] { attachment },
                components: components,
                flags: MessageFlags.ComponentsV2
            );
        }

        await SendShop(containerSakura, sakuraAttachment);
        await SendShop(containerSango, sangoAttachment);
        await SendShop(containerTech, techAttachment);
        await SendShop(containerIds, idsAttachment);
        await SendShop(containerGames, techAttachment);
    }

    private async Task<FileAttachment> BuildShelfAttachment(string outputFileName, params string[] itemFileNames) {
        using var shelf = Image.Load(Path.Combine(AppContext.BaseDirectory, "Images", "shelf.png"));

        var loadedItems = new Image[itemFileNames.Length];
        for (int i = 0; i < itemFileNames.Length; i++) {
            loadedItems[i] = Image.Load(Path.Combine(AppContext.BaseDirectory, "Images", itemFileNames[i]));
        }

        try {
            int slotWidth  = shelf.Width / loadedItems.Length;
            int shelfLineY = (int)(shelf.Height * 0.75); 

            shelf.Mutate(ctx => {
                for (int i = 0; i < loadedItems.Length; i++) {
                    var item = loadedItems[i];

                    bool isCharm = itemFileNames[i].Contains("charm", StringComparison.OrdinalIgnoreCase);
                    double sizeMultiplier = isCharm ? 1.75 : 1.0;

                    int maxItemHeight = (int)(shelfLineY * 0.85 * sizeMultiplier);
                    int maxItemWidth  = (int)(slotWidth * 0.8 * sizeMultiplier);

                    if (item.Width > maxItemWidth || item.Height > maxItemHeight) {
                        item.Mutate(o => o.Resize(new ResizeOptions {
                            Mode = ResizeMode.Max,
                            Size = new Size(maxItemWidth, maxItemHeight)
                        }));
                    }

                    double slotMultiplier = isCharm ? 1.2 : 1.0;
                    int charmYOffset = isCharm ? 230 : 0;
                    
                    int slotCenterX = slotWidth * i + slotWidth / 2;
                    int x = (int)((slotCenterX - item.Width / 2) * slotMultiplier);
                    int y = (shelfLineY - item.Height + 100) + charmYOffset;

                    ctx.DrawImage(item, new Point(x, y), 1f);
                }
            });

            var stream = new MemoryStream();
            await shelf.SaveAsPngAsync(stream);
            stream.Position = 0;

            return new FileAttachment(stream, outputFileName);
        } finally {
            foreach (var item in loadedItems)
                item.Dispose();
        }
    }

    public async Task Buy(int num, ulong ownerId, ulong channelId, SocketGuild guild) {

        var prices = new[] {8000, 3000, 4000, 10000};
        var channel = guild.GetTextChannel(channelId);
        string boughtName = null;
        string alreadyOwnedMessage = null;

        switch (num) {
            case 1:
                if (await OwnsCase(ownerId, "SAKURA") || await OwnsCharm(ownerId, "SAKURA") || await OwnsWallpaper(ownerId, "SAKURA")) {
                    alreadyOwnedMessage = "You already own part of the Sakura bundle!";
                    break;
                }
                if (await CheckBeforeBuy(ownerId, prices[0] + prices[1] + prices[2] - 1000)) {
                    await _db.GiveNewCase(ownerId, "SAKURA");
                    await _db.GiveNewCharm(ownerId, "SAKURA");
                    await _db.GiveNewWallpaper(ownerId, "SAKURA");
                    boughtName = "Sakura Full Bundle!";
                }
                break;
            case 2:
                if (await OwnsCase(ownerId, "SAKURA")) { alreadyOwnedMessage = "You already own the Sakura Case!"; break; }
                if (await CheckBeforeBuy(ownerId, prices[0])) {
                    await _db.GiveNewCase(ownerId, "SAKURA");
                    boughtName = "Sakura Case";
                }
                break;
            case 3:
                if (await OwnsCharm(ownerId, "SAKURA")) { alreadyOwnedMessage = "You already own the Sakura Charm!"; break; }
                if (await CheckBeforeBuy(ownerId, prices[1])) {
                    await _db.GiveNewCharm(ownerId, "SAKURA");
                    boughtName = "Sakura Charm";
                }
                break;
            case 4:
                if (await OwnsWallpaper(ownerId, "SAKURA")) { alreadyOwnedMessage = "You already own the Sakura Wallpaper!"; break; }
                if (await CheckBeforeBuy(ownerId, prices[2])) {
                    await _db.GiveNewWallpaper(ownerId, "SAKURA");
                    boughtName = "Sakura Wallpaper";
                }
                break;
            case 5:
                if (await OwnsCase(ownerId, "SANGO") || await OwnsCharm(ownerId, "SANGO") || await OwnsWallpaper(ownerId, "SANGO")) {
                    alreadyOwnedMessage = "You already own part of the Sango bundle!";
                    break;
                }
                if (await CheckBeforeBuy(ownerId, prices[0] + prices[1] + prices[2] - 1000)) {
                    await _db.GiveNewCase(ownerId, "SANGO");
                    await _db.GiveNewCharm(ownerId, "SANGO");
                    await _db.GiveNewWallpaper(ownerId, "SANGO");
                    boughtName = "Sango Full Bundle!";
                }
                break;
            case 6:
                if (await OwnsCase(ownerId, "SANGO")) { alreadyOwnedMessage = "You already own the Sango Case!"; break; }
                if (await CheckBeforeBuy(ownerId, prices[0])) {
                    await _db.GiveNewCase(ownerId, "SANGO");
                    boughtName = "Sango Case";
                }
                break;
            case 7:
                if (await OwnsCharm(ownerId, "SANGO")) { alreadyOwnedMessage = "You already own the Sango Charm!"; break; }
                if (await CheckBeforeBuy(ownerId, prices[1])) {
                    await _db.GiveNewCharm(ownerId, "SANGO");
                    boughtName = "Sango Charm";
                }
                break;
            case 8:
                if (await OwnsWallpaper(ownerId, "SANGO")) { alreadyOwnedMessage = "You already own the Sango Wallpaper!"; break; }
                if (await CheckBeforeBuy(ownerId, prices[2])) {
                    await _db.GiveNewWallpaper(ownerId, "SANGO");
                    boughtName = "Sango Wallpaper";
                }
                break;
            case 9:
                if (await OwnsCase(ownerId, "TECH") || await OwnsWallpaper(ownerId, "TECH")) {
                    alreadyOwnedMessage = "You already own part of the Tech bundle!";
                    break;
                }
                if (await CheckBeforeBuy(ownerId, prices[0] + prices[2] - 1000)) {
                    await _db.GiveNewCase(ownerId, "TECH");
                    await _db.GiveNewWallpaper(ownerId, "TECH");
                    boughtName = "Tech Bundle!";
                }
                break;
            case 10:
                if (await OwnsCase(ownerId, "TECH")) { alreadyOwnedMessage = "You already own the Tech Case!"; break; }
                if (await CheckBeforeBuy(ownerId, prices[0])) {
                    await _db.GiveNewCase(ownerId, "TECH");
                    boughtName = "Tech Case";
                }
                break;
            case 11:
                if (await OwnsWallpaper(ownerId, "TECH")) { alreadyOwnedMessage = "You already own the Tech Wallpaper!"; break; }
                if (await CheckBeforeBuy(ownerId, prices[2])) {
                    await _db.GiveNewWallpaper(ownerId, "TECH");
                    boughtName = "Tech Wallpaper";
                }
                break;
            case 12:
                if (await OwnsId(ownerId, "PINK")) { alreadyOwnedMessage = "You already own the Pink ID Skin!"; break; }
                if (await CheckBeforeBuy(ownerId, prices[3])) {
                    await _db.GiveNewId(ownerId, "PINK");
                    boughtName = "Pink ID Skin";
                }
                break;
            case 13:
                if (await OwnsId(ownerId, "RED")) { alreadyOwnedMessage = "You already own the Red ID Skin!"; break; }
                if (await CheckBeforeBuy(ownerId, prices[3])) {
                    await _db.GiveNewId(ownerId, "RED");
                    boughtName = "Red ID Skin";
                }
                break;
            case 14:
                if (await OwnsId(ownerId, "GREEN")) { alreadyOwnedMessage = "You already own the Green ID Skin!"; break; }
                if (await CheckBeforeBuy(ownerId, prices[3])) {
                    await _db.GiveNewId(ownerId, "GREEN");
                    boughtName = "Green ID Skin";
                }
                break;
            case 15:
                if (await OwnsId(ownerId, "BLUE")) { alreadyOwnedMessage = "You already own the Blue ID Skin!"; break; }
                if (await CheckBeforeBuy(ownerId, prices[3])) {
                    await _db.GiveNewId(ownerId, "BLUE");
                    boughtName = "Blue ID Skin";
                }
                break;
            case 16:
                if (await OwnsApp(ownerId, "RHYTHMTENGOKU")) { alreadyOwnedMessage = "You already own Rhythm Tengoku!"; break; }
                if (await CheckBeforeBuy(ownerId, prices[3])) {
                    await _db.GiveNewApp(ownerId, "RHYTHMTENGOKU");
                    boughtName = "Rhythm Tengoku";
                }
                break;
        }

        if (alreadyOwnedMessage != null) {
            var ownedMessage = await channel.SendMessageAsync(alreadyOwnedMessage);
            await Task.Delay(TimeSpan.FromSeconds(5));
            await ownedMessage.DeleteAsync();
            return;
        }

        if (boughtName == null) {
            var fundsMessage = await channel.SendMessageAsync("You don't have the funds for this.");
            await Task.Delay(TimeSpan.FromSeconds(5));
            await fundsMessage.DeleteAsync();
            return;
        }

        var message = await channel.SendMessageAsync($"<@{ownerId}> has bought a new **{boughtName}**! Enjoy. ^^");
        await Task.Delay(TimeSpan.FromSeconds(5));
        await message.DeleteAsync();
    }

    private async Task<bool> CheckBeforeBuy(ulong ownerId, int num) {

        if (await _db.GetYen(ownerId) >= num) 
            await _db.RemoveYen(ownerId, num);
        else
            return false;
        
        return true;
    }

    private async Task<bool> OwnsCase(ulong userId, string type) {
        return (await _db.GetCases(userId)).Contains(type);
    }

    private async Task<bool> OwnsCharm(ulong userId, string type) {
        return (await _db.GetCharms(userId)).Contains(type);
    }

    private async Task<bool> OwnsWallpaper(ulong userId, string type) {
        return (await _db.GetWallpapers(userId)).Contains(type);
    }

    private async Task<bool> OwnsId(ulong userId, string type) {
        return (await _db.GetIds(userId)).Contains(type);
    }
    
    private async Task<bool> OwnsApp(ulong userId, string type) {
        return (await _db.GetCollectedApps(userId)).Contains(type);
    }
}