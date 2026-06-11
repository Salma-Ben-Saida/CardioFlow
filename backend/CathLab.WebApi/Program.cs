using CathLab.Infrastructure.Persistence;
using CathLab.Infrastructure.Pacs;
using CathLab.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using CathLab.Infrastructure.Persistence.Repositories;
using CathLab.Application.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<CathLabDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("CathLabDb")));

builder.Services.AddDbContext<OrthancPacsContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("OrthancPACS")));

builder.Services.AddScoped<IPacsReader, OrthancPacsReader>();
builder.Services.AddControllers();

builder.Services.AddScoped<IPatientRepository, PatientRepository>();
builder.Services.AddScoped<ICathStudyRepository, CathStudyRepository>();
builder.Services.AddScoped<StudyIngestionService>();

var app = builder.Build();
app.MapControllers();
app.Run();