using System.Collections.Generic;
using GBA.Domain.Entities;

namespace GBA.Domain.Repositories.Supports.Contracts;

public interface ISupportVideoRepository {
    long Add(SupportVideo supportVideo);

    void Update(SupportVideo supportVideo);

    SupportVideo GetById(long id);

    IEnumerable<SupportVideo> GetAll();
}