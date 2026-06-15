using CathLab.Infrastructure.Persistence;
using CathLab.Infrastructure.Pacs;
using CathLab.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using CathLab.Infrastructure.Persistence.Repositories;
using CathLab.Application.Services;
using CathLab.Infrastructure.BackgroundServices;

var builder = WebApplication.CreateBuilder(args);

// --- 1. Database Context Configurations ---
builder.Services.AddDbContext<CathLabDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("CathLabDb")));

builder.Services.AddDbContext<OrthancPacsContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("OrthancPACS")));

// --- 2. Controller & OpenAPI Explorer Configurations ---
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "CathLab Management API",
        Version = "v1",
        Description = "Backend API for Patient Dashboards and Cath Lab Studies"
    });

    var xmlFilename = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFilename);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

// --- 3. Dependency Injections (Onion Layers) ---
builder.Services.AddScoped<IPacsReader, OrthancPacsReader>();
builder.Services.AddScoped<IPatientRepository, PatientRepository>();
builder.Services.AddScoped<ICathStudyRepository, CathStudyRepository>();

// Scoped Service (Must be resolved via IServiceProvider scope inside the Background Worker)
builder.Services.AddScoped<StudyIngestionService>(); 
builder.Services.AddScoped<PatientService>();

// Singleton Background worker
builder.Services.AddHostedService<PacsIngestionWorker>();

// --- 4. Cross-Origin Resource Sharing (CORS) Security Policies ---
builder.Services.AddCors(options =>
{
    options.AddPolicy("DevFrontend", policy =>
        policy.SetIsOriginAllowed(origin =>
        {
            if (string.IsNullOrEmpty(origin)) return false;
            var host = new Uri(origin).Host;
            
            return host == "localhost"
                || host == "127.0.0.1"
                || host.EndsWith(".stackblitz.io")    // Bolt.new Sandbox
                || host.EndsWith(".webcontainer.io");  // Bolt.new WebContainers
                
        })
        .AllowAnyHeader() 
        .AllowAnyMethod());
});

var app = builder.Build();

// --- 5. HTTP Pipeline Middlewares (Order Matters!) ---

// Ensure Swagger is generated and visible when developing locally
if (app.Environment.IsDevelopment())
{
    app.UseSwagger(); // <--- ADDED: Generates the swagger.json endpoint
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "CathLab API v1");
    }); // <--- ADDED: Generates the actual interactive web dashboard panels
}

app.UseCors("DevFrontend");

app.UseHttpsRedirection(); // Standard routing fallback
app.UseAuthorization();   // Standard routing fallback

app.MapControllers();

app.Run();
