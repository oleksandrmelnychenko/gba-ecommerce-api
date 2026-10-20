using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Sales;

namespace GBA.Services.Infrastructure.Checkout;

public static class CheckoutFingerprint {
    public const int Length = 32;

    public static byte[] Compute(params string[] parts) {
        ArgumentNullException.ThrowIfNull(parts);

        using IncrementalHash hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        byte[] length = new byte[sizeof(int)];

        foreach (string part in parts) {
            byte[] bytes = Encoding.UTF8.GetBytes(part ?? string.Empty);
            BinaryPrimitives.WriteInt32BigEndian(length, bytes.Length);
            hash.AppendData(length);
            hash.AppendData(bytes);
        }

        return hash.GetHashAndReset();
    }

    public static byte[] ForCart(bool withVat) {
        return Compute("cart", withVat ? "1" : "0");
    }

    public static byte[] ForInvoice(string withVat, string invoice, string number, byte[] invoiceFileHash) {
        return Compute(
            "invoice",
            withVat,
            invoice,
            number,
            invoiceFileHash == null ? string.Empty : Convert.ToHexString(invoiceFileHash));
    }

    public static byte[] ForQuickInvoice(Guid retailClientNetId, bool fullPayment, Sale sale) {
        return Compute(
            "quick-invoice",
            retailClientNetId.ToString("D"),
            fullPayment ? "1" : "0",
            sale == null ? string.Empty : JsonSerializer.Serialize(sale, CheckoutExecutor.ReceiptJsonOptions));
    }

    public static byte[] ForCartAdd(IEnumerable<OrderItem> items, bool withVat) {
        IEnumerable<string> lines = (items ?? Enumerable.Empty<OrderItem>())
            .Where(item => item != null)
            .Select(item => string.Create(
                CultureInfo.InvariantCulture,
                $"{item.Product?.NetUid ?? Guid.Empty:D}|{item.Product?.Id ?? 0}|{item.ProductId}={item.Qty:R}"));

        return Compute(
            new[] {
                    "cart-add",
                    withVat ? "1" : "0"
                }
                .Concat(lines)
                .ToArray());
    }

    public static byte[] ForOffer(ClientShoppingCart offer, bool addCurrentCartItems) {
        IEnumerable<string> items = (offer?.OrderItems ?? Enumerable.Empty<OrderItem>())
            .Where(item => item != null)
            .Select(item => string.Create(
                CultureInfo.InvariantCulture,
                $"{item.NetUid:D}={item.Qty:R}"))
            .OrderBy(item => item, StringComparer.Ordinal);

        return Compute(
            new[] {
                    "offer",
                    (offer?.NetUid ?? Guid.Empty).ToString("D"),
                    addCurrentCartItems ? "1" : "0"
                }
                .Concat(items)
                .ToArray());
    }
}
