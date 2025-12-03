using Microsoft.AspNetCore.Mvc;
using Product.API.Services;
using System.Collections.Generic;
using System.Threading.Tasks;
using Product.Application.Dtos;
using Microsoft.AspNetCore.Authorization;

namespace Product.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductController : ControllerBase
    {
        private readonly ProductService _productService;

        public ProductController(Services.ProductService productService)
        {
            _productService = productService;
        }

        [HttpGet("all")]
        public async Task<ActionResult<List<Domain.Entities.ProductEntity>>> GetAllProducts()
        {
            Console.WriteLine("Fetching all products - 1");
            var products = await _productService.GetAllProductsAsync();
            return Ok(products);
        }

        [HttpPost("add")]
        public async Task<ActionResult<Domain.Entities.ProductEntity>> SaveProduct(ProductAddDto productAddDto)
        {
            var product = await _productService.SaveProductAsync(productAddDto);
            return Ok(product);
        }
    }
}