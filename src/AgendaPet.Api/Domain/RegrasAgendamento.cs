using AgendaPet.Api.Models;

namespace AgendaPet.Api.Domain;

/// <summary>Dados mínimos de um agendamento usados na verificação de conflito.</summary>
public record Ocupacao(
    int Id,
    int AnimalId,
    TipoServico Tipo,
    int? ProfissionalId,
    int? BaiaId,
    DateTime Inicio,
    DateTime Fim,
    StatusAgendamento Status,
    string Descricao);

/// <summary>
/// Regra central do sistema: impedir agendamentos sobrepostos.
/// É uma classe sem acesso a banco para poder ser testada isoladamente (xUnit).
/// </summary>
public static class RegrasAgendamento
{
    /// <summary>Dois intervalos [inicio, fim) se cruzam?</summary>
    public static bool Sobrepoe(DateTime inicioA, DateTime fimA, DateTime inicioB, DateTime fimB) =>
        inicioA < fimB && inicioB < fimA;

    /// <summary>Agendamentos cancelados ou com falta não ocupam mais o horário.</summary>
    public static bool OcupaHorario(StatusAgendamento status) =>
        status is not (StatusAgendamento.Cancelado or StatusAgendamento.Falta);

    /// <summary>Validações básicas do pedido. Retorna a lista de erros (vazia = válido).</summary>
    public static List<string> Validar(Ocupacao novo)
    {
        var erros = new List<string>();
        if (novo.Fim <= novo.Inicio)
            erros.Add("O horário de término deve ser depois do horário de início.");
        if (novo.Tipo == TipoServico.Hospedagem)
        {
            if (novo.BaiaId is null) erros.Add("Escolha a baia da hospedagem.");
        }
        else if (novo.ProfissionalId is null)
        {
            erros.Add("Escolha o profissional que fará o atendimento.");
        }
        return erros;
    }

    /// <summary>
    /// Verifica o novo agendamento contra os já existentes e devolve, em texto, cada conflito encontrado.
    /// Regras:
    /// 1. O mesmo profissional não pode ter dois atendimentos ao mesmo tempo.
    /// 2. A mesma baia não pode receber duas hospedagens no mesmo período.
    /// 3. O mesmo animal não pode ter dois atendimentos ao mesmo tempo
    ///    (a hospedagem não conta: o animal hospedado pode tomar banho ou ser consultado).
    /// </summary>
    public static List<string> Conflitos(Ocupacao novo, IEnumerable<Ocupacao> existentes)
    {
        var conflitos = new List<string>();
        foreach (var e in existentes)
        {
            if (e.Id == novo.Id && novo.Id != 0) continue; // o próprio agendamento, em uma edição
            if (!OcupaHorario(e.Status)) continue;
            if (!Sobrepoe(novo.Inicio, novo.Fim, e.Inicio, e.Fim)) continue;

            var periodo = FormatarPeriodo(e);

            if (novo.Tipo != TipoServico.Hospedagem && e.Tipo != TipoServico.Hospedagem
                && novo.ProfissionalId is not null && e.ProfissionalId == novo.ProfissionalId)
                conflitos.Add($"O profissional já tem atendimento marcado em {periodo}: {e.Descricao}.");
            else if (novo.Tipo == TipoServico.Hospedagem && e.Tipo == TipoServico.Hospedagem
                && novo.BaiaId is not null && e.BaiaId == novo.BaiaId)
                conflitos.Add($"A baia já está ocupada em {periodo}: {e.Descricao}.");
            else if (novo.Tipo != TipoServico.Hospedagem && e.Tipo != TipoServico.Hospedagem
                && e.AnimalId == novo.AnimalId)
                conflitos.Add($"Este animal já tem outro atendimento em {periodo}: {e.Descricao}.");
            else if (novo.Tipo == TipoServico.Hospedagem && e.Tipo == TipoServico.Hospedagem
                && e.AnimalId == novo.AnimalId)
                conflitos.Add($"Este animal já está hospedado em {periodo}.");
        }
        return conflitos;
    }

    /// <summary>
    /// Lista os horários livres de um profissional em um dia, de acordo com a duração do serviço.
    /// </summary>
    public static List<DateTime> HorariosLivres(
        DateOnly dia, TimeOnly abertura, TimeOnly fechamento, int duracaoMinutos,
        IEnumerable<(DateTime Inicio, DateTime Fim)> ocupados, int passoMinutos = 30)
    {
        var livres = new List<DateTime>();
        var lista = ocupados.ToList();
        var inicio = dia.ToDateTime(abertura);
        var limite = dia.ToDateTime(fechamento);
        for (var t = inicio; t.AddMinutes(duracaoMinutos) <= limite; t = t.AddMinutes(passoMinutos))
        {
            var fim = t.AddMinutes(duracaoMinutos);
            if (!lista.Any(o => Sobrepoe(t, fim, o.Inicio, o.Fim)))
                livres.Add(t);
        }
        return livres;
    }

    private static string FormatarPeriodo(Ocupacao e) =>
        e.Inicio.Date == e.Fim.Date || (e.Fim - e.Inicio).TotalHours < 24
            ? $"{e.Inicio:dd/MM} das {e.Inicio:HH:mm} às {e.Fim:HH:mm}"
            : $"{e.Inicio:dd/MM HH:mm} a {e.Fim:dd/MM HH:mm}";
}
