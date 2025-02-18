using CloudinaryDotNet;
using dotenv.net;
using EventHub.DataAccess.Data;
using EventHub.DataAccess.Repository;
using EventHub.DataAccess.Repository.Admin;
using EventHub.Initializers;
using EventHub.Models;
using EventHub.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

//Inject the DbContext
builder.Services.AddDbContext<EventManagementSystemDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
    options.EnableSensitiveDataLogging();
});


//Inject Identity
builder.Services.AddIdentity<User, IdentityRole>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 1;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
    options.Password.RequiredUniqueChars = 0;
})
    .AddDefaultUI()
    .AddEntityFrameworkStores<EventManagementSystemDbContext>();

/* ====Provide ClaimsPrincipal and Handle concurrent connection to Database issues in Blazor==== */
builder.Services.AddScoped<AuthenticationStateProvider, ServerAuthenticationStateProvider>();
builder.Services.AddScoped<UserManager<User>>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddMemoryCache();
/* ====Provide ClaimsPrincipal and Handle concurrent connection to Database issues in Blazor==== */


builder.Services.AddScoped<RoleInitializer>();

builder.Services.AddScoped<IAdminEventRepository, AdminEventRepository>();
builder.Services.AddScoped<IAdminTicketTypeRepository, AdminTicketTypeRepository>();
builder.Services.AddScoped<IAdminUserRepository, AdminUserRepository>();
builder.Services.AddScoped<IEventRepository, EventRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ITicketTypeRepository, TicketTypeRepository>();
builder.Services.AddScoped<IEventService, EventService>();

builder.Services.AddSingleton(sp =>
{
    DotEnv.Load(options: new DotEnvOptions(probeForEnv: true));
    var cloudinary = new Cloudinary(Environment.GetEnvironmentVariable("CLOUDINARY_URL"));
    cloudinary.Api.Secure = true;
    return cloudinary;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");


// ========================FOR INITIALIZE===========================//
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<EventManagementSystemDbContext>();
    var userManager = services.GetRequiredService<UserManager<User>>();
    var roleInitializer = services.GetRequiredService<RoleInitializer>();

    await roleInitializer.InitializeRolesAsync();
    await DbInitializer.SeedAsync(context, userManager);
}

    app.Run();
