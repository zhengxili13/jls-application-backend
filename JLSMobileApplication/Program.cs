using System;
using System.IO;
using System.Text;
using AutoMapper;
using Hangfire;
using JLSApplicationBackend.ApplicationServices;
using JLSApplicationBackend.Constants;
using JLSApplicationBackend.Heplers;
using JLSApplicationBackend.hubs;
using JLSApplicationBackend.Middleware;
using JLSApplicationBackend.Services;
using JLSDataAccess;
using JLSDataAccess.Interfaces;
using JLSDataAccess.Repositories;
using JLSDataModel.Models;
using JLSDataModel.Models.User;
using JLSMobileApplication.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json.Serialization;
using Serilog;

// Add an init logger
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("Logs/startup-errors.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    Log.Information("Application Starting...");

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, config) => config.ReadFrom.Configuration(context.Configuration));

    // Add services to the container.
    var services = builder.Services;
    var configuration = builder.Configuration;

    services.AddAutoMapper();
    services.AddRazorPages();
    services.AddControllersWithViews().AddNewtonsoftJson(options =>
    {
        options.SerializerSettings.DateFormatString = "yyyy-MM-dd HH:mm:ss";
        options.SerializerSettings.ContractResolver = new DefaultContractResolver();
    });

    services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "JLS backend",
            Version = "v3.1",
            Description = "JLS backend"
        });
    });

    services.AddDbContext<JlsDbContext>(options =>
        options.UseSqlServer(configuration.GetConnectionString("DefaultConnectionString")));

    services.AddIdentityCore<User>()
        .AddRoles<IdentityRole<int>>()
        .AddEntityFrameworkStores<JlsDbContext>();

    var appSettingsSection = configuration.GetSection("AppSettings");
    services.Configure<AppSettings>(appSettingsSection);

    Log.Logger = new LoggerConfiguration()
        .ReadFrom.Configuration(configuration)
        .CreateLogger();
    Log.Information("Start logging");

    services.AddTransient<IOrderServices, OrderServices>();
    services.AddTransient<IEmailService, MailkitEmailService>();
    services.AddTransient<IExportService, ExportService>();
    services.AddTransient<ISendEmailAndMessageService, SendEmailAndMessageService>();
    
    // Register Scriban renderer and pass the path to the HTML templates
    var templateDir = Path.Combine(AppContext.BaseDirectory, "EmailTemplates");
    services.AddTransient<IEmailTemplateRenderer>(_ => new ScribanEmailTemplateRenderer(templateDir));

    services.Configure<IdentityOptions>(options =>
    {
        options.Password.RequireDigit = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
        options.Password.RequiredLength = 6;
        options.Password.RequiredUniqueChars = 0;
        options.User.RequireUniqueEmail = true;
    });

    services.Configure<DataProtectionTokenProviderOptions>(options =>
        options.TokenLifespan = TimeSpan.FromDays(10));

    var appSettings = appSettingsSection.Get<AppSettings>();
    Initialization._appSettings = appSettings;
    var key = Encoding.ASCII.GetBytes(appSettings.Secret);

    services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = "JwtBearer";
            options.DefaultChallengeScheme = "JwtBearer";
        })
        .AddJwtBearer("JwtBearer", jwtBearerOptions =>
        {
            jwtBearerOptions.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                ValidateIssuer = false,
                ValidateAudience = false,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ClockSkew = TimeSpan.FromMinutes(5)
            };
        });

    var origins = appSettings.AllowedOrigins.Split(";");
    services.AddCors(options =>
    {
        options.AddPolicy("_myAllowSpecificOrigins",
            builder =>
            {
                builder.WithOrigins(origins)
                    .AllowAnyHeader()
                    .WithMethods()
                    .AllowCredentials();
            });
    });

    services.AddSignalR();
    services.AddHangfire(x => x.UseSqlServerStorage(configuration.GetConnectionString("DefaultConnectionString")));
    services.AddHttpContextAccessor();
    services.AddScoped<IUserRepository, UserRepository>();
    services.AddScoped<IProductRepository, ProductRepository>();
    services.AddScoped<IOrderRepository, OrderRepository>();
    services.AddScoped<IReferenceRepository, ReferenceRepository>();
    services.AddScoped<IAdressRepository, AdressRepository>();
    services.AddScoped<IMessageRepository, MessageRepository>();
    services.AddScoped<IAnalyticsReporsitory, AnalyticsRepository>();
    services.AddScoped<TokenModel>();
    services.AddHangfireServer();

    var app = builder.Build();

    if (app.Environment.IsDevelopment()) app.UseDeveloperExceptionPage();

    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
        c.RoutePrefix = "_swagger";
    });

    app.UseHangfireDashboard();


    app.UseCors("_myAllowSpecificOrigins");

    using (var scope = app.Services.CreateScope())
    {
        var scopedProvider = scope.ServiceProvider;
        RecurringJob.AddOrUpdate(ApplicationConstants.SendEmailJob,
            () => scopedProvider.GetRequiredService<ISendEmailAndMessageService>().SendQueuedEmails(), Cron.Minutely);
    }

    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<JlsDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        Initialization.AddAdminUser(userManager, context);
    }

    app.UseErrorHandling();
    app.UseStaticFiles();

    app.UseAuthentication();
    app.UseAuthorization();

    app.UseMiddleware<VisitorCounterMiddleware>();

    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), "Angular", "Admin/dist")),
        RequestPath = "/admin"
    });

    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider =
            new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), "Angular", "Website/dist"))
    });

    app.MapControllers();
    app.MapRazorPages();
    app.MapControllerRoute("default", "{controller}/{action=Index}/{id?}");
    app.MapHub<MessageHub>("/MessageHub");

    app.Map("/admin", client =>
    {
        if (app.Environment.IsDevelopment() == false)
        {
            var clientApp2Dist = new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(
                    Path.Combine(
                        Directory.GetCurrentDirectory(),
                        @"Angular/Admin/dist"
                    )
                )
            };
            client.UseSpaStaticFiles(clientApp2Dist);
            client.UseSpa(spa =>
            {
                spa.Options.StartupTimeout = new TimeSpan(0, 5, 0);
                spa.Options.SourcePath = "Angular/Admin";
                spa.Options.DefaultPageStaticFileOptions = clientApp2Dist;
            });
        }
    });

    if (app.Environment.IsDevelopment() == false)
    {
        var clientApp2Dist = new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(
                Path.Combine(
                    Directory.GetCurrentDirectory(),
                    @"Angular/Website/dist"
                )
            )
        };
        app.UseSpaStaticFiles(clientApp2Dist);
        app.UseSpa(spa =>
        {
            spa.Options.StartupTimeout = new TimeSpan(0, 5, 0);
            spa.Options.SourcePath = "Angular/Website";
            spa.Options.DefaultPageStaticFileOptions = clientApp2Dist;
        });
    }

    Log.Information("Application started.");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "The Application failed to start:");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program { }