using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventsWebApplication.Controllers;

[ApiController]
public class FilesController : ControllerBase
{
    [HttpPost("/files")]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<IActionResult> UploadFile(IFormFile file)
    {
        if (file.Length == 0)
        {
            return BadRequest();
        }
        
        var uploadsDirectory = Path.Combine(Directory.GetCurrentDirectory(), "uploads");

        if (!Directory.Exists(uploadsDirectory))
        {
            Directory.CreateDirectory(uploadsDirectory);
        }
        
        var fileExtension = Path.GetExtension(file.FileName);
        var fileName = $"{Guid.NewGuid()}{fileExtension}";
        var filePath = Path.Combine(uploadsDirectory, fileName);

        await using (var stream = System.IO.File.Create(filePath))
        {
            await file.CopyToAsync(stream);
        }
        
        return Ok(fileName);  
    }

    [HttpGet("/files/{fileId}")]
    public async Task<IActionResult> GetFile(string fileId)
    {
        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "uploads", fileId);
        
        if (!System.IO.File.Exists(filePath))
        {
            return NotFound();
        }
        
        var fileContent = await System.IO.File.ReadAllBytesAsync(filePath);
        var contentType = "application/octet-stream";
        
        return File(fileContent, contentType, fileId);
    }
}