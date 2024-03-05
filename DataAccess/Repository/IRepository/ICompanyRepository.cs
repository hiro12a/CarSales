using CarSales.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace CarSales.DataAccess.Repository.IRepository
{
    public interface ICompanyRepository : IRepository<Company>
    {
        // For updating users
        public void Update(Company company);
    }
}
