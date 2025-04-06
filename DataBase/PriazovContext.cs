using DataBase.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace DataBase
{
    public class PriazovContext : DbContext
    {

        public PriazovContext(DbContextOptions<PriazovContext> options) : base(options)
        {

        }
        //Создание таблиц в бд
        public DbSet<Manager> Managers { get; set; }
        public DbSet<Company> Companies { get; set; }
        public DbSet<Industry> Industries { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

            //Настройки таблиц
            modelBuilder.Entity<Industry>()
                .Property<uint>("Id")
                .ValueGeneratedOnAdd();
            modelBuilder.Entity<Industry>()
               .Property<uint>("Id").IsRequired();
            modelBuilder.Entity<Industry>()
                .HasKey(i => i.Id);

            //modelBuilder.Entity<Address>()
            //    .Property<uint>("Id")
            //    .ValueGeneratedOnAdd();
            //modelBuilder.Entity<Address>()
            //   .Property<uint>("Id").IsRequired();
            //modelBuilder.Entity<Address>()
            //    .HasKey(i => i.Id);

            modelBuilder.Entity<Manager>()
                .Property<Guid>("Id")
                .ValueGeneratedOnAdd();
            modelBuilder.Entity<Manager>()
              .Property<Guid>("Id").IsRequired();
            modelBuilder.Entity<Manager>()
                .HasKey(i => i.Id);

            modelBuilder.Entity<Company>()
                .Property<Guid>("Id")
                .ValueGeneratedOnAdd();
            modelBuilder.Entity<Company>()
              .Property<Guid>("Id").IsRequired();
            modelBuilder.Entity<Company>()
                .HasKey(i => i.Id);

            //modelBuilder.Entity<Manager>().HasAlternateKey(u => u.Email);
            //modelBuilder.Entity<Company>().HasAlternateKey(u => u.Email);

            //Создание элементов в таблице индустрий
            modelBuilder.Entity<Industry>().HasData(
                new Industry() { Name = "Образовательное учреждение", Id = 1 },
                new Industry() { Name = "Научно-исследовательский институт", Id = 2 },
                new Industry() { Name = "Научно-образовательный проект", Id = 3 },
                new Industry() { Name = "Государственное учреждение", Id = 4 },
                new Industry() { Name = "Компания, ведущая коммерческую деятельность", Id = 5 },
                new Industry() { Name = "Стартап", Id = 6 },
                new Industry() { Name = "Финансовый инструмент (банк, фонд и другие)", Id = 7 },
                new Industry() { Name = "Акселератор/инкубатор/технопарк", Id = 8 },
                new Industry() { Name = "Ассоциация/объединение", Id = 9 },
                new Industry() { Name = "Инициатива", Id = 10 },
                new Industry() { Name = "Отраслевое событие/научная конференция", Id = 11 },
                new Industry() { Name = "Другое", Id = 12 }
                );
            //modelBuilder.Entity<Address>().HasData(
            //    new Address() { Name = "Краснодарский край", Id = 1 },
            //    new Address() { Name = "Ростовская область", Id = 2 },
            //    new Address() { Name = "ЛНР", Id = 3 },
            //    new Address() { Name = "ДНР", Id = 4 },
            //    new Address() { Name = "Херсонская область", Id = 5 },
            //    new Address() { Name = "Запорожская область", Id = 6 }
            //    );
        }
    }
}