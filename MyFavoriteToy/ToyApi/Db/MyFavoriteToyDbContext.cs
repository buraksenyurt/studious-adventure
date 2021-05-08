using Microsoft.EntityFrameworkCore;
using System;
using ToyApi.Db.Models;

namespace ToyApi.Db
{
    public class MyFavoriteToyDbContext
        : DbContext
    {
        public MyFavoriteToyDbContext(DbContextOptions<MyFavoriteToyDbContext> options) : base(options)
        {
        }

        public DbSet<Toy> Toys { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Toy>().HasData(new Toy
            {
                ToyId = 1,
                Nickname = "Kırmızı Şimşek",
                Description = "En sevdiğim oyuncak arabamdır :)",
                LastUpdated = DateTime.Now.AddDays(-1),
                Photo = PhotoUtility.GetBase64("kirmizi_simsek.jpg", "image/jpeg")
            });

            modelBuilder.Entity<Toy>().HasData(new Toy
            {
                ToyId =2,
                Nickname = "Çekici Meytır",
                Description = "Oğlumla severek izlediğim animasyonlardan Cars'ın eğlenceli karakteri Meytır.",
                LastUpdated = DateTime.Now,
                Photo = PhotoUtility.GetBase64("cekici_meytir.jpg", "image/jpeg")
            });
        }
    }
}
