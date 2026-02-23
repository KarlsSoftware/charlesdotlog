# Setup

## Backend

- Create .NET minimal API: `dotnet new web -n MinimalApiDemo`

- Go to the project folder and run it: `dotnet run`

- Call the API: `curl http://localhost:5192/api/posts`

- Install these 2 packages:
  - `dotnet add package Microsoft.EntityFrameworkCore.Sqlite --version 9.*` — Entity Framework Core's SQLite provider
  - `dotnet add package Microsoft.EntityFrameworkCore.Design --version 9.*` — gives you the `dotnet ef` commands for creating database migrations

- Check if it builds correctly: `dotnet build`

- `dotnet ef migrations add InitialCreate` — EF Core looks at your AppDbContext and Product model, then generates C# code that describes how to create the database tables. It creates a Migrations folder with the schema definition. Think of it like a "blueprint" for your database.

- `dotnet ef database update` — takes the migration blueprint you just created and actually creates the products.db SQLite file with the Products table in it. After this, your database is ready to use.

- `dotnet run` — runs the API

- Install Swagger to interact with the DB:
  `dotnet add package Swashbuckle.AspNetCore --version 6.*`
  Then open Swagger at: http://localhost:5192/swagger- 



### Additional .NET commands
- .env -> dotnet add package dotenv.net
- Delete database: `dotnet ef database drop --force`
- Delete migrations: `Remove-Item -Recurse -Force .\Migrations`
- New migration: `dotnet ef migrations add BlogPostMigration`
- Create database: `dotnet ef database update`

### Auth

- `dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer --version 9.*`

> Full backend guide: [`Backend/DotNetTutorial.md`](Backend/DotNetTutorial.md)

## Frontend

- `ng new blog-frontend`

> Full frontend guide: [`Frontend/AngularTutorial.md`](Frontend/AngularTutorial.md)

---

## .env Setup (Secrets)

Create a `.env` file in the `Backend/` folder with your secrets:

```
JWT_SECRET=a-secret-key-at-least-32-characters-long!
ADMIN_PASSWORD=your-admin-password
```

The `dotenv.net` NuGet package loads these values automatically on startup — no need to set PowerShell environment variables manually.

> **Important:** `.env` is listed in `.gitignore` and is never committed to the repository.
