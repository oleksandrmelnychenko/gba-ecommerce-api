using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using Dapper;
using GBA.Common.Helpers;
using GBA.Common.IdentityConfiguration.Roles;
using GBA.Domain.Entities;
using GBA.Domain.FilterEntities;
using GBA.Domain.IdentityEntities;
using GBA.Domain.Repositories.Users.Contracts;
using GBA.Domain.TranslationEntities;

namespace GBA.Domain.Repositories.Users;

public sealed class UserRepository : IUserRepository {
    private readonly IDbConnection _connection;

    public UserRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long Add(User user) {
        return _connection.Query<long>(
            "INSERT INTO [User] (FirstName, LastName, MiddleName, Email, PhoneNumber, Region, UserRoleId, Abbreviation, Updated) " +
            "VALUES (@FirstName, @LastName, @MiddleName, @Email, @PhoneNumber, @Region, @UserRoleId, @Abbreviation, getutcdate()); " +
            "SELECT SCOPE_IDENTITY()",
            user
        ).Single();
    }

    public void Update(User user) {
        _connection.Execute(
            "UPDATE [User] SET " +
            "FirstName = @FirstName, LastName = @LastName, MiddleName = @MiddleName, Email = @Email, PhoneNumber = @PhoneNumber, Region = @Region, " +
            "UserRoleId = @UserRoleId, Abbreviation = @Abbreviation, Updated = getutcdate() " +
            "WHERE [User].NetUID = @NetUid",
            user
        );
    }

    public List<User> GetAll() {
        return _connection.Query<User, UserRole, UserRoleTranslation, User>(
            "SELECT * FROM [User] " +
            "LEFT JOIN [UserRole] " +
            "ON [User].UserRoleID = [UserRole].ID " +
            "LEFT OUTER JOIN [UserRoleTranslation] " +
            "ON [UserRole].ID = [UserRoleTranslation].UserRoleID " +
            "AND [UserRoleTranslation].CultureCode = @Culture " +
            "AND [UserRoleTranslation].Deleted = 0 " +
            "WHERE [User].Deleted = 0",
            (user, role, roleTranslation) => {
                if (roleTranslation != null) role.Name = roleTranslation.Name;

                user.UserRole = role;

                return user;
            },
            new { Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName }
        ).ToList();
    }

    public List<User> GetAll(long limit, long offset) {
        return _connection.Query<User, UserRole, UserRoleTranslation, User>(
            ";WITH [Search_CTE] " +
            "AS " +
            "( " +
            "SELECT ROW_NUMBER() OVER (ORDER BY [User].ID) AS RowNumber " +
            ", [User].ID " +
            "FROM [User] " +
            "WHERE [User].Deleted = 0 " +
            ") " +
            "SELECT * " +
            "FROM [User] " +
            "LEFT JOIN [UserRole] AS [User.UserRole] " +
            "ON [User].UserRoleID = [User.UserRole].ID " +
            "LEFT OUTER JOIN [UserRoleTranslation] AS [User.UserRoleTranslation] " +
            "ON [User.UserRole].ID = [User.UserRoleTranslation].UserRoleID " +
            "AND [User.UserRoleTranslation].CultureCode = @Culture " +
            "AND [User.UserRoleTranslation].Deleted = 0 " +
            "WHERE [User].ID IN ( " +
            "SELECT ID " +
            "FROM [Search_CTE] " +
            "WHERE [Search_CTE].RowNumber > @Offset " +
            "AND [Search_CTE].RowNumber <= @Limit + @Offset " +
            ") ",
            (user, role, roleTranslation) => {
                if (roleTranslation != null) role.Name = roleTranslation.Name;

                user.UserRole = role;

                return user;
            },
            new {
                Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName,
                Offset = offset,
                Limit = limit
            }
        ).ToList();
    }

