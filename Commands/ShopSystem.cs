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
        await command.DeferAsync();

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

        var items = new[] { "Full Bundle!", "Case", "Charm", "Wallpaper", "ID Skin" };

        var containerSakura = new ContainerBuilder()
            .WithAccentColor(new Color(254,201,209))
            .AddComponent(new TextDisplayBuilder().WithContent("**Shop Contents**"))
            .AddComponent(new MediaGalleryBuilder()
                .AddItem(new MediaGalleryItemProperties(new UnfurledMediaItemProperties("attachment://sakura-shelf.png"))))
            .AddComponent(new SeparatorBuilder().WithIsDivider(true).WithSpacing(SeparatorSpacingSize.Large))
            .AddComponent(new ActionRowBuilder()
                .WithButton($"Buy {items[0]}", customId: $"buy_item_1_{command.Channel.Id}", style: ButtonStyle.Secondary)
                .WithButton($"Buy {items[1]}", customId: $"buy_item_2_{command.Channel.Id}", style: ButtonStyle.Secondary)
                .WithButton($"Buy {items[2]}", customId: $"buy_item_3_{command.Channel.Id}", style: ButtonStyle.Secondary)
                .WithButton($"Buy {items[3]}", customId: $"buy_item_4_{command.Channel.Id}", style: ButtonStyle.Secondary)
            );

        var containerSango = new ContainerBuilder()
            .WithAccentColor(new Color(160,41,39))
            .AddComponent(new MediaGalleryBuilder()
                .AddItem(new MediaGalleryItemProperties(new UnfurledMediaItemProperties("attachment://sango-shelf.png"))))
            .AddComponent(new SeparatorBuilder().WithIsDivider(true).WithSpacing(SeparatorSpacingSize.Large))
            .AddComponent(new ActionRowBuilder()
                .WithButton($"Buy {items[0]}", customId: $"buy_item_5_{command.Channel.Id}", style: ButtonStyle.Secondary)
                .WithButton($"Buy {items[1]}", customId: $"buy_item_6_{command.Channel.Id}", style: ButtonStyle.Secondary)
                .WithButton($"Buy {items[2]}", customId: $"buy_item_7_{command.Channel.Id}", style: ButtonStyle.Secondary)
                .WithButton($"Buy {items[3]}", customId: $"buy_item_8_{command.Channel.Id}", style: ButtonStyle.Secondary)
            );

        var containerTech = new ContainerBuilder()
            .WithAccentColor(new Color(0,0,156))
            .AddComponent(new MediaGalleryBuilder()
                .AddItem(new MediaGalleryItemProperties(new UnfurledMediaItemProperties("attachment://tech-shelf.png"))))
            .AddComponent(new SeparatorBuilder().WithIsDivider(true).WithSpacing(SeparatorSpacingSize.Large))
            .AddComponent(new ActionRowBuilder()
                .WithButton($"Buy {items[0]}", customId: $"buy_item_9_{command.Channel.Id}", style: ButtonStyle.Secondary)
                .WithButton($"Buy {items[1]}", customId: $"buy_item_10_{command.Channel.Id}", style: ButtonStyle.Secondary)
                .WithButton($"Buy {items[2]}", customId: $"buy_item_11_{command.Channel.Id}", style: ButtonStyle.Secondary)
            );

        var containerIds = new ContainerBuilder()
            .WithAccentColor(new Color(0,0,0))
            .AddComponent(new MediaGalleryBuilder()
                .AddItem(new MediaGalleryItemProperties(new UnfurledMediaItemProperties("attachment://ids-shelf.png"))))
            .AddComponent(new SeparatorBuilder().WithIsDivider(true).WithSpacing(SeparatorSpacingSize.Large))
            .AddComponent(new ActionRowBuilder()
                .WithButton($"Buy Pink {items[4]}", customId: $"buy_item_12_{command.Channel.Id}", style: ButtonStyle.Secondary)
                .WithButton($"Buy Red {items[4]}", customId: $"buy_item_13_{command.Channel.Id}", style: ButtonStyle.Secondary)
                .WithButton($"Buy Green {items[4]}", customId: $"buy_item_14_{command.Channel.Id}", style: ButtonStyle.Secondary)
                .WithButton($"Buy Blue {items[4]}", customId: $"buy_item_15_{command.Channel.Id}", style: ButtonStyle.Secondary)
            );

        var components = new ComponentBuilderV2()
            .WithSeparator(new SeparatorBuilder().WithIsDivider(false))
            .AddComponent(containerSakura)
            .AddComponent(containerSango)
            .AddComponent(containerTech)
            .AddComponent(containerIds)
            .Build();

        await command.FollowupWithFilesAsync(
            attachments: new[] { sakuraAttachment, sangoAttachment, techAttachment, idsAttachment },
            components: components,
            flags: MessageFlags.ComponentsV2
        );
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
                    int charmYOffset = isCharm ? 100 : 0;
                    
                    int slotCenterX = slotWidth * i + slotWidth / 2;
                    int x = (int)((slotCenterX - item.Width / 2) * slotMultiplier);
                    int y = (int)((shelfLineY - item.Height) / slotMultiplier + 5) + charmYOffset;

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

        switch (num) {
            case 1:
                if (await CheckBeforeBuy(ownerId, prices[0] + prices[1] + prices[2] - 1000)) {
                    await _db.GiveNewCase(ownerId, "SAKURA");
                    await _db.GiveNewCharm(ownerId, "SAKURA");
                    await _db.GiveNewWallpaper(ownerId, "SAKURA");
                    boughtName = "Sakura Full Bundle!";
                }
                break;
            case 2:
                if (await CheckBeforeBuy(ownerId, prices[0])) {
                    await _db.GiveNewCase(ownerId, "SAKURA");
                    boughtName = "Sakura Case";
                }
                break;
            case 3:
                if (await CheckBeforeBuy(ownerId, prices[1])) {
                    await _db.GiveNewCharm(ownerId, "SAKURA");
                    boughtName = "Sakura Charm";
                }
                break;
            case 4:
                if (await CheckBeforeBuy(ownerId, prices[2])) {
                    await _db.GiveNewWallpaper(ownerId, "SAKURA");
                    boughtName = "Sakura Wallpaper";
                }
                break;
            case 5:
                if (await CheckBeforeBuy(ownerId, prices[0] + prices[1] + prices[2] - 1000)) {
                    await _db.GiveNewCase(ownerId, "SANGO");
                    await _db.GiveNewCharm(ownerId, "SANGO");
                    await _db.GiveNewWallpaper(ownerId, "SANGO");
                    boughtName = "Sango Full Bundle!";
                }
                break;
            case 6:
                if (await CheckBeforeBuy(ownerId, prices[0])) {
                    await _db.GiveNewCase(ownerId, "SANGO");
                    boughtName = "Sango Case";
                }
                break;
            case 7:
                if (await CheckBeforeBuy(ownerId, prices[1])) {
                    await _db.GiveNewCharm(ownerId, "SANGO");
                    boughtName = "Sango Charm";
                }
                break;
            case 8:
                if (await CheckBeforeBuy(ownerId, prices[2])) {
                    await _db.GiveNewWallpaper(ownerId, "SANGO");
                    boughtName = "Sango Wallpaper";
                }
                break;
            case 9:
                if (await CheckBeforeBuy(ownerId, prices[0] + prices[2] - 1000)) {
                    await _db.GiveNewCase(ownerId, "TECH");
                    await _db.GiveNewWallpaper(ownerId, "TECH");
                    boughtName = "Tech Bundle!";
                }
                break;
            case 10:
                if (await CheckBeforeBuy(ownerId, prices[0])) {
                    await _db.GiveNewCase(ownerId, "TECH");
                    boughtName = "Tech Case";
                }
                break;
            case 11:
                if (await CheckBeforeBuy(ownerId, prices[2])) {
                    await _db.GiveNewWallpaper(ownerId, "TECH");
                    boughtName = "Tech Wallpaper";
                }
                break;
            case 12:
                if (await CheckBeforeBuy(ownerId, prices[3])) {
                    await _db.GiveNewId(ownerId, "PINK");
                    boughtName = "Pink ID Skin";
                }
                break;
            case 13:
                if (await CheckBeforeBuy(ownerId, prices[3])) {
                    await _db.GiveNewId(ownerId, "RED");
                    boughtName = "Red ID Skin";
                }
                break;
            case 14:
                if (await CheckBeforeBuy(ownerId, prices[3])) {
                    await _db.GiveNewId(ownerId, "GREEN");
                    boughtName = "Green ID Skin";
                }
                break;
            case 15:
                if (await CheckBeforeBuy(ownerId, prices[3])) {
                    await _db.GiveNewId(ownerId, "BLUE");
                    boughtName = "Blue ID Skin";
                }
                break;
        }

        if (boughtName == null) {
            await channel.SendMessageAsync("You don't have the funds for this.");
            return;
        }

        var message = await channel.SendMessageAsync($"@<{ownerId}> has bought a new {boughtName}! Enjoy. ^^");
        await Task.Delay(TimeSpan.FromSeconds(7));
        await message.DeleteAsync();
    }

    private async Task<bool> CheckBeforeBuy(ulong ownerId, int num) {

        if (await _db.GetYen(ownerId) >= num) 
            await _db.RemoveYen(ownerId, num);
        else
            return false;
        
        return true;
    }
}