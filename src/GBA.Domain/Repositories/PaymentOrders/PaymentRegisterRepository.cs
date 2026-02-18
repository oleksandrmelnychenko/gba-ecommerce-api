using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using Dapper;
using GBA.Domain.Entities;
using GBA.Domain.Entities.PaymentOrders;
using GBA.Domain.EntityHelpers.TotalDashboards.PaymentRegisters;
using GBA.Domain.Repositories.PaymentOrders.Contracts;

namespace GBA.Domain.Repositories.PaymentOrders;

public sealed class PaymentRegisterRepository : IPaymentRegisterRepository {
    private readonly IDbConnection _connection;

    public PaymentRegisterRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(PaymentRegister paymentRegister) {
        return _connection.Query<long>(
            "INSERT INTO [PaymentRegister] (Name, [Type], OrganizationId, AccountNumber, SortCode, IBAN, SwiftCode, BankName, City, FromDate, ToDate, IsActive, " +
            "Updated, [IsMain], IsForRetail, CVV, IsSelected) " +
            "VALUES (@Name, @Type, @OrganizationId, @AccountNumber, @SortCode, @IBAN, @SwiftCode, @BankName, @City, @FromDate, @ToDate, @IsActive, getutcdate(), " +
            "@IsMain, @IsForRetail, @CVV, @IsSelected); " +
            "SELECT SCOPE_IDENTITY()",
            paymentRegister
        ).Single();
    }

    public void Update(PaymentRegister paymentRegister) {
        _connection.Execute(
            "UPDATE [PaymentRegister] " +
            "SET Name = @Name, OrganizationID = @OrganizationId, AccountNumber = @AccountNumber, SortCode = @SortCode, IBAN = @IBAN, SwiftCode = @SwiftCode, " +
            "BankName = @BankName, City = @City, FromDate = @FromDate, ToDate = @ToDate, IsActive = @IsActive, Updated = getutcdate(), " +
            "[IsMain] = @IsMain, IsForRetail = @IsForRetail, CVV = @CVV, IsSelected = @IsSelected " +
            "WHERE [PaymentRegister].ID = @Id",
            paymentRegister
        );
    }

    public void SetInactiveByOrganizationAndCurrencyIds(long organizationId, long currencyId) {
        _connection.Execute(
            "UPDATE [PaymentRegister] " +
            "SET IsActive = 0 " +
            "FROM [PaymentRegister] " +
            "LEFT JOIN [PaymentCurrencyRegister] " +
            "ON [PaymentCurrencyRegister].PaymentRegisterID = [PaymentRegister].ID " +
            "AND [PaymentCurrencyRegister].Deleted = 0 " +
            "WHERE [PaymentRegister].OrganizationID = @OrganizationId " +
            "AND [PaymentCurrencyRegister].CurrencyID = @CurrencyId",
            new { OrganizationId = organizationId, CurrencyId = currencyId }
        );
    }

    public void SetActiveById(long id) {
        _connection.Execute(
            "UPDATE [PaymentRegister] " +
            "SET IsActive = 1, Updated = getutcdate() " +
            "WHERE [PaymentRegister].ID = @Id",
            new { Id = id }
        );
    }

    public void SetSelectedByNetId(Guid netId) {
        _connection.Execute(
            "UPDATE [PaymentRegister] " +
            "SET IsSelected = 1 " +
            "WHERE NetUID = @NetId",
            new { NetId = netId });
    }

    public void DeselectByNetId(Guid netId) {
        _connection.Execute(
            "UPDATE [PaymentRegister] " +
            "SET IsSelected = 0 " +
            "WHERE NetUID = @NetId ",
            new { NetId = netId });
    }

    public PaymentRegister GetIsSelected() {
        return _connection.Query<PaymentRegister>(
            "SELECT * FROM PaymentRegister " +
            "WHERE IsSelected = 1 " +
            "AND Deleted = 0 "
        ).FirstOrDefault();
    }

