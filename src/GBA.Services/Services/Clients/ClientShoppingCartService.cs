using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Transactions;
using GBA.Common.Configuration;
using GBA.Common.Exceptions.CustomExceptions;
using GBA.Common.Helpers;
using GBA.Common.ResourceNames.ECommerce;
using GBA.Common.Search;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Products;
using GBA.Domain.Entities.Sales;
using GBA.Domain.EntityHelpers.ClientShoppingCartModels;
using GBA.Domain.Repositories.Agreements.Contracts;
using GBA.Domain.Repositories.Clients.Contracts;
using GBA.Domain.Repositories.Currencies.Contracts;
using GBA.Domain.Repositories.ExchangeRates.Contracts;
using GBA.Domain.Repositories.Pricings.Contracts;
using GBA.Domain.Repositories.Products.Contracts;
using GBA.Domain.Repositories.Sales.Contracts;
using GBA.Domain.Repositories.Storages.Contracts;
using GBA.Services.Infrastructure;
using GBA.Services.Infrastructure.Checkout;
using GBA.Services.Services.Clients.Contracts;
using Microsoft.Extensions.Http;
using GBA.Common.Models;

namespace GBA.Services.Services.Clients;

public sealed class ClientShoppingCartService : IClientShoppingCartService {
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new() {
        PropertyNameCaseInsensitive = true
    };

    private static OrderItem NormalizeOverLordQty(OrderItem orderItem) {
        if (orderItem.OverLordQty <= 0 && orderItem.Qty > 0) orderItem.OverLordQty = orderItem.Qty;

        return orderItem;
    }

    internal static (ClientAgreement ClientAgreement, Workplace Workplace) ResolveCartContext(
        Guid actorNetId,
        IClientAgreementRepository clientAgreementRepository,
        IWorkplaceRepository workplaceRepository) {
        return ShopClientAgreements.ResolveWithWorkplace(actorNetId, clientAgreementRepository, workplaceRepository);
    }

    private decimal ResolveLiveProductExchangeRate(
        IDbConnection connection,
        Product product,
        ClientAgreement clientAgreement) {
        long currencyId = clientAgreement?.Agreement?.CurrencyId ??
            throw new InvalidOperationException("Client agreement currency is not configured.");
        decimal exchangeRateAmount = _exchangeRateRepositoriesFactory
            .NewExchangeRateRepository(connection)
            .GetEuroExchangeRateByCurrentCultureFiltered(
                product.NetUid,
                clientAgreement.Agreement.WithVATAccounting,
                false,
                currencyId);

        if (exchangeRateAmount <= 0m)
            throw new InvalidOperationException(
                $"A valid exchange rate is not configured for product {product.NetUid} and currency {currencyId}.");

        return exchangeRateAmount;
    }

