﻿using Abp.Application.Services.Dto;

namespace PALMASoft.Authorization.Users.Dto
{
    public interface IGetUsersInput : ISortedResultRequest
    {
        string Filter { get; set; }

        string Permission { get; set; }

        int? Role { get; set; }

        bool OnlyLockedUsers { get; set; }
    }
}