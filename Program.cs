using Microsoft.EntityFrameworkCore;
using DotNetEnv;
using EIUBetApp.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

// Load .env file
DotNetEnv.Env.Load();

// Read connection string from environment variable
var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION");

// Add services to the container
builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<EIUBetAppContext>(options =>
    options.UseSqlServer(connectionString));

//builder.Services.AddDefaultIdentity<IdentityUser>()
//    .AddEntityFrameworkStores<EIUBetAppContext>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Home/Login";
    });

builder.Services.AddAuthorization();

builder.Services.AddSignalR();
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
app.UseAuthentication();
app.UseAuthorization();
app.MapHub<EIUBetAppHub>("/bethub");
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
