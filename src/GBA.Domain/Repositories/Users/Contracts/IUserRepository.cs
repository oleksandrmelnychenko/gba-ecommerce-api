using System;
using System.Collections.Generic;
using GBA.Common.Helpers;
using GBA.Domain.Entities;
using GBA.Domain.FilterEntities;
using GBA.Domain.IdentityEntities;

namespace GBA.Domain.Repositories.Users.Contracts;

public interface IUserRepository {
    UserIdentity GetUserIdentity(Guid netUid);
    long Add(User user);

    long GetManagerOrGBAIdByClientNetId(Guid netId);

    void Update(User user);

    List<User> GetAll();

    List<User> GetAll(long limit, long offset);

    List<User> GetAll(string order, long limit, long offset);

    List<User> GetAll(string sql, string order, GetQuery query, string value);

    IEnumerable<User> GetAllFromSearch(string value);

    List<User> GetAllSalesManagers();

    List<User> GetAllByUserRoleTypes(IEnumerable<UserRoleType> userRoleTypes);

    IEnumerable<long> GetAllSalesManagersIds();

    List<User> GetAllPurchaseManagers();

    User GetHeadPolishLogistic();

    User GetHeadPurchaseAnalytic();

    User GetById(long id);

    User GetByNetId(Guid netId);

    User GetByNetIdWithoutIncludes(Guid netId);

    Guid GetFirstSalesManagerNetUid();

    Guid GetUserNetIdByEmail(string value);

    User GetGbaUser();

    void Remove(Guid netId);

    long GetUserIdByNetId(Guid userNetId);

    IEnumerable<User> GetManagersFromSearch(string value);

    long GetIdByNetId(Guid nedId);
}