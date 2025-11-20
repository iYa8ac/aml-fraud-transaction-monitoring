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

namespace Jube.App.Code.WatcherDispatch
{
    using System.Threading.Tasks;
    using DynamicEnvironment;
    using log4net;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.SignalR;
    using Microsoft.Extensions.DependencyInjection;
    using Newtonsoft.Json.Serialization;
    using RabbitMQ.Client;
    using signalr;
    using TaskCancellation;

    public static class RelayExtension
    {
        public static async Task StartRelayAsync(this IApplicationBuilder app)
        {
            using var scope = app.ApplicationServices.CreateScope();

            var relay = scope.ServiceProvider.GetService<Relay>();

            if (relay == null)
            {
                return;
            }

            var taskCoordinator = scope.ServiceProvider.GetService<TaskCoordinator>();
            var hub = scope.ServiceProvider.GetService<IHubContext<WatcherHub>>();
            var environment = scope.ServiceProvider.GetService<DynamicEnvironment>();
            var contractResolver = scope.ServiceProvider.GetService<DefaultContractResolver>();
            var log = scope.ServiceProvider.GetService<ILog>();
            var rabbitMqConnection = scope.ServiceProvider.GetService<IConnection>();

            await relay.StartAsync(hub, environment, log, rabbitMqConnection, contractResolver, taskCoordinator).ConfigureAwait(false);
        }
    }
}