    public PaymentRegister GetById(long id) {
        PaymentRegister toReturn = null;

        _connection.Query<PaymentRegister, PaymentCurrencyRegister, Currency, Organization, PaymentRegister>(
            "SELECT * " +
            "FROM [PaymentRegister] " +
            "LEFT JOIN [PaymentCurrencyRegister] " +
            "ON [PaymentCurrencyRegister].PaymentRegisterID = [PaymentRegister].ID " +
            "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
            "ON [Currency].ID = [PaymentCurrencyRegister].CurrencyID " +
            "AND [Currency].CultureCode = @Culture " +
            "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
            "ON [Organization].ID = [PaymentRegister].OrganizationID " +
            "AND [Organization].CultureCode = @Culture " +
            "WHERE [PaymentRegister].ID = @Id",
            (register, registerCurrency, currency, organization) => {
                if (toReturn == null) {
                    if (registerCurrency != null) {
                        registerCurrency.Currency = currency;

                        register.PaymentCurrencyRegisters.Add(registerCurrency);
                    }

                    register.Organization = organization;

                    toReturn = register;
                } else if (registerCurrency != null) {
                    if (toReturn.PaymentCurrencyRegisters.Any(r => r.Id.Equals(registerCurrency.Id))) return register;

                    registerCurrency.Currency = currency;

                    toReturn.PaymentCurrencyRegisters.Add(registerCurrency);
                }

                return register;
            },
            new { Id = id, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
        );

        return toReturn;
    }

    public PaymentRegister GetByNetId(Guid netId) {
        PaymentRegister toReturn = null;

        _connection.Query<PaymentRegister, PaymentCurrencyRegister, Currency, Organization, PaymentRegister>(
            "SELECT * " +
            "FROM [PaymentRegister] " +
            "LEFT JOIN [PaymentCurrencyRegister] " +
            "ON [PaymentCurrencyRegister].PaymentRegisterID = [PaymentRegister].ID " +
            "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
            "ON [Currency].ID = [PaymentCurrencyRegister].CurrencyID " +
            "AND [Currency].CultureCode = @Culture " +
            "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
            "ON [Organization].ID = [PaymentRegister].OrganizationID " +
            "AND [Organization].CultureCode = @Culture " +
            "WHERE [PaymentRegister].NetUID = @NetId",
            (register, registerCurrency, currency, organization) => {
                if (toReturn == null) {
                    if (registerCurrency != null) {
                        registerCurrency.Currency = currency;

                        register.PaymentCurrencyRegisters.Add(registerCurrency);
                    }

                    register.Organization = organization;

                    toReturn = register;
                } else if (registerCurrency != null) {
                    if (toReturn.PaymentCurrencyRegisters.Any(r => r.Id.Equals(registerCurrency.Id))) return register;

                    registerCurrency.Currency = currency;

                    toReturn.PaymentCurrencyRegisters.Add(registerCurrency);
                }

                return register;
            },
            new { NetId = netId, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
        );

        return toReturn;
    }

    public PaymentRegister GetByNetIdWithoutIncludes(Guid netId) {
        return _connection.Query<PaymentRegister>(
            "SELECT * " +
            "FROM [PaymentRegister] " +
            "WHERE [PaymentRegister].NetUID = @NetId",
            new { NetId = netId }
        ).SingleOrDefault();
    }

    public PaymentRegister GetActiveBankAccountByCurrencyAndOrganizationIds(long currencyId, long organizationId) {
        return _connection.Query<PaymentRegister>(
            "SELECT TOP(1) [PaymentRegister].* " +
            "FROM [PaymentRegister] " +
            "LEFT JOIN [PaymentCurrencyRegister] " +
            "ON [PaymentCurrencyRegister].PaymentRegisterID = [PaymentRegister].ID " +
            "AND [PaymentCurrencyRegister].Deleted = 0 " +
            "WHERE [PaymentRegister].OrganizationID = @OrganizationId " +
            "AND [PaymentCurrencyRegister].CurrencyID = @CurrencyId " +
            "AND [PaymentRegister].Deleted = 0 " +
            "AND [PaymentRegister].[Type] = 2 " +
            "ORDER BY [PaymentRegister].IsActive DESC",
            new { CurrencyId = currencyId, OrganizationId = organizationId }
        ).SingleOrDefault();
    }

    public List<PaymentRegister> GetAll(PaymentRegisterType? type, string value, Guid? organizationNetId) {
        List<PaymentRegister> toReturn = new();

        string sqlExpression =
            "SELECT * " +
            "FROM [PaymentRegister] " +
            "LEFT JOIN [PaymentCurrencyRegister] " +
            "ON [PaymentCurrencyRegister].PaymentRegisterID = [PaymentRegister].ID " +
            "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
            "ON [Currency].ID = [PaymentCurrencyRegister].CurrencyID " +
            "AND [Currency].CultureCode = @Culture " +
            "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
            "ON [Organization].ID = [PaymentRegister].OrganizationID " +
            "AND [Organization].CultureCode = @Culture " +
            "WHERE [PaymentRegister].Deleted = 0 ";

        if (type.HasValue) sqlExpression += "AND [PaymentRegister].Type = @Type ";

        if (organizationNetId.HasValue) sqlExpression += "AND [Organization].NetUID = @OrganizationNetId ";

        sqlExpression += "AND [PaymentRegister].Name like '%' + @Value + '%' " +
                         "ORDER BY [PaymentRegister].Name";

        _connection.Query<PaymentRegister, PaymentCurrencyRegister, Currency, Organization, PaymentRegister>(
            sqlExpression,
            (register, registerCurrency, currency, organization) => {
                if (!toReturn.Any(r => r.Id.Equals(register.Id))) {
                    if (registerCurrency != null) {
                        registerCurrency.Currency = currency;

                        register.PaymentCurrencyRegisters.Add(registerCurrency);
                    }

                    register.Organization = organization;

                    toReturn.Add(register);
                } else if (registerCurrency != null) {
                    PaymentRegister fromList = toReturn.First(r => r.Id.Equals(register.Id));

                    if (fromList.PaymentCurrencyRegisters.Any(r => r.Id.Equals(registerCurrency.Id))) return register;

                    registerCurrency.Currency = currency;

                    fromList.PaymentCurrencyRegisters.Add(registerCurrency);
                }

                return register;
            },
            new {
                Type = type ?? PaymentRegisterType.Bank,
                OrganizationNetId = organizationNetId ?? Guid.Empty,
                Value = value,
                Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
            }
        );

        return toReturn;
    }

    public List<PaymentRegister> GetAllForRetail(Guid? organizationNetUid) {
        List<PaymentRegister> paymentRegisters = new();

        string sqlExpression =
            "SELECT * FROM PaymentRegister " +
            "LEFT JOIN PaymentCurrencyRegister " +
            "ON PaymentCurrencyRegister.PaymentRegisterID = PaymentRegister.ID " +
            "LEFT JOIN Currency " +
            "ON Currency.ID = PaymentCurrencyRegister.CurrencyID " +
            "LEFT JOIN Organization " +
            "ON Organization.ID = PaymentRegister.OrganizationID " +
            "WHERE IsForRetail = 1 " +
            "AND PaymentRegister.Deleted = 0 ";

        if (organizationNetUid != null) sqlExpression += "AND Organization.NetUID = @OrganizationNetId ";

        _connection.Query<PaymentRegister, PaymentCurrencyRegister, Currency, Organization, PaymentRegister>(
            sqlExpression,
            (paymentRegister, paymentCurrencyRegister, currency, organization) => {
                if (paymentRegisters.Any(e => e.Id.Equals(paymentRegister.Id))) {
                    if (paymentCurrencyRegister != null) {
                        paymentCurrencyRegister.Currency = currency;
                        paymentRegister.PaymentCurrencyRegisters.Add(paymentCurrencyRegister);
                    }
                } else if (paymentCurrencyRegister != null) {
                    paymentCurrencyRegister.Currency = currency;
                    paymentRegister.PaymentCurrencyRegisters.Add(paymentCurrencyRegister);
                }

                paymentRegister.Organization = organization;

                paymentRegisters.Add(paymentRegister);

                return paymentRegister;
            },
            new { OrganizationNetId = organizationNetUid ?? Guid.Empty });

        return paymentRegisters;
    }

    public List<PaymentRegister> GetAllFromSearch(string value) {
        List<PaymentRegister> toReturn = new();

        _connection.Query<PaymentRegister, PaymentCurrencyRegister, Currency, Organization, PaymentRegister, PaymentRegister>(
            "SELECT * " +
            "FROM [PaymentRegister] " +
            "LEFT JOIN [PaymentCurrencyRegister] " +
            "ON [PaymentCurrencyRegister].PaymentRegisterID = [PaymentRegister].ID " +
            "LEFT JOIN [views].[CurrencyView] AS [Currency] " +
            "ON [Currency].ID = [PaymentCurrencyRegister].CurrencyID " +
            "AND [Currency].CultureCode = @Culture " +
            "LEFT JOIN [views].[OrganizationView] AS [Organization] " +
            "ON [Organization].ID = [PaymentRegister].OrganizationID " +
            "AND [Organization].CultureCode = @Culture " +
            "LEFT JOIN [PaymentRegister] AS [InsidePaymentRegister] " +
            "ON [InsidePaymentRegister].ID = [PaymentCurrencyRegister].PaymentRegisterID " +
            "WHERE [PaymentRegister].Deleted = 0 " +
            "AND [PaymentRegister].Name like '%' + @Value + '%' " +
            "ORDER BY [PaymentRegister].Name",
            (register, registerCurrency, currency, organization, insideRegister) => {
                if (!toReturn.Any(r => r.Id.Equals(register.Id))) {
                    if (registerCurrency != null) {
                        registerCurrency.Currency = currency;
                        registerCurrency.PaymentRegister = insideRegister;

                        register.PaymentCurrencyRegisters.Add(registerCurrency);
                    }

                    register.Organization = organization;

                    toReturn.Add(register);
                } else if (registerCurrency != null) {
                    PaymentRegister fromList = toReturn.First(r => r.Id.Equals(register.Id));

                    if (fromList.PaymentCurrencyRegisters.Any(r => r.Id.Equals(registerCurrency.Id))) return register;

                    registerCurrency.Currency = currency;
                    registerCurrency.PaymentRegister = insideRegister;

                    fromList.PaymentCurrencyRegisters.Add(registerCurrency);
                }

                return register;
            },
            new { Value = value, Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
        );

        return toReturn;
    }

    public void Remove(Guid netId) {
        _connection.Execute(
            "UPDATE [PaymentRegister] " +
            "SET Deleted = 1, Updated = getutcdate() " +
            "WHERE [PaymentRegister].NetUID = @NetId",
            new { NetId = netId }
        );
    }

    public PaymentCurrencyRegisterModel GetFilteredMovementsByPaymentRegisterNetId(
        Guid netId,
        TypeFilteredMovements type,
        DateTime from,
        DateTime to,
        int limit,
        int offset) {
        (decimal initialAmountBeforeCalculated, decimal initialAmountBeforeCalculatedEur) =
            _connection.Query<decimal, decimal, Tuple<decimal, decimal>>(
                "DECLARE @INITIAL_AMOUNT money = " +
                "(SELECT [PaymentCurrencyRegister].[InitialAmount] FROM [PaymentCurrencyRegister] " +
                "WHERE [PaymentCurrencyRegister].[NetUID] = @NetId); " +
                "DECLARE @INITIAL_AMOUNT_EUR money = 0; " +
                ";WITH [BEFORE_CALCULATED_INITIAL_BALANCE_CTE] AS ( " +
                "SELECT " +
                "[IncomePaymentOrder].[NetUID] " +
                ", [IncomePaymentOrder].[Created] " +
                ", CASE " +
                "WHEN [IncomePaymentOrder].[Amount] IS NULL " +
                "THEN 0 " +
                "ELSE [IncomePaymentOrder].[Amount] " +
                "END AS [Value] " +
                ", 1 AS [IsIncrease] " +
                ", [PaymentCurrencyRegister].[Amount] " +
                ", [PaymentCurrencyRegister].[CurrencyID] " +
                "FROM [IncomePaymentOrder] " +
                "LEFT JOIN [PaymentRegister] " +
                "ON [PaymentRegister].[ID] = [IncomePaymentOrder].[PaymentRegisterID] " +
                "LEFT JOIN [PaymentCurrencyRegister] " +
                "ON [PaymentCurrencyRegister].[PaymentRegisterID] = [PaymentRegister].[ID] " +
                "WHERE [PaymentCurrencyRegister].[NetUID] = @NetId " +
                "AND [IncomePaymentOrder].[Created] < @From " +
                "UNION " +
                "SELECT " +
                "[OutcomePaymentOrder].[NetUID] " +
                ", [OutcomePaymentOrder].[Created] " +
                ", CASE " +
                "WHEN [OutcomePaymentOrder].[Amount] IS NULL " +
                "THEN 0 " +
                "ELSE [OutcomePaymentOrder].[Amount] " +
                "END AS [Value] " +
                ", 0 AS [IsIncrease] " +
                ", [PaymentCurrencyRegister].[Amount] " +
                ", [PaymentCurrencyRegister].[CurrencyID] " +
                "FROM [OutcomePaymentOrder] " +
                "LEFT JOIN [PaymentCurrencyRegister] " +
                "ON [PaymentCurrencyRegister].[ID] = [OutcomePaymentOrder].[PaymentCurrencyRegisterID] " +
                "WHERE [PaymentCurrencyRegister].[NetUID] = @NetId " +
                "AND [OutcomePaymentOrder].[Created] < @From " +
                "UNION " +
                "SELECT " +
                "[PaymentRegisterTransfer].[NetUID] " +
                ", [PaymentRegisterTransfer].[Created] " +
                ", [PaymentRegisterTransfer].[Amount] AS [Value] " +
                ", CASE WHEN [IncomeTransfer].[NetUID] = @NetId " +
                "THEN 1 " +
                "ELSE 0 " +
                "END AS [IsIncrease] " +
                ", CASE WHEN [IncomeTransfer].[NetUID] = @NetId " +
                "THEN [IncomeTransfer].[Amount] " +
                "ELSE [OutcomeTransfer].[Amount] " +
                "END AS [Amount] " +
                ", [OutcomeTransfer].[CurrencyID] " +
                "FROM [PaymentRegisterTransfer] " +
                "LEFT JOIN [PaymentCurrencyRegister] AS [IncomeTransfer] " +
                "ON [IncomeTransfer].[ID] = [PaymentRegisterTransfer].[ToPaymentCurrencyRegisterID] " +
                "LEFT JOIN [PaymentRegister] AS [ToPaymentRegister] " +
                "ON [ToPaymentRegister].[ID] = [IncomeTransfer].[PaymentRegisterID] " +
                "LEFT JOIN [PaymentCurrencyRegister] AS [OutcomeTransfer] " +
                "ON [OutcomeTransfer].[ID] = [PaymentRegisterTransfer].[FromPaymentCurrencyRegisterID] " +
                "LEFT JOIN [PaymentRegister] AS [FromPaymentRegister] " +
                "ON [FromPaymentRegister].[ID] = [OutcomeTransfer].[PaymentRegisterID] " +
                "LEFT JOIN [Currency] " +
                "ON [Currency].[ID] = CASE WHEN [IncomeTransfer].[NetUID] = @NetId " +
                "THEN [IncomeTransfer].[CurrencyID] " +
                "ELSE [OutcomeTransfer].[CurrencyID] " +
                "END " +
                "WHERE [PaymentRegisterTransfer].[Created] < @From " +
                "AND ([IncomeTransfer].[NetUID] = @NetId " +
                "OR [OutcomeTransfer].[NetUID] = @NetId) " +
                ") " +
                "SELECT " +
                "@INITIAL_AMOUNT = " +
                "CASE " +
                "WHEN [BEFORE_CALCULATED_INITIAL_BALANCE_CTE].[IsIncrease] = 1 " +
                "THEN @INITIAL_AMOUNT + [BEFORE_CALCULATED_INITIAL_BALANCE_CTE].[Value] " +
                "ELSE @INITIAL_AMOUNT - [BEFORE_CALCULATED_INITIAL_BALANCE_CTE].[Value] " +
                "END " +
                "FROM [BEFORE_CALCULATED_INITIAL_BALANCE_CTE] " +
                "ORDER BY [BEFORE_CALCULATED_INITIAL_BALANCE_CTE].[Created] DESC " +
                "SELECT CONVERT(money, @INITIAL_AMOUNT) AS [InitialAmount] " +
                ", CONVERT(money, dbo.GetExchangedToEuroValue( " +
                "@INITIAL_AMOUNT " +
                ", [PaymentCurrencyRegister].CurrencyID " +
                ", @From " +
                ")) AS [InitialAmountEur] " +
                "FROM [PaymentCurrencyRegister] " +
                "WHERE [PaymentCurrencyRegister].[NetUID] = @NetId "
                , (initialAmount, initialAmountEur) =>
                    new Tuple<decimal, decimal>(initialAmount, initialAmountEur)
                , new { NetId = netId, From = from }, splitOn: "InitialAmount,InitialAmountEur").Single();

        List<PaymentMovementInfoModel> paymentMovementInfos =
            _connection.Query<PaymentMovementInfoModel>(
                ";WITH [CALCULATED_BALANCE_CTE] AS ( " +
                "SELECT " +
                "[IncomePaymentOrder].[NetUid] AS [NetUId] " +
                ", [IncomePaymentOrder].[Created] AS [Created] " +
                ", [IncomePaymentOrder].[Amount] AS [Value] " +
                ", dbo.GetExchangedToEuroValue( " +
                "[IncomePaymentOrder].[Amount] " +
                ",[PaymentCurrencyRegister].[CurrencyID] " +
                ",[IncomePaymentOrder].[Created] " +
                ") AS [ValueEur] " +
                ", 1 AS [IsIncrease] " +
                "FROM [IncomePaymentOrder] " +
                "LEFT JOIN [PaymentRegister] " +
                "ON [PaymentRegister].[ID] = [IncomePaymentOrder].[PaymentRegisterID] " +
                "LEFT JOIN [PaymentCurrencyRegister] " +
                "ON [PaymentCurrencyRegister].[PaymentRegisterID] = [PaymentRegister].[ID] " +
                "WHERE [PaymentCurrencyRegister].[NetUID] = @NetId " +
                "AND [IncomePaymentOrder].[Created] >= @From " +
                "AND [IncomePaymentOrder].[Created] <= @To " +
                "UNION " +
                "SELECT " +
                "[OutcomePaymentOrder].[NetUid] AS [NetUId] " +
                ", [OutcomePaymentOrder].[Created] AS [Created] " +
                ", [OutcomePaymentOrder].[Amount] AS [Value] " +
                ", dbo.GetExchangedToEuroValue( " +
                "[OutcomePaymentOrder].[Amount] " +
                ",[PaymentCurrencyRegister].[CurrencyID] " +
                ",[OutcomePaymentOrder].[Created] " +
                ") AS [ValueEur] " +
                ", 0 AS [IsIncrease] " +
                "FROM [OutcomePaymentOrder] " +
                "LEFT JOIN [PaymentCurrencyRegister] " +
                "ON [PaymentCurrencyRegister].[ID] = [OutcomePaymentOrder].[PaymentCurrencyRegisterID] " +
                "WHERE [PaymentCurrencyRegister].[NetUID] = @NetId " +
                "AND [OutcomePaymentOrder].[Created] >= @From " +
                "AND [OutcomePaymentOrder].[Created] <= @To " +
                "UNION " +
                "SELECT " +
                "[PaymentRegisterTransfer].[NetUid] AS [NetUId] " +
                ", [PaymentRegisterTransfer].[Created] AS [Created] " +
                ", [PaymentRegisterTransfer].[Amount] AS [Value] " +
                ", dbo.GetExchangedToEuroValue( " +
                "[PaymentRegisterTransfer].[Amount] " +
                ",[OutcomeTransfer].[CurrencyID] " +
                ",[PaymentRegisterTransfer].[Created] " +
                ") AS [ValueEur] " +
                ", CASE WHEN [IncomeTransfer].[NetUID] = @NetId " +
                "THEN 1 " +
                "ELSE 0 " +
                "END AS [IsIncrease] " +
                "FROM [PaymentRegisterTransfer] " +
                "LEFT JOIN [PaymentCurrencyRegister] AS [IncomeTransfer] " +
                "ON [IncomeTransfer].[ID] = [PaymentRegisterTransfer].[ToPaymentCurrencyRegisterID] " +
                "LEFT JOIN [PaymentRegister] AS [ToPaymentRegister] " +
                "ON [ToPaymentRegister].[ID] = [IncomeTransfer].[PaymentRegisterID] " +
                "LEFT JOIN [PaymentCurrencyRegister] AS [OutcomeTransfer] " +
                "ON [OutcomeTransfer].[ID] = [PaymentRegisterTransfer].[FromPaymentCurrencyRegisterID] " +
                "LEFT JOIN [PaymentRegister] AS [FromPaymentRegister] " +
                "ON [FromPaymentRegister].[ID] = [OutcomeTransfer].[PaymentRegisterID] " +
                "WHERE [PaymentRegisterTransfer].[Created] >= @From " +
                "AND [PaymentRegisterTransfer].[Created] <= @To " +
                "AND ([IncomeTransfer].[NetUID] = @NetId " +
                "OR [OutcomeTransfer].[NetUID] = @NetId) " +
                ") " +
                "SELECT " +
                "[CALCULATED_BALANCE_CTE].[NetUId] AS [NetId] " +
                ", [CALCULATED_BALANCE_CTE].[IsIncrease] " +
                ", [CALCULATED_BALANCE_CTE].[Value] " +
                ", [CALCULATED_BALANCE_CTE].[ValueEur] " +
                "FROM [CALCULATED_BALANCE_CTE] " +
                "ORDER BY [CALCULATED_BALANCE_CTE].[Created] ",
                new { NetId = netId, To = to, From = from }).ToList();


        foreach (PaymentMovementInfoModel movement in paymentMovementInfos)
            if (movement.IsIncrease) {
                movement.InitialBalance = initialAmountBeforeCalculated;

                movement.InitialBalanceEur = initialAmountBeforeCalculatedEur;

                movement.FinalBalance = initialAmountBeforeCalculated += movement.Value;

                movement.FinalBalanceEur = initialAmountBeforeCalculatedEur += movement.ValueEur;
            } else {
                movement.InitialBalance = initialAmountBeforeCalculated;

                movement.InitialBalanceEur = initialAmountBeforeCalculatedEur;

                movement.FinalBalance = initialAmountBeforeCalculated -= movement.Value;

                movement.FinalBalanceEur = initialAmountBeforeCalculatedEur -= movement.ValueEur;
            }

        PaymentCurrencyRegisterModel toReturn =
            _connection.Query<PaymentCurrencyRegister, PaymentRegister, Currency, Organization, PaymentCurrencyRegisterModel>(
                "SELECT " +
                "[PaymentCurrencyRegister].* " +
                ",[PaymentRegister].* " +
                ",[Currency].* " +
                ",[Organization].* " +
                "FROM [PaymentCurrencyRegister] " +
                "LEFT JOIN [PaymentRegister] " +
                "ON [PaymentRegister].[ID] = [PaymentCurrencyRegister].[PaymentRegisterID] " +
                "LEFT JOIN [Currency] " +
                "ON [Currency].[ID] = [PaymentCurrencyRegister].[CurrencyID] " +
                "LEFT JOIN [Organization] " +
                "ON [Organization].[ID] = [PaymentRegister].[OrganizationID] " +
                "WHERE [PaymentCurrencyRegister].[NetUID] = @NetId ",
                (paymentCurrencyRegister, paymentRegister, currency, organization) =>
                    new PaymentCurrencyRegisterModel {
                        Organization = organization,
                        Currency = currency,
                        PaymentRegister = paymentRegister,
                        Amount = paymentCurrencyRegister.Amount,
                        FromDate = paymentRegister.FromDate,
                        NetUId = paymentCurrencyRegister.NetUid,
                        TotalValue = new TotalValueByPeriod {
                            InitialBalance = initialAmountBeforeCalculated,
                            FinalBalance = initialAmountBeforeCalculated
                        },
                        TotalValueEur = new TotalValueByPeriod {
                            InitialBalance = initialAmountBeforeCalculatedEur,
                            FinalBalance = initialAmountBeforeCalculatedEur
                        }
                    },
                new { NetId = netId }).FirstOrDefault();

        if (toReturn == null)
            throw new Exception();

        toReturn.From = from.Date;
        toReturn.To = to.Date;

        string sqlQuery;

        if (type.Equals(TypeFilteredMovements.Income))
            sqlQuery = ";WITH [PAYMENT_MOVEMENT_LIST_CTE] AS ( " +
                       "SELECT " +
                       "[IncomePaymentOrder].[NetUid] AS [NetUId] " +
                       ", [IncomePaymentOrder].[Created] " +
                       "FROM [IncomePaymentOrder] " +
                       "LEFT JOIN [PaymentRegister] " +
                       "ON [PaymentRegister].[ID] = [IncomePaymentOrder].[PaymentRegisterID] " +
                       "LEFT JOIN [PaymentCurrencyRegister] " +
                       "ON [PaymentCurrencyRegister].[PaymentRegisterID] = [PaymentRegister].[ID] " +
                       "WHERE [PaymentCurrencyRegister].[NetUID] = @NetId " +
                       "AND [IncomePaymentOrder].[Created] >= @From " +
                       "AND [IncomePaymentOrder].[Created] <= @To " +
                       "AND [IncomePaymentOrder].[Deleted] = 0 " +
                       "UNION " +
                       "SELECT " +
                       "[PaymentRegisterTransfer].[NetUid] AS [NetUId] " +
                       ", [PaymentRegisterTransfer].[Created] " +
                       "FROM [PaymentRegisterTransfer] " +
                       "LEFT JOIN [PaymentCurrencyRegister] AS [IncomeTransfer] " +
                       "ON [IncomeTransfer].[ID] = [PaymentRegisterTransfer].[ToPaymentCurrencyRegisterID] " +
                       "LEFT JOIN [PaymentRegister] AS [ToPaymentRegister] " +
                       "ON [ToPaymentRegister].[ID] = [IncomeTransfer].[PaymentRegisterID] " +
                       "LEFT JOIN [PaymentCurrencyRegister] AS [OutcomeTransfer] " +
                       "ON [OutcomeTransfer].[ID] = [PaymentRegisterTransfer].[FromPaymentCurrencyRegisterID] " +
                       "WHERE [PaymentRegisterTransfer].[Created] >= @From " +
                       "AND [PaymentRegisterTransfer].[Created] <= @To " +
                       "AND [IncomeTransfer].[Deleted] = 0 " +
                       "AND [OutcomeTransfer].[Deleted] = 0 " +
                       "AND ([IncomeTransfer].[NetUID] = @NetId " +
                       "OR [OutcomeTransfer].[NetUID] = @NetId) " +
                       "), " +
                       "[FILTERED_LIST_CTE] AS ( " +
                       "SELECT ROW_NUMBER() OVER (ORDER BY [PAYMENT_MOVEMENT_LIST_CTE].[Created] DESC) AS [RowNumber] " +
                       ", [PAYMENT_MOVEMENT_LIST_CTE].[NetUId] " +
                       "FROM [PAYMENT_MOVEMENT_LIST_CTE] " +
                       ") " +
                       "SELECT " +
                       "[IncomePaymentOrder].[NetUid] AS [NetUId] " +
                       ", [IncomePaymentOrder].[Created] " +
                       ", [IncomePaymentOrder].[FromDate] " +
                       ", [IncomePaymentOrder].[Amount] AS [Value] " +
                       ", [IncomePaymentOrder].[Number] " +
                       ", [IncomePaymentOrder].[Comment] " +
                       ", 1 AS [IsIncrease] " +
                       ", 0 AS [Type] " +
                       ", null AS [ToPaymentRegisterName] " +
                       ", null AS [FromPaymentRegisterName] " +
                       ", [Client].[FullName] AS [ClientName] " +
                       ", [IncomePaymentOrder].[IsAccounting] AS [IsAccounting] " +
                       ", [Currency].* " +
                       ", [User].* " +
                       "FROM [IncomePaymentOrder] " +
                       "LEFT JOIN [PaymentRegister] " +
                       "ON [PaymentRegister].[ID] = [IncomePaymentOrder].[PaymentRegisterID] " +
                       "LEFT JOIN [PaymentCurrencyRegister] " +
                       "ON [PaymentCurrencyRegister].[PaymentRegisterID] = [PaymentRegister].[ID] " +
                       "LEFT JOIN [Currency] " +
                       "ON [Currency].[ID] = [IncomePaymentOrder].[CurrencyID] " +
                       "LEFT JOIN [User] " +
                       "ON [User].[ID] = [IncomePaymentOrder].[UserID] " +
                       "LEFT JOIN [Client] " +
                       "ON [Client].[ID] = [IncomePaymentOrder].[ClientID] " +
                       "WHERE [PaymentCurrencyRegister].[NetUID] = @NetId " +
                       "AND [IncomePaymentOrder].[Created] >= @From " +
                       "AND [IncomePaymentOrder].[Created] <= @To " +
                       "AND [IncomePaymentOrder].[NetUID] IN (SELECT [FILTERED_LIST_CTE].[NetUId] FROM [FILTERED_LIST_CTE] " +
                       "WHERE [FILTERED_LIST_CTE].RowNumber > @Offset " +
                       "AND [FILTERED_LIST_CTE].[RowNumber] <= @Limit + @Offset) " +
                       "UNION " +
                       "SELECT " +
                       "[PaymentRegisterTransfer].[NetUid] AS [NetUId] " +
                       ", [PaymentRegisterTransfer].[Created] " +
                       ", [PaymentRegisterTransfer].[FromDate] " +
                       ", [PaymentRegisterTransfer].[Amount] AS [Value] " +
                       ", [PaymentRegisterTransfer].[Number] " +
                       ", [PaymentRegisterTransfer].[Comment] " +
                       ", CASE WHEN [IncomeTransfer].[NetUID] = @NetId " +
                       "THEN 1 " +
                       "ELSE 0 " +
                       "END AS [IsIncrease] " +
                       ", 2 AS [Type] " +
                       ", [ToPaymentRegister].[Name] AS [ToPaymentRegisterName] " +
                       ", [FromPaymentRegister].[Name] As [FromPaymentRegisterName] " +
                       "'' AS [ClientName] " +
                       ", 0 AS [IsAccounting] " +
                       ", [Currency].* " +
                       ", [User].* " +
                       "FROM [PaymentRegisterTransfer] " +
                       "LEFT JOIN [PaymentCurrencyRegister] AS [IncomeTransfer] " +
                       "ON [IncomeTransfer].[ID] = [PaymentRegisterTransfer].[ToPaymentCurrencyRegisterID] " +
                       "LEFT JOIN [PaymentRegister] AS [ToPaymentRegister] " +
                       "ON [ToPaymentRegister].[ID] = [IncomeTransfer].[PaymentRegisterID] " +
                       "LEFT JOIN [PaymentCurrencyRegister] AS [OutcomeTransfer] " +
                       "ON [OutcomeTransfer].[ID] = [PaymentRegisterTransfer].[FromPaymentCurrencyRegisterID] " +
                       "LEFT JOIN [PaymentRegister] AS [FromPaymentRegister] " +
                       "ON [FromPaymentRegister].[ID] = [OutcomeTransfer].[PaymentRegisterID] " +
                       "LEFT JOIN [Currency] " +
                       "ON [Currency].[ID] = CASE WHEN [IncomeTransfer].[NetUID] = @NetId " +
                       "THEN [IncomeTransfer].[CurrencyID] " +
                       "ELSE [OutcomeTransfer].[CurrencyID] " +
                       "END " +
                       "LEFT JOIN [User] " +
                       "ON [User].[ID] = [PaymentRegisterTransfer].[UserID] " +
                       "WHERE [PaymentRegisterTransfer].[Created] >= @From " +
                       "AND [PaymentRegisterTransfer].[Created] <= @To " +
                       "AND ([IncomeTransfer].[NetUID] = @NetId " +
                       "OR [OutcomeTransfer].[NetUID] = @NetId) " +
                       "AND [PaymentRegisterTransfer].[NetUID] IN (SELECT [FILTERED_LIST_CTE].[NetUId] FROM [FILTERED_LIST_CTE] " +
                       "WHERE [FILTERED_LIST_CTE].RowNumber > @Offset " +
                       "AND [FILTERED_LIST_CTE].[RowNumber] <= @Limit + @Offset) ";
        else if (type.Equals(TypeFilteredMovements.Outcome))
            sqlQuery = ";WITH [PAYMENT_MOVEMENT_LIST_CTE] AS ( " +
                       "SELECT " +
                       "[OutcomePaymentOrder].[NetUid] AS [NetUId] " +
                       ", [OutcomePaymentOrder].[Created] " +
                       "FROM [OutcomePaymentOrder] " +
                       "LEFT JOIN [PaymentCurrencyRegister] " +
                       "ON [PaymentCurrencyRegister].[ID] = [OutcomePaymentOrder].[PaymentCurrencyRegisterID] " +
                       "WHERE [PaymentCurrencyRegister].[NetUID] = @NetId " +
                       "AND [OutcomePaymentOrder].[Created] >= @From " +
                       "AND [OutcomePaymentOrder].[Created] <= @To " +
                       "AND [OutcomePaymentOrder].[Deleted] = 0 " +
                       "UNION " +
                       "SELECT " +
                       "[PaymentRegisterTransfer].[NetUid] AS [NetUId] " +
                       ", [PaymentRegisterTransfer].[Created] " +
                       "FROM [PaymentRegisterTransfer] " +
                       "LEFT JOIN [PaymentCurrencyRegister] AS [IncomeTransfer] " +
                       "ON [IncomeTransfer].[ID] = [PaymentRegisterTransfer].[ToPaymentCurrencyRegisterID] " +
                       "LEFT JOIN [PaymentRegister] AS [ToPaymentRegister] " +
                       "ON [ToPaymentRegister].[ID] = [IncomeTransfer].[PaymentRegisterID] " +
                       "LEFT JOIN [PaymentCurrencyRegister] AS [OutcomeTransfer] " +
                       "ON [OutcomeTransfer].[ID] = [PaymentRegisterTransfer].[FromPaymentCurrencyRegisterID] " +
                       "WHERE [PaymentRegisterTransfer].[Created] >= @From " +
                       "AND [PaymentRegisterTransfer].[Created] <= @To " +
                       "AND [IncomeTransfer].[Deleted] = 0 " +
                       "AND [OutcomeTransfer].[Deleted] = 0 " +
                       "AND ([IncomeTransfer].[NetUID] = @NetId " +
                       "OR [OutcomeTransfer].[NetUID] = @NetId) " +
                       "), " +
                       "[FILTERED_LIST_CTE] AS ( " +
                       "SELECT ROW_NUMBER() OVER (ORDER BY [PAYMENT_MOVEMENT_LIST_CTE].[Created] DESC) AS [RowNumber] " +
                       ", [PAYMENT_MOVEMENT_LIST_CTE].[NetUId] " +
                       "FROM [PAYMENT_MOVEMENT_LIST_CTE] " +
                       ") " +
                       "SELECT " +
                       "[OutcomePaymentOrder].[NetUid] AS [NetUId] " +
                       ", [OutcomePaymentOrder].[Created] " +
                       ", [OutcomePaymentOrder].[FromDate] " +
                       ", [OutcomePaymentOrder].[Amount] AS [Value] " +
                       ", [OutcomePaymentOrder].[Number] " +
                       ", [OutcomePaymentOrder].[Comment] " +
                       ", 0 AS [IsIncrease] " +
                       ", 1 AS [Type] " +
                       ", null AS [ToPaymentRegisterName] " +
                       ", null AS [FromPaymentRegisterName] " +
                       ", [Client].[ClientName] AS [ClientName] " +
                       ", [SupplyPaymentTask].[IsAccounting] AS [IsAccounting] " +
                       ", [Currency].* " +
                       ", [User].* " +
                       "FROM [OutcomePaymentOrder] " +
                       "LEFT JOIN [PaymentCurrencyRegister] " +
                       "ON [PaymentCurrencyRegister].[ID] = [OutcomePaymentOrder].[PaymentCurrencyRegisterID] " +
                       "LEFT JOIN [Currency] " +
                       "ON [Currency].[ID] = [PaymentCurrencyRegister].[CurrencyID] " +
                       "LEFT JOIN [User] " +
                       "ON [User].[ID] = [OutcomePaymentOrder].[UserID] " +
                       "LEFT JOIN [ClientAgreement] " +
                       "ON [ClientAgreement].[ID] = [OutcomePaymentOrder].[ClientAgreementID] " +
                       "LEFT JOIN [Client] " +
                       "ON [Client].[ID] = [ClientAgreement].[ClientID] " +
                       "LEFT JOIN [OutcomePaymentOrderSupplyPaymentTask] " +
                       "ON [OutcomePaymentOrderSupplyPaymentTask].[OutcomePaymentOrderID] = [OutcomePaymentOrder].[ID] " +
                       "LEFT JOIN [SupplyPaymentTask] " +
                       "ON [SupplyPaymentTask].[ID] = [OutcomePaymentOrderSupplyPaymentTask].[SupplyPaymentTaskID] " +
                       "WHERE [PaymentCurrencyRegister].[NetUID] = @NetId " +
                       "AND [OutcomePaymentOrder].[Created] >= @From " +
                       "AND [OutcomePaymentOrder].[Created] <= @To " +
                       "AND [OutcomePaymentOrder].[NetUID] IN (SELECT [FILTERED_LIST_CTE].[NetUId] FROM [FILTERED_LIST_CTE] " +
                       "WHERE [FILTERED_LIST_CTE].RowNumber > @Offset " +
                       "AND [FILTERED_LIST_CTE].[RowNumber] <= @Limit + @Offset) " +
                       "UNION " +
                       "SELECT " +
                       "[PaymentRegisterTransfer].[NetUid] AS [NetUId] " +
                       ", [PaymentRegisterTransfer].[Created] " +
                       ", [PaymentRegisterTransfer].[FromDate] " +
                       ", [PaymentRegisterTransfer].[Amount] AS [Value] " +
                       ", [PaymentRegisterTransfer].[Number] " +
                       ", [PaymentRegisterTransfer].[Comment] " +
                       ", CASE WHEN [IncomeTransfer].[NetUID] = @NetId " +
                       "THEN 1 " +
                       "ELSE 0 " +
                       "END AS [IsIncrease] " +
                       ", 2 AS [Type] " +
                       ", [ToPaymentRegister].[Name] AS [ToPaymentRegisterName] " +
                       ", [FromPaymentRegister].[Name] As [FromPaymentRegisterName] " +
                       ", '' AS [ClientName] " +
                       ", 0 AS [IsAccounting] " +
                       ", [Currency].* " +
                       ", [User].* " +
                       "FROM [PaymentRegisterTransfer] " +
                       "LEFT JOIN [PaymentCurrencyRegister] AS [IncomeTransfer] " +
                       "ON [IncomeTransfer].[ID] = [PaymentRegisterTransfer].[ToPaymentCurrencyRegisterID] " +
                       "LEFT JOIN [PaymentRegister] AS [ToPaymentRegister] " +
                       "ON [ToPaymentRegister].[ID] = [IncomeTransfer].[PaymentRegisterID] " +
                       "LEFT JOIN [PaymentCurrencyRegister] AS [OutcomeTransfer] " +
                       "ON [OutcomeTransfer].[ID] = [PaymentRegisterTransfer].[FromPaymentCurrencyRegisterID] " +
                       "LEFT JOIN [PaymentRegister] AS [FromPaymentRegister] " +
                       "ON [FromPaymentRegister].[ID] = [OutcomeTransfer].[PaymentRegisterID] " +
                       "LEFT JOIN [Currency] " +
                       "ON [Currency].[ID] = CASE WHEN [IncomeTransfer].[NetUID] = @NetId " +
                       "THEN [IncomeTransfer].[CurrencyID] " +
                       "ELSE [OutcomeTransfer].[CurrencyID] " +
                       "END " +
                       "LEFT JOIN [User] " +
                       "ON [User].[ID] = [PaymentRegisterTransfer].[UserID] " +
                       "WHERE [PaymentRegisterTransfer].[Created] >= @From " +
                       "AND [PaymentRegisterTransfer].[Created] <= @To " +
                       "AND ([IncomeTransfer].[NetUID] = @NetId " +
                       "OR [OutcomeTransfer].[NetUID] = @NetId) " +
                       "AND [PaymentRegisterTransfer].[NetUID] IN (SELECT [FILTERED_LIST_CTE].[NetUId] FROM [FILTERED_LIST_CTE] " +
                       "WHERE [FILTERED_LIST_CTE].RowNumber > @Offset " +
                       "AND [FILTERED_LIST_CTE].[RowNumber] <= @Limit + @Offset) ";
        else
            sqlQuery = ";WITH [PAYMENT_MOVEMENT_LIST_CTE] AS ( " +
                       "SELECT " +
                       "[IncomePaymentOrder].[NetUid] AS [NetUId] " +
                       ", [IncomePaymentOrder].[Created] " +
                       "FROM [IncomePaymentOrder] " +
                       "LEFT JOIN [PaymentRegister] " +
                       "ON [PaymentRegister].[ID] = [IncomePaymentOrder].[PaymentRegisterID] " +
                       "LEFT JOIN [PaymentCurrencyRegister] " +
                       "ON [PaymentCurrencyRegister].[PaymentRegisterID] = [PaymentRegister].[ID] " +
                       "WHERE [PaymentCurrencyRegister].[NetUID] = @NetId " +
                       "AND [IncomePaymentOrder].[Created] >= @From " +
                       "AND [IncomePaymentOrder].[Created] <= @To " +
                       "AND [IncomePaymentOrder].[Deleted] = 0 " +
                       "UNION " +
                       "SELECT " +
                       "[OutcomePaymentOrder].[NetUid] AS [NetUId] " +
                       ", [OutcomePaymentOrder].[Created] " +
                       "FROM [OutcomePaymentOrder] " +
                       "LEFT JOIN [PaymentCurrencyRegister] " +
                       "ON [PaymentCurrencyRegister].[ID] = [OutcomePaymentOrder].[PaymentCurrencyRegisterID] " +
                       "WHERE [PaymentCurrencyRegister].[NetUID] = @NetId " +
                       "AND [OutcomePaymentOrder].[Created] >= @From " +
                       "AND [OutcomePaymentOrder].[Created] <= @To " +
                       "AND [OutcomePaymentOrder].[Deleted] = 0 " +
                       "UNION " +
                       "SELECT " +
                       "[PaymentRegisterTransfer].[NetUid] AS [NetUId] " +
                       ", [PaymentRegisterTransfer].[Created] " +
                       "FROM [PaymentRegisterTransfer] " +
                       "LEFT JOIN [PaymentCurrencyRegister] AS [IncomeTransfer] " +
                       "ON [IncomeTransfer].[ID] = [PaymentRegisterTransfer].[ToPaymentCurrencyRegisterID] " +
                       "LEFT JOIN [PaymentRegister] AS [ToPaymentRegister] " +
                       "ON [ToPaymentRegister].[ID] = [IncomeTransfer].[PaymentRegisterID] " +
                       "LEFT JOIN [PaymentCurrencyRegister] AS [OutcomeTransfer] " +
                       "ON [OutcomeTransfer].[ID] = [PaymentRegisterTransfer].[FromPaymentCurrencyRegisterID] " +
                       "WHERE [PaymentRegisterTransfer].[Created] >= @From " +
                       "AND [PaymentRegisterTransfer].[Created] <= @To " +
                       "AND [IncomeTransfer].[Deleted] = 0 " +
                       "AND [OutcomeTransfer].[Deleted] = 0 " +
                       "AND ([IncomeTransfer].[NetUID] = @NetId " +
                       "OR [OutcomeTransfer].[NetUID] = @NetId) " +
                       "), " +
                       "[FILTERED_LIST_CTE] AS ( " +
                       "SELECT ROW_NUMBER() OVER (ORDER BY [PAYMENT_MOVEMENT_LIST_CTE].[Created] DESC) AS [RowNumber] " +
                       ", [PAYMENT_MOVEMENT_LIST_CTE].[NetUId] " +
                       "FROM [PAYMENT_MOVEMENT_LIST_CTE] " +
                       ") " +
                       "SELECT " +
                       "[IncomePaymentOrder].[NetUid] AS [NetUId] " +
                       ", [IncomePaymentOrder].[Created] " +
                       ", [IncomePaymentOrder].[FromDate] " +
                       ", [IncomePaymentOrder].[Amount] AS [Value] " +
                       ", [IncomePaymentOrder].[Number] " +
                       ", [IncomePaymentOrder].[Comment] " +
                       ", 1 AS [IsIncrease] " +
                       ", 0 AS [Type] " +
                       ", null AS [ToPaymentRegisterName] " +
                       ", null AS [FromPaymentRegisterName] " +
                       ", [Client].[FullName] AS [ClientName] " +
                       ", [IncomePaymentOrder].[IsAccounting] AS [IsAccounting] " +
                       ", [Currency].* " +
                       ", [User].* " +
                       "FROM [IncomePaymentOrder] " +
                       "LEFT JOIN [PaymentRegister] " +
                       "ON [PaymentRegister].[ID] = [IncomePaymentOrder].[PaymentRegisterID] " +
                       "LEFT JOIN [PaymentCurrencyRegister] " +
                       "ON [PaymentCurrencyRegister].[PaymentRegisterID] = [PaymentRegister].[ID] " +
                       "LEFT JOIN [Currency] " +
                       "ON [Currency].[ID] = [IncomePaymentOrder].[CurrencyID] " +
                       "LEFT JOIN [User] " +
                       "ON [User].[ID] = [IncomePaymentOrder].[UserID] " +
                       "LEFT JOIN [Client] " +
                       "ON [Client].[ID] = [IncomePaymentOrder].[ClientID] " +
                       "WHERE [PaymentCurrencyRegister].[NetUID] = @NetId " +
                       "AND [IncomePaymentOrder].[Created] >= @From " +
                       "AND [IncomePaymentOrder].[Created] <= @To " +
                       "AND [IncomePaymentOrder].[NetUID] IN (SELECT [FILTERED_LIST_CTE].[NetUId] FROM [FILTERED_LIST_CTE] " +
                       "WHERE [FILTERED_LIST_CTE].RowNumber > @Offset " +
                       "AND [FILTERED_LIST_CTE].[RowNumber] <= @Limit + @Offset) " +
                       "UNION " +
                       "SELECT " +
                       "[OutcomePaymentOrder].[NetUid] AS [NetUId] " +
                       ", [OutcomePaymentOrder].[Created] " +
                       ", [OutcomePaymentOrder].[FromDate] " +
                       ", [OutcomePaymentOrder].[Amount] AS [Value] " +
                       ", [OutcomePaymentOrder].[Number] " +
                       ", [OutcomePaymentOrder].[Comment] " +
                       ", 0 AS [IsIncrease] " +
                       ", 1 AS [Type] " +
                       ", null AS [ToPaymentRegisterName] " +
                       ", null AS [FromPaymentRegisterName] " +
                       ", [Client].[FullName] AS [ClientName] " +
                       ", [SupplyPaymentTask].[IsAccounting] AS [IsAccounting] " +
                       ", [Currency].* " +
                       ", [User].* " +
                       "FROM [OutcomePaymentOrder] " +
                       "LEFT JOIN [PaymentCurrencyRegister] " +
                       "ON [PaymentCurrencyRegister].[ID] = [OutcomePaymentOrder].[PaymentCurrencyRegisterID] " +
                       "LEFT JOIN [Currency] " +
                       "ON [Currency].[ID] = [PaymentCurrencyRegister].[CurrencyID] " +
                       "LEFT JOIN [User] " +
                       "ON [User].[ID] = [OutcomePaymentOrder].[UserID] " +
                       "LEFT JOIN [ClientAgreement] " +
                       "ON [ClientAgreement].[ID] = [OutcomePaymentOrder].[ClientAgreementID] " +
                       "LEFT JOIN [Client] " +
                       "ON [Client].[ID] = [ClientAgreement].[ClientID] " +
                       "LEFT JOIN [OutcomePaymentOrderSupplyPaymentTask] " +
                       "ON [OutcomePaymentOrderSupplyPaymentTask].[OutcomePaymentOrderID] = [OutcomePaymentOrder].[ID] " +
                       "LEFT JOIN [SupplyPaymentTask] " +
                       "ON [SupplyPaymentTask].[ID] = [OutcomePaymentOrderSupplyPaymentTask].[SupplyPaymentTaskID] " +
                       "WHERE [PaymentCurrencyRegister].[NetUID] = @NetId " +
                       "AND [OutcomePaymentOrder].[Created] >= @From " +
                       "AND [OutcomePaymentOrder].[Created] <= @To " +
                       "AND [OutcomePaymentOrder].[NetUID] IN (SELECT [FILTERED_LIST_CTE].[NetUId] FROM [FILTERED_LIST_CTE] " +
                       "WHERE [FILTERED_LIST_CTE].RowNumber > @Offset " +
                       "AND [FILTERED_LIST_CTE].[RowNumber] <= @Limit + @Offset) " +
                       "UNION " +
                       "SELECT " +
                       "[PaymentRegisterTransfer].[NetUid] AS [NetUId] " +
                       ", [PaymentRegisterTransfer].[Created] " +
                       ", [PaymentRegisterTransfer].[FromDate] " +
                       ", [PaymentRegisterTransfer].[Amount] AS [Value] " +
                       ", [PaymentRegisterTransfer].[Number] " +
                       ", [PaymentRegisterTransfer].[Comment] " +
                       ", CASE WHEN [IncomeTransfer].[NetUID] = @NetId " +
                       "THEN 1 " +
                       "ELSE 0 " +
                       "END AS [IsIncrease] " +
                       ", 2 AS [Type] " +
                       ", [ToPaymentRegister].[Name] AS [ToPaymentRegisterName] " +
                       ", [FromPaymentRegister].[Name] As [FromPaymentRegisterName] " +
                       ", '' AS [ClientName] " +
                       ", 0 AS [IsAccounting] " +
                       ", [Currency].* " +
                       ", [User].* " +
                       "FROM [PaymentRegisterTransfer] " +
                       "LEFT JOIN [PaymentCurrencyRegister] AS [IncomeTransfer] " +
                       "ON [IncomeTransfer].[ID] = [PaymentRegisterTransfer].[ToPaymentCurrencyRegisterID] " +
                       "LEFT JOIN [PaymentRegister] AS [ToPaymentRegister] " +
                       "ON [ToPaymentRegister].[ID] = [IncomeTransfer].[PaymentRegisterID] " +
                       "LEFT JOIN [PaymentCurrencyRegister] AS [OutcomeTransfer] " +
                       "ON [OutcomeTransfer].[ID] = [PaymentRegisterTransfer].[FromPaymentCurrencyRegisterID] " +
                       "LEFT JOIN [PaymentRegister] AS [FromPaymentRegister] " +
                       "ON [FromPaymentRegister].[ID] = [OutcomeTransfer].[PaymentRegisterID] " +
                       "LEFT JOIN [Currency] " +
                       "ON [Currency].[ID] = CASE WHEN [IncomeTransfer].[NetUID] = @NetId " +
                       "THEN [IncomeTransfer].[CurrencyID] " +
                       "ELSE [OutcomeTransfer].[CurrencyID] " +
                       "END " +
                       "LEFT JOIN [User] " +
                       "ON [User].[ID] = [PaymentRegisterTransfer].[UserID] " +
                       "WHERE [PaymentRegisterTransfer].[Created] >= @From " +
                       "AND [PaymentRegisterTransfer].[Created] <= @To " +
                       "AND ([IncomeTransfer].[NetUID] = @NetId " +
                       "OR [OutcomeTransfer].[NetUID] = @NetId) " +
                       "AND [PaymentRegisterTransfer].[NetUID] IN (SELECT [FILTERED_LIST_CTE].[NetUId] FROM [FILTERED_LIST_CTE] " +
                       "WHERE [FILTERED_LIST_CTE].RowNumber > @Offset " +
                       "AND [FILTERED_LIST_CTE].[RowNumber] <= @Limit + @Offset) ";

        toReturn.PaymentMovements = _connection.Query<PaymentMovement, Currency, User, PaymentMovement>(
            sqlQuery, (paymentMovement, currency, user) => {
                paymentMovement.Currency = currency;
                paymentMovement.User = user;
                PaymentMovementInfoModel movementInfo =
                    paymentMovementInfos.First(e => e.NetId.Equals(paymentMovement.NetUId));
                paymentMovement.InitialBalance = movementInfo.InitialBalance;
                paymentMovement.FinalBalance = movementInfo.FinalBalance;
                paymentMovement.InitialBalanceEur = movementInfo.InitialBalanceEur;
                paymentMovement.FinalBalanceEur = movementInfo.FinalBalanceEur;
                paymentMovement.ValueEur = movementInfo.ValueEur;
                paymentMovement.Value = movementInfo.Value;

                toReturn.TotalValue.InitialBalance = paymentMovementInfos[0].InitialBalance;
                toReturn.TotalValueEur.InitialBalance = paymentMovementInfos[0].InitialBalanceEur;
                toReturn.TotalValue.FinalBalance = paymentMovementInfos.Last().FinalBalance;
                toReturn.TotalValueEur.FinalBalance = paymentMovementInfos.Last().FinalBalanceEur;

                if (paymentMovement.IsIncrease) {
                    toReturn.TotalValue.Receipts += movementInfo.Value;
                    toReturn.TotalValueEur.Receipts += movementInfo.ValueEur;
                } else {
                    toReturn.TotalValue.Expense += movementInfo.Value;
                    toReturn.TotalValueEur.Expense += movementInfo.ValueEur;
                }

                return paymentMovement;
            }, new { NetId = netId, From = from, To = to, Limit = limit, Offset = offset }
        ).OrderByDescending(x => x.Created).ToList();

        return toReturn;
    }

    public Dictionary<string, decimal> GetTotalBalanceByCurrency() {
        return _connection.Query<string, decimal?, KeyValuePair<string, decimal?>>(
            "SELECT " +
            "'Total' AS [Code] " +
            ",CONVERT(money, SUM( " +
            "dbo.GetExchangedToEuroValue( " +
            "[PaymentCurrencyRegister].[Amount] " +
            ",[PaymentCurrencyRegister].[CurrencyID] " +
            ",GETUTCDATE() " +
            "))) AS [Amount] " +
            "FROM [PaymentRegister] " +
            "LEFT JOIN [PaymentCurrencyRegister] " +
            "ON [PaymentCurrencyRegister].[PaymentRegisterID] = [PaymentRegister].[ID] " +
            "WHERE [PaymentRegister].[Deleted] = 0 " +
            "AND [PaymentCurrencyRegister].[Deleted] = 0 " +
            "UNION " +
            "SELECT " +
            "[Currency].[Code] " +
            ",CONVERT(money, SUM([PaymentCurrencyRegister].[Amount])) AS [Amount] " +
            "FROM [PaymentRegister] " +
            "LEFT JOIN [PaymentCurrencyRegister] " +
            "ON [PaymentCurrencyRegister].[PaymentRegisterID] = [PaymentRegister].[ID] " +
            "LEFT JOIN [Currency] " +
            "ON [Currency].[ID] = [PaymentCurrencyRegister].[CurrencyID] " +
            "WHERE [PaymentRegister].[Deleted] = 0 " +
            "AND [PaymentCurrencyRegister].[Deleted] = 0 " +
            "GROUP BY [Currency].[Code] ",
            (key, value) =>
                new KeyValuePair<string, decimal?>(key, value), splitOn: "Code,Amount").ToDictionary(
            k => k.Key,
            k => k.Value ?? 0);
    }

    public TotalCurrencyRegisters GetStatePaymentByPeriod(DateTime from, DateTime to) {
        TotalCurrencyRegisters toReturn =
            new() {
                CurrencyRegisters = new List<CurrencyRegisterStateByPeriod>(),
                TotalValueEur = new TotalValueByPeriod()
            };

        Type[] initialTypes = {
            typeof(decimal),
            typeof(decimal),
            typeof(PaymentCurrencyRegister),
            typeof(Currency),
            typeof(PaymentRegister)
        };

        Func<object[], decimal> initialMapper = objects => {
            decimal initialBalance = (decimal)objects[0];
            decimal initialBalanceEur = (decimal)objects[1];
            PaymentCurrencyRegister currencyRegister = (PaymentCurrencyRegister)objects[2];
            Currency currency = (Currency)objects[3];
            PaymentRegister paymentRegister = (PaymentRegister)objects[4];

            if (!toReturn.CurrencyRegisters.Any(x => x.PaymentCurrencyRegister.Id.Equals(currencyRegister.Id)))
                toReturn.CurrencyRegisters.Add(new CurrencyRegisterStateByPeriod {
                    PaymentCurrencyRegister = currencyRegister,
                    TotalValue = new TotalValueByPeriod {
                        InitialBalance = initialBalance,
                        FinalBalance = initialBalance
                    },
                    TotalValueEur = new TotalValueByPeriod {
                        InitialBalance = initialBalanceEur,
                        FinalBalance = initialBalanceEur
                    }
                });

            toReturn.TotalValueEur.InitialBalance += initialBalanceEur;
            toReturn.TotalValueEur.FinalBalance += initialBalanceEur;

            currencyRegister.Currency = currency;
            currencyRegister.PaymentRegister = paymentRegister;

            return initialBalance;
        };

        _connection.Query(
            ";WITH [PAYMENT_STATE] AS ( " +
            "SELECT " +
            "[PaymentCurrencyRegister].[ID] AS [ID] " +
            ", SUM([IncomePaymentOrder].[Amount]) AS [Value] " +
            ", SUM(dbo.GetExchangedToEuroValue( " +
            "[IncomePaymentOrder].[Amount] " +
            ", [IncomePaymentOrder].[CurrencyID] " +
            ", [IncomePaymentOrder].[Created] " +
            ")) AS [ValueEur] " +
            "FROM [IncomePaymentOrder] " +
            "LEFT JOIN [PaymentRegister] " +
            "ON [PaymentRegister].[ID] = [IncomePaymentOrder].[PaymentRegisterID] " +
            "LEFT JOIN [PaymentCurrencyRegister] " +
            "ON [PaymentCurrencyRegister].[PaymentRegisterID] = [PaymentRegister].[ID] " +
            "AND [PaymentCurrencyRegister].[CurrencyID] = [IncomePaymentOrder].[CurrencyID] " +
            "WHERE [IncomePaymentOrder].[Deleted] = 0 " +
            "AND [IncomePaymentOrder].[Created] < @From " +
            "GROUP BY [PaymentCurrencyRegister].[ID] " +
            "UNION " +
            "SELECT " +
            "[OutcomePaymentOrder].[PaymentCurrencyRegisterID] AS [ID] " +
            ", SUM([OutcomePaymentOrder].[Amount]) * -1 AS [Value] " +
            ", SUM(dbo.GetExchangedToEuroValue( " +
            "[OutcomePaymentOrder].[Amount] " +
            ", [PaymentCurrencyRegister].[CurrencyID] " +
            ", [OutcomePaymentOrder].[Created] " +
            ")) * -1 AS [ValueEur] " +
            "FROM [OutcomePaymentOrder] " +
            "LEFT JOIN [PaymentCurrencyRegister] " +
            "ON [PaymentCurrencyRegister].[ID] = [OutcomePaymentOrder].[PaymentCurrencyRegisterID] " +
            "WHERE [OutcomePaymentOrder].[Deleted] = 0 " +
            "AND [OutcomePaymentOrder].[Created] < @From " +
            "GROUP BY [OutcomePaymentOrder].[PaymentCurrencyRegisterID] " +
            "UNION " +
            "SELECT " +
            "[PaymentRegisterTransfer].[FromPaymentCurrencyRegisterID] AS [ID] " +
            ", SUM([PaymentRegisterTransfer].[Amount]) * -1 AS [Value] " +
            ", SUM(dbo.GetExchangedToEuroValue( " +
            "[PaymentRegisterTransfer].[Amount] " +
            ", [PaymentCurrencyRegister].[CurrencyID] " +
            ", [PaymentRegisterTransfer].[Created] " +
            ")) * -1 AS [ValueEur] " +
            "FROM [PaymentRegisterTransfer] " +
            "LEFT JOIN [PaymentCurrencyRegister] " +
            "ON [PaymentCurrencyRegister].[ID] = [PaymentRegisterTransfer].[FromPaymentCurrencyRegisterID] " +
            "WHERE [PaymentRegisterTransfer].[Deleted] = 0 " +
            "AND [PaymentRegisterTransfer].[Created] < @From " +
            "GROUP BY [PaymentRegisterTransfer].[FromPaymentCurrencyRegisterID] " +
            "UNION " +
            "SELECT " +
            "[PaymentRegisterTransfer].[ToPaymentCurrencyRegisterID] AS [ID] " +
            ",SUM([PaymentRegisterTransfer].[Amount]) AS [Value] " +
            ", SUM(dbo.GetExchangedToEuroValue( " +
            "[PaymentRegisterTransfer].[Amount] " +
            ", [PaymentCurrencyRegister].[CurrencyID] " +
            ", [PaymentRegisterTransfer].[Created] " +
            ")) AS [ValueEur] " +
            "FROM [PaymentRegisterTransfer] " +
            "LEFT JOIN [PaymentCurrencyRegister] " +
            "ON [PaymentCurrencyRegister].[ID] = [PaymentRegisterTransfer].[ToPaymentCurrencyRegisterID] " +
            "WHERE [PaymentRegisterTransfer].[Deleted] = 0 " +
            "AND [PaymentRegisterTransfer].[Created] < @From " +
            "GROUP BY [PaymentRegisterTransfer].[ToPaymentCurrencyRegisterID] " +
            ") " +
            ",[GROUPED_PAYMENT_LIST] AS ( " +
            "SELECT " +
            "[PAYMENT_STATE].[ID] AS [ID] " +
            ", SUM([PAYMENT_STATE].[Value]) AS [Value] " +
            ", SUM([PAYMENT_STATE].[ValueEur]) AS [ValueEur] " +
            "FROM [PAYMENT_STATE] " +
            "GROUP BY [PAYMENT_STATE].[ID] " +
            ") " +
            "SELECT " +
            "[PaymentCurrencyRegister].[InitialAmount] + " +
            "CASE " +
            "WHEN [GROUPED_PAYMENT_LIST].[Value] IS NULL " +
            "THEN 0 " +
            "ELSE [GROUPED_PAYMENT_LIST].[Value] " +
            "END AS [InitialBalance] " +
            ", dbo.GetExchangedToEuroValue( " +
            "[PaymentCurrencyRegister].[InitialAmount] " +
            ",[PaymentCurrencyRegister].[CurrencyID] " +
            ",[PaymentCurrencyRegister].[Created]) + " +
            "CASE " +
            "WHEN [GROUPED_PAYMENT_LIST].[ValueEur] IS NULL " +
            "THEN 0 " +
            "ELSE [GROUPED_PAYMENT_LIST].[ValueEur] " +
            "END AS [InitialBalanceEur] " +
            ", [PaymentCurrencyRegister].* " +
            ", [Currency].* " +
            ", [PaymentRegister].* " +
            "FROM [PaymentCurrencyRegister] " +
            "LEFT JOIN [GROUPED_PAYMENT_LIST] " +
            "ON [GROUPED_PAYMENT_LIST].[ID] = [PaymentCurrencyRegister].[ID] " +
            "LEFT JOIN [Currency] " +
            "ON [Currency].[ID] = [PaymentCurrencyRegister].[CurrencyID] " +
            "LEFT JOIN [PaymentRegister] " +
            "ON [PaymentRegister].[ID] = [PaymentCurrencyRegister].[PaymentRegisterID] " +
            "WHERE [PaymentCurrencyRegister].[Deleted] = 0 ",
            initialTypes, initialMapper,
            new { From = from },
            splitOn: "InitialBalance,InitialBalanceEur,ID,ID,ID");

        _connection.Query<long, bool, decimal, decimal, long>(
            ";WITH [PAYMENT_STATE] AS ( " +
            "SELECT " +
            "[PaymentCurrencyRegister].[ID] AS [ID] " +
            ", SUM([IncomePaymentOrder].[Amount]) AS [Value] " +
            ", SUM(dbo.GetExchangedToEuroValue( " +
            "[IncomePaymentOrder].[Amount] " +
            ", [IncomePaymentOrder].[CurrencyID] " +
            ", [IncomePaymentOrder].[Created] " +
            ")) AS [ValueEur] " +
            ", 1 AS [IsIncrease] " +
            "FROM [IncomePaymentOrder] " +
            "LEFT JOIN [PaymentRegister] " +
            "ON [PaymentRegister].[ID] = [IncomePaymentOrder].[PaymentRegisterID] " +
            "LEFT JOIN [PaymentCurrencyRegister] " +
            "ON [PaymentCurrencyRegister].[PaymentRegisterID] = [PaymentRegister].[ID] " +
            "AND [PaymentCurrencyRegister].[CurrencyID] = [IncomePaymentOrder].[CurrencyID] " +
            "WHERE [IncomePaymentOrder].[Deleted] = 0 " +
            "AND [IncomePaymentOrder].[Created] >= @From " +
            "AND [IncomePaymentOrder].[Created] <= @To " +
            "GROUP BY [PaymentCurrencyRegister].[ID] " +
            "UNION " +
            "SELECT " +
            "[OutcomePaymentOrder].[PaymentCurrencyRegisterID] AS [ID] " +
            ", SUM([OutcomePaymentOrder].[Amount]) AS [Value] " +
            ", SUM(dbo.GetExchangedToEuroValue( " +
            "[OutcomePaymentOrder].[Amount] " +
            ", [PaymentCurrencyRegister].[CurrencyID] " +
            ", [OutcomePaymentOrder].[Created] " +
            ")) AS [ValueEur] " +
            ", 0 AS [IsIncrease] " +
            "FROM [OutcomePaymentOrder] " +
            "LEFT JOIN [PaymentCurrencyRegister] " +
            "ON [PaymentCurrencyRegister].[ID] = [OutcomePaymentOrder].[PaymentCurrencyRegisterID] " +
            "WHERE [OutcomePaymentOrder].[Deleted] = 0 " +
            "AND [OutcomePaymentOrder].[Created] >= @From " +
            "AND [OutcomePaymentOrder].[Created] <= @To " +
            "GROUP BY [OutcomePaymentOrder].[PaymentCurrencyRegisterID] " +
            "UNION " +
            "SELECT " +
            "[PaymentRegisterTransfer].[FromPaymentCurrencyRegisterID] AS [ID] " +
            ", SUM([PaymentRegisterTransfer].[Amount]) AS [Value] " +
            ", SUM(dbo.GetExchangedToEuroValue( " +
            "[PaymentRegisterTransfer].[Amount] " +
            ", [PaymentCurrencyRegister].[CurrencyID] " +
            ", [PaymentRegisterTransfer].[Created] " +
            ")) AS [ValueEur] " +
            ", 0 AS [IsIncrease] " +
            "FROM [PaymentRegisterTransfer] " +
            "LEFT JOIN [PaymentCurrencyRegister] " +
            "ON [PaymentCurrencyRegister].[ID] = [PaymentRegisterTransfer].[FromPaymentCurrencyRegisterID] " +
            "WHERE [PaymentRegisterTransfer].[Deleted] = 0 " +
            "AND [PaymentRegisterTransfer].[Created] >= @From " +
            "AND [PaymentRegisterTransfer].[Created] <= @To " +
            "GROUP BY [PaymentRegisterTransfer].[FromPaymentCurrencyRegisterID] " +
            "UNION " +
            "SELECT " +
            "[PaymentRegisterTransfer].[ToPaymentCurrencyRegisterID] AS [ID] " +
            ",SUM([PaymentRegisterTransfer].[Amount]) AS [Value] " +
            ", SUM(dbo.GetExchangedToEuroValue( " +
            "[PaymentRegisterTransfer].[Amount] " +
            ", [PaymentCurrencyRegister].[CurrencyID] " +
            ", [PaymentRegisterTransfer].[Created] " +
            ")) AS [ValueEur] " +
            ", 1 AS [IsIncrease] " +
            "FROM [PaymentRegisterTransfer] " +
            "LEFT JOIN [PaymentCurrencyRegister] " +
            "ON [PaymentCurrencyRegister].[ID] = [PaymentRegisterTransfer].[ToPaymentCurrencyRegisterID] " +
            "WHERE [PaymentRegisterTransfer].[Deleted] = 0 " +
            "AND [PaymentRegisterTransfer].[Created] >= @From " +
            "AND [PaymentRegisterTransfer].[Created] <= @To " +
            "GROUP BY [PaymentRegisterTransfer].[ToPaymentCurrencyRegisterID] " +
            ") " +
            "SELECT " +
            "[PAYMENT_STATE].[ID] AS [ID] " +
            ", CONVERT(bit, [PAYMENT_STATE].[IsIncrease]) AS [IsIncrease] " +
            ", SUM([PAYMENT_STATE].[Value]) AS [Value] " +
            ", SUM([PAYMENT_STATE].[ValueEur]) AS [ValueEur] " +
            "FROM [PAYMENT_STATE] " +
            "GROUP BY [PAYMENT_STATE].[ID] " +
            ", [PAYMENT_STATE].[IsIncrease] ",
            (id, isIncrease, value, valueEur) => {
                CurrencyRegisterStateByPeriod currencyPayment =
                    toReturn.CurrencyRegisters.First(x => x.PaymentCurrencyRegister.Id.Equals(id));

                if (isIncrease) {
                    currencyPayment.TotalValue.Receipts = value;
                    currencyPayment.TotalValue.FinalBalance += value;
                    currencyPayment.TotalValueEur.Receipts = valueEur;
                    currencyPayment.TotalValueEur.FinalBalance += valueEur;

                    toReturn.TotalValueEur.Receipts += valueEur;
                    toReturn.TotalValueEur.FinalBalance += valueEur;
                } else {
                    currencyPayment.TotalValue.Expense = value;
                    currencyPayment.TotalValue.FinalBalance -= value;
                    currencyPayment.TotalValueEur.Expense = valueEur;
                    currencyPayment.TotalValueEur.FinalBalance -= valueEur;

                    toReturn.TotalValueEur.Expense += valueEur;
                    toReturn.TotalValueEur.FinalBalance -= valueEur;
                }

                return id;
            },
            new { From = from, To = to },
            splitOn: "ID,IsIncrease,Value,ValueEur");

        return toReturn;
    }

    public void UpdateAllNotMainByOrganizationId(long id) {
        _connection.Execute(
            "UPDATE [PaymentRegister] " +
            "SET [Updated] = getutcdate() " +
            ", [PaymentRegister].[IsMain] = 0 " +
            "WHERE [PaymentRegister].[OrganizationID] = @Id; ", new { Id = id });
    }

    public void UpdateIsMainById(long id) {
        _connection.Execute(
            "UPDATE [PaymentRegister] " +
            "SET [Updated] = getutcdate() " +
            ", [PaymentRegister].[IsMain] = 1 " +
            "WHERE [PaymentRegister].[ID] = @Id; ", new { Id = id });
    }

    public PaymentRegister GetMainPaymentRegisterByOrganization(long organizationId) {
        return _connection.Query<PaymentRegister>(
            "SELECT * FROM [PaymentRegister] " +
            "WHERE [PaymentRegister].[OrganizationID] = @Id " +
            "AND [PaymentRegister].[IsMain] = 1; ",
            new { Id = organizationId }).FirstOrDefault();
    }

    public List<PaymentRegister> GetAllByBank(string bank) {
        List<PaymentRegister> toReturn = new();

        _connection.Query<PaymentRegister, PaymentCurrencyRegister, Currency, PaymentRegister>(
            "SELECT * FROM [PaymentRegister] " +
            "LEFT JOIN [PaymentCurrencyRegister] " +
            "ON [PaymentCurrencyRegister].[PaymentRegisterID] = [PaymentRegister].[ID] " +
            "AND [PaymentCurrencyRegister].[Deleted] = 0 " +
            "LEFT JOIN [Currency] " +
            "ON [Currency].[ID] = [PaymentCurrencyRegister].[CurrencyID] " +
            "WHERE [PaymentRegister].BankName = @Bank " +
            "AND [PaymentRegister].[Deleted] = 0 ",
            (paymentRegister, currencyRegister, currency) => {
                if (!toReturn.Any(x => x.Id == paymentRegister.Id))
                    toReturn.Add(paymentRegister);
                else
                    paymentRegister = toReturn.First(x => x.Id == paymentRegister.Id);

                if (currencyRegister != null) {
                    currencyRegister.Currency = currency;

                    paymentRegister.PaymentCurrencyRegisters.Add(currencyRegister);
                }

                return paymentRegister;
            },
            new { Bank = bank }
        );

        return toReturn;
    }
}