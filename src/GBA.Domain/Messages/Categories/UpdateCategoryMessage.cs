using GBA.Domain.Entities;

namespace GBA.Domain.Messages.Categories;

public sealed class UpdateCategoryMessage {
    public UpdateCategoryMessage(Category category) {
        Category = category;
    }

    public Category Category { get; set; }
}