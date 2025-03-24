using CloudinaryDotNet;
using dotenv.net;
using EventHub.DataAccess.Data;
using EventHub.DataAccess.Repository;
using EventHub.DataAccess.Repository.Admin;
using EventHub.Initializers;
using EventHub.Models;
using EventHub.Services;
using EventHub.ModelBinders;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using EventHub.App;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews(options =>
{
    options.ModelBinderProviders.Insert(0, new DateOnlyModelBinderProvider()); // Custom ModelBinder
    /*options.Filters.Add<SaveRefererFilter>();*/ // Custom Filter *Failed*.
});

builder.Services.AddRazorPages(); // for identity ui
builder.Services.AddRazorComponents().AddInteractiveServerComponents(); // Add blazor server

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

builder.Services.AddHttpClient();
builder.Services.AddScoped<RoleInitializer>();

builder.Services.AddScoped<IAdminEventRepository, AdminEventRepository>();
builder.Services.AddScoped<IAdminTicketTypeRepository, AdminTicketTypeRepository>();
builder.Services.AddScoped<IAdminUserRepository, AdminUserRepository>();
builder.Services.AddScoped<IEventRepository, EventRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ITicketTypeRepository, TicketTypeRepository>();
builder.Services.AddScoped<IEventService, EventService>();

builder.Services.AddScoped<IEmailService, EmailService>();

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

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
      name: "areas",
      pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}"
    );
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
    );
app.MapControllerRoute(
    name: "catchall",
    pattern: "{*url}",
    defaults: new { controller = "CatchAll", action = "Index" }
    );

app.MapRazorPages(); // for identity

app.UseAntiforgery(); // blazor protect anonymous data

app.MapRazorComponents<App>().AddInteractiveServerRenderMode(); // plug-in blazor server

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
