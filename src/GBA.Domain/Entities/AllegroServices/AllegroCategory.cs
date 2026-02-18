using System.Collections.Generic;

namespace GBA.Domain.Entities.AllegroServices;

public sealed class AllegroCategory : EntityBase {
    public AllegroCategory() {
        SubCategories = new HashSet<AllegroCategory>();
    }

    public string Name { get; set; }

    public int CategoryId { get; set; }

    public int ParentCategoryId { get; set; }

    public int Position { get; set; }

    public bool IsLeaf { get; set; }

    public ICollection<AllegroCategory> SubCategories { get; set; }
}