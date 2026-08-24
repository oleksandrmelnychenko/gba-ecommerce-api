using System.Data;
using GBA.Domain.Entities.PaymentOrders;
using GBA.Domain.EntityHelpers.TotalDashboards.PaymentRegisters;
using GBA.Domain.Repositories.PaymentOrders;
using Moq;

namespace GBA.Ecommerce.Api.Tests;

public sealed class SelectedRetailPaymentRegisterRepositoryTests {
    [Fact]
    public void SelectedCard_IsRetailScopedAndDuplicateSelectionFailsClosed() {
        var selectedFixture = CreateFixture(CreateSelectedCardTable(81));
        var selectedRepository = new PaymentRegisterRepository(
            selectedFixture.Connection);

        PaymentRegister selected = selectedRepository.GetIsSelected();

        Assert.NotNull(selected);
        Assert.Equal(81, selected.Id);
        Assert.Contains(
            "SELECT TOP (2)",
            selectedFixture.Command.CommandText,
            StringComparison.Ordinal);
        Assert.Contains(
            "[Type] = @Type",
            selectedFixture.Command.CommandText,
            StringComparison.Ordinal);
        Assert.Contains(
            "[IsForRetail] = 1",
            selectedFixture.Command.CommandText,
            StringComparison.Ordinal);
        Assert.Contains(
            "ORDER BY [ID]",
            selectedFixture.Command.CommandText,
            StringComparison.Ordinal);

        var duplicateFixture = CreateFixture(
            CreateSelectedCardTable(81, 82));
        var duplicateRepository = new PaymentRegisterRepository(
            duplicateFixture.Connection);

        Assert.Throws<InvalidOperationException>(
            duplicateRepository.GetIsSelected);
    }

    private static DataTable CreateSelectedCardTable(params long[] ids) {
        var table = new DataTable();
        table.Columns.Add("ID", typeof(long));
        table.Columns.Add("NetUID", typeof(Guid));
        table.Columns.Add("Type", typeof(int));
        table.Columns.Add("IsForRetail", typeof(bool));
        table.Columns.Add("IsSelected", typeof(bool));
        table.Columns.Add("Deleted", typeof(bool));

        foreach (long id in ids)
            table.Rows.Add(
                id,
                Guid.NewGuid(),
                (int)PaymentRegisterType.Card,
                true,
                true,
                false);

        return table;
    }

    private static QueryFixture CreateFixture(DataTable table) {
        var parameters = new Mock<IDataParameterCollection>();
        parameters.Setup(collection => collection.Add(It.IsAny<object>()))
            .Returns(0);

        var command = new Mock<IDbCommand>();
        command.SetupAllProperties();
        command.SetupGet(value => value.Parameters)
            .Returns(parameters.Object);
        command.Setup(value => value.CreateParameter())
            .Returns(() => new Mock<IDbDataParameter>().Object);
        command.Setup(value => value.ExecuteReader())
            .Returns(() => table.CreateDataReader());
        command.Setup(value => value.ExecuteReader(
                It.IsAny<CommandBehavior>()))
            .Returns(() => table.CreateDataReader());

        var connection = new Mock<IDbConnection>();
        connection.SetupGet(value => value.State)
            .Returns(ConnectionState.Open);
        connection.Setup(value => value.CreateCommand())
            .Returns(command.Object);

        return new QueryFixture(connection.Object, command.Object);
    }

    private sealed record QueryFixture(
        IDbConnection Connection,
        IDbCommand Command);
}
