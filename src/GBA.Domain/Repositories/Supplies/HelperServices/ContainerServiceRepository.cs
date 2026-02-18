using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Dapper;
using GBA.Common.Helpers;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Supplies;
using GBA.Domain.Entities.Supplies.Documents;
using GBA.Domain.Entities.Supplies.HelperServices;
using GBA.Domain.Entities.Supplies.PackingLists;
using GBA.Domain.Repositories.Supplies.Contracts;

namespace GBA.Domain.Repositories.Supplies.HelperServices;

public sealed class ContainerServiceRepository : IContainerServiceRepository {
    private readonly IDbConnection _connection;

    public ContainerServiceRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(ContainerService containerService) {
        return _connection.Query<long>(
                "INSERT INTO ContainerService (IsCalculatedExtraCharge, SupplyExtraChargeType, GroosWeight, FromDate, ContainerOrganizationID, TermDeliveryInDays, " +
                "IsActive, BillOfLadingDocumentID, UserId, SupplyPaymentTaskId, LoadDate, GrossPrice, NetPrice, Vat, Number, Name, VatPercent, ContainerNumber, ServiceNumber, " +
                "SupplyOrganizationAgreementId, Updated, AccountingGrossPrice, AccountingNetPrice, AccountingVat, AccountingPaymentTaskId, AccountingVatPercent, " +
                "[AccountingSupplyCostsWithinCountry], [SupplyInformationTaskID], [ExchangeRate], [AccountingExchangeRate], [IsIncludeAccountingValue], " +
                "[ActProvidingServiceDocumentID], [SupplyServiceAccountDocumentID]) " +
                "VALUES(@IsCalculatedExtraCharge, @SupplyExtraChargeType, @GroosWeight, @FromDate, @ContainerOrganizationID, @TermDeliveryInDays, @IsActive, " +
                "@BillOfLadingDocumentID, @UserId, @SupplyPaymentTaskId, @LoadDate, @GrossPrice, @NetPrice, @Vat, @Number, @Name, @VatPercent, " +
                "@ContainerNumber, @ServiceNumber, @SupplyOrganizationAgreementId, getutcdate(), @AccountingGrossPrice, @AccountingNetPrice, " +
                "@AccountingVat, @AccountingPaymentTaskId, @AccountingVatPercent, @AccountingSupplyCostsWithinCountry, @SupplyInformationTaskId, " +
                "@ExchangeRate, @AccountingExchangeRate, @IsIncludeAccountingValue, @ActProvidingServiceDocumentId, @SupplyServiceAccountDocumentId); " +
                "SELECT SCOPE_IDENTITY()",
                containerService
            )
            .Single();
    }

    public void Add(IEnumerable<ContainerService> containerServices) {
        _connection.Execute(
            "INSERT INTO ContainerService (" +
            "IsCalculatedExtraCharge " +
            ", SupplyExtraChargeType " +
            ", GroosWeight " +
            ", FromDate " +
            ", ContainerOrganizationID " +
            ", TermDeliveryInDays " +
            ", IsActive " +
            ", BillOfLadingDocumentID " +
            ", UserId, SupplyPaymentTaskId " +
            ", LoadDate " +
            ", GrossPrice " +
            ", NetPrice " +
            ", Vat " +
            ", Number " +
            ", Name " +
            ", VatPercent " +
            ", ContainerNumber " +
            ", ServiceNumber " +
            ", Updated " +
            ", AccountingGrossPrice " +
            ", AccountingNetPrice " +
            ", AccountingVat " +
            ", AccountingPaymentTaskId " +
            ", AccountingVatPercent " +
            ", [AccountingSupplyCostsWithinCountry] " +
            ", [SupplyInformationTaskID] " +
            ", [ExchangeRate] " +
            ", [AccountingExchangeRate]" +
            ", [IsIncludeAccountingValue]" +
            ", [ActProvidingServiceDocumentID] " +
            ", [SupplyServiceAccountDocumentID]) " +
            "VALUES(" +
            "@IsCalculatedExtraCharge " +
            ", @SupplyExtraChargeType " +
            ", @GroosWeight " +
            ", @FromDate " +
            ", @ContainerOrganizationID " +
            ", @TermDeliveryInDays " +
            ", @IsActive " +
            ", @BillOfLadingDocumentID " +
            ", @UserId " +
            ", @SupplyPaymentTaskId " +
            ", @LoadDate " +
            ", @GrossPrice " +
            ", @NetPrice " +
            ", @Vat " +
            ", @Number " +
            ", @Name " +
            ", @VatPercent " +
            ", @ContainerNumber " +
            ", @ServiceNumber " +
            ", getutcdate() " +
            ", @AccountingGrossPrice " +
            ", @AccountingNetPrice " +
            ", @AccountingVat " +
            " @AccountingPaymentTaskId " +
            ", @AccountingVatPercent " +
            ", @AccountingSupplyCostsWithinCountry " +
            ", @SupplyInformationTaskId " +
            ", @ExchangeRate " +
            ", @AccountingExchangeRate" +
            ", @IsIncludeAccountingValue" +
            ", @ActProvidingServiceDocumentId " +
            ", @SupplyServiceAccountDocumentId)",
            containerServices
        );
    }

