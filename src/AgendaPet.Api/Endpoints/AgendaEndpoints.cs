using AgendaPet.Api.Data;
using AgendaPet.Api.Domain;
using AgendaPet.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AgendaPet.Api.Endpoints;

public record AgendamentoDto(int AnimalId, int ServicoId, int? ProfissionalId, int? BaiaId,
    DateTime Inicio, DateTime? Fim, string? Observacoes);
public record StatusDto(StatusAgendamento Status);
public record RetornoDto(DateOnly? ProximoRetorno, string? DescricaoRetorno);

/// <summary>Linha da agenda já com os nomes, pronta para a tela.</summary>
public class ItemAgenda
{
    public int Id { get; set; }
    public DateTime Inicio { get; set; }
    public DateTime Fim { get; set; }
    public StatusAgendamento Status { get; set; }
    public string? Observacoes { get; set; }
    public DateOnly? ProximoRetorno { get; set; }
    public string? DescricaoRetorno { get; set; }
    public int AnimalId { get; set; }
    public string Animal { get; set; } = "";
    public string? Raca { get; set; }
    public string? AlertasAnimal { get; set; }
    public int ClienteId { get; set; }
    public string Tutor { get; set; } = "";
    public string Telefone { get; set; } = "";
    public int ServicoId { get; set; }
    public TipoServico Tipo { get; set; }
    public string Servico { get; set; } = "";
    public int? ProfissionalId { get; set; }
    public string? Profissional { get; set; }
    public int? BaiaId { get; set; }
    public string? Baia { get; set; }
}

public static class AgendaEndpoints
{
    // Horário de funcionamento usado para sugerir horários livres.
    private static readonly TimeOnly Abertura = new(8, 0);
    private static readonly TimeOnly Fechamento = new(18, 0);

    public static IQueryable<ItemAgenda> ConsultaAgenda(AppDbContext db) =>
        from ag in db.Agendamentos
        join a in db.Animais on ag.AnimalId equals a.Id
        join c in db.Clientes on a.ClienteId equals c.Id
        join s in db.Servicos on ag.ServicoId equals s.Id
        join p in db.Profissionais on ag.ProfissionalId equals (int?)p.Id into pj
        from p in pj.DefaultIfEmpty()
        join b in db.Baias on ag.BaiaId equals (int?)b.Id into bj
        from b in bj.DefaultIfEmpty()
        select new ItemAgenda
        {
            Id = ag.Id, Inicio = ag.Inicio, Fim = ag.Fim, Status = ag.Status, Observacoes = ag.Observacoes,
            ProximoRetorno = ag.ProximoRetorno, DescricaoRetorno = ag.DescricaoRetorno,
            AnimalId = a.Id, Animal = a.Nome, Raca = a.Raca, AlertasAnimal = a.Observacoes,
            ClienteId = c.Id, Tutor = c.Nome, Telefone = c.Telefone,
            ServicoId = s.Id, Tipo = s.Tipo, Servico = s.Nome,
            ProfissionalId = ag.ProfissionalId, Profissional = p != null ? p.Nome : null,
            BaiaId = ag.BaiaId, Baia = b != null ? b.Nome : null
        };

