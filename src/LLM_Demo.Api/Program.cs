using System.Text;
using LLM_Demo.Api.Endpoints;
using LLM_Demo.Api.Extensions;
using LLM_Demo.Api.Middleware;
using LLM_Demo.Application.DI;
using LLM_Demo.Infrastructure.DI;
using Microsoft.AspNetCore.Authentication.JwtBearer;
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
        Description = "Enter your JWT token"
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

// CORS (for SSE clients)
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

// Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Map endpoints
app.MapGroup("/api/auth").MapAuthEndpoints().AllowAnonymous();
app.MapGroup("/api/agents").MapAgentEndpoints().RequireAuthorization();
app.MapGroup("/api/conversations").MapConversationEndpoints().RequireAuthorization();
app.MapGroup("/api/chat").MapChatEndpoints().RequireAuthorization();
app.MapGroup("/api/tools").MapToolEndpoints().RequireAuthorization();

app.Run();
