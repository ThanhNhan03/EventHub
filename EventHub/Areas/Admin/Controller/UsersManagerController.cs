using EventHub.DataAccess.Repository;
using EventHub.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using EventHub.Utilities;
using EventHub.Services;

namespace EventManagementSystem.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class UsersManagerController : Controller
    {
        private readonly IUserRepository _userRepository;
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IEmailService _emailService;
        private readonly ILogger<EmailService> _logger;

        public UsersManagerController(
            IUserRepository userRepository,
            UserManager<User> userManager,
            RoleManager<IdentityRole> roleManager,
            IEmailService emailService,
            ILogger<EmailService> logger)
        {
            _userRepository = userRepository;
            _userManager = userManager;
            _roleManager = roleManager;
            _emailService = emailService;
            _logger = logger;
        }

        private async Task SendSuspensionEmail(User user, int durationDays, string reason, DateTime endDate)
        {
            string subject = "Your account has been suspended";
            string message = $@"
                <h2>Account Suspension Notice</h2>
                <p>Dear {user.DisplayName},</p>
                <p>Your account has been suspended for {durationDays} days until {endDate.ToLocalTime():dd/MM/yyyy}.</p>
                <p><strong>Reason for suspension:</strong></p>
                <p>{reason}</p>
                <p>If you believe this is a mistake, please contact our support team.</p>
                <p>Best regards,<br>EventHub Team</p>";

            await _emailService.SendEmailAsync(user.Email, subject, message);
        }

        private async Task SendBanEmail(User user, string reason)
        {
            string subject = "Your account has been banned";
            string message = $@"
                <h2>Account Ban Notice</h2>
                <p>Dear {user.DisplayName},</p>
                <p>Your account has been permanently banned from our platform.</p>
                <p><strong>Reason for ban:</strong></p>
                <p>{reason}</p>
                <p>If you believe this is a mistake, please contact our support team.</p>
                <p>Best regards,<br>EventHub Team</p>";

            await _emailService.SendEmailAsync(user.Email, subject, message);
        }

        public async Task<IActionResult> Index(string searchString, int page = 1, int pageSize = 10)
        {
            var users = await _userRepository.GetAllAsync();

            // Apply search filter if provided
            if (!string.IsNullOrEmpty(searchString))
            {
                users = users.Where(u =>
                    u.UserName.Contains(searchString, StringComparison.OrdinalIgnoreCase) ||
                    u.Email.Contains(searchString, StringComparison.OrdinalIgnoreCase) ||
                    u.DisplayName.Contains(searchString, StringComparison.OrdinalIgnoreCase));
            }

            var allUsers = users.ToList();
            var paginatedUsers = new PaginatedList<User>(
                allUsers.Skip((page - 1) * pageSize).Take(pageSize).ToList(),
                allUsers.Count,
                page,
                pageSize
            );

            paginatedUsers.SearchQuery = searchString;
            ViewBag.CurrentPageSize = pageSize;
            ViewBag.SearchString = searchString;

            return View(paginatedUsers);
        }

        public async Task<IActionResult> Detail(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var roles = await _userManager.GetRolesAsync(user);
            ViewBag.Roles = roles;
            ViewBag.RegisteredEvents = await _userRepository.GetRegisteredEventsAsync(id);

            return View(user);
        }

        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var roles = await _userManager.GetRolesAsync(user);
            ViewBag.Roles = roles;
            ViewBag.AllRoles = _roleManager.Roles.ToList();

            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(User user, List<string> selectedRoles)
        {
            if (ModelState.IsValid)
            {
                var existingUser = await _userManager.FindByIdAsync(user.Id);
                if (existingUser == null)
                {
                    return NotFound();
                }

                existingUser.DisplayName = user.DisplayName;
                existingUser.Email = user.Email;
                existingUser.PhoneNumber = user.PhoneNumber;
                existingUser.UserName = user.UserName;

                var result = await _userManager.UpdateAsync(existingUser);
                if (result.Succeeded)
                {
                    // Update roles
                    var userRoles = await _userManager.GetRolesAsync(existingUser);

                    // Remove roles not in the selection
                    foreach (var role in userRoles)
                    {
                        if (!selectedRoles.Contains(role))
                        {
                            await _userManager.RemoveFromRoleAsync(existingUser, role);
                        }
                    }

                    // Add new roles
                    foreach (var role in selectedRoles)
                    {
                        if (!userRoles.Contains(role))
                        {
                            await _userManager.AddToRoleAsync(existingUser, role);
                        }
                    }

                    TempData["Success"] = "User updated successfully";
                    return RedirectToAction(nameof(Index));
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }

            ViewBag.AllRoles = _roleManager.Roles.ToList();
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Suspend(string id, int durationDays, string reason)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            // Set lockout end date
            var lockoutEndDate = DateTime.UtcNow.AddDays(durationDays);
            await _userManager.SetLockoutEnabledAsync(user, true);
            await _userManager.SetLockoutEndDateAsync(user, lockoutEndDate);

            // Send email notification
            try
            {
                await SendSuspensionEmail(user, durationDays, reason, lockoutEndDate);
            }
            catch (Exception ex)
            {
                // Log the error but don't stop the suspension process
                _logger.LogError(ex, "Failed to send suspension email to {Email}", user.Email);
            }

            TempData["Success"] = $"User {user.UserName} suspended until {lockoutEndDate.ToLocalTime()}";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Ban(string id, string reason)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            // Permanently lock the account
            await _userManager.SetLockoutEnabledAsync(user, true);
            await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);

            // Send email notification
            try
            {
                await SendBanEmail(user, reason);
            }
            catch (Exception ex)
            {
                // Log the error but don't stop the ban process
                _logger.LogError(ex, "Failed to send ban email to {Email}", user.Email);
            }

            TempData["Success"] = $"User {user.UserName} has been banned permanently";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unban(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            // Reset all lockout settings
            await _userManager.SetLockoutEndDateAsync(user, null);
            await _userManager.SetLockoutEnabledAsync(user, false);
            await _userManager.ResetAccessFailedCountAsync(user);

            TempData["Success"] = $"User {user.UserName} has been unbanned";
            return RedirectToAction(nameof(Index));
        }

        private async Task SendAccountCreationEmail(User user, string password)
        {
            string subject = "Your EventHub Account Has Been Created";
            string message = $@"
                <h2>Welcome to EventHub!</h2>
                <p>Dear {user.DisplayName},</p>
                <p>An account has been created for you on the EventHub platform.</p>
                <p><strong>Your login details:</strong></p>
                <p>Username: {user.UserName}</p>
                <p>Email: {user.Email}</p>
                <p>Password: {password}</p>
                <p>Please change your password after your first login for security reasons.</p>
                <p>Best regards,<br>EventHub Team</p>";

            await _emailService.SendEmailAsync(user.Email, subject, message);
        }

        // GET: Admin/UsersManager/Create
        public IActionResult Create()
        {
            ViewBag.AllRoles = _roleManager.Roles.ToList();
            return View();
        }

        // POST: Admin/UsersManager/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(User user, List<string> selectedRoles)
        {
            if (ModelState.IsValid)
            {
                // Use email as username
                user.UserName = user.Email;
                
                // Set creation time
                user.Create_at = DateTime.UtcNow;
                
                // Generate a random password using the utility class
                string password = PasswordGenerator.GenerateRandomPassword();
                
                // Create the user with the generated password
                var result = await _userManager.CreateAsync(user, password);
                
                if (result.Succeeded)
                {
                    // Assign selected roles to the user
                    if (selectedRoles != null && selectedRoles.Count > 0)
                    {
                        foreach (var role in selectedRoles)
                        {
                            await _userManager.AddToRoleAsync(user, role);
                        }
                    }
                    
                    // Send email with account details
                    try
                    {
                        await SendAccountCreationEmail(user, password);
                        TempData["Success"] = $"User account created successfully and login details sent to {user.Email}.";
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send account creation email to {Email}", user.Email);
                        TempData["Success"] = $"User account created successfully but failed to send email with login details.";
                    }
                    
                    return RedirectToAction(nameof(Index));
                }
                
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }
            
            ViewBag.AllRoles = _roleManager.Roles.ToList();
            return View(user);
        }
    }
}