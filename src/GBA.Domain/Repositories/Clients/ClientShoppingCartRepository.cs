using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using Dapper;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Agreements;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.Regions;
using GBA.Domain.Entities.Sales;
using GBA.Domain.Repositories.Clients.Contracts;

namespace GBA.Domain.Repositories.Clients;

public sealed class ClientShoppingCartRepository : IClientShoppingCartRepository {
    private readonly IDbConnection _connection;

    public ClientShoppingCartRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(ClientShoppingCart clientShoppingCart) {
        return _connection.Query<long>(
                "INSERT INTO ClientShoppingCart (ValidUntil, ClientAgreementID, IsVatCart, WorkplaceID, Updated) " +
                "VALUES(@ValidUntil, @ClientAgreementId, @IsVatCart, @WorkplaceId, getutcdate()); " +
                "SELECT SCOPE_IDENTITY()",
                clientShoppingCart
            )
            .Single();
    }

    public long AddAsOffer(ClientShoppingCart clientShoppingCart) {
        return _connection.Query<long>(
                "INSERT INTO ClientShoppingCart (ValidUntil, ClientAgreementID, Deleted, Number, IsOffer, CreatedByID, IsVatCart, WorkplaceID, Updated) " +
                "VALUES(@ValidUntil, @ClientAgreementId, 0, @Number, 1, @CreatedById, @IsVatCart, @WorkplaceId, getutcdate()); " +
                "SELECT SCOPE_IDENTITY()",
                clientShoppingCart
            )
            .Single();
    }

    public ClientShoppingCart GetById(long id) {
        return _connection.Query<ClientShoppingCart>(
                "SELECT * FROM ClientShoppingCart WHERE ID = @Id",
                new { Id = id }
            )
            .SingleOrDefault();
    }

