namespace GBA.Domain.Entities.Consumables;

public sealed class CompanyCarRoadListDriver : EntityBase {
    public long CompanyCarRoadListId { get; set; }

    public long UserId { get; set; }

    public CompanyCarRoadList CompanyCarRoadList { get; set; }

    public User User { get; set; }
}