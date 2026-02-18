using System.Collections.Generic;
using GBA.Domain.Entities.Agreements;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Sales;

namespace GBA.Domain.Entities;

public sealed class Workplace : EntityBase {
    public Workplace() {
        WorkplaceClientAgreements = new HashSet<WorkplaceClientAgreement>();

        ClientShoppingCarts = new HashSet<ClientShoppingCart>();

        Sales = new HashSet<Sale>();
    }

    public string FirstName { get; set; }

    public string MiddleName { get; set; }

    public string LastName { get; set; }

    public string Abbreviation { get; set; }

    public string Region { get; set; }

    public string Email { get; set; }

    public string PhoneNumber { get; set; }

    public bool IsBlocked { get; set; }

    public long MainClientId { get; set; }

    public long? ClientGroupId { get; set; }

    public Client MainClient { get; set; }

    public ClientGroup ClientGroup { get; set; }

    public ICollection<WorkplaceClientAgreement> WorkplaceClientAgreements { get; set; }

    public ICollection<ClientShoppingCart> ClientShoppingCarts { get; set; }

    public ICollection<Sale> Sales { get; set; }

    public string Password { get; set; }
}