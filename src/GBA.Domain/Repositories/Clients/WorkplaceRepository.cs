using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using Dapper;
using GBA.Domain.Entities;
using GBA.Domain.Entities.Agreements;
using GBA.Domain.Entities.Clients;
using GBA.Domain.Entities.Regions;
using GBA.Domain.Entities.Sales;
using GBA.Domain.Repositories.Clients.Contracts;
using GBA.Domain.TranslationEntities;

namespace GBA.Domain.Repositories.Clients;

public sealed class WorkplaceRepository : IWorkplaceRepository {
    private readonly IDbConnection _connection;

    public WorkplaceRepository(IDbConnection connection) {
        _connection = connection;
    }

    public long AddWorkplace(Workplace workplace) {
        return _connection.Query<long>(
            "INSERT INTO [Workplace] " +
            "([FirstName], [MiddleName], [LastName], [Email], " +
            "[PhoneNumber], [IsBlocked], [MainClientID], [ClientGroupID], [Updated], " +
            "[Abbreviation], [Region]) " +
            "VALUES (@FirstName, @MiddleName, @LastName, @Email, " +
            "@PhoneNumber, @IsBlocked, @MainClientID, @ClientGroupID, GETUTCDATE(), @Abbreviation, @Region) " +
            "SELECT SCOPE_IDENTITY(); ",
            workplace).FirstOrDefault();
    }

    public void Update(Workplace workplace) {
        _connection.Execute(
            "UPDATE [Workplace] SET " +
            "[Updated] = GETUTCDATE(), " +
            "[Deleted] = @Deleted, " +
            "[FirstName] = @FirstName, " +
            "[MiddleName] = @MiddleName, " +
            "[LastName] = @LastName, " +
            "[Email] = @Email, " +
            "[PhoneNumber] = @PhoneNumber, " +
            "[IsBlocked] = @IsBlocked, " +
            "[MainClientID] = @MainClientId, " +
            "[ClientGroupID] = @ClientGroupId, " +
            "[Abbreviation] = @Abbreviation, " +
            "[Region] = @Region " +
            "WHERE [Workplace].ID = @Id ",
            workplace);
    }

    public void Update(IEnumerable<Workplace> workplaces) {
        _connection.Execute(
            "UPDATE [Workplace] SET " +
            "[Updated] = GETUTCDATE(), " +
            "[Deleted] = @Deleted, " +
            "[FirstName] = @FirstName, " +
            "[MiddleName] = @MiddleName, " +
            "[LastName] = @LastName, " +
            "[Email] = @Email, " +
            "[PhoneNumber] = @PhoneNumber, " +
            "[IsBlocked] = @IsBlocked, " +
            "[MainClientID] = @MainClientId, " +
            "[ClientGroupID] = @ClientGroupId, " +
            "[Abbreviation] = @Abbreviation, " +
            "[Region] = @Region " +
            "WHERE [Workplace].ID = @Id ",
            workplaces);
    }

    public void RemoveById(long id) {
        _connection.Execute(
            "UPDATE [Workplace] SET " +
            "Deleted = 1 " +
            "WHERE ID = @Id ",
            new { Id = id });
    }

    public void RemoveClientGroupByNetId(Guid netId) {
        _connection.Execute(
            "UPDATE [Workplace] SET [ClientGroupId] = NULL " +
            "WHERE [Workplace].NetUID = @NetId",
            new { NetId = netId });
    }

    public void RemoveWorkplaceClientAgreementById(long id) {
        _connection.Execute(
            "UPDATE [WorkplaceClientAgreement] SET " +
            "[Deleted] = 1, " +
            "[Updated] = GETUTCDATE() " +
            "WHERE ID = @Id",
            new { Id = id });
    }

    public void DisableById(long id) {
        _connection.Execute(
            "UPDATE [Workplace] SET [IsBlocked] = 1 " +
            "WHERE [Workplace].ID = @Id",
            new { Id = id });
    }

    public long AddWorkplaceClientAgreement(WorkplaceClientAgreement workplaceClientAgreement) {
        return _connection.Query<long>(
            "INSERT INTO [WorkplaceClientAgreement] ([Updated], [WorkplaceID], [ClientAgreementID], [IsSelected]) " +
            "VALUES (GETUTCDATE(), @WorkplaceId, @ClientAgreementId, @IsSelected); " +
            "SELECT SCOPE_IDENTITY() ",
            workplaceClientAgreement).Single();
    }

