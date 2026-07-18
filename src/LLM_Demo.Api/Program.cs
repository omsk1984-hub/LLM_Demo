using System.Text;
using LLM_Demo.Api.Endpoints;
using LLM_Demo.Api.Extensions;
using LLM_Demo.Api.Middleware;
using LLM_Demo.Application.DI;
using LLM_Demo.Infrastructure.Auth;
using LLM_Demo.Infrastructure.DI;
using LLM_Demo.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Register layers
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// JWT Authentication
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtSecret = jwtSection["SecretKey"] ?? "default-dev-key";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSection["Issuer"] ?? "LLM_Demo",
        ValidAudience = jwtSection["Audience"] ?? "LLM_Demo_Api",
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtSecret)),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "LLM_Demo API", Version = "v1" });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = @"
Dev-токен автозаполняется автоматически (demo_user / demo@example.com).
Токен для другого пользователя: выполните POST /api/auth/login и вставьте токен вручную."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// CORS (for React dev server and SSE clients)
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});


// Register endpoints
builder.Services.AddScoped<AuthEndpoints>();
builder.Services.AddScoped<AgentEndpoints>();
builder.Services.AddScoped<ConversationEndpoints>();
builder.Services.AddScoped<ChatEndpoints>();

var app = builder.Build();

// Middleware pipeline
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

// Exception handling
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Swagger (только Development)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.InjectJavascript("/swagger-dev-token.js");
    });
}

// Map endpoints
app.MapGroup("/api/auth").MapAuthEndpoints().AllowAnonymous();
app.MapGroup("/api/agents").MapAgentEndpoints().RequireAuthorization();
app.MapGroup("/api/conversations").MapConversationEndpoints().RequireAuthorization();
app.MapGroup("/api/chat").MapChatEndpoints().RequireAuthorization();
app.MapGroup("/api/tools").MapToolEndpoints().RequireAuthorization();

// Serve React SPA static files in production
if (!app.Environment.IsDevelopment())
{
    var frontendDistPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "LLM_Demo.Frontend", "dist");
    if (Directory.Exists(frontendDistPath))
    {
        app.UseDefaultFiles(new DefaultFilesOptions
        {
            FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(frontendDistPath),
            DefaultFileNames = new[] { "index.html" }
        });
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(frontendDistPath)
        });
        app.MapFallbackToFile("index.html", new StaticFileOptions
        {
            FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(frontendDistPath)
        });
    }
}

// Применяем миграции и seed-данные
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await DbSeeder.SeedAsync(context);
}

// Dev-токен: динамический endpoint для автозаполнения в Swagger (без записи на диск)
if (app.Environment.IsDevelopment())
{
    string? cachedDevToken = null;

    app.MapGet("/swagger-dev-token.js", async (HttpContext http) =>
    {
        if (cachedDevToken is null)
        {
            var jwtService = http.RequestServices.GetRequiredService<JwtTokenService>();
            cachedDevToken = jwtService.GenerateToken(
                "a1b2c3d4-e5f6-7890-abcd-ef1234567890", ["user"]);
        }

        http.Response.ContentType = "application/javascript";
        await http.Response.WriteAsync($@"
(function() {{
    var USER_INFO = {{
        id: 'a1b2c3d4-e5f6-7890-abcd-ef1234567890',
        username: 'demo_user',
        email: 'demo@example.com'
    }};

    function showUserBar() {{
        var bar = document.createElement('div');
        bar.id = 'swagger-dev-user-bar';
        bar.style.cssText = 'display:flex;align-items:center;gap:12px;padding:10px 16px;margin-bottom:12px;background:linear-gradient(135deg,#e8f5e9,#c8e6c9);border:1px solid #66bb6a;border-radius:8px;font-family:-apple-system,BlinkMacSystemFont,Segoe UI,Roboto,sans-serif;font-size:14px;color:#1b5e20;';

        var avatar = document.createElement('div');
        avatar.style.cssText = 'width:36px;height:36px;border-radius:50%;background:#2e7d32;color:#fff;display:flex;align-items:center;justify-content:center;font-weight:700;font-size:16px;flex-shrink:0;';
        avatar.textContent = USER_INFO.username.charAt(0).toUpperCase();

        var info = document.createElement('div');
        info.style.cssText = 'flex:1;line-height:1.4;';
        info.innerHTML = '<strong>✓ Dev-токен автозаполнен</strong><br><span style=""font-size:13px;opacity:0.85;"">Пользователь: ' + USER_INFO.username + '' | ' + USER_INFO.email + '</span>';

        var badge = document.createElement('span');
        badge.style.cssText = 'background:#2e7d32;color:#fff;border-radius:4px;padding:3px 10px;font-size:12px;font-weight:600;flex-shrink:0;';
        badge.textContent = 'dev';

        bar.appendChild(avatar);
        bar.appendChild(info);
        bar.appendChild(badge);

        var topbar = document.querySelector('.swagger-ui .topbar');
        if (topbar) {{
            topbar.after(bar);
        }}
    }}

    setTimeout(function() {{
        var btn = document.querySelector('.swagger-ui .btn.authorize');
        if (btn) btn.click();
        setTimeout(function() {{
            var input = document.querySelector('.auth-container input[type=""text""]');
            if (input) {{
                input.value = '{cachedDevToken}';
                input.dispatchEvent(new Event('input', {{ bubbles: true }}));
            }}
            var authBtn = document.querySelector('.auth-btn-wrapper .btn.modal-btn.auth');
            if (authBtn) authBtn.click();
            showUserBar();
        }}, 300);
    }}, 1000);
}})();
");
    });
}

app.Run();
