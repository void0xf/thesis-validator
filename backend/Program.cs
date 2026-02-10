using System.Reflection;
using backend.Endpoints;
using backend.Models;
using backend.Services;
using Backend.Models;
using ThesisValidator.Rules;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.Configure<UniversityConfig>(
    builder.Configuration.GetSection("UniversityConfig"));

builder.Services.AddHttpClient<LanguageToolService>();
builder.Services.AddScoped<LanguageToolService>();

var assembly = typeof(Program).Assembly;
var ruleTypes = assembly.
    GetTypes().
    Where(t => typeof(IValidationRule).IsAssignableFrom(t)
                                              && !t.IsInterface
                                              && !t.IsAbstract);
foreach (var ruleType in ruleTypes)
{
    builder.Services.AddScoped(typeof(IValidationRule), ruleType);
}

builder.Services.AddScoped<ThesisValidatorService>();
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowFrontend");

app.MapDocumentEndpoint();

app.UseHttpsRedirection();

app.Run();