    public static void MapAgenda(this WebApplication app)
    {
        // Agenda do dia: os quatro serviços em uma única lista.
        app.MapGet("/api/agenda", async (DateOnly? data, AppDbContext db) =>
        {
            var dia = data ?? DateOnly.FromDateTime(DateTime.Today);
            var ini = dia.ToDateTime(TimeOnly.MinValue);
            var fim = ini.AddDays(1);
            var itens = await ConsultaAgenda(db)
                .Where(x => x.Inicio < fim && x.Fim > ini)
                .OrderBy(x => x.Inicio)
                .ToListAsync();
            return Results.Ok(itens);
        });

        // Ocupação da hospedagem em uma data.
        app.MapGet("/api/hospedagem", async (DateOnly? data, AppDbContext db) =>
        {
            var dia = data ?? DateOnly.FromDateTime(DateTime.Today);
            var ini = dia.ToDateTime(TimeOnly.MinValue);
            var fim = ini.AddDays(1);
            var baias = await db.Baias.Where(b => b.Ativa).OrderBy(b => b.Nome).ToListAsync();
            var ocup = (await ConsultaAgenda(db)
                .Where(x => x.Tipo == TipoServico.Hospedagem && x.Inicio < fim && x.Fim > ini)
                .ToListAsync())
                .Where(x => RegrasAgendamento.OcupaHorario(x.Status))
                .ToList();
            return Results.Ok(baias.Select(b => new
            {
                b.Id, b.Nome,
                Ocupacao = ocup.Where(o => o.BaiaId == b.Id).OrderBy(o => o.Inicio).ToList()
            }));
        });

        app.MapGet("/api/agendamentos/{id:int}", async (int id, AppDbContext db) =>
        {
            var item = await ConsultaAgenda(db).FirstOrDefaultAsync(x => x.Id == id);
            return item is null ? Results.NotFound() : Results.Ok(item);
        });

        // Horários livres do profissional no dia, para a duração do serviço.
        app.MapGet("/api/horarios-livres", async (int profissionalId, int servicoId, DateOnly data, AppDbContext db) =>
        {
            var servico = await db.Servicos.FirstOrDefaultAsync(s => s.Id == servicoId);
            if (servico is null) return Results.NotFound();
            var ini = data.ToDateTime(TimeOnly.MinValue);
            var fim = ini.AddDays(1);
            var ocupados = (await db.Agendamentos
                    .Where(a => a.ProfissionalId == profissionalId && a.Inicio < fim && a.Fim > ini)
                    .ToListAsync())
                .Where(a => RegrasAgendamento.OcupaHorario(a.Status))
                .Select(a => (a.Inicio, a.Fim));
            var livres = RegrasAgendamento.HorariosLivres(data, Abertura, Fechamento,
                servico.DuracaoPadraoMinutos, ocupados);
            if (data == DateOnly.FromDateTime(DateTime.Today))
                livres = livres.Where(h => h > DateTime.Now).ToList();
            return Results.Ok(livres.Select(h => h.ToString("HH:mm")));
        });

        app.MapPost("/api/agendamentos", async (AgendamentoDto dto, AppDbContext db) =>
        {
            var ag = new Agendamento();
            return await Salvar(ag, dto, db, novo: true);
        });

        app.MapPut("/api/agendamentos/{id:int}", async (int id, AgendamentoDto dto, AppDbContext db) =>
        {
            var ag = await db.Agendamentos.FirstOrDefaultAsync(x => x.Id == id);
            if (ag is null) return Results.NotFound();
            return await Salvar(ag, dto, db, novo: false);
        });

        app.MapPatch("/api/agendamentos/{id:int}/status", async (int id, StatusDto dto, AppDbContext db) =>
        {
            var ag = await db.Agendamentos.FirstOrDefaultAsync(x => x.Id == id);
            if (ag is null) return Results.NotFound();
            ag.Status = dto.Status;
            await db.SaveChangesAsync();
            return Results.Ok(new { ag.Id, ag.Status });
        });

        app.MapPatch("/api/agendamentos/{id:int}/retorno", async (int id, RetornoDto dto, AppDbContext db) =>
        {
            var ag = await db.Agendamentos.FirstOrDefaultAsync(x => x.Id == id);
            if (ag is null) return Results.NotFound();
            ag.ProximoRetorno = dto.ProximoRetorno;
            ag.DescricaoRetorno = string.IsNullOrWhiteSpace(dto.DescricaoRetorno) ? null : dto.DescricaoRetorno.Trim();
            await db.SaveChangesAsync();
            return Results.Ok(new { ag.Id, ag.ProximoRetorno, ag.DescricaoRetorno });
        });

        // Retornos (vacina, vermífugo, consulta) previstos para os próximos dias, inclusive os atrasados.
        app.MapGet("/api/retornos", async (int? dias, AppDbContext db) =>
        {
            var hoje = DateOnly.FromDateTime(DateTime.Today);
            var limite = hoje.AddDays(dias ?? 7);
            var lista = await ConsultaAgenda(db)
                .Where(x => x.ProximoRetorno != null && x.ProximoRetorno <= limite)
                .ToListAsync();
            // Se o animal já tem um atendimento do mesmo serviço marcado depois do registro,
            // o retorno é considerado resolvido.
            var futuros = await db.Agendamentos
                .Where(a => a.Inicio >= DateTime.Today && a.Status != StatusAgendamento.Cancelado)
                .Select(a => new { a.Id, a.AnimalId, a.ServicoId, a.Inicio })
                .ToListAsync();
            var pendentes = lista
                .Where(r => !futuros.Any(f => f.AnimalId == r.AnimalId && f.ServicoId == r.ServicoId
                                               && f.Id != r.Id && f.Inicio > r.Fim))
                .OrderBy(r => r.ProximoRetorno)
                .Select(r => new
                {
                    r.Id, r.AnimalId, r.Animal, r.Tutor, r.Telefone, r.Servico, r.Tipo,
                    r.ProximoRetorno, r.DescricaoRetorno,
                    Atrasado = r.ProximoRetorno < hoje
                });
            return Results.Ok(pendentes);
        });
    }

