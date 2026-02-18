using GBA.Domain.Entities;

namespace GBA.Domain.Messages.Categories;

public sealed class AddCategoryMessage {
    public AddCategoryMessage(Category category) {
        Category = category;
    }

    public Category Category { get; set; }
}