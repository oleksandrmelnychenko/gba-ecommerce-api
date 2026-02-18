using System;
using System.Threading.Tasks;
using GBA.Domain.Entities.Sales;

namespace GBA.Services.Services.Orders.Contracts;

public interface IPreOrderService {
    Task<PreOrder> AddNewPreOrder(PreOrder preOrder, Guid clientNetId);
}