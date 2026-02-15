using DdoDatApi.Caching;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using VoK.Sdk.Properties;

namespace DdoDatApi.Controllers;

/// <summary>
/// Handles loading 0x70* / 0x79* objects from client_gamelogic.dat.
/// </summary>
[ApiController]
[Route("[controller]")]
public class DbPropertiesController : ControllerBase
{
    /// <summary>
    /// Gets a DbProperties collection object. If the ID provided starts with 0x70******, it will be modified
    /// to be 0x79******.
    /// </summary>
    /// <param name="id">the Id to get. Hexadecimal is best and should have a "0x" prefix. Also supports integers (without "0x"), if need be. Values must be in the range
    /// of a UINT, from 0x78000000 to 0x79FFFFFF</param>
    /// <returns>The DBProperties object from client_gamelogic.dat</returns>
    [HttpGet("{id}")]
    [ProducesResponseType<IPropertyCollection>((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.NotFound)]
    [ProducesResponseType((int)HttpStatusCode.BadRequest)]
    public ActionResult<IPropertyCollection> Get(string id)
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

        if (datId >= 0x70000000 && datId < 0x71000000)
            datId += 0x09000000;

        if (datId < 0x78000000 || datId > 0x79FFFFFF)
            return new BadRequestObjectResult("ID provided not within the valid range");

        var result = DatSource.PropertyMaster.GetPropertyCollection(datId);
        if (result == null) return new NotFoundResult();

        return new OkObjectResult(result);
    }

    [HttpGet("IdsForName")]
    [ProducesResponseType<List<uint>>((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.FailedDependency, Description = "Happens when the index data has not been loaded")]
    public IActionResult GetByName([FromQuery] string name)
    {
        if (DatCache.Index.Names.Count < 1)
            return new StatusCodeResult((int)HttpStatusCode.FailedDependency);

        if (DatCache.Index.Names.ContainsKey(name))
            return new OkObjectResult(DatCache.Index.Names[name]);

        return new OkObjectResult(new List<uint>());
    }
}
