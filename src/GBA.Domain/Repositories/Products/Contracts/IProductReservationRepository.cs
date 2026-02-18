using System;
using System.Collections.Generic;
using GBA.Domain.Entities.Products;

namespace GBA.Domain.Repositories.Products.Contracts;

public interface IProductReservationRepository {
    ProductReservation GetByOrderItemAndProductAvailabilityIds(long orderItemId, long productAvailabilityId);

    ProductReservation GetByOrderItemProductAvailabilityAndConsignmentItemIds(long orderItemId, long productAvailabilityId, long consignmentItemId);

    List<ProductReservation> GetAllCurrentReservationsByProductNetId(Guid productNetId);

    List<ProductReservation> GetAllCurrentReservationsByProductNetId(Guid productNetId, bool withVat);

    List<ProductReservation> GetAllCurrentReservationsByProductNetId(Guid productNetId, long organizationId, bool withVat);

    List<ProductReservation> GetAllCurrentReservationsByProductNetIdAndCulture(Guid productNetId, string culture);

    IEnumerable<ProductReservation> GetAllByOrderItemIdWithAvailability(long orderItemId);

    IEnumerable<ProductReservation> GetAllByOrderItemIdWithAvailabilityAndReSaleAvailabilities(long orderItemId);

    IEnumerable<ProductReservation> GetByOrderItemId(long orderItemId);

    long AddWithId(ProductReservation productReservation);

    double GetAvailableSumByOrderItemIdWithAvailabilityAndReSaleAvailabilities(long orderItemId);

    void Add(ProductReservation productReservation);

    void Update(ProductReservation productReservation);

    void Update(List<ProductReservation> productReservations);

    void Delete(Guid netId);
}