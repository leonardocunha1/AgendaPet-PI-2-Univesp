using AgendaPet.Api.Data;
using AgendaPet.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace AgendaPet.Api.Endpoints;

/// <summary>
/// Indicadores de uso do sistema no período (usados para avaliar o impacto da solução no petshop).
/// </summary>
public static class IndicadoresEndpoints
{
    public static void MapIndicadores(this WebApplication app)
    {
        app.MapGet("/api/indicadores", async (DateOnly? de, DateOnly? ate, AppDbContext db) =>
        {
            var inicio = (de ?? DateOnly.FromDateTime(DateTime.Today.AddDays(-30))).ToDateTime(TimeOnly.MinValue);
            var fim = (ate ?? DateOnly.FromDateTime(DateTime.Today)).ToDateTime(TimeOnly.MinValue).AddDays(1);

            var ags = await AgendaEndpoints.ConsultaAgenda(db)
                .Where(x => x.Inicio >= inicio && x.Inicio < fim)
                .ToListAsync();
            var conflitos = await db.ConflitosEvitados
                .Where(c => c.Quando >= inicio && c.Quando < fim)
                .OrderByDescending(c => c.Quando)
                .ToListAsync();

            var concluidos = ags.Count(a => a.Status is StatusAgendamento.Realizado or StatusAgendamento.Falta);
            var faltas = ags.Count(a => a.Status == StatusAgendamento.Falta);

            return Results.Ok(new
            {
                Periodo = new { De = DateOnly.FromDateTime(inicio), Ate = DateOnly.FromDateTime(fim.AddDays(-1)) },
                TotalAgendamentos = ags.Count,
                PorServico = ags.GroupBy(a => a.Servico).Select(g => new { Servico = g.Key, Quantidade = g.Count() }),
                PorStatus = ags.GroupBy(a => a.Status).Select(g => new { Status = g.Key, Quantidade = g.Count() }),
                TaxaFaltas = concluidos == 0 ? 0 : Math.Round(100.0 * faltas / concluidos, 1),
                ConfirmadosAntes = ags.Count(a => a.Status is StatusAgendamento.Confirmado),
                ConflitosEvitados = conflitos.Count,
                UltimosConflitos = conflitos.Take(10).Select(c => new { c.Quando, c.Motivo }),
                ClientesCadastrados = await db.Clientes.CountAsync(),
                AnimaisCadastrados = await db.Animais.CountAsync(a => a.Ativo)
            });
        });
    }
}
