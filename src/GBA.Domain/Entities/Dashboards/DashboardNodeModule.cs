using System.Collections.Generic;

namespace GBA.Domain.Entities.Dashboards;

public sealed class DashboardNodeModule : EntityBase {
    public DashboardNodeModule() {
        Children = new HashSet<DashboardNode>();
    }

    public string Language { get; set; }

    public string Module { get; set; }

    public string Description { get; set; }

    public string CssClass { get; set; }

    public ICollection<DashboardNode> Children { get; set; }
}