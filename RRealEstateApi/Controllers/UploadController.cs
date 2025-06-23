
using Microsoft.AspNetCore.Mvc;
using RRealEstateApi.Data;
using RRealEstateApi.DTOs;
using RRealEstateApi.Models;

[ApiController]
[Route("api/[controller]")]
public class UploadController : ControllerBase
{
    private readonly IWebHostEnvironment _env;
    private readonly RealEstateDbContext _context;

    public UploadController(IWebHostEnvironment env, RealEstateDbContext context)
    {
        _env = env;
        _context = context;
    }

    [HttpPost("property-image")]
    public async Task<IActionResult> UploadPropertyImage([FromForm] UploadImageDto dto)
    {
        var property = await _context.Properties.FindAsync(dto.PropertyId);
        if (property == null) return NotFound("Property not found");

        var file = dto.File;
        if (file == null || file.Length == 0)
            return BadRequest("File is required");

        var ext = Path.GetExtension(file.FileName).ToLower();
        var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        if (!allowed.Contains(ext))
            return BadRequest("Invalid file type");

        var uploadsDir = Path.Combine(_env.WebRootPath ?? "wwwroot", "uploads");
        if (!Directory.Exists(uploadsDir)) Directory.CreateDirectory(uploadsDir);

        var fileName = Guid.NewGuid() + ext;
        var filePath = Path.Combine(uploadsDir, fileName);
        var fileUrl = $"/uploads/{fileName}"; // This is the relative URL to be used

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        // Save file info in a separate image table
        var image = new PropertyImage
        {
            PropertyId = dto.PropertyId,
            FileName = fileName,
            FileUrl = fileUrl
        };

        // Save relative URL in main Property record
        property.ImageUrl = fileUrl;

        _context.PropertyImages.Add(image);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Upload successful", imageUrl = fileUrl });
    }

    [HttpGet("image/{propertyId}")]
    public IActionResult GetPropertyImage(int propertyId)
    {
        var property = _context.Properties.FirstOrDefault(p => p.Id == propertyId);
        if (property == null || string.IsNullOrEmpty(property.ImageUrl))
            return NotFound(new { message = "Property image not found" });

        // Construct the full path from the relative URL
        var imagePath = Path.Combine(_env.WebRootPath ?? "wwwroot", property.ImageUrl.TrimStart('/'));

        if (!System.IO.File.Exists(imagePath))
        {
            return NotFound(new { message = "Image file does not exist", path = imagePath });
        }

        var imageStream = System.IO.File.OpenRead(imagePath);
        var mimeType = "image/jpeg"; // You could enhance this by detecting type from extension
        return File(imageStream, mimeType);
    }
}
