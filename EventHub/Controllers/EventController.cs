﻿using EventHub.DataAccess.Repository;
using EventHub.Models;
using EventHub.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text.Json;

namespace EventHub.Controllers;

[Route("event")]
public class EventController : Controller
{
    private readonly IEventRepository _eventRepository;
    private readonly IUserRepository _userRepository;
    private readonly ITicketTypeRepository _ticketTypeRepository;
    private readonly IEventService _eventService;
    private readonly UserManager<User> _userManager;
    private readonly IStripeService _stripeService;
    private IEnumerable<Event> events;

    public EventController(
        IEventRepository eventRepository, 
        UserManager<User> userManager, 
        IUserRepository userRepository, 
        ITicketTypeRepository ticketTypeRepository, 
        IEventService eventService,
        IStripeService stripeService)
    {
        _eventRepository = eventRepository;
        _userManager = userManager;
        _userRepository = userRepository;
        _ticketTypeRepository = ticketTypeRepository;
        _eventService = eventService;
        _stripeService = stripeService;
        events = _eventService.Events;
    }

    [Route("Index")]
    public IActionResult Index()
    {
        return Redirect("/events");
    }

    [Route("Details/{id?}")]
    public async Task<IActionResult> Details(int? id)
    {
        if (id is not null)
        {
            //var existingEvent = await _eventRepository.GetByIdAsync(id.Value);
            var existingEvent = events.FirstOrDefault(e => e.Id == id);
            if (existingEvent is not null)
            {
                await _eventRepository.UpdateVisitorCountAsync(existingEvent.Id);                

                ViewBag.GoogleMap = $"https://www.google.com/maps/embed?pb=!1m18!1m12!1m3!1d3153.8354345090644!2d{existingEvent.Longitude}!3d{existingEvent.Latitude}!2m3!1f0!2f0!3f0!3m2!1i1024!2i768!4f13.1!3m3!1m2!1s0x6ad642af0f11fd81%3A0xf577b68364c12aef!2sFederation%20Square!5e0!3m2!1sen!2sau!4v1618973873610!5m2!1sen!2sau";

                existingEvent.TicketTypes = [.. existingEvent.TicketTypes!.OrderBy(tt => tt.Price)];

                foreach (var TicketType in existingEvent.TicketTypes)
                {
                    TicketType.Tickets  = [.. await _ticketTypeRepository
                        .GetTicketsByTickeyTypeIdAsync(TicketType.Id)];                    
                }

                return View(existingEvent);
            }
        }
        return Redirect("/events");
    }

    [Route("ConfirmTicket")]
    public async Task<IActionResult> ConfirmTicket(int ticketTypeId)
    {
        var ticketType = await _ticketTypeRepository.GetTicketTypeByIdAsync(ticketTypeId);

        if (ticketType is not null)
        {
            if (ticketType.TotalTicketsSold < ticketType.MaxCapital)
            {
                return View(ticketType);
            }
        }

        var backUrl = TempData["Referer"]?.ToString();
        if (!string.IsNullOrEmpty(backUrl))
        {
            return Redirect(backUrl);
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Route("ConfirmTicket")]
    public async Task<IActionResult> ConfirmTicket(TicketType ticketType)
    {
        if (ModelState.IsValid)
        {
            var user = await _userManager.GetUserAsync(User);
            var existingTicketType = await _ticketTypeRepository.GetTicketTypeByIdAsync(ticketType.Id);
    
            if (user is null || existingTicketType is null || existingTicketType.Event is null)
            {
                return Redirect("/Identity/Account/Login");
            }
    
            if (existingTicketType.TotalTicketsSold < existingTicketType.MaxCapital)
            {
                // Create Stripe checkout session
                var successUrl = Url.Action("PaymentSuccess", "Event", new { ticketTypeId = existingTicketType.Id }, Request.Scheme);
                var cancelUrl = Url.Action("PaymentCancel", "Event", new { ticketTypeId = existingTicketType.Id }, Request.Scheme);
    
                var session = await _stripeService.CreateCheckoutSessionAsync(
                    (decimal)existingTicketType.Price,
                    existingTicketType.Event.Title,
                    existingTicketType.Name,
                    successUrl!,
                    cancelUrl!
                );
    
                // Store ticket information in TempData for later use
                var ticketInfo = new
                {
                    TicketTypeId = existingTicketType.Id,
                    UserId = user.Id,
                    SessionId = session.Id
                };
                TempData["PendingTicket"] = JsonConvert.SerializeObject(ticketInfo);
    
                // Redirect to Stripe Checkout
                return Redirect(session.Url);
            }
        }
    
        var backUrl = TempData["Referer"]?.ToString();
        if (!string.IsNullOrEmpty(backUrl))
        {
            return Redirect(backUrl);
        }
    
        return RedirectToAction(nameof(Index));
    }

    [Route("SuccessfulRegister")]
    public IActionResult SuccessfulRegister()
    {
        var ticketJson = TempData["TicketSuccess"] as string;
        if (!string.IsNullOrEmpty(ticketJson))
        {
            var ticket = JsonConvert.DeserializeObject<Ticket>(ticketJson);
            return View(ticket);
        }

        return RedirectToAction(nameof(Index));
    }

    [Route("PaymentSuccess")]
    public async Task<IActionResult> PaymentSuccess(int ticketTypeId)
    {
        var ticketInfoJson = TempData["PendingTicket"] as string;
        if (string.IsNullOrEmpty(ticketInfoJson))
        {
            return RedirectToAction(nameof(Index));
        }
    
        var ticketInfo = JsonConvert.DeserializeObject<dynamic>(ticketInfoJson);
        var user = await _userManager.GetUserAsync(User);
        var existingTicketType = await _ticketTypeRepository.GetTicketTypeByIdAsync(ticketTypeId);
    
        if (user is null || existingTicketType is null || existingTicketType.Event is null)
        {
            return RedirectToAction(nameof(Index));
        }
    
        existingTicketType.TotalTicketsSold++;
        var ticket = new Ticket()
        {
            Detail = $"Ticket No.{existingTicketType.TotalTicketsSold}, name: {existingTicketType.Detail} At {existingTicketType.Event.VenueName}, {existingTicketType.Event.Country} - {existingTicketType.Event.Address} in {existingTicketType.Event.StartDate}.",
            TicketTypeId = existingTicketType.Id,
            UserId = user.Id,
            User = user
        };
    
        user.Tickets ??= [];
        user.Tickets.Add(ticket);
        await _userRepository.UpdateUserAsync(user);
        await _ticketTypeRepository.UpdateTicketTypeAsync(existingTicketType);
    
        var ticketJson = JsonConvert.SerializeObject(ticket, new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        });
        TempData["TicketSuccess"] = ticketJson;
    
        return RedirectToAction(nameof(SuccessfulRegister));
    }

    [Route("PaymentCancel")]
    public IActionResult PaymentCancel()
    {
        TempData["Error"] = "Payment was cancelled.";
        return RedirectToAction(nameof(Index));
    }
}
