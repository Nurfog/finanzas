using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FinancialAnalytics.API.Data;
using FinancialAnalytics.API.Models;

namespace FinancialAnalytics.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CustomersController : ControllerBase
{
    private readonly FinancialDbContext _context;
    private readonly ILogger<CustomersController> _logger;

    public CustomersController(FinancialDbContext context, ILogger<CustomersController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get all customers
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetCustomers([FromQuery] bool includeInactive = false)
    {
        try
        {
            var query = _context.Customers.AsQueryable();

            if (!includeInactive)
            {
                query = query.Where(c => c.IsActive);
            }

            var customers = await query
                .Include(c => c.Transactions)
                .Include(c => c.Students)
                .Select(c => new
                {
                    c.Id,
                    c.Name,
                    c.Email,
                    c.Phone,
                    c.RegistrationDate,
                    c.CustomerType,
                    c.IsActive,
                    TransactionCount = c.Transactions.Count,
                    TotalSpent = c.Transactions.Sum(t => t.Amount),
                    StudentCount = c.Students.Count
                })
                .ToListAsync();

            return Ok(customers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting customers");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get customer by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetCustomer(int id)
    {
        try
        {
            var customer = await _context.Customers
                .Include(c => c.Transactions)
                .Include(c => c.Students)
                    .ThenInclude(s => s.ProgressRecords)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (customer == null)
            {
                return NotFound(new { error = "Customer not found" });
            }

            var result = new
            {
                customer.Id,
                customer.Name,
                customer.Email,
                customer.Phone,
                customer.RegistrationDate,
                customer.CustomerType,
                customer.IsActive,
                Transactions = customer.Transactions.Select(t => new
                {
                    t.Id,
                    t.TransactionDate,
                    t.Amount,
                    t.TransactionType,
                    t.PaymentMethod,
                    t.Description
                }).OrderByDescending(t => t.TransactionDate),
                Students = customer.Students.Select(s => new
                {
                    s.Id,
                    s.Name,
                    s.Email,
                    s.Program,
                    s.EnrollmentDate,
                    s.IsActive,
                    ProgressCount = s.ProgressRecords.Count,
                    AverageScore = s.ProgressRecords.Any() ? s.ProgressRecords.Average(p => p.Score) : 0
                })
            };

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting customer {CustomerId}", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Create a new customer
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateCustomer([FromBody] Customer customer)
    {
        try
        {
            customer.RegistrationDate = DateTime.Now;
            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetCustomer), new { id = customer.Id }, customer);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating customer");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Update customer
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCustomer(int id, [FromBody] Customer customer)
    {
        try
        {
            if (id != customer.Id)
            {
                return BadRequest(new { error = "ID mismatch" });
            }

            _context.Entry(customer).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return NoContent();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await _context.Customers.AnyAsync(c => c.Id == id))
            {
                return NotFound(new { error = "Customer not found" });
            }
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating customer {CustomerId}", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
}
