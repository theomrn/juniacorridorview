using DataBaseApi.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using System.IO.Compression;

var builder = WebApplication.CreateBuilder(args);

// Force le port 5078

var firebaseProjectId = builder.Configuration["Firebase:ProjectId"]
    ?? throw new InvalidOperationException("Firebase:ProjectId is missing from appsettings.json");

// // Limite upload (panoramas + preview peuvent être volumineux)
// builder.WebHost.ConfigureKestrel(options =>
// {
//     options.Limits.MaxRequestBodySize = null; // illimité
// });
// builder.Services.Configure<FormOptions>(options =>
// {
//     options.MultipartBodyLengthLimit = long.MaxValue; // illimité
// });

// Controllers / Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Firebase JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = $"https://securetoken.google.com/{firebaseProjectId}";
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = $"https://securetoken.google.com/{firebaseProjectId}",
            ValidateAudience = true,
            ValidAudience = firebaseProjectId,
            ValidateLifetime = true,
        };
    });

// CORS (pas de AllowCredentials si AllowAnyOrigin)
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod());
});

// Compression HTTP
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<GzipCompressionProvider>();
    options.Providers.Add<BrotliCompressionProvider>();
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[]
 {
        "image/svg+xml",
        "image/avif", // Ajout du support pour les images AVIF
        "image/png",
        "application/javascript",
        "text/css",
        "application/octet-stream", // Pour les polices web ou autres binaires
    });
});

builder.Services.Configure<BrotliCompressionProviderOptions>(options =>
{
    options.Level = CompressionLevel.Optimal;
});

builder.Services.Configure<GzipCompressionProviderOptions>(options =>
{
    options.Level = CompressionLevel.Optimal;
});

// DI
builder.Services.AddSingleton<DatabaseService>();
builder.Services.AddSingleton<LibreTranslateService>();

var app = builder.Build();

// Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors();                  // ✔ Avant StaticFiles
app.UseResponseCompression();   // ✔ Compression
app.UseStaticFiles();           // ✔ Images avec CORS
app.UseRouting();

app.UseAuthentication();        // ← doit être avant UseAuthorization
app.UseAuthorization();
app.MapControllers();

app.Run();
