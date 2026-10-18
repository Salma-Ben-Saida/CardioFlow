using CathLab.Infrastructure.Persistence;
using CathLab.Infrastructure.Pacs;
using CathLab.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using CathLab.Infrastructure.Persistence.Repositories;
using CathLab.Application.Services;
using CathLab.Infrastructure.BackgroundServices;
using CathLab.Domain.Repositories;
using CathLab.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// --- Configuration d'Authentification JWT ---
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = Encoding.UTF8.GetBytes(jwtSettings["Secret"]!);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.MapInboundClaims = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(secretKey),
        ClockSkew = TimeSpan.Zero, // Supprime la tolérance de 5 minutes par défaut
        
        // 👈 CORRECTIF CRUCIAL : Indique à .NET de lire la clé "role" brute du JSON de votre Token
        RoleClaimType = "role" 
    };
});

builder.Services.AddAuthorization();

// --- Configuration des Contextes de Base de Données ---
builder.Services.AddDbContext<CathLabDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("CathLabDb"))
       .ConfigureWarnings(w => w.Ignore(
           Microsoft.EntityFrameworkCore.Diagnostics.CoreEventId
               .PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning)));

builder.Services.AddDbContext<OrthancPacsContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("OrthancPACS")));

// --- Configuration des Contrôleurs & OpenAPI Swagger ---
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Permet de lire et d'écrire les enums sous forme de texte (ex: "ResponsablePharmacie")
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "CathLab API", Version = "v1" });

    // 1. Définir le schéma de sécurité JWT (Bouton de saisie dans l'interface)
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Entrez votre jeton JWT sous la forme suivante : Bearer {votre_token}"
    });

    // 2. Appliquer la sécurité globalement à tous les points de terminaison de Swagger
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// --- Injections de Dépendances (Couches Onion) ---
builder.Services.AddScoped<IPacsReader, OrthancPacsReader>();
builder.Services.AddScoped<IPatientRepository, PatientRepository>();
builder.Services.AddScoped<ICathStudyRepository, CathStudyRepository>();
builder.Services.AddScoped<IArchiveRepository, ArchiveRepository>();
builder.Services.AddScoped<IAuditRepository, AuditRepository>();

// Services Web Scoped (Résolus via IServiceProvider à l'intérieur du Background Worker)
builder.Services.AddScoped<StudyIngestionService>(); 
builder.Services.AddScoped<PatientService>();
builder.Services.AddScoped<PatientLifecycleService>();
builder.Services.AddScoped<AuditService>();

// Répertoires (Repositories) - Pharmacie
builder.Services.AddScoped<IInventoryProductRepository, InventoryProductRepository>();
builder.Services.AddScoped<IInventoryItemRepository, InventoryItemRepository>();
builder.Services.AddScoped<IStockMovementRepository, StockMovementRepository>();

// Répertoires (Repositories) - Procédure / Lésion / Traitement
builder.Services.AddScoped<IProcedureReportRepository, ProcedureReportRepository>();
builder.Services.AddScoped<ICoronaryLesionRepository, CoronaryLesionRepository>();
builder.Services.AddScoped<ILesionTreatmentRepository, LesionTreatmentRepository>();

// Unit of work (Garantit la cohérence transactionnelle entre les services)
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Services applicatifs
builder.Services.AddScoped<InventoryService>();
builder.Services.AddScoped<ProcedureService>();

// Worker d'arrière-plan Singleton
builder.Services.AddHostedService<PacsIngestionWorker>();

// Services et Répertoires d'utilisateurs
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<UserService>();

builder.Services.AddScoped<IPdfGenerator>(_ =>
    new QuestPdfProcedureReportGenerator(Path.Combine(builder.Environment.ContentRootPath, "reports")));
// --- Politiques de sécurité Cross-Origin (CORS) ---
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
                || host.EndsWith(".webcontainer.io")  // Bolt.new WebContainers
                || host.EndsWith(":5501")            // Serveur Vite local pour le visualiseur DICOM
                || host.EndsWith(":5500");
        })
        .AllowAnyHeader() 
        .AllowAnyMethod());
});

builder.Services.AddHttpClient("orthanc");


var app = builder.Build();

// --- Middlewares du Pipeline HTTP (L'ordre est d'importance absolue !) ---

if (app.Environment.IsDevelopment())
{
    app.UseSwagger(); 
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "CathLab API v1");
    }); 
}

app.UseCors("DevFrontend");

app.UseHttpsRedirection(); 

// L'ordre ici est déjà correct dans votre code d'origine :
app.UseAuthentication(); // 1. Détecte et valide le Token
app.UseAuthorization();  // 2. Vérifie les permissions d'accès au contrôleur

app.MapControllers();

app.Run();
