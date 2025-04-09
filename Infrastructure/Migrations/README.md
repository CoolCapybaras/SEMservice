# Migrations

Эта папка содержит миграции базы данных для проекта SEM.

## Создание миграции

```
dotnet ef migrations add InitialCreate -p Infrastructure -s API
```

## Применение миграций

```
dotnet ef database update -p Infrastructure -s API
``` 