using System.Collections.Generic;
using GBA.Domain.Entities.Agreements;
using GBA.Domain.Entities.PaymentOrders;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.ReSales;
using GBA.Domain.Entities.SaleReturns;
using GBA.Domain.Entities.Sales;
using GBA.Domain.Entities.Supplies;
using GBA.Domain.Entities.Supplies.Returns;
using GBA.Domain.Entities.Supplies.Ukraine;

namespace GBA.Domain.Entities.Clients;

public sealed class ClientAgreement : EntityBase {
    public ClientAgreement() {
        ProductGroupDiscounts = new HashSet<ProductGroupDiscount>();

        Sales = new HashSet<Sale>();

        Orders = new HashSet<Order>();

        IncomePaymentOrders = new HashSet<IncomePaymentOrder>();

        OutcomePaymentOrders = new HashSet<OutcomePaymentOrder>();

        ClientShoppingCarts = new HashSet<ClientShoppingCart>();

        ClientBalanceMovements = new HashSet<ClientBalanceMovement>();

        SupplyOrderUkraines = new HashSet<SupplyOrderUkraine>();

        SupplyOrders = new HashSet<SupplyOrder>();

        SupplyReturns = new HashSet<SupplyReturn>();

        TaxFreePackLists = new HashSet<TaxFreePackList>();

        Sads = new HashSet<Sad>();

        AdvancePayments = new HashSet<AdvancePayment>();

        ReSales = new HashSet<ReSale>();

        SaleReturns = new HashSet<SaleReturn>();

        WorkplaceClientAgreements = new HashSet<WorkplaceClientAgreement>();
    }

    public long? OriginalClientAmgCode { get; set; }

    public long? OriginalClientFenixCode { get; set; }

    public long ClientId { get; set; }

    public long AgreementId { get; set; }

    public int ProductReservationTerm { get; set; }

    public decimal CurrentAmount { get; set; }

    public Client Client { get; set; }

    public Agreement Agreement { get; set; }

    public ICollection<ProductGroupDiscount> ProductGroupDiscounts { get; set; }

    public ICollection<Sale> Sales { get; set; }

    public ICollection<Order> Orders { get; set; }

    public ICollection<IncomePaymentOrder> IncomePaymentOrders { get; set; }

    public ICollection<OutcomePaymentOrder> OutcomePaymentOrders { get; set; }

    public ICollection<ClientShoppingCart> ClientShoppingCarts { get; set; }

    public ICollection<ClientBalanceMovement> ClientBalanceMovements { get; set; }

    public ICollection<SupplyOrderUkraine> SupplyOrderUkraines { get; set; }

    public ICollection<SupplyOrder> SupplyOrders { get; set; }

    public ICollection<SupplyReturn> SupplyReturns { get; set; }

    public ICollection<TaxFreePackList> TaxFreePackLists { get; set; }

    public ICollection<Sad> Sads { get; set; }

    public ICollection<AdvancePayment> AdvancePayments { get; set; }

    public ICollection<ReSale> ReSales { get; set; }

    public ICollection<SaleReturn> SaleReturns { get; set; }

    public ICollection<WorkplaceClientAgreement> WorkplaceClientAgreements { get; set; }

    public decimal AccountBalance { get; set; }

    public bool FromAmg { get; set; }

    public string OriginalClientName { get; set; }
}