using AgendaPet.Api.Models;

namespace AgendaPet.Api.Data;

/// <summary>Cria a estrutura básica na primeira execução: serviços, baias e profissionais de exemplo
/// (os nomes podem ser alterados na tela "Configurações").</summary>
public static class DadosIniciais
{
    public static void Criar(AppDbContext db)
    {
        if (!db.Servicos.Any())
        {
            db.Servicos.Add(new Servico { Tipo = TipoServico.BanhoTosa, Nome = "Banho e tosa", DuracaoPadraoMinutos = 90 });
            db.Servicos.Add(new Servico { Tipo = TipoServico.Consulta, Nome = "Consulta veterinária", DuracaoPadraoMinutos = 30 });
            db.Servicos.Add(new Servico { Tipo = TipoServico.Vacina, Nome = "Vacinação", DuracaoPadraoMinutos = 15 });
            db.Servicos.Add(new Servico { Tipo = TipoServico.Hospedagem, Nome = "Hospedagem", DuracaoPadraoMinutos = 1440 });
        }
        if (!db.Baias.Any())
        {
            for (var i = 1; i <= 3; i++) db.Baias.Add(new Baia { Nome = $"Baia {i}" });
        }
        if (!db.Profissionais.Any())
        {
            db.Profissionais.Add(new Profissional { Nome = "Tosador(a) 1", Funcao = "Banho e tosa", Servicos = "BanhoTosa" });
            db.Profissionais.Add(new Profissional { Nome = "Veterinária", Funcao = "Médica veterinária", Servicos = "Consulta,Vacina" });
        }
        db.SaveChanges();
    }
}
