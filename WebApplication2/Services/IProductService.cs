using WebApplication2.ViewModels;

namespace WebApplication2.Services
{
    public interface IProductService
    {
        Task<IEnumerable<ProductViewModel>> GetAllAsync();
        Task<ProductViewModel> GetByIdAsync(int id);
        Task<ProductViewModel> CreateAsync(ProductCreateViewModel product);
        Task<ProductViewModel> UpdateAsync(int id, ProductUpdateViewModel product);
        Task DeleteAsync(int id);
    }
}
