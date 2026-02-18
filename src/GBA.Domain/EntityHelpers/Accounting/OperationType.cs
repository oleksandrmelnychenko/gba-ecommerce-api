namespace GBA.Domain.EntityHelpers.Accounting;

public enum OperationType {
    ClientPayment = 0, //Оплата покупця 
    SupplierReturn = 1, // Повернення грошових коштів від постачальника
    OtherAccountingWithCounterparts = 2, // Інші розрахунки із контрагентами
    OtherIncome = 3, // Інше надходження безготівкових грошових коштів
    PaymentToSupplierByPaymentTask = 4, // Оплата постачальнику по платіжній задачі
    PaymentToSupplier = 5, // Оплата постачальнику
    BuyerReturn = 6, // Повернення грошових коштів покупцю
    OtherOutcomeWithCounterparts = 7, // Інші розрахунки з контрагентами
    OtherOutcome = 8, // Інше списання безготівкових грошових коштів
    ReturnFromColleague = 9, // Повернення грошових коштів підзвітником
    TransferToColleague = 10, // Перерахунок грошових коштів підзвітнику
    CurrencyTransfering = 11 //Переміщення валюти
}