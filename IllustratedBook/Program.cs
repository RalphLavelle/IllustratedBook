using Microsoft.EntityFrameworkCore;
using IllustratedBook.Models;
using IllustratedBook.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// Add session services for caching page data
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddDbContext<DataContext>(options => {
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"));
    options.EnableSensitiveDataLogging();
});

// Register HTTP client for external API calls
builder.Services.AddHttpClient();

// Register application services
builder.Services.AddScoped<BookService>();
builder.Services.AddScoped<ChatService>();
builder.Services.AddScoped<ImageService>();
builder.Services.AddScoped<ImageStorageService>();

var app = builder.Build();

app.UseStaticFiles();

// Enable session middleware
app.UseSession();

app.MapControllers();
app.MapControllerRoute("controllers", "controllers/{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();
app.MapBlazorHub();

var context = app.Services.CreateScope().ServiceProvider.GetRequiredService<DataContext>();
SeedData.Initialise(context);

app.Run();
