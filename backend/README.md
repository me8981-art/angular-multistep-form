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

## Uploads and tracking

Each successful submission receives a tracking ID such as `TMS-2026-928129`. Users can enter that ID in the form’s tracking panel to retrieve the current status. The API accepts one profile image and up to nine additional PDF/image attachments, with a 10 MB maximum per file. Metadata is stored in PostgreSQL, while local development files are stored under `wwwroot/uploads` and exposed through the API.

For production, replace local file storage with durable object storage and protect the admin dashboard and management endpoints with authentication and authorization before exposing them publicly.
