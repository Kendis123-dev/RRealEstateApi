using Microsoft.AspNetCore.Mvc;
using RRealEstateApi.Services;
using RRealEstateApi.DTOs;
using RRealEstateApi.Models;
using RRealEstateApi.Data;
using Microsoft.EntityFrameworkCore;
using DocumentFormat.OpenXml.CustomProperties;

namespace RRealEstateApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PropertiesController : ControllerBase
    {
        private readonly RealEstateDbContext _context;
        private readonly IPropertyService _propertyService;

        public PropertiesController(RealEstateDbContext context, IPropertyService propertyService)
        {
            _context = context;
            _propertyService = propertyService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var totalCount = await _context.Properties.CountAsync();
                var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

                var properties = await _context.Properties
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var response = new
                {
                    currentPage = pageNumber,
                    pageSize = pageSize,
                    totalCount = totalCount,
                    totalPages = totalPages,
                    data = properties
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var property = await _context.Properties.FindAsync(id);
            if (property == null) return NotFound();
            return Ok(property);
        }

        [HttpPost]
        public async Task<IActionResult> Create(PropertyDto property)
        {
            var existingProperty = await _context.Properties.FirstOrDefaultAsync(p => p.Id == property.Id);
            if (existingProperty != null)
                return BadRequest(new { message = "Property with this ID already exists" });

            var agent = await _context.Agents.FirstOrDefaultAsync(a => a.Id == property.AgentId);
            if (agent == null)
                return BadRequest(new { message = "Agent not found" });

            var prop = new Property
            {
                Title = property.Title,
                Description = property.Description,
                Location = property.Location,
                Price = property.Price,
                ImageUrl = property.ImageUrl,
                PropertyType = property.PropertyType,
                PropertyValue = property.PropertyValue,
                DatePosted = property.DatePosted,
              //  AgentId = property.AgentId
            };

            _context.Properties.Add(prop);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(Get), new { id = property.Id }, property);
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchProperties([FromQuery] string location)
        {
            if (string.IsNullOrWhiteSpace(location))
                return BadRequest("Location is required");

            var properties = await _propertyService.SearchByLocationAsync(location);
            if (properties == null || !properties.Any())
                return NotFound("No properties found in this location");

            return Ok(properties);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, Property updated)
        {
            var property = await _context.Properties.FindAsync(id);
            if (property == null) return NotFound();

            property.Title = updated.Title;
            property.Description = updated.Description;
            property.Location = updated.Location;
            property.Price = updated.Price;
            property.PropertyType = updated.PropertyType;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var property = await _context.Properties.FindAsync(id);
            if (property == null) return NotFound();

            _context.Properties.Remove(property);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}