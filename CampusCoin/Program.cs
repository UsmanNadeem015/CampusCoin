#pragma warning disable OPENAI001
using CampusCoin.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using CampusCoin.Services;
using OpenAI.Responses;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<CampusCoinDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
    )
);

// COOOKIEEEE
builder.Services
    .AddAuthentication("CampusCoinCookie")
    .AddCookie("CampusCoinCookie", options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
        options.SlidingExpiration = true;
    });

builder.Services.AddSingleton<ResponsesClient>(sp =>
{
    var apiKey = sp
        .GetRequiredService<IConfiguration>()["OpenAI:ApiKey"];

    if (string.IsNullOrWhiteSpace(apiKey))
    {
        throw new InvalidOperationException(
            "OpenAI API key is not configured."
        );
    }

    return new ResponsesClient(apiKey: apiKey);
});

builder.Services.AddScoped<EmailService>();

builder.Services.AddScoped<AICategorizationService>();

builder.Services.AddScoped<AIInsightsService>();

var app = builder.Build();

// Category seeder
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider
        .GetRequiredService<CampusCoinDbContext>();

    await DbSeeder.SeedAsync(context);
}

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
);


app.Run();

#pragma warning restore OPENAI001