    private OrderItem ApplyAuthoritativeProduct(
        IDbConnection connection,
        ClientAgreement clientAgreement,
        OrderItem orderItem) {
        if (orderItem == null || !double.IsFinite(orderItem.Qty) ||
            orderItem.Qty <= 0 || orderItem.Qty > 100000)
            throw new ArgumentException("Order item quantity is invalid.");

        IGetSingleProductRepository productRepository =
            _productRepositoriesFactory.NewGetSingleProductRepository(connection);
        Guid productNetId = orderItem.Product?.NetUid ?? Guid.Empty;
        if (productNetId == Guid.Empty && orderItem.ProductId > 0)
            productNetId = productRepository.GetById(orderItem.ProductId)?.NetUid ?? Guid.Empty;
        if (productNetId == Guid.Empty)
            throw new ArgumentException("A valid product is required.");

        Product product = productRepository.GetProductByNetId(
            productNetId,
            clientAgreement.NetUid,
            clientAgreement.Agreement.WithVATAccounting,
            clientAgreement.Agreement.CurrencyId,
            clientAgreement.Agreement.OrganizationId);
        if (!EcommercePurchasability.IsPurchasable(product))
            throw new ArgumentException(EcommercePurchasability.NotAvailableMessage);
        if (!EcommercePurchasability.HasSellablePrice(product))
            throw new ArgumentException(EcommercePurchasability.NotPricedMessage);

        decimal exchangeRateAmount = ResolveLiveProductExchangeRate(
            connection,
            product,
            clientAgreement);
        product.CurrentLocalPrice = decimal.Round(
            product.CurrentPrice * exchangeRateAmount, 2, MidpointRounding.AwayFromZero);

        orderItem.Product = product;
        orderItem.ProductId = product.Id;
        orderItem.PricePerItem = product.CurrentPrice;
        orderItem.ExchangeRateAmount = exchangeRateAmount;
        orderItem.TotalAmount = decimal.Round(product.CurrentPrice * Convert.ToDecimal(orderItem.Qty), 2, MidpointRounding.AwayFromZero);
        orderItem.TotalAmountLocal = decimal.Round(product.CurrentLocalPrice * Convert.ToDecimal(orderItem.Qty), 2, MidpointRounding.AwayFromZero);
        orderItem.OverLordQty = orderItem.Qty;
        orderItem.OrderedQty = orderItem.Qty;
        orderItem.OneTimeDiscount = 0;
        orderItem.Discount = 0;
        orderItem.DiscountAmount = 0;
        orderItem.PricePerItemWithoutVat = 0;
        orderItem.FromOfferQty = 0;
        orderItem.IsFromOffer = false;
        orderItem.IsFromReSale = false;
        orderItem.AssignedSpecificationId = null;
        orderItem.MisplacedSaleId = null;
        orderItem.IsValidForCurrentSale = true;
        orderItem.Vat = clientAgreement.Agreement.WithVATAccounting
            ? Convert.ToDecimal(clientAgreement.Agreement.Organization?.VatRate?.Value ?? 0)
            : 0;

        return orderItem;
    }

    private readonly IAgreementRepositoriesFactory _agreementRepositoriesFactory;
    private readonly IClientRepositoriesFactory _clientRepositoriesFactory;
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ICurrencyRepositoriesFactory _currencyRepositoriesFactory;
    private readonly IExchangeRateRepositoriesFactory _exchangeRateRepositoriesFactory;
    private readonly IPricingRepositoriesFactory _pricingRepositoriesFactory;
    private readonly IProductRepositoriesFactory _productRepositoriesFactory;
    private readonly ISaleRepositoriesFactory _saleRepositoriesFactory;
    private readonly IStorageRepositoryFactory _storageRepositoryFactory;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ISearchReindexSignal _reindexSignal;

    public ClientShoppingCartService(
        IClientRepositoriesFactory clientRepositoriesFactory,
        ISaleRepositoriesFactory saleRepositoriesFactory,
        IProductRepositoriesFactory productRepositoriesFactory,
        IStorageRepositoryFactory storageRepositoryFactory,
        IPricingRepositoriesFactory pricingRepositoriesFactory,
        IExchangeRateRepositoriesFactory exchangeRateRepositoriesFactory,
        ICurrencyRepositoriesFactory currencyRepositoriesFactory,
        IDbConnectionFactory connectionFactory,
        IAgreementRepositoriesFactory agreementRepositoriesFactory,
        IHttpClientFactory httpClientFactory,
        ISearchReindexSignal reindexSignal) {
        _clientRepositoriesFactory = clientRepositoriesFactory;
        _reindexSignal = reindexSignal;

        _saleRepositoriesFactory = saleRepositoriesFactory;

        _productRepositoriesFactory = productRepositoriesFactory;

        _storageRepositoryFactory = storageRepositoryFactory;

        _pricingRepositoriesFactory = pricingRepositoriesFactory;

        _exchangeRateRepositoriesFactory = exchangeRateRepositoriesFactory;
        _currencyRepositoriesFactory = currencyRepositoriesFactory;

        _connectionFactory = connectionFactory;
        _agreementRepositoriesFactory = agreementRepositoriesFactory;
        _httpClientFactory = httpClientFactory;
    }