    public List<User> GetAll(string order, long limit, long offset) {
        return _connection.Query<User, UserRole, UserRoleTranslation, User>(
            ";WITH [Search_CTE] " +
            "AS " +
            "( " +
            $"SELECT ROW_NUMBER() OVER ({order}) AS RowNumber " +
            ", [User].ID " +
            "FROM [User] " +
            "LEFT JOIN [UserRole] AS [User.UserRole] " +
            "ON [User].UserRoleID = [User.UserRole].ID " +
            "LEFT OUTER JOIN [UserRoleTranslation] AS [User.UserRoleTranslation] " +
            "ON [User.UserRole].ID = [User.UserRoleTranslation].UserRoleID " +
            "AND [User.UserRoleTranslation].CultureCode = @Culture " +
            "AND [User.UserRoleTranslation].Deleted = 0 " +
            "WHERE [User].Deleted = 0 " +
            ") " +
            "SELECT * " +
            "FROM [User] " +
            "LEFT JOIN [UserRole] AS [User.UserRole] " +
            "ON [User].UserRoleID = [User.UserRole].ID " +
            "LEFT OUTER JOIN [UserRoleTranslation] AS [User.UserRoleTranslation] " +
            "ON [User.UserRole].ID = [User.UserRoleTranslation].UserRoleID " +
            "AND [User.UserRoleTranslation].CultureCode = @Culture " +
            "AND [User.UserRoleTranslation].Deleted = 0 " +
            "WHERE [User].ID IN ( " +
            "SELECT ID " +
            "FROM [Search_CTE] " +
            "WHERE [Search_CTE].RowNumber > @Offset " +
            "AND [Search_CTE].RowNumber <= @Limit + @Offset " +
            ") ",
            (user, role, roleTranslation) => {
                if (roleTranslation != null) role.Name = roleTranslation.Name;

                user.UserRole = role;

                return user;
            },
            new {
                Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName,
                Offset = offset,
                Limit = limit
            }
        ).ToList();
    }

    public List<User> GetAll(string sql, string order, GetQuery query, string value) {
        return _connection.Query<User, UserRole, UserRoleTranslation, User>(
            ";WITH [Search_CTE] " +
            "AS " +
            "( " +
            $"SELECT ROW_NUMBER() OVER ({order}) AS RowNumber " +
            ", [User].ID " +
            "FROM [User] " +
            "LEFT JOIN [UserRole] AS [User.UserRole] " +
            "ON [User].UserRoleID = [User.UserRole].ID " +
            "LEFT OUTER JOIN [UserRoleTranslation] AS [User.UserRoleTranslation] " +
            "ON [User.UserRole].ID = [User.UserRoleTranslation].UserRoleID " +
            "AND [User.UserRoleTranslation].CultureCode = @Culture " +
            "AND [User.UserRoleTranslation].Deleted = 0 " +
            sql +
            ") " +
            "SELECT * " +
            "FROM [User] " +
            "LEFT JOIN [UserRole] AS [User.UserRole] " +
            "ON [User].UserRoleID = [User.UserRole].ID " +
            "LEFT OUTER JOIN [UserRoleTranslation] AS [User.UserRoleTranslation] " +
            "ON [User.UserRole].ID = [User.UserRoleTranslation].UserRoleID " +
            "AND [User.UserRoleTranslation].CultureCode = @Culture " +
            "AND [User.UserRoleTranslation].Deleted = 0 " +
            "WHERE [User].ID IN ( " +
            "SELECT ID " +
            "FROM [Search_CTE] " +
            "WHERE [Search_CTE].RowNumber > @Offset " +
            "AND [Search_CTE].RowNumber <= @Limit + @Offset " +
            ") ",
            (user, role, roleTranslation) => {
                if (roleTranslation != null) role.Name = roleTranslation.Name;

                user.UserRole = role;

                return user;
            },
            new {
                query.Offset,
                query.Limit,
                Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName,
                Value = value
            }
        ).ToList();
    }

    public List<User> GetAllSalesManagers() {
        return _connection.Query<User, UserRole, UserRoleTranslation, User>(
            "SELECT * FROM [User] " +
            "LEFT JOIN [UserRole] " +
            "ON [User].UserRoleID = [UserRole].ID " +
            "LEFT OUTER JOIN [UserRoleTranslation] " +
            "ON [UserRole].ID = [UserRoleTranslation].UserRoleID " +
            "AND [UserRoleTranslation].CultureCode = @Culture " +
            "AND [UserRoleTranslation].Deleted = 0 " +
            "WHERE [User].Deleted = 0 " +
            "AND ([UserRole].Name = @Role1 OR [UserRole].Name = @Role2 " +
            //Temporary hard coded
            "OR [User].ID = 1) ",
            (user, role, roleTranslation) => {
                if (roleTranslation != null) role.Name = roleTranslation.Name;

                user.UserRole = role;

                return user;
            },
            new {
                Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName,
                Role1 = IdentityRoles.SalesAnalytic,
                Role2 = IdentityRoles.HeadSalesAnalytic
            }
        ).ToList();
    }

