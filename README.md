# RSS Feed to PostgreSQL Database Application

This is a simple C# console application that fetches RSS feed data from https://alexablockchain.com/feed/ and stores it in a PostgreSQL database using Entity Framework Core.

## Prerequisites

1. .NET 9.0 SDK
2. PostgreSQL database server
3. A database named `rssdb` (or modify the connection string accordingly)

## Setup Instructions

### 1. Database Setup

First, make sure you have PostgreSQL installed and running. Then create a database:

```sql
CREATE DATABASE rssdb;
```

You may also need to create a user with appropriate permissions:
```sql
CREATE USER rssuser WITH PASSWORD 'yourpassword';
GRANT ALL PRIVILEGES ON DATABASE rssdb TO rssuser;
```

### 2. Update Connection String

In `Program.cs`, update the connection string in the `OnConfiguring` method:

```csharp
optionsBuilder.UseNpgsql("Host=localhost;Database=rssdb;Username=postgres;Password=yourpassword");
```

Replace with your actual PostgreSQL credentials.

### 3. Build and Run

Navigate to the project directory and run:

```bash
cd /C/fast_docker_projects/cursor/rss-ef-core-app
dotnet build
dotnet run
```

## Application Features

- Fetches RSS feed from alexablockchain.com
- Parses XML feed data
- Stores items in PostgreSQL database using Entity Framework Core
- Handles basic error cases
- Automatically creates the database schema if it doesn't exist

## Database Schema

The application creates a table `RssItems` with the following columns:
- Id (Primary Key)
- Title
- Description
- Link
- PubDate
- Author
- Category

## Project Structure

```
/rss-ef-core-app/
├── Program.cs          # Main application code
├── RssFeedApp.csproj   # Project file
└── README.md           # This file
```

## Dependencies

This project uses:
- Microsoft.EntityFrameworkCore (9.0.9)
- Npgsql.EntityFrameworkCore.PostgreSQL (9.0.9)
- System.Xml.ReaderWriter (4.3.1)