    public Task<OrderItem> Add(OrderItem orderItem, Guid clientNetId, bool withVat) {
        if (orderItem == null)
            throw new Exception("OrderItem can not be empty.");
        if (!orderItem.IsNew())
            throw new Exception("Existing OrderItem is not valid input for current request.");
        if (orderItem.ProductId.Equals(0) && orderItem.Product == null)
            throw new Exception("Product need to be specified.");
        if (orderItem.Qty.Equals(0)) throw new Exception("You need to specify Qty of product that will be added.");

        return Task.FromResult(RunCartWrite((connection, effects) => {
            CartContext context = ResolveCartContext(connection, clientNetId);

            ApplyAuthoritativeProduct(connection, context.ClientAgreement, orderItem);

            IShoppingCartStockRepository stock = _clientRepositoriesFactory.NewShoppingCartStockRepository(connection);
            OpenCart cart = OpenCartForAdding(connection, stock, context);

            stock.LockProductStock(new[] { orderItem.ProductId });

            List<ProductAvailability> availabilities = ReadPurchasableStock(connection, context, orderItem.ProductId);
            double sellableQuantity = SellableQuantity(availabilities);

            if (sellableQuantity < orderItem.Qty)
                throw new LocalizedException(
                    ClientShoppingCartResourceNames.INSUFFICIENT_QTY_OF_PRODUCTS,
                    sellableQuantity
                );

            OrderItem line = AddToCart(connection, stock, cart, orderItem, availabilities, orderItem.Qty);

            stock.ExtendCart(cart.Id, DateTime.Now.Date);
            effects.StockChanged(line.ProductId, orderItem.Product.NetUid);

            return LoadCartLine(connection, line.Id, context.ClientAgreement);
        }));
    }

    public Task<List<OrderItem>> Add(List<OrderItem> orderItems, Guid clientNetId, bool withVat) {
        List<OrderItem> requested = (orderItems ?? new List<OrderItem>())
            .Where(item => item != null &&
                           item.IsNew() &&
                           !(item.ProductId.Equals(0) && item.Product == null) &&
                           !item.Qty.Equals(0))
            .ToList();
        if (requested.Count > 100)
            throw new ArgumentException("Between 1 and 100 order items can be added at once.");
        if (requested.Count == 0) return Task.FromResult(new List<OrderItem>());

        return Task.FromResult(RunCartWrite((connection, effects) => {
            CartContext context = ResolveCartContext(connection, clientNetId);

            foreach (OrderItem orderItem in requested)
                ApplyAuthoritativeProduct(connection, context.ClientAgreement, orderItem);

            IShoppingCartStockRepository stock = _clientRepositoriesFactory.NewShoppingCartStockRepository(connection);
            OpenCart cart = OpenCartForAdding(connection, stock, context);

            stock.LockProductStock(requested.Select(item => item.ProductId));

            List<long> lineIds = new();
            foreach (OrderItem orderItem in requested) {
                List<ProductAvailability> availabilities = ReadPurchasableStock(connection, context, orderItem.ProductId);
                double qty = Math.Min(orderItem.Qty, SellableQuantity(availabilities));
                if (qty <= 0d) continue;

                orderItem.Qty = qty;
                orderItem.OverLordQty = qty;
                orderItem.OrderedQty = qty;

                OrderItem line = AddToCart(connection, stock, cart, orderItem, availabilities, qty);
                if (!lineIds.Contains(line.Id)) lineIds.Add(line.Id);
                effects.StockChanged(line.ProductId, orderItem.Product.NetUid);
            }

            if (lineIds.Count > 0) stock.ExtendCart(cart.Id, DateTime.Now.Date);

            return lineIds
                .Select(id => LoadCartLine(connection, id, context.ClientAgreement))
                .ToList();
        }));
    }

    public Task<OrderItem> Update(OrderItem orderItem, Guid clientNetId, bool withVat) {
        if (orderItem == null)
            throw new Exception("OrderItem can not be empty.");
        if (orderItem.IsNew())
            throw new Exception("New OrderItem is not valid input for current request.");
        ValidateRequestedQuantity(orderItem);

        return Task.FromResult(RunCartWrite((connection, effects) => {
            CartContext context = ResolveCartContext(connection, clientNetId);
            IShoppingCartStockRepository stock = _clientRepositoriesFactory.NewShoppingCartStockRepository(connection);
            OpenCart cart = OpenExistingCart(connection, stock, context)
                ?? throw new ArgumentException("Order item is not part of the current shopping cart.");

            OrderItem current = cart.Items.SingleOrDefault(item => item.Id == orderItem.Id)
                ?? throw new ArgumentException("Order item is not part of the current shopping cart.");

            stock.LockProductStock(new[] { current.ProductId });

            ChangeQuantity(connection, stock, context, cart, current, orderItem, effects);

            return NormalizeOverLordQty(
                _saleRepositoriesFactory
                    .NewOrderItemRepository(connection)
                    .GetByIdWithIncludes(current.Id, context.ClientAgreement.NetUid));
        }));
    }

