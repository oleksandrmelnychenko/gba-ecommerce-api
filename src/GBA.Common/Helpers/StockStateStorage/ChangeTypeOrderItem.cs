namespace GBA.Common.Helpers.StockStateStorage;

public enum ChangeTypeOrderItem {
    Reserve, //резерв 2 крок
    SetLastStep, //останій крок
    ActEditTheInvoice, //акт редагування
    Return, //повернення
    Set,
    AddProductCapitalization, //оприход
    NewPackingListDynamic,
    AddNewFromSupplyOrderUkraineDynamicPlacements,
    NewClientsShoppingCartItems,
    DeleteClientsShoppingCartItems,
    UpdateClientsShoppingCartItems,
    OrderNewIvoice,
    DepreciatedOrder, //списання без управління
    DepreciatedOrderManagement, //списання з управлінням
    ProductPlacementUpdate, //редагування весь асортимент
    DepreciatedOrderFile //cписання з файлу
}