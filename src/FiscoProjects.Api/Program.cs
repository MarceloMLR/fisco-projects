using FiscoProjects.Api;

var builder = WebApplication.CreateBuilder(args);
builder.AddFiscoProjectsApi();

var app = builder.Build();

// The production deployment applies the migrations with the same services, without starting the server (ADR-0125).
if (MigrateCommand.IsRequested(args))
{
    return await MigrateCommand.RunAsync(app.Services, CancellationToken.None);
}

app.UseFiscoProjectsApi();

app.Run();
return 0;
