# How to use this project
## First, start the containers. Migrations will apply automatically.
```
docker-compose up -d
```
## Populate ClickHouse using the following command. This might take a couple of minutes.
```
dotnet run -c Release --project OlapOverHttp.Clickhouse.Filler
```
### By default, it generates postings for the last year at 2 million rows per month. To change this behavior, use command-line arguments:
```
dotnet run -c Release --project OlapOverHttp.Clickhouse.Filler -- --postings-per-month 3000000 --period-start 2024-01-01 --period-end 2025-01-01
```
## To populate PostgreSQL, use the following command. This will also take a couple of minutes.
```
dotnet run -c Release --project OlapOverHttp.Postgres.Filler
```
### By default, it transfers data from ClickHouse for the last 2 months. To change this behavior, you can also use command-line arguments:
```
dotnet run --project OlapOverHttp.Postgres.Filler -- --period-start 2026-04-13 --period-end 2026-06-13
```

## After preparations are complete, you can run the host project.
```
dotnet run -c Release --project OlapOverHttp.Host --launch-profile https
```
## You are now ready to run the load test. To run it, specify requests per second, duration in seconds, a start date, and a test case.
### The following 4 test cases are available:
+ **GetPostings** — gets postings from ClickHouse
+ **GetReports** — gets reports from ClickHouse
+ **GetHotColdPostings** — gets postings from PostgreSQL only
+ **GetCachedReports** — gets reports from ClickHouse or the cache (depending on whether the report is in the cache)
```
dotnet run --project OlapOverHttp.LoadTest -- --requests-per-second 100 --duration-seconds 60 --from 2026-05-01 --case GetHotColdPostings
```

Alternatively, you can run all projects from an IDE of your choice.
