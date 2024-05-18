using AutoMapper;
using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Net.Mime;
using System.Threading.Tasks;
using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using WebApi.Core;
using WebApi.Core.DomainModel.Entities;
using WebApi.Core.Dto;
using WebApi.Core.Misc;

namespace WebApi.Controllers; 
[Route("banking/v{version:apiVersion}/owners")]
[ApiVersion("1.0")]
[ApiController]
public class OwnersController(
   // Dependency injection
   IOwnersRepository ownersRepository,
   IDataContext dataContext,
   IMapper mapper,
   ILogger<OwnersController> logger
) : ControllerBase {
   
   // http://localhost:5100/banking/owners
   /// <summary>
   /// Get all owners
   /// </summary>
   /// <returns>IEnumerable{OwnerDto} the owners</returns>
   /// <response code="200">Ok: Owners returned</response>
   [HttpGet("")]
   [Produces(MediaTypeNames.Application.Json)]
   [ProducesResponseType(StatusCodes.Status200OK)]
   [ProducesDefaultResponseType]
   public async Task<ActionResult<IEnumerable<OwnerDto>>> GetOwners() {
      logger.LogDebug("GetOwners()");
      
      // get all owners
      var owners = await ownersRepository.SelectAsync();
      
      // return owners as Dtos
      var ownerDtos = mapper.Map<IEnumerable<OwnerDto>>(owners);
      return Ok(ownerDtos);  
   }
   
   // http://localhost:5100/banking/owners/{id}
   /// <summary>
   /// Get owner by Id
   /// </summary>
   /// <param name="id">Id to find</param>
   /// <returns>OwnerDto?, i.e.found owner or null</returns>
   [HttpGet("{id:guid}")]
   [Produces(MediaTypeNames.Application.Json)]
   [ProducesResponseType(StatusCodes.Status200OK)]
   [ProducesResponseType(StatusCodes.Status404NotFound)]
   [ProducesDefaultResponseType]
   public async Task<ActionResult<OwnerDto?>> GetOwnerById(
      [FromRoute] Guid id
   ) {
      logger.LogDebug("GetOwnerById() id={id}", id.As8());
      return await ownersRepository.FindByIdAsync(id) switch {
         // return owner as Dto
         { } owner => Ok(mapper.Map<OwnerDto>(owner)),
         // return not found
         null => NotFound("Owner with given Id not found")
      };
   }

   // http://localhost:5100/banking/owners/name?name=abc
   /// <summary>
   /// Get owner by name
   /// </summary>
   /// <param name="name">Name to find</param>
   /// <returns>OwnerDto?, i.e.found owner or null</returns>
   [HttpGet("name")]
   [Produces(MediaTypeNames.Application.Json)]
   [ProducesResponseType(StatusCodes.Status200OK)]
   [ProducesResponseType(StatusCodes.Status404NotFound)]
   [ProducesDefaultResponseType]
   public async Task<ActionResult<OwnerDto?>> GetOwnerByName(
      [FromQuery] string name
   ) {
      logger.LogDebug("GetOwnerByName() name={name}", name);
      
      //     await ownersRepository.SelectByNameAsync(name)) switch {
      return await ownersRepository.FindByAsync(o => o.Name == name) switch {
        // return owners as Dtos
         { } owner => Ok(mapper.Map<OwnerDto>(owner)),
         // return not found
         null => NotFound("Owner with given name not found")
      };
   }

   // http://localhost:5100/banking/owners/email?email=abc
   /// <summary>
   /// Get owner by email
   /// </summary>
   /// <param name="email">E-Mail to find</param>
   /// <returns>OwnerDto?, i.e.found owner or null</returns>
   [HttpGet("email")]
   [Produces(MediaTypeNames.Application.Json)]
   [ProducesResponseType(StatusCodes.Status200OK)]
   [ProducesResponseType(StatusCodes.Status404NotFound)]
   [ProducesDefaultResponseType]
   public async Task<ActionResult<OwnerDto?>> GetOwnerByEmail(
      [FromQuery] string email
   ) {
      logger.LogDebug("GetOwnersByName() email={email}", email);
      
      return await ownersRepository.FindByAsync(o => o.Email == email) switch {
         // return owner as Dto
         { } owner => Ok(mapper.Map<OwnerDto>(owner)),
         // return not found
         null => NotFound("Owner with given email not found")
      };
   }
   
   // http://localhost:5100/banking/owners/birthdate/?from=yyyy-MM-dd&to=yyyy-MM-dd
   /// <summary>
   /// Get owners by birthdate
   /// </summary>
   /// <param name="from">Birthdate from</param>
   /// <param name="to">Birthdate to</param>
   /// <returns>IEnumerable{OwnerDto} the owners</returns>
   [HttpGet("birthdate")]
   [Produces(MediaTypeNames.Application.Json)]
   [ProducesResponseType(StatusCodes.Status200OK)]
   [ProducesDefaultResponseType]
   public async Task<ActionResult<IEnumerable<OwnerDto>>> GetOwnersByBirthdate(
      [FromQuery] string from,   // Date must be in the format yyyy-MM-dd
                                 // MM = 01 for January through 12 for December
      [FromQuery] string to      
   ) {
      logger.LogDebug("GetOwnersByBirthdate() from={from} to={to}", from, to);

      // Convert string to DateTime
      var (errorFrom, dateFrom) = ConvertToDateTime(from);
      if(errorFrom) 
         return BadRequest($"GetOwnersByBirthdate: Invalid date 'from': {from}");
      var (errorTo, dateTo) = ConvertToDateTime(to);
      if(errorTo) 
         return BadRequest($"GetOwnersByBirthdate: Invalid date 'to': {to}");

      // Get owners by birthdate
//    var owners = await ownersRepository.SelectByBirthDateAsync(dateFrom, dateTo);   
      var owners = await ownersRepository.FilterByAsync(o => 
         o.Birthdate >= dateFrom && o.Birthdate <= dateTo);   
      
      // return owners as Dtos
      return Ok(mapper.Map<IEnumerable<OwnerDto>>(owners));
   }
   
   // Convert string in German format dd.MM.yyyy to DateTime
   private static (bool, DateTime) ConvertToDateTime(string date) {
      try {
         var dateTime = DateTime.ParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture);
         return (false, dateTime );
      } catch {
         return (true, DateTime.MinValue);
      }
   }
   
   // http://localhost:5100/banking/owners
   /// <summary>
   /// Create an owner
   /// </summary>
   /// <param name="ownerDto">Owner data to create</param>
   /// <returns>OwnerDto, i.e the created resource</returns>
   /// <response code="201">Created: Owner created</response>
   /// <response code="400">Bad request: Id in route and body do not match</response>
   /// <response code="409">Conflict: Owner with given id already exists</response>
   /// <response code="500">Server internal error</response>
   [HttpPost("")]
   [Consumes(MediaTypeNames.Application.Json)]
   [Produces(MediaTypeNames.Application.Json)]
   [ProducesResponseType(StatusCodes.Status201Created)]
   [ProducesResponseType(StatusCodes.Status409Conflict)]
   [ProducesDefaultResponseType]
   public async Task<ActionResult<OwnerDto>> CreateOwner(
      [FromBody] OwnerDto ownerDto
   ) {
      logger.LogDebug("CreateOwner() ownerDto={ownerDto}", ownerDto.Name);
      
      // map ownerDto to owner
      var owner = mapper.Map<Owner>(ownerDto);
      
      // check if owner with given Id already exists   
      if(await ownersRepository.FindByIdAsync(owner.Id) != null) 
         return Conflict("CreateOwner: Owner with the given id already exists");
      
      // add owner to repository
      ownersRepository.Add(owner); 
      // save to datastore
      await dataContext.SaveAllChangesAsync();
      
      // return created account as Dto
      var path = Request == null
         ? $"/banking/owners/{owner.Id}"
         : $"{Request.Path}";
      var uri = new Uri(path, UriKind.Relative);return Created(uri, mapper.Map<OwnerDto>(owner));     
   }
   
   // Update owner
   // http://localhost:5100/banking/owners/{id}
   /// <summary>
   /// Update an owner
   /// </summary>
   /// <param name="id">Id of the owner to update</param>
   /// <param name="updOwnerDto">Owner data to update name or email</param>
   /// <returns>OwnerDto, i.e. the updated owner</returns>
   /// <response code="200">Ok: Owner updated</response>
   /// <response code="400">Bad request: Id in route and body do not match</response>
   /// <response code="404">Not found: Owner with given id not found</response>
   /// <response code="500">Server internal error</response>
   [HttpPut("{id:guid}")] 
   [Consumes(MediaTypeNames.Application.Json)]
   [Produces(MediaTypeNames.Application.Json)]
   [ProducesResponseType(StatusCodes.Status200OK)]
   [ProducesResponseType(StatusCodes.Status400BadRequest)]
   [ProducesResponseType(StatusCodes.Status404NotFound)]
   [ProducesDefaultResponseType]
   public async Task<ActionResult<OwnerDto>> UpdateOwner(
      [FromRoute] Guid id,
      [FromBody]  OwnerDto updOwnerDto
   ) {
      logger.LogDebug("UpdateOwner() id={id} updOwnerDto={updOwnerDto}", id.As8(), updOwnerDto.Name);
      
      var updOwner = mapper.Map<Owner>(updOwnerDto);

      // check if Id in the route and body match
      if(id != updOwner.Id) 
         return BadRequest("UpdateOwner: Id in the route and body do not match.");
      
      // check if owner with given Id exists
      var owner = await ownersRepository.FindByIdAsync(id);
      if (owner == null)
         return NotFound("UpdateOwner: Owner with given id not found.");

      // Update person
      owner.Update(updOwner.Name, updOwner.Email);
      
      // save to repository 
      await ownersRepository.UpdateAsync(owner);
      // write to database
      await dataContext.SaveAllChangesAsync();

      // return updated owner
      return Ok(mapper.Map<OwnerDto>(updOwner)); 
   }
   
   // http://localhost:5100/banking/owners/{id}
   /// <summary>
   /// Delete owner
   /// </summary>
   /// <param name="id">Id of the owner to delete</param>
   [HttpDelete("{id:guid}")] 
   [ProducesResponseType(StatusCodes.Status204NoContent)]
   [ProducesResponseType(StatusCodes.Status404NotFound)]
   [ProducesDefaultResponseType]
   public async Task<IActionResult> DeleteOwner(
      [FromRoute] Guid id
   ) {
      logger.LogDebug("DeleteOwner {id}", id.As8());
      
      // check if owner with given Id exists
      Owner? owner = await ownersRepository.FindByIdAsync(id);
      if (owner == null)
         return NotFound("DeleteOwner: Owner with given id not found.");

      // remove in repository 
      ownersRepository.Remove(owner);
      // write to database
      await dataContext.SaveAllChangesAsync();

      // return no content
      return NoContent(); 
   }
}