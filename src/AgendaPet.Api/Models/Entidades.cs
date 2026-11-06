namespace AgendaPet.Api.Models;

/// <summary>Tutor (dono) do animal.</summary>
public class Cliente
{
    public int Id { get; set; }
    public string Nome { get; set; } = "";
    public string Telefone { get; set; } = "";
    public string? Email { get; set; }
    public string? Endereco { get; set; }
    /// <summary>LGPD: o tutor autorizou o uso dos dados para agendamento e lembretes.</summary>
    public bool ConsentimentoLgpd { get; set; }
    public DateTime? DataConsentimento { get; set; }
    public DateTime CriadoEm { get; set; } = DateTime.Now;
}

/// <summary>O animal é o centro do atendimento: todos os serviços usam o mesmo cadastro.</summary>
public class Animal
{
    public int Id { get; set; }
    public int ClienteId { get; set; }
    public string Nome { get; set; } = "";
    public string Especie { get; set; } = "Cão";
    public string? Raca { get; set; }
    public string? Porte { get; set; }
    public DateOnly? DataNascimento { get; set; }
    /// <summary>Alertas exibidos ao agendar (alergias, comportamento, focinheira...).</summary>
    public string? Observacoes { get; set; }
    public bool Ativo { get; set; } = true;
}

public enum TipoServico
{
    BanhoTosa = 1,
    Consulta = 2,
    Vacina = 3,
    Hospedagem = 4
}

public class Servico
{
    public int Id { get; set; }
    public TipoServico Tipo { get; set; }
    public string Nome { get; set; } = "";
    public int DuracaoPadraoMinutos { get; set; }
}

public class Profissional
{
    public int Id { get; set; }
    public string Nome { get; set; } = "";
    public string Funcao { get; set; } = "";
    /// <summary>Tipos de serviço que realiza, separados por vírgula. Ex.: "BanhoTosa" ou "Consulta,Vacina".</summary>
    public string Servicos { get; set; } = "";
    public bool Ativo { get; set; } = true;

    public bool Realiza(TipoServico tipo) =>
        Servicos.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Contains(tipo.ToString());
}

/// <summary>Espaço de hospedagem.</summary>
public class Baia
{
    public int Id { get; set; }
    public string Nome { get; set; } = "";
    public bool Ativa { get; set; } = true;
}

public enum StatusAgendamento
{
    Agendado = 1,
    Confirmado = 2,
    Realizado = 3,
    Falta = 4,
    Cancelado = 5
}

public class Agendamento
{
    public int Id { get; set; }
    public int AnimalId { get; set; }
    public int ServicoId { get; set; }
    /// <summary>Obrigatório para banho e tosa, consulta e vacina.</summary>
    public int? ProfissionalId { get; set; }
    /// <summary>Obrigatório para hospedagem.</summary>
    public int? BaiaId { get; set; }
    public DateTime Inicio { get; set; }
    public DateTime Fim { get; set; }
    public StatusAgendamento Status { get; set; } = StatusAgendamento.Agendado;
    public string? Observacoes { get; set; }
    /// <summary>Data prevista do próximo retorno (vacina, vermífugo, consulta de retorno).</summary>
    public DateOnly? ProximoRetorno { get; set; }
    public string? DescricaoRetorno { get; set; }
    public DateTime CriadoEm { get; set; } = DateTime.Now;
}

/// <summary>Registro de cada tentativa de agendamento barrada por conflito (indicador do projeto).</summary>
public class ConflitoEvitado
{
    public int Id { get; set; }
    public DateTime Quando { get; set; } = DateTime.Now;
    public string Motivo { get; set; } = "";
}
