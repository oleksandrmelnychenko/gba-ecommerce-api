using System;

namespace GBA.Domain.EntityHelpers.GbaDataExportModels;

public sealed class ProductGroupDto {
    public Guid NetUId { get; set; }
    public string Name { get; set; }
    public Guid? ParentNetUid { get; set; }
}