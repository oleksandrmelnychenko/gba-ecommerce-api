using System.Collections.Generic;
using GBA.Domain.Entities.AllegroServices;

namespace GBA.Domain.EntityHelpers;

public sealed class AllegroSearchResponse {
    public List<AllegroCategory> CategoryTree { get; set; }
}