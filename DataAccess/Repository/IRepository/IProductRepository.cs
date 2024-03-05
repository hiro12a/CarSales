using CarSales.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace CarSales.DataAccess.Repository.IRepository
{
    public interface IProductRepository : IRepository<Product>
    {
        // For updating product
        public void Update(Product product);
    }
}
