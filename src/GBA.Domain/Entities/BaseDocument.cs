namespace GBA.Domain.Entities;

public abstract class BaseDocument : EntityBase {
    public string DocumentUrl { get; set; }

    public string FileName { get; set; }

    public string ContentType { get; set; }

    public string GeneratedName { get; set; }
}