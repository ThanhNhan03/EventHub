using EventHub.DataAccess.Repository;
using EventHub.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EventHub.Areas.Identity.Pages.Account.Manage
{
    public class EventRegisteredHistoryModel : PageModel
    {
        private readonly UserManager<User> _userManager;
        private readonly IUserRepository _userRepository;

        public EventRegisteredHistoryModel(UserManager<User> userManager, IUserRepository userRepository)
        {
            _userManager = userManager;
            _userRepository = userRepository;
        }

        public IEnumerable<Event> RegisteredEvents { get; set; }
        [TempData]
        public string StatusMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            RegisteredEvents = await _userRepository.GetRegisteredEventsAsync(user.Id);
            ViewData["ActivePage"] = ManageNavPages.EventRegisteredHistory;
            return Page();
        }

        public async Task<IActionResult> OnPostUnregisterAsync(int eventId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            var result = await _userRepository.UnregisterEventAsync(user.Id, eventId);
            if (!result.Succeeded)
            {
                StatusMessage = "Error: Unable to unregister from the event.";
                return RedirectToPage();
            }

            StatusMessage = "You have successfully unregistered from the event.";
            return RedirectToPage();
        }
    }
}