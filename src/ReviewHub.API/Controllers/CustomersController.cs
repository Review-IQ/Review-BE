using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReviewHub.Core.Entities;
using ReviewHub.Infrastructure.Data;
using System.Security.Claims;

namespace ReviewHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CustomersController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CustomersController> _logger;

    public CustomersController(
        ApplicationDbContext context,
        ILogger<CustomersController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet("{businessId}")]
    public async Task<IActionResult> GetCustomers(int businessId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        try
        {
            var auth0Id = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Auth0Id == auth0Id);

            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            // Validate business ownership
            var business = await _context.Businesses
                .FirstOrDefaultAsync(b => b.Id == businessId && b.UserId == user.Id);

            if (business == null)
            {
                return NotFound(new { message = "Business not found" });
            }

            var customers = await _context.Customers
                .Where(c => c.BusinessId == businessId)
                .OrderByDescending(c => c.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new
                {
                    c.Id,
                    c.Name,
                    c.Email,
                    c.PhoneNumber,
                    c.LastVisit,
                    c.TotalVisits,
                    c.CreatedAt
                })
                .ToListAsync();

            var totalCount = await _context.Customers.CountAsync(c => c.BusinessId == businessId);

            return Ok(new
            {
                customers = customers,
                totalCount = totalCount,
                page = page,
                pageSize = pageSize,
                totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting customers");
            return StatusCode(500, new { message = "Failed to get customers" });
        }
    }

    [HttpGet("detail/{id}")]
    public async Task<IActionResult> GetCustomer(int id)
    {
        try
        {
            var auth0Id = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Auth0Id == auth0Id);

            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            var customer = await _context.Customers
                .Include(c => c.Business)
                .FirstOrDefaultAsync(c => c.Id == id && c.Business.UserId == user.Id);

            if (customer == null)
            {
                return NotFound(new { message = "Customer not found" });
            }

            return Ok(new
            {
                customer.Id,
                customer.Name,
                customer.Email,
                customer.PhoneNumber,
                customer.LastVisit,
                customer.TotalVisits,
                customer.Notes,
                customer.CreatedAt,
                BusinessName = customer.Business.Name
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting customer");
            return StatusCode(500, new { message = "Failed to get customer" });
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateCustomer([FromBody] CreateCustomerRequest request)
    {
        try
        {
            var auth0Id = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Auth0Id == auth0Id);

            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            // Validate business ownership
            var business = await _context.Businesses
                .FirstOrDefaultAsync(b => b.Id == request.BusinessId && b.UserId == user.Id);

            if (business == null)
            {
                return NotFound(new { message = "Business not found" });
            }

            // Check if customer with same phone number already exists for this business
            var existingCustomer = await _context.Customers
                .FirstOrDefaultAsync(c => c.BusinessId == request.BusinessId && c.PhoneNumber == request.PhoneNumber);

            if (existingCustomer != null)
            {
                return BadRequest(new { message = "Customer with this phone number already exists for this business" });
            }

            var customer = new Customer
            {
                BusinessId = request.BusinessId,
                Name = request.Name,
                Email = request.Email,
                PhoneNumber = request.PhoneNumber,
                LastVisit = request.LastVisit ?? DateTime.UtcNow,
                TotalVisits = request.TotalVisits ?? 1,
                Notes = request.Notes,
                CreatedAt = DateTime.UtcNow
            };

            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetCustomer), new { id = customer.Id }, new
            {
                customer.Id,
                customer.Name,
                customer.Email,
                customer.PhoneNumber,
                customer.LastVisit,
                customer.TotalVisits,
                customer.CreatedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating customer");
            return StatusCode(500, new { message = "Failed to create customer" });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCustomer(int id, [FromBody] UpdateCustomerRequest request)
    {
        try
        {
            var auth0Id = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Auth0Id == auth0Id);

            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            var customer = await _context.Customers
                .Include(c => c.Business)
                .FirstOrDefaultAsync(c => c.Id == id && c.Business.UserId == user.Id);

            if (customer == null)
            {
                return NotFound(new { message = "Customer not found" });
            }

            customer.Name = request.Name;
            customer.Email = request.Email;
            customer.PhoneNumber = request.PhoneNumber;
            customer.LastVisit = request.LastVisit ?? customer.LastVisit;
            customer.TotalVisits = request.TotalVisits ?? customer.TotalVisits;
            customer.Notes = request.Notes;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                customer.Id,
                customer.Name,
                customer.Email,
                customer.PhoneNumber,
                customer.LastVisit,
                customer.TotalVisits,
                customer.Notes
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating customer");
            return StatusCode(500, new { message = "Failed to update customer" });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCustomer(int id)
    {
        try
        {
            var auth0Id = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Auth0Id == auth0Id);

            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            var customer = await _context.Customers
                .Include(c => c.Business)
                .FirstOrDefaultAsync(c => c.Id == id && c.Business.UserId == user.Id);

            if (customer == null)
            {
                return NotFound(new { message = "Customer not found" });
            }

            _context.Customers.Remove(customer);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting customer");
            return StatusCode(500, new { message = "Failed to delete customer" });
        }
    }

    [HttpPost("{id}/record-visit")]
    public async Task<IActionResult> RecordVisit(int id)
    {
        try
        {
            var auth0Id = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Auth0Id == auth0Id);

            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            var customer = await _context.Customers
                .Include(c => c.Business)
                .FirstOrDefaultAsync(c => c.Id == id && c.Business.UserId == user.Id);

            if (customer == null)
            {
                return NotFound(new { message = "Customer not found" });
            }

            customer.LastVisit = DateTime.UtcNow;
            customer.TotalVisits++;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                customer.Id,
                customer.LastVisit,
                customer.TotalVisits
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording visit");
            return StatusCode(500, new { message = "Failed to record visit" });
        }
    }
}

public record CreateCustomerRequest(
    int BusinessId,
    string Name,
    string Email,
    string PhoneNumber,
    DateTime? LastVisit,
    int? TotalVisits,
    string? Notes);

public record UpdateCustomerRequest(
    string Name,
    string Email,
    string PhoneNumber,
    DateTime? LastVisit,
    int? TotalVisits,
    string? Notes);