    public Guid GetFirstSalesManagerNetUid() {
        return _connection.Query<Guid>(
            "SELECT [User].NetUID FROM [User] " +
            "LEFT JOIN [UserRole] " +
            "ON [User].UserRoleID = [UserRole].ID " +
            "LEFT OUTER JOIN [UserRoleTranslation] " +
            "ON [UserRole].ID = [UserRoleTranslation].UserRoleID " +
            "AND [UserRoleTranslation].CultureCode = @Culture " +
            "AND [UserRoleTranslation].Deleted = 0 " +
            "WHERE [User].Deleted = 0 " +
            "AND ([UserRole].Name = @Role1 OR [UserRole].Name = @Role2 " +
            //Temporary hard coded
            "OR [User].ID = 1) ",
            new {
                Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName,
                Role1 = IdentityRoles.SalesAnalytic,
                Role2 = IdentityRoles.HeadSalesAnalytic
            }
        ).First();
    }

    public Guid GetUserNetIdByEmail(string value) {
        return _connection.Query<Guid>(
            "SELECT [User].NetUID FROM [User] " +
            "WHERE Deleted = 0 " +
            "AND Email = @Value ",
            new { Value = value }
        ).FirstOrDefault();
    }

    public IEnumerable<long> GetAllSalesManagersIds() {
        return _connection.Query<long>(
            "SELECT * FROM [User] " +
            "LEFT JOIN [UserRole] " +
            "ON [User].UserRoleID = [UserRole].ID " +
            "WHERE [User].Deleted = 0 " +
            "AND ([UserRole].Name = @Role1 OR [UserRole].Name = @Role2 " +
            //Temporary hard coded
            "OR [User].ID = 1) ",
            new {
                Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName,
                Role1 = IdentityRoles.SalesAnalytic,
                Role2 = IdentityRoles.HeadSalesAnalytic
            }
        );
    }

    public User GetHeadPolishLogistic() {
        return _connection.Query<User>(
            "SELECT TOP(1) [User].* " +
            "FROM [User] " +
            "LEFT JOIN [UserRole] " +
            "ON [User].UserRoleID = [UserRole].ID " +
            "WHERE [User].Deleted = 0 " +
            "AND [UserRole].Name = @Role",
            new {
                Role = IdentityRoles.HeadPolishLogistic
            }
        ).SingleOrDefault();
    }

    public User GetGbaUser() {
        return _connection.Query<User>(
            "SELECT TOP(1) [User].* " +
            "FROM [User] " +
            "LEFT JOIN [UserRole] " +
            "ON [UserRole].ID = [User].UserRoleID " +
            "WHERE [UserRole].[Name] = @RoleName",
            new { RoleName = IdentityRoles.GBA }
        ).SingleOrDefault();
    }

    public long GetManagerOrGBAIdByClientNetId(Guid netId) {
        return _connection.Query<long>(
            "SELECT ISNULL( " +
            "( " +
            "SELECT TOP(1) " +
            "( " +
            "CASE " +
            "WHEN [ClientUserProfile].ID IS NOT NULL " +
            "THEN [ClientUserProfile].UserProfileID " +
            "ELSE ( " +
            "SELECT TOP(1) [User].ID " +
            "FROM [User] " +
            "LEFT JOIN [UserRole] " +
            "ON [UserRole].ID = [User].UserRoleID " +
            "WHERE [UserRole].[Name] = @RoleName " +
            ") " +
            "END " +
            ") " +
            "FROM [Client] " +
            "LEFT JOIN [ClientUserProfile] " +
            "ON [ClientUserProfile].ClientID = [Client].ID " +
            "AND [ClientUserProfile].Deleted = 0 " +
            "WHERE [Client].NetUID = @NetId " +
            "), " +
            "( " +
            "SELECT TOP(1) [User].ID " +
            "FROM [User] " +
            "LEFT JOIN [UserRole] " +
            "ON [UserRole].ID = [User].UserRoleID " +
            "WHERE [UserRole].[Name] = @RoleName " +
            ") " +
            ")",
            new { NetId = netId, RoleName = IdentityRoles.GBA }
        ).SingleOrDefault();
    }

    public User GetHeadPurchaseAnalytic() {
        return _connection.Query<User>(
            "SELECT TOP(1) [User].* " +
            "FROM [User] " +
            "LEFT JOIN [UserRole] " +
            "ON [User].UserRoleID = [UserRole].ID " +
            "WHERE [User].Deleted = 0 " +
            "AND [UserRole].Name = @Role",
            new {
                Role = IdentityRoles.HeadPurchaseAnalytic
            }
        ).SingleOrDefault();
    }

