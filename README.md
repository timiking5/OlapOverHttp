# How to use this project
## First start the containers. Migrations will apply automatically
```
docker-compose up -d
```
## Fill up ClickHouse using the following command. This might take a couple minutes.
```
dotnet run -c Release --project OlapOverHttp.Clickhouse.Filler
```
### By default it will generate postings in the period of last year, generating 2 million rows per month. To change this behaviour, you can use command line args:
```
dotnet run -c Release --project OlapOverHttp.Clickhouse.Filler -- --postings-per-month 3000000 --period-start 2024-01-01 --period-end 2025-01-01
```
## To fill the postgres use the following command. This will also take a couple minutes.
```
dotnet run -c Release --project OlapOverHttp.Postgres.Filler
```
### By default it will transition data from ClickHouse for last 2 months. o change this behaviour, you can also use command line args:
```
dotnet run --project OlapOverHttp.Postgres.Filler -- --period-start 2026-04-13 --period-end 2026-06-13
```

## After preparations are complete, you can run the host project
```
dotnet run -c Release --project OlapOverHttp.Host --launch-profile https
```
## Now you are ready to load test. To actually run it specify rps, duration in seconds, from date and test case
### There is a grand total of 4 cases:
+ GetPostings - gets posting from clickhouse
+ GetReports - gets reports from clickhouse
+ GetHotColdPostings - gets postings only from postgres
+ GetCachedReports - gets reports from clickhouse or cache (depending on whether there is the report in cache)
```
dotnet run --project OlapOverHttp.LoadTest -- --requests-per-second 100 --duration-seconds 60 --from 2026-05-01 --case GetHotColdPostings
```

# OR you can just run all of the projects from an IDE of your choice