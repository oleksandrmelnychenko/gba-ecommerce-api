using System;
using System.Linq;
using GBA.Common.Helpers;

namespace GBA.Common.Extensions;

public static class NumberToLetterConverterExtensionMethod {
    public static string ToText(this decimal num, bool firstCapital, bool toUkrainianText, bool isMale = true) {
        return toUkrainianText ? NuberToUkrainianTextConverter.Convert(num, firstCapital, isMale) : NuberToPolandTextConverter.Convert(num, firstCapital);
    }

    public static string ToCompleteText(this decimal amount, string currencyCode, bool isFractionalPartToText, bool isUpperLetter, bool toUkrainianText) {
        int fullNumberMainPart = Convert.ToInt32(Math.Truncate(amount));
        int endNumberMainPart = Convert.ToInt32(fullNumberMainPart.ToString().Last().ToString());

        bool isMale = !(endNumberMainPart == 1 || endNumberMainPart == 2);

        string fullNumberToCompleteText = amount.ToText(isUpperLetter, toUkrainianText, isMale);

        string keyWordMainPart;

        if (toUkrainianText)
            switch (currencyCode) {
                case "UAH":
                    if (fullNumberMainPart > 10 && fullNumberMainPart < 20)
                        keyWordMainPart = "гривень";
                    else
                        switch (endNumberMainPart) {
                            case 1:
                                keyWordMainPart = "гривня";
                                break;
                            case 2:
                            case 3:
                            case 4:
                                keyWordMainPart = "гривні";
                                break;
                            default:
                                keyWordMainPart = "гривень";
                                break;
                        }

                    break;
                case "PLN":
                    if (fullNumberMainPart > 10 && fullNumberMainPart < 20)
                        keyWordMainPart = "злотих";
                    else
                        switch (endNumberMainPart) {
                            case 1:
                                keyWordMainPart = "злотий";
                                break;
                            case 2:
                            case 3:
                            case 4:
                            default:
                                keyWordMainPart = "злотих";
                                break;
                        }

                    break;
                case "USD":
                    if (fullNumberMainPart > 10 && fullNumberMainPart < 20)
                        keyWordMainPart = "доларів";
                    else
                        switch (endNumberMainPart) {
                            case 1:
                                keyWordMainPart = "доллар";
                                break;
                            case 2:
                            case 3:
                            case 4:
                                keyWordMainPart = "доллара";
                                break;
                            default:
                                keyWordMainPart = "доларів";
                                break;
                        }

                    break;
                case "EUR":
                default:
                    keyWordMainPart = "євро";
                    break;
            }
        else
            switch (currencyCode) {
                case "UAH":
                    if (fullNumberMainPart > 10 && fullNumberMainPart < 20)
                        keyWordMainPart = "hrywien";
                    else
                        switch (endNumberMainPart) {
                            case 1:
                                keyWordMainPart = "hrywna";
                                break;
                            case 2:
                            case 3:
                            case 4:
                                keyWordMainPart = "hrywien";
                                break;
                            default:
                                keyWordMainPart = "hrywien";
                                break;
                        }

                    break;
                case "PLN":
                    if (fullNumberMainPart > 10 && fullNumberMainPart < 20)
                        keyWordMainPart = "złotych";
                    else
                        switch (endNumberMainPart) {
                            case 1:
                                keyWordMainPart = "zł";
                                break;
                            case 2:
                            case 3:
                            case 4:
                            default:
                                keyWordMainPart = "złotych";
                                break;
                        }

                    break;
                case "USD":
                    if (fullNumberMainPart > 10 && fullNumberMainPart < 20)
                        keyWordMainPart = "dolarów";
                    else
                        switch (endNumberMainPart) {
                            case 1:
                                keyWordMainPart = "dolar";
                                break;
                            case 2:
                            case 3:
                            case 4:
                                keyWordMainPart = "dolary";
                                break;
                            default:
                                keyWordMainPart = "dolarów";
                                break;
                        }

                    break;
                case "EUR":
                default:
                    keyWordMainPart = "euro";
                    break;
            }


        fullNumberToCompleteText += " " + keyWordMainPart;

        int fractionalPart = Convert.ToInt32(Math.Round(amount % 1, 2) * 100);
        int endNumberFractionalPart = Convert.ToInt32(fractionalPart.ToString().Last().ToString());

        bool isEndFractionMale = !(endNumberFractionalPart == 1 || endNumberFractionalPart == 2);

        if (isFractionalPartToText)
            fullNumberToCompleteText += " " + ToText(fractionalPart, false, toUkrainianText, isEndFractionMale);
        else
            fullNumberToCompleteText += " " + fractionalPart;

        string keyWordFractionalPart;

        if (toUkrainianText)
            switch (currencyCode) {
                case "UAH":
                    if (fractionalPart > 10 && fractionalPart < 20)
                        keyWordFractionalPart = "копійок";
                    else
                        switch (endNumberFractionalPart) {
                            case 1:
                                keyWordFractionalPart = "копійка";
                                break;
                            case 2:
                            case 3:
                            case 4:
                                keyWordFractionalPart = "копійки";
                                break;
                            default:
                                keyWordFractionalPart = "копійок";
                                break;
                        }

                    break;
                case "PLN":
                    if (fractionalPart > 10 && fractionalPart < 20)
                        keyWordFractionalPart = "грошів";
                    else
                        switch (endNumberFractionalPart) {
                            case 1:
                                keyWordFractionalPart = "грош";
                                break;
                            case 2:
                            case 3:
                            case 4:
                                keyWordFractionalPart = "гроша";
                                break;
                            default:
                                keyWordFractionalPart = "грошів";
                                break;
                        }

                    break;
                case "USD":
                    if (fractionalPart > 10 && fractionalPart < 20)
                        keyWordFractionalPart = "центів";
                    else
                        switch (endNumberFractionalPart) {
                            case 1:
                                keyWordFractionalPart = "цент";
                                break;
                            case 2:
                            case 3:
                            case 4:
                                keyWordFractionalPart = "цента";
                                break;
                            default:
                                keyWordFractionalPart = "центів";
                                break;
                        }

                    break;
                case "EUR":
                    if (fractionalPart > 10 && fractionalPart < 20)
                        keyWordFractionalPart = "центів";
                    else
                        switch (endNumberFractionalPart) {
                            case 1:
                                keyWordFractionalPart = "цент";
                                break;
                            case 2:
                            case 3:
                            case 4:
                                keyWordFractionalPart = "цента";
                                break;
                            default:
                                keyWordFractionalPart = "центів";
                                break;
                        }

                    break;
                default:
                    keyWordFractionalPart = "центів";
                    break;
            }
        else
            switch (currencyCode) {
                case "UAH":
                    if (fractionalPart > 10 && fractionalPart < 20)
                        keyWordFractionalPart = "kopiejek";
                    else
                        switch (endNumberFractionalPart) {
                            case 1:
                                keyWordFractionalPart = "kopiejeki";
                                break;
                            case 2:
                            case 3:
                            case 4:
                                keyWordFractionalPart = "kopiejeki";
                                break;
                            default:
                                keyWordFractionalPart = "kopiejek";
                                break;
                        }

                    break;
                case "PLN":
                    keyWordFractionalPart = "grosz";
                    break;
                case "USD":
                case "EUR":
                    if (fractionalPart > 10 && fractionalPart < 20)
                        keyWordFractionalPart = "centów";
                    else
                        switch (endNumberFractionalPart) {
                            case 1:
                                keyWordFractionalPart = "cent";
                                break;
                            case 2:
                            case 3:
                            case 4:
                                keyWordFractionalPart = "centy";
                                break;
                            default:
                                keyWordFractionalPart = "centów";
                                break;
                        }

                    break;
                default:
                    keyWordFractionalPart = "centów";
                    break;
            }

        fullNumberToCompleteText += " " + keyWordFractionalPart;

        return fullNumberToCompleteText;
    }
}