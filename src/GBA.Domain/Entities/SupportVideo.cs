namespace GBA.Domain.Entities;

public sealed class SupportVideo : EntityBase {
    public string NameUk { get; set; }

    public string NamePl { get; set; }

    public string Url { get; set; }

    public string DocumentUrl { get; set; }
}