using OlapOverHttp.Host.ClickHouse;
using OlapOverHttp.Host.Data;
using OlapOverHttp.Host.Endpoints;
using OlapOverHttp.Host.Excel;
using OlapOverHttp.Host.Minio;
using OlapOverHttp.Host.Postgres;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddClickHouse(builder.Configuration);
builder.Services.AddPostgres(builder.Configuration);
builder.Services.AddMinio(builder.Configuration);
builder.Services.AddSingleton<IPostingRepository, PostingRepository>();
builder.Services.AddSingleton<PostingExcelBuilder>();
builder.Services.AddSingleton<ObjectStorage>();
builder.Services.AddSingleton<CachedPostingReportGenerator>();

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
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
