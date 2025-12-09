using Microsoft.EntityFrameworkCore;
using MiddlewareNexi.Models;

namespace MiddlewareNexi.Data
{
    public class PagamentoDbContext : DbContext
    {
        public PagamentoDbContext(DbContextOptions<PagamentoDbContext> options) : base(options) { }

        public DbSet<Pagamento> Pagamenti { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Aggiunge un indice su ObjectId nella tabella Pagamenti
            modelBuilder.Entity<Pagamento>()
                .HasIndex(p => p.ObjectId)
                .HasDatabaseName("IX_Pagamenti_ObjectId");
        }
    }
}
