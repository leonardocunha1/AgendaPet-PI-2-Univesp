using System.Text.Json.Serialization;
using AgendaPet.Api.Data;
using AgendaPet.Api.Endpoints;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Banco SQLite em arquivo único (agendapet.db), criado na primeira execução.
var conexao = builder.Configuration.GetConnectionString("Padrao") ?? "Data Source=agendapet.db";
// No modo demonstração usa SEMPRE outro arquivo, para nunca misturar com os dados reais.
if (builder.Configuration.GetValue<bool>("Demo"))
    conexao = "Data Source=agendapet-demo.db";
builder.Services.AddDbContext<AppDbContext>(o => o.UseSqlite(conexao));

builder.Services.ConfigureHttpJsonOptions(o =>
{
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

// Durante o desenvolvimento o React (Vite) roda em outra porta.
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.WithOrigins("http://localhost:5173").AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
    DadosIniciais.Criar(db);
    // Modo demonstração (iniciar-demo.bat): banco separado com dados fictícios
    if (app.Configuration.GetValue<bool>("Demo"))
        DadosDemo.Criar(db);
}

app.UseCors();
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapCadastros();
app.MapAgenda();
app.MapIndicadores();

// Qualquer rota que não seja da API devolve o sistema web (React).
app.MapFallbackToFile("index.html");

app.Run();

public partial class Program { }
