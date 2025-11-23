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

namespace Jube.Engine.BackgroundTasks.TaskStarters
{
    using System;
    using System.Threading.Tasks;
    using Context;
    using Helpers;

    public class NotificationsViaConcurrentQueueStarter(Context context)
    {
        public async Task StartAsync()
        {
            try
            {
                if (context.Services.Log.IsDebugEnabled)
                {
                    context.Services.Log.Debug("Notification Relay From Concurrent Queue: Will poll for new notifications.");
                }

                while (true)
                {
                    if (context.Services.TaskCoordinator.CancellationToken.IsCancellationRequested && context.ConcurrentQueues.PendingNotifications.IsEmpty)
                    {
                        context.Services.Log.Debug("Notification Relay From Concurrent Queue: Cancellation requested and queue empty. Exiting.");
                        break;
                    }

                    if (context.ConcurrentQueues.PendingNotifications.TryDequeue(out var notification))
                    {
                        try
                        {
                            await DispatchNotificationHelpers.DispatchNotificationAsync(context, notification, context.Services.TaskCoordinator.CancellationToken).ConfigureAwait(false);
                        }
                        catch (Exception ex) when (ex is not OperationCanceledException)
                        {
                            context.Services.Log.Error($"Notification Relay From Concurrent Queue: Error dispatching notification {ex}");
                        }
                    }
                    else
                    {
                        if (context.Services.Log.IsDebugEnabled)
                        {
                            context.Services.Log.Debug("Notification Relay From Concurrent Queue: Nothing to relay. Waiting before trying again.");
                        }

                        await Task.Delay(1000, context.Services.TaskCoordinator.CancellationToken).ConfigureAwait(false);
                    }
                }
            }
            catch (OperationCanceledException ex)
            {
                context.Services.Log.Info($"Graceful Cancellation NotificationRelayFromConcurrentQueueAsync: has produced an error {ex}");
            }
            catch (Exception ex)
            {
                context.Services.Log.Error($"NotificationRelayFromConcurrentQueueAsync: has produced an error {ex}");
            }
        }
    }
}
