using GBA.Domain.FilterEntities;

namespace GBA.Domain.Messages.Filters;

public sealed class GetAllFilterItemsMessage {
    public GetAllFilterItemsMessage(FilterEntityType type) {
        Type = type;
    }

    public FilterEntityType Type { get; set; }
}