    public IEnumerable<User> GetAllFromSearch(string value) {
        return _connection.Query<User, UserRole, UserRoleTranslation, User>(
            "SELECT * " +
            "FROM [User] " +
            "LEFT JOIN [UserRole] " +
            "ON [User].UserRoleID = [UserRole].ID " +
            "LEFT OUTER JOIN [UserRoleTranslation] " +
            "ON [UserRole].ID = [UserRoleTranslation].UserRoleID " +
            "AND [UserRoleTranslation].CultureCode = @Culture " +
            "AND [UserRoleTranslation].Deleted = 0 " +
            "WHERE [User].Deleted = 0 " +
            "AND (" +
            "[User].FirstName like '%' + @Value + '%' " +
            "OR " +
            "[User].LastName like '%' + @Value + '%' " +
            "OR " +
            "[User].MiddleName like '%' + @Value + '%' " +
            "OR " +
            "[User].Email like '%' + @Value + '%' " +
            "OR " +
            "[User].PhoneNumber like '%' + @Value + '%' " +
            ") " +
            "ORDER BY [User].LastName",
            (user, role, roleTranslation) => {
                if (roleTranslation != null) role.Name = roleTranslation.Name;

                user.UserRole = role;

                return user;
            },
            new {
                Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName,
                Value = value
            }
        );
    }

    public List<User> GetAllPurchaseManagers() {
        return _connection.Query<User, UserRole, UserRoleTranslation, User>(
            "SELECT * FROM [User] " +
            "LEFT JOIN [UserRole] " +
            "ON [User].UserRoleID = [UserRole].ID " +
            "LEFT OUTER JOIN [UserRoleTranslation] " +
            "ON [UserRole].ID = [UserRoleTranslation].UserRoleID " +
            "AND [UserRoleTranslation].CultureCode = @Culture " +
            "AND [UserRoleTranslation].Deleted = 0 " +
            "WHERE [User].Deleted = 0 " +
            "AND ([UserRole].Name = @Role1 OR [UserRole].Name = @Role2) ",
            (user, role, roleTranslation) => {
                if (roleTranslation != null) role.Name = roleTranslation.Name;

                user.UserRole = role;

                return user;
            },
            new {
                Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName,
                Role1 = IdentityRoles.PurchaseAnalytic,
                Role2 = IdentityRoles.HeadPurchaseAnalytic
            }
        ).ToList();
    }

    public User GetById(long id) {
        return _connection.Query<User, UserRole, UserRoleTranslation, User>(
            "SELECT * FROM [User] " +
            "LEFT JOIN [UserRole] " +
            "ON [User].UserRoleID = [UserRole].ID " +
            "LEFT OUTER JOIN [UserRoleTranslation] " +
            "ON [UserRole].ID = [UserRoleTranslation].UserRoleID " +
            "AND [UserRoleTranslation].CultureCode = @Culture " +
            "AND [UserRoleTranslation].Deleted = 0 " +
            "WHERE [User].ID = @Id",
            (user, role, roleTranslation) => {
                if (roleTranslation != null) role.Name = roleTranslation.Name;

                user.UserRole = role;

                return user;
            },
            new {
                Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName,
                Id = id
            }
        ).SingleOrDefault();
    }

    public User GetByNetIdWithoutIncludes(Guid netId) {
        return _connection.Query<User>(
            "SELECT * FROM [User] " +
            "WHERE NetUID = @NetId",
            new { NetId = netId }
        ).SingleOrDefault();
    }

    public User GetByNetId(Guid netId) {
        User toReturn = null;

        _connection.Query<User, UserRole, UserRoleTranslation, Permission, User>(
            "SELECT " +
            "[User].* " +
            ", [UserRole].* " +
            ", [UserRoleTranslation].* " +
            ", [Permission].* " +
            "FROM [User] " +
            "LEFT JOIN [UserRole] " +
            "ON [User].UserRoleID = [UserRole].ID " +
            "LEFT OUTER JOIN [UserRoleTranslation] " +
            "ON [UserRole].ID = [UserRoleTranslation].UserRoleID " +
            "AND [UserRoleTranslation].CultureCode = @Culture " +
            "AND [UserRoleTranslation].Deleted = 0 " +
            "LEFT JOIN [RolePermission] " +
            "ON [RolePermission].UserRoleID = [UserRole].ID " +
            "LEFT JOIN [Permission] " +
            "ON [Permission].ID = [RolePermission].PermissionID " +
            "AND [Permission].Deleted = 0 " +
            "WHERE [User].NetUID = @NetId ",
            (user, role, roleTranslation, permission) => {
                if (toReturn == null) {
                    if (roleTranslation != null) role.Name = roleTranslation.Name;

                    if (permission != null) role.Permissions.Add(permission);

                    user.UserRole = role;

                    toReturn = user;
                } else {
                    if (permission != null && !toReturn.UserRole.Permissions.Any(e => e.Id.Equals(permission.Id))) toReturn.UserRole.Permissions.Add(permission);
                }

                return user;
            },
            new {
                Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName,
                NetId = netId.ToString()
            }
        );

        return toReturn;
    }


