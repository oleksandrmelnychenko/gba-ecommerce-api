using System;
using System.Collections.Generic;
using GBA.Domain.Entities;

namespace GBA.Domain.Repositories.Categories.Contracts;

public interface ICategoryRepository {
    long Add(Category category);

    void Update(Category category);

    Category GetById(long id);

    Category GetByNetId(Guid netId);

    List<Category> GetAll();

    void Remove(Guid netId);
}