using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Xml.Linq;

// Define the RSS item model
public class RssItem
{
    public int Id { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Link { get; set; }
    public DateTime PubDate { get; set; }
    public string? Author { get; set; }
    public string? Category { get; set; }
}

// Database context
public class RssDbContext : DbContext
{
    public DbSet<RssItem> RssItems { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Use localhost for Docker environment
        optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=rssdb;Username=postgres;Password=yourpassword");
         Console.WriteLine("Connected");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RssItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.Description).HasMaxLength(5000);
            entity.Property(e => e.Link).IsRequired().HasMaxLength(2000);
            entity.Property(e => e.Author).HasMaxLength(500);
            entity.Property(e => e.Category).HasMaxLength(250);
            // Configure PubDate to use UTC
            entity.Property(e => e.PubDate).HasColumnType("timestamp with time zone");
            
            // Map to the actual database table name
            entity.ToTable("rss_items");
        });
    }
}

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("RSS Feed to Database Application");
        Console.WriteLine("================================");

        try
        {
            // First check database schema
            await CheckDatabaseSchema();
            
            // Fetch and store RSS data
            await FetchAndStoreRssData();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            if (ex.InnerException != null)
                Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
        }

        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }

    static async Task CheckDatabaseSchema()
    {
        Console.WriteLine("Checking database schema...");
        try
        {
            using (var context = new RssDbContext())
            {
                var columns = await context.Database.SqlQueryRaw<string>("SELECT column_name FROM information_schema.columns WHERE table_name = 'rss_items'").ToListAsync();
                Console.WriteLine($"Columns in rss_items table: {string.Join(", ", columns)}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error checking schema: {ex.Message}");
        }
    }

    static async Task FetchAndStoreRssData()
    {
        // Fetch RSS feed from the provided URL
        var rssUrl = "https://alexablockchain.com/feed/";
        Console.WriteLine($"Fetching RSS feed from: {rssUrl}");
        
        var rssFeed = await FetchRssFeed(rssUrl);
        if (string.IsNullOrEmpty(rssFeed))
        {
            Console.WriteLine("Failed to fetch RSS feed.");
            return;
        }

        // Parse the RSS feed
        var rssItems = ParseRssFeed(rssFeed);
        Console.WriteLine($"Found {rssItems.Count} items in the RSS feed.");

        // Store in database
        using (var context = new RssDbContext())
        {
            try
            {
                // Check if table exists and recreate if needed
                await RecreateTableIfNecessary(context);
                
                // Ensure database is created and migrations are applied
                await context.Database.EnsureCreatedAsync();
                
                // Apply any pending migrations
                await context.Database.MigrateAsync();

                // Clear existing data (optional)
                await context.RssItems.ExecuteDeleteAsync();

                // Add new items
                foreach (var item in rssItems)
                {
                    // Convert to UTC before saving
                    item.PubDate = DateTime.SpecifyKind(item.PubDate, DateTimeKind.Utc);
                    context.RssItems.Add(item);
                }

                Console.WriteLine("Try to store");
                await context.SaveChangesAsync();
                Console.WriteLine($"Successfully stored {rssItems.Count} items to database.");
            }
            catch (DbUpdateException dbue)
            {
                Console.WriteLine($"Error: {dbue.Message}");
                if (dbue.InnerException != null)
                    Console.WriteLine($"Inner Exception: {dbue.InnerException.Message}");
                // Log the full exception for debugging
                Console.WriteLine($"Full exception: {dbue}");
                
                // Additional debug information - Check schema
                Console.WriteLine("Database schema check:");
                var columns = await context.Database.SqlQueryRaw<string>("SELECT column_name FROM information_schema.columns WHERE table_name = 'rss_items'").ToListAsync();
                Console.WriteLine($"Columns in rss_items table: {string.Join(", ", columns)}");
                
                // Also try to check if table exists
                var tableExists = await context.Database.SqlQueryRaw<int>("SELECT 1 FROM information_schema.tables WHERE table_name = 'rss_items'").ToListAsync();
                Console.WriteLine($"Table rss_items exists: {tableExists.Any()}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"General error: {ex.Message}");
                if (ex.InnerException != null)
                    Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
            }
        }
    }

    static async Task RecreateTableIfNecessary(RssDbContext context)
    {
        try
        {
            // Check if table exists and has the right columns
            var tableExists = await context.Database.SqlQueryRaw<int>("SELECT 1 FROM information_schema.tables WHERE table_name = 'rss_items'").ToListAsync();
            
            if (tableExists.Any())
            {
                Console.WriteLine("Table rss_items exists. Checking schema...");
                
                // Check if Author column exists
                var authorColumnExists = await context.Database.SqlQueryRaw<int>("SELECT 1 FROM information_schema.columns WHERE table_name = 'rss_items' AND column_name = 'author'").ToListAsync();
                
                if (!authorColumnExists.Any())
                {
                    Console.WriteLine("Author column missing. Recreating table...");
                    // Drop and recreate the table
                    await context.Database.ExecuteSqlRawAsync("DROP TABLE IF EXISTS rss_items");
                    await context.Database.ExecuteSqlRawAsync(
                        "CREATE TABLE rss_items (id SERIAL PRIMARY KEY, title VARCHAR(1000) NOT NULL, description TEXT, link VARCHAR(2000) NOT NULL, pub_date TIMESTAMP WITH TIME ZONE NOT NULL, author VARCHAR(500), category VARCHAR(250))");
                    Console.WriteLine("Table recreated with correct schema.");
                }
                else
                {
                    Console.WriteLine("Table exists and has Author column.");
                }
            }
            else
            {
                Console.WriteLine("Table rss_items does not exist. Creating new table...");
                await context.Database.ExecuteSqlRawAsync(
                    "CREATE TABLE rss_items (id SERIAL PRIMARY KEY, title VARCHAR(1000) NOT NULL, description TEXT, link VARCHAR(2000) NOT NULL, pub_date TIMESTAMP WITH TIME ZONE NOT NULL, author VARCHAR(500), category VARCHAR(250))");
                Console.WriteLine("Table created with correct schema.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in RecreateTableIfNecessary: {ex.Message}");
        }
    }

    static async Task<string?> FetchRssFeed(string url)
    {
        try
        {
            using (var httpClient = new HttpClient())
            {
                var response = await httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync();
                }
                else
                {
                    Console.WriteLine($"Failed to fetch RSS feed. Status code: {response.StatusCode}");
                    return null;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching RSS feed: {ex.Message}");
            return null;
        }
    }

    static List<RssItem> ParseRssFeed(string? rssContent)
    {
        var items = new List<RssItem>();
        
        if (string.IsNullOrEmpty(rssContent))
        {
            Console.WriteLine("RSS content is null or empty.");
            return items;
        }
        
        try
        {
            var doc = XDocument.Parse(rssContent);
            var rssItems = doc.Descendants("item");
            
            foreach (var item in rssItems)
            {
                var rssItem = new RssItem
                {
                    Title = item.Element("title")?.Value,
                    Description = item.Element("description")?.Value,
                    Link = item.Element("link")?.Value,
                    PubDate = ParseDateTime(item.Element("pubDate")?.Value ?? ""),
                    Author = item.Element("author")?.Value,
                    Category = item.Element("category")?.Value
                };
                
                items.Add(rssItem);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error parsing RSS feed: {ex.Message}");
        }
        
        return items;
    }

    static DateTime ParseDateTime(string dateString)
    {
        Console.WriteLine($"initial string {dateString}");
        if (DateTime.TryParse(dateString, out DateTime result))
        {
            // Convert to UTC after parsing
            Console.WriteLine($"parsed value {result}");
            return DateTime.SpecifyKind(result, DateTimeKind.Utc);
        }
        return DateTime.UtcNow; // Return current UTC time as fallback
    }
}