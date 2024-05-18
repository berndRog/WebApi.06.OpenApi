﻿using System;
using System.Collections.Generic;
using WebApi.Core.DomainModel.NullEntities;
namespace WebApi.Core.DomainModel.Entities; 

public class Owner: AEntity {
   
   #region properties
   public override Guid Id  { get; init; } = Guid.NewGuid();
   public string   Name     { get; set; } = string.Empty;
   public DateTime Birthdate{ get; init; } = DateTime.UtcNow;
   public string   Email    { get; set; } = string.Empty;

   // Navigation property
   // One-to-many relationship Owner -> Account
   public ICollection<Account> Accounts { get; init; } = new List<Account>();
   #endregion

   #region methods
   public void Update (string name, string email) {
      Name = name;
      Email = email;
   }
   
   public void Add(Account account) {
      if (account.OwnerId != NullOwner.Instance.Id && account.OwnerId != Id)
         throw new ApplicationException("Account is already asigned to another owner");
      account.Owner = this;
      account.OwnerId = Id;
      Accounts.Add(account);
   }
   #endregion
}
