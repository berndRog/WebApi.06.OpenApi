using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WebApi.Core;
using WebApi.Core.DomainModel.Entities;
[assembly: InternalsVisibleToAttribute("WebApiTest")]
[assembly: InternalsVisibleToAttribute("WebApiTest")]
[assembly: InternalsVisibleToAttribute("WebApiTest.Controllers")] 
 
namespace WebApi.Persistence;
internal  class AccountsRepository(
   DataContext dataContext
) : AGenericRepository<Account>(dataContext), IAccountsRepository {
   
   public async Task<IEnumerable<Account>> SelectByOwnerIdJoinAsync(
      Guid ownerId,
      bool joinAccounts = false,
      bool withTracking = false
   ) {
      // convert DbSet into an IQueryable
      IQueryable<Account> query = _dbContext.Accounts;
      
      // switch off tracking if not needed
      if(!withTracking) query = query.AsNoTracking();
      
      // find accounts by ownerId
      query = query.Where(a => a.OwnerId == ownerId);
      
      // join accounts with owner
      if(joinAccounts) query = query.Include(a => a.Owner);
      
      // eager evaluation of results
      return await query.ToListAsync();
      
      // lazy evaluation, tolistAsync() is called in the service layer
      // return query;
   }
}
