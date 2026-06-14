using OlapOverHttp.Host.ClickHouse;
using OlapOverHttp.Host.ClickHouse.Infrastructure;
using OlapOverHttp.Host.Data;
using OlapOverHttp.Host.Endpoints;
using OlapOverHttp.Host.Excel;
using OlapOverHttp.Host.Minio;
using OlapOverHttp.Host.Postgres.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddClickHouse(builder.Configuration);
builder.Services.AddPostgres(builder.Configuration);
builder.Services.AddMinio(builder.Configuration);
builder.Services.AddKeyedSingleton<IPostingRepository, OlapOverHttp.Host.ClickHouse.PostingRepository>("longterm");
builder.Services.AddKeyedSingleton<IPostingRepository, OlapOverHttp.Host.Postgres.PostingRepository>("shortterm");
builder.Services.AddSingleton<PostingRepositoryFactory>();
builder.Services.AddSingleton<PostingExcelBuilder>();
builder.Services.AddSingleton<ObjectStorage>();
builder.Services.AddSingleton<ReportDataProvider>();
builder.Services.AddSingleton<CachedPostingReportGenerator>();

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();
app.MapPostingEndpoints();

app.Run();
