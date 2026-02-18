using GBA.Domain.FilterEntities;

namespace GBA.Domain.Messages.ColumnItems;

public sealed class UpdateColumnItemMessage {
    public UpdateColumnItemMessage(ColumnItem columnItem) {
        ColumnItem = columnItem;
    }

    public ColumnItem ColumnItem { get; set; }
}