    public Task<List<OrderItem>> Update(List<OrderItem> orderItems, Guid clientNetId, bool withVat) {
        List<OrderItem> requested = (orderItems ?? new List<OrderItem>())
            .Where(item => item != null && !item.IsNew())
            .ToList();
        if (requested.Count > 100)
            throw new ArgumentException("Between 1 and 100 order items can be updated at once.");
        foreach (OrderItem orderItem in requested) ValidateRequestedQuantity(orderItem);
        if (requested.Count == 0) return Task.FromResult(new List<OrderItem>());

        return Task.FromResult(RunCartWrite((connection, effects) => {
            CartContext context = ResolveCartContext(connection, clientNetId);
            IShoppingCartStockRepository stock = _clientRepositoriesFactory.NewShoppingCartStockRepository(connection);
            OpenCart cart = OpenExistingCart(connection, stock, context)
                ?? throw new ArgumentException("Order item is not part of the current shopping cart.");

            List<OrderItem> currentLines = requested
                .Select(orderItem => cart.Items.SingleOrDefault(item => item.Id == orderItem.Id)
                    ?? throw new ArgumentException("Order item is not part of the current shopping cart."))
                .ToList();

            stock.LockProductStock(currentLines.Select(item => item.ProductId));

            for (int index = 0; index < requested.Count; index++)
                ChangeQuantity(connection, stock, context, cart, currentLines[index], requested[index], effects);

            IOrderItemRepository orderItemRepository = _saleRepositoriesFactory.NewOrderItemRepository(connection);

            return currentLines
                .Select(item => NormalizeOverLordQty(orderItemRepository.GetByIdWithIncludes(item.Id, context.ClientAgreement.NetUid)))
                .ToList();
        }));
    }

    public Task<IEnumerable<OrderItem>> GetAllItemsFromCurrentShoppingCartByClientNetId(Guid netId, bool withVat) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
        CartContext context;

        try {
            context = ResolveCartContext(connection, netId);
        } catch (ArgumentException) {
            return Task.FromResult(Enumerable.Empty<OrderItem>());
        }

        ClientAgreement selectedAgreement = context.ClientAgreement;

        IEnumerable<OrderItem> orderItems = _saleRepositoriesFactory
            .NewOrderItemRepository(connection)
            .GetAllFromCurrentShoppingByClientNetId(
                context.Workplace?.Id,
                selectedAgreement.NetUid,
                selectedAgreement.Agreement.CurrencyId,
                selectedAgreement.Agreement.OrganizationId,
                selectedAgreement.Agreement.WithVATAccounting
            );

