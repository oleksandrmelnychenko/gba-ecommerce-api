namespace GBA.Domain.EntityHelpers.GbaDataExportModels;

public sealed class DeliveryDocumentItemDto {
    public ProductDto Product { get; set; }
    public string MeasureUnit { get; set; }
    public double Qty { get; set; }

    public decimal InvoiceAmount { get; set; } // Фактурана вартість
    public decimal CustomsValue { get; set; } //Митна вартість
    public decimal DutyPercent { get; set; } //  Митна ставка
    public decimal Duty { get; set; } //Сума Мита
    public decimal VatPercent { get; set; }
    public decimal VatAmount { get; set; }
}

// Номенклатура - Артикул
// Од. - Одиниця виміру
// Кількість - К-ТЬ
// Фактурана вартість - це Вартість нетто інвойса*курс НБУ на дату МД по валюті договору (тобто це треба обрахувати)
// Митна вартість - Митна вартість
//     Сума Мита - Мито
// Сума ПДВ - ПДВ
// Сума аксцизу - 0 ( в нас немає)

// let supplyOrderItems = packLists.PackingListPackageOrderItems as Array<PackingListPackageOrderItem>;
//         if (supplyOrderItems) {
//             for (let i = 0; i < supplyOrderItems.length; i++) {
//                 let netPriceColumns: RowRecord[] = [];
//                 let generalPriceColumns: RowRecord[] = [];
//                 let managementPriceColumns: RowRecord[] = [];
//
//                 let lastProductSpecification = new List<ProductSpecification>(
//                     supplyOrderItems[i].SupplyInvoiceOrderItem.Product.ProductSpecifications
//                 ).LastOrDefault();
//                 let row = {
//                     Entity: supplyOrderItems[i],
//                     VendorCode: supplyOrderItems[i].SupplyInvoiceOrderItem.Product.VendorCode,
//                     Name: supplyOrderItems[i].SupplyInvoiceOrderItem.Product.Name,
//                     SpecificationCode: (
//                         <div>
//                             {lastProductSpecification ? lastProductSpecification.SpecificationCode : ''}
//                             <PermissionCheck permissionKey='ProductDeliveryProtocols_specifications_customs_codes_infoBtn_PKEY'>
//                                 <div className={'audit_icon'} onClick={() => onOpenSpecificationCodeEdit(supplyOrderItems[i])} />
//                             </PermissionCheck>
//                         </div>
//                     ),
//                     Qty: supplyOrderItems[i].Qty,
//                     MeasureUnit: supplyOrderItems[i].SupplyInvoiceOrderItem.Product.MeasureUnit.Name,
//                     UnitPrice: supplyOrderItems[i].UnitPrice,
//                     NetPrice: supplyOrderItems[i].TotalNetPrice,
//                     NetWeight: Math.round(supplyOrderItems[i].TotalNetWeight * 1000) / 1000,
//                     GrossWeight: Math.round(supplyOrderItems[i].TotalGrossWeight * 1000) / 1000,
//                     CustomsValue: lastProductSpecification ? lastProductSpecification.CustomsValue : 0,
//                     DutyPercent: lastProductSpecification ? lastProductSpecification.DutyPercent : 0,
//                     Duty: lastProductSpecification ? lastProductSpecification.Duty : 0,
//                     VATPercent: lastProductSpecification ? lastProductSpecification.VATPercent : 0,
//                     VATValue: lastProductSpecification ? lastProductSpecification.VATValue : 0,