    public void Remove(Guid netId) {
        _connection.Execute(
            "UPDATE [User] SET " +
            "Deleted = 1 " +
            "WHERE [User].NetUID = @NetId",
            new { NetId = netId.ToString() }
        );
    }

    public List<User> GetAllByUserRoleTypes(IEnumerable<UserRoleType> userRoleTypes) {
        return _connection.Query<User, UserRole, UserRoleTranslation, User>(
            "SELECT * FROM [User] " +
            "LEFT JOIN [UserRole] " +
            "ON [User].UserRoleID = [UserRole].ID AND [UserRole].Deleted = 0 " +
            "LEFT OUTER JOIN [UserRoleTranslation] " +
            "ON [UserRole].ID = [UserRoleTranslation].UserRoleID " +
            "AND [UserRoleTranslation].CultureCode = @Culture " +
            "AND [UserRoleTranslation].Deleted = 0 " +
            "WHERE [User].Deleted = 0 " +
            "AND [UserRole].UserRoleType IN @UserRoleTypes",
            (user, role, roleTranslation) => {
                if (roleTranslation != null) role.Name = roleTranslation.Name;

                user.UserRole = role;

                return user;
            },
            new {
                Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName,
                UserRoleTypes = userRoleTypes
            }
        ).ToList();
    }

    public long GetUserIdByNetId(Guid userNetId) {
        return _connection.Query<long>(
            "SELECT [User].[ID] " +
            "FROM [User] " +
            "WHERE [User].[NetUID] = @netId",
            new { netId = userNetId }
        ).SingleOrDefault();
    }

    public IEnumerable<User> GetManagersFromSearch(string value) {
        return _connection.Query<User, UserRole, UserRoleTranslation, User>(
            "SELECT * " +
            "FROM [User] " +
            "LEFT JOIN [UserRole] " +
            "ON [User].UserRoleID = [UserRole].ID " +
            "LEFT OUTER JOIN [UserRoleTranslation] " +
            "ON [UserRole].ID = [UserRoleTranslation].UserRoleID " +
            "AND [UserRoleTranslation].CultureCode = @Culture " +
            "AND [UserRoleTranslation].Deleted = 0 " +
            "WHERE [User].Deleted = 0 " +
            "AND ([UserRole].Name = @Role1 OR [UserRole].Name = @Role2 " +
            "OR [User].ID = 1) " +
            "AND (" +
            "[User].FirstName like '%' + @Value + '%' " +
            "OR " +
            "[User].LastName like '%' + @Value + '%' " +
            "OR " +
            "[User].MiddleName like '%' + @Value + '%' " +
            "OR " +
            "[User].Email like '%' + @Value + '%' " +
            "OR " +
            "[User].PhoneNumber like '%' + @Value + '%' " +
            ") " +
            "ORDER BY [User].LastName",
            (user, role, roleTranslation) => {
                if (roleTranslation != null) role.Name = roleTranslation.Name;

                user.UserRole = role;

                return user;
            },
            new {
                Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName,
                Value = value ?? string.Empty,
                Role1 = IdentityRoles.SalesAnalytic,
                Role2 = IdentityRoles.HeadSalesAnalytic
            }
        ).ToList();
    }

    public long GetIdByNetId(Guid nedId) {
        return _connection.Query<long>(
            "SELECT [User].[Id] " +
            "FROM [User] " +
            "WHERE [User].[NetUID] = @NetId; ",
            new { NetId = nedId }).FirstOrDefault();
    }

    public UserIdentity GetUserIdentity(Guid netUid) {
        return _connection.Query<UserIdentity>(
            "SELECT * " +
            "FROM [AspNetUsers]" +
            "WHERE [AspNetUsers].NetId = @NetId",
            new { NetId = netUid }
        ).FirstOrDefault();
    }
}