using FiscoProjects.Api;

var builder = WebApplication.CreateBuilder(args);
builder.AddFiscoProjectsApi();

var app = builder.Build();
app.UseFiscoProjectsApi();

app.Run();
