using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using Dapper;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.Regions;
using GBA.Domain.Entities.ReSales;
using GBA.Domain.Entities.Sales;
using GBA.Domain.Entities.Sales.LifeCycleStatuses;
using GBA.Domain.Repositories.Products.Contracts;

namespace GBA.Domain.Repositories.Products;

public sealed class ProductReservationRepository : IProductReservationRepository {
    private readonly IDbConnection _connection;

    public ProductReservationRepository(IDbConnection connection) {
        _connection = connection;
    }

    public void Add(ProductReservation productReservation) {
        _connection.Execute(
            "INSERT INTO ProductReservation (OrderItemID, ProductAvailabilityID, Qty, ConsignmentItemId, IsReSaleReservation, Updated) " +
            "VALUES(@OrderItemID, @ProductAvailabilityID, @Qty, @ConsignmentItemId, @IsReSaleReservation, getutcdate())",
            productReservation
        );
    }

    public long AddWithId(ProductReservation productReservation) {
        return _connection.Query<long>(
            "INSERT INTO ProductReservation " +
            "(OrderItemID, ProductAvailabilityID, Qty, ConsignmentItemId, IsReSaleReservation, Updated) " +
            "VALUES " +
            "(@OrderItemID, @ProductAvailabilityID, @Qty, @ConsignmentItemId, @IsReSaleReservation, GETUTCDATE()) " +
            "SELECT SCOPE_IDENTITY()",
            productReservation
        ).Single();
    }

    public void Update(ProductReservation productReservation) {
        _connection.Execute(
            "UPDATE ProductReservation SET " +
            "OrderItemID = @OrderItemID, ProductAvailabilityID = @ProductAvailabilityID, Qty = @Qty, Updated = getutcdate() " +
            "WHERE NetUID = @NetUid",
            productReservation
        );
    }

    public void Update(List<ProductReservation> productReservations) {
        _connection.Execute(
            "UPDATE ProductReservation SET " +
            "OrderItemID = @OrderItemID, ProductAvailabilityID = @ProductAvailabilityID, Qty = @Qty, Updated = getutcdate() " +
            "WHERE NetUID = @NetUid",
            productReservations
        );
    }

    public void Delete(Guid netId) {
        _connection.Execute(
            "UPDATE ProductReservation SET " +
            "Deleted = 1 " +
            "WHERE NetUID = @NetId",
            new { NetId = netId.ToString() }
        );
    }

    public double GetAvailableSumByOrderItemIdWithAvailabilityAndReSaleAvailabilities(long orderItemId) {
        return _connection.Query<double>(
            ";WITH [AvailableQty_CTE] " +
            "AS ( " +
            "SELECT " +
            "[ProductReservation].Qty " +
            "FROM [ProductReservation] " +
            "LEFT JOIN [ProductAvailability] " +
            "ON [ProductAvailability].ID = [ProductReservation].ProductAvailabilityID " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [ProductAvailability].StorageID " +
            // "LEFT JOIN [ReSaleAvailability]  " +
            // "ON [ReSaleAvailability].ProductReservationID = [ProductReservation].ID " +
            // "AND [ReSaleAvailability].Deleted = 0 " +
            "WHERE [ProductReservation].OrderItemID = @OrderItemId " +
            "AND [ProductReservation].Deleted = 0 " +
            ") " +
            "SELECT ISNULL(SUM([Available].Qty), 0) [Qty] " +
            "FROM [AvailableQty_CTE] AS [Available] ",
            new { OrderItemId = orderItemId }
        ).Single();
    }

