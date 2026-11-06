using AgendaPet.Api.Domain;
using AgendaPet.Api.Models;
using Xunit;

namespace AgendaPet.Tests;

public class RegrasAgendamentoTests
{
    private static readonly DateTime Dia = new(2026, 11, 9);

    private static Ocupacao Banho(int id, int animal, int prof, string ini, string fim,
        StatusAgendamento status = StatusAgendamento.Agendado) =>
        new(id, animal, TipoServico.BanhoTosa, prof, null, Dia.Add(TimeSpan.Parse(ini)), Dia.Add(TimeSpan.Parse(fim)), status, "Thor");

    private static Ocupacao Hosp(int id, int animal, int baia, int diaIni, int diaFim) =>
        new(id, animal, TipoServico.Hospedagem, null, baia, Dia.AddDays(diaIni).AddHours(9), Dia.AddDays(diaFim).AddHours(9),
            StatusAgendamento.Agendado, "Toby");

    [Fact]
    public void Mesmo_profissional_em_horario_sobreposto_gera_conflito()
    {
        var existente = Banho(1, animal: 10, prof: 1, "08:00", "09:30");
        var novo = Banho(0, animal: 20, prof: 1, "08:30", "10:00");
        Assert.Single(RegrasAgendamento.Conflitos(novo, new[] { existente }));
    }

    [Fact]
    public void Horario_encostado_nao_gera_conflito()
    {
        var existente = Banho(1, 10, 1, "08:00", "09:30");
        var novo = Banho(0, 20, 1, "09:30", "11:00");
        Assert.Empty(RegrasAgendamento.Conflitos(novo, new[] { existente }));
    }

    [Fact]
    public void Profissionais_diferentes_no_mesmo_horario_nao_geram_conflito()
    {
        var existente = Banho(1, 10, 1, "08:00", "09:30");
        var novo = Banho(0, 20, 2, "08:00", "09:30");
        Assert.Empty(RegrasAgendamento.Conflitos(novo, new[] { existente }));
    }

    [Fact]
    public void Mesmo_animal_em_dois_servicos_ao_mesmo_tempo_gera_conflito()
    {
        var banho = Banho(1, animal: 10, prof: 1, "08:00", "09:30");
        var consulta = new Ocupacao(0, 10, TipoServico.Consulta, 3, null,
            Dia.AddHours(9), Dia.AddHours(9.5), StatusAgendamento.Agendado, "");
        Assert.Single(RegrasAgendamento.Conflitos(consulta, new[] { banho }));
    }

    [Fact]
    public void Agendamento_cancelado_ou_falta_libera_o_horario()
    {
        var cancelado = Banho(1, 10, 1, "08:00", "09:30", StatusAgendamento.Cancelado);
        var falta = Banho(2, 11, 1, "08:00", "09:30", StatusAgendamento.Falta);
        var novo = Banho(0, 20, 1, "08:00", "09:30");
        Assert.Empty(RegrasAgendamento.Conflitos(novo, new[] { cancelado, falta }));
    }

    [Fact]
    public void Edicao_nao_conflita_com_o_proprio_agendamento()
    {
        var existente = Banho(5, 10, 1, "08:00", "09:30");
        var editado = Banho(5, 10, 1, "08:30", "10:00");
        Assert.Empty(RegrasAgendamento.Conflitos(editado, new[] { existente }));
    }

    [Fact]
    public void Mesma_baia_em_periodos_sobrepostos_gera_conflito()
    {
        var existente = Hosp(1, 10, baia: 1, 0, 3);
        var novo = Hosp(0, 20, baia: 1, 2, 5);
        Assert.Single(RegrasAgendamento.Conflitos(novo, new[] { existente }));
    }

    [Fact]
    public void Animal_hospedado_pode_tomar_banho()
    {
        var hospedagem = Hosp(1, 10, 1, 0, 3);
        var banho = Banho(0, 10, 1, "10:00", "11:30");
        Assert.Empty(RegrasAgendamento.Conflitos(banho, new[] { hospedagem }));
    }

    [Fact]
    public void Validacao_exige_profissional_ou_baia()
    {
        var semProf = new Ocupacao(0, 1, TipoServico.Consulta, null, null, Dia, Dia.AddHours(1), StatusAgendamento.Agendado, "");
        var semBaia = new Ocupacao(0, 1, TipoServico.Hospedagem, null, null, Dia, Dia.AddDays(1), StatusAgendamento.Agendado, "");
        Assert.NotEmpty(RegrasAgendamento.Validar(semProf));
        Assert.NotEmpty(RegrasAgendamento.Validar(semBaia));
    }

    [Fact]
    public void Horarios_livres_pulam_o_periodo_ocupado()
    {
        var ocupados = new[] { (Dia.AddHours(8), Dia.AddHours(9.5)) };
        var livres = RegrasAgendamento.HorariosLivres(DateOnly.FromDateTime(Dia), new TimeOnly(8, 0), new TimeOnly(12, 0), 90, ocupados);
        Assert.Equal(new[] { "09:30", "10:00", "10:30" }, livres.Select(h => h.ToString("HH:mm")));
    }
}
