using System.Collections.Generic;
using GBA.Domain.TranslationEntities;

namespace GBA.Domain.Entities.Transporters;

public sealed class TransporterType : EntityBase {
    public TransporterType() {
        Transporters = new HashSet<Transporter>();

        TransporterTypeTranslations = new HashSet<TransporterTypeTranslation>();
    }

    public string Name { get; set; }

    public ICollection<Transporter> Transporters { get; set; }

    public ICollection<TransporterTypeTranslation> TransporterTypeTranslations { get; set; }
}