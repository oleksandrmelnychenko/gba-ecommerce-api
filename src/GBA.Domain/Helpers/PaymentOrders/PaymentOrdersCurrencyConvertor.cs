using System;
using GBA.Domain.Entities;
using GBA.Domain.Entities.ExchangeRates;
using GBA.Domain.Repositories.ExchangeRates.Contracts;

namespace GBA.Domain.Helpers.PaymentOrders;

public sealed class PaymentOrdersCurrencyConvertor {
    private const string UAH_CODE = "uah";
    private const string PLN_CODE = "pln";
    private const string EUR_CODE = "eur";
    private const string USD_CODE = "usd";
    private readonly ICrossExchangeRateRepository _crossExchangeRateRepository;

    private readonly Currency _euro;
    private readonly IExchangeRateRepository _exchangeRateRepository;
    private readonly DateTime _fromDate;
    private readonly Currency _paymentCurrency;

    public PaymentOrdersCurrencyConvertor(
        Currency euro,
        Currency paymentCurrency,
        DateTime fromDate,
        IExchangeRateRepository exchangeRateRepository,
        ICrossExchangeRateRepository crossExchangeRateRepository) {
        _euro = euro;
        _paymentCurrency = paymentCurrency;
        _fromDate = fromDate;
        _exchangeRateRepository = exchangeRateRepository;
        _crossExchangeRateRepository = crossExchangeRateRepository;
    }

    public decimal ConvertAmountToEuro(decimal amount) {
        decimal inEuroAmount = decimal.Zero;

        if (_euro == null) return inEuroAmount;

        if (_paymentCurrency.Id.Equals(_euro.Id)) {
            inEuroAmount = amount;
        } else if (_paymentCurrency.Code.ToLower().Equals(UAH_CODE) || _paymentCurrency.Code.ToLower().Equals(PLN_CODE)) {
            ExchangeRate exchangeRate = _exchangeRateRepository
                .GetByCurrencyIdAndCode(
                    _paymentCurrency.Id,
                    _euro.Code,
                    TimeZoneInfo.ConvertTimeToUtc(_fromDate));

            inEuroAmount = Math.Round(amount / exchangeRate.Amount, 4, MidpointRounding.AwayFromZero);
        } else {
            CrossExchangeRate crossExchangeRate =
                _crossExchangeRateRepository
                    .GetByCurrenciesIds(
                        _paymentCurrency.Id,
                        _euro.Id,
                        TimeZoneInfo.ConvertTimeToUtc(_fromDate))
                ??
                _crossExchangeRateRepository.GetByCurrenciesIds(
                    _euro.Id,
                    _paymentCurrency.Id,
                    TimeZoneInfo.ConvertTimeToUtc(_fromDate));

            inEuroAmount = _paymentCurrency.Code.ToLower().Equals(EUR_CODE)
                ? Math.Round(amount * crossExchangeRate?.Amount ?? 1m, 4, MidpointRounding.AwayFromZero)
                : Math.Round(amount / crossExchangeRate?.Amount ?? 1m, 4, MidpointRounding.AwayFromZero);
        }

        return inEuroAmount;
    }

    public AgreementConversionResult GetConvertedAmountToAgreementCurrency(decimal amount, decimal exchangeRateAmount, Currency agreementCurrency) {
        decimal inAgreementCurrencyAmount = decimal.Zero;

        if (exchangeRateAmount > 0)
            inAgreementCurrencyAmount = ConvertByPredefinedExchangeRate(amount, exchangeRateAmount, agreementCurrency);
        else
            (inAgreementCurrencyAmount, exchangeRateAmount) = ConvertIfExchangeRateNotSpecified(amount, agreementCurrency);

        return new AgreementConversionResult(inAgreementCurrencyAmount, exchangeRateAmount);
    }

    private decimal ConvertByPredefinedExchangeRate(decimal amount, decimal exchangeRateAmount, Currency agreementCurrency) {
        decimal inAgreementCurrencyAmount = decimal.Zero;

        switch (_paymentCurrency.Code.ToLower()) {
            case USD_CODE when agreementCurrency.Code.ToLower().Equals(EUR_CODE):
            case UAH_CODE:
            case PLN_CODE when !agreementCurrency.Code.ToLower().Equals(UAH_CODE):
                inAgreementCurrencyAmount = Math.Round(amount / exchangeRateAmount, 4, MidpointRounding.AwayFromZero);
                break;
            default:
                inAgreementCurrencyAmount = Math.Round(amount * exchangeRateAmount, 4, MidpointRounding.AwayFromZero);
                break;
        }

        return inAgreementCurrencyAmount;
    }

