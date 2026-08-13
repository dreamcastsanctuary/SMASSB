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
        
        var items = new[] { "temp1", "temp2", "temp3", "temp4" };

        var container = new ContainerBuilder()
            .WithAccentColor(Color.Blue)
            .AddComponent(new TextDisplayBuilder().WithContent("**Shop Contents**"))
            .AddComponent(new SeparatorBuilder().WithIsDivider(true))
            .AddComponent(new ActionRowBuilder()
                .WithButton($"Buy {items[0]}", customId: "buy_item_1", style: ButtonStyle.Secondary)
                .WithButton($"Buy {items[1]}", customId: "buy_item_2", style: ButtonStyle.Secondary)
                .WithButton($"Buy {items[2]}", customId: "buy_item_3", style: ButtonStyle.Secondary)
                .WithButton($"Buy {items[3]}", customId: "buy_item_4", style: ButtonStyle.Secondary)
            );

        var components = new ComponentBuilderV2()
            .WithTextDisplay(new TextDisplayBuilder().WithContent("temp"))
            .WithSeparator(new SeparatorBuilder().WithIsDivider(true))
            .AddComponent(container)
            .Build();

        await command.FollowupAsync(
            components: components,
            flags: MessageFlags.ComponentsV2
        );
    }
}