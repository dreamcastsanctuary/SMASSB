using Discord;
using Discord.WebSocket;

namespace SMASSB.Commands;

public class ShopSystem {
    
    private DatabaseService _db;
    
    public ShopSystem(DatabaseService db) {
        _db = db;
    }
    
    public async Task PostShopContents(SocketSlashCommand command) {
        await command.DeferAsync();
        
        var items = new[] { "Full Bundle!", "Case", "Charm", "Wallpaper", "ID Skin"};

        var containerSakura = new ContainerBuilder()
            .WithAccentColor(new Color(254,201,209))
            .AddComponent(new TextDisplayBuilder().WithContent("**Shop Contents**"))
            .AddComponent(new SeparatorBuilder().WithIsDivider(true).WithSpacing(SeparatorSpacingSize.Large))
            .AddComponent(new ActionRowBuilder()
                .WithButton($"Buy {items[0]}", customId: $"buy_item_1_{command.User.Id}_{command.Channel.Id}", style: ButtonStyle.Secondary)
                .WithButton($"Buy {items[1]}", customId: $"buy_item_2_{command.User.Id}_{command.Channel.Id}", style: ButtonStyle.Secondary)
                .WithButton($"Buy {items[2]}", customId: $"buy_item_3_{command.User.Id}_{command.Channel.Id}", style: ButtonStyle.Secondary)
                .WithButton($"Buy {items[3]}", customId: $"buy_item_4_{command.User.Id}_{command.Channel.Id}", style: ButtonStyle.Secondary)
            );
        
        var containerSango = new ContainerBuilder()
            .WithAccentColor(new Color(160,41,39))
            .AddComponent(new SeparatorBuilder().WithIsDivider(true).WithSpacing(SeparatorSpacingSize.Large))
            .AddComponent(new ActionRowBuilder()
                .WithButton($"Buy {items[0]}", customId: $"buy_item_5_{command.User.Id}_{command.Channel.Id}", style: ButtonStyle.Secondary)
                .WithButton($"Buy {items[1]}", customId: $"buy_item_6_{command.User.Id}_{command.Channel.Id}", style: ButtonStyle.Secondary)
                .WithButton($"Buy {items[2]}", customId: $"buy_item_7_{command.User.Id}_{command.Channel.Id}", style: ButtonStyle.Secondary)
                .WithButton($"Buy {items[3]}", customId: $"buy_item_8_{command.User.Id}_{command.Channel.Id}", style: ButtonStyle.Secondary)
            );
        
        var containerTech = new ContainerBuilder()
            .WithAccentColor(new Color(0,0,156))
            .AddComponent(new SeparatorBuilder().WithIsDivider(true).WithSpacing(SeparatorSpacingSize.Large))
            .AddComponent(new ActionRowBuilder()
                .WithButton($"Buy {items[0]}", customId: $"buy_item_9_{command.User.Id}_{command.Channel.Id}", style: ButtonStyle.Secondary)
                .WithButton($"Buy {items[1]}", customId: $"buy_item_10_{command.User.Id}_{command.Channel.Id}", style: ButtonStyle.Secondary)
                .WithButton($"Buy {items[2]}", customId: $"buy_item_11_{command.User.Id}_{command.Channel.Id}", style: ButtonStyle.Secondary)
            );
        
        var containerIds = new ContainerBuilder()
            .WithAccentColor(new Color(0,0,0))
            .AddComponent(new SeparatorBuilder().WithIsDivider(true).WithSpacing(SeparatorSpacingSize.Large))
            .AddComponent(new ActionRowBuilder()
                .WithButton($"Buy Pink {items[4]}", customId: $"buy_item_12_{command.User.Id}_{command.Channel.Id}", style: ButtonStyle.Secondary)
                .WithButton($"Buy Red {items[4]}", customId: $"buy_item_13_{command.User.Id}_{command.Channel.Id}", style: ButtonStyle.Secondary)
                .WithButton($"Buy Green {items[4]}", customId: $"buy_item_14_{command.User.Id}_{command.Channel.Id}", style: ButtonStyle.Secondary)
                .WithButton($"Buy Blue {items[4]}", customId: $"buy_item_15_{command.User.Id}_{command.Channel.Id}", style: ButtonStyle.Secondary)
            );

        var components = new ComponentBuilderV2()
            .WithTextDisplay(new TextDisplayBuilder().WithContent("temp"))
            .WithSeparator(new SeparatorBuilder().WithIsDivider(true))
            .AddComponent(containerSakura)
            .AddComponent(containerSango)
            .AddComponent(containerTech)
            .AddComponent(containerIds)
            .Build();

        await command.FollowupAsync(
            components: components,
            flags: MessageFlags.ComponentsV2
        );
    }