    private Tuple<decimal, decimal> ConvertIfExchangeRateNotSpecified(decimal amount, Currency agreementCurrency) {
        decimal inAgreementCurrencyAmount = decimal.Zero;
        decimal exchangeRateAmount = decimal.Zero;

        if (_paymentCurrency.Id.Equals(agreementCurrency.Id)) {
            inAgreementCurrencyAmount = amount;
        } else if (_paymentCurrency.Code.ToLower().Equals(UAH_CODE) || _paymentCurrency.Code.ToLower().Equals(PLN_CODE)) {
            ExchangeRate exchangeRate =
                _exchangeRateRepository
                    .GetByCurrencyIdAndCode(
                        _paymentCurrency.Id,
                        agreementCurrency.Code,
                        TimeZoneInfo.ConvertTimeToUtc(_fromDate))
                ??
                _exchangeRateRepository
                    .GetByCurrencyIdAndCode(
                        agreementCurrency.Id,
                        _paymentCurrency.Code,
                        TimeZoneInfo.ConvertTimeToUtc(_fromDate));

            exchangeRateAmount = exchangeRate?.Amount ?? 1m;
            inAgreementCurrencyAmount = Math.Round(amount / exchangeRate?.Amount ?? 1m, 4, MidpointRounding.AwayFromZero);
        } else {
            CrossExchangeRate crossExchangeRate =
                _crossExchangeRateRepository.GetByCurrenciesIds(
                    _paymentCurrency.Id,
                    agreementCurrency.Id,
                    TimeZoneInfo.ConvertTimeToUtc(_fromDate))
                ??
                _crossExchangeRateRepository.GetByCurrenciesIds(
                    agreementCurrency.Id,
                    _paymentCurrency.Id,
                    TimeZoneInfo.ConvertTimeToUtc(_fromDate));

            exchangeRateAmount = crossExchangeRate?.Amount ?? 1m;

            inAgreementCurrencyAmount = agreementCurrency.Code.ToLower().Equals(USD_CODE)
                ? Math.Round(amount * crossExchangeRate?.Amount ?? 1m, 4, MidpointRounding.AwayFromZero)
                : Math.Round(amount / crossExchangeRate?.Amount ?? 1m, 4, MidpointRounding.AwayFromZero);
        }

        return new Tuple<decimal, decimal>(inAgreementCurrencyAmount, exchangeRateAmount);
    }

    public decimal GetAgreementCurrencyToEuroExchangeRate(Currency agreementCurrency) {
        decimal agreementToEuroExchangeRate = decimal.Zero;

        if (agreementCurrency.Id.Equals(_euro.Id)) {
            agreementToEuroExchangeRate = 1m;
        } else {
            ExchangeRate exchangeRate =
                _exchangeRateRepository
                    .GetByCurrencyIdAndCode(
                        agreementCurrency.Id,
                        _euro.Code,
                        TimeZoneInfo.ConvertTimeToUtc(_fromDate)
                    );

            if (exchangeRate != null) {
                agreementToEuroExchangeRate = exchangeRate.Amount;
            } else {
                CrossExchangeRate crossExchangeRate =
                    _crossExchangeRateRepository
                        .GetByCurrenciesIds(
                            agreementCurrency.Id,
                            _euro.Id,
                            TimeZoneInfo.ConvertTimeToUtc(_fromDate)
                        );

                if (crossExchangeRate != null) {
                    agreementToEuroExchangeRate = decimal.Zero - crossExchangeRate.Amount; // WTF?
                } else {
                    crossExchangeRate =
                        _crossExchangeRateRepository
                            .GetByCurrenciesIds(
                                _euro.Id,
                                agreementCurrency.Id,
                                TimeZoneInfo.ConvertTimeToUtc(_fromDate)
                            );

                    agreementToEuroExchangeRate = crossExchangeRate?.Amount ?? 1m;
                }
            }
        }

        return agreementToEuroExchangeRate;
    }
}