    public Guid GetSupplyOrderNetIdBySupplyOrderContainerServiceNetId(Guid netId) {
        return _connection.Query<Guid>(
                "SELECT [SupplyOrder].NetUID " +
                "FROM [SupplyOrderContainerService] " +
                "LEFT JOIN [SupplyOrder] " +
                "ON [SupplyOrder].ID = [SupplyOrderContainerService].SupplyOrderID " +
                "WHERE [SupplyOrderContainerService].NetUID = @NetId",
                new { NetId = netId }
            )
            .SingleOrDefault();
    }

    public List<ContainerService> GetAllFromSearch(string value, long limit, long offset, DateTime from, DateTime to, Guid? clientNetId) {
        List<ContainerService> toReturn = new();

        string sqlExpression =
            ";WITH [Search_CTE] " +
            "AS " +
            "( " +
            "SELECT ROW_NUMBER() OVER (ORDER BY ID) AS RowNumber " +
            ",ID " +
            "FROM " +
            "( " +
            "SELECT DISTINCT [ContainerService].ID " +
            "FROM [ContainerService] " +
            "LEFT JOIN [BillOfLadingDocument] " +
            "ON [BillOfLadingDocument].ID = [ContainerService].BillOfLadingDocumentID " +
            "LEFT JOIN [SupplyOrderContainerService] " +
            "ON [SupplyOrderContainerService].ContainerServiceID = [ContainerService].ID " +
            "AND [SupplyOrderContainerService].Deleted = 0 " +
            "LEFT JOIN [SupplyOrder] " +
            "ON [SupplyOrder].ID = [SupplyOrderContainerService].SupplyOrderID " +
            "LEFT JOIN [Client] " +
            "ON [SupplyOrder].ClientID = [Client].ID " +
            "WHERE [ContainerService].Deleted = 0 " +
            "AND [ContainerService].Created >= @From " +
            "AND [ContainerService].Created <= @To " +
            "AND [BillOfLadingDocument].Number like '%' + @Value + '%' ";

        if (clientNetId.HasValue) sqlExpression += "AND [Client].NetUID = @ClientNetId ";

        sqlExpression +=
            ") [Distincts] " +
            ") " +
            "SELECT * " +
            "FROM [ContainerService] " +
            "LEFT JOIN [BillOfLadingDocument] " +
            "ON [BillOfLadingDocument].ID = [ContainerService].BillOfLadingDocumentID " +
            "LEFT JOIN [SupplyOrganization] AS [ContainerOrganization] " +
            "ON [ContainerOrganization].ID = [ContainerService].ContainerOrganizationID " +
            "LEFT JOIN [SupplyPaymentTask] " +
            "ON [SupplyPaymentTask].ID = [ContainerService].SupplyPaymentTaskID " +
            "LEFT JOIN [SupplyPaymentTask] AS [AccountingPaymentTask]" +
            "ON [AccountingPaymentTask].ID = [ContainerService].AccountingPaymentTaskID " +
            "LEFT JOIN [InvoiceDocument] " +
            "ON [ContainerService].ID = [InvoiceDocument].ContainerServiceID " +
            "AND [InvoiceDocument].Deleted = 0 " +
            "LEFT JOIN [PackingList] " +
            "ON [PackingList].ContainerServiceID = [ContainerService].ID " +
            "AND [PackingList].Deleted = 0 " +
            "LEFT JOIN [SupplyInvoice] " +
            "ON [SupplyInvoice].ID = [PackingList].SupplyInvoiceID " +
            "LEFT JOIN [SupplyOrder] " +
            "ON [SupplyOrder].ID = [SupplyInvoice].SupplyOrderID " +
            "LEFT JOIN [SupplyOrderNumber] " +
            "ON [SupplyOrderNumber].ID = [SupplyOrder].ID " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [SupplyOrder].ClientID " +
            "WHERE [ContainerService].ID IN ( " +
            "SELECT [Search_CTE].ID " +
            "FROM [Search_CTE] " +
            "WHERE [Search_CTE].RowNumber > @Offset " +
            "AND [Search_CTE].RowNumber <= @Limit + @Offset " +
            ")";

        Type[] types = {
            typeof(ContainerService),
            typeof(BillOfLadingDocument),
            typeof(SupplyOrganization),
            typeof(SupplyPaymentTask),
            typeof(SupplyPaymentTask),
            typeof(InvoiceDocument),
            typeof(PackingList),
            typeof(SupplyInvoice),
            typeof(SupplyOrder),
            typeof(SupplyOrderNumber),
            typeof(Client)
        };

        Func<object[], ContainerService> mapper = objects => {
            ContainerService containerService = (ContainerService)objects[0];
            BillOfLadingDocument billOfLadingDocument = (BillOfLadingDocument)objects[1];
            SupplyOrganization containerOrganization = (SupplyOrganization)objects[2];
            SupplyPaymentTask supplyPaymentTask = (SupplyPaymentTask)objects[3];
            SupplyPaymentTask accountingPaymentTask = (SupplyPaymentTask)objects[4];
            InvoiceDocument invoiceDocument = (InvoiceDocument)objects[5];
            PackingList packingList = (PackingList)objects[6];
            SupplyInvoice supplyInvoice = (SupplyInvoice)objects[7];
            SupplyOrder supplyOrder = (SupplyOrder)objects[8];
            SupplyOrderNumber supplyOrderNumber = (SupplyOrderNumber)objects[9];
            Client client = (Client)objects[10];

            if (!toReturn.Any(s => s.Id.Equals(containerService.Id))) {
                if (invoiceDocument != null) containerService.InvoiceDocuments.Add(invoiceDocument);

                if (packingList != null) {
                    supplyOrder.Client = client;
                    supplyOrder.SupplyOrderNumber = supplyOrderNumber;

                    supplyInvoice.SupplyOrder = supplyOrder;

                    packingList.SupplyInvoice = supplyInvoice;

                    containerService.PackingLists.Add(packingList);
                }

                containerService.ContainerOrganization = containerOrganization;
                containerService.BillOfLadingDocument = billOfLadingDocument;
                containerService.SupplyPaymentTask = supplyPaymentTask;

                if (accountingPaymentTask != null)
                    containerService.AccountingPaymentTask = accountingPaymentTask;

                toReturn.Add(containerService);
            } else {
                ContainerService fromList = toReturn.First(s => s.Id.Equals(containerService.Id));

                if (invoiceDocument != null && !fromList.InvoiceDocuments.Any(d => d.Id.Equals(invoiceDocument.Id))) fromList.InvoiceDocuments.Add(invoiceDocument);

                if (packingList != null && !fromList.PackingLists.Any(d => d.Id.Equals(packingList.Id))) {
                    supplyOrder.Client = client;
                    supplyOrder.SupplyOrderNumber = supplyOrderNumber;

                    supplyInvoice.SupplyOrder = supplyOrder;

                    packingList.SupplyInvoice = supplyInvoice;

                    fromList.PackingLists.Add(packingList);
                }
            }

            return containerService;
        };

        var props = new { Value = value, Limit = limit, Offset = offset, From = from, To = to, ClientNetId = clientNetId ?? Guid.Empty };

        _connection.Query(
            sqlExpression,
            types,
            mapper,
            props
        );

        return toReturn;
    }

