using System.Collections.Generic;
using System.Data;
using System.Linq;
using Akka.Actor;
using AllegroWebApiSandbox;
using GBA.Domain.DbConnectionFactory.Contracts;
using GBA.Domain.Entities.AllegroServices;
using GBA.Domain.EntityHelpers;
using GBA.Domain.Messages.AllegroServices.Categories;
using GBA.Domain.Messages.AllegroServices.WebApi;
using GBA.Domain.Repositories.AllegroServices.Contracts;
using GBA.Services.ActorHelpers.ActorNames;
using GBA.Services.ActorHelpers.ReferenceManager;

namespace GBA.Services.Actors.AllegroServices;

public sealed class AllegroCategoriesActor : ReceiveActor {
    private readonly IAllegroServicesRepositoriesFactory _allegroServicesRepositoriesFactory;
    private readonly IDbConnectionFactory _connectionFactory;

    public AllegroCategoriesActor(
        IDbConnectionFactory connectionFactory,
        IAllegroServicesRepositoriesFactory allegroServicesRepositoriesFactory) {
        _connectionFactory = connectionFactory;
        _allegroServicesRepositoriesFactory = allegroServicesRepositoriesFactory;

        Receive<GetAllCategoriesMessage>(_ => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            IAllegroCategoryRepository allegroCategoryRepository = _allegroServicesRepositoriesFactory.NewAllegroCategoryRepository(connection);

            List<AllegroCategory> toReturn = allegroCategoryRepository.GetAllRootCategoriesWithSubCategories();

            foreach (AllegroCategory category in toReturn) LoadSubCategories(allegroCategoryRepository, category.SubCategories.Where(s => !s.IsLeaf));

            Sender.Tell(toReturn);
        });

        Receive<SearchForCategoriesMessage>(message => {
            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            IAllegroCategoryRepository allegroCategoryRepository = _allegroServicesRepositoriesFactory.NewAllegroCategoryRepository(connection);

            if (message.Limit <= 0) message.Limit = 20;
            if (message.Offset < 0) message.Offset = 0;

            List<AllegroCategory> categories = allegroCategoryRepository.GetAllFromSearch(message.Value, message.Limit, message.Offset);

            Sender.Tell(
                categories
                    .Select(category => new AllegroSearchResponse { CategoryTree = allegroCategoryRepository.GetTreeByCategoryId(category.CategoryId) })
                    .ToList()
            );
        });

        ReceiveAsync<SynchronizeAllCategoriesMessage>(async _ => {
            CatInfoType[] originalCategories =
                await ActorReferenceManager.Instance.Get(AllegroServicesActorNames.ALLEGRO_WEB_API_ACTOR).Ask<CatInfoType[]>(new GetAllAllegroOriginalCategoriesMessage());

            using IDbConnection connection = _connectionFactory.NewSqlConnection();
            IAllegroCategoryRepository allegroCategoryRepository = _allegroServicesRepositoriesFactory.NewAllegroCategoryRepository(connection);

            List<AllegroCategory> storedCategories = allegroCategoryRepository.GetAll();

            //Deleting outdated categories, that was deleted on Allegro.pl
            allegroCategoryRepository.RemoveByIds(
                storedCategories
                    .Where(category => !originalCategories.Any(o => o.catId.Equals(category.CategoryId)))
                    .Select(c => c.Id)
            );

            //Retrieving stored categories to dismiss deleted categories on previous step
            storedCategories = allegroCategoryRepository.GetAll();

            //Storing newly added categories from Allegro.pl
            allegroCategoryRepository.Add(
                originalCategories
                    .Where(o => !storedCategories.Any(c => c.CategoryId.Equals(o.catId)))
                    .Select(o => new AllegroCategory {
                        CategoryId = o.catId,
                        IsLeaf = o.catIsLeaf,
                        Name = o.catName,
                        ParentCategoryId = o.catParent,
                        Position = o.catPosition
                    })
            );

            //Updating existing categories, if was changed on Allegro.pl
            List<AllegroCategory> toUpdate = new();

            foreach (AllegroCategory category in storedCategories) {
                CatInfoType originalCategory = originalCategories.First(o => o.catId.Equals(category.CategoryId));

                if (category.Name.Equals(originalCategory.catName)) continue;

                category.Name = originalCategory.catName;

                toUpdate.Add(category);
            }

            if (toUpdate.Any()) allegroCategoryRepository.Update(toUpdate);

            //Clean deleted categories
            allegroCategoryRepository.DeleteRemovedCategories();
        });
    }

    private static void LoadSubCategories(IAllegroCategoryRepository allegroCategoryRepository, IEnumerable<AllegroCategory> subCategories) {
        foreach (AllegroCategory subCategory in subCategories) {
            subCategory.SubCategories = allegroCategoryRepository.GetAllSubCategoriesByParentCategoryId(subCategory.CategoryId);

            if (subCategory.SubCategories.Any(s => !s.IsLeaf))
                LoadSubCategories(allegroCategoryRepository, subCategory.SubCategories.Where(s => !s.IsLeaf));
        }
    }
}