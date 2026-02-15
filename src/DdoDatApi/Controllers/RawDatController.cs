using DdoDatApi.Caching;
using DdoDatApi.Models;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Globalization;
using System.Net;

namespace DdoDatApi.Controllers;

[ApiController]
[Route("[controller]")]
public class RawDatController(ICacheBuilderService cacheBuilderService) : ControllerBase
{
    [HttpGet("{dat}/{id}")]
    [ProducesResponseType<FileContentResult>((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    public IActionResult Get(DatFileType dat, string id)
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

        var contents = (dat switch
        {
            DatFileType.Anim => DatSource.AnimDat,
            DatFileType.Cell1 => DatSource.Cell1,
            DatFileType.Cell2 => DatSource.Cell2,
            DatFileType.Cell3 => DatSource.Cell3,
            DatFileType.Cell4 => DatSource.Cell4,
            DatFileType.GameLogic => DatSource.GameLogicDat,
            DatFileType.General => DatSource.GeneralDat,
            DatFileType.Highres => DatSource.Highres,
            DatFileType.LocalEnglish => DatSource.LocalEnglishDat,
            DatFileType.Map1 => DatSource.Map1,
            DatFileType.Map2 => DatSource.Map2,
            DatFileType.Map3 => DatSource.Map3,
            DatFileType.Map4 => DatSource.Map4,
            DatFileType.Mesh => DatSource.Mesh,
            DatFileType.Sound => DatSource.SoundDat,
            DatFileType.Surface => DatSource.SurfaceDat,
            _ => null

        })?.GetFileContents(datId);

        if (contents == null) return new NotFoundResult();
        return File(contents, "application/octet-stream");
    }

    [HttpGet("IdRanges")]
    [ProducesResponseType<List<DatIdRange>>((int)HttpStatusCode.OK)]
    public IActionResult IdRanges()
    {
        return new OkObjectResult(DatSource.IdRanges);
    }

    /// <summary>
    /// Kicks off a long-running process to rebuild the cache. Should only really be done after patching
    /// the client or after rebuilding the WebApi.
    /// </summary>
    [HttpPost("RebuildCache")]
    [ProducesResponseType((int)HttpStatusCode.Accepted)]
    public IActionResult RebuildCache()
    {
        cacheBuilderService.Export();

        return new StatusCodeResult((int)HttpStatusCode.Accepted);
    }
}
