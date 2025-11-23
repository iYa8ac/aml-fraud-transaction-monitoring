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
    using EntityAnalysisModelInvoke;
    using EntityAnalysisModelInvoke.Exceptions;

    public class AsyncHttpContextCorrelationStarter(Context context)
    {
        public async Task StartAsync()
        {
            try
            {
                while (true)
                {
                    if (context.Services.TaskCoordinator.CancellationToken.IsCancellationRequested && context.ConcurrentQueues.PendingEntityInvoke.IsEmpty)
                    {
                        context.Services.Log.Info("Async Http Context Correlation: Cancellation requested and queue is empty. Exiting.");
                        break;
                    }

                    if (context.ConcurrentQueues.PendingEntityInvoke.TryDequeue(out var callbackContext))
                    {
                        if (context.Services.Log.IsInfoEnabled)
                        {
                            context.Services.Log.Info(
                                $"Async Http Context Correlation: Found Async with guid of {callbackContext.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid}. Is about to start.");
                        }

                        try
                        {
                            await EntityAnalysisModelInvoke.InvokeAsync(callbackContext).ConfigureAwait(false);

                            if (context.Services.Log.IsInfoEnabled)
                            {
                                context.Services.Log.Info(
                                    $"Async Http Context Correlation: Finished Async with guid of {callbackContext.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid}.");
                            }
                        }
                        catch (Exception ex) when (ex is not OperationCanceledException && ex is not ReferenceDateInFutureException)
                        {
                            context.Services.Log.Error($"Async Http Context Correlation: Error processing payload {ex}");
                        }
                    }
                    else
                    {
                        await Task.Delay(100, context.Services.TaskCoordinator.CancellationToken).ConfigureAwait(false);
                    }
                }
            }
            catch (OperationCanceledException ex)
            {
                context.Services.Log.Info($"Graceful Cancellation AsyncHttpContextCorrelationAsync: has produced an error {ex}");
            }
            catch (Exception ex)
            {
                context.Services.Log.Error($"AsyncHttpContextCorrelationAsync: has produced an error {ex}");
            }
        }
    }
}
