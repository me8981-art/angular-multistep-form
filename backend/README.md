# Project Inquiry API

This ASP.NET Core 8 minimal API stores project inquiry submissions in PostgreSQL through Entity Framework Core and Npgsql.

## Local development

Start PostgreSQL and the API from the repository root:

```bash
docker compose up --build
```

The API is available at `http://localhost:5080`. The health endpoint is `GET /health`, and submissions are saved through `POST /api/submissions`.

The Angular app currently points to `http://localhost:5080`. Update `src/app/submission.service.ts` with the deployed API URL before publishing the frontend for production use.

## Production deployment

Set the `ConnectionStrings__Postgres` environment variable to a managed PostgreSQL connection string. Do not commit production credentials. The API host must allow requests from `https://me8981-art.github.io` through the configured CORS policy.
