namespace GBA.Domain.Entities.ConsignmentNoteSettings;

public sealed class ConsignmentNoteSetting : EntityBase {
    public string Name { get; set; }

    public string Number { get; set; }

    public string BrandAndNumberCar { get; set; }

    public string TrailerNumber { get; set; }

    public string Driver { get; set; }

    public string Carrier { get; set; }

    public string TypeTransportation { get; set; }

    public string UnloadingPoint { get; set; }

    public string LoadingPoint { get; set; }

    public string Customer { get; set; }

    public bool ForReSale { get; set; }

    public string CarLabel { get; set; }

    public int CarLength { get; set; }

    public int CarWidth { get; set; }

    public int CarHeight { get; set; }

    public decimal CarNetWeight { get; set; }

    public decimal CarGrossWeight { get; set; }

    public string TrailerLabel { get; set; }

    public int TrailerLength { get; set; }

    public int TrailerWidth { get; set; }

    public int TrailerHeight { get; set; }

    public decimal TrailerNetWeight { get; set; }

    public decimal TrailerGrossWeight { get; set; }
}