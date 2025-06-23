//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Identity;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;
//using RRealEstateApi.Data;
//using RRealEstateApi.DTOs;
//using RRealEstateApi.Models;

//[Authorize]
//[ApiController]
//[Route("api/[controller]")]
//public class TransactionController : ControllerBase
//{
//    private readonly RealEstateDbContext _context;
//    private readonly UserManager<ApplicationUser> _userManager;

//    public TransactionController(RealEstateDbContext context, UserManager<ApplicationUser> userManager)
//    {
//        _context = context;
//        _userManager = userManager;
//    }

//    [HttpPost]
//    public async Task<IActionResult> CreateTransaction([FromBody] CreateTransactionDto dto)
//    {
//        var user = await _userManager.GetUserAsync(User);
//        var property = await _context.Properties.FindAsync(dto.PropertyId);

//        if (property == null)
//            return NotFound("Property not found");

//        var transaction = new Transaction
//        {
//            PropertyId = dto.PropertyId,
//            BuyerId = user.Id,
//            Amount = dto.Amount,
//            Status = "Pending"
//        };

//        _context.Transactions.Add(transaction);
//        await _context.SaveChangesAsync();

//        return Ok(new { message = "Transaction initiated", transactionId = transaction.Id });
//    }

//    [HttpGet]
//    public async Task<IActionResult> GetMyTransactions()
//    {
//        var user = await _userManager.GetUserAsync(User);
//        var transactions = await _context.Transactions
//            .Where(t => t.BuyerId == user.Id)
//            .Include(t => t.Property)
//            .ToListAsync();

//        return Ok(transactions);
//    }
//}