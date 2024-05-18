﻿using System.Runtime.CompilerServices;
using AutoMapper;
using WebApi.Core.DomainModel.Entities;
using WebApi.Core.Dto;

[assembly: InternalsVisibleTo("WebApiTest")]
[assembly: InternalsVisibleTo("WebApiTest.Core")]
[assembly: InternalsVisibleTo("WebApiTest.Controllers")]
namespace WebApi.Core.Mapping;
internal class MappingProfile : Profile {
   public MappingProfile() {
      //        Source Destination
      CreateMap<Owner, OwnerDto>();
      CreateMap<OwnerDto, Owner>()
         .ForMember(m => m.Accounts, options => options.Ignore());

      CreateMap<Account, AccountDto>();
      CreateMap<AccountDto, Account>()
         .ForMember(m => m.Owner, options => options.Ignore());
   }
}