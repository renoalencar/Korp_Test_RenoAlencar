using Microsoft.EntityFrameworkCore;
using AutoMapper;
using FluentValidation;
using Serilog;
using EstoqueService.Infrastructure.Data;
using EstoqueService.Infrastructure.Repositories;
using EstoqueService.Application.Services;
using EstoqueService.Application.Interfaces;
using EstoqueService.Application.Mappings;
using EstoqueService.Domain.Interfaces;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File(
        path: "logs/estoque-.txt",
        rollingInterval: RollingInterval.Day,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "EstoqueService")
    .CreateLogger();

try
{
    Log.Information("Iniciando EstoqueService API");

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog();

    builder.Services.AddDbContext<EstoqueDbContext>(options =>
    {
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
        options.UseNpgsql(connectionString, npgsqlOptions =>
        {
            npgsqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorCodesToAdd: null);

            npgsqlOptions.CommandTimeout(30);
        });

        if (builder.Environment.IsDevelopment())
        {
            options.EnableSensitiveDataLogging();
            options.EnableDetailedErrors();
        }
    });

    builder.Services.AddAutoMapper(cfg =>
    {
        cfg.AddMaps(typeof(ProdutoProfile).Assembly);
    });

    builder.Services.AddValidatorsFromAssemblyContaining<ProdutoProfile>();

    builder.Services.AddScoped<IProdutoRepository, ProdutoRepository>();
    builder.Services.AddScoped<IOperacaoRepository, OperacaoRepository>();

    builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

    builder.Services.AddScoped<IProdutoService, ProdutoService>();

    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy = 
                System.Text.Json.JsonNamingPolicy.CamelCase;
            options.JsonSerializerOptions.WriteIndented = true;
        });

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAngular", policy =>
        {
            policy.WithOrigins("http://localhost:4200")
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        });
    });

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
        {
            Title = "Estoque Service API",
            Version = "v1",
            Description = "API para gerenciamento de estoque de produtos",
            Contact = new Microsoft.OpenApi.Models.OpenApiContact
            {
                Name = "Equipe de Desenvolvimento",
                Email = "dev@empresa.com"
            }
        });

        var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        if (File.Exists(xmlPath))
        {
            options.IncludeXmlComments(xmlPath);
        }
    });

    builder.Services.AddHealthChecks()
        .AddDbContextCheck<EstoqueDbContext>("database");

    builder.Services.AddHttpClient("FaturamentoService", client =>
    {
        client.BaseAddress = new Uri(
            builder.Configuration["FaturamentoServiceUrl"] ?? "http://localhost:5002");
        client.Timeout = TimeSpan.FromSeconds(30);
    });

    var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "Estoque API v1");
            options.RoutePrefix = string.Empty;
        });
        app.UseDeveloperExceptionPage();
    }

    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
    });

    app.UseExceptionHandler(errorApp =>
    {
        errorApp.Run(async context =>
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";

            var exceptionHandlerFeature = context.Features
                .Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();

            if (exceptionHandlerFeature != null)
            {
                Log.Error(exceptionHandlerFeature.Error, "Exceção não tratada");

                await context.Response.WriteAsJsonAsync(new
                {
                    mensagem = "Erro interno do servidor",
                    detalhes = app.Environment.IsDevelopment() 
                        ? exceptionHandlerFeature.Error.Message 
                        : null
                });
            }
        });
    });

    app.UseHttpsRedirection();
    app.UseCors("AllowAngular");
    app.UseAuthorization();

    app.MapControllers();
    app.MapHealthChecks("/health");

    if (app.Environment.IsDevelopment())
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EstoqueDbContext>();
        
        Log.Information("Aplicando migrations...");
        await db.Database.MigrateAsync();
        Log.Information("Migrations aplicadas com sucesso");
    }

    var port = builder.Configuration["Port"] ?? "5001";
    app.Urls.Add($"http://localhost:{port}");

    Log.Information("EstoqueService API iniciada na porta {Port}", port);
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Aplicação falhou ao iniciar");
    return 1;
}
finally
{
    Log.CloseAndFlush();
}

return 0;