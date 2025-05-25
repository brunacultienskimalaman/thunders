using Microsoft.EntityFrameworkCore;
using Thunders.TechTest.ApiService.Models;

namespace Thunders.TechTest.ApiService.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options): base(options)
        {
        }

        public DbSet<UtilizacaoPedagio> Utilizacoes { get; set; }
        public DbSet<Praca> Pracas { get; set; }
        public DbSet<RelatorioProcessado> Relatorios { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<UtilizacaoPedagio>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.DataUtilizacao)
                    .HasColumnType("datetime2(3)");

                entity.Property(e => e.DataInsercao)
                    .HasColumnType("datetime2(3)")
                    .HasDefaultValueSql("GETDATE()");

                entity.Property(e => e.ValorPago)
                    .HasPrecision(10, 2);

                entity.Property(e => e.TipoVeiculo)
                    .HasConversion<int>();

                
                entity.HasIndex(e => new { e.DataUtilizacao, e.Cidade })
                    .HasDatabaseName("IX_DataUtilizacao_Cidade");

                entity.HasIndex(e => new { e.PracaId, e.DataUtilizacao })
                    .HasDatabaseName("IX_PracaId_Mes");

                entity.HasIndex(e => new { e.Estado, e.Cidade })
                    .HasDatabaseName("IX_Estado_Cidade");

                
                entity.HasOne(e => e.Praca)
                    .WithMany(p => p.Utilizacoes)
                    .HasForeignKey(e => e.PracaId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Praca>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Ativa)
                    .HasDefaultValue(true);

                entity.HasIndex(e => new { e.Estado, e.Cidade })
                    .HasDatabaseName("IX_Praca_Estado_Cidade");
            });

            modelBuilder.Entity<RelatorioProcessado>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.DataProcessamento)
                    .HasColumnType("datetime2(3)")
                    .HasDefaultValueSql("GETDATE()");

                entity.Property(e => e.Status)
                    .HasConversion<int>();

                entity.HasIndex(e => new { e.TipoRelatorio, e.DataProcessamento })
                    .HasDatabaseName("IX_TipoRelatorio_Data");

                entity.HasIndex(e => e.IdSolicitacao)
                    .IsUnique()
                    .HasDatabaseName("IX_IdSolicitacao");
            });
        }
    }
}
