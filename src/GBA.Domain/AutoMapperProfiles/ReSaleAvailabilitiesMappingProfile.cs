using System.Linq;
using AutoMapper;
using GBA.Domain.EntityHelpers.GbaDataExportModels.ReSales;
using GBA.Domain.EntityHelpers.ReSaleModels;

namespace GBA.Domain.AutoMapperProfiles;

public sealed class ReSaleAvailabilitiesMappingProfile : Profile {
    public ReSaleAvailabilitiesMappingProfile() {
        CreateMap<GroupingReSaleAvailabilityModel, ReSaleAvailabilityDto>()
            .ForMember(dest => dest.Product, opt => opt.MapFrom(src => src.ConsignmentItems.First().Product));

        CreateMap<ReSaleAvailabilityWithTotalsModel, TotalReSaleAvailabilitiesDto>();
    }
}