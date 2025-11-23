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
    using System.Diagnostics;
    using System.Threading.Tasks;
    using EntityAnalysisModelManager.EntityAnalysisModel.Models.Models;

    public static class DictionaryKvpExtensions
    {
        public static Task<Context> ExecuteDictionaryKvPsAsync(this Context context)
        {
            if (context.Log.IsInfoEnabled)
            {
                context.Log.Info(
                    $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} Will now look for KVP Dictionary Values.");
            }

            IterateAndProcess(context);
            StorePerformanceFromStopwatch(context);

            return Task.FromResult(context);
        }
        private static void StorePerformanceFromStopwatch(Context context)
        {

            context.EntityAnalysisModelInstanceEntryPayload.InvokeTaskPerformance.ComputeTimes.DictionaryKvPsAsync = (int)(context.Stopwatch.ElapsedTicks * 1000000 / Stopwatch.Frequency);

            if (context.Log.IsInfoEnabled)
            {
                context.Log.Info(
                    $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} has finished looking for Dictionary KVP values.");
            }
        }

        private static void IterateAndProcess(Context context)
        {
            foreach (var (i, kvpDictionary) in context.EntityAnalysisModel.Dependencies.KvpDictionaries)
            {
                try
                {
                    if (context.Log.IsInfoEnabled)
                    {
                        context.Log.Info(
                            $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} is evaluating dictionary kvp key of {i}.");
                    }

                    var value = LookupFromTheLocalCacheOfDictionaryKeyValuePairs(context, kvpDictionary, i);
                    AddToResponsesIfNotAdded(context, kvpDictionary, value, i);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    if (context.Log.IsInfoEnabled)
                    {
                        context.Log.Info(
                            $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} has caused an error in Dictionary KVP as {ex}.");
                    }
                }
            }
        }

        private static void AddToResponsesIfNotAdded(Context context, EntityAnalysisModelDictionary kvpDictionary, double value, int i)
        {
            if (context.EntityAnalysisModelInstanceEntryPayload.Dictionary.TryAdd(kvpDictionary.Name, value))
            {
                if (context.Log.IsInfoEnabled)
                {
                    context.Log.Info(
                        $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} is evaluating dictionary kvp key of {i} has added the name of {kvpDictionary.Name} and value of {value} for processing.");
                }
            }
            else
            {
                if (context.Log.IsInfoEnabled)
                {
                    context.Log.Info(
                        $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} is evaluating dictionary kvp key of {i} has already added the name of {kvpDictionary.Name}.");
                }
            }
        }

        private static double LookupFromTheLocalCacheOfDictionaryKeyValuePairs(Context context, EntityAnalysisModelDictionary kvpDictionary, int i)
        {
            double value;
            if (context.EntityAnalysisModelInstanceEntryPayload.Payload.TryGetValue(kvpDictionary.DataName, out var valueCache))
            {
                if (context.Log.IsInfoEnabled)
                {
                    context.Log.Info(
                        $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} is evaluating dictionary kvp key of {i} has been found in the data payload.");
                }

                var key = valueCache.AsString();

                if (context.Log.IsInfoEnabled)
                {
                    context.Log.Info(
                        $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} is evaluating dictionary kvp key of {i} has been found in the data payload and has returned a value of {key}, which will be used for the lookup.");
                }

                if (kvpDictionary.KvPs.TryGetValue(key, out var p))
                {
                    value = p;

                    if (context.Log.IsInfoEnabled)
                    {
                        context.Log.Info(
                            $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} is evaluating dictionary kvp key of {i} has been found in the data payload and has returned a value of {key}, found a lookup value.  The dictionary value has been set to {value}.");
                    }
                }
                else
                {
                    if (context.Log.IsInfoEnabled)
                    {
                        context.Log.Info(
                            $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} is evaluating dictionary kvp key of {i} has been found in the data payload and has returned a value of {key}, does not contain a lookup value.  The dictionary value has been set to zero.");
                    }

                    value = 0;
                }
            }
            else
            {
                if (context.Log.IsInfoEnabled)
                {
                    context.Log.Info(
                        $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} is evaluating dictionary kvp key of {i} but the payload does not have such a value,  so have set the value to zero.");
                }

                value = 0;
            }

            return value;
        }
    }
}
