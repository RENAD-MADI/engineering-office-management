using Microsoft.EntityFrameworkCore;
using RenadWebApp.Models.DataModel;
using RenadWebApp.Models.DataModel.FinaicalModels;

namespace RenadWebApp.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<LoginModel> Login { get; set; }
        public DbSet<ClientModels> Clients { get; set; }
        public DbSet<ClientContract> ClientContract { get; set; }
        public DbSet<ContractModel> Contracts { get; set; }
        public DbSet<PaymentModel> Payment { get; set; }
        public DbSet<ContractPayment> ContractPayment { get; set; }
        public DbSet<EngModel> Eng { get; set; }
        public DbSet<EngContract> EngContract { get; set; }
        public DbSet<FinaicalRequest> FinaicalRequests { get; set; }
        public DbSet<ContractFinaical> ContractFinaical { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Client <-> Contract
            builder.Entity<ClientModels>().HasKey(p => p.Id);
            builder.Entity<ClientContract>().HasKey(x => new { x.ContractId, x.ClientId });
            builder.Entity<ClientContract>()
                .HasOne(x => x.Contract).WithMany(c => c.ClientContract)
                .HasForeignKey(x => x.ContractId).OnDelete(DeleteBehavior.NoAction);
            builder.Entity<ClientContract>()
                .HasOne(x => x.Client).WithMany(c => c.ClientContract)
                .HasForeignKey(x => x.ClientId).OnDelete(DeleteBehavior.NoAction);

            // Engineer <-> Contract
            builder.Entity<EngModel>().HasKey(p => p.Id);
            builder.Entity<EngContract>().HasKey(x => new { x.ContractId, x.EngId });
            builder.Entity<EngContract>()
                .HasOne(x => x.Contract).WithMany(c => c.EngContract)
                .HasForeignKey(x => x.ContractId).OnDelete(DeleteBehavior.NoAction);
            builder.Entity<EngContract>()
                .HasOne(x => x.Eng).WithMany(e => e.EngContract)
                .HasForeignKey(x => x.EngId).OnDelete(DeleteBehavior.NoAction);

            // Contract <-> Payment
            builder.Entity<PaymentModel>().HasKey(p => p.Id);
            builder.Entity<ContractPayment>().HasKey(x => new { x.ContractId, x.PaymentId });
            builder.Entity<ContractPayment>()
                .HasOne(x => x.Contract).WithMany(c => c.ContractPayment)
                .HasForeignKey(x => x.ContractId).OnDelete(DeleteBehavior.NoAction);
            builder.Entity<ContractPayment>()
                .HasOne(x => x.Payment).WithMany(p => p.ContractPayment)
                .HasForeignKey(x => x.PaymentId).OnDelete(DeleteBehavior.NoAction);

            // Contract <-> Financial request
            builder.Entity<FinaicalRequest>().HasKey(p => p.Id);
            builder.Entity<ContractFinaical>().HasKey(x => new { x.ContractId, x.FinaicalId });
            builder.Entity<ContractFinaical>()
                .HasOne(x => x.Contract).WithMany(c => c.ContractFinaical)
                .HasForeignKey(x => x.ContractId).OnDelete(DeleteBehavior.NoAction);
            builder.Entity<ContractFinaical>()
                .HasOne(x => x.Finaical).WithMany(f => f.ContractFinaical)
                .HasForeignKey(x => x.FinaicalId).OnDelete(DeleteBehavior.NoAction);
        }
    }
}
