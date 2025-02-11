using ApplicationSecurityApp;
using ApplicationSecurityApp.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddScoped<ReCaptchaService>();
builder.Services.AddHttpClient();



// Register ApplicationDbContext with SQL Server
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add authentication & session
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(2); // Session timeout
        options.SlidingExpiration = true; // Extend session if active
    });

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Session expires after 2 mins
    options.Cookie.HttpOnly = true; // Prevent JS access
    options.Cookie.IsEssential = true;
});

builder.Services.AddDistributedMemoryCache(); // Required for session
builder.Services.AddHttpContextAccessor(); // Allows accessing HttpContext in middleware

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseStatusCodePagesWithReExecute("/Error/{0}");


app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseSession(); // Enable session

// ✅ Middleware: Enforce Single Active Session
app.Use(async (context, next) =>
{
    var userId = context.Session.GetInt32("UserId");
    var sessionId = context.Session.GetString("SessionId");

    if (userId.HasValue && !string.IsNullOrEmpty(sessionId))
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var user = await dbContext.Members.FirstOrDefaultAsync(m => m.Id == userId);

        if (user == null || user.SessionId != sessionId)
        {
            Console.WriteLine($"🔴 Session mismatch detected for User ID: {userId}. Forcing logout.");

            context.Session.Clear();
            await context.SignOutAsync();
            context.Response.Redirect("/Account/Login");
            return;
        }
    }

    await next();
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
