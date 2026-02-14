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
    using System.Threading.Tasks;
    using Data.Poco;
    using Newtonsoft.Json;
    using EntityAnalysisModelHttpAdaptation=EntityAnalysisModelManager.EntityAnalysisModel.Models.Models.EntityAnalysisModelHttpAdaptation;

    public static class HttpAdaptationsExtensions
    {
        public static async Task<Context> ExecuteHttpAdaptationsAsync(this Context context)
        {
            if (context.Log.IsInfoEnabled)
            {
                context.Log.Info($"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} will begin processing adaptations.");
            }

            await IterateAndProcessAsync(context).ConfigureAwait(false);
            StorePerformanceFromStopwatch(context);

            if (context.Log.IsInfoEnabled)
            {
                context.Log.Info($"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} Adaptations have concluded.");
            }

            return context;
        }
        private static void StorePerformanceFromStopwatch(Context context)
        {

            context.EntityAnalysisModelInstanceEntryPayload.InvokeTaskPerformance.ComputeTimes.ExecuteHttpAdaptation = (int)(context.Stopwatch.ElapsedTicks * 1000000 / Stopwatch.Frequency);
        }

        private static async Task IterateAndProcessAsync(Context context)
        {
            foreach (var (adaptationKey, modelAdaptation) in context.EntityAnalysisModel.Collections.EntityAnalysisModelAdaptations)
            {
                try
                {
                    var totalKeys = CalculateTotalKeys(context);
                    var jsonForPlumber = new Dictionary<string, object>(totalKeys);

                    if (context.Log.IsInfoEnabled)
                    {
                        context.Log.Info(
                            $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} is evaluating {adaptationKey} has finished allocating the Data collection for R Plumber POST.");
                    }

                    AddAbstractions(context, jsonForPlumber, adaptationKey);
                    AddTtlCounters(context, jsonForPlumber, adaptationKey);
                    AddExtractionCalculations(context, jsonForPlumber, adaptationKey);

                    if (context.Log.IsInfoEnabled)
                    {
                        context.Log.Info(
                            $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} is evaluating {adaptationKey} has finished allocating and created JSON for R Plumber:{jsonForPlumber}.");
                    }

                    var adaptationSimulation = await RecallHttpEndpointAsync(context, modelAdaptation, jsonForPlumber, adaptationKey, context.EntityAnalysisModel.JsonSerializationHelper.DefaultJsonSerializerSettingsSettings).ConfigureAwait(false);

                    context.EntityAnalysisModelInstanceEntryPayload.HttpAdaptation[modelAdaptation.Name] = adaptationSimulation;
                    AddToArchiveKeysDictionary(context, modelAdaptation, adaptationSimulation, adaptationKey);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    if (context.Log.IsInfoEnabled)
                    {
                        context.Log.Info(
                            $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} is evaluating {adaptationKey} produced an error {ex}.");
                    }
                }
            }
        }

        private static int CalculateTotalKeys(Context context)
        {
            var totalKeys = context.EntityAnalysisModel.Collections.ModelAbstractionRules.Count + context.EntityAnalysisModel.Collections.ModelTtlCounters.Count + context.EntityAnalysisModel.Collections.EntityAnalysisModelAbstractionCalculations.Count;
            return totalKeys;
        }

        private static void AddToArchiveKeysDictionary(Context context, EntityAnalysisModelHttpAdaptation modelAdaptation, double adaptationSimulation, int adaptationKey)
        {
            if (!modelAdaptation.ReportTable || context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelReprocessingRuleInstanceId.HasValue)
            {
                return;
            }

            context.EntityAnalysisModelInstanceEntryPayload.ArchiveKeys.Add(new ArchiveKey
            {
                ProcessingTypeId = 9,
                Key = modelAdaptation.Name,
                KeyValueFloat = adaptationSimulation,
                EntityAnalysisModelInstanceEntryGuid = context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid
            });

            if (context.Log.IsInfoEnabled)
            {
                context.Log.Info(
                    $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} is evaluating {adaptationKey} has called R Plumber with a response of {adaptationSimulation} and has added it to the SQL report payload.");
            }
        }

        private static async Task<double> RecallHttpEndpointAsync(Context context,
            EntityAnalysisModelHttpAdaptation modelAdaptation,
            Dictionary<string, object> jsonForPlumber, int adaptationKey, JsonSerializerSettings jsonSerializerSettings)
        {
            var adaptationSimulation = await modelAdaptation.PostAsync(jsonForPlumber, jsonSerializerSettings, context.Log).ConfigureAwait(false);

            if (context.Log.IsInfoEnabled)
            {
                context.Log.Info(
                    $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} is evaluating {adaptationKey} has called R Plumber with a response of {adaptationSimulation}.");
            }

            return adaptationSimulation;
        }

        private static void AddExtractionCalculations(Context context, Dictionary<string, object> jsonForPlumber, int adaptationKey)
        {
            foreach (var kvp in context.EntityAnalysisModelInstanceEntryPayload.AbstractionCalculation)
            {
                if (!jsonForPlumber.ContainsKey(kvp.Key))
                {
                    jsonForPlumber[kvp.Key] = kvp.Value;
                }
            }

            if (context.Log.IsInfoEnabled)
            {
                context.Log.Info(
                    $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} is evaluating {adaptationKey} has finished allocating the Abstraction Calculations for R Plumber POST.");
            }
        }

        private static void AddTtlCounters(Context context, Dictionary<string, object> jsonForPlumber, int adaptationKey)
        {
            foreach (var kvp in context.EntityAnalysisModelInstanceEntryPayload.TtlCounter)
            {
                if (!jsonForPlumber.ContainsKey(kvp.Key))
                {
                    jsonForPlumber[kvp.Key] = kvp.Value;
                }
            }

            if (context.Log.IsInfoEnabled)
            {
                context.Log.Info(
                    $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} is evaluating {adaptationKey} has finished allocating the TTL Counters for R Plumber POST.");
            }
        }

        private static void AddAbstractions(Context context, Dictionary<string, object> jsonForPlumber, int adaptationKey)
        {
            foreach (var kvp in context.EntityAnalysisModelInstanceEntryPayload.Abstraction)
            {
                jsonForPlumber[kvp.Key] = kvp.Value;
            }

            if (context.Log.IsInfoEnabled)
            {
                context.Log.Info(
                    $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} is evaluating {adaptationKey} has finished allocating the Abstractions for R Plumber POST.");
            }
        }
    }
}
