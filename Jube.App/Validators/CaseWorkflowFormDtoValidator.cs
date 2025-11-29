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

namespace Jube.App.Validators
{
    using System.Collections.Generic;
    using Data.Repository;
    using Dto;
    using FluentValidation;

    public class CaseWorkflowFormDtoValidator : AbstractValidator<CaseWorkflowFormDto>
    {
        public CaseWorkflowFormDtoValidator(CaseWorkflowFormRepository repository)
        {
            RuleFor(p => p.Name)
                .NotEmpty()
                .MustAsync(async (dto, name, cancellation) =>
                {
                    var existing = await repository.GetByNameCaseWorkflowIdAsync(name, dto.CaseWorkflowId, cancellation);

                    if (existing == null)
                    {
                        return true;
                    }

                    return existing.Id == dto.Id;
                })
                .WithMessage("This name already exists.");

            RuleFor(p => p.CaseWorkflowId).GreaterThan(0);
            RuleFor(p => p.Active).NotNull();
            RuleFor(p => p.Locked).NotNull();

            RuleFor(p => p.Html).NotEmpty();

            RuleFor(p => p.EnableHttpEndpoint).NotNull();
            RuleFor(p => p.EnableNotification).NotNull();

            var httpTypes = new List<int>
            {
                1,
                2
            };
            RuleFor(p => p.HttpEndpointTypeId)
                .Must(m => httpTypes.Contains(m))
                .When(w => w.EnableHttpEndpoint);

            RuleFor(p => p.HttpEndpoint)
                .NotEmpty()
                .When(w => w.EnableHttpEndpoint);

            var notificationTypes = new List<int>
            {
                1,
                2
            };
            RuleFor(p => p.NotificationTypeId)
                .Must(m => notificationTypes.Contains(m))
                .When(w => w.EnableNotification);

            RuleFor(p => p.NotificationDestination)
                .NotEmpty()
                .When(w => w.EnableNotification);
        }
    }
}
