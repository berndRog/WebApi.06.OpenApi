using AutoMapper;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Mime;
using System.Threading.Tasks;
using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using WebApi.Core;
using WebApi.Core.DomainModel.Entities;
using WebApi.Core.Dto;

namespace WebApi.Controllers; 

[ApiController]
[Route("banking/v{version:apiVersion}")]
[ApiVersion("1.0")]
[ApiVersion("2.0")]
public class AccountsController(
   IOwnersRepository ownersRepository,
   IAccountsRepository accountsRepository,
   IDataContext dataContext,
   IMapper mapper,
   ILogger<AccountsController> logger
) : ControllerBase {

   // http://localhost:5010/banking/owners/{ownerId:Guid}/accounts
   /// <summary>
   /// Get all accounts of a given ownerId
   /// </summary>
   /// <param name="ownerId"></param>
   /// <returns>IEnumerable{AccountDto} the accounts for the owner with given id</returns>
   [HttpGet("owners/{ownerId:guid}/accounts")]
   [Produces(MediaTypeNames.Application.Json)]
   [ProducesResponseType(StatusCodes.Status200OK)]
   [ProducesResponseType(StatusCodes.Status404NotFound)]
   public async Task<ActionResult<IEnumerable<AccountDto>>> GetAccountsByOwnerId(
      [FromRoute] Guid ownerId
   ) {
      logger.LogDebug("GetAccountsByOwner ownerId={ownerId}", ownerId);
      
      // get all accounts of a given owner
//    var accounts = await accountsRepository.SelectByOwnerIdAsync(ownerId);
      var accounts = await accountsRepository.FilterByAsync(a => a.OwnerId == ownerId);      
      // return accounts as Dtos
      var accountDtos = mapper.Map<IEnumerable<AccountDto>>(accounts);
      return Ok(accountDtos);  
   }

   // http://localhost:5010/banking/accounts/{id}
   /// <summary>
   /// Get account by Id
   /// </summary>
   /// <param name="id">Id to find</param>
   /// <returns>AccountDto?, i.e.found account or null</returns>
   [HttpGet("accounts/{id:guid}")]
   [Produces(MediaTypeNames.Application.Json)]
   [ProducesResponseType(StatusCodes.Status200OK)]
   [ProducesResponseType(StatusCodes.Status404NotFound)]
   [ProducesDefaultResponseType]
   public async Task<ActionResult<AccountDto?>> GetAccountById(
      [FromRoute] Guid id
   ) {
      logger.LogDebug("GetAccountById id={id}", id);

      return await accountsRepository.FindByIdAsync(id) switch {
         // return account as Dto
         { } account => Ok(mapper.Map<AccountDto>(account)),
         // return not found
         null => NotFound("Account with given Id not found")
      };
   }

   // http://localhost:5010/banking/accounts/iban?iban=abc
   /// <summary>
   /// Get account by iban
   /// </summary>
   /// <param name="iban">Iban to find</param>
   /// <returns>AccounDto?, i.e.found account or null</returns>
   [HttpGet("name")]
   [Produces(MediaTypeNames.Application.Json)]
   [ProducesResponseType(StatusCodes.Status200OK)]
   [ProducesResponseType(StatusCodes.Status404NotFound)]
   [ProducesDefaultResponseType]
   [HttpGet("accounts/iban")]
   public async Task<ActionResult<AccountDto?>> GetAccountByIban(
      [FromQuery] string iban
   ) {
      logger.LogDebug("GetAccountByIban iban={iban}", iban);

//                 accountsRepository.FindByIbanAsync(iban)) {
      return await accountsRepository.FindByAsync(a => a.Iban == iban) switch {
         // return account as Dto
         { } account => Ok(mapper.Map<AccountDto>(account)),
         // return not found
         null => NotFound("Account with given Id not found")
      };
   }

   // http://localhost:5010/banking/owner/{ownerId}/accounts
   /// <summary>
   /// Create a new account for a given owner 
   /// </summary>
   /// <param name="ownerId">Id of the owner</param>
   /// <param name="accountDto">Account data to create</param>
   /// <returns>AccountDto, i.e the created resource</returns>
   /// <response code="201">Created: Account created</response>
   /// <response code="400">BadRequest: OwnerId does not exist</response>
   /// <response code="409">Conflict: Account with given Id already exists</response>
   [HttpPost("owners/{ownerId:guid}/accounts")]
   [Consumes(MediaTypeNames.Application.Json)]
   [Produces(MediaTypeNames.Application.Json)]
   [ProducesResponseType(StatusCodes.Status201Created)]
   [ProducesResponseType(StatusCodes.Status400BadRequest)]
   [ProducesResponseType(StatusCodes.Status409Conflict)]
   [ProducesDefaultResponseType]
   public async Task<ActionResult<AccountDto>> CreateAccount(
      [FromRoute] Guid ownerId,
      [FromBody]  AccountDto accountDto
   ) {
      logger.LogDebug("CreateAccount iban={iban}", accountDto.Iban);
      
      // map Dto to DomainModel
//    var account = mapper.Map<Account>(accountDto);
      var account = new Account(accountDto);
      
      
      // check if ownerId exists
      var owner = await ownersRepository.FindByIdAsync(ownerId);
      if (owner == null)
         return BadRequest("Bad request: ownerId does't exists.");
      
      // check if account with given Id already exists   
      if(await accountsRepository.FindByIdAsync(account.Id) != null) 
         return Conflict("Account with given Id already exists");
      
      // update owner in DomainModel
      owner.Add(account);
      
      // add account to repository
      accountsRepository.Add(account); 
      // save to datastore
      await dataContext.SaveAllChangesAsync();
      
      // return created account as Dto
      var path = Request == null
         ? $"/banking/accounts/{account.Id}"
         : $"{Request.Path}";
      var uri = new Uri(path, UriKind.Relative);
      return Created(uri, mapper.Map<AccountDto>(account));     
   }

   // http://localhost:5100/banking/owner/{ownerId}/accounts
   /// <summary>
   /// Delete an account for a given owner
   /// </summary>
   /// <param name="ownerId">Id of the owner</param>
   /// <param name="id">Id of the account to delete</param>
   [HttpDelete("owners/{ownerId:guid}/accounts/{id:guid}")]
   [ProducesResponseType(StatusCodes.Status204NoContent)]
   [ProducesResponseType(StatusCodes.Status404NotFound)]
   [ProducesDefaultResponseType]
   public async Task<IActionResult> DeleteAccount(
      [FromRoute] Guid ownerId,
      [FromRoute] Guid id
   ) {
      logger.LogDebug("DeleteAccount ownerId={ownerId} id={id}", ownerId, id);
      
      // check if account with given Id already exists   
      Account? account = await accountsRepository.FindByIdAsync(id); 
      if(account == null)
         return NotFound("UpdateAccount: Account not found.");

      // remove the account from the owners account list is not necessary,
      // there is no datafield in the dabase referencing the account
      // i.e. the foreign key is the ownerId in the account table
      
      // save to repository 
      accountsRepository.Remove(account);
      
      // delete in database
      await dataContext.SaveAllChangesAsync();

      // return no content
      return NoContent(); 
   }
}