    public List<ContainerService> GetAllRanged(DateTime from, DateTime to) {
        List<ContainerService> containerServicesToReturn = new();

        _connection.Query<ContainerService, User, BillOfLadingDocument, SupplyPaymentTask, User, SupplyOrganization, ContainerService>(
                "SELECT * FROM ContainerService " +
                "LEFT JOIN [User] AS [ContainerServiceUser] " +
                "ON [ContainerServiceUser].ID = ContainerService.UserID " +
                "LEFT JOIN BillOfLadingDocument " +
                "ON BillOfLadingDocument.ID = ContainerService.BillOfLadingDocumentID " +
                "LEFT JOIN SupplyPaymentTask " +
                "ON SupplyPaymentTask.ID = ContainerService.SupplyPaymentTaskID " +
                "LEFT JOIN [User] AS [ContainerServiceSupplyPaymentTaskUser] " +
                "ON [ContainerServiceSupplyPaymentTaskUser].ID = SupplyPaymentTask.UserID " +
                "LEFT JOIN [SupplyOrganization] AS [ContainerOrganization] " +
                "ON [ContainerOrganization].ID = [ContainerService].ContainerOrganizationID " +
                "WHERE ContainerService.LoadDate >= @From " +
                "AND ContainerService.LoadDate <= @To " +
                "AND ContainerService.Deleted = 0",
                (containerService, containerServiceUser, billOfLadingDocument, supplyPaymentTask, supplyPaymentTaskUser, organization) => {
                    if (supplyPaymentTask != null) {
                        supplyPaymentTask.User = supplyPaymentTaskUser;
                        containerService.SupplyPaymentTask = supplyPaymentTask;
                    }

                    containerService.User = containerServiceUser;
                    containerService.BillOfLadingDocument = billOfLadingDocument;
                    containerService.ContainerOrganization = organization;

                    if (!containerServicesToReturn.Any(c => c.Id.Equals(containerService.Id))) containerServicesToReturn.Add(containerService);

                    return containerService;
                },
                new { From = from, To = to }
            )
            .ToList();

        return containerServicesToReturn;
    }

