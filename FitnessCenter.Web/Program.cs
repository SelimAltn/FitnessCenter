using FitnessCenter.Web.Data.Context;
using FitnessCenter.Web.Models;
using FitnessCenter.Web.Models.Entities;
using FitnessCenter.Web.Services.Implementations;
using FitnessCenter.Web.Services.Interfaces;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
    

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();

// ===== Upload Size Limit (foto yüklemede crash önleme) =====
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 10 * 1024 * 1024; // 10 MB
});

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 10 * 1024 * 1024; // 10 MB
});

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
    options.Password.RequiredLength = 3;
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

    options.AddPolicy("TrainerOnly", policy =>
    {
        policy.RequireRole("Trainer");
    });

    options.AddPolicy("BranchManagerOnly", policy =>
    {
        policy.RequireRole("BranchManager");
    });

    options.AddPolicy("AdminOrBranchManager", policy =>
    {
        policy.RequireRole("Admin", "BranchManager");
    });
});

// ===== AI Servis Yapılandırması =====
// DeepSeek (Text/Plan generation)
builder.Services.Configure<AiSettings>(
    builder.Configuration.GetSection("AiSettings"));

// Groq (Vision/Photo analysis)
builder.Services.Configure<GroqSettings>(
    builder.Configuration.GetSection("GroqSettings"));

// MemoryCache (AI yanıt cache için)
builder.Services.AddMemoryCache();

// DeepSeek Service (metin plan üretimi)
builder.Services.AddHttpClient<IDeepSeekService, DeepSeekService>();

// Groq Vision Service (fotoğraf analizi)
builder.Services.AddHttpClient<IAiVisionService, GroqVisionService>();

// Image Generation Service (Placeholder)
builder.Services.AddScoped<IImageGenerationService, PlaceholderImageService>();

// ===== Email Servis Yapılandırması =====
builder.Services.Configure<SmtpSettings>(
    builder.Configuration.GetSection("SmtpSettings"));
builder.Services.AddScoped<IEmailService, EmailService>();

// ===== Bildirim Servisi =====
builder.Services.AddScoped<IBildirimService, BildirimService>();

// ===== Mesaj Servisi =====
builder.Services.AddScoped<IMesajService, MesajService>();

// ===== Appearance Image Mapper (Kural tabanlı before/after görsel eşleştirme) =====
builder.Services.AddSingleton<AppearanceImageMapper>();

// ===== OpenAI Image Service (Photo Mode için - AKTİF) =====
// API Key: OpenAIImageSettings:ApiKey veya OPENAI_API_KEY environment variable
builder.Services.Configure<OpenAIImageSettings>(options =>
{
    builder.Configuration.GetSection("OpenAIImageSettings").Bind(options);
    // ENV fallback: OPENAI_API_KEY
    if (string.IsNullOrEmpty(options.ApiKey))
    {
        options.ApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? "";
    }
});
builder.Services.AddHttpClient<OpenAIImageService>();


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
