using DataBaseApi.Services;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
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

// Firebase Admin SDK
var serviceAccountPath = builder.Configuration["Firebase:ServiceAccountPath"] ?? "firebase-service-account.json";
if (File.Exists(serviceAccountPath))
{
    // Lire manuellement pour normaliser les fins de ligne (CRLF → LF)
    // qui peuvent corrompre la clé privée RSA au parsing
    var jsonContent = File.ReadAllText(serviceAccountPath)
        .Replace("\r\n", "\n")
        .Replace("\r", "\n");

    FirebaseApp.Create(new AppOptions
    {
        Credential = GoogleCredential.FromJson(jsonContent)
    });
}
else
{
    throw new FileNotFoundException(
        $"Firebase service account file not found: {serviceAccountPath}. " +
        "Placer firebase-service-account.json dans le répertoire de l'API.");
}

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

// Politique "AdminOnly" : vérifie le custom claim Firebase admin=true
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireAssertion(ctx =>
        {
            var claim = ctx.User.FindFirst("admin");
            return claim != null && (claim.Value == "true" || claim.Value == "True");
        }));
});

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
