using System;

namespace GBA.Common.Helpers;

public static class NuberToPolandTextConverter {
    private static readonly string zero = "zero";
    private static readonly string firstMale = "jeden";
    private static readonly string firstFemale = "jeden";
    private static readonly string firstFemaleAccusative = "jeden";
    private static readonly string firstMaleGenetive = "jedno";
    private static readonly string secondMale = "dwa";
    private static readonly string secondFemale = "dwa";
    private static readonly string secondMaleGenetive = "dwa";
    private static readonly string secondFemaleGenetive = "dwa";

    private static readonly string[] from3till19 = {
        "", "trzy", "cztery", "pięć", "sześć",
        "siedem", "osiem", "dziewięć", "dziesięć", "jedenaście",
        "dwanaście", "trzynaście", "czternaście", "piętnaście",
        "szesnaście", "siedemnaście", "osiemnaście", "dziewiętnaście"
    };

    private static readonly string[] from3till19Genetive = {
        "", "trzy", "cztery", "pięć", "sześć",
        "siedem", "osiem", "dziewięć", "dziesięć", "jedenaście",
        "dwanaście", "trzynaście", "czternaście", "piętnaście",
        "szesnaście", "siedemnaście", "osiemnaście", "dziewiętnaście"
    };

    private static readonly string[] tens = {
        "", "dwadzieścia", "trzydzieści", "czterdzieści", "pięćdziesiąt",
        "sześćdziesiąt", "siedemdziesiąt", "osiemdziesiąt", "dziewięćdziesiąt"
    };

    private static readonly string[] tensGenetive = {
        "", "dwadzieścia", "trzydzieści", "czterdzieści", "pięćdziesiąt",
        "sześćdziesiąt", "siedemdziesiąt", "osiemdziesiąt", "dziewięćdziesiąt"
    };

    private static readonly string[] hundreds = {
        "", "sto", "dwieście", "trzysta", "czterysta",
        "pięćset", "sześćset", "siedemset", "osiemset", "dziewięćset"
    };

    private static readonly string[] hundredsGenetive = {
        "", "sto", "dwieście", "trzysta", "czterysta",
        "pięćset", "sześćset", "siedemset", "osiemset", "dziewięćset"
    };

    private static readonly string[] thousands = {
        "", "tysiąc", "tysiące", "tysiące"
    };

    private static readonly string[] thousandsAccusative = {
        "", "tysiąc", "tysiące", "tysiące"
    };

    private static readonly string[] millions = {
        "", "milion", "milion", "miliony"
    };

    private static readonly string[] billions = {
        "", "miliard", "miliard", "miliardy"
    };

    private static readonly string[] trillions = {
        "", "tryliona", "tryliona", "tryliony"
    };

    public static string Convert(decimal amount, bool firstCapital) {
        long UAHAmount = (long)Math.Floor(amount);
        int lastUahDigit = lastDigit(UAHAmount);

        string s = NumeralsToTxt(UAHAmount, TextCases.Nominative, true, firstCapital) + " ";


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
                            s = MakeText(remainder, hundreds, tens, from3till19, _isMale ? secondMale : secondFemale, _isMale ? firstMale : firstFemaleAccusative, powerArray) + s;
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