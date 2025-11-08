using Microsoft.EntityFrameworkCore;
using EstoqueService.Domain.Entities;

namespace EstoqueService.Infrastructure.Data;

public class EstoqueDbContext : DbContext
{
    public EstoqueDbContext(DbContextOptions<EstoqueDbContext> options) 
        : base(options)
    {
    }

    public DbSet<Produto> Produtos { get; set; }

    public DbSet<OperacaoProcessada> OperacoesProcessadas { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Produto>(entity =>
        {
            entity.ToTable("Produtos");

            entity.HasKey(p => p.Id);

            entity.HasIndex(p => p.Codigo)
                .IsUnique()
                .HasDatabaseName("IX_Produtos_Codigo");

            entity.HasIndex(p => p.Descricao)
                .HasDatabaseName("IX_Produtos_Descricao");

            entity.HasIndex(p => p.Deletado)
                .HasDatabaseName("IX_Produtos_Deletado");

            entity.Property(p => p.Codigo)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(p => p.Descricao)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(p => p.Saldo)
                .HasPrecision(18, 2)
                .IsRequired();

            entity.Property(p => p.DataCriacao)
                .HasDefaultValueSql("(now() AT TIME ZONE 'utc')");

            entity.HasQueryFilter(p => !p.Deletado);
        });

        modelBuilder.Entity<OperacaoProcessada>(entity =>
        {
            entity.ToTable("OperacoesProcessadas");

            entity.HasKey(o => o.IdempotencyKey);

            entity.HasIndex(o => o.DataProcessamento)
                .HasDatabaseName("IX_OperacoesProcessadas_DataProcessamento");

            entity.HasIndex(o => o.TipoOperacao)
                .HasDatabaseName("IX_OperacoesProcessadas_TipoOperacao");

            entity.Property(o => o.IdempotencyKey)
                .HasMaxLength(100);

            entity.Property(o => o.TipoOperacao)
                .HasMaxLength(50);

            entity.Property(o => o.Resultado)
                .HasMaxLength(2000);

            entity.Property(o => o.DataProcessamento)
                .HasDefaultValueSql("(now() AT TIME ZONE 'utc')");
        });
    }

    public override int SaveChanges()
    {
        AtualizarTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        AtualizarTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void AtualizarTimestamps()
    {
        var entries = ChangeTracker.Entries<Produto>()
            .Where(e => e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
            entry.Entity.DataAtualizacao = DateTime.UtcNow;
        }
    }
}