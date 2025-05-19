using Microsoft.EntityFrameworkCore;
using DotNetEnv;
using EIUBetApp.Data;

var builder = WebApplication.CreateBuilder(args);

// Load .env file
DotNetEnv.Env.Load();

// Read connection string from environment variable
var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION");

// Add services to the container
builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<EIUBetAppContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