    public ClientShoppingCart GetByNetId(Guid netId) {
        ClientShoppingCart toReturn =
            _connection.Query<ClientShoppingCart, ClientAgreement, Agreement, Organization, ClientShoppingCart>(
                    "SELECT * " +
                    "FROM [ClientShoppingCart] " +
                    "LEFT JOIN [ClientAgreement] " +
                    "ON [ClientAgreement].ID = [ClientShoppingCart].ClientAgreementID " +
                    "LEFT JOIN [Agreement] " +
                    "ON [Agreement].[ID] = [ClientAgreement].[AgreementID] " +
                    "WHERE [ClientShoppingCart].NetUID = @NetId",
                    (cart, clientAgreement, agreement, organization) => {
                        if (clientAgreement != null) {
                            clientAgreement.Agreement = agreement;

                            agreement.Organization = organization;
                        }

                        cart.ClientAgreement = clientAgreement;

                        return cart;
                    },
                    new { NetId = netId }
                )
                .SingleOrDefault();

        if (toReturn == null) return null;

        {
            string sqlExpression =
                "SELECT * " +
                "FROM [ClientShoppingCart] " +
                "LEFT JOIN [OrderItem] " +
                "ON [OrderItem].ClientShoppingCartID = [ClientShoppingCart].ID " +
                "AND [OrderItem].Deleted = 0 " +
                "LEFT JOIN (" +
                "SELECT " +
                "[Product].ID " +
                ", [Product].Created " +
                ", [Product].Deleted ";

            if (CultureInfo.CurrentCulture.TwoLetterISOLanguageName.Equals("pl")) {
                sqlExpression += ", [Product].[NameUA] AS [Name] ";
                sqlExpression += ", [Product].[DescriptionUA] AS [Description] ";
            } else {
                sqlExpression += ", [Product].[NameUA] AS [Name] ";
                sqlExpression += ", [Product].[DescriptionUA] AS [Description] ";
            }

            sqlExpression +=
                ", [Product].HasAnalogue " +
                ", [Product].HasComponent " +
                ", [Product].HasImage " +
                ", [Product].[Image] " +
                ", [Product].IsForSale " +
                ", [Product].IsForWeb " +
                ", [Product].IsForZeroSale " +
                ", [Product].MainOriginalNumber " +
                ", [Product].MeasureUnitID " +
                ", [Product].NetUID " +
                ", [Product].OrderStandard " +
                ", [Product].PackingStandard " +
                ", [Product].Size " +
                ", [Product].[Top] " +
                ", [Product].UCGFEA " +
                ", [Product].Updated " +
                ", [Product].VendorCode " +
                ", [Product].Volume " +
                ", [Product].[Weight] " +
                ", ISNULL(( " +
                "SELECT SUM([ProductAvailability].Amount) " +
                "FROM [ProductAvailability] " +
                "LEFT JOIN [Storage] " +
                "ON [Storage].ID = [ProductAvailability].StorageID " +
                "WHERE [ProductAvailability].ProductID = [Product].ID " +
                "AND [ProductAvailability].Deleted = 0 " +
                "AND [Storage].ForDefective = 0 " +
                "AND [Storage].Locale = N'uk' " +
                "AND [Storage].ForVatProducts = 0 " +
                "), 0) AS [AvailableQtyUk] " +
                ", ISNULL(( " +
                "SELECT SUM([ProductAvailability].Amount) " +
                "FROM [ProductAvailability] " +
                "LEFT JOIN [Storage] " +
                "ON [Storage].ID = [ProductAvailability].StorageID " +
                "WHERE [ProductAvailability].ProductID = [Product].ID " +
                "AND [ProductAvailability].Deleted = 0 " +
                "AND [Storage].ForDefective = 0 " +
                "AND [Storage].Locale = N'uk' " +
                "AND [Storage].ForVatProducts = 1 " +
                "), 0) AS [AvailableQtyUkVAT] " +
                ", ISNULL(( " +
                "SELECT SUM([ProductAvailability].Amount) " +
                "FROM [ProductAvailability] " +
                "LEFT JOIN [Storage] " +
                "ON [Storage].ID = [ProductAvailability].StorageID " +
                "WHERE [ProductAvailability].ProductID = [Product].ID " +
                "AND [ProductAvailability].Deleted = 0 " +
                "AND [Storage].ForDefective = 0 " +
                "AND [Storage].Locale = N'pl' " +
                "AND [Storage].ForVatProducts = 0 " +
                "), 0) AS [AvailableQtyPl] " +
                ", ISNULL(( " +
                "SELECT SUM([ProductAvailability].Amount) " +
                "FROM [ProductAvailability] " +
                "LEFT JOIN [Storage] " +
                "ON [Storage].ID = [ProductAvailability].StorageID " +
                "WHERE [ProductAvailability].ProductID = [Product].ID " +
                "AND [ProductAvailability].Deleted = 0 " +
                "AND [Storage].ForDefective = 0 " +
                "AND [Storage].Locale = N'pl' " +
                "AND [Storage].ForVatProducts = 1 " +
                "), 0) AS [AvailableQtyPlVAT] " +
                ",dbo.GetCalculatedProductPriceWithSharesAndVat([Product].NetUID, @ClientAgreementNetId, @Culture, @WithVat, [OrderItem].[ID]) AS [CurrentPrice] " +
                ",dbo.GetCalculatedProductLocalPriceWithSharesAndVat([Product].NetUID, @ClientAgreementNetId, @Culture, @WithVat, [OrderItem].[ID]) AS [CurrentLocalPrice] " +
                "FROM [Product]" +
                ") AS [Product]" +
                "ON [OrderItem].ProductID = [Product].ID " +
                "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
                "ON [MeasureUnit].ID = [Product].MeasureUnitID " +
                "AND [MeasureUnit].CultureCode = @Culture " +
                "WHERE [ClientShoppingCart].NetUID = @NetId";

            _connection.Query<ClientShoppingCart, OrderItem, Product, MeasureUnit, ClientShoppingCart>(
                sqlExpression,
                (cart, item, product, measureUnit) => {
                    if (toReturn != null) {
                        if (item == null || toReturn.OrderItems.Any(i => i.Id.Equals(item.Id))) return cart;
                        product.MeasureUnit = measureUnit;

                        item.Product = product;

                        toReturn.OrderItems.Add(item);
                    } else {
                        if (item != null) {
                            product.MeasureUnit = measureUnit;

                            item.Product = product;

                            cart.OrderItems.Add(item);
                        }

                        toReturn = cart;
                    }

                    return cart;
                },
                new {
                    NetId = netId, ClientAgreementNetId = toReturn.ClientAgreement.NetUid, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName,
                    WithVat = toReturn.IsVatCart
                }
            );
        }

        return toReturn;
    }

