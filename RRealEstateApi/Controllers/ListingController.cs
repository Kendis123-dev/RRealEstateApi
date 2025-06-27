//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;
//using RRealEstateApi.Data;
//using RRealEstateApi.DTOs;
//using RRealEstateApi.Models;

//[Route("api/[controller]")]
//[ApiController]
//public class ListingController : ControllerBase
//{
//    private readonly RealEstateDbContext _context;

//    public ListingController(RealEstateDbContext context)
//    {
//        _context = context;
//    }

//    [HttpGet]
//    public async Task<ActionResult<IEnumerable<Listing>>> GetListings()
//    {
//        //return await _context.Listings
//        //    .Include(l => l.Property)
//        //    .Include(l => l.Agent)
//        //    .ToListAsync();
//        return new List<Listing> { };
//    }

//    [HttpGet("{id}")]
//    public async Task<ActionResult<Listing>> GetListing(int id)
//    {
//    var listing = await _context.Listings
//      //  .Include(l => l.Property)
//        //.Include(l => l.Agent)
//        .FirstOrDefaultAsync(l => l.Id == id);

//    if (listing == null) return NotFound();
//    return listing;
//    return new Listing { };
//    }


//    [HttpPost]
//    public async Task<IActionResult> CreateListing([FromBody] CreateListingDto dto)
//    {
//        var property = await _context.Properties.FindAsync(dto.PropertyId);
//        if (property == null)
//            return NotFound("Property not found");

//        var listing = new Listing
//        {
//            PropertyId = dto.PropertyId,
//            Title = dto.Title,
//            Description = dto.Description,
//            Price = dto.Price,
//            IsAvailable = dto.IsAvailable
//        };

//        _context.Listings.Add(listing);
//        await _context.SaveChangesAsync();

//        return CreatedAtAction(nameof(GetListing), new { id = listing.Id }, listing);
//    }


//    [HttpPut("{id}")]
//    public async Task<IActionResult> UpdateListing(int id, Listing listing)
//    {
//        if (id != listing.Id) return BadRequest();

//        _context.Entry(listing).State = EntityState.Modified;
//        await _context.SaveChangesAsync();

//        return NoContent();
//    }

//    [HttpDelete("{id}")]
//    public async Task<IActionResult> DeleteListing(int id)
//    {
//        var listing = await _context.Listings.FindAsync(id);
//        if (listing == null) return NotFound();

//        _context.Listings.Remove(listing);
//        await _context.SaveChangesAsync();

//        return NoContent();
//    }
//}