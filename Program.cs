﻿using EIUBetApp.Data;
using EIUBetApp.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Load environment variables from .env
DotNetEnv.Env.Load();

// Read connection string from environment variables
var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION");
if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("Database connection string not found in environment variables.");
}

// Add services
builder.Services.AddControllersWithViews();

// Database context
builder.Services.AddDbContext<EIUBetAppContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";

    // Set cookie expiration time
    options.ExpireTimeSpan = TimeSpan.FromMinutes(60); // Valid for 60 minutes

    // Optional: Reset expiration time on each request
    options.SlidingExpiration = true;
});

// SignalR support
builder.Services.AddSignalR(options =>
{
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(15); // default: 30s
    options.KeepAliveInterval = TimeSpan.FromSeconds(5);      // default: 15s
});

var app = builder.Build();

// Middleware pipeline
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

app.MapHub<EIUBetAppHub>("/betHub");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
