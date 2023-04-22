using Microsoft.Data.SqlClient;
using TimeTracker.Shared.Models.Project;

var builder = WebApplication.CreateBuilder(args);

var connection = builder.Configuration.GetConnectionString("DefaultConnection");
// Build Connection String
if (builder.Environment.IsDevelopment()) {
    var conStrBuilder = new SqlConnectionStringBuilder(
    builder.Configuration.GetConnectionString("DefaultConnection"));
    conStrBuilder.UserID = builder.Configuration["DbUser"];
    conStrBuilder.Password = builder.Configuration["DbPassword"];
    connection = conStrBuilder.ConnectionString;
}

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<DataContext>(options => options.UseSqlServer(connection));
builder.Services.AddScoped<ITimeEntryRepository, TimeEntryRepository>();
builder.Services.AddScoped<IProjectRepository, ProjectRepository>();
builder.Services.AddScoped<ITimeEntryService, TimeEntryService>();
builder.Services.AddScoped<IProjectService, ProjectService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

ConfigureMapster();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

void ConfigureMapster()
{
    TypeAdapterConfig<Project, ProjectResponse>.NewConfig()
        .Map(dest => dest.Description, src => src.ProjectDetails != null ? src.ProjectDetails.Description : null)
        .Map(dest => dest.StartDate, src => src.ProjectDetails != null ? src.ProjectDetails.StartDate : null)
        .Map(dest => dest.EndDate, src => src.ProjectDetails != null ? src.ProjectDetails.EndDate : null);
}