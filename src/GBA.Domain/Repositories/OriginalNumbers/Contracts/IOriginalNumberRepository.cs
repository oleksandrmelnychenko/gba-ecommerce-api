using System;
using System.Collections.Generic;
using GBA.Domain.Entities;

namespace GBA.Domain.Repositories.OriginalNumbers.Contracts;

public interface IOriginalNumberRepository {
    long Add(OriginalNumber originalNumber);

    void Update(OriginalNumber originalNumber);

    OriginalNumber GetById(long id);

    OriginalNumber GetByNetId(Guid netId);

    OriginalNumber GetByNumber(string number);

    List<OriginalNumber> GetAll();

    void Remove(Guid netId);

    void RemoveAllByIds(IEnumerable<long> ids);

    void DeleteAllByIds(IEnumerable<long> ids);
}