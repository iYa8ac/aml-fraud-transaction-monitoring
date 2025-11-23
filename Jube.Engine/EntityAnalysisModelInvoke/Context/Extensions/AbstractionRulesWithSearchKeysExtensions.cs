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
    using AbstractionRulesWithSearchKeys;
    using Cache;
    using Cache.Redis.Models;
    using Data.Poco;
    using Dictionary;
    using TaskCancellation.TaskHelper;
    using EntityAnalysisModelAbstractionRule=EntityAnalysisModelManager.EntityAnalysisModel.Models.Models.EntityAnalysisModelAbstractionRule;

    public static class AbstractionRulesWithSearchKeysExtensions
    {
        public static async Task<Context> ExecuteAbstractionRulesWithSearchKeysAsync(this Context context)
        {
            var pendingExecutionThreads = new List<Task>();
            if (context.EntityAnalysisModel.Flags.EnableCache)
            {
                if (context.Log.IsInfoEnabled)
                {
                    context.Log.Info(
                        $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} Entity cache storage is enabled so will now proceed to loop through the distinct grouping keys for this model.");
                }

                var abstractionRuleMatches = new Dictionary<int, List<DictionaryNoBoxing>>();
                foreach (var (key, value) in context.EntityAnalysisModel.Collections.DistinctSearchKeys)
                {
                    if (context.Log.IsInfoEnabled)
                    {
                        context.Log.Info(
                            $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} is evaluating grouping key {key}.");
                    }

                    try
                    {
                        if (value.SearchKeyCache)
                        {
                            if (context.Log.IsInfoEnabled)
                            {
                                context.Log.Info(
                                    $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} grouping key {key} is a search key,  so the values will be fetched from the cache later on.");
                            }
                        }
                        else
                        {
                            if (context.Log.IsInfoEnabled)
                            {
                                context.Log.Info(
                                    $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} checking if grouping key {key} exists in the current payload data.");
                            }

                            if (context.EntityAnalysisModelInstanceEntryPayload.Payload.ContainsKey(key))
                            {
                                var execute = new Execute
                                {
                                    EntityInstanceEntryDictionaryKvPs = context.EntityAnalysisModelInstanceEntryPayload.Dictionary,
                                    AbstractionRuleGroupingKey = key,
                                    DistinctSearchKey = value,
                                    CachePayloadDocument = context.EntityAnalysisModelInstanceEntryPayload.Payload,
                                    EntityAnalysisModelInstanceEntryPayload =
                                        context.EntityAnalysisModelInstanceEntryPayload,
                                    AbstractionRuleMatches = abstractionRuleMatches,
                                    EntityAnalysisModel = context.EntityAnalysisModel,
                                    Log = context.Log,
                                    DynamicEnvironment = context.Environment,
                                    CacheService = context.EntityAnalysisModel.Services.CacheService,
                                    PendingWritesTasks = context.PendingWriteTasks
                                };

                                if (context.Log.IsInfoEnabled)
                                {
                                    context.Log.Info(
                                        $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} has created a execute object to run all the abstraction rules rolling up to the grouping key.  It has been added to a collection to track it when multi threaded abstraction rules are enabled.");
                                }

                                pendingExecutionThreads.Add(TaskHelper.MeasureTaskTimeAndMemoryAllocatedAsync(TaskType.ExecuteAbstractionRulesWithSearchKeyAsync, async () => await execute.StartAsync().ConfigureAwait(false)));
                            }
                            else
                            {
                                if (context.Log.IsInfoEnabled)
                                {
                                    context.Log.Info(
                                        $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} grouping key {key} does not exist in the current transaction data being processed.");
                                }
                            }
                        }
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        context.Log.Error(
                            $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} checking if grouping key {key} has created an error as {ex}.");
                    }
                }

                if (context.Log.IsInfoEnabled)
                {
                    context.Log.Info(
                        $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} will now loop around all of the Abstraction rules for the purposes of performing the aggregations.");
                }

                await Task.WhenAll(pendingExecutionThreads).ConfigureAwait(false);

                await CalculateAbstractionRuleValuesOrLookupFromTheCacheAsync(context, context.EntityAnalysisModel.Services.CacheService, abstractionRuleMatches).ConfigureAwait(false);

                if (context.Log.IsInfoEnabled)
                {
                    context.Log.Info(
                        $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} all abstraction aggregation has finished, basic rules will now be processed.");
                }
            }
            else
            {
                if (context.Log.IsInfoEnabled)
                {
                    context.Log.Info(
                        $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} Entity cache storage is not enabled so it cannot fetch anything relating to Abstraction Rules.");
                }
            }

            context.EntityAnalysisModelInstanceEntryPayload.InvokeTaskPerformance.ComputeTimes.AbstractionRulesWithSearchKeysAsync = (int)(context.Stopwatch.ElapsedTicks * 1000000 / Stopwatch.Frequency);

            return context;
        }

        private static async Task CalculateAbstractionRuleValuesOrLookupFromTheCacheAsync(Context context,
            CacheService cacheService, Dictionary<int, List<DictionaryNoBoxing>> abstractionRuleMatches)
        {
            var listEntityAnalysisModelIdAbstractionRuleNameSearchKeySearchValueRequest =
                new List<EntityAnalysisModelIdAbstractionRuleNameSearchKeySearchValue>();
            foreach (var abstractionRule in context.EntityAnalysisModel.Collections.ModelAbstractionRules)
            {
                try
                {
                    if (abstractionRule.Search)
                    {
                        if (context.Log.IsInfoEnabled)
                        {
                            context.Log.Info(
                                $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} is evaluating abstraction rule {abstractionRule.Id}.");
                        }

                        if (context.EntityAnalysisModel.Collections.DistinctSearchKeys.FirstOrDefault(x =>
                                x.Key == abstractionRule.SearchKey && x.Value.SearchKeyCache).Value != null)
                        {
                            if (context.Log.IsInfoEnabled)
                            {
                                context.Log.Info(
                                    $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} abstraction rule {abstractionRule.Id} has its values in the cache.");
                            }

                            listEntityAnalysisModelIdAbstractionRuleNameSearchKeySearchValueRequest.Add(
                                new EntityAnalysisModelIdAbstractionRuleNameSearchKeySearchValue
                                {
                                    AbstractionRuleName = abstractionRule.Name,
                                    SearchKey = abstractionRule.SearchKey,
                                    SearchValue = context.EntityAnalysisModelInstanceEntryPayload.Payload[abstractionRule.SearchKey].AsString()
                                });

                            if (context.Log.IsInfoEnabled)
                            {
                                context.Log.Info(
                                    $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} abstraction rule {abstractionRule.Id} has added " +
                                    $"EntityAnalysisModelId:{context.EntityAnalysisModel.Instance.Id}, AbstractionRuleName:{abstractionRule.Name},SearchKey:{abstractionRule.SearchKey} " +
                                    $"and SearchValue:{context.EntityAnalysisModelInstanceEntryPayload.Payload[abstractionRule.SearchKey].AsString()} to he bulk select list.");
                            }
                        }
                        else
                        {
                            if (context.Log.IsInfoEnabled)
                            {
                                context.Log.Info(
                                    $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} is aggregating abstraction rule {abstractionRule.Id} using documents in the entities collection of the cache.");
                            }

                            var aggregatedValue = EntityAnalysisModelAbstractionRuleAggregatorUtility.Aggregate(context.EntityAnalysisModelInstanceEntryPayload
                                , abstractionRuleMatches
                                , abstractionRule
                                , context.Log);

                            AddComputedValuesToAbstractionRulePayload(context, abstractionRule, aggregatedValue);
                        }
                    }

                    if (listEntityAnalysisModelIdAbstractionRuleNameSearchKeySearchValueRequest.Count == 0)
                    {
                        continue;
                    }

                    foreach (var abstractionRuleNameValue in await cacheService.CacheAbstractionRepository
                                 .GetAsync(
                                     context.EntityAnalysisModel.Instance.TenantRegistryId, context.EntityAnalysisModel.Instance.Guid,
                                     listEntityAnalysisModelIdAbstractionRuleNameSearchKeySearchValueRequest)
                                 .ConfigureAwait(false))
                    {
                        AddComputedValuesToAbstractionRulePayload(context, abstractionRule, abstractionRuleNameValue.Value);
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    context.Log.Error(
                        $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} is aggregating abstraction rule {abstractionRule.Id} but has created an error as {ex}.");
                }
            }
        }

        private static void AddComputedValuesToAbstractionRulePayload(Context context,
            EntityAnalysisModelAbstractionRule abstractionRule, double value)
        {
            context.EntityAnalysisModelInstanceEntryPayload.Abstraction
                .Add(abstractionRule.Name, value);

            if (abstractionRule.ReportTable)
            {
                context.EntityAnalysisModelInstanceEntryPayload.ArchiveKeys.Add(new ArchiveKey
                {
                    ProcessingTypeId = 5,
                    Key = abstractionRule.Name,
                    KeyValueFloat = value,
                    EntityAnalysisModelInstanceEntryGuid = context.EntityAnalysisModelInstanceEntryPayload
                        .EntityAnalysisModelInstanceEntryGuid
                });

                if (context.Log.IsInfoEnabled)
                {
                    context.Log.Info(
                        $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} is aggregating abstraction rule {abstractionRule.Id} added value {value} to report payload with a column name of {abstractionRule.Name}.");
                }
            }

            if (context.Log.IsInfoEnabled)
            {
                context.Log.Info(
                    $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} finished aggregating abstraction rule {abstractionRule.Id}.");
            }
        }
    }
}
