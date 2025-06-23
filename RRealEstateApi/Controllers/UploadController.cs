
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
        var property = _context.Properties.FirstOrDefault(p => p.Id == propertyId);
        if (property == null || string.IsNullOrWhiteSpace(property.ImageUrl))
        {
            return NotFound(new { message = "Property image not found" });

        // Construct the full path from the relative URL
        var imagePath = Path.Combine(_env.WebRootPath ?? "wwwroot", property.ImageUrl.TrimStart('/'));
        // Get only the filename, drop any directory parts from DB
        var fileName = Path.GetFileName(property.ImageUrl);

        var uploadsDir = Path.Combine(_env.WebRootPath ?? "wwwroot", "uploads");
        var filePath = Path.Combine(uploadsDir, fileName);

        if (!System.IO.File.Exists(filePath))
        {
            return NotFound(new { message = "Image file does not exist", path = filePath });
        }

        var stream = System.IO.File.OpenRead(filePath);
        var contentType = GetContentType(filePath);
        return File(stream, contentType);
    }

        var imageStream = System.IO.File.OpenRead(imagePath);
        var mimeType = "image/jpeg"; // You could enhance this by detecting type from extension
        return File(imageStream, mimeType);
    private string GetContentType(string path)
    {
        var extension = Path.GetExtension(path).ToLowerInvariant();
        return extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            _ => "application/octet-stream",
        };
    }
}

}