    public async Task Buy(int num, ulong ownerId, ulong channelId, SocketGuild guild) {

        var prices = new[] {8000, 3000, 4000, 10000};
        var channel = guild.GetTextChannel(channelId);
        
        switch (num) {
            
            case 1:
                if (await CheckBeforeBuy(ownerId, prices[0] + prices[1] + prices[2] - 1000)) {
                    await _db.GiveNewCase(ownerId, "SAKURA");
                    await _db.GiveNewCharm(ownerId, "SAKURA");
                    await _db.GiveNewWallpaper(ownerId, "SAKURA");
                }
                
                break;
            case 2:
                if (await CheckBeforeBuy(ownerId, prices[0]))
                    await _db.GiveNewCase(ownerId, "SAKURA");
                break;
            case 3:
                if (await CheckBeforeBuy(ownerId, prices[1]))
                    await _db.GiveNewCharm(ownerId, "SAKURA");
                break;
            case 4:
                if (await CheckBeforeBuy(ownerId, prices[2]))
                    await _db.GiveNewWallpaper(ownerId, "SAKURA");
                break;
            case 5:
                if (await CheckBeforeBuy(ownerId, prices[0] + prices[1] + prices[2] - 1000)) {
                    await _db.GiveNewCase(ownerId, "SANGO");
                    await _db.GiveNewCharm(ownerId, "SANGO");
                    await _db.GiveNewWallpaper(ownerId, "SANGO");
                }
                break;
            case 6:
                if (await CheckBeforeBuy(ownerId, prices[0]))
                    await _db.GiveNewCase(ownerId, "SANGO");
                break;
            case 7:
                if (await CheckBeforeBuy(ownerId, prices[1]))
                    await _db.GiveNewCharm(ownerId, "SANGO");
                break;
            case 8:
                if (await CheckBeforeBuy(ownerId, prices[2]))
                    await _db.GiveNewWallpaper(ownerId, "SANGO");
                break;
            case 9:
                if (await CheckBeforeBuy(ownerId, prices[0] + prices[2] - 1000)) {
                    await _db.GiveNewCase(ownerId, "TECH");
                    await _db.GiveNewWallpaper(ownerId, "TECH");
                }
                break;
            case 10:
                if (await CheckBeforeBuy(ownerId, prices[0]))
                    await _db.GiveNewCase(ownerId, "TECH");
                break;
            case 11:
                if (await CheckBeforeBuy(ownerId, prices[2]))
                    await _db.GiveNewWallpaper(ownerId, "TECH");
                break;
            case 12:
                if (await CheckBeforeBuy(ownerId, prices[3]))
                    await _db.GiveNewId(ownerId, "PINK");
                break;
            case 13:
                if (await CheckBeforeBuy(ownerId, prices[3]))
                    await _db.GiveNewId(ownerId, "RED");
                break;
            case 14:
                if (await CheckBeforeBuy(ownerId, prices[3]))
                    await _db.GiveNewId(ownerId, "GREEN");
                break;
            case 15:
                if (await CheckBeforeBuy(ownerId, prices[3]))
                    await _db.GiveNewId(ownerId, "BLUE");
                break;
        }

        var items = new[] { "Full Bundle!", "Case", "Charm", "Wallpaper", "ID Skin"};
        var message = await channel.SendMessageAsync("You have bought a new " + items[num] + "! Enjoy. ^^");
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(7));
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