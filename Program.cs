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

    public override string ToString()
    {
        return $"RssItem {{ Id = {Id}, Title = \"{Title}\", Description = \"{Description}\", Link = \"{Link}\", PubDate = {PubDate}, Author = \"{Author}\", Category = \"{Category}\" }}";
    }
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
            entity.Property(e => e.Author).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Category).IsRequired().HasMaxLength(250);
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

                // Add new items
                foreach (var item in rssItems)
                {
                    // Convert to UTC before saving
                    item.PubDate = DateTime.SpecifyKind(item.PubDate, DateTimeKind.Utc);
                    context.RssItems.Add(item);
                    Console.WriteLine($"Data - {item}");
                }

                Console.WriteLine("Try to store");
                context.SaveChanges();
                Console.WriteLine($"Successfully stored {rssItems.Count} items to database.");
            }
            catch (DbUpdateException dbue)
            {
                Console.WriteLine($"Error: {dbue.Message}");
                if (dbue.InnerException != null)
                    Console.WriteLine($"Inner Exception: {dbue.InnerException.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"General error: {ex.Message}");
                if (ex.InnerException != null)
                    Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
            }
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
                    Title = item.Element("title")?.Value ?? "",
                    Description = item.Element("description")?.Value ?? "",
                    Link = item.Element("link")?.Value ?? "",
                    PubDate = ParseDateTime(item.Element("pubDate")?.Value ?? ""),
                    Author = item.Element("author")?.Value ?? "Fuck",
                    Category = item.Element("category")?.Value ?? ""
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