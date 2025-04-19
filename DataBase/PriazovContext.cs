using DataBase.Models;
using Microsoft.EntityFrameworkCore;

namespace DataBase
{
    public class PriazovContext : DbContext
    {

        public PriazovContext(DbContextOptions<PriazovContext> options) : base(options)
        {

        }
        //Создание таблиц в бд
        public DbSet<User> Users { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<UserSession> Sessions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

            //Настройки таблиц
            modelBuilder.Entity<Project>()
                .Property<Guid>("Id")
                .ValueGeneratedOnAdd();
            modelBuilder.Entity<Project>()
               .Property<Guid>("Id").IsRequired();
            modelBuilder.Entity<Project>()
            .HasKey(i => i.Id);

            modelBuilder.Entity<Company>()
                .HasMany(e => e.Projects)
                .WithOne(e => e.Company)
                .HasForeignKey(e => e.CompanyId)
                .IsRequired();

            modelBuilder.Entity<UserSession>()
                .Property<Guid>("Id")
                .ValueGeneratedOnAdd();
            modelBuilder.Entity<UserSession>()
              .Property<Guid>("Id").IsRequired();
            modelBuilder.Entity<UserSession>()
                .HasKey(i => i.Id);

            // Настройка наследования (TPH)
            modelBuilder.Entity<User>()
                .ToTable("Users")
                .HasDiscriminator<string>("UserType")
                .HasValue<Manager>("Manager")
                .HasValue<Company>("Company");

            // Явная настройка связи (1 к многим)
            modelBuilder.Entity<UserSession>()
                .HasOne(us => us.User)
                .WithMany()
                .HasForeignKey(us => us.UserId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            // Убедимся, что не создаются лишние таблицы
            modelBuilder.Entity<Manager>().ToTable("Users");
            modelBuilder.Entity<Company>().ToTable("Users");


            //modelBuilder.Entity<Address>()
            //    .Property<uint>("Id")
            //    .ValueGeneratedOnAdd();
            //modelBuilder.Entity<Address>()
            //   .Property<uint>("Id").IsRequired();
            //modelBuilder.Entity<Address>()
            //    .HasKey(i => i.Id);

            //Создание элементов в таблице индустрий
            //modelBuilder.Entity<Industry>().HasData(
            //    new Industry() { Name = "Образовательное учреждение", Id = 1 },
            //    new Industry() { Name = "Научно-исследовательский институт", Id = 2 },
            //    new Industry() { Name = "Научно-образовательный проект", Id = 3 },
            //    new Industry() { Name = "Государственное учреждение", Id = 4 },
            //    new Industry() { Name = "Компания, ведущая коммерческую деятельность", Id = 5 },
            //    new Industry() { Name = "Стартап", Id = 6 },
            //    new Industry() { Name = "Финансовый инструмент (банк, фонд и другие)", Id = 7 },
            //    new Industry() { Name = "Акселератор/инкубатор/технопарк", Id = 8 },
            //    new Industry() { Name = "Ассоциация/объединение", Id = 9 },
            //    new Industry() { Name = "Инициатива", Id = 10 },
            //    new Industry() { Name = "Отраслевое событие/научная конференция", Id = 11 },
            //    new Industry() { Name = "Другое", Id = 12 }
            //    );
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