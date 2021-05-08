using System;
using System.Collections.Generic;
using System.Linq;
using ToyApi.Db.Models;

namespace ToyApi.Db
{
    public class ToyRepository
        : IToyRepository
    {
        private readonly MyFavoriteToyDbContext _dbContext;
        public ToyRepository(MyFavoriteToyDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        public int Create(Toy toy)
        {
            var newId = _dbContext.Toys.Select(t => t.ToyId).Max() + 1;
            toy.ToyId = newId;
            toy.LastUpdated = DateTime.Now;

            _dbContext.Toys.Add(toy);
            var inserted=_dbContext.SaveChanges();
            return inserted;
        }

        public IEnumerable<Toy> GetTopFive()
        {
            var result = _dbContext.Toys.OrderBy(t => t.Like).Take(5);
            return result;
        }

        public Toy Update(Toy toy)
        {
            var current = _dbContext.Toys.FirstOrDefault(t => t.ToyId == toy.ToyId);
            if(current!=null)
            {
                current.LastUpdated = toy.LastUpdated;
                current.Like = toy.Like;
                current.Nickname = toy.Nickname;
                current.Description = toy.Description;
                current.Photo = toy.Photo;

                _dbContext.SaveChanges();

                return current;
            }
            return null;
        }
    }
}
