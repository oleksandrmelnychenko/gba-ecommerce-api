using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace GBA.Common.Helpers.PrintingDocuments;

public sealed class PrintDocumentsHelper {
    private readonly IEnumerable<ColumnsDataForPrinting> _columns;
    private readonly object _itemForPrint;

    public PrintDocumentsHelper(
        object itemForPrint,
        IEnumerable<ColumnsDataForPrinting> columns) {
        _itemForPrint = itemForPrint;
        _columns = columns;
    }

    public List<Dictionary<string, string>> GetRowsForPrintDocument() {
        List<Dictionary<string, string>> rows = new();

        if (_columns == null) return rows;

        if (_itemForPrint is IEnumerable)
            foreach (object item in (IEnumerable<object>)_itemForPrint)
                rows.Add(GetReflectionRowForPrinting(item, _columns));
        else
            rows.Add(GetReflectionRowForPrinting(_itemForPrint, _columns));

        return rows;
    }

    private Dictionary<string, string> GetReflectionRowForPrinting(
        object item,
        IEnumerable<ColumnsDataForPrinting> columns) {
        Dictionary<string, string> row = new();

        foreach (ColumnsDataForPrinting column in columns) {
            string value = "";

            IEnumerable<string> props = column.TableName.Split('.');

            if (props.Count() > 1) {
                props = props.Skip(1);

                value = GetValueFromProperty(column.ColumnName, item, props, value);

                if (!string.IsNullOrEmpty(value))
                    value = value.Remove(value.Length - 2, 2);
            } else {
                Type type = item.GetType();

                PropertyInfo property = type.GetProperty(column.ColumnName);

                if (property != null) {
                    object valProp = property.GetValue(item);

                    if (valProp != null && !valProp.Equals(DateTime.MinValue)) {
                        if (valProp is DateTime date)
                            value += TimeZoneInfo.ConvertTimeFromUtc(
                                date,
                                TimeZoneInfo.FindSystemTimeZoneById(
                                    CultureInfo.CurrentCulture.TwoLetterISOLanguageName.Equals("uk")
                                        ? "FLE Standard Time"
                                        : "Central European Standard Time"
                                )
                            ).ToString("dd.MM.yyyy HH:mm") + ", ";
                        else
                            value += valProp + ", ";
                    }
                }
            }

            if (!string.IsNullOrEmpty(value) && value.Contains(", "))
                value = value.Remove(value.Length - 2, 2);

            row.Add(column.ColumnName, value);
        }

        return row;
    }

    private string GetValueFromProperty(string columnName, object item, IEnumerable<string> props, string value) {
        Type type = item.GetType();
        if (props.Any()) {
            foreach (string prop in props) {
                PropertyInfo propInfo = type.GetProperty(prop);

                if (propInfo == null) continue;

                object valueFromItem = propInfo.GetValue(item);

                if (valueFromItem is IEnumerable)
                    foreach (object valueItem in (IEnumerable)valueFromItem)
                        value = GetValueFromProperty(columnName, valueItem, props.Skip(1), value);
                else
                    value = GetValueFromProperty(columnName, valueFromItem, props.Skip(1), value);
            }
        } else {
            PropertyInfo propInfo = type.GetProperty(columnName);


            if (propInfo != null) {
                object valProp = propInfo.GetValue(item);

                if (valProp != null && !valProp.Equals(DateTime.MinValue))
                    value += valProp + ", ";
            }
        }

        return value;
    }
}