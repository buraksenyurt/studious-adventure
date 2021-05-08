using System.Collections.Generic;
using ToyApi.Db.Models;

namespace ToyApi.Db
{
    public interface IToyRepository
    {
        IEnumerable<Toy> GetTopFive();
        int Create(Toy toy);
        Toy Update(Toy toy);
    }
}
