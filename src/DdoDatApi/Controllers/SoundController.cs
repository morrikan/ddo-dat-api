using Microsoft.AspNetCore.Mvc;
using System;
using System.Globalization;
using System.Net;

namespace DdoDatApi.Controllers;

[ApiController]
[Route("[controller]")]
public class SoundController : ControllerBase
{
    /// <summary>
    /// Sounds have a 0x2A****** file in client_general that gives more information about the actual sound bit.
    /// </summary>
    /// <param name="id">Id of the sound to fetch. This should match the IdRange for "SoundInfo", which is 0x2A000000 - 0x2AFFFFFF.</param>
    [HttpGet("{id}")]
    [ProducesResponseType<FileContentResult>((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest, Description = "Malformed Id, or when the SDK throws an Argument exception, which usually means a bad ID was provided.")]
    public IActionResult Index(string id)
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

        try
        {
            var bytes = DatSource.SoundExporter.GetSoundData(datId, out string ext);
            if (bytes == null || bytes.Length < 1) return new NotFoundResult();
            // ext is (so far) only ever ".ogg" or ".wav"
            var contentType = $"audio/{ext?.TrimStart('.') ?? "octet-stream"}";
            return new FileContentResult(bytes, contentType);
        }
        catch (ArgumentException)
        {
            return new StatusCodeResult((int)HttpStatusCode.BadRequest);
        }
    }
}
