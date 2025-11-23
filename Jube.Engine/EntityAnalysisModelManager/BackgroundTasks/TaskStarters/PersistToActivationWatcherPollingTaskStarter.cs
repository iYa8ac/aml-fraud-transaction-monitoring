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

namespace Jube.Engine.EntityAnalysisModelManager.BackgroundTasks.TaskStarters
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using Context;
    using Data.Context;
    using Data.Poco;
    using Data.Repository;

    public class PersistToActivationWatcherPollingTaskStarter(Context context)
    {
        public async Task StartAsync()
        {
            try
            {
                var activationWatchers = new List<ActivationWatcher>();
                while (!context.Services.TaskCoordinator.CancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        context.ConcurrentQueues.PersistToActivationWatcher.TryDequeue(out var payload);

                        if (payload != null)
                        {
                            try
                            {
                                if (context.Services.Log.IsInfoEnabled)
                                {
                                    context.Services.Log.Info(
                                        "Database Activation Watcher Persist: a message has been received to be persisted to the Database database Activation Watcher.");
                                }

                                var dbContext =
                                    DataConnectionDbContext.GetDbContextDataConnection(
                                        context.Services.DynamicEnvironment.AppSettings("ConnectionString"));

                                activationWatchers.Add(payload);

                                if (context.Services.Log.IsInfoEnabled)
                                {
                                    context.Services.Log.Info(
                                        $"Database Persist: Added record to the data table pending SQL Bulk insert Tenant_Registry_ID {payload.TenantRegistryId},Symbol_Entity_Key {payload.Key},Longitude {payload.Longitude},Latitude {payload.Latitude}, Activation_Rule_Summary {payload.ActivationRuleSummary}, Response_Elevation_Content {payload.ResponseElevationContent}, Response_Elevation {payload.ResponseElevation}, Back_Color {payload.BackColor}, Fore_Color {payload.ForeColor}.");
                                }

                                if (context.Services.Log.IsInfoEnabled)
                                {
                                    context.Services.Log.Info(
                                        $"Database Activation Watcher Persist: The table count threshold has been set to {activationWatchers.Count} and the bulk copy threshold is {context.Services.DynamicEnvironment.AppSettings("ActivationWatcherBulkCopyThreshold")}.");
                                }

                                if (activationWatchers.Count <
                                    Int32.Parse(context.Services.DynamicEnvironment.AppSettings("ActivationWatcherBulkCopyThreshold")))
                                {
                                    continue;
                                }

                                var sw = new Stopwatch();
                                sw.Start();

                                if (context.Services.Log.IsInfoEnabled)
                                {
                                    context.Services.Log.Info(
                                        "Database Activation Watcher Persist: The bulk copy threshold has been exceeded and the SQL Bulk Copy will be executed. A timer has been started.");
                                }

                                if (context.Services.Log.IsInfoEnabled)
                                {
                                    context.Services.Log.Info("Database Activation Watcher Persist: Opened an SQL Bulk Collection.");
                                }

                                var repository = new ActivationWatcherRepository(dbContext);

                                if (context.Services.Log.IsInfoEnabled)
                                {
                                    context.Services.Log.Info("Database Activation Watcher Persist: Will proceed to bulk copy.");
                                }

                                try
                                {
                                    await repository.BulkCopyAsync(activationWatchers, context.Services.TaskCoordinator.CancellationToken).ConfigureAwait(false);

                                    if (context.Services.Log.IsInfoEnabled)
                                    {
                                        context.Services.Log.Info(
                                            $"Database Activation Watcher Persist: The bulk copy has inserted {activationWatchers.Count} Activation Watcher records and cleared the data table.  The time taken is {sw.ElapsedMilliseconds} in ms.");
                                    }

                                    sw.Reset();
                                }
                                catch (Exception ex) when (ex is not OperationCanceledException)
                                {
                                    context.Services.Log.Error(ex.ToString());

                                    sw.Reset();
                                }
                                finally
                                {
                                    activationWatchers.Clear();

                                    await dbContext.CloseAsync(context.Services.TaskCoordinator.CancellationToken).ConfigureAwait(false);
                                    await dbContext.DisposeAsync(context.Services.TaskCoordinator.CancellationToken).ConfigureAwait(false);

                                    if (context.Services.Log.IsInfoEnabled)
                                    {
                                        context.Services.Log.Info("Database Activation Watcher Persist: Closed an SQL Bulk Collection.");
                                    }
                                }
                            }
                            catch (Exception ex) when (ex is not OperationCanceledException)
                            {
                                context.Services.Log.Error($"Database Activation Watcher Persist: An error has occurred as {ex}.");
                            }
                        }
                        else
                        {
                            await Task.Delay(100, context.Services.TaskCoordinator.CancellationToken).ConfigureAwait(false);
                        }
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        context.Services.Log.Error($"Database Activation Watcher Persist: An error has occurred as {ex}.");
                    }
                }

                context.ConcurrentQueues.PersistToActivationWatcher.Clear();
            }
            catch (OperationCanceledException ex)
            {
                context.Services.Log.Info($"Graceful Cancellation PersistToActivationWatcherPollingAsync: has produced an error {ex}");
            }
            catch (Exception ex)
            {
                context.Services.Log.Error($"PersistToActivationWatcherPollingAsync: has produced an error {ex}");
            }
        }
    }
}