        return Task.FromResult(orderItems.Select(NormalizeOverLordQty));
    }

    public Task DeleteItemFromShoppingCartByNetId(Guid itemNetId, Guid clientNetId, bool withVat) {
        RunCartWrite((connection, effects) => {
            CartContext context = ResolveCartContext(connection, clientNetId);
            IShoppingCartStockRepository stock = _clientRepositoriesFactory.NewShoppingCartStockRepository(connection);
            OpenCart cart = OpenExistingCart(connection, stock, context);

            OrderItem item = cart?.Items.FirstOrDefault(cartItem => cartItem.NetUid.Equals(itemNetId));
            if (item == null) return 0;

            stock.LockProductStock(new[] { item.ProductId });

            if (!RemoveFromCart(connection, stock, cart, item, effects)) return 0;

            stock.ExtendCart(cart.Id, DateTime.Now.Date);

            return 1;
        });

        return Task.CompletedTask;
    }

    public Task DeleteAllItemsFromShoppingCartByClientNetId(Guid clientNetId, bool withVat) {
        RunCartWrite((connection, effects) => {
            CartContext context;

            try {
                context = ResolveCartContext(connection, clientNetId);
            } catch (ArgumentException) {
                return 0;
            }

            IShoppingCartStockRepository stock = _clientRepositoriesFactory.NewShoppingCartStockRepository(connection);
            OpenCart cart = OpenExistingCart(connection, stock, context);
            if (cart == null || cart.Items.Count == 0) return 0;

            stock.LockProductStock(cart.Items.Select(item => item.ProductId));

            int removed = cart.Items
                .OrderBy(item => item.Id)
                .Count(item => RemoveFromCart(connection, stock, cart, item, effects));

            if (removed > 0) stock.ExtendCart(cart.Id, DateTime.Now.Date);

            return removed;
        });

        return Task.CompletedTask;
    }

    public Task<int> ReleaseExpiredCartsAsync() {
        DateTime today = DateTime.Now.Date;
        DateTime utcNow = DateTime.UtcNow;
        List<long> expiredCartIds;

        using (IDbConnection connection = _connectionFactory.NewSqlConnection())
            expiredCartIds = _clientRepositoriesFactory
                .NewShoppingCartStockRepository(connection)
                .GetExpiredCartIds(today, utcNow);

        int releasedReservations = 0;

        foreach (long cartId in expiredCartIds) {
            try {
                releasedReservations += RunCartWrite((connection, effects) =>
                    ReleaseExpiredCart(connection, effects, cartId, today, utcNow));
            } catch (CheckoutConflictException) {
            }
        }

        return Task.FromResult(releasedReservations);
    }

    private int ReleaseExpiredCart(
        IDbConnection connection,
        CartWriteEffects effects,
        long cartId,
        DateTime today,
        DateTime utcNow) {
        IShoppingCartStockRepository stock = _clientRepositoriesFactory.NewShoppingCartStockRepository(connection);

        if (!stock.TryLockCart(cartId) || !stock.IsExpired(cartId, today, utcNow)) return 0;

        List<OrderItem> items = stock.GetCartItems(cartId);
        stock.LockProductStock(items.Select(item => item.ProductId));

        IOrderItemRepository orderItemRepository = _saleRepositoriesFactory.NewOrderItemRepository(connection);
        int releasedReservations = 0;

        foreach (OrderItem item in items.OrderBy(cartItem => cartItem.Id)) {
            if (!orderItemRepository.RemoveFromShoppingCart(item.NetUid, cartId)) continue;

            releasedReservations += stock.ReleaseAll(item.Id);
            effects.Reindex(item.ProductId);
        }

        stock.RemoveIfEmpty(cartId);

        return releasedReservations;
    }

    public Task<Tuple<bool, string>> VerifyProductAvailability(OrderItem orderItem) {
        using IDbConnection connection = _connectionFactory.NewSqlConnection();
            if (orderItem == null)
                throw new Exception("OrderItem can not be empty.");
            if (!orderItem.IsNew())
                throw new Exception("Existing OrderItem is not valid input for current request.");
            if (orderItem.ProductId.Equals(0) && orderItem.Product == null)
                throw new Exception("Product need to be specified.");
            if (!double.IsFinite(orderItem.Qty) || orderItem.Qty <= 0 || orderItem.Qty > 100000)
                throw new ArgumentException("Order item quantity is invalid.");

            IClientAgreementRepository clientAgreementRepository = _clientRepositoriesFactory.NewClientAgreementRepository(connection);

            Storage storage = _storageRepositoryFactory.NewStorageRepository(connection)
                .GetWithHighestPriority();
            if (storage?.OrganizationId == null)
                return Task.FromResult(new Tuple<bool, string>(false, "Retail storage is not configured"));

            Client retailClient = _clientRepositoriesFactory
                .NewClientRepository(connection)
                .GetRetailClient();
            if (retailClient == null)
                return Task.FromResult(new Tuple<bool, string>(false, "Retail client is not configured"));

            ClientAgreement clientAgreement = clientAgreementRepository.GetByClientNetIdWithOrWithoutVat(
                retailClient.NetUid,
                storage.OrganizationId.Value,
                storage.ForVatProducts);
            if (clientAgreement?.Agreement == null)
                return Task.FromResult(new Tuple<bool, string>(false, "Retail agreement is not configured"));

            IGetSingleProductRepository productRepository =
                _productRepositoriesFactory.NewGetSingleProductRepository(connection);
            Guid productNetId = orderItem.Product?.NetUid ?? Guid.Empty;
            if (productNetId == Guid.Empty && orderItem.ProductId > 0)
                productNetId = productRepository.GetById(orderItem.ProductId)?.NetUid ?? Guid.Empty;

            Product product = productNetId == Guid.Empty
                ? null
                : productRepository.GetByNetIdForRetail(
                    productNetId,
                    storage.Id,
                    storage.OrganizationId.Value,
                    storage.ForVatProducts);
            if (!EcommercePurchasability.IsPurchasable(product))
                return Task.FromResult(new Tuple<bool, string>(false, EcommercePurchasability.NotAvailableMessage));
            if (!EcommercePurchasability.HasSellablePrice(product))
                return Task.FromResult(new Tuple<bool, string>(false, EcommercePurchasability.NotPricedMessage));

            orderItem.ProductId = product.Id;

            IProductAvailabilityRepository productAvailabilityRepository = _productRepositoriesFactory.NewProductAvailabilityRepository(connection);
            ProductAvailability productAvailability =
                productAvailabilityRepository.GetByProductAndStorageIds(product.Id, storage.Id);
            double qtyRemainderProducts = Math.Max(0d, productAvailability?.Amount ?? 0d);

            if (qtyRemainderProducts < orderItem.Qty) return Task.FromResult(new Tuple<bool, string>(false, "Product is not available"));

            return Task.FromResult(new Tuple<bool, string>(true, string.Empty));
    }

    private const double QuantityTolerance = 1e-9;

    private T RunCartWrite<T>(Func<IDbConnection, CartWriteEffects, T> work) {
        if (Transaction.Current != null)
            throw new InvalidOperationException("Shopping cart writes must own their database transaction.");

        CartWriteEffects effects = new();
        T result;

        try {
            using TransactionScope scope = CheckoutTransaction.Begin();
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            connection.Open();

            result = work(connection, effects);

            scope.Complete();
        } catch (Exception exception) when (CheckoutExecutor.IsTransientConcurrencyFailure(exception)) {
            throw new CheckoutConflictException(CheckoutConflictException.CartStockChangedMessage);
        }

        foreach (long productId in effects.ReindexProductIds) _reindexSignal.Request(productId);
        foreach (Guid productNetId in effects.AvailabilitySyncProductNetIds) QueueAvailabilitySync(productNetId);

        return result;
    }

    private CartContext ResolveCartContext(IDbConnection connection, Guid actorNetId) {
        (ClientAgreement clientAgreement, Workplace workplace) = ResolveCartContext(
            actorNetId,
            _clientRepositoriesFactory.NewClientAgreementRepository(connection),
            _clientRepositoriesFactory.NewWorkplaceRepository(connection));

        return new CartContext(clientAgreement, workplace);
    }

    private OpenCart OpenCartForAdding(
        IDbConnection connection,
        IShoppingCartStockRepository stock,
        CartContext context) {
        ClientAgreement clientAgreement = context.ClientAgreement;
        bool isVatCart = clientAgreement.Agreement.WithVATAccounting;

        if (!stock.TryLockCartScope(clientAgreement.Id, isVatCart, context.Workplace?.Id))
            throw new CheckoutConflictException(CheckoutConflictException.CartStockChangedMessage);

        OpenCart cart = OpenExistingCart(connection, stock, context);
        if (cart != null) return cart;

        Client client = _clientRepositoriesFactory
            .NewClientRepository(connection)
            .GetByIdWithRegionCode(clientAgreement.ClientId);

        long cartId = _clientRepositoriesFactory
            .NewClientShoppingCartRepository(connection)
            .Add(new ClientShoppingCart {
                ValidUntil = DateTime.Now.Date.AddDays(client?.ClearCartAfterDays ?? 0),
                ClientAgreementId = clientAgreement.Id,
                IsVatCart = isVatCart,
                WorkplaceId = context.Workplace?.Id
            });

        return new OpenCart(cartId, new List<OrderItem>());
    }

    private OpenCart OpenExistingCart(
        IDbConnection connection,
        IShoppingCartStockRepository stock,
        CartContext context) {
        ClientShoppingCart cart = _clientRepositoriesFactory
            .NewClientShoppingCartRepository(connection)
            .GetByClientAgreementNetId(
                context.ClientAgreement.NetUid,
                context.ClientAgreement.Agreement.WithVATAccounting,
                context.Workplace?.Id);

        if (cart == null || !stock.TryLockCart(cart.Id)) return null;

        return new OpenCart(cart.Id, stock.GetCartItems(cart.Id));
    }

    private List<ProductAvailability> ReadPurchasableStock(
        IDbConnection connection,
        CartContext context,
        long productId) {
        return _productRepositoriesFactory
            .NewProductAvailabilityRepository(connection)
            .GetForEcommercePurchase(
                productId,
                context.ClientAgreement.Agreement.Organization.Id)
            .ToList();
    }

    private static double SellableQuantity(IEnumerable<ProductAvailability> availabilities) {
        return availabilities.Sum(availability => Math.Max(0d, availability.Amount));
    }

    private OrderItem AddToCart(
        IDbConnection connection,
        IShoppingCartStockRepository stock,
        OpenCart cart,
        OrderItem orderItem,
        List<ProductAvailability> availabilities,
        double qty) {
        OrderItem line = cart.Items.FirstOrDefault(item => item.ProductId.Equals(orderItem.ProductId));

        if (line != null) {
            if (!stock.IncreaseItemQty(line.Id, cart.Id, line.Qty, qty))
                throw new CheckoutConflictException(CheckoutConflictException.CartStockChangedMessage);

            line.Qty += qty;
        } else {
            orderItem.ClientShoppingCartId = cart.Id;
            orderItem.Id = _saleRepositoriesFactory.NewOrderItemRepository(connection).Add(orderItem);
            line = orderItem;
            cart.Items.Add(line);
        }

        Reserve(stock, line.Id, availabilities, qty);

        return line;
    }

    private void ChangeQuantity(
        IDbConnection connection,
        IShoppingCartStockRepository stock,
        CartContext context,
        OpenCart cart,
        OrderItem current,
        OrderItem requested,
        CartWriteEffects effects) {
        IOrderItemRepository orderItemRepository = _saleRepositoriesFactory.NewOrderItemRepository(connection);

        requested.ProductId = current.ProductId;
        requested.Product = current.Product;
        if (requested.OverLordQty <= 0) requested.OverLordQty = requested.Qty;

        if (requested.Qty <= 0) return;

        orderItemRepository.UpdateOverLoadQty(requested);

        if (requested.Qty.Equals(current.Qty)) return;

        double delta = requested.Qty - current.Qty;

        if (delta > 0) {
            List<ProductAvailability> availabilities = ReadPurchasableStock(connection, context, current.ProductId);
            double sellableQuantity = SellableQuantity(availabilities);

            if (sellableQuantity < delta) {
                requested.Qty -= delta - sellableQuantity;
                delta = sellableQuantity;
            }

            if (delta > 0) Reserve(stock, current.Id, availabilities, delta);
        } else {
            Release(stock, current.Id, -delta);
        }

        if (!stock.SetItemQty(current.Id, cart.Id, current.Qty, requested.Qty))
            throw new CheckoutConflictException(CheckoutConflictException.CartStockChangedMessage);

        current.Qty = requested.Qty;

        orderItemRepository.UpdateOverLoadQty(requested);
        stock.ExtendCart(cart.Id, DateTime.Now.Date);
        effects.StockChanged(current.ProductId, current.Product?.NetUid ?? Guid.Empty);
    }

    private bool RemoveFromCart(
        IDbConnection connection,
        IShoppingCartStockRepository stock,
        OpenCart cart,
        OrderItem item,
        CartWriteEffects effects) {
        if (!_saleRepositoriesFactory
                .NewOrderItemRepository(connection)
                .RemoveFromShoppingCart(item.NetUid, cart.Id))
            return false;

        stock.ReleaseAll(item.Id);
        effects.StockChanged(item.ProductId, item.Product?.NetUid ?? Guid.Empty);

        return true;
    }

    private static void Reserve(
        IShoppingCartStockRepository stock,
        long orderItemId,
        IEnumerable<ProductAvailability> availabilities,
        double qty) {
        double remaining = qty;

        foreach (ProductAvailability availability in availabilities.Where(candidate => candidate.Amount > 0)) {
            if (remaining <= QuantityTolerance) break;

            double portion = Math.Min(availability.Amount, remaining);

            if (!stock.Reserve(orderItemId, availability.Id, portion))
                throw new CheckoutConflictException(CheckoutConflictException.CartStockChangedMessage);

            availability.Amount -= portion;
            remaining -= portion;
        }

        if (remaining > QuantityTolerance)
            throw new CheckoutConflictException(CheckoutConflictException.CartStockChangedMessage);
    }

    private static void Release(IShoppingCartStockRepository stock, long orderItemId, double qty) {
        double remaining = qty;

        foreach (ShoppingCartReservation reservation in stock.GetReservations(orderItemId)) {
            if (remaining <= QuantityTolerance) break;

            double portion = Math.Min(reservation.Qty, remaining);
            if (portion <= 0d) continue;

            if (!stock.Release(reservation, portion))
                throw new CheckoutConflictException(CheckoutConflictException.CartStockChangedMessage);

            remaining -= portion;
        }
    }

    private OrderItem LoadCartLine(IDbConnection connection, long orderItemId, ClientAgreement clientAgreement) {
        return NormalizeOverLordQty(
            _saleRepositoriesFactory
                .NewOrderItemRepository(connection)
                .GetByIdAndClientAgreementNetIdWithIncludes(
                    orderItemId,
                    clientAgreement.NetUid,
                    clientAgreement.Agreement.CurrencyId ??
                    throw new InvalidOperationException("Client agreement currency is not configured.")));
    }

    private static void ValidateRequestedQuantity(OrderItem orderItem) {
        if (!double.IsFinite(orderItem.Qty) || orderItem.Qty > 100000 ||
            !double.IsFinite(orderItem.OverLordQty) || orderItem.OverLordQty > 100000)
            throw new ArgumentException("Order item quantity is invalid.");
    }

    private void QueueAvailabilitySync(Guid productNetId) {
        CultureInfo culture = CultureInfo.CurrentCulture;

        BackgroundSyncRunner.Run(async cancellationToken => {
            string configPath = NoltFolderManager.GetEcommerceCrmConfigJsonFilePath();
            if (!File.Exists(configPath))
                throw new InvalidOperationException("CRM endpoint is not configured.");

            EcommerceCrmConfig data = JsonSerializer.Deserialize<EcommerceCrmConfig>(
                File.ReadAllText(configPath),
                _jsonSerializerOptions);

            string availabilitySyncUrl =
                $"{data?.CrmServerUrl}/api/v1/{culture}/products/sync/availability?netId={productNetId}";

            using HttpClient httpClient = _httpClientFactory.CreateClient(
                EcommerceInternalHttpClientDefaults.ClientName);
            await httpClient.PostAsync(availabilitySyncUrl, null, cancellationToken);
        }, "Cart availability sync");
    }

    private readonly record struct CartContext(ClientAgreement ClientAgreement, Workplace Workplace);

    private sealed record OpenCart(long Id, List<OrderItem> Items);

    private sealed class CartWriteEffects {
        private readonly HashSet<Guid> _availabilitySyncProductNetIds = new();
        private readonly HashSet<long> _reindexProductIds = new();

        public IReadOnlyCollection<Guid> AvailabilitySyncProductNetIds => _availabilitySyncProductNetIds;

        public IReadOnlyCollection<long> ReindexProductIds => _reindexProductIds;

        public void Reindex(long productId) {
            if (productId > 0) _reindexProductIds.Add(productId);
        }

        public void StockChanged(long productId, Guid productNetId) {
            Reindex(productId);
            if (productNetId != Guid.Empty) _availabilitySyncProductNetIds.Add(productNetId);
        }
    }
}
