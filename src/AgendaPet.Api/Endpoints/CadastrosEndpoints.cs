using AgendaPet.Api.Data;
using AgendaPet.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AgendaPet.Api.Endpoints;

public record ClienteDto(string Nome, string Telefone, string? Email, string? Endereco, bool ConsentimentoLgpd);
public record AnimalDto(int ClienteId, string Nome, string Especie, string? Raca, string? Porte,
    DateOnly? DataNascimento, string? Observacoes);
public record ProfissionalDto(string Nome, string Funcao, string Servicos, bool Ativo);
public record ServicoDto(int DuracaoPadraoMinutos);
public record BaiaDto(string Nome, bool Ativa);

public static class CadastrosEndpoints
{
    public static void MapCadastros(this WebApplication app)
    {
        // ---------- Clientes (tutores) ----------
        var cli = app.MapGroup("/api/clientes");

        cli.MapGet("/", async (AppDbContext db, string? busca) =>
        {
            var q = db.Clientes.AsQueryable();
            if (!string.IsNullOrWhiteSpace(busca))
            {
                var b = busca.Trim().ToLower();
                // busca também pelo nome do animal
                var donos = db.Animais.Where(a => a.Nome.ToLower().Contains(b)).Select(a => a.ClienteId);
                q = q.Where(c => c.Nome.ToLower().Contains(b) || c.Telefone.Contains(b) || donos.Contains(c.Id));
            }
            var clientes = await q.OrderBy(c => c.Nome).Take(100).ToListAsync();
            var ids = clientes.Select(c => c.Id).ToList();
            var animais = await db.Animais.Where(a => ids.Contains(a.ClienteId) && a.Ativo).ToListAsync();
            return clientes.Select(c => new
            {
                c.Id, c.Nome, c.Telefone, c.Email, c.Endereco, c.ConsentimentoLgpd,
                Animais = animais.Where(a => a.ClienteId == c.Id).OrderBy(a => a.Nome)
                                 .Select(a => new { a.Id, a.Nome, a.Especie, a.Raca, a.Porte, a.Observacoes })
            });
        });

        cli.MapGet("/{id:int}", async (int id, AppDbContext db) =>
        {
            var c = await db.Clientes.FirstOrDefaultAsync(x => x.Id == id);
            if (c is null) return Results.NotFound();
            var animais = await db.Animais.Where(a => a.ClienteId == id && a.Ativo).OrderBy(a => a.Nome).ToListAsync();
            return Results.Ok(new { cliente = c, animais });
        });

        cli.MapPost("/", async (ClienteDto dto, AppDbContext db) =>
        {
            var erro = ValidarCliente(dto);
            if (erro is not null) return Results.BadRequest(new { erros = new[] { erro } });
            var c = new Cliente();
            PreencherCliente(c, dto);
            db.Clientes.Add(c);
            await db.SaveChangesAsync();
            return Results.Created($"/api/clientes/{c.Id}", c);
        });

        cli.MapPut("/{id:int}", async (int id, ClienteDto dto, AppDbContext db) =>
        {
            var c = await db.Clientes.FirstOrDefaultAsync(x => x.Id == id);
            if (c is null) return Results.NotFound();
            var erro = ValidarCliente(dto);
            if (erro is not null) return Results.BadRequest(new { erros = new[] { erro } });
            PreencherCliente(c, dto);
            await db.SaveChangesAsync();
            return Results.Ok(c);
        });

        // ---------- Animais ----------
        var ani = app.MapGroup("/api/animais");

        ani.MapGet("/{id:int}", async (int id, AppDbContext db) =>
        {
            var a = await db.Animais.FirstOrDefaultAsync(x => x.Id == id);
            if (a is null) return Results.NotFound();
            var tutor = await db.Clientes.FirstOrDefaultAsync(c => c.Id == a.ClienteId);
            return Results.Ok(new { animal = a, tutor });
        });

        ani.MapPost("/", async (AnimalDto dto, AppDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(dto.Nome)) return Results.BadRequest(new { erros = new[] { "Informe o nome do animal." } });
            if (!await db.Clientes.AnyAsync(c => c.Id == dto.ClienteId))
                return Results.BadRequest(new { erros = new[] { "Tutor não encontrado." } });
            var a = new Animal();
            PreencherAnimal(a, dto);
            db.Animais.Add(a);
            await db.SaveChangesAsync();
            return Results.Created($"/api/animais/{a.Id}", a);
        });

        ani.MapPut("/{id:int}", async (int id, AnimalDto dto, AppDbContext db) =>
        {
            var a = await db.Animais.FirstOrDefaultAsync(x => x.Id == id);
            if (a is null) return Results.NotFound();
            if (string.IsNullOrWhiteSpace(dto.Nome)) return Results.BadRequest(new { erros = new[] { "Informe o nome do animal." } });
            PreencherAnimal(a, dto);
            await db.SaveChangesAsync();
            return Results.Ok(a);
        });

