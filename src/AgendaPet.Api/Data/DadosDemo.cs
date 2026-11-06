using AgendaPet.Api.Models;

namespace AgendaPet.Api.Data;

/// <summary>
/// Dados FICTÍCIOS para demonstração (gravação do vídeo, apresentações).
/// Só são criados quando o sistema roda em modo demonstração (iniciar-demo.bat),
/// em um banco separado (agendapet-demo.db). Nunca tocam no banco real.
/// </summary>
public static class DadosDemo
{
    public static void Criar(AppDbContext db)
    {
        if (db.Clientes.Any()) return;

        var hoje = DateTime.Today;
        var servicos = db.Servicos.ToList();
        int Svc(TipoServico t) => servicos.First(s => s.Tipo == t).Id;

        // Profissionais da demonstração
        var profs = db.Profissionais.ToList();
        var tosa = profs.First(p => p.Servicos.Contains("BanhoTosa"));
        var vet = profs.First(p => p.Servicos.Contains("Consulta"));
        tosa.Nome = "Banho e tosa (apoio)"; tosa.Funcao = "Banho e tosa";
        vet.Nome = "Veterinária"; vet.Funcao = "Médica veterinária";
        db.SaveChanges();
        var baias = db.Baias.OrderBy(b => b.Id).ToList();

        // Tutores e animais fictícios
        var dados = new (string Tutor, string Fone, (string Nome, string Especie, string Raca, string Porte, string? Alerta)[] Animais)[]
        {
            ("Marina Alves", "(16) 90000-0001", new[] { ("Thor", "Cão", "Golden Retriever", "Grande", "Precisa de focinheira no banho"), ("Mel", "Cão", "Shih-tzu", "Pequeno", (string?)null) }),
            ("Carlos Pereira", "(16) 90000-0002", new[] { ("Bob", "Cão", "Poodle", "Médio", "Alergia a xampu com fragrância") }),
            ("Juliana Rocha", "(16) 90000-0003", new[] { ("Luna", "Gato", "SRD", "Pequeno", "Arisca: manusear com toalha") }),
            ("Paulo Mendes", "(16) 90000-0004", new[] { ("Max", "Cão", "Labrador", "Grande", (string?)null), ("Nina", "Cão", "SRD", "Médio", (string?)null) }),
            ("Renata Lima", "(16) 90000-0005", new[] { ("Pipoca", "Cão", "Lhasa Apso", "Pequeno", (string?)null) }),
            ("Fernando Dias", "(16) 90000-0006", new[] { ("Toby", "Cão", "Beagle", "Médio", "Medo de secador") }),
            ("Beatriz Santos", "(16) 90000-0007", new[] { ("Lola", "Cão", "Spitz Alemão", "Pequeno", (string?)null) }),
        };

        var animais = new Dictionary<string, Animal>();
        foreach (var (tutor, fone, lista) in dados)
        {
            var c = new Cliente { Nome = tutor, Telefone = fone, ConsentimentoLgpd = true, DataConsentimento = DateTime.Now };
            db.Clientes.Add(c);
            db.SaveChanges();
            foreach (var a in lista)
            {
                var an = new Animal { ClienteId = c.Id, Nome = a.Nome, Especie = a.Especie, Raca = a.Raca, Porte = a.Porte, Observacoes = a.Alerta };
                db.Animais.Add(an);
                db.SaveChanges();
                animais[a.Nome] = an;
            }
        }

        void Ag(string animal, TipoServico tipo, int? prof, int? baia, DateTime ini, DateTime fim,
                StatusAgendamento st = StatusAgendamento.Agendado, DateOnly? retorno = null, string? descRetorno = null)
        {
            db.Agendamentos.Add(new Agendamento
            {
                AnimalId = animais[animal].Id, ServicoId = Svc(tipo), ProfissionalId = prof, BaiaId = baia,
                Inicio = ini, Fim = fim, Status = st, ProximoRetorno = retorno, DescricaoRetorno = descRetorno
            });
        }
        DateTime H(int dias, int h, int m = 0) => hoje.AddDays(dias).AddHours(h).AddMinutes(m);

        // Histórico (dias anteriores)
        Ag("Mel", TipoServico.Vacina, vet.Id, null, H(-20, 10), H(-20, 10, 15), StatusAgendamento.Realizado,
           DateOnly.FromDateTime(hoje.AddDays(3)), "2ª dose V10");
        Ag("Bob", TipoServico.Consulta, vet.Id, null, H(-10, 14), H(-10, 14, 30), StatusAgendamento.Realizado,
           DateOnly.FromDateTime(hoje.AddDays(-1)), "Vermífugo");
        Ag("Thor", TipoServico.BanhoTosa, tosa.Id, null, H(-7, 9), H(-7, 10, 30), StatusAgendamento.Realizado);
        Ag("Pipoca", TipoServico.BanhoTosa, tosa.Id, null, H(-7, 14), H(-7, 15, 30), StatusAgendamento.Falta);

        // Hoje
        Ag("Thor", TipoServico.BanhoTosa, tosa.Id, null, H(0, 8), H(0, 9, 30), StatusAgendamento.Confirmado);
        Ag("Mel", TipoServico.BanhoTosa, tosa.Id, null, H(0, 10), H(0, 11, 30));
        Ag("Lola", TipoServico.BanhoTosa, tosa.Id, null, H(0, 14), H(0, 15, 30));
        Ag("Luna", TipoServico.Consulta, vet.Id, null, H(0, 9), H(0, 9, 30), StatusAgendamento.Confirmado);
        Ag("Max", TipoServico.Vacina, vet.Id, null, H(0, 11), H(0, 11, 15));
        Ag("Nina", TipoServico.Consulta, vet.Id, null, H(0, 15), H(0, 15, 30));

        // Hospedagem
        Ag("Toby", TipoServico.Hospedagem, null, baias[0].Id, H(-1, 9), H(2, 17), StatusAgendamento.Confirmado);
        if (baias.Count > 1)
            Ag("Bob", TipoServico.Hospedagem, null, baias[1].Id, H(0, 8), H(1, 17));

        // Amanhã
        Ag("Pipoca", TipoServico.BanhoTosa, tosa.Id, null, H(1, 9), H(1, 10, 30));
        Ag("Max", TipoServico.Consulta, vet.Id, null, H(1, 10), H(1, 10, 30));

        db.SaveChanges();
    }
}
