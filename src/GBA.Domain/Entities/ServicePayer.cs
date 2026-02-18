using GBA.Domain.Entities.Clients;
using GBA.Domain.EntityHelpers;

namespace GBA.Domain.Entities;

public sealed class ServicePayer : EntityBase {
    public string FirstName { get; set; }

    public string LastName { get; set; }

    public string MiddleName { get; set; }

    public string MobilePhone { get; set; }

    public string Comment { get; set; }

    public string PaymentAddress { get; set; }

    public string PaymentCard { get; set; }

    public ServiceType ServiceType { get; set; }

    public long ClientId { get; set; }

    public Client Client { get; set; }
}