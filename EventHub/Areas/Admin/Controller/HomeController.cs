using EventHub.DataAccess.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventManagementSystem.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class HomeController : Controller
    {
        private readonly IUserRepository _userRepository;
        private readonly ITicketTypeRepository _ticketTypeRepository;

        public HomeController(IUserRepository userRepository, ITicketTypeRepository ticketTypeRepository)
        {
            _userRepository = userRepository;
            _ticketTypeRepository = ticketTypeRepository;
        }

        public async Task<IActionResult> Index()
        {
            var users = await _userRepository.GetAllAsync();
            ViewBag.TotalUsers = users.Count();
            
            // Get new users this week
            ViewBag.NewUsersThisWeek = await _userRepository.GetNewUsersInWeekAsync();
            
            // Calculate total revenue from ticket sales
            decimal totalRevenue = await _ticketTypeRepository.GetTotalRevenueAsync();
            ViewBag.TotalRevenue = totalRevenue;
            
            return View();
        }
    }
}
