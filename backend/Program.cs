using System.Reflection;
using backend.Models;
using backend.Services;
using ThesisValidator.Rules;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

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
    app.MapOpenApi();
}


app.UseHttpsRedirection();

app.Run();
