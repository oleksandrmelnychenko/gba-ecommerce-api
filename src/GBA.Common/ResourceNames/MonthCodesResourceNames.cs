using System;

namespace GBA.Common.ResourceNames;

public static class MonthCodesResourceNames {
    public const string JANUARY = "Ян";
    public const string FEBRUARY = "Фв";
    public const string MARCH = "Мр";
    public const string APRIL = "Ап";
    public const string MAY = "Ма";
    public const string JUNE = "Ин";
    public const string JULY = "Ил";
    public const string AUGUST = "Ав";
    public const string SEPTEMBER = "Сн";
    public const string OCTOBER = "Ок";
    public const string NOVEMBER = "Но";
    public const string DECEMBER = "Дк";

    public static string GetCurrentMonthCode() {
        switch (DateTime.Now.Month) {
            case 1:
                return JANUARY;
            case 2:
                return FEBRUARY;
            case 3:
                return MARCH;
            case 4:
                return APRIL;
            case 5:
                return MAY;
            case 6:
                return JUNE;
            case 7:
                return JULY;
            case 8:
                return AUGUST;
            case 9:
                return SEPTEMBER;
            case 10:
                return OCTOBER;
            case 11:
                return NOVEMBER;
            case 12:
                return DECEMBER;
        }

        return string.Empty;
    }
}