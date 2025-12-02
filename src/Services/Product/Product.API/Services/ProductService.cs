using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Product.Application.Dtos;
using Product.Domain.Entities;
using Product.API.Persistence;

namespace Product.API.Services
{

    public class ProductService
    {
        
        private readonly ApplicationDbContext _db;
        private readonly IMapper _mapper;
        public ProductService(ApplicationDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }


        // Product Save
        public async Task<ProductEntity> SaveProductAsync(ProductAddDto productAddDto)
        {
            var product = _mapper.Map<ProductEntity>(productAddDto);
            _db.Products.Add(product);
            await _db.SaveChangesAsync();
            return product;
        }

        public Task<List<ProductEntity>> GetAllProductsAsync()
        {
            var products = _db.Products.ToList();
            return Task.FromResult(products);
        }



    }

}