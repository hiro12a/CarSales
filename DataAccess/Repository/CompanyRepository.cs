﻿using CarSales.DataAccess.Data;
using CarSales.DataAccess.Repository.IRepository;
using CarSales.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarSales.DataAccess.Repository
{
    public class CompanyRepository : Repository<Company>, ICompanyRepository
    {
        private ApplicationDbContext _db;
        public CompanyRepository(ApplicationDbContext db) : base(db) 
        {
            _db = db;
        }

        public void Update(Company company)
        {
            _db.Companys.Update(company);
        }
    }
}
