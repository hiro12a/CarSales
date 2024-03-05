using CarSales.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace CarSales.DataAccess.Repository.IRepository
{
    public interface IImageRepository : IRepository<Image>
    {
        // For updating category
        public void Update(Image image);
    }
}
