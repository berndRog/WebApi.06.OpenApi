using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WebApi.Core;
using WebApi.Core.DomainModel.Entities;

[assembly: InternalsVisibleTo("WebApiTest")]
[assembly: InternalsVisibleTo("WebApiTest.Controllers")] 
namespace WebApi.Persistence;

internal class OwnersRepository(
   DataContext dataContext
) : AGenericRepository<Owner>(dataContext), IOwnersRepository {

   public async Task<IEnumerable<Owner>> FilterByJoinAsync(
      Expression<Func<Owner, bool>>? predicate,
      bool joinAccounts,
      bool withTracking
   ) {
      // convert DbSet into an IQueryable
      IQueryable<Owner> query = _dbContext.Owners;
      
      // switch off tracking if not needed
      if(!withTracking)     query = query.AsNoTracking();
      
      // filter by predicate
      if(predicate != null) query = query.Where(predicate);
      
      // join accounts with owner
      if (joinAccounts)     query = query.Include(o => o.Accounts);
      
      // eager evaluation of results
      return await query.ToListAsync(); 
      
   }
}