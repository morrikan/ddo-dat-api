using Microsoft.AspNetCore.Mvc;
using System.Globalization;

namespace DdoDatApi.Controllers;

/// <summary>
/// Loads images from dat files known to contain images.
/// </summary>
[ApiController]
[Route("[controller]")]
public class ImageController : Controller
{
    /// <summary>
    /// gets a singular image from the SDK's ImageExporter. Does not load from the highres dat.
    /// </summary>
    /// <param name="id">the Id to get. Hexadecimal is best and should have a "0x" prefix. Also supports integers (without "0x"), if need be. Values must be in the range
    /// of a UINT, from 0x78000000 to 0x79FFFFFF</param>
    /// <returns></returns>
    [HttpGet("{id}")]
    [ProducesResponseType<FileContentResult>(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public IActionResult Get(string id)
    {
        uint datId = 0;
        if (string.IsNullOrWhiteSpace(id))
            return BadRequest();

        if (id.StartsWith("0x"))
        {
            id = id.Substring(2);
            if (!uint.TryParse(id, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out datId))
                return new BadRequestObjectResult("Could not parse the ID provided.");
        }
        else
            if (!uint.TryParse(id, out datId))
            return new BadRequestObjectResult("Could not parse the ID provided.");

        var imgBytes = DatSource.ImageExporter.GetPngImageBytes(datId);
        if (imgBytes == null || imgBytes.Length < 1) return new NotFoundResult();

        return File(imgBytes, "image/png");
    }
}
