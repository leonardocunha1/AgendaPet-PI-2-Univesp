using AgendaPet.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AgendaPet.Api.Data;

public partial class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Animal> Animais => Set<Animal>();
    public DbSet<Servico> Servicos => Set<Servico>();
    public DbSet<Profissional> Profissionais => Set<Profissional>();
    public DbSet<Baia> Baias => Set<Baia>();
    public DbSet<Agendamento> Agendamentos => Set<Agendamento>();
    public DbSet<ConflitoEvitado> ConflitosEvitados => Set<ConflitoEvitado>();
}
