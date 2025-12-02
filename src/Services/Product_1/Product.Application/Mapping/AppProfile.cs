

using AutoMapper;
using Product.Application.Dtos;
using Product.Domain.Entities;

namespace Product.Application.Mapping
{
    public class AppProfile : Profile
    {
        public AppProfile()
        {
            // User
            CreateMap<ProductAddDto, ProductEntity>();
            
        }
    }
}