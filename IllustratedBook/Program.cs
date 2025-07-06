using Microsoft.EntityFrameworkCore;
using IllustratedBook.Models;
using IllustratedBook.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

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

var app = builder.Build();

app.UseStaticFiles();

app.MapControllers();
app.MapControllerRoute("controllers", "controllers/{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();
app.MapBlazorHub();

var context = app.Services.CreateScope().ServiceProvider.GetRequiredService<DataContext>();
SeedData.Initialise(context);

app.Run();
