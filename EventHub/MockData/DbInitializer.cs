using EventHub.DataAccess.Data;
using EventHub.MockData;
using EventHub.Models;
using Microsoft.AspNetCore.Identity;
using System.Diagnostics;

public class DbInitializer
{
    public static async Task SeedAsync(EventManagementSystemDbContext context, UserManager<User> userManager)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        await SeedAdminUserAsync(userManager);

        string adminUserId = await GetAdminUserIdAsync(userManager);

        if (!context.Events.Any())
        {
            string[] categories = ["Sport", "Conferences", "Expo", "Concert", "Festival", "Art", "Community", "Holiday"];
            List<Event> newEvents = new List<Event>();

            foreach (var category in categories)
            {
                var titleAndShortList = SampleEventsData.GetTitleAndShortDescription(category);
                var imageUrls = SampleEventsData.GetAllImgsUrlBytype(category);

                for (int i = 0; i < 20; i++)
                {
                    (var title, var shortDescription) = titleAndShortList[i];
                    var description = SampleEventsData.GetRandomDescription();
                    (var startDate, var endDate) = SampleEventsData.GetRandomStartEndDate();
                    var venueName = SampleEventsData.GetRandomVenueName();
                    (var lat, var lng) = SampleEventsData.GetRandomLatLng();
                    var countryName = SampleEventsData.GetRandomCountry();
                    var address = SampleEventsData.GenerateRandomAddress(countryName);
                    var imageUrl = imageUrls[i];
                    var transports = SampleEventsData.GetRandomTransport();
                    var pageVisitorCount = SampleEventsData.GetPageVisitorCount();

                    Event newEvent = new()
                    {
                        Title = title,
                        ShortDescription = shortDescription,
                        Description = description,
                        Create_at = DateTime.UtcNow,
                        StartDate = startDate,
                        EndDate = endDate,
                        VenueName = venueName,
                        Latitude = lat,
                        Longitude = lng,
                        Country = countryName,
                        Address = address,
                        Image = imageUrl,
                        Transports = transports,
                        TicketTypes = [],
                        PageVisitorCount = pageVisitorCount,
                        Category = Enum.Parse<Category>(category),
                        UserId = adminUserId // 🔹 Sử dụng ID admin hợp lệ
                    };

                    newEvent.TicketTypes.Add(new TicketType()
                    {
                        Tickets = [],
                        Name = "Free",
                        Detail = $"Free Ticket on {title}",
                        Event = newEvent,
                        MaxCapital = 10000,
                        Price = 0
                    });

                    newEvents.Add(newEvent);
                }
            }

            await context.Events.AddRangeAsync(newEvents);
            await context.SaveChangesAsync();
        }

        stopwatch.Stop();
        Console.WriteLine($"Seeding completed in: {stopwatch.ElapsedMilliseconds} ms");
    }


    private static async Task SeedAdminUserAsync(UserManager<User> userManager)
    {
        string adminEmail = "admin@hub.com";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);

        if (adminUser == null)
        {
            adminUser = new User
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true
            };

            await userManager.CreateAsync(adminUser, "Admin@123"); // Mật khẩu mặc định
        }

        // Đảm bảo Admin có vai trò chính xác
        if (!await userManager.IsInRoleAsync(adminUser, nameof(UserRoles.Admin)))
        {
            await userManager.RemoveFromRoleAsync(adminUser, nameof(UserRoles.User));
            await userManager.AddToRoleAsync(adminUser, nameof(UserRoles.Admin));
        }
    }

    /// 🔹 Lấy ID của admin sau khi đã seed
    private static async Task<string> GetAdminUserIdAsync(UserManager<User> userManager)
    {
        string adminEmail = "admin@hub.com";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);

        return adminUser?.Id ?? throw new Exception("Admin user was not found after seeding.");
    }
}
