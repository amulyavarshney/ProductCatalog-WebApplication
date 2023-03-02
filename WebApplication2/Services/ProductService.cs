using Microsoft.EntityFrameworkCore;
using WebApplication2.DAL;
using WebApplication2.Models;
using WebApplication2.ViewModels;

namespace WebApplication2.Services
{
    public class ProductService : IProductService
    {
        private readonly ProductCatalogContext _context;
        public ProductService(ProductCatalogContext context)
        {
            _context = context;
        }
        public async Task<IEnumerable<ProductViewModel>> GetAllAsync()
        {
            return await _context.Products
                .Select(p => new ProductViewModel
                {
                    Id = p.Id,
                    Name = p.Name,
                    BasePrice = p.BasePrice,
                    TaxRate = p.TaxRate,
                    Description = p.Description
                }).ToListAsync();
        }
        public async Task<ProductViewModel> GetByIdAsync(int id)
        {
            return ToViewModel(await FromId(id));
        }
        public async Task<ProductViewModel> CreateAsync(ProductCreateViewModel product)
        {
            var p = ToEntity(product);
            await _context.AddAsync(p);
            await _context.SaveChangesAsync();
            return ToViewModel(p);
        }
        public async Task<ProductViewModel> UpdateAsync(int id, ProductUpdateViewModel product)
        {
            var p = await FromId(id);

            p.Name = product.Name;
            p.Description = product.Description;

            await _context.SaveChangesAsync();

            return ToViewModel(p);
        }
        public async Task DeleteAsync(int id)
        {
            var product = await FromId(id);
            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
        }
        private ProductViewModel ToViewModel(Product product)
        {
            return new ProductViewModel
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                BasePrice = product.BasePrice,
                TaxRate = product.TaxRate
            };
        }

        private Product ToEntity(ProductCreateViewModel product)
        {
            return new Product
            {
                Name = product.Name,
                Description = product.Description,
                BasePrice = product.BasePrice,
                TaxRate = product.TaxRate
            };
        }

        private async Task<Product> FromId(int id)
        {
            return await _context.Products.FirstAsync(p => p.Id == id);
        }
    }
}
