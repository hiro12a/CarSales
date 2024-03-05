using CarSales.DataAccess.Data;
using CarSales.DataAccess.Repository.IRepository;
using CarSales.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarSales.DataAccess.Repository
{
    public class ImageRepository : Repository<Image>, IImageRepository
    {
        private ApplicationDbContext _db;
        public ImageRepository(ApplicationDbContext db) : base(db) 
        {
            _db = db;
        }

        public void Update(Image image)
        {
            _db.Images.Update(image);
        }
    }
}
