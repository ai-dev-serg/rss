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
    public string Title { get; set; }
    public string Description { get; set; }
    public string Link { get; set; }
    public DateTime PubDate { get; set; }
    public string Author { get; set; }
    public string Category { get; set; }
}

// Database context
public class RssDbContext : DbContext
{
    public DbSet<RssItem> RssItems { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Replace with your actual PostgreSQL connection string
        optionsBuilder.UseNpgsql("Host=localhost;Database=rssdb;Username=postgres;Password=yourpassword");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RssItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.Property(e => e.Link).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.Author).HasMaxLength(200);
            entity.Property(e => e.Category).HasMaxLength(100);
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
        if (rssFeed == null)
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
            // Create database if it doesn't exist
            await context.Database.EnsureCreatedAsync();
            
            // Clear existing data (optional)
            // await context.RssItems.ExecuteDeleteAsync();
            
            // Add new items
            foreach (var item in rssItems)
            {
                await context.RssItems.AddAsync(item);
            }
            
            await context.SaveChangesAsync();
            Console.WriteLine($"Successfully stored {rssItems.Count} items to database.");
        }
    }

    static async Task<string> FetchRssFeed(string url)
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

    static List<RssItem> ParseRssFeed(string rssContent)
    {
        var items = new List<RssItem>();
        
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
                    Author = item.Element("author")?.Value ?? "",
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
        if (DateTime.TryParse(dateString, out DateTime result))
        {
            return result;
        }
        return DateTime.Now; // Return current time as fallback
    }
}