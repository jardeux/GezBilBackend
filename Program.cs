using BilbakalimAPI;
using BilbakalimAPI.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// DbContext'i SQL Server ile yapılandır
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Servisleri ekle
builder.Services.AddControllers(); // Controller'ları etkinleştir

// JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Supabase:ValidIssuer"],
            ValidAudience = builder.Configuration["Supabase:ValidAudience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Supabase:JwtSecret"]))
        };
    });

builder.Services.AddAuthorization();

// CORS politikasını ekle
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowNextApp",
        policy =>
        {
            policy.WithOrigins("http://localhost:3001", "http://localhost:3000") // Next.js uygulamasının adresi
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});



var app = builder.Build();

// Seed the database with questions if it's empty.
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<ApplicationDbContext>();

    // Ensure the database is created.
    context.Database.EnsureCreated();

    if (!context.Questions.Any())
    {
        var questionsJson = File.ReadAllText("Data/seedData.json");

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        var seedQuestions = JsonSerializer.Deserialize<List<QuestionSeedDto>>(questionsJson, options);

        if (seedQuestions != null)
        { 
            // Map from the seed DTO to the Question entity, letting the DB generate the Id.
            var questions = seedQuestions.Select(q => new Question(0, q.CityId, q.QuestionText, q.MediaUrl, q.Options, q.CorrectAnswerIndex)).ToList();
            
            context.Questions.AddRange(questions);
            context.SaveChanges();
        }
    }
}

// Geliştirme ortamı için Swagger'ı etkinleştir (isteğe bağlı)
if (app.Environment.IsDevelopment())
{
    // app.UseSwagger();
    // app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// CORS politikasını kullan
app.UseCors("AllowNextApp");

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// API Key Middleware
app.Use(async (context, next) =>
{
    if (!context.Request.Path.StartsWithSegments("/api"))
    {
        await next();
        return;
    }

    if (!context.Request.Headers.TryGetValue("X-Internal-Api-Key", out var apiKey) || 
        apiKey != builder.Configuration["ApiKey"]) 
    {
        context.Response.StatusCode = 401; // Unauthorized
        await context.Response.WriteAsync("Invalid API Key");
        return;
    }

    await next();
});

// Controller rotalarını haritala
app.MapControllers();

app.Run();

// Temporary record to match the structure of seedData.json, including 'id'.
public record QuestionSeedDto(int id, string CityId, string QuestionText, string? MediaUrl, List<string> Options, int CorrectAnswerIndex);
