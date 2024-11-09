using System.Reflection;
using System.Text.Json.Serialization;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

using NotificationService.Configuration;
using NotificationService.DAL.Roles;
using NotificationService.Managers;
using NotificationService.Managers.Interfaces;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.AddHttpLogging(logging =>
{
    logging.LoggingFields = HttpLoggingFields.RequestPath
                            | HttpLoggingFields.RequestBody
                            | HttpLoggingFields.ResponseBody
                            | HttpLoggingFields.Duration
                            | HttpLoggingFields.ResponseStatusCode;
});

#if DEBUG

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigin",
        policyBuilder =>
        {
            policyBuilder.AllowAnyOrigin()
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
});

#endif

builder.Services.AddAuthorizationBuilder()
    .AddPolicy(nameof(RolePolicy.RequireAnyAdmin), policy =>
        policy.RequireRole(nameof(UserRole.SuperAdmin), nameof(UserRole.Admin)));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = JwtConfig.Values.Issuer,
            ValidateAudience = true,
            ValidAudience = JwtConfig.Values.Audience,
            ValidateLifetime = true,
            IssuerSigningKey = JwtConfig.Values.Key,
            ValidateIssuerSigningKey = true,
        };
    });

builder.Services.AddScoped<IMailManager, MailManager>();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddEndpointsApiExplorer();

if (builder.Environment.IsDevelopment())
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc(
            "current",
            new OpenApiInfo
            {
                Title = "Notification Service API",
                Version = Assembly.GetExecutingAssembly().GetName().Version?.ToString()
            });

        options.IncludeXmlComments(
            Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml"));

        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description =
                "JWT Authorization header using the Bearer scheme. \r\n\r\n" +
                "Enter 'Bearer' [space and then your token in the text input below. \r\n\r\n" +
                "Example: 'Bearer HHH.PPP.CCC'",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Scheme = "Bearer"
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
                    },
                    Scheme = "OAuth 2.0",
                    Name = "Bearer",
                    In = ParameterLocation.Header
                },
                new List<string>()
            }
        });
    });

var app = builder.Build();

#if DEBUG

app.UseCors("AllowSpecificOrigin");

#endif

app.UseHttpLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger(options =>
    {
        options.RouteTemplate = "/api/{documentName}/swagger.json";
        options.PreSerializeFilters.Add((document, _) => document.Servers.Clear());
    });

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/api/current/swagger.json", "Notification Service API");
        options.RoutePrefix = "api";
    });

    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();