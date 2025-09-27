# RSS Feed to Database Application

This application fetches RSS feeds and stores them in a PostgreSQL database.

## Problem Description

The error occurs when trying to save data to the `rss_items` table because the `Author` column doesn't exist in the database, even though:
1. The C# model includes an `Author` property
2. The Entity Framework configuration maps this property to the database
3. The SQL script creates the table with an `author` column

## Solution

I've implemented several fixes in the code:

1. **Schema validation**: Added checks to verify the database schema before attempting to save data
2. **Automatic table recreation**: If the table is missing or incomplete, it will be recreated with the correct schema
3. **Enhanced error reporting**: More detailed debugging information when errors occur

## How to Run

1. Ensure PostgreSQL is running
2. Make sure your database credentials are correct in `Program.cs`
3. Run the application using:
   ```bash
   dotnet run
   ```
   
   Or use the provided script:
   ```bash
   ./run.sh
   ```

## Database Setup

The application expects a PostgreSQL database named `rssdb` with a user `postgres` and the default password.

## Troubleshooting

If you continue to see errors:
1. Check that your PostgreSQL server is running
2. Verify database connection details in `Program.cs`
3. Manually verify the table structure using:
   ```sql
   SELECT column_name FROM information_schema.columns WHERE table_name = 'rss_items';
   ```