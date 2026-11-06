using AgendaPet.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AgendaPet.Api.Data;

/// <summary>Mapeamento do modelo relacional: chaves estrangeiras e índices.</summary>
public partial class AppDbContext
{
    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<Cliente>(e =>
        {
            e.Property(x => x.Nome).HasMaxLength(150).IsRequired();
            e.Property(x => x.Telefone).HasMaxLength(30).IsRequired();
            e.HasIndex(x => x.Telefone);
        });

        mb.Entity<Animal>(e =>
        {
            e.Property(x => x.Nome).HasMaxLength(100).IsRequired();
            e.HasOne<Cliente>().WithMany().HasForeignKey(x => x.ClienteId).OnDelete(DeleteBehavior.Restrict);
        });

        mb.Entity<Servico>(e =>
        {
            e.Property(x => x.Tipo).HasConversion<string>();
            e.HasIndex(x => x.Tipo).IsUnique();
        });

        mb.Entity<Agendamento>(e =>
        {
            e.Property(x => x.Status).HasConversion<string>();
            e.HasOne<Animal>().WithMany().HasForeignKey(x => x.AnimalId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Servico>().WithMany().HasForeignKey(x => x.ServicoId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Profissional>().WithMany().HasForeignKey(x => x.ProfissionalId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Baia>().WithMany().HasForeignKey(x => x.BaiaId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.Inicio, x.Fim });
            e.HasIndex(x => x.ProximoRetorno);
        });
    }
}
