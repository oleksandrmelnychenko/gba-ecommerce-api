using System.Collections.Generic;
using GBA.Domain.Entities.Clients.Documents;
using GBA.Domain.Entities.Clients.PackingMarkings;
using GBA.Domain.Entities.Clients.PerfectClients;
using GBA.Domain.Entities.Delivery;
using GBA.Domain.Entities.PaymentOrders;
using GBA.Domain.Entities.Regions;
using GBA.Domain.Entities.SaleReturns;
using GBA.Domain.Entities.Sales;
using GBA.Domain.Entities.Supplies;
using GBA.Domain.Entities.Supplies.Returns;
using GBA.Domain.Entities.Supplies.Ukraine;

namespace GBA.Domain.Entities.Clients;

public sealed class Client : EntityBase {
    /// <summary>
    /// ctor().
    /// </summary>
    public Client() {
        PreOrders = new HashSet<PreOrder>();

        PerfectClientValues = new HashSet<ClientPerfectClient>();

        PerfectClients = new HashSet<PerfectClient>();

        ClientManagers = new HashSet<ClientUserProfile>();

        ClientAgreements = new HashSet<ClientAgreement>();

        ClientGroups = new HashSet<ClientGroup>();

        RootClients = new HashSet<ClientSubClient>();

        SubClients = new HashSet<ClientSubClient>();

        MainClients = new HashSet<ClientWorkplace>();

        ClientWorkplaces = new HashSet<ClientWorkplace>();

        Workplaces = new HashSet<Workplace>();

        ClientInDebts = new HashSet<ClientInDebt>();

        ServicePayers = new HashSet<ServicePayer>();

        DeliveryRecipients = new HashSet<DeliveryRecipient>();

        SupplyOrders = new HashSet<SupplyOrder>();

        ClientContractDocuments = new HashSet<ClientContractDocument>();

        ClientRegistrationTasks = new HashSet<ClientRegistrationTask>();

        SaleFutureReservations = new HashSet<SaleFutureReservation>();

        OutcomePaymentOrders = new HashSet<OutcomePaymentOrder>();

        IncomePaymentOrders = new HashSet<IncomePaymentOrder>();

        SaleReturns = new HashSet<SaleReturn>();

        SupplyOrderUkraines = new HashSet<SupplyOrderUkraine>();

        SupplyReturns = new HashSet<SupplyReturn>();

        SupplyOrderUkraineItems = new HashSet<SupplyOrderUkraineItem>();

        SupplyOrderUkraineCartItems = new HashSet<SupplyOrderUkraineCartItem>();

        SadItems = new HashSet<SadItem>();

        Sads = new HashSet<Sad>();

        TaxFreePackLists = new HashSet<TaxFreePackList>();

        GroupSubClients = new HashSet<Client>();
    }

    /// <summary>
    /// Taxpayer Identification Number.
    /// </summary>
    public string TIN { get; set; }

    /// <summary>
    /// Unified State Register of Enterprises and Organizations of Ukraine.
    /// </summary>
    public string USREOU { get; set; }

    /// <summary>
    /// State register of individuals.
    /// </summary>
    public string SROI { get; set; }

    public string Name { get; set; }

    public string FullName { get; set; }

    public string FirstName { get; set; }

    public string MiddleName { get; set; }

    public string LastName { get; set; }

    public string SupplierName { get; set; }

    public string SupplierContactName { get; set; }

    public string SupplierCode { get; set; }

    public string Manufacturer { get; set; }

    public string Brand { get; set; }

    public string Comment { get; set; }

    public string MobileNumber { get; set; }

    public string ClientNumber { get; set; }

    public string SMSNumber { get; set; }

    public string FaxNumber { get; set; }

    public string AccountantNumber { get; set; }

    public string DirectorNumber { get; set; }

    public string ICQ { get; set; }

    public string EmailAddress { get; set; }

    public string DeliveryAddress { get; set; }

    public string LegalAddress { get; set; }

    public string ActualAddress { get; set; }

    public string IncotermsElse { get; set; }

    public string Street { get; set; }

    public string ZipCode { get; set; }

    public string HouseNumber { get; set; }

    public string RefId { get; set; }

    public int ClearCartAfterDays { get; set; }

    public long? SourceAmgCode { get; set; }

    public long? SourceFenixCode { get; set; }

    public byte[] SourceAmgId { get; set; }

