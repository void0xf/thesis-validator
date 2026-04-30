using backend.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddPresentation(builder.Configuration, builder.Environment)
    .AddDocumentValidation(builder.Configuration, typeof(Program).Assembly);

var app = builder.Build();

app.UsePresentation();
app.Run();
