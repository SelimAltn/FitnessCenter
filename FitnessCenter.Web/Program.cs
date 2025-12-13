using FitnessCenter.Web.Data.Context;
using FitnessCenter.Web.Models;
using FitnessCenter.Web.Models.Entities;
using FitnessCenter.Web.Services.Implementations;
using FitnessCenter.Web.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
    

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();

// Swagger servisleri:
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "FitnessCenter API",
        Version = "v1",
        Description = "Eğitmen, üye ve randevu işlemleri için REST API"
    });
});
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireDigit = false;
})
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
    {
        policy.RequireRole("Admin");
    });

    options.AddPolicy("MemberOnly", policy =>
    {
        policy.RequireRole("Member");
    });
});

// ===== AI Servis Yapılandırması =====
// AiSettings binding (appsettings.json + User Secrets + Environment Variables)
builder.Services.Configure<AiSettings>(
    builder.Configuration.GetSection("AiSettings"));

// IMemoryCache (opsiyonel ikincil cache)
builder.Services.AddMemoryCache();

// HttpClient ile AI Recommendation Service
builder.Services.AddHttpClient<IAiRecommendationService, AiRecommendationService>();

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();

    // Swagger UI
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "FitnessCenter API v1");
        c.RoutePrefix = "swagger"; // https://localhost:xxxx/swagger
    });
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    await FitnessCenter.Web.Data.Seed.SeedData.InitializeAsync(services);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

// 🔹 statik dosyalar (wwwroot için)
app.UseStaticFiles();

app.UseRouting();
app.UseAuthentication();

app.UseAuthorization();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");



app.Run();
