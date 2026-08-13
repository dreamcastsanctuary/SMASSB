using Discord;
using Discord.WebSocket;

namespace SMASSB.Commands;

public class ShopSystem {
    
    private DatabaseService _db;
    
    public ShopSystem(DatabaseService db) {
        _db = db;
    }
    
    public async Task PostShopContents(SocketSlashCommand command) {
        
        var container = new ContainerBuilder()
            //.AddComponent(new MediaGalleryBuilder().AddItem(new MediaGalleryItemProperties("temp")))
            .AddComponent(new ActionRowBuilder()
                .WithButton($"Buy {"tempItemOne"}", style: ButtonStyle.Secondary)
                .WithButton($"Buy {"tempItemTwo"}", style: ButtonStyle.Secondary)
                .WithButton($"Buy {"tempItemThree"}", style: ButtonStyle.Secondary)
                .WithButton($"Buy {"tempItemFour"}", style: ButtonStyle.Secondary)
            );
        
        var components = new ComponentBuilderV2()
            .WithTextDisplay(new TextDisplayBuilder().WithContent("temp"))
            .WithSeparator(new SeparatorBuilder().WithIsDivider(true))
            .WithSeparator(new SeparatorBuilder().WithIsDivider(false))
            .AddComponent(container)
            .Build();
        
        await command.FollowupWithFilesAsync(
            attachments: new[] { new FileAttachment() },
            components: components,
            flags: MessageFlags.ComponentsV2
        );

    }
}