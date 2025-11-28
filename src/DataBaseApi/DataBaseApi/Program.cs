using DataBaseApi.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Configuration des services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
// ⭐️ Enregistrement du service Swagger/OpenAPI
builder.Services.AddSwaggerGen();

// Ligne redondante si AddEndpointsApiExplorer() est déjà appelé plus haut
// builder.Services.AddEndpointsApiExplorer();

// CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

// DI
builder.Services.AddSingleton<DatabaseService>();

var app = builder.Build();

// Configuration du pipeline de requêtes

// Généralement, Swagger est activé uniquement en mode Développement
if (app.Environment.IsDevelopment())
{
    // ⭐️ MIDDLEWARE SWAGGER : Essentiel pour servir la documentation
    app.UseSwagger();
    app.UseSwaggerUI();
}
// Fin de la configuration du pipeline Swagger

app.UseHttpsRedirection();
app.UseStaticFiles(); // pour servir les images depuis wwwroot
app.UseRouting();
app.UseCors();
app.UseAuthorization();
app.MapControllers();
app.Run();