    public List<ContainerService> GetAllAvailable() {
        List<ContainerService> toReturn = new();

        Type[] types = {
            typeof(ContainerService),
            typeof(SupplyOrder),
            typeof(PackingList),
            typeof(SupplyInvoice),
            typeof(SupplyOrganization),
            typeof(BillOfLadingDocument),
            typeof(SupplyOrganizationAgreement),
            typeof(Client),
            typeof(SupplyOrderContainerService)
        };

        Func<object[], ContainerService> mapper = objects => {
            ContainerService service = (ContainerService)objects[0];
            SupplyOrder supplyOrder = (SupplyOrder)objects[1];
            PackingList packingList = (PackingList)objects[2];
            SupplyInvoice invoice = (SupplyInvoice)objects[3];
            SupplyOrganization organization = (SupplyOrganization)objects[4];
            BillOfLadingDocument billOfLadingDocument = (BillOfLadingDocument)objects[5];
            SupplyOrganizationAgreement supplyOrganizationAgreement = (SupplyOrganizationAgreement)objects[6];
            Client client = (Client)objects[7];
            SupplyOrderContainerService supplyOrderContainerService = (SupplyOrderContainerService)objects[8];

            if (service != null && !service.IsNew()) {
                if (toReturn.Any(s => s.Id.Equals(service.Id))) {
                    if (packingList != null) {
                        ContainerService fromList = toReturn.First(s => s.Id.Equals(service.Id));

                        if (!fromList.PackingLists.Any(p => p.Id.Equals(packingList.Id))) {
                            packingList.SupplyInvoice = invoice;

                            fromList.PackingLists.Add(packingList);
                        }
                    }

                    supplyOrder.Client = client;

                    supplyOrderContainerService.SupplyOrder = supplyOrder;

                    if (!service.SupplyOrderContainerServices.Any(x => x.Id.Equals(supplyOrderContainerService.Id)))
                        service.SupplyOrderContainerServices.Add(supplyOrderContainerService);
                    else
                        supplyOrderContainerService = service.SupplyOrderContainerServices.FirstOrDefault(x => x.Id.Equals(supplyOrderContainerService.Id));

                    service.SupplyOrganizationAgreement = supplyOrganizationAgreement;
                } else {
                    if (packingList != null) {
                        packingList.SupplyInvoice = invoice;

                        service.PackingLists.Add(packingList);
                    }

                    supplyOrder.Client = client;

                    supplyOrderContainerService.SupplyOrder = supplyOrder;

                    if (!service.SupplyOrderContainerServices.Any(x => x.Id.Equals(supplyOrderContainerService.Id)))
                        service.SupplyOrderContainerServices.Add(supplyOrderContainerService);
                    else
                        supplyOrderContainerService = service.SupplyOrderContainerServices.FirstOrDefault(x => x.Id.Equals(supplyOrderContainerService.Id));

                    service.ContainerOrganization = organization;
                    service.BillOfLadingDocument = billOfLadingDocument;

                    service.SupplyOrganizationAgreement = supplyOrganizationAgreement;

                    toReturn.Add(service);
                }
            }

            return service;
        };

        _connection.Query("SELECT " +
                          "[ContainerService].* " +
                          ",[SupplyOrder].* " +
                          ",[PackingList].* " +
                          ",[SupplyInvoice].* " +
                          ",[ContainerOrganization].* " +
                          ",[BillOfLadingDocument].* " +
                          ",[SupplyOrganizationAgreement].* " +
                          ",[Client].* " +
                          ",[SupplyOrderContainerService].* " +
                          "FROM [SupplyOrderContainerService] " +
                          "LEFT JOIN [SupplyOrder] " +
                          "ON [SupplyOrder].ID = [SupplyOrderContainerService].SupplyOrderID " +
                          "LEFT JOIN [ContainerService] " +
                          "ON [ContainerService].ID = [SupplyOrderContainerService].ContainerServiceID " +
                          "LEFT JOIN [PackingList] " +
                          "ON [PackingList].ContainerServiceID = [ContainerService].ID " +
                          "AND [PackingList].Deleted = 0 " +
                          "LEFT JOIN [SupplyInvoice] " +
                          "ON [SupplyInvoice].ID = [PackingList].SupplyInvoiceID " +
                          "LEFT JOIN [SupplyOrganization] AS [ContainerOrganization] " +
                          "ON [ContainerOrganization].ID = [ContainerService].ContainerOrganizationID " +
                          "LEFT JOIN [BillOfLadingDocument] " +
                          "ON [BillOfLadingDocument].ID = [ContainerService].BillOfLadingDocumentID " +
                          "LEFT JOIN [SupplyOrganizationAgreement] " +
                          "ON [SupplyOrganizationAgreement].[ID] = [ContainerService].[SupplyOrganizationAgreementID] " +
                          "LEFT JOIN [Client] " +
                          "ON [Client].[ID] = [SupplyOrder].[ClientID] " +
                          "WHERE [SupplyOrderContainerService].Deleted = 0 " +
                          "AND [SupplyOrder].IsOrderShipped = 0", types, mapper);

        return toReturn;
    }