        // Histórico do animal: todos os atendimentos, de todos os serviços.
        ani.MapGet("/{id:int}/historico", async (int id, AppDbContext db) =>
        {
            var lista = await (
                from ag in db.Agendamentos
                join s in db.Servicos on ag.ServicoId equals s.Id
                join p in db.Profissionais on ag.ProfissionalId equals (int?)p.Id into pj
                from p in pj.DefaultIfEmpty()
                join b in db.Baias on ag.BaiaId equals (int?)b.Id into bj
                from b in bj.DefaultIfEmpty()
                where ag.AnimalId == id
                orderby ag.Inicio descending
                select new
                {
                    ag.Id, ag.Inicio, ag.Fim, ag.Status, ag.Observacoes, ag.ProximoRetorno, ag.DescricaoRetorno,
                    Servico = s.Nome, s.Tipo,
                    Profissional = p != null ? p.Nome : null,
                    Baia = b != null ? b.Nome : null
                }).ToListAsync();
            return Results.Ok(lista);
        });

        // ---------- Profissionais ----------
        var pro = app.MapGroup("/api/profissionais");
        pro.MapGet("/", async (AppDbContext db) => await db.Profissionais.OrderBy(p => p.Nome).ToListAsync());
        pro.MapPost("/", async (ProfissionalDto dto, AppDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(dto.Nome)) return Results.BadRequest(new { erros = new[] { "Informe o nome." } });
            var p = new Profissional { Nome = dto.Nome.Trim(), Funcao = dto.Funcao.Trim(), Servicos = dto.Servicos, Ativo = dto.Ativo };
            db.Profissionais.Add(p);
            await db.SaveChangesAsync();
            return Results.Created($"/api/profissionais/{p.Id}", p);
        });
        pro.MapPut("/{id:int}", async (int id, ProfissionalDto dto, AppDbContext db) =>
        {
            var p = await db.Profissionais.FirstOrDefaultAsync(x => x.Id == id);
            if (p is null) return Results.NotFound();
            p.Nome = dto.Nome.Trim(); p.Funcao = dto.Funcao.Trim(); p.Servicos = dto.Servicos; p.Ativo = dto.Ativo;
            await db.SaveChangesAsync();
            return Results.Ok(p);
        });

        // ---------- Serviços ----------
        app.MapGet("/api/servicos", async (AppDbContext db) => await db.Servicos.OrderBy(s => s.Id).ToListAsync());
        app.MapPut("/api/servicos/{id:int}", async (int id, ServicoDto dto, AppDbContext db) =>
        {
            var s = await db.Servicos.FirstOrDefaultAsync(x => x.Id == id);
            if (s is null) return Results.NotFound();
            if (dto.DuracaoPadraoMinutos <= 0) return Results.BadRequest(new { erros = new[] { "Duração inválida." } });
            s.DuracaoPadraoMinutos = dto.DuracaoPadraoMinutos;
            await db.SaveChangesAsync();
            return Results.Ok(s);
        });

        // ---------- Baias ----------
        app.MapGet("/api/baias", async (AppDbContext db) => await db.Baias.OrderBy(b => b.Nome).ToListAsync());
        app.MapPost("/api/baias", async (BaiaDto dto, AppDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(dto.Nome)) return Results.BadRequest(new { erros = new[] { "Informe o nome da baia." } });
            var b = new Baia { Nome = dto.Nome.Trim(), Ativa = dto.Ativa };
            db.Baias.Add(b);
            await db.SaveChangesAsync();
            return Results.Created($"/api/baias/{b.Id}", b);
        });
        app.MapPut("/api/baias/{id:int}", async (int id, BaiaDto dto, AppDbContext db) =>
        {
            var b = await db.Baias.FirstOrDefaultAsync(x => x.Id == id);
            if (b is null) return Results.NotFound();
            b.Nome = dto.Nome.Trim(); b.Ativa = dto.Ativa;
            await db.SaveChangesAsync();
            return Results.Ok(b);
        });
    }

    private static string? ValidarCliente(ClienteDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Nome)) return "Informe o nome do tutor.";
        if (string.IsNullOrWhiteSpace(dto.Telefone)) return "Informe o telefone do tutor.";
        return null;
    }

    private static void PreencherCliente(Cliente c, ClienteDto dto)
    {
        c.Nome = dto.Nome.Trim();
        c.Telefone = dto.Telefone.Trim();
        c.Email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim();
        c.Endereco = string.IsNullOrWhiteSpace(dto.Endereco) ? null : dto.Endereco.Trim();
        if (dto.ConsentimentoLgpd && !c.ConsentimentoLgpd) c.DataConsentimento = DateTime.Now;
        if (!dto.ConsentimentoLgpd) c.DataConsentimento = null;
        c.ConsentimentoLgpd = dto.ConsentimentoLgpd;
    }

    private static void PreencherAnimal(Animal a, AnimalDto dto)
    {
        a.ClienteId = dto.ClienteId;
        a.Nome = dto.Nome.Trim();
        a.Especie = string.IsNullOrWhiteSpace(dto.Especie) ? "Cão" : dto.Especie.Trim();
        a.Raca = dto.Raca?.Trim();
        a.Porte = dto.Porte?.Trim();
        a.DataNascimento = dto.DataNascimento;
        a.Observacoes = string.IsNullOrWhiteSpace(dto.Observacoes) ? null : dto.Observacoes.Trim();
    }
}