    public byte[] SourceFenixId { get; set; }

    public long? RegionId { get; set; }

    public long? RegionCodeId { get; set; }

    public long? CountryId { get; set; }

    public long? ClientBankDetailsId { get; set; }

    public long? TermsOfDeliveryId { get; set; }

    public long? PackingMarkingId { get; set; }

    public long? PackingMarkingPaymentId { get; set; }

    public long? MainManagerId { get; set; }

    /// <summary>
    /// Abbreviation of first and last name, two characters.
    /// </summary>
    public string Abbreviation { get; set; }

    /// <summary>
    /// Determine either person individual or entity.
    /// </summary>
    public bool IsIndividual { get; set; }

    public bool IsActive { get; set; }

    public bool IsSubClient { get; set; }

    public bool IsTradePoint { get; set; }

    public bool IsPayForDelivery { get; set; }

    public bool IsBlocked { get; set; }

    public bool IsIncotermsElse { get; set; }

    public bool IsTemporaryClient { get; set; }

    public bool IsFromECommerce { get; set; }

    public bool IsForRetail { get; set; }

    public bool IsWorkplace { get; set; }

    public int NameDistance { get; set; }

    public string Manager { get; set; }

    public string OriginalRegionCode { get; set; }

    public bool IsNotResident { get; set; }

    public long? MainClientId { get; set; }

    public int OrderExpireDays { get; set; }

    public Region Region { get; set; }

    public RegionCode RegionCode { get; set; }

    public ClientInRole ClientInRole { get; set; }

    public Country Country { get; set; }

    public ClientBankDetails ClientBankDetails { get; set; }

    public TermsOfDelivery TermsOfDelivery { get; set; }

    public PackingMarking PackingMarking { get; set; }

    public PackingMarkingPayment PackingMarkingPayment { get; set; }

    public User MainManager { get; set; }

    public ICollection<ClientGroup> ClientGroups { get; set; }

    public ICollection<PreOrder> PreOrders { get; set; }

    public ICollection<ClientPerfectClient> PerfectClientValues { get; set; }

    public ICollection<PerfectClient> PerfectClients { get; set; }

    public ICollection<ClientUserProfile> ClientManagers { get; set; }

    public ICollection<ClientAgreement> ClientAgreements { get; set; }

    public ICollection<ClientSubClient> RootClients { get; set; }

    public ICollection<ClientSubClient> SubClients { get; set; }

    public ICollection<ClientWorkplace> MainClients { get; set; }

    public ICollection<ClientWorkplace> ClientWorkplaces { get; set; }

    public ICollection<ClientInDebt> ClientInDebts { get; set; }

    public ICollection<ServicePayer> ServicePayers { get; set; }

    public ICollection<DeliveryRecipient> DeliveryRecipients { get; set; }

    public ICollection<SupplyOrder> SupplyOrders { get; set; }

    public ICollection<ClientContractDocument> ClientContractDocuments { get; set; }

    public ICollection<ClientRegistrationTask> ClientRegistrationTasks { get; set; }

    public ICollection<SaleFutureReservation> SaleFutureReservations { get; set; }

    public ICollection<IncomePaymentOrder> IncomePaymentOrders { get; set; }

    public ICollection<OutcomePaymentOrder> OutcomePaymentOrders { get; set; }

    public ICollection<SaleReturn> SaleReturns { get; set; }

    public ICollection<SupplyOrderUkraine> SupplyOrderUkraines { get; set; }

    public ICollection<SupplyReturn> SupplyReturns { get; set; }

    public ICollection<SupplyOrderUkraineItem> SupplyOrderUkraineItems { get; set; }

    public ICollection<SupplyOrderUkraineCartItem> SupplyOrderUkraineCartItems { get; set; }

    public ICollection<SadItem> SadItems { get; set; }

    public ICollection<Sad> Sads { get; set; }

    public ICollection<TaxFreePackList> TaxFreePackLists { get; set; }

    public ICollection<Workplace> Workplaces { get; set; }

    public ICollection<Client> GroupSubClients { get; set; }

    public Client RootClient { get; set; }

    public Client MainClient { get; set; }

    public decimal AccountBalance { get; set; }

    public Workplace CurrentWorkplace { get; set; }

    public string ClientGroupName { get; set; }

    public bool IsSupplier { get; set; }

    public decimal TotalCurrentAmount { get; set; }
}