    public ContainerService GetById(long id) {
        ContainerService containerServiceToReturn = null;

        string sqlExpression = "SELECT * FROM ContainerService " +
                               "LEFT OUTER JOIN [SupplyOrganization] AS ContainerOrganization " +
                               "ON ContainerOrganization.ID = ContainerService.ContainerOrganizationID " +
                               "LEFT OUTER JOIN BillOfLadingDocument " +
                               "ON BillOfLadingDocument.ID = ContainerService.BillOfLadingDocumentID AND BillOfLadingDocument.Deleted = 0 " +
                               "LEFT OUTER JOIN [User] AS [ContainerService.User] " +
                               "ON [ContainerService.User].ID = ContainerService.UserID " +
                               "LEFT OUTER JOIN SupplyPaymentTask " +
                               "ON SupplyPaymentTask.ID = ContainerService.SupplyPaymentTaskID AND SupplyPaymentTask.Deleted = 0 " +
                               "LEFT OUTER JOIN [User] AS [SupplyPaymentTask.User]" +
                               "ON [SupplyPaymentTask.User].ID = SupplyPaymentTask.UserID " +
                               "LEFT OUTER JOIN InvoiceDocument " +
                               "ON InvoiceDocument.ContainerServiceID = ContainerService.ID AND InvoiceDocument.Deleted = 0 " +
                               "LEFT OUTER JOIN SupplyPaymentTask AS [AccountingPaymentTask] " +
                               "ON AccountingPaymentTask.ID = ContainerService.AccountingPaymentTaskID AND AccountingPaymentTask.Deleted = 0 " +
                               "LEFT OUTER JOIN [User] AS [AccountingPaymentTaskUser]" +
                               "ON [AccountingPaymentTaskUser].ID = AccountingPaymentTask.UserID " +
                               "WHERE ContainerService.ID = @Id ";

        Type[] types = {
            typeof(ContainerService),
            typeof(SupplyOrganization),
            typeof(BillOfLadingDocument),
            typeof(User),
            typeof(SupplyPaymentTask),
            typeof(User),
            typeof(InvoiceDocument),
            typeof(SupplyPaymentTask),
            typeof(User)
        };

        Func<object[], ContainerService> mapper = objects => {
            ContainerService containerService = (ContainerService)objects[0];
            SupplyOrganization containerOrganization = (SupplyOrganization)objects[1];
            BillOfLadingDocument billOfLadingDocument = (BillOfLadingDocument)objects[2];
            User containerServiceUser = (User)objects[3];
            SupplyPaymentTask paymentTask = (SupplyPaymentTask)objects[4];
            User paymentTaskUser = (User)objects[5];
            InvoiceDocument invoiceDocument = (InvoiceDocument)objects[6];
            SupplyPaymentTask accountingPaymentTask = (SupplyPaymentTask)objects[7];
            User accountingPaymentTaskUser = (User)objects[8];

            if (containerServiceUser != null) containerService.User = containerServiceUser;

            if (billOfLadingDocument != null) containerService.BillOfLadingDocument = billOfLadingDocument;

            if (containerOrganization != null) containerService.ContainerOrganization = containerOrganization;

            if (paymentTask != null) {
                paymentTask.User = paymentTaskUser;
                containerService.SupplyPaymentTask = paymentTask;
            }

            if (accountingPaymentTask != null) {
                accountingPaymentTask.User = accountingPaymentTaskUser;
                containerService.AccountingPaymentTask = accountingPaymentTask;
            }

            if (invoiceDocument != null) containerService.InvoiceDocuments.Add(invoiceDocument);

            if (containerServiceToReturn != null) {
                if (invoiceDocument != null && !containerServiceToReturn.InvoiceDocuments.Any(d => d.Id.Equals(invoiceDocument.Id)))
                    containerServiceToReturn.InvoiceDocuments.Add(invoiceDocument);
            } else {
                containerServiceToReturn = containerService;
            }

            return containerService;
        };

        var props = new { Id = id };

        _connection.Query(sqlExpression, types, mapper, props);

        return containerServiceToReturn;
    }

