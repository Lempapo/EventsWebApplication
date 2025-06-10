using Microsoft.AspNetCore.Mvc;

namespace EventsWebApplication.Controllers;

[ApiController]
public class FilesController : ControllerBase
{
    [HttpPost("/files")]
    public async Task<IActionResult> UploadFile(IFormFile file)
    {
        if (file.Length == 0)
        {
            return BadRequest();
        }

        var filePath = Path.GetTempFileName();
        var newFilePath = Path.ChangeExtension(filePath, Path.GetExtension(file.FileName));

        await using (var stream = System.IO.File.Create(newFilePath))
        {
            await file.CopyToAsync(stream);
        }
        
        return Ok(file);  
    }
}