    private static async Task<IResult> Salvar(Agendamento ag, AgendamentoDto dto, AppDbContext db, bool novo)
    {
        var servico = await db.Servicos.FirstOrDefaultAsync(s => s.Id == dto.ServicoId);
        var animal = await db.Animais.FirstOrDefaultAsync(a => a.Id == dto.AnimalId);
        if (servico is null || animal is null)
            return Results.BadRequest(new { erros = new[] { "Animal ou serviço não encontrado." } });

        var hospedagem = servico.Tipo == TipoServico.Hospedagem;
        var fim = dto.Fim ?? dto.Inicio.AddMinutes(servico.DuracaoPadraoMinutos);
        var pedido = new Ocupacao(ag.Id, dto.AnimalId, servico.Tipo,
            hospedagem ? null : dto.ProfissionalId, hospedagem ? dto.BaiaId : null,
            dto.Inicio, fim, StatusAgendamento.Agendado, "");

        var erros = RegrasAgendamento.Validar(pedido);
        if (!hospedagem && dto.ProfissionalId is int pid)
        {
            var prof = await db.Profissionais.FirstOrDefaultAsync(p => p.Id == pid);
            if (prof is null) erros.Add("Profissional não encontrado.");
            else if (!prof.Realiza(servico.Tipo)) erros.Add($"{prof.Nome} não realiza {servico.Nome.ToLower()}.");
        }
        if (erros.Count > 0) return Results.BadRequest(new { erros });

        // Busca só o que se cruza com o período pedido e aplica a regra de conflito.
        var existentes = await ConsultaAgenda(db)
            .Where(x => x.Inicio < pedido.Fim && x.Fim > pedido.Inicio)
            .ToListAsync();
        var conflitos = RegrasAgendamento.Conflitos(pedido, existentes.Select(x => new Ocupacao(
            x.Id, x.AnimalId, x.Tipo, x.ProfissionalId, x.BaiaId, x.Inicio, x.Fim, x.Status,
            $"{x.Animal} ({x.Servico}{(x.Profissional != null ? " com " + x.Profissional : "")}{(x.Baia != null ? " na " + x.Baia : "")})")));

        if (conflitos.Count > 0)
        {
            db.ConflitosEvitados.Add(new ConflitoEvitado { Motivo = string.Join(" | ", conflitos) });
            await db.SaveChangesAsync();
            return Results.Conflict(new { erros = conflitos });
        }

        ag.AnimalId = dto.AnimalId;
        ag.ServicoId = dto.ServicoId;
        ag.ProfissionalId = pedido.ProfissionalId;
        ag.BaiaId = pedido.BaiaId;
        ag.Inicio = pedido.Inicio;
        ag.Fim = pedido.Fim;
        ag.Observacoes = string.IsNullOrWhiteSpace(dto.Observacoes) ? null : dto.Observacoes.Trim();
        if (novo) db.Agendamentos.Add(ag);
        await db.SaveChangesAsync();

        var item = await ConsultaAgenda(db).FirstAsync(x => x.Id == ag.Id);
        return novo ? Results.Created($"/api/agendamentos/{ag.Id}", item) : Results.Ok(item);
    }
}