    public void UpdateWorkplaceClientAgreement(WorkplaceClientAgreement workplaceClientAgreement) {
        _connection.Execute(
            "UPDATE [WorkplaceClientAgreement] SET " +
            "[Updated] = GETUTCDATE(), " +
            "[Deleted] = @Deleted, " +
            "[WorkplaceID] = @WorkplaceId, " +
            "[ClientAgreementID] = @ClientAgreementID, " +
            "[IsSelected] = @IsSelected " +
            "WHERE [WorkplaceClientAgreement].ID = @Id ",
            workplaceClientAgreement);
    }

    public Workplace GetById(long id) {
        return _connection.Query<Workplace>(
            "SELECT * FROM [Workplace] " +
            "WHERE [Workplace].ID = @Id ",
            new { Id = id }
        ).FirstOrDefault();
    }

    public Workplace GetByNetId(Guid netId) {
        return _connection.Query<Workplace>(
            "SELECT * FROM [Workplace] " +
            "WHERE [Workplace].NetUID = @NetId ",
            new { NetId = netId }
        ).FirstOrDefault();
    }


    public Workplace GetByNetIdWithClient(Guid netId) {
        Workplace workplaceToReturn = null;

        Type[] types = {
            typeof(Workplace),
            typeof(WorkplaceClientAgreement),
            typeof(ClientAgreement),
            typeof(Agreement),
            typeof(Organization),
            typeof(Currency),
            typeof(ClientInDebt),
            typeof(Debt)
        };

        Func<object[], Workplace> mapper = objects => {
            Workplace workplace = (Workplace)objects[0];
            WorkplaceClientAgreement workplaceClientAgreement = (WorkplaceClientAgreement)objects[1];
            ClientAgreement clientAgreement = (ClientAgreement)objects[2];
            Agreement agreement = (Agreement)objects[3];
            Organization organization = (Organization)objects[4];
            Currency currency = (Currency)objects[5];
            ClientInDebt clientInDebt = (ClientInDebt)objects[6];
            Debt debt = (Debt)objects[7];

            if (workplaceToReturn == null) {
                if (workplaceClientAgreement != null) {
                    if (clientInDebt != null) {
                        clientInDebt.Debt = debt;

                        agreement.ClientInDebts.Add(clientInDebt);
                    }

                    agreement.Currency = currency;
                    agreement.Organization = organization;

                    clientAgreement.Agreement = agreement;

                    workplaceClientAgreement.ClientAgreement = clientAgreement;

                    workplace.WorkplaceClientAgreements.Add(workplaceClientAgreement);
                }

                workplaceToReturn = workplace;
            } else {
                if (workplaceClientAgreement == null) return workplace;

                if (workplaceToReturn.WorkplaceClientAgreements.Any(c => c.Id.Equals(workplaceClientAgreement.Id))) {
                    if (clientInDebt != null) {
                        Agreement first = workplaceToReturn.WorkplaceClientAgreements
                            .First(c => c.Id.Equals(workplaceClientAgreement.Id)).ClientAgreement.Agreement;

                        if (!first.ClientInDebts.Any(d => d.Id.Equals(clientInDebt.Id))) {
                            clientInDebt.Debt = debt;

                            first.ClientInDebts.Add(clientInDebt);
                        }
                    }
                } else {
                    if (clientInDebt != null) {
                        clientInDebt.Debt = debt;
                        agreement.ClientInDebts.Add(clientInDebt);
                    }

                    agreement.Currency = currency;
                    agreement.Organization = organization;

                    clientAgreement.Agreement = agreement;

                    workplaceClientAgreement.ClientAgreement = clientAgreement;

                    workplaceToReturn.WorkplaceClientAgreements.Add(workplaceClientAgreement);
                }
            }

            return workplace;
        };

        _connection.Query(
            "SELECT * FROM [Workplace] " +
            "LEFT JOIN [WorkplaceClientAgreement] " +
            "ON [WorkplaceClientAgreement].WorkplaceID = [Workplace].ID " +
            "AND [WorkplaceClientAgreement].Deleted = 0 " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].ID = [WorkplaceClientAgreement].ClientAgreementID " +
            "AND [ClientAgreement].Deleted = 0 " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].ID = [ClientAgreement].AgreementID " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].ID = [Agreement].OrganizationID " +
            "LEFT JOIN [Currency] " +
            "ON [Currency].ID = [Agreement].CurrencyID " +
            "LEFT JOIN [ClientInDebt] " +
            "ON [ClientInDebt].AgreementID = [Agreement].ID " +
            "AND [ClientInDebt].Deleted = 0 " +
            "LEFT JOIN ( " +
            "SELECT [Debt].ID " +
            ",[Debt].Deleted " +
            ",[Debt].Created " +
            ",[Debt].NetUID " +
            ",[Debt].Updated " +
            ",DATEDIFF(DAY, [Debt].Created, GETUTCDATE()) AS [Days] " +
            ",[Debt].Total AS [Total] " +
            "FROM [Debt] " +
            ") AS [Debt] " +
            "ON ClientInDebt.DebtID = Debt.ID " +
            "WHERE [Workplace].NetUID = @NetId ",
            types,
            mapper,
            new {
                NetId = netId
            }
        );

        if (workplaceToReturn == null) return workplaceToReturn;

        Type[] clientTypes = {
            typeof(Client),
            typeof(ClientInRole),
            typeof(ClientType),
            typeof(ClientTypeTranslation),
            typeof(ClientTypeRole),
            typeof(ClientTypeRoleTranslation),
            typeof(RegionCode),
            typeof(ClientAgreement),
            typeof(Agreement),
            typeof(Organization),
            typeof(Currency),
            typeof(ClientInDebt),
            typeof(Debt)
        };

        Client clientToReturn = null;

        Func<object[], Client> clientMapper = objects => {
            Client client = (Client)objects[0];
            ClientInRole clientInRole = (ClientInRole)objects[1];
            ClientType clientType = (ClientType)objects[2];
            ClientTypeTranslation clientTypeTranslation = (ClientTypeTranslation)objects[3];
            ClientTypeRole clientTypeRole = (ClientTypeRole)objects[4];
            ClientTypeRoleTranslation clientTypeRoleTranslation = (ClientTypeRoleTranslation)objects[5];
            RegionCode regionCode = (RegionCode)objects[6];
            ClientAgreement clientAgreement = (ClientAgreement)objects[7];
            Agreement agreement = (Agreement)objects[8];
            Organization organization = (Organization)objects[9];
            Currency currency = (Currency)objects[10];
            ClientInDebt clientInDebt = (ClientInDebt)objects[11];
            Debt debt = (Debt)objects[12];

            if (clientToReturn == null) {
                if (clientInRole != null) {
                    if (clientTypeTranslation != null) clientType.Name = clientTypeTranslation.Name;

                    if (clientTypeRoleTranslation != null) {
                        clientTypeRole.Name = clientTypeRoleTranslation.Name;
                        clientTypeRole.Description = clientTypeRoleTranslation.Description;
                    }

                    clientInRole.ClientType = clientType;
                    clientInRole.ClientTypeRole = clientTypeRole;

                    client.ClientInRole = clientInRole;
                }

                if (clientAgreement != null)
                    if (agreement != null) {
                        if (clientInDebt != null) {
                            clientInDebt.Debt = debt;
                            agreement.ClientInDebts.Add(clientInDebt);
                        }

                        agreement.Currency = currency;
                        agreement.Organization = organization;

                        clientAgreement.Agreement = agreement;

                        client.ClientAgreements.Add(clientAgreement);
                    }

                if (clientInDebt != null) {
                    clientInDebt.Debt = debt;

                    client.ClientInDebts.Add(clientInDebt);
                }

                client.RegionCode = regionCode;
                clientToReturn = client;
            } else {
                if (clientToReturn.ClientAgreements.Any(c => c.Id.Equals(clientAgreement.Id))) {
                    if (clientInDebt != null) {
                        Agreement first = clientToReturn.ClientAgreements.First(c => c.Id.Equals(clientAgreement.Id)).Agreement;

                        if (!first.ClientInDebts.Any(d => d.Id.Equals(clientInDebt.Id))) {
                            clientInDebt.Debt = debt;

                            first.ClientInDebts.Add(clientInDebt);
                        }
                    }
                } else {
                    if (clientInDebt != null) agreement.ClientInDebts.Add(clientInDebt);

                    agreement.Currency = currency;
                    agreement.Organization = organization;

                    clientAgreement.Agreement = agreement;

                    clientToReturn.ClientAgreements.Add(clientAgreement);
                }

                if (clientInDebt != null && !clientToReturn.ClientInDebts.Any(d => d.Id.Equals(clientInDebt.Id))) {
                    clientInDebt.Debt = debt;

                    clientToReturn.ClientInDebts.Add(clientInDebt);
                }
            }

            return client;
        };

        _connection.Query(
            "SELECT * FROM [Client] " +
            "LEFT JOIN [ClientInRole] " +
            "ON [ClientInRole].ClientID = [Client].ID " +
            "LEFT JOIN [ClientType] " +
            "ON [ClientType].ID = [ClientInRole].ClientTypeID " +
            "LEFT JOIN [ClientTypeTranslation] " +
            "ON [ClientTypeTranslation].ClientTypeID = [ClientType].ID " +
            "AND [ClientTypeTranslation].CultureCode = @Culture " +
            "AND [ClientTypeTranslation].Deleted = 0 " +
            "LEFT JOIN [ClientTypeRole] " +
            "ON [ClientTypeRole].ID = [ClientInRole].ClientTypeRoleID " +
            "LEFT JOIN [ClientTypeRoleTranslation] " +
            "ON [ClientTypeRoleTranslation].ClientTypeRoleID = [ClientTypeRole].ID " +
            "AND [ClientTypeRoleTranslation].CultureCode = @Culture " +
            "AND [ClientTypeRoleTranslation].Deleted = 0 " +
            "LEFT JOIN [RegionCode] " +
            "ON [RegionCode].ID = [Client].RegionCodeID " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].ClientID = [Client].ID " +
            "AND [ClientAgreement].Deleted = 0 " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].ID = [ClientAgreement].AgreementID " +
            "LEFT JOIN [Organization] " +
            "ON [Organization].ID = [Agreement].OrganizationID " +
            "LEFT JOIN [Currency] " +
            "ON [Currency].ID = [Agreement].CurrencyID " +
            "LEFT JOIN [ClientInDebt] " +
            "ON [ClientInDebt].AgreementID = [Agreement].ID " +
            "AND [ClientInDebt].Deleted = 0 " +
            "LEFT JOIN ( " +
            "SELECT [Debt].ID " +
            ",[Debt].Deleted " +
            ",[Debt].Created " +
            ",[Debt].NetUID " +
            ",[Debt].Updated " +
            ",DATEDIFF(DAY, [Debt].Created, GETUTCDATE()) AS [Days] " +
            ",[Debt].Total AS [Total] " +
            "FROM [Debt] " +
            ") AS [Debt] " +
            "ON ClientInDebt.DebtID = Debt.ID " +
            "WHERE [Client].ID = @Id " +
            "AND Agreement.ForReSale = 0 ",
            clientTypes,
            clientMapper,
            new {
                Id = workplaceToReturn.MainClientId,
                Culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName
            }
        );

        workplaceToReturn.MainClient = clientToReturn;

        return workplaceToReturn;
    }

    public IEnumerable<Workplace> GetWorkplacesByMainClientId(long id) {
        List<Workplace> workplaces = new();

        _connection.Query<Workplace, WorkplaceClientAgreement, ClientAgreement, Agreement, Workplace>(
            "SELECT * FROM [Workplace] " +
            "LEFT JOIN [WorkplaceClientAgreement] " +
            "ON [WorkplaceClientAgreement].WorkplaceID = [Workplace].ID " +
            "AND [WorkplaceClientAgreement].Deleted = 0 " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].ID = [WorkplaceClientAgreement].ClientAgreementID " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].ID = [ClientAgreement].AgreementID " +
            "WHERE [Workplace].MainClientID = @Id " +
            "AND [Workplace].Deleted = 0 ",
            (workplace, workplaceClientAgreement, clientAgreement, agreement) => {
                if (workplaceClientAgreement == null) {
                    workplaces.Add(workplace);
                    return workplace;
                }

                if (workplaces.Any(e => e.Id.Equals(workplaceClientAgreement.WorkplaceId))) {
                    Workplace first = workplaces.First(e => e.Id.Equals(workplaceClientAgreement.WorkplaceId));

                    clientAgreement.Agreement = agreement;
                    workplaceClientAgreement.ClientAgreement = clientAgreement;

                    first.WorkplaceClientAgreements.Add(workplaceClientAgreement);
                } else {
                    clientAgreement.Agreement = agreement;
                    workplaceClientAgreement.ClientAgreement = clientAgreement;

                    workplace.WorkplaceClientAgreements.Add(workplaceClientAgreement);

                    workplaces.Add(workplace);
                }

                return workplace;
            },
            new { Id = id });

        return workplaces;
    }


    public IEnumerable<Workplace> GetWorkplacesByMainClientNetId(Guid netId) {
        List<Workplace> workplaces = new();

        _connection.Query<Workplace, WorkplaceClientAgreement, ClientAgreement, Agreement, Workplace>(
            "SELECT [Workplace].*, [WorkplaceClientAgreement].*, [ClientAgreement].*, [Agreement].* FROM [Workplace] " +
            "LEFT JOIN [WorkplaceClientAgreement] " +
            "ON [WorkplaceClientAgreement].WorkplaceID = [Workplace].ID " +
            "AND [WorkplaceClientAgreement].Deleted = 0 " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].ID = [WorkplaceClientAgreement].ClientAgreementID " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].ID = [ClientAgreement].AgreementID " +
            "LEFT JOIN [Client] " +
            "ON [Client].ID = [Workplace].MainClientID " +
            "WHERE [Client].NetUID = @NetId " +
            "AND [Workplace].Deleted = 0 ",
            (workplace, workplaceClientAgreement, clientAgreement, agreement) => {
                if (workplaceClientAgreement == null) {
                    workplaces.Add(workplace);
                    return workplace;
                }

                if (workplaces.Any(e => e.Id.Equals(workplaceClientAgreement.WorkplaceId))) {
                    Workplace first = workplaces.First(e => e.Id.Equals(workplaceClientAgreement.WorkplaceId));

                    clientAgreement.Agreement = agreement;
                    workplaceClientAgreement.ClientAgreement = clientAgreement;

                    first.WorkplaceClientAgreements.Add(workplaceClientAgreement);
                } else {
                    clientAgreement.Agreement = agreement;
                    workplaceClientAgreement.ClientAgreement = clientAgreement;

                    workplace.WorkplaceClientAgreements.Add(workplaceClientAgreement);

                    workplaces.Add(workplace);
                }

                return workplace;
            },
            new { NetId = netId });

        return workplaces;
    }

    public IEnumerable<Workplace> GetWorkplacesByClientGroupId(long id) {
        return _connection.Query<Workplace>(
            "SELECT * FROM [Workplace] " +
            "WHERE [Workplace].ClientGroupID = @ClientGroupId " +
            "AND [Workplace].Deleted = 0 ",
            new { ClientGroupId = id });
    }

    public IEnumerable<Workplace> GetWorkplacesByClientGroupNetId(Guid netId) {
        return _connection.Query<Workplace, ClientGroup, Workplace>(
            "SELECT * FROM Workplace " +
            "LEFT JOIN ClientGroup " +
            "ON ClientGroup.ID = Workplace.ClientGroupID " +
            "WHERE Workplace.Deleted = 0 " +
            "AND ClientGroup.NetUID = @NetId ",
            (workplace, clientGroup) => {
                if (clientGroup != null) workplace.ClientGroup = clientGroup;

                return workplace;
            },
            new { NetId = netId });
    }

    public IEnumerable<WorkplaceClientAgreement> GetWorkplaceClientAgreementsByWorkplaceId(long id) {
        return _connection.Query<WorkplaceClientAgreement, ClientAgreement, Agreement, WorkplaceClientAgreement>(
            "SELECT * FROM [WorkplaceClientAgreement] " +
            "LEFT JOIN [ClientAgreement] " +
            "ON [ClientAgreement].ID = [WorkplaceClientAgreement].ClientAgreementID " +
            "LEFT JOIN [Agreement] " +
            "ON [Agreement].ID = [ClientAgreement].AgreementID " +
            "WHERE [WorkplaceClientAgreement].WorkplaceID = @Id " +
            "AND [WorkplaceClientAgreement].Deleted = 0 ",
            (workplaceClientAgreement, clientAgreement, agreement) => {
                if (clientAgreement == null) return workplaceClientAgreement;

                clientAgreement.Agreement = agreement;

                workplaceClientAgreement.ClientAgreement = clientAgreement;

                return workplaceClientAgreement;
            },
            new { Id = id });
    }
}