using System.Collections.Generic;
using System.Drawing;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace GBA.Domain.DocumentsManagement;

public static class XlsManagementExtensions {
    public static void ApplyStyledValue(
        this ExcelRange range,
        object value,
        float fontSize,
        string fontName = "Arial",
        bool bold = false,
        bool merge = true,
        bool wrapText = true,
        ExcelHorizontalAlignment horizontalAlignment = ExcelHorizontalAlignment.Center,
        ExcelVerticalAlignment verticalAlignment = ExcelVerticalAlignment.Center,
        ExcelBorderStyle? borderAroundStyle = null) {
        range.Merge = merge;
        range.Style.Font.Name = fontName;
        range.Style.Font.Size = fontSize;
        range.Style.Font.Bold = bold;
        range.Style.HorizontalAlignment = horizontalAlignment;
        range.Style.VerticalAlignment = verticalAlignment;
        range.Style.WrapText = wrapText;
        range.Value = value;

        if (borderAroundStyle.HasValue) range.Style.Border.BorderAround(borderAroundStyle.Value);
    }

    public static void ApplyStyledValue(
        this ExcelRange range,
        object value,
        float fontSize,
        string numberFormat,
        string fontName = "Arial",
        bool bold = false,
        bool merge = true,
        bool wrapText = true,
        ExcelHorizontalAlignment horizontalAlignment = ExcelHorizontalAlignment.Center,
        ExcelVerticalAlignment verticalAlignment = ExcelVerticalAlignment.Center,
        ExcelBorderStyle? borderAroundStyle = null) {
        range.Merge = merge;
        range.Style.Font.Name = fontName;
        range.Style.Font.Size = fontSize;
        range.Style.Font.Bold = bold;
        range.Style.HorizontalAlignment = horizontalAlignment;
        range.Style.VerticalAlignment = verticalAlignment;
        range.Style.Numberformat.Format = numberFormat;
        range.Style.WrapText = wrapText;
        range.Value = value;

        if (borderAroundStyle.HasValue) range.Style.Border.BorderAround(borderAroundStyle.Value);
    }

    public static void ApplyStyledEmptyValue(
        this ExcelRange range,
        float fontSize,
        string fontName = "Arial",
        bool bold = false,
        bool merge = true,
        bool wrapText = true,
        ExcelHorizontalAlignment horizontalAlignment = ExcelHorizontalAlignment.Center,
        ExcelVerticalAlignment verticalAlignment = ExcelVerticalAlignment.Center,
        ExcelBorderStyle? borderAroundStyle = null) {
        range.Merge = merge;
        range.Style.Font.Name = fontName;
        range.Style.Font.Size = fontSize;
        range.Style.Font.Bold = bold;
        range.Style.HorizontalAlignment = horizontalAlignment;
        range.Style.VerticalAlignment = verticalAlignment;
        range.Style.WrapText = wrapText;

        if (borderAroundStyle.HasValue) range.Style.Border.BorderAround(borderAroundStyle.Value);
    }

    public static void ApplyStyledEmptyValue(
        this ExcelRange range,
        float fontSize,
        string numberFormat,
        string fontName = "Arial",
        bool bold = false,
        bool merge = true,
        bool wrapText = true,
        ExcelHorizontalAlignment horizontalAlignment = ExcelHorizontalAlignment.Center,
        ExcelVerticalAlignment verticalAlignment = ExcelVerticalAlignment.Center,
        ExcelBorderStyle? borderAroundStyle = null) {
        range.Merge = merge;
        range.Style.Font.Name = fontName;
        range.Style.Font.Size = fontSize;
        range.Style.Font.Bold = bold;
        range.Style.HorizontalAlignment = horizontalAlignment;
        range.Style.VerticalAlignment = verticalAlignment;
        range.Style.Numberformat.Format = numberFormat;
        range.Style.WrapText = wrapText;

        if (borderAroundStyle.HasValue) range.Style.Border.BorderAround(borderAroundStyle.Value);
    }

    public static void SetColumnWidth(this ExcelWorksheet worksheet, double width, IEnumerable<int> indexes) {
        foreach (int index in indexes) worksheet.Column(index).Width = width;
    }

    public static void SetColumnWidth(this ExcelWorksheet worksheet, double width, int indexFrom, int indexTo) {
        for (int index = indexFrom; index <= indexTo; index++) worksheet.Column(index).Width = width;
    }

    public static void SetColumnWidth(this ExcelWorksheet worksheet, double width, int index) {
        worksheet.Column(index).Width = width;
    }

    public static void SetRowHeight(this ExcelWorksheet worksheet, double height, IEnumerable<int> indexes) {
        foreach (int index in indexes) worksheet.Row(index).Height = height;
    }

    public static void SetRowHeight(this ExcelWorksheet worksheet, double height, int indexFrom, int indexTo) {
        for (int index = indexFrom; index <= indexTo; index++) worksheet.Row(index).Height = height;
    }

    public static void SetRowHeight(this ExcelWorksheet worksheet, double height, int index) {
        worksheet.Row(index).Height = height;
    }

    public static void SetTableHeaderStyle(this ExcelRange range) {
        range.Style.Font.Bold = true;
        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
        range.Style.Font.Size = 10;
        range.Style.Border.BorderAround(ExcelBorderStyle.Thin);
        range.Style.Fill.PatternType = ExcelFillStyle.Solid;
        range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(238, 238, 238));
    }

    public static void ApplyPrinterSettings(
        this ExcelWorksheet worksheet,
        decimal topMargin,
        decimal bottomMargin,
        decimal rightMargin,
        decimal leftMargin,
        decimal headerMargin,
        decimal footerMargin,
        bool fitToPage,
        int? scale = null,
        eOrientation? orientation = null) {
        worksheet.PrinterSettings.TopMargin = (double)topMargin;
        worksheet.PrinterSettings.BottomMargin = (double)bottomMargin;
        worksheet.PrinterSettings.RightMargin = (double)rightMargin;
        worksheet.PrinterSettings.LeftMargin = (double)leftMargin;
        worksheet.PrinterSettings.HeaderMargin = (double)headerMargin;
        worksheet.PrinterSettings.FooterMargin = (double)footerMargin;
        worksheet.PrinterSettings.FitToPage = fitToPage;

        if (scale.HasValue) worksheet.PrinterSettings.Scale = scale.Value;

        if (orientation.HasValue) worksheet.PrinterSettings.Orientation = orientation.Value;
    }
}