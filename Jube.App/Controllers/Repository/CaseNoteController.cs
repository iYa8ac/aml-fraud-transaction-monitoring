/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.
 *
 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
 * see <https://www.gnu.org/licenses/>.
 */

namespace Jube.App.Controllers.Repository
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using AutoMapper;
    using Case;
    using Code;
    using Data.Context;
    using Data.Poco;
    using Data.Repository;
    using Dto;
    using DynamicEnvironment;
    using FluentValidation;
    using log4net;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Newtonsoft.Json.Linq;
    using Validators;

    [Route("api/[controller]")]
    [Produces("application/json")]
    [Authorize]
    public class CaseNoteController : Controller
    {
        private readonly DbContext dbContext;
        private readonly DynamicEnvironment dynamicEnvironment;
        private readonly ILog log;
        private readonly IMapper mapper;
        private readonly PermissionValidation permissionValidation;
        private readonly CaseNoteRepository repository;
        private readonly string userName;
        private readonly IValidator<CaseNoteDto> validator;

        public CaseNoteController(ILog log,
            DynamicEnvironment dynamicEnvironment
            , IHttpContextAccessor httpContextAccessor)
        {
            if (httpContextAccessor.HttpContext?.User.Identity != null)
            {
                userName = httpContextAccessor.HttpContext.User.Identity.Name;
            }

            this.log = log;

            dbContext =
                DataConnectionDbContext.GetDbContextDataConnection(dynamicEnvironment.AppSettings("ConnectionString"));
            permissionValidation = new PermissionValidation(dbContext, userName);

            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<CaseNote, CaseNoteDto>();
                cfg.CreateMap<CaseNoteDto, CaseNote>();
            });

            mapper = new Mapper(config);
            repository = new CaseNoteRepository(dbContext, userName);
            validator = new CaseNoteDtoValidator();
            this.dynamicEnvironment = dynamicEnvironment;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                dbContext.Close();
                dbContext.Dispose();
            }

            base.Dispose(disposing);
        }

        [HttpPost]
        public async Task<ActionResult<CaseNoteDto>> InsertAsync([FromBody] CaseNoteDto model, CancellationToken token = default)
        {
            if (!permissionValidation.Validate(new[]
                {
                    1
                }))
            {
                return Forbid();
            }

            var results = await validator.ValidateAsync(model, token);

            if (!results.IsValid)
            {
                return Ok(await repository.InsertAsync(mapper.Map<CaseNote>(model), token));
            }

            var jObject = JObject.Parse(model.Payload);

            var values = new Dictionary<string, string>();
            foreach (var (key, value) in jObject)
            {
                if (value != null)
                {
                    values.Add(key, value.ToString());
                }
            }

            var caseWorkflowActionRepository = new CaseWorkflowActionRepository(dbContext, userName);

            var caseWorkflowAction = await caseWorkflowActionRepository.GetByIdActiveOnlyAsync(model.ActionId, token);

            if (caseWorkflowAction == null)
            {
                return Forbid();
            }

            if (caseWorkflowAction.EnableNotification != 1 && caseWorkflowAction.EnableHttpEndpoint != 1)
            {
                return Ok(await repository.InsertAsync(mapper.Map<CaseNote>(model), token));
            }

            if (caseWorkflowAction.EnableNotification == 1)
            {
                var notification = new Notification(log, dynamicEnvironment);
                await notification.SendAsync(caseWorkflowAction.NotificationTypeId ?? 1,
                    caseWorkflowAction.NotificationDestination,
                    caseWorkflowAction.NotificationSubject,
                    caseWorkflowAction.NotificationBody, values, token);
            }

            if (caseWorkflowAction.EnableHttpEndpoint != 1)
            {
                return Ok(await repository.InsertAsync(mapper.Map<CaseNote>(model), token));
            }

            if (caseWorkflowAction.HttpEndpointTypeId != null)
            {
                await SendHttpEndpoint.SendAsync(caseWorkflowAction.HttpEndpoint,
                    caseWorkflowAction.HttpEndpointTypeId.Value
                    , values);
            }

            return Ok(await repository.InsertAsync(mapper.Map<CaseNote>(model), token));
        }

        [HttpGet("ByCaseKeyValue")]
        public async Task<ActionResult<List<CaseNoteDto>>> GetByCaseKeyValueAsync(string key, string value, CancellationToken token = default)
        {
            try
            {
                if (!permissionValidation.Validate(new[]
                    {
                        1
                    }))
                {
                    return Forbid();
                }

                return Ok(mapper.Map<List<CaseNote>>(await repository.GetByCaseKeyValueAsync(key, value, token)));
            }
            catch (Exception e)
            {
                log.Error(e);
                return StatusCode(500);
            }
        }
    }
}
