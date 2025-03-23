﻿using EventHub.DataAccess.Repository;
using EventHub.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using EventHub.Utilities;

namespace EventHub.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class UsersManagerController : Controller
    {
        private readonly IUserRepository _userRepository;
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UsersManagerController(
            IUserRepository userRepository,
            UserManager<User> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _userRepository = userRepository;
            _userManager = userManager;
            _roleManager = roleManager;
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
    }
}
