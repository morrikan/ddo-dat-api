using DdoDatApi.Caching;
using DdoDatApi.Models;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
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

    /// <summary>
    /// Fetches the ID ranges of common DAT object types. Generally, a given object type only has 1 home. Exceptions are Map/Cell being split by region, and graphics/images spanning surface, highres, and local_english.
    /// </summary>
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
    [ProducesResponseType((int)HttpStatusCode.Accepted, Description = "Rebuilding the cache runs in a background worker service.")]
    [ProducesResponseType((int)HttpStatusCode.AlreadyReported, Description = "A cache rebuild is already in progress.")]
    public IActionResult RebuildCache()
    {
        if (cacheBuilderService.IsRunning)
            return new StatusCodeResult((int)HttpStatusCode.AlreadyReported);

        cacheBuilderService.Export();

        return new StatusCodeResult((int)HttpStatusCode.Accepted);
    }

    /// <summary>
    /// Gets basic timestamp / version / size of the Index. Useful for determining if/when the Index needs to be rebuilt.
    /// </summary>
    [HttpGet("Index/Metadata")]
    [ProducesResponseType<IndexMetadata>((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NotFound, Description = "Returned with index cache has not been built yet.")]
    public IActionResult IndexMetadata()
    {
        if (DatCache.Index.CompiledOnUtc == null)
            return new NotFoundResult();

        var fileInfo = new FileInfo(IndexLoader.CachePath);

        var md = new IndexMetadata()
        {
            ClientVersion = DatCache.Index.ClientVersion,
            CompiledOnUtc = DatCache.Index.CompiledOnUtc,
            CompilationDuration = DatCache.Index.LastIndexDuration,
            SizeInBytes = fileInfo.Length
        };
        return new OkObjectResult(md);
    }

    /// <summary>
    /// Allows the consumer to download the entire Index. As of time of writing, this index was ~22MB, and is expected to get larger.
    /// </summary>
    [HttpGet("Index")]
    [ProducesResponseType<FileContentResult>((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NotFound, Description = "Returned with index cache has not been built yet.")]
    public IActionResult DownloadIndex()
    {
        if (DatCache.Index.CompiledOnUtc == null)
            return new NotFoundResult();

        var stream = new FileStream(IndexLoader.CachePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return File(stream, "application/json", "indexcache.json");
    }
}
