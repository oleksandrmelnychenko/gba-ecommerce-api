using System;
using System.Collections.Generic;
using GBA.Domain.Entities;

namespace GBA.Domain.Repositories.Storages.Contracts;

public interface IStorageRepository {
    long Add(Storage storage);

    long GetTotalProductsCountByStorageNetId(Guid netId);

    Storage GetById(long id);
    Storage GetReSale();

    Storage GetByNetId(Guid netId);

    Storage GetByLocale(string locale, bool withDefective = false);

    Storage GetWithHighestPriority();

    List<Storage> GetAll();

    IEnumerable<Storage> GetAllForReturnsFiltered(
        Guid organizationNetId,
        Guid? orderItemNetId,
        bool onlyDefective = false);

    IEnumerable<Storage> GetAllForReturns(bool onlyDefective = false);

    IEnumerable<Storage> GetAllNonDefectiveByCurrentLocale();

    IEnumerable<Storage> GetAllDefectiveByCurrentLocale();

    IEnumerable<Storage> GetAllWithOrganizations();

    IEnumerable<Storage> GetAllFilteredByOrganizationNetId(Guid organizationNetId, bool skipDefective);

    IEnumerable<Storage> GetAllForEcommerce();

    long GetIdByLocale(string locale);

    void Update(Storage storage);

    void Remove(Guid netId);

    void SetStorageForEcommerce(Guid netId);

    void SetStoragePriority(long storageId, int priority);

    void UnselectStorageForEcommerce(Guid netId);

    IEnumerable<Storage> GetAllForReSaleAvailabilities();
}