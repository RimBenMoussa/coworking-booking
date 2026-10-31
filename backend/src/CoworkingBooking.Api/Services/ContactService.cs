using CoworkingBooking.Api.Data;
using CoworkingBooking.Api.DTOs;
using CoworkingBooking.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CoworkingBooking.Api.Services;

public class ContactService(AppDbContext db, TimeProvider clock, ILogger<ContactService> logger)
{
    public async Task<ContactMessage> CreateAsync(CreateContactMessageRequest request)
    {
        var email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();
        var phone = string.IsNullOrWhiteSpace(request.Phone) ? null : PhoneNumber.NormalizeOrThrow(request.Phone);
        if (email is null && phone is null)
            throw BusinessException.BadRequest("Indiquez un email ou un numéro de téléphone pour qu'on puisse vous répondre.");

        var message = new ContactMessage
        {
            Name = request.Name.Trim(),
            Email = email,
            Phone = phone,
            Message = request.Message.Trim(),
            CreatedAt = clock.GetUtcNow()
        };
        db.ContactMessages.Add(message);
        await db.SaveChangesAsync();

        logger.LogInformation("Message de contact reçu {Id}", message.Id);
        return message;
    }

    public async Task<List<ContactMessageDto>> ListAsync()
    {
        var messages = await db.ContactMessages.OrderByDescending(m => m.CreatedAt).ToListAsync();
        return messages.Select(ContactMessageDto.From).ToList();
    }
}
