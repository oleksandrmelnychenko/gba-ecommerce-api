using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Sales;

namespace GBA.Domain.Repositories.Sales.Contracts;

public interface IOrderRepository {
    long Add(Order order);

    void Update(Order order);

    void UpdateClientAgreementByIds(long orderId, long clientAgreementId);

    Order GetById(long id);

    Order GetByNetId(Guid netId);

    List<Order> GetAll();

    List<Order> GetAllShopOrders();

    List<Order> GetAllShopOrders(long limit, long offset);

    List<Order> GetAllShopOrdersByUserNetId(Guid netId);

    List<Order> GetAllShopOrdersByClientNetId(Guid netId);

    long GetAllShopOrdersTotalAmount();

    long GetAllShopOrdersTotalAmountByUserNetId(Guid userNetId);

    void Remove(Guid netId);
}