    public ContainerService GetBySupplyOrderContainerServiceNetIdWithoutIncludes(Guid netId) {
        return _connection.Query<ContainerService>(
                "SELECT [ContainerService].* " +
                "FROM [SupplyOrderContainerService] " +
                "LEFT JOIN [ContainerService] " +
                "ON [ContainerService].ID = [SupplyOrderContainerService].ContainerServiceID " +
                "WHERE [SupplyOrderContainerService].NetUID = @NetId",
                new { NetId = netId }
            )
            .SingleOrDefault();
    }

    public ContainerService GetByNetId(Guid netId) {
        ContainerService containerServiceToReturn = null;

        string sqlExpression = "SELECT * FROM ContainerService " +
                               "LEFT OUTER JOIN [SupplyOrganization] AS ContainerOrganization " +
                               "ON ContainerOrganization.ID = ContainerService.ContainerOrganizationID " +
                               "LEFT OUTER JOIN BillOfLadingDocument " +
                               "ON BillOfLadingDocument.ID = ContainerService.BillOfLadingDocumentID AND BillOfLadingDocument.Deleted = 0 " +
                               "LEFT OUTER JOIN [User] AS [ContainerService.User] " +
                               "ON [ContainerService.User].ID = ContainerService.UserID " +
                               "LEFT OUTER JOIN SupplyPaymentTask " +
                               "ON SupplyPaymentTask.ID = ContainerService.SupplyPaymentTaskID AND SupplyPaymentTask.Deleted = 0 " +
                               "LEFT OUTER JOIN [User] AS [SupplyPaymentTask.User]" +
                               "ON [SupplyPaymentTask.User].ID = SupplyPaymentTask.UserID " +
                               "LEFT OUTER JOIN InvoiceDocument " +
                               "ON InvoiceDocument.ContainerServiceID = ContainerService.ID AND InvoiceDocument.Deleted = 0 " +
                               "LEFT OUTER JOIN SupplyPaymentTask AS [AccountingPaymentTask]" +
                               "ON AccountingPaymentTask.ID = ContainerService.AccountingPaymentTaskID AND AccountingPaymentTask.Deleted = 0 " +
                               "LEFT OUTER JOIN [User] AS [SupplyAccountingPaymentTaskUser]" +
                               "ON [SupplyAccountingPaymentTaskUser].ID = AccountingPaymentTask.UserID " +
                               "WHERE ContainerService.NetUID = @NetId ";

        Type[] types = {
            typeof(ContainerService),
            typeof(SupplyOrganization),
            typeof(BillOfLadingDocument),
            typeof(User),
            typeof(SupplyPaymentTask),
            typeof(User),
            typeof(InvoiceDocument),
            typeof(SupplyPaymentTask),
            typeof(User)
        };

        Func<object[], ContainerService> mapper = objects => {
            ContainerService containerService = (ContainerService)objects[0];
            SupplyOrganization containerOrganization = (SupplyOrganization)objects[1];
            BillOfLadingDocument billOfLadingDocument = (BillOfLadingDocument)objects[2];
            User containerServiceUser = (User)objects[3];
            SupplyPaymentTask paymentTask = (SupplyPaymentTask)objects[4];
            User paymentTaskUser = (User)objects[5];
            InvoiceDocument invoiceDocument = (InvoiceDocument)objects[6];
            SupplyPaymentTask accountingPaymentTask = (SupplyPaymentTask)objects[7];
            User accountingPaymentTaskUser = (User)objects[8];

            if (containerServiceUser != null) containerService.User = containerServiceUser;

            if (billOfLadingDocument != null) containerService.BillOfLadingDocument = billOfLadingDocument;

            if (containerOrganization != null) containerService.ContainerOrganization = containerOrganization;

            if (paymentTask != null) {
                paymentTask.User = paymentTaskUser;
                containerService.SupplyPaymentTask = paymentTask;
            }

            if (accountingPaymentTask != null) {
                accountingPaymentTask.User = accountingPaymentTaskUser;
                containerService.AccountingPaymentTask = accountingPaymentTask;
            }

            if (containerServiceToReturn != null) {
                if (invoiceDocument != null && !containerServiceToReturn.InvoiceDocuments.Any(d => d.Id.Equals(invoiceDocument.Id)))
                    containerServiceToReturn.InvoiceDocuments.Add(invoiceDocument);
            } else {
                if (invoiceDocument != null) containerService.InvoiceDocuments.Add(invoiceDocument);

                containerServiceToReturn = containerService;
            }

            return containerService;
        };

        var props = new { NetId = netId };

        _connection.Query(sqlExpression, types, mapper, props);

        return containerServiceToReturn;
    }

