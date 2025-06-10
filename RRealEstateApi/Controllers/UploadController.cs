using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

        var fileName = Guid.NewGuid().ToString() + ext;
        var filePath = Path.Combine(uploadsDir, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var image = new PropertyImage
        {
            PropertyId = dto.PropertyId,
            FileName = fileName,
            FileUrl = $"/uploads/{fileName}"
        };
        property.ImageUrl = filePath;
        _context.PropertyImages.Add(image);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Upload successful", imageUrl = image.FileUrl });
    }

    [HttpGet("image/{propertyId}")]
    public IActionResult GetPropertyImage(int propertyId)
    {
        var propertyImage = _context.Properties.FirstOrDefault(p => p.Id == propertyId);
        if (propertyImage == null || string.IsNullOrEmpty(propertyImage.ImageUrl))
        {
            return NotFound(new { message = "Property image not found" });
        }

        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");
        var imagePath = propertyImage.ImageUrl;

        if (!System.IO.File.Exists(imagePath))
        {
            return NotFound(new { message = "Image file does not exist", path = imagePath });
        }

        var imageFileStream = System.IO.File.OpenRead(imagePath);
        return File(imageFileStream, "image/jpeg"); // Or use "image/png" if you save PNGs
    }
}