namespace GBA.Domain.Entities.Consignments;

public sealed class ConsignmentItemMovementTypeName : EntityBase {
    public string NamePl { get; set; }

    public string NameUa { get; set; }

    public ConsignmentItemMovementType MovementType { get; set; }
}