    public void UpdateDeliveryTerms(long id, string days) {
        _connection.Execute(
            "UPDATE ContainerService SET TermDeliveryInDays = @Days WHERE ID = @Id",
            new { Id = id, Days = days }
        );
    }

    public string GetTermDeliveryInDaysById(long id) {
        return _connection.Query<string>(
                "SELECT TermDeliveryInDays FROM ContainerService WHERE ID = @Id",
                new { Id = id }
            )
            .SingleOrDefault();
    }

    public void Update(ContainerService containerService) {
        _connection.Execute(
            "UPDATE ContainerService " +
            "SET IsCalculatedExtraCharge = @IsCalculatedExtraCharge, SupplyExtraChargeType = @SupplyExtraChargeType, GroosWeight = @GroosWeight, FromDate = @FromDate, " +
            "ContainerOrganizationID = @ContainerOrganizationID, TermDeliveryInDays = @TermDeliveryInDays, IsActive = @IsActive, ContainerNumber = @ContainerNumber, " +
            "BillOfLadingDocumentID = @BillOfLadingDocumentID, LoadDate = @LoadDate, UserId = @UserId, SupplyPaymentTaskId = @SupplyPaymentTaskId, GrossPrice = @GrossPrice, " +
            "NetPrice = @NetPrice, Vat = @Vat, Number = @Number, Name = @Name, VatPercent = @VatPercent, Updated = getutcdate(), " +
            "AccountingGrossPrice = @AccountingGrossPrice,  AccountingNetPrice = @AccountingNetPrice, AccountingVat = @AccountingVat, AccountingPaymentTaskId = @AccountingPaymentTaskId, " +
            "AccountingVatPercent = @AccountingVatPercent, SupplyOrganizationAgreementId = @SupplyOrganizationAgreementId, " +
            "[AccountingSupplyCostsWithinCountry] = @AccountingSupplyCostsWithinCountry, [SupplyInformationTaskID] = @SupplyInformationTaskId, " +
            "[ExchangeRate] = @ExchangeRate, [AccountingExchangeRate] = @AccountingExchangeRate, [IsIncludeAccountingValue] = @IsIncludeAccountingValue, " +
            "[ActProvidingServiceDocumentID] = @ActProvidingServiceDocumentId, [SupplyServiceAccountDocumentID] = @SupplyServiceAccountDocumentId " +
            "WHERE NetUID = @NetUID",
            containerService
        );
    }

    public void SetIsExtraChargeCalculatedByNetId(Guid netId, SupplyExtraChargeType extraChargeType) {
        _connection.Execute(
            "UPDATE ContainerService " +
            "SET IsCalculatedExtraCharge = 1, Updated = getutcdate(), SupplyExtraChargeType = @SupplyExtraChargeType " +
            "WHERE ID = (SELECT [SupplyOrderContainerService].ContainerServiceID FROM [SupplyOrderContainerService] WHERE [SupplyOrderContainerService].NetUID = @NetId)",
            new { NetId = netId, SupplyExtraChargeType = extraChargeType }
        );
    }

