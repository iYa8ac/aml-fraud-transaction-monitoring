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

namespace Jube.Engine.EntityAnalysisModelInvoke.Context.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;
    using Cache;
    using Cache.Redis.Models;
    using EntityAnalysisModelManager.EntityAnalysisModel.Models.Models;
    using Sanctions;
    using Sanctions.Models;
    using TaskCancellation.TaskHelper;

    public static class SanctionsExtensions
    {
        public static async Task<Context> ExecuteSanctionsAsync(this Context context)
        {
            await IterateAndProcessAsync(context, context.EntityAnalysisModel.Services.CacheService).ConfigureAwait(false);
            StorePerformanceFromStopwatch(context);

            return context;
        }

        private static void StorePerformanceFromStopwatch(Context context)
        {

            context.EntityAnalysisModelInstanceEntryPayload.InvokeTaskPerformance.ComputeTimes.SanctionsAsync =
                (int)(context.Stopwatch.ElapsedTicks * 1000000 / Stopwatch.Frequency);

            if (context.Log.IsInfoEnabled)
            {
                context.Log.Info(
                    $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} has finished sanctions processing.");
            }
        }

        private static async Task IterateAndProcessAsync(Context context, CacheService cacheService)
        {

            if (context.Log.IsInfoEnabled)
            {
                context.Log.Info(
                    $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} is starting Sanctions processing.");
            }

            foreach (var entityAnalysisModelSanction in context.EntityAnalysisModel.Collections.EntityAnalysisModelSanctions)
            {
                if (context.Log.IsInfoEnabled)
                {
                    context.Log.Info(
                        $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} is evaluating Sanctions {entityAnalysisModelSanction.Name}.");
                }

                try
                {
                    if (context.EntityAnalysisModelInstanceEntryPayload.Payload.ContainsKey(entityAnalysisModelSanction.MultipartStringDataName))
                    {
                        if (context.Log.IsInfoEnabled)
                        {
                            context.Log.Info(
                                $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} is about to look for Sanctions Match in the Cache.");
                        }
                        else
                        {
                            var multiPartStringValue = context.EntityAnalysisModelInstanceEntryPayload.Payload
                                [entityAnalysisModelSanction.MultipartStringDataName].AsString();

                            var sanction = await LookupFromCacheAsync(context, cacheService, multiPartStringValue, entityAnalysisModelSanction).ConfigureAwait(false);
                            var foundCacheSanctionsAndNotExpired = false;

                            if (sanction != null)
                            {
                                foundCacheSanctionsAndNotExpired = TestIfSanctionHasExpiredAndFound(context, sanction, multiPartStringValue, entityAnalysisModelSanction);
                            }
                            else
                            {
                                if (context.Log.IsInfoEnabled)
                                {
                                    context.Log.Info(
                                        $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} has extracted multi part string name value as {multiPartStringValue} cache is not available.");
                                }
                            }

                            if (foundCacheSanctionsAndNotExpired)
                            {
                                if (sanction.Value.HasValue)
                                {
                                    AddToResponses(context, entityAnalysisModelSanction, sanction.Value.Value, multiPartStringValue);

                                    context.Log.Info(
                                        $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} has extracted multi part string name value as {multiPartStringValue} from cache and is returning.");

                                    return;
                                }

                                context.Log.Info(
                                    $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} has extracted multi part string name value as {multiPartStringValue} from cache but the value is null.");
                            }

                            var averageLevenshteinDistance = CalculateSanctionAndUpsertCache(context, cacheService,
                                entityAnalysisModelSanction, multiPartStringValue);

                            if (!averageLevenshteinDistance.HasValue)
                            {
                                context.Log.Info(
                                    $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} has extracted multi part string name value as {multiPartStringValue} does not have a value which means no match.");

                                continue;
                            }

                            AddToResponses(context, entityAnalysisModelSanction, averageLevenshteinDistance.Value, multiPartStringValue);

                            context.Log.Info(
                                $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} has extracted multi part string name value as {multiPartStringValue} recalculated and is returning {averageLevenshteinDistance}.");

                            return;
                        }
                    }
                    else
                    {
                        if (context.Log.IsInfoEnabled)
                        {
                            context.Log.Info(
                                $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} is evaluating Sanctions {entityAnalysisModelSanction.Name} but could not find it in the payload.");
                        }
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    context.Log.Error(
                        $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} has seen an error in sanctions checking as {ex}.");
                }
            }
        }

        private static double? CalculateSanctionAndUpsertCache(Context context, CacheService cacheService,
            EntityAnalysisModelSanction entityAnalysisModelSanction, string multiPartStringValue)
        {
            var sanctionEntryReturns = FindAllDistanceMatches(context, entityAnalysisModelSanction, multiPartStringValue);

            double? averageLevenshteinDistance = null;
            if (sanctionEntryReturns.Count == 0)
            {
                if (context.Log.IsInfoEnabled)
                {
                    context.Log.Info(
                        $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} is evaluating Sanctions {entityAnalysisModelSanction.Name} and finished execute the fuzzy logic with a distance of {entityAnalysisModelSanction.Distance} found no matches.");
                }
            }
            else
            {
                averageLevenshteinDistance = CalculateAverageDistance(context, entityAnalysisModelSanction, sanctionEntryReturns);
                if (averageLevenshteinDistance.HasValue)
                {
                    AddToResponses(context, entityAnalysisModelSanction, averageLevenshteinDistance.Value, multiPartStringValue);
                }
                else
                {
                    context.Log.Info(
                        $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} is evaluating Sanctions {entityAnalysisModelSanction.Name} and finished execute the fuzzy logic with a distance of {entityAnalysisModelSanction.Distance} found no matches when calculating distance.");
                }
            }

            if (context.Log.IsInfoEnabled)
            {
                context.Log.Info(
                    $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} has constructed a cache payload as Distance of {averageLevenshteinDistance}, MultiPartString of {multiPartStringValue} and a created date of now.  Will upset it in cache.");
            }

            UpsertSanctionInCache(context, cacheService, multiPartStringValue, entityAnalysisModelSanction, averageLevenshteinDistance);

            return averageLevenshteinDistance;
        }

        private static void UpsertSanctionInCache(Context context, CacheService cacheService,
            string multiPartStringValue, EntityAnalysisModelSanction entityAnalysisModelSanction, double? averageLevenshteinDistance)
        {
            if (context.Log.IsInfoEnabled)
            {
                context.Log.Info(
                    $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} is about to insert cache payload.");
            }

            context.PendingWriteTasks.Add(TaskHelper.MeasureTaskTimeAndMemoryAllocatedAsync(TaskType.CacheSanctionInsertAsync, async () => await cacheService.CacheSanctionRepository.InsertAsync(
                context.EntityAnalysisModel.Instance.TenantRegistryId,
                context.EntityAnalysisModel.Instance.Guid,
                multiPartStringValue,
                entityAnalysisModelSanction.Distance, averageLevenshteinDistance).ConfigureAwait(false)));

            if (context.Log.IsInfoEnabled)
            {
                context.Log.Info(
                    $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} has inserted cache payload.");
            }
        }

        private static List<SanctionEntryReturn> FindAllDistanceMatches(Context context, EntityAnalysisModelSanction entityAnalysisModelSanction, string multiPartStringValue)
        {

            if (context.Log.IsInfoEnabled)
            {
                context.Log.Info(
                    $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} is evaluating Sanctions {entityAnalysisModelSanction.Name} and is about to execute the fuzzy logic with a distance of {entityAnalysisModelSanction.Distance}.");
            }

            var sanctionEntryReturns = LevenshteinDistance.CheckMultipartString(
                multiPartStringValue,
                entityAnalysisModelSanction.Distance, context.EntityAnalysisModel.Dependencies.SanctionsEntries);

            if (context.Log.IsInfoEnabled)
            {
                context.Log.Info(
                    $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} is evaluating Sanctions {entityAnalysisModelSanction.Name} and finished execute the fuzzy logic with a distance of {entityAnalysisModelSanction.Distance} and found {sanctionEntryReturns.Count} matches.");
            }
            return sanctionEntryReturns;
        }

        private static double? CalculateAverageDistance(Context context, EntityAnalysisModelSanction entityAnalysisModelSanction, List<SanctionEntryReturn> sanctionEntryReturns)
        {
            if (context.Log.IsInfoEnabled)
            {
                context.Log.Info(
                    $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} is evaluating Sanctions {entityAnalysisModelSanction.Name} and finished execute the fuzzy logic with a distance of {entityAnalysisModelSanction.Distance} is about to calculate the average.");
            }

            var sumLevenshteinDistance = sanctionEntryReturns.Sum(s => s.LevenshteinDistance);

            if (context.Log.IsInfoEnabled)
            {
                context.Log.Info(
                    $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} is evaluating Sanctions {entityAnalysisModelSanction.Name} and finished execute the fuzzy logic with a distance of {entityAnalysisModelSanction.Distance} has a sum of {sumLevenshteinDistance}.");
            }

            if (sumLevenshteinDistance == 0)
            {
                return sumLevenshteinDistance;
            }

            if (Double.IsNaN(sumLevenshteinDistance))
            {
                double? averageLevenshteinDistance = sumLevenshteinDistance / sanctionEntryReturns.Count;

                if (context.Log.IsInfoEnabled)
                {
                    context.Log.Info(
                        $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} is evaluating Sanctions {entityAnalysisModelSanction.Name} and finished execute the fuzzy logic with a distance of {entityAnalysisModelSanction.Distance} has a sum of {sumLevenshteinDistance} and calculated average as {averageLevenshteinDistance}.");
                }

                return averageLevenshteinDistance;
            }

            if (context.Log.IsInfoEnabled)
            {
                context.Log.Info(
                    $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} is evaluating Sanctions {entityAnalysisModelSanction.Name} and finished execute the fuzzy logic with a distance of {entityAnalysisModelSanction.Distance} has a sum of {sumLevenshteinDistance} but is an invalid number.");
            }

            return null;
        }

        private static void AddToResponses(Context context, EntityAnalysisModelSanction entityAnalysisModelSanction, double value, string multiPartStringValue)
        {
            if (!context.EntityAnalysisModelInstanceEntryPayload.Sanction.TryAdd(entityAnalysisModelSanction.Name, value))
            {
                return;
            }
            if (context.Log.IsInfoEnabled)
            {
                context.Log.Info(
                    $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} has extracted multi part string name value as {multiPartStringValue} is adding cache value of {value} to processing. Reprocessing will not take place.");
            }
        }

        private static bool TestIfSanctionHasExpiredAndFound(Context context, CacheSanction sanction, string multiPartStringValue, EntityAnalysisModelSanction entityAnalysisModelSanction)
        {

            bool foundCacheSanctions;
            var deleteLineCacheKeys = sanction.CreatedDate;

            if (context.Log.IsInfoEnabled)
            {
                context.Log.Info(
                    $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} has extracted multi part string name value as {multiPartStringValue} has a cache interval of {entityAnalysisModelSanction.CacheInterval} and value of {entityAnalysisModelSanction.CacheValue}.");
            }

            deleteLineCacheKeys = entityAnalysisModelSanction.CacheInterval switch
            {
                's' => deleteLineCacheKeys.AddSeconds(
                    entityAnalysisModelSanction.CacheValue),
                'n' => deleteLineCacheKeys.AddMinutes(
                    entityAnalysisModelSanction.CacheValue),
                'h' => deleteLineCacheKeys.AddHours(entityAnalysisModelSanction.CacheValue),
                _ => deleteLineCacheKeys.AddDays(entityAnalysisModelSanction.CacheValue)
            };

            if (context.Log.IsInfoEnabled)
            {
                context.Log.Info(
                    $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} has extracted multi part string name value as {multiPartStringValue} has an expiry date of {deleteLineCacheKeys}");
            }

            if (deleteLineCacheKeys <= DateTime.Now)
            {
                foundCacheSanctions = false;

                if (context.Log.IsInfoEnabled)
                {
                    context.Log.Info(
                        $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} has extracted multi part string name value as {multiPartStringValue} cache is not available because of expiration.");
                }
            }
            else
            {
                foundCacheSanctions = true;

                if (context.Log.IsInfoEnabled)
                {
                    context.Log.Info(
                        $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} has extracted multi part string name value as {multiPartStringValue} cache is available.");
                }
            }
            return foundCacheSanctions;
        }

        private static async Task<CacheSanction> LookupFromCacheAsync(Context context,
            CacheService cacheService, string multiPartStringValue, EntityAnalysisModelSanction entityAnalysisModelSanction)
        {

            var sanction = await cacheService.CacheSanctionRepository
                .GetByMultiPartStringDistanceThresholdAsync(
                    context.EntityAnalysisModel.Instance.TenantRegistryId,
                    context.EntityAnalysisModel.Instance.Guid, multiPartStringValue,
                    entityAnalysisModelSanction.Distance
                ).ConfigureAwait(false);

            if (context.Log.IsInfoEnabled)
            {
                context.Log.Info(
                    $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} has extracted multi part string name value as {multiPartStringValue} and has found sanction as {sanction != null}.");
            }
            return sanction;
        }
    }
}