    public List<ProductReservation> GetAllCurrentReservationsByProductNetId(Guid productNetId) {
        List<ProductReservation> productReservations = new();

        string sqlExpression =
            "SELECT [ProductReservation].* " +
            ",[OrderItem].* " +
            ",[Product].* " +
            ",[Order].* " +
            ",[User].* " +
            ",[ClientAgreement].* " +
            ",[Client].* " +
            ",[Sale].* " +
            ",[BaseLifeCycleStatus].* " +
            ",[SaleNumber].* " +
            ",[RegionCode].* " +
            "FROM [ProductReservation] " +
            "LEFT JOIN [OrderItem] " +
            "ON [ProductReservation].OrderItemID = [OrderItem].ID " +
            "LEFT JOIN [Product] " +
            "ON [OrderItem].ProductID = [Product].ID " +
            "LEFT JOIN [Order] " +
            "ON [OrderItem].OrderID = [Order].ID " +
            "LEFT JOIN [User] " +
            "ON [OrderItem].UserID = [User].ID " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [Order].ClientAgreementID = ClientAgreement.ID " +
            "LEFT JOIN [Client] " +
            "ON [ClientAgreement].ClientID = [Client].ID " +
            "LEFT JOIN [Sale] " +
            "ON [Order].ID = [Sale].OrderID " +
            "LEFT JOIN [BaseLifeCycleStatus] " +
            "ON [Sale].BaseLifeCycleStatusID = [BaseLifeCycleStatus].ID " +
            "LEFT JOIN [SaleNumber] " +
            "ON [Sale].SaleNumberID = [SaleNumber].ID " +
            "LEFT JOIN [ClientAgreement] AS [SaleClientAgreement] " +
            "ON [SaleClientAgreement].ID = [Sale].ClientAgreementID " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].ID = [SaleClientAgreement].AgreementID " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].ID = [Agreement].OrganizationID " +
            "LEFT JOIN [RegionCode] " +
            "ON [Client].RegionCodeID = [RegionCode].ID " +
            "WHERE [BaseLifeCycleStatus].SaleLifeCycleType = 0 " +
            "AND [OrderItem].Qty > 0 " +
            "AND [Product].NetUID = @ProductNetId " +
            "AND [ProductReservation].Deleted = 0 " +
            "AND [Sale].Deleted = 0 " +
            "AND [Sale].IsMerged = 0 " +
            "AND [Organization].Culture = @Culture";

        Type[] types = {
            typeof(ProductReservation),
            typeof(OrderItem),
            typeof(Product),
            typeof(Order),
            typeof(User),
            typeof(ClientAgreement),
            typeof(Client),
            typeof(Sale),
            typeof(BaseLifeCycleStatus),
            typeof(SaleNumber),
            typeof(RegionCode)
        };

        Func<object[], ProductReservation> mapper = objects => {
            ProductReservation productReservation = (ProductReservation)objects[0];
            OrderItem orderItem = (OrderItem)objects[1];
            Product product = (Product)objects[2];
            Order order = (Order)objects[3];
            User user = (User)objects[4];
            ClientAgreement clientAgreement = (ClientAgreement)objects[5];
            Client client = (Client)objects[6];
            Sale sale = (Sale)objects[7];
            BaseLifeCycleStatus baseLifeCycleStatus = (BaseLifeCycleStatus)objects[8];
            SaleNumber saleNumber = (SaleNumber)objects[9];
            RegionCode regionCode = (RegionCode)objects[10];

            clientAgreement.Client = client;

            sale.SaleNumber = saleNumber;
            sale.BaseLifeCycleStatus = baseLifeCycleStatus;

            order.Sales.Add(sale);
            order.ClientAgreement = clientAgreement;

            orderItem.User = user;
            orderItem.Product = product;
            orderItem.Order = order;

            productReservation.OrderItem = orderItem;

            productReservation.RegionCode = regionCode?.Value;

            if (!productReservations.Any(r => r.Id.Equals(productReservation.Id))) productReservations.Add(productReservation);

            return productReservation;
        };

        var props = new { ProductNetId = productNetId, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName };

        _connection.Query(
            sqlExpression,
            types,
            mapper,
            props
        );

        return productReservations;
    }

    public List<ProductReservation> GetAllCurrentReservationsByProductNetId(Guid productNetId, bool withVat) {
        List<ProductReservation> productReservations = new();

        string sqlExpression =
            "SELECT [ProductReservation].* " +
            ",[OrderItem].* " +
            ",[Product].* " +
            ",[Order].* " +
            ",[User].* " +
            ",[ClientAgreement].* " +
            ",[Client].* " +
            ",[Sale].* " +
            ",[BaseLifeCycleStatus].* " +
            ",[SaleNumber].* " +
            ",[RegionCode].* " +
            "FROM [ProductReservation] " +
            "LEFT JOIN [OrderItem] " +
            "ON [ProductReservation].OrderItemID = [OrderItem].ID " +
            "LEFT JOIN [Product] " +
            "ON [OrderItem].ProductID = [Product].ID " +
            "LEFT JOIN [Order] " +
            "ON [OrderItem].OrderID = [Order].ID " +
            "LEFT JOIN [User] " +
            "ON [OrderItem].UserID = [User].ID " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [Order].ClientAgreementID = ClientAgreement.ID " +
            "LEFT JOIN [Client] " +
            "ON [ClientAgreement].ClientID = [Client].ID " +
            "LEFT JOIN [Sale] " +
            "ON [Order].ID = [Sale].OrderID " +
            "LEFT JOIN [BaseLifeCycleStatus] " +
            "ON [Sale].BaseLifeCycleStatusID = [BaseLifeCycleStatus].ID " +
            "LEFT JOIN [SaleNumber] " +
            "ON [Sale].SaleNumberID = [SaleNumber].ID " +
            "LEFT JOIN [ClientAgreement] AS [SaleClientAgreement] " +
            "ON [SaleClientAgreement].ID = [Sale].ClientAgreementID " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].ID = [SaleClientAgreement].AgreementID " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].ID = [Agreement].OrganizationID " +
            "LEFT JOIN [RegionCode] " +
            "ON [Client].RegionCodeID = [RegionCode].ID " +
            "WHERE [BaseLifeCycleStatus].SaleLifeCycleType = 0 " +
            "AND [OrderItem].Qty > 0 " +
            "AND [Product].NetUID = @ProductNetId " +
            "AND [ProductReservation].Deleted = 0 " +
            "AND [Sale].Deleted = 0 " +
            "AND [Sale].IsMerged = 0 " +
            "AND [Sale].IsVatSale = @WithVat " +
            "AND [Organization].Culture = @Culture";

        Type[] types = {
            typeof(ProductReservation),
            typeof(OrderItem),
            typeof(Product),
            typeof(Order),
            typeof(User),
            typeof(ClientAgreement),
            typeof(Client),
            typeof(Sale),
            typeof(BaseLifeCycleStatus),
            typeof(SaleNumber),
            typeof(RegionCode)
        };

        Func<object[], ProductReservation> mapper = objects => {
            ProductReservation productReservation = (ProductReservation)objects[0];
            OrderItem orderItem = (OrderItem)objects[1];
            Product product = (Product)objects[2];
            Order order = (Order)objects[3];
            User user = (User)objects[4];
            ClientAgreement clientAgreement = (ClientAgreement)objects[5];
            Client client = (Client)objects[6];
            Sale sale = (Sale)objects[7];
            BaseLifeCycleStatus baseLifeCycleStatus = (BaseLifeCycleStatus)objects[8];
            SaleNumber saleNumber = (SaleNumber)objects[9];
            RegionCode regionCode = (RegionCode)objects[10];

            clientAgreement.Client = client;

            sale.SaleNumber = saleNumber;
            sale.BaseLifeCycleStatus = baseLifeCycleStatus;

            order.Sales.Add(sale);
            order.ClientAgreement = clientAgreement;

            orderItem.User = user;
            orderItem.Product = product;
            orderItem.Order = order;

            productReservation.OrderItem = orderItem;

            productReservation.RegionCode = regionCode?.Value;

            if (!productReservations.Any(r => r.Id.Equals(productReservation.Id))) productReservations.Add(productReservation);

            return productReservation;
        };

        var props = new { ProductNetId = productNetId, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName, WithVat = withVat };

        _connection.Query(
            sqlExpression,
            types,
            mapper,
            props
        );

        return productReservations;
    }

    public List<ProductReservation> GetAllCurrentReservationsByProductNetId(Guid productNetId, long organizationId, bool withVat) {
        List<ProductReservation> productReservations = new();

        string sqlExpression =
            "SELECT [ProductReservation].* " +
            ",[OrderItem].* " +
            ",[Product].* " +
            ",[Order].* " +
            ",[User].* " +
            ",[ClientAgreement].* " +
            ",[Client].* " +
            ",[Sale].* " +
            ",[BaseLifeCycleStatus].* " +
            ",[SaleNumber].* " +
            ",[RegionCode].* " +
            "FROM [ProductReservation] " +
            "LEFT JOIN [OrderItem] " +
            "ON [ProductReservation].OrderItemID = [OrderItem].ID " +
            "LEFT JOIN [Product] " +
            "ON [OrderItem].ProductID = [Product].ID " +
            "LEFT JOIN [Order] " +
            "ON [OrderItem].OrderID = [Order].ID " +
            "LEFT JOIN [User] " +
            "ON [OrderItem].UserID = [User].ID " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [Order].ClientAgreementID = ClientAgreement.ID " +
            "LEFT JOIN [Client] " +
            "ON [ClientAgreement].ClientID = [Client].ID " +
            "LEFT JOIN [Sale] " +
            "ON [Order].ID = [Sale].OrderID " +
            "LEFT JOIN [BaseLifeCycleStatus] " +
            "ON [Sale].BaseLifeCycleStatusID = [BaseLifeCycleStatus].ID " +
            "LEFT JOIN [SaleNumber] " +
            "ON [Sale].SaleNumberID = [SaleNumber].ID " +
            "LEFT JOIN [ClientAgreement] AS [SaleClientAgreement] " +
            "ON [SaleClientAgreement].ID = [Sale].ClientAgreementID " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].ID = [SaleClientAgreement].AgreementID " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].ID = [Agreement].OrganizationID " +
            "LEFT JOIN [RegionCode] " +
            "ON [Client].RegionCodeID = [RegionCode].ID " +
            "WHERE [BaseLifeCycleStatus].SaleLifeCycleType = 0 " +
            "AND [OrderItem].Qty > 0 " +
            "AND [Product].NetUID = @ProductNetId " +
            "AND [ProductReservation].Deleted = 0 " +
            "AND [Sale].Deleted = 0 " +
            "AND [Sale].IsMerged = 0 " +
            "AND [Sale].IsVatSale = @WithVat " +
            "AND [Organization].Culture = @Culture " +
            "AND [Organization].ID = @OrganizationId";

        Type[] types = {
            typeof(ProductReservation),
            typeof(OrderItem),
            typeof(Product),
            typeof(Order),
            typeof(User),
            typeof(ClientAgreement),
            typeof(Client),
            typeof(Sale),
            typeof(BaseLifeCycleStatus),
            typeof(SaleNumber),
            typeof(RegionCode)
        };

        Func<object[], ProductReservation> mapper = objects => {
            ProductReservation productReservation = (ProductReservation)objects[0];
            OrderItem orderItem = (OrderItem)objects[1];
            Product product = (Product)objects[2];
            Order order = (Order)objects[3];
            User user = (User)objects[4];
            ClientAgreement clientAgreement = (ClientAgreement)objects[5];
            Client client = (Client)objects[6];
            Sale sale = (Sale)objects[7];
            BaseLifeCycleStatus baseLifeCycleStatus = (BaseLifeCycleStatus)objects[8];
            SaleNumber saleNumber = (SaleNumber)objects[9];
            RegionCode regionCode = (RegionCode)objects[10];

            clientAgreement.Client = client;

            sale.SaleNumber = saleNumber;
            sale.BaseLifeCycleStatus = baseLifeCycleStatus;

            order.Sales.Add(sale);
            order.ClientAgreement = clientAgreement;

            orderItem.User = user;
            orderItem.Product = product;
            orderItem.Order = order;

            productReservation.OrderItem = orderItem;

            productReservation.RegionCode = regionCode?.Value;

            if (!productReservations.Any(r => r.Id.Equals(productReservation.Id))) productReservations.Add(productReservation);

            return productReservation;
        };

        _connection.Query(
            sqlExpression,
            types,
            mapper,
            new {
                ProductNetId = productNetId,
                OrganizationId = organizationId,
                Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName,
                WithVat = withVat
            }
        );

        return productReservations;
    }

    public List<ProductReservation> GetAllCurrentReservationsByProductNetIdAndCulture(Guid productNetId, string culture) {
        List<ProductReservation> productReservations = new();

        string sqlExpression =
            "SELECT [ProductReservation].* " +
            ",[OrderItem].* " +
            ",[Product].* " +
            ",[Order].* " +
            ",[User].* " +
            ",[ClientAgreement].* " +
            ",[Client].* " +
            ",[Sale].* " +
            ",[BaseLifeCycleStatus].* " +
            ",[SaleNumber].* " +
            ",[RegionCode].* " +
            ",[ClientShoppingCart].* " +
            ",[CartClientAgreement].* " +
            ",[CartClient].* " +
            "FROM [ProductReservation] " +
            "LEFT JOIN [OrderItem] " +
            "ON [ProductReservation].OrderItemID = [OrderItem].ID " +
            "LEFT JOIN [Product] " +
            "ON [OrderItem].ProductID = [Product].ID " +
            "LEFT JOIN [Order] " +
            "ON [OrderItem].OrderID = [Order].ID " +
            "LEFT JOIN [User] " +
            "ON [OrderItem].UserID = [User].ID " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [Order].ClientAgreementID = ClientAgreement.ID " +
            "LEFT JOIN [Client] " +
            "ON [ClientAgreement].ClientID = [Client].ID " +
            "LEFT JOIN [Sale] " +
            "ON [Order].ID = [Sale].OrderID " +
            "LEFT JOIN [BaseLifeCycleStatus] " +
            "ON [Sale].BaseLifeCycleStatusID = [BaseLifeCycleStatus].ID " +
            "LEFT JOIN [SaleNumber] " +
            "ON [Sale].SaleNumberID = [SaleNumber].ID " +
            "LEFT JOIN [ClientAgreement] AS [SaleClientAgreement] " +
            "ON [SaleClientAgreement].ID = [Sale].ClientAgreementID " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].ID = [SaleClientAgreement].AgreementID " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].ID = [Agreement].OrganizationID " +
            "LEFT JOIN [RegionCode] " +
            "ON [Client].RegionCodeID = [RegionCode].ID " +
            "LEFT JOIN [ClientShoppingCart] " +
            "ON [ClientShoppingCart].ID = [OrderItem].ClientShoppingCartID " +
            "LEFT JOIN [ClientAgreement] AS [CartClientAgreement] " +
            "ON [CartClientAgreement].ID = [ClientShoppingCart].ClientAgreementID " +
            "LEFT JOIN [Client] AS [CartClient] " +
            "ON [CartClient].ID = [CartClientAgreement].ClientID " +
            "LEFT JOIN [Agreement] AS [CartAgreement] " +
            "ON [CartAgreement].ID = [CartClientAgreement].AgreementID " +
            "LEFT JOIN [Organization] AS [CartOrganization] " +
            "ON [CartOrganization].ID = [CartAgreement].OrganizationID " +
            "WHERE [OrderItem].Qty > 0 " +
            "AND [Product].NetUID = @ProductNetId " +
            "AND [ProductReservation].Deleted = 0" +
            "AND (" +
            "(" +
            "[Sale].Deleted = 0 " +
            "AND " +
            "[Sale].IsMerged = 0 " +
            "AND " +
            "[Organization].Culture = @Culture " +
            "AND " +
            "[BaseLifeCycleStatus].SaleLifeCycleType = 0" +
            ")" +
            "OR " +
            "(" +
            "[ClientShoppingCart].Deleted = 0 " +
            "AND " +
            "[CartOrganization].Culture = @Culture" +
            ")" +
            ")";

        Type[] types = {
            typeof(ProductReservation),
            typeof(OrderItem),
            typeof(Product),
            typeof(Order),
            typeof(User),
            typeof(ClientAgreement),
            typeof(Client),
            typeof(Sale),
            typeof(BaseLifeCycleStatus),
            typeof(SaleNumber),
            typeof(RegionCode),
            typeof(ClientShoppingCart),
            typeof(ClientAgreement),
            typeof(Client)
        };

        Func<object[], ProductReservation> mapper = objects => {
            ProductReservation productReservation = (ProductReservation)objects[0];
            OrderItem orderItem = (OrderItem)objects[1];
            Product product = (Product)objects[2];
            Order order = (Order)objects[3];
            User user = (User)objects[4];
            ClientAgreement clientAgreement = (ClientAgreement)objects[5];
            Client client = (Client)objects[6];
            Sale sale = (Sale)objects[7];
            BaseLifeCycleStatus baseLifeCycleStatus = (BaseLifeCycleStatus)objects[8];
            SaleNumber saleNumber = (SaleNumber)objects[9];
            RegionCode regionCode = (RegionCode)objects[10];
            ClientShoppingCart clientShoppingCart = (ClientShoppingCart)objects[11];
            ClientAgreement cartClientAgreement = (ClientAgreement)objects[12];
            Client cartClient = (Client)objects[13];

            if (order != null) {
                clientAgreement.Client = client;

                sale.SaleNumber = saleNumber;
                sale.BaseLifeCycleStatus = baseLifeCycleStatus;

                order.Sales.Add(sale);
                order.ClientAgreement = clientAgreement;
            }

            if (clientShoppingCart != null) {
                cartClientAgreement.Client = cartClient;

                clientShoppingCart.ClientAgreement = cartClientAgreement;
            }

            orderItem.User = user;
            orderItem.Order = order;
            orderItem.Product = product;
            orderItem.ClientShoppingCart = clientShoppingCart;

            productReservation.OrderItem = orderItem;

            productReservation.RegionCode = regionCode?.Value;

            if (!productReservations.Any(r => r.Id.Equals(productReservation.Id))) productReservations.Add(productReservation);

            return productReservation;
        };

        var props = new { ProductNetId = productNetId, Culture = culture };

        _connection.Query(
            sqlExpression,
            types,
            mapper,
            props
        );

        return productReservations;
    }

    public IEnumerable<ProductReservation> GetByOrderItemId(long orderItemId) {
        return _connection.Query<ProductReservation>(
            "SELECT * " +
            "FROM ProductReservation " +
            "WHERE OrderItemID = @OrderItemId " +
            "AND ProductReservation.Deleted = 0",
            new { OrderItemId = orderItemId }
        );
    }

    public IEnumerable<ProductReservation> GetAllByOrderItemIdWithAvailability(long orderItemId) {
        return _connection.Query<ProductReservation, ProductAvailability, Storage, ProductReservation>(
            "SELECT * " +
            "FROM [ProductReservation] " +
            "LEFT JOIN [ProductAvailability] " +
            "ON [ProductAvailability].ID = [ProductReservation].ProductAvailabilityID " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [ProductAvailability].StorageID " +
            "WHERE [ProductReservation].OrderItemID = @OrderItemId " +
            "AND [ProductReservation].Deleted = 0 " +
            "ORDER BY [ProductReservation].ID DESC",
            (reservation, availability, storage) => {
                availability.Storage = storage;

                reservation.ProductAvailability = availability;

                return reservation;
            },
            new { OrderItemId = orderItemId }
        );
    }

    public IEnumerable<ProductReservation> GetAllByOrderItemIdWithAvailabilityAndReSaleAvailabilities(long orderItemId) {
        List<ProductReservation> reservations = new();

        _connection.Query<ProductReservation, ProductAvailability, Storage, ReSaleAvailability, ProductReservation>(
            "SELECT [ProductReservation].ID " +
            ", [ProductReservation].Created " +
            ", [ProductReservation].Deleted " +
            ", [ProductReservation].NetUID " +
            ", [ProductReservation].OrderItemID " +
            ", [ProductReservation].ProductAvailabilityID " +
            ", ( " +
            "[ProductReservation].Qty " +
            // "- " +
            // "ISNULL(( " +
            // "SELECT SUM([JoinItem].Qty) " +
            // "FROM [ReSaleAvailability] AS [JoinItem] " +
            // "WHERE [JoinItem].ProductReservationID = [ProductReservation].ID " +
            // "AND [JoinItem].[RemainingQty] > 0 " +
            // "), 0) " +
            ") [Qty] " +
            ", [ProductReservation].Updated " +
            ", [ProductReservation].ConsignmentItemID " +
            ", [ProductReservation].IsReSaleReservation " +
            ", [ProductAvailability].* " +
            ", [Storage].* " +
            ", [ReSaleAvailability].* " +
            "FROM [ProductReservation] " +
            "LEFT JOIN [ProductAvailability] " +
            "ON [ProductAvailability].ID = [ProductReservation].ProductAvailabilityID " +
            "LEFT JOIN [Storage] " +
            "ON [Storage].ID = [ProductAvailability].StorageID " +
            "LEFT JOIN [ReSaleAvailability] " +
            "ON [ReSaleAvailability].ProductReservationID = [ProductReservation].ID " +
            "AND [ReSaleAvailability].Deleted = 0 " +
            "WHERE [ProductReservation].OrderItemID = @OrderItemId " +
            "AND [ProductReservation].Deleted = 0 " +
            //"AND [ReSaleAvailability].ID IS NULL " +
            "ORDER BY [ProductReservation].ID DESC ",
            (reservation, availability, storage, reSaleAvailability) => {
                if (reservations.Any(r => r.Id.Equals(reservation.Id))) {
                    reservation = reservations.First(r => r.Id.Equals(reservation.Id));
                } else {
                    availability.Storage = storage;

                    reservation.ProductAvailability = availability;

                    reservations.Add(reservation);
                }

                if (reSaleAvailability == null) return reservation;

                reservation.ReSaleAvailabilities.Add(reSaleAvailability);

                return reservation;
            },
            new { OrderItemId = orderItemId }
        );

        return reservations;
    }

    public ProductReservation GetByOrderItemAndProductAvailabilityIds(long orderItemId, long productAvailabilityId) {
        return _connection.Query<ProductReservation>(
            "SELECT * FROM ProductReservation " +
            "WHERE OrderItemId = @OrderItemId " +
            "AND ProductAvailabilityId = @ProductAvailabilityId " +
            "AND ProductReservation.Deleted = 0 " +
            "AND ConsignmentItemID IS NULL",
            new { OrderItemId = orderItemId, ProductAvailabilityId = productAvailabilityId }
        ).FirstOrDefault();
    }

    public ProductReservation GetByOrderItemProductAvailabilityAndConsignmentItemIds(long orderItemId, long productAvailabilityId, long consignmentItemId) {
        return _connection.Query<ProductReservation>(
            "SELECT * FROM ProductReservation " +
            "WHERE OrderItemId = @OrderItemId " +
            "AND ProductAvailabilityId = @ProductAvailabilityId " +
            "AND ConsignmentItemID = @ConsignmentItemId " +
            "AND ProductReservation.Deleted = 0",
            new { OrderItemId = orderItemId, ProductAvailabilityId = productAvailabilityId, ConsignmentItemId = consignmentItemId }
        ).FirstOrDefault();
    }
}