    public void Update(IEnumerable<ContainerService> containerServices) {
        _connection.Execute(
            "UPDATE ContainerService " +
            "SET IsCalculatedExtraCharge = @IsCalculatedExtraCharge, SupplyExtraChargeType = @SupplyExtraChargeType, GroosWeight = @GroosWeight, FromDate = @FromDate, ContainerOrganizationID = @ContainerOrganizationID, TermDeliveryInDays = @TermDeliveryInDays, IsActive = @IsActive, ContainerNumber = @ContainerNumber, " +
            "BillOfLadingDocumentID = @BillOfLadingDocumentID, LoadDate = @LoadDate, UserId = @UserId, SupplyPaymentTaskId = @SupplyPaymentTaskId, GrossPrice = @GrossPrice, NetPrice = @NetPrice, Vat = @Vat, Number = @Number, Name = @Name, VatPercent = @VatPercent, Updated = getutcdate(), " +
            "AccountingGrossPrice = @AccountingGrossPrice, AccountingNetPrice = @AccountingNetPrice, AccountingVat = @AccountingVat, AccountingPaymentTaskId = @AccountingPaymentTaskId, " +
            "AccountingVatPercent = @AccountingVatPercent, SupplyOrganizationAgreementId = @SupplyOrganizationAgreementId, " +
            "[AccountingSupplyCostsWithinCountry] = @AccountingSupplyCostsWithinCountry, [SupplyInformationTaskID] = @SupplyInformationTaskId, " +
            "[ExchangeRate] = @ExchangeRate, [AccountingExchangeRate] = @AccountingExchangeRate, [IsIncludeAccountingValue] = @IsIncludeAccountingValue, " +
            "[ActProvidingServiceDocumentID] = @ActProvidingServiceDocumentId, [SupplyServiceAccountDocumentID] = @SupplyServiceAccountDocumentId " +
            "WHERE NetUID = @NetUID",
            containerServices
        );
    }

    public void UpdateSupplyPaymentTaskId(IEnumerable<ContainerService> containerServices) {
        _connection.Execute(
            "UPDATE [ContainerService] " +
            "SET SupplyPaymentTaskID = @SupplyPaymentTaskId " +
            "WHERE ID = @Id",
            containerServices
        );
    }

    public ContainerService GetByIdWithoutIncludes(long containerServiceId) {
        return _connection.Query<ContainerService>(
            "SELECT * FROM [ContainerService] " +
            "WHERE [ContainerService].[ID] = @Id",
            new { Id = containerServiceId }).FirstOrDefault();
    }

    public bool IsContainerInOrder(Guid supplyOrderNetId, Guid containerServiceNetId) {
        return _connection.Query<bool>(
            "SELECT " +
            "TOP(1) IIF([ContainerService].[ID] IS NULL, 0, 1) " +
            "FROM [SupplyOrder] " +
            "LEFT JOIN [SupplyOrderContainerService] " +
            "ON [SupplyOrderContainerService].[SupplyOrderID] = [SupplyOrder].[ID] " +
            "LEFT JOIN [ContainerService] " +
            "ON [ContainerService].[ID] = [SupplyOrderContainerService].[ContainerServiceID] " +
            "WHERE [ContainerService].[NetUID] = @ServiceNetId " +
            "AND [SupplyOrder].[NetUID] = @SupplyOrderNetId " +
            "AND [SupplyOrderContainerService].[Deleted] = 0 " +
            "AND [SupplyOrder].[Deleted] = 0",
            new {
                SupplyOrderNetId = supplyOrderNetId,
                ServiceNetId = containerServiceNetId
            }).Single();
    }

    public long GetIdByNetId(Guid containerServiceNetId) {
        return _connection.Query<long>(
            "SELECT [ContainerService].[ID] FROM [ContainerService] " +
            "WHERE [ContainerService].[NetUID] = @NetId ",
            new { NetId = containerServiceNetId }).Single();
    }

    public void Remove(long id) {
        _connection.Execute(
            "UPDATE [ContainerService] " +
            "SET [Deleted] = 1 " +
            ", [Updated] = getutcdate() " +
            "WHERE [ContainerService].[Id] = @Id; ",
            new { Id = id });
    }
}