    public ClientShoppingCart GetByClientNetId(Guid netId, bool withVat) {
        ClientShoppingCart toReturn = null;

        _connection.Query<ClientShoppingCart, OrderItem, Product, ClientShoppingCart>(
            "SELECT * " +
            "FROM [ClientShoppingCart] " +
            "LEFT JOIN [OrderItem] " +
            "ON [OrderItem].ClientShoppingCartID = [ClientShoppingCart].ID " +
            "AND [OrderItem].Deleted = 0 " +
            "LEFT JOIN [Product] " +
            "ON [OrderItem].ProductID = [Product].ID " +
            "WHERE [ClientShoppingCart].ID = (" +
            "SELECT TOP(1) [ClientShoppingCart].ID " +
            "FROM [ClientShoppingCart] " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].ID = [ClientShoppingCart].ClientAgreementID " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [ClientAgreement].ClientID " +
            "LEFT JOIN [OrderItem] " +
            "ON [OrderItem].ClientShoppingCartID = [ClientShoppingCart].ID " +
            "AND [OrderItem].Deleted = 0 " +
            "WHERE [Client].NetUID = @NetId " +
            "AND [ClientShoppingCart].Deleted = 0 " +
            "AND [ClientShoppingCart].IsOffer = 0 " +
            "AND [ClientShoppingCart].IsVatCart = @WithVat " +
            "ORDER BY CASE WHEN [OrderItem].ID IS NOT NULL THEN 0 ELSE 1 END" +
            ")",
            (cart, item, product) => {
                if (toReturn != null) {
                    if (item != null && !toReturn.OrderItems.Any(i => i.Id.Equals(item.Id))) {
                        item.Product = product;

                        toReturn.OrderItems.Add(item);
                    }
                } else {
                    if (item != null) {
                        item.Product = product;

                        cart.OrderItems.Add(item);
                    }

                    toReturn = cart;
                }

                return cart;
            },
            new { NetId = netId, WithVat = withVat }
        );

        return toReturn;
    }

    public ClientShoppingCart GetByClientAgreementNetId(Guid netId, bool withVat, long? workplaceId = null) {
        ClientShoppingCart toReturn = null;

        string sqlExpression =
            "SELECT * " +
            "FROM [ClientShoppingCart] " +
            "LEFT JOIN [OrderItem] " +
            "ON [OrderItem].ClientShoppingCartID = [ClientShoppingCart].ID " +
            "AND [OrderItem].Deleted = 0 " +
            "LEFT JOIN [Product] " +
            "ON [OrderItem].ProductID = [Product].ID " +
            "WHERE [ClientShoppingCart].ID = (" +
            "SELECT TOP(1) [ClientShoppingCart].ID " +
            "FROM [ClientShoppingCart] " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].ID = [ClientShoppingCart].ClientAgreementID " +
            "LEFT JOIN [OrderItem] " +
            "ON [OrderItem].ClientShoppingCartID = [ClientShoppingCart].ID " +
            "AND [OrderItem].Deleted = 0 " +
            "WHERE [ClientAgreement].NetUID = @NetId " +
            "AND [ClientShoppingCart].Deleted = 0 " +
            "AND [ClientShoppingCart].IsOffer = 0 " +
            "AND [ClientShoppingCart].IsVatCart = @WithVat ";
        sqlExpression += workplaceId == null
            ? "AND [ClientShoppingCart].WorkplaceID IS NULL "
            : "AND [ClientShoppingCart].WorkplaceID = @WorkplaceId ";
        sqlExpression +=
            "ORDER BY CASE WHEN [OrderItem].ID IS NOT NULL THEN 0 ELSE 1 END" +
            ")";

        _connection.Query<ClientShoppingCart, OrderItem, Product, ClientShoppingCart>(
            sqlExpression,
            (cart, item, product) => {
                if (toReturn != null) {
                    if (item != null && !toReturn.OrderItems.Any(i => i.Id.Equals(item.Id))) {
                        item.Product = product;

                        toReturn.OrderItems.Add(item);
                    }
                } else {
                    if (item != null) {
                        item.Product = product;

                        cart.OrderItems.Add(item);
                    }

                    toReturn = cart;
                }

                return cart;
            },
            new { NetId = netId, WithVat = withVat, WorkplaceId = workplaceId }
        );

        return toReturn;
    }

    public ClientShoppingCart GetLastOfferByCulture(string culture) {
        string sqlExpression = "SELECT TOP(1) * " +
                               "FROM [ClientShoppingCart] " +
                               "WHERE [ClientShoppingCart].[Number] <> '' ";

        if (culture.Equals("pl"))
            sqlExpression += "AND [ClientShoppingCart].[Number] like N'P%' ";
        else
            sqlExpression += "AND [ClientShoppingCart].[Number] NOT like N'P%' ";

        sqlExpression += "ORDER BY [ClientShoppingCart].ID DESC";

        return _connection.Query<ClientShoppingCart>(
                sqlExpression
            )
            .SingleOrDefault();
    }

    public void UpdateValidUntilDate(ClientShoppingCart clientShoppingCart) {
        _connection.Execute(
            "UPDATE [ClientShoppingCart] " +
            "SET ValidUntil = @ValidUntil, Updated = GETUTCDATE() " +
            "WHERE [ClientShoppingCart].ID = @Id",
            clientShoppingCart
        );
    }

    public void UpdateProcessingStatus(ClientShoppingCart clientShoppingCart) {
        _connection.Execute(
            "UPDATE [ClientShoppingCart] " +
            "SET OfferProcessingStatus = @OfferProcessingStatus, OfferProcessingStatusChangedById = @OfferProcessingStatusChangedById, Comment = @Comment " +
            "WHERE ID = @Id",
            clientShoppingCart
        );
    }

    public void Remove(Guid netId) {
        _connection.Execute(
            "UPDATE [ClientShoppingCart] " +
            "SET Deleted = 1, Updated = GETUTCDATE() " +
            "WHERE [ClientShoppingCart].NetUID = @NetId",
            new { NetId = netId }
        );
    }

    public void SetProcessedByNetId(Guid netId) {
        _connection.Execute(
            "UPDATE [ClientShoppingCart] " +
            "SET IsOfferProcessed = 1 " +
            "WHERE [ClientShoppingCart].NetUID = @NetId",
            new { NetId = netId }
        );
    }

    public List<ClientShoppingCart> GetAllValidClientShoppingCarts() {
        // Use Dictionary for O(1) lookup instead of O(n) List.Any/First
        Dictionary<long, ClientShoppingCart> cartDict = new();

        string sqlExpression =
            "SELECT [ClientShoppingCart].* " +
            ",[ClientAgreement].* " +
            ",[Agreement].* " +
            ",[Client].* " +
            ",[RegionCode].* " +
            ",[OrderItem].* " +
            ",[Product].ID " +
            ",[Product].Deleted " +
            ",[Product].HasAnalogue " +
            ",[Product].HasComponent " +
            ",[Product].HasImage " +
            ",[Product].Created " +
            ",[Product].Image " +
            ",[Product].IsForSale " +
            ",[Product].IsForWeb " +
            ",[Product].IsForZeroSale " +
            ",[Product].MainOriginalNumber " +
            ",[Product].OrderStandard " +
            ",[Product].PackingStandard " +
            ",[Product].Size " +
            ",[Product].[Top] " +
            ",[Product].UCGFEA " +
            ",[Product].Updated " +
            ",[Product].VendorCode " +
            ",[Product].Volume " +
            ",[Product].Weight " +
            ",[Product].MeasureUnitID ";

        if (CultureInfo.CurrentCulture.TwoLetterISOLanguageName.Equals("pl")) {
            sqlExpression += ",[Product].[NameUA] AS [Name] ";
            sqlExpression += ",[Product].[DescriptionUA] AS [Description] ";
        } else {
            sqlExpression += ",[Product].[NameUA] AS [Name] ";
            sqlExpression += ",[Product].[DescriptionUA] AS [Description] ";
        }

        sqlExpression +=
            ",dbo.GetCalculatedProductPriceWithSharesAndVat([Product].NetUID, [ClientAgreement].NetUID, @Culture, [ClientShoppingCart].IsVatCart, [OrderItem].[ID]) AS [CurrentPrice] " +
            ",dbo.GetCalculatedProductLocalPriceWithSharesAndVat([Product].NetUID, [ClientAgreement].NetUID, @Culture, [ClientShoppingCart].IsVatCart, [OrderItem].[ID]) AS [CurrentLocalPrice] " +
            ",[MeasureUnit].* " +
            ",[CreatedBy].* " +
            ",[OrderItemUser].* " +
            "FROM [ClientShoppingCart] " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].ID = [ClientShoppingCart].ClientAgreementID " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [ClientAgreement].ClientID " +
            "LEFT JOIN [RegionCode] " +
            "ON [RegionCode].ID = [Client].RegionCodeID " +
            "LEFT JOIN [ClientInRole] " +
            "ON [ClientInRole].ClientID = [Client].ID " +
            "LEFT JOIN [OrderItem] " +
            "ON [OrderItem].ClientShoppingCartID = [ClientShoppingCart].ID " +
            "AND [OrderItem].Deleted = 0 " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [OrderItem].ProductID " +
            "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
            "ON [MeasureUnit].ID = [Product].MeasureUnitID " +
            "AND [MeasureUnit].CultureCode = @Culture " +
            "LEFT JOIN [User] AS [CreatedBy] " +
            "ON [CreatedBy].ID = [ClientShoppingCart].CreatedByID " +
            "LEFT JOIN [User] AS [OrderItemUser] " +
            "ON [OrderItemUser].ID = [OrderItem].UserID " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].ID = [ClientAgreement].AgreementID " +
            "WHERE [ClientShoppingCart].Deleted = 0 " +
            "AND ( " +
            "(@Culture = 'uk' AND [ClientInRole].ClientTypeRoleID = 1) " +
            "OR " +
            "(@Culture = 'pl' AND [ClientInRole].ClientTypeRoleID <> 1) " +
            ") " +
            "AND [OrderItem].ID IS NOT NULL " +
            "AND [ClientShoppingCart].IsOffer = 0 " +
            "ORDER BY [ClientShoppingCart].ValidUntil";

        Type[] types = {
            typeof(ClientShoppingCart),
            typeof(ClientAgreement),
            typeof(Agreement),
            typeof(Client),
            typeof(RegionCode),
            typeof(OrderItem),
            typeof(Product),
            typeof(MeasureUnit),
            typeof(User),
            typeof(User)
        };

        Func<object[], ClientShoppingCart> mapper = objects => {
            ClientShoppingCart clientShoppingCart = (ClientShoppingCart)objects[0];
            ClientAgreement clientAgreement = (ClientAgreement)objects[1];
            Agreement agreement = (Agreement)objects[2];
            Client client = (Client)objects[3];
            RegionCode regionCode = (RegionCode)objects[4];
            OrderItem orderItem = (OrderItem)objects[5];
            Product product = (Product)objects[6];
            MeasureUnit measureUnit = (MeasureUnit)objects[7];
            User createdBy = (User)objects[8];
            User orderItemUser = (User)objects[9];

            // O(1) lookup with TryGetValue instead of O(n) Any + First
            if (cartDict.TryGetValue(clientShoppingCart.Id, out ClientShoppingCart existingCart)) {
                product.MeasureUnit = measureUnit;

                orderItem.Product = product;
                orderItem.User = orderItemUser;

                existingCart.OrderItems.Add(orderItem);
            } else {
                client.RegionCode = regionCode;

                clientAgreement.Client = client;
                clientAgreement.Agreement = agreement;

                clientShoppingCart.ClientAgreement = clientAgreement;
                clientShoppingCart.CreatedBy = createdBy;

                product.MeasureUnit = measureUnit;

                orderItem.Product = product;

                clientShoppingCart.OrderItems.Add(orderItem);

                cartDict[clientShoppingCart.Id] = clientShoppingCart;
            }

            return clientShoppingCart;
        };

        _connection.Query(
            sqlExpression,
            types,
            mapper,
            new { Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
        );

        return cartDict.Values.ToList();
    }

    public List<ClientShoppingCart> GetAllAvailableOffersByClientNetId(Guid netId) {
        List<ClientShoppingCart> toReturn =
            _connection.Query<ClientShoppingCart, ClientAgreement, ClientShoppingCart>(
                    "SELECT [ClientShoppingCart].* " +
                    ", [ClientAgreement].*" +
                    "FROM [ClientShoppingCart] " +
                    "LEFT JOIN [ClientAgreement] " +
                    "ON [ClientAgreement].ID = [ClientShoppingCart].ClientAgreementID " +
                    "LEFT JOIN [Client] " +
                    "ON [Client].ID = [ClientAgreement].ClientID " +
                    "WHERE [Client].NetUID = @NetId " +
                    "AND [ClientShoppingCart].Deleted = 0 " +
                    "AND [ClientShoppingCart].IsOffer = 1 " +
                    "AND [ClientShoppingCart].ValidUntil >= @FromDate " +
                    "ORDER BY [ClientShoppingCart].ID DESC",
                    (cart, agreement) => {
                        cart.ClientAgreement = agreement;

                        return cart;
                    },
                    new { NetId = netId, FromDate = DateTime.Now.Date }
                )
                .ToList();

        return toReturn;
    }

    public List<ClientShoppingCart> GetAllExistingUnavailableCarts() {
        // Use Dictionary for O(1) lookup instead of O(n) List.Any/First
        Dictionary<long, ClientShoppingCart> cartDict = new();

        _connection.Query<ClientShoppingCart, OrderItem, ClientShoppingCart>(
            "SELECT * " +
            "FROM [ClientShoppingCart] " +
            "LEFT JOIN [OrderItem] " +
            "ON [OrderItem].ClientShoppingCartID = [ClientShoppingCart].ID " +
            "AND [OrderItem].Deleted = 0 " +
            "WHERE [ClientShoppingCart].Deleted = 0 " +
            "AND [ClientShoppingCart].ValidUntil < @FromDate",
            (cart, item) => {
                // O(1) lookup with TryGetValue instead of O(n) Any + First
                if (cartDict.TryGetValue(cart.Id, out ClientShoppingCart existingCart)) {
                    if (item != null) existingCart.OrderItems.Add(item);
                } else {
                    if (item != null) cart.OrderItems.Add(item);

                    cartDict[cart.Id] = cart;
                }

                return cart;
            },
            new { FromDate = DateTime.Now.Date }
        );

        return cartDict.Values.ToList();
    }

    public List<ClientShoppingCart> GetAllExistingExpiredClientShoppingCarts() {
        // Use Dictionary for O(1) lookup instead of O(n) List.Any/First
        Dictionary<long, ClientShoppingCart> cartDict = new();

        _connection.Query<ClientShoppingCart, OrderItem, ClientShoppingCart>(
            "SELECT [ClientShoppingCart].* " +
            ", [OrderItem].* " +
            "FROM [ClientShoppingCart] " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].ID = [ClientShoppingCart].ClientAgreementID " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [ClientAgreement].ClientID " +
            "LEFT JOIN [OrderItem] " +
            "ON [OrderItem].ClientShoppingCartID = [ClientShoppingCart].ID " +
            "AND [OrderItem].Deleted = 0 " +
            "WHERE [ClientShoppingCart].Deleted = 0 " +
            "AND [Client].ClearCartAfterDays <> 0 " +
            "AND [Client].ClearCartAfterDays <= DATEDIFF(day, [OrderItem].Created, GETUTCDATE()) " +
            "AND [ClientShoppingCart].ValidUntil < @FromDate ",
            (cart, item) => {
                // O(1) lookup with TryGetValue instead of O(n) Any + First
                if (cartDict.TryGetValue(cart.Id, out ClientShoppingCart existingCart)) {
                    if (item != null) existingCart.OrderItems.Add(item);
                } else {
                    if (item != null) cart.OrderItems.Add(item);

                    cartDict[cart.Id] = cart;
                }

                return cart;
            },
            new { FromDate = DateTime.Now.Date }
        );

        return cartDict.Values.ToList();
    }

    public List<ClientShoppingCart> GetAllOffersFiltered(DateTime from, DateTime to) {
        // Use Dictionary for O(1) lookup instead of O(n) List.Any/First
        Dictionary<long, ClientShoppingCart> cartDict = new();

        string sqlExpression = "SELECT [ClientShoppingCart].* " +
                               ",[ClientAgreement].* " +
                               ",[Agreement].* " +
                               ",[Client].* " +
                               ",[RegionCode].* " +
                               ",[OrderItem].* " +
                               ",[Product].ID " +
                               ",[Product].Deleted " +
                               ",[Product].HasAnalogue " +
                               ",[Product].HasComponent " +
                               ",[Product].HasImage " +
                               ",[Product].Created " +
                               ",[Product].Image " +
                               ",[Product].IsForSale " +
                               ",[Product].IsForWeb " +
                               ",[Product].IsForZeroSale " +
                               ",[Product].MainOriginalNumber " +
                               ",[Product].OrderStandard " +
                               ",[Product].PackingStandard " +
                               ",[Product].Size " +
                               ",[Product].[Top] " +
                               ",[Product].UCGFEA " +
                               ",[Product].Updated " +
                               ",[Product].VendorCode " +
                               ",[Product].Volume " +
                               ",[Product].Weight " +
                               ",[Product].MeasureUnitID ";

        if (CultureInfo.CurrentCulture.TwoLetterISOLanguageName.Equals("pl")) {
            sqlExpression += ",[Product].[NameUA] AS [Name] ";
            sqlExpression += ",[Product].[DescriptionUA] AS [Description] ";
        } else {
            sqlExpression += ",[Product].[NameUA] AS [Name] ";
            sqlExpression += ",[Product].[DescriptionUA] AS [Description] ";
        }

        sqlExpression +=
            ",dbo.GetCalculatedProductPriceWithSharesAndVat([Product].NetUID, [ClientAgreement].NetUID, @Culture, [Agreement].[WithVATAccounting], [OrderItem].[ID]) AS [CurrentPrice] " +
            ",dbo.GetCalculatedProductLocalPriceWithSharesAndVat([Product].NetUID, [ClientAgreement].NetUID, @Culture, [Agreement].[WithVATAccounting], [OrderItem].[ID]) AS [CurrentLocalPrice] " +
            ",[MeasureUnit].* " +
            ",[CreatedBy].* " +
            ",[OrderItemUser].* " +
            "FROM [ClientShoppingCart] " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].ID = [ClientShoppingCart].ClientAgreementID " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [ClientAgreement].ClientID " +
            "LEFT JOIN [RegionCode] " +
            "ON [RegionCode].ID = [Client].RegionCodeID " +
            "LEFT JOIN [ClientInRole] " +
            "ON [ClientInRole].ClientID = [Client].ID " +
            "LEFT JOIN [OrderItem] " +
            "ON [OrderItem].ClientShoppingCartID = [ClientShoppingCart].ID " +
            "AND [OrderItem].Deleted = 0 " +
            "LEFT JOIN [Product] " +
            "ON [Product].ID = [OrderItem].ProductID " +
            "LEFT JOIN [views].[MeasureUnitView] AS [MeasureUnit] " +
            "ON [MeasureUnit].ID = [Product].MeasureUnitID " +
            "AND [MeasureUnit].CultureCode = @Culture " +
            "LEFT JOIN [User] AS [CreatedBy] " +
            "ON [CreatedBy].ID = [ClientShoppingCart].CreatedByID " +
            "LEFT JOIN [User] AS [OrderItemUser] " +
            "ON [OrderItemUser].ID = [OrderItem].UserID " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].ID = [ClientAgreement].AgreementID " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].ID = [Agreement].OrganizationID " +
            "WHERE [ClientShoppingCart].Deleted = 0 " +
            "AND [ClientShoppingCart].IsOffer = 1 " +
            "AND [Organization].Culture = @Culture " +
            "AND [ClientShoppingCart].Created >= @From " +
            "AND [ClientShoppingCart].Created <= @To " +
            "ORDER BY [ClientShoppingCart].Created DESC";

        Type[] types = {
            typeof(ClientShoppingCart),
            typeof(ClientAgreement),
            typeof(Agreement),
            typeof(Client),
            typeof(RegionCode),
            typeof(OrderItem),
            typeof(Product),
            typeof(MeasureUnit),
            typeof(User),
            typeof(User)
        };

        Func<object[], ClientShoppingCart> mapper = objects => {
            ClientShoppingCart clientShoppingCart = (ClientShoppingCart)objects[0];
            ClientAgreement clientAgreement = (ClientAgreement)objects[1];
            Agreement agreement = (Agreement)objects[2];
            Client client = (Client)objects[3];
            RegionCode regionCode = (RegionCode)objects[4];
            OrderItem orderItem = (OrderItem)objects[5];
            Product product = (Product)objects[6];
            MeasureUnit measureUnit = (MeasureUnit)objects[7];
            User createdBy = (User)objects[8];
            User orderItemUser = (User)objects[9];

            // O(1) lookup with TryGetValue instead of O(n) Any + First
            if (cartDict.TryGetValue(clientShoppingCart.Id, out ClientShoppingCart existingCart)) {
                product.MeasureUnit = measureUnit;

                orderItem.Product = product;
                orderItem.User = orderItemUser;

                existingCart.OrderItems.Add(orderItem);
            } else {
                client.RegionCode = regionCode;

                clientAgreement.Client = client;
                clientAgreement.Agreement = agreement;

                clientShoppingCart.ClientAgreement = clientAgreement;
                clientShoppingCart.CreatedBy = createdBy;

                product.MeasureUnit = measureUnit;

                orderItem.Product = product;

                clientShoppingCart.OrderItems.Add(orderItem);

                cartDict[clientShoppingCart.Id] = clientShoppingCart;
            }

            return clientShoppingCart;
        };

        _connection.Query(
            sqlExpression,
            types,
            mapper,
            new { Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName, From = from, To = to }
        );

        return cartDict.Values.ToList();
    }
}