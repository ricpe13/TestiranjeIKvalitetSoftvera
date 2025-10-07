using System.Text.Json.Serialization;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MuseumTickets.Api.Data;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        o.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
var dataDir = Path.Combine(builder.Environment.ContentRootPath, "App_Data");
Directory.CreateDirectory(dataDir);
builder.Services.AddDbContext<AppDbContext>(opts =>
{
    opts.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"));
});

var app = builder.Build();
var useMigrations = builder.Configuration.GetValue<bool>("UseMigrations", false);
var runSqlScript = builder.Configuration.GetValue<bool>("RunSqlScriptOnStartup", true);
var scriptPathConf = builder.Configuration.GetValue<string>("SqlScriptPath");
string? scriptAbsolutePath = null;
if (!string.IsNullOrWhiteSpace(scriptPathConf))
{
    scriptAbsolutePath = Path.IsPathRooted(scriptPathConf)
        ? scriptPathConf
        : Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, scriptPathConf));
}

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    if (useMigrations)
    {
        Console.WriteLine("[API] UseMigrations=true → EF Migrate()");
        db.Database.Migrate();
    }
    else if (runSqlScript)
    {
        var configuredConnStr = builder.Configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Nedostaje ConnectionStrings:DefaultConnection");

        var csb = new SqliteConnectionStringBuilder(configuredConnStr);
        if (!Path.IsPathRooted(csb.DataSource))
            csb.DataSource = Path.Combine(builder.Environment.ContentRootPath, csb.DataSource);

        Console.WriteLine($"[API] UseMigrations=false, RunSqlScriptOnStartup=true");
        Console.WriteLine($"[API] ContentRootPath: {builder.Environment.ContentRootPath}");
        Console.WriteLine($"[API] DB file: {csb.DataSource}");
        Console.WriteLine($"[API] SQL script path (resolved): {scriptAbsolutePath}");

        try
        {
            if (string.IsNullOrWhiteSpace(scriptAbsolutePath) || !File.Exists(scriptAbsolutePath))
                throw new FileNotFoundException($"SQL skripta nije pronađena: {scriptAbsolutePath}");

            using var conn = new SqliteConnection(csb.ConnectionString);
            conn.Open();

            var sql = File.ReadAllText(scriptAbsolutePath);
            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            cmd.ExecuteNonQuery();

            Console.WriteLine("[API] SQL skripta uspešno izvršena.");
        }
        catch (Exception ex)
        {
            var logPath = Path.Combine(builder.Environment.ContentRootPath, "App_Data", "startup-error.log");
            File.WriteAllText(logPath, $"[API] GREŠKA pri izvršavanju SQL skripte:\n{ex}");
            Console.WriteLine($"[API] GREŠKA pri izvršavanju SQL skripte! Detalji: {logPath}");
        }
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "MuseumTickets API v1");
        c.RoutePrefix = string.Empty;
    });
}

app.MapControllers();
app.Run();
