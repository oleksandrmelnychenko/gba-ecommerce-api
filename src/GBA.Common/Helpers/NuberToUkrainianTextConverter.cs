using System;

namespace GBA.Common.Helpers;

public static class NuberToUkrainianTextConverter {
    private static readonly string zero = "нуль";
    private static readonly string firstMale = "один";
    private static readonly string firstFemale = "одна";
    private static readonly string firstFemaleAccusative = "одну";
    private static readonly string firstMaleGenetive = "одне";
    private static readonly string secondMale = "два";
    private static readonly string secondFemale = "дві";
    private static readonly string secondMaleGenetive = "двох";
    private static readonly string secondFemaleGenetive = "двох";

    private static readonly string[] from3till19 = {
        "", "три", "чотири", "п'ять", "шість",
        "сім", "вісім", "дев'ять", "десять", "одинадцять",
        "дванадцять", "тринадцять", "чотирнадцять", "п'ятнадцять",
        "шістнадцять", "сімнадцять", "вісімнадцять", "дев'ятнадцять"
    };

    private static readonly string[] from3till19Genetive = {
        "", "трьох", "чотирьох", "п'яти", "шести",
        "семи", "восьми", "дев'яти", "десяти", "одинадцяти",
        "дванадцяти", "тринадцяти", "чотирнадцяти", "п'ятнадцяти",
        "шістнадцяти", "сімнадцяти", "вісімнадцяти", "дев'ятнадцяти"
    };

    private static readonly string[] tens = {
        "", "двадцять", "тридцять", "сорок", "п'ятдесят",
        "шістдесят", "сімдесят", "вісімдесят", "дев'яносто"
    };

    private static readonly string[] tensGenetive = {
        "", "двадцяти", "тридцяти", "сорока", "п'ятдесяти",
        "шістдесяти", "сімдесяти", "восьмидесяти", "дев'яноста"
    };

    private static readonly string[] hundreds = {
        "", "сто", "двісті", "триста", "чотириста",
        "п'ятсот", "шістсот", "сімсот", "вісімсот", "дев'ятсот"
    };

    private static readonly string[] hundredsGenetive = {
        "", "ста", "двохсот", "трьохсот", "чотирьохсот",
        "п'ятисот", "шестисот", "семисот", "восьмисот", "дев'ятисот"
    };

    private static readonly string[] thousands = {
        "", "тисяча", "тисячі", "тисяч"
    };

    private static readonly string[] thousandsAccusative = {
        "", "тисячу", "тисячі", "тисяч"
    };

    private static readonly string[] millions = {
        "", "мільйон", "мільйона", "мільйонів"
    };

    private static readonly string[] billions = {
        "", "мільярд", "мільярда", "мільярдів"
    };

    private static readonly string[] trillions = {
        "", "трильйон", "трильйона", "трильйонів"
    };

    public static string Convert(decimal amount, bool firstCapital, bool isMale) {
        long UAHAmount = (long)Math.Floor(amount);
        int lastUahDigit = lastDigit(UAHAmount);

        string s = NumeralsToTxt(UAHAmount, TextCases.Nominative, isMale, firstCapital) + " ";

        return s.Trim();
    }

    private static string MakeText(int _digits, string[] _hundreds, string[] _tens, string[] _from3till19, string _second, string _first, string[] _power) {
        string s = "";
        int digits = _digits;

        if (digits >= 100) {
            s += _hundreds[digits / 100] + " ";
            digits = digits % 100;
        }

        if (digits >= 20) {
            s += _tens[digits / 10 - 1] + " ";
            digits = digits % 10;
        }

        if (digits >= 3)
            s += _from3till19[digits - 2] + " ";
        else if (digits == 2)
            s += _second + " ";
        else if (digits == 1) s += _first + " ";

        if (_digits != 0 && _power.Length > 0) {
            digits = lastDigit(_digits);

            if (IsPluralGenitive(digits))
                s += _power[3] + " ";
            else if (IsSingularGenitive(digits))
                s += _power[2] + " ";
            else
                s += _power[1] + " ";
        }

        return s;
    }

    private static bool IsSingularGenitive(int _digits) {
        if (_digits >= 2 && _digits <= 4)
            return true;

        return false;
    }

    private static bool IsPluralGenitive(int _digits) {
        if (_digits >= 5 || _digits == 0)
            return true;

        return false;
    }

    public static string NumeralsToTxt(long _sourceNumber, TextCases _case, bool _isMale, bool _firstCapital) {
        string s = "";
        long number = _sourceNumber;
        int remainder;
        int power = 0;

        if (number >= (long)Math.Pow(10, 15) || number < 0) return "";

        while (number > 0) {
            remainder = (int)(number % 1000);
            number = number / 1000;

            switch (power) {
                case 12:
                    s = MakeText(remainder, hundreds, tens, from3till19, secondMale, firstMale, trillions) + s;
                    break;
                case 9:
                    s = MakeText(remainder, hundreds, tens, from3till19, secondMale, firstMale, billions) + s;
                    break;
                case 6:
                    s = MakeText(remainder, hundreds, tens, from3till19, secondMale, firstMale, millions) + s;
                    break;
                case 3:
                    switch (_case) {
                        case TextCases.Accusative:
                            s = MakeText(remainder, hundreds, tens, from3till19, secondFemale, firstFemaleAccusative, thousandsAccusative) + s;
                            break;
                        default:
                            s = MakeText(remainder, hundreds, tens, from3till19, secondFemale, firstFemale, thousands) + s;
                            break;
                    }

                    break;
                default:
                    string[] powerArray = { };
                    switch (_case) {
                        case TextCases.Genitive:
                            s = MakeText(remainder, hundredsGenetive, tensGenetive, from3till19Genetive, _isMale ? secondMaleGenetive : secondFemaleGenetive,
                                _isMale ? firstMaleGenetive : firstFemale, powerArray) + s;
                            break;
                        case TextCases.Accusative:
                            s = MakeText(remainder, hundreds, tens, from3till19, _isMale ? secondMale : secondFemale, _isMale ? firstMale : firstFemaleAccusative, powerArray) +
                                s;
                            break;
                        default:
                            s = MakeText(remainder, hundreds, tens, from3till19, _isMale ? secondMale : secondFemale, _isMale ? firstMale : firstFemale, powerArray) + s;
                            break;
                    }

                    break;
            }

            power += 3;
        }

        if (_sourceNumber == 0) s = zero + " ";

        if (s != "" && _firstCapital)
            s = s.Substring(0, 1).ToUpper() + s.Substring(1);

        return s.Trim();
    }

    private static int lastDigit(long _amount) {
        long amount = _amount;

        if (amount >= 100)
            amount = amount % 100;

        if (amount >= 20)
            amount = amount % 10;

        return (int)amount;
    }
}