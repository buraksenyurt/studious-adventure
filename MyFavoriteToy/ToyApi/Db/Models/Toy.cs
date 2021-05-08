using System;

namespace ToyApi.Db.Models
{
    public class Toy
    {
        public int ToyId { get; set; }
        public string Nickname { get; set; }
        public string Description { get; set; }
        public DateTime LastUpdated { get; set; }
        public string Photo { get; set; }
    }
}
