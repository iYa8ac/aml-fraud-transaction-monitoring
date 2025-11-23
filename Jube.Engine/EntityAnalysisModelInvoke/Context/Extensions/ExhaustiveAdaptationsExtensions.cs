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
    using ExhaustiveSearchInstance=Exhaustive.Models.ExhaustiveSearchInstance;

    public static class ExhaustiveAdaptationsExtensions
    {
        public static Context ExecuteExhaustiveAdaptation(this Context context)
        {
            if (context.Log.IsInfoEnabled)
            {
                context.Log.Info(
                    $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} will now perform Exhaustive and will loop through each.");
            }

            IterateAndProcess(context);
            StorePerformanceFromStopwatch(context);

            return context;
        }
        private static void StorePerformanceFromStopwatch(Context context)
        {
            context.EntityAnalysisModelInstanceEntryPayload.InvokeTaskPerformance.ComputeTimes.ExecuteExhaustiveAdaptation = (int)(context.Stopwatch.ElapsedTicks * 1000000 / Stopwatch.Frequency);
        }

        private static void IterateAndProcess(Context context)
        {
            foreach (var exhaustive in context.EntityAnalysisModel.Collections.ExhaustiveModels)
            {
                if (context.Log.IsInfoEnabled)
                {
                    context.Log.Info(
                        $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} evaluating Exhaustive Search Instance Id {exhaustive.Id}.");
                }

                try
                {
                    var data = new double[exhaustive.NetworkVariablesInOrder.Count];
                    for (var i = 0; i < exhaustive.NetworkVariablesInOrder.Count; i++)
                    {
                        ExtractValueGivenProcessingTypeAndUpdateArray(context, exhaustive, i, data);
                    }

                    if (context.Log.IsInfoEnabled)
                    {
                        context.Log.Info(
                            $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} is about to recall model with {data.Length} variables.");
                    }

                    var value = exhaustive.TopologyNetwork.Compute(data)[0];

                    if (context.Log.IsInfoEnabled)
                    {
                        context.Log.Info(
                            $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} has recalled a score of {value}.  Will proceed to add the value to payload collection.");
                    }

                    context.EntityAnalysisModelInstanceEntryPayload.ExhaustiveAdaptation.Add(exhaustive.Name, value);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    if (context.Log.IsInfoEnabled)
                    {
                        context.Log.Info(
                            $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} has exception {ex}.");
                    }
                }

                if (context.Log.IsInfoEnabled)
                {
                    context.Log.Info(
                        $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} has concluded exhaustive recall.");
                }
            }
        }

        private static void ExtractValueGivenProcessingTypeAndUpdateArray(Context context, ExhaustiveSearchInstance exhaustive, int i, double[] data)
        {
            var cleanName = exhaustive.NetworkVariablesInOrder[i].Name.Contains('.')
                ? exhaustive.NetworkVariablesInOrder[i].Name.Split(".")[1]
                : exhaustive.NetworkVariablesInOrder[i].Name;

            if (context.Log.IsInfoEnabled)
            {
                context.Log.Info(
                    $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} evaluating Exhaustive Search Instance Id {exhaustive.Id}." +
                    $"  Will look up {cleanName} for processing type id {exhaustive.NetworkVariablesInOrder[i].ProcessingTypeId}.");
            }

            switch (exhaustive.NetworkVariablesInOrder[i].ProcessingTypeId)
            {
                case 1:
                    ExtractFromPayload(context, exhaustive, i, data, cleanName);
                    break;
                case 2:
                    ExtractFromDictionary(context, exhaustive, i, data, cleanName);
                    break;
                case 3:
                    ExtractFromTtlCounter(context, exhaustive, i, data, cleanName);
                    break;
                case 4:
                    ExtractFromSanction(context, exhaustive, i, data, cleanName);
                    break;
                case 5:
                    ExtractFromAbstraction(context, exhaustive, i, data, cleanName);
                    break;
                case 6:
                    ExtractFromAbstractionCalculation(context, exhaustive, i, data, cleanName);
                    break;
                default:
                    ExtractFromAbstraction(context, exhaustive, i, data, cleanName);
                    break;
            }
        }

        private static void ExtractFromAbstractionCalculation(Context context, ExhaustiveSearchInstance exhaustive, int i, double[] data, string cleanName)
        {
            if (context.Log.IsInfoEnabled)
            {
                context.Log.Info(
                    $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} " +
                    $" and model {context.EntityAnalysisModel.Instance.Id} evaluating Exhaustive Search Instance Id {exhaustive.Id}." +
                    $" will look up {cleanName} for Abstraction Calculation.");
            }

            if (context.EntityAnalysisModelInstanceEntryPayload
                .AbstractionCalculation.TryGetValue(cleanName, out var valueAbstractionCalculation))
            {
                data[i] = exhaustive.NetworkVariablesInOrder[i].ZScore(
                    valueAbstractionCalculation);

                if (context.Log.IsInfoEnabled)
                {
                    context.Log.Info(
                        $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} " +
                        $" and model {context.EntityAnalysisModel.Instance.Id} evaluating Exhaustive Search Instance Id {exhaustive.Id}." +
                        $" {cleanName} found value {data[i]}.");
                }
            }
            else
            {
                data[i] = exhaustive.NetworkVariablesInOrder[i].ZScore(
                    exhaustive.NetworkVariablesInOrder[i].Mean);

                if (context.Log.IsInfoEnabled)
                {
                    context.Log.Info(
                        $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} " +
                        $" and model {context.EntityAnalysisModel.Instance.Id} evaluating Exhaustive Search Instance Id {exhaustive.Id}." +
                        $" {cleanName} fall back value {data[i]}.");
                }
            }
        }

        private static void ExtractFromAbstraction(Context context, ExhaustiveSearchInstance exhaustive, int i, double[] data, string cleanName)
        {
            if (context.Log.IsInfoEnabled)
            {
                context.Log.Info(
                    $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} " +
                    $" and model {context.EntityAnalysisModel.Instance.Id} evaluating Exhaustive Search Instance Id {exhaustive.Id}." +
                    $" will look up {cleanName} for Abstraction.");
            }

            if (context.EntityAnalysisModelInstanceEntryPayload
                .Abstraction.TryGetValue(cleanName, out var valueAbstraction))
            {
                data[i] = exhaustive.NetworkVariablesInOrder[i].ZScore(
                    valueAbstraction);

                if (context.Log.IsInfoEnabled)
                {
                    context.Log.Info(
                        $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} " +
                        $" and model {context.EntityAnalysisModel.Instance.Id} evaluating Exhaustive Search Instance Id {exhaustive.Id}." +
                        $" {cleanName} found value {data[i]}.");
                }
            }
            else
            {
                data[i] = exhaustive.NetworkVariablesInOrder[i].ZScore(
                    exhaustive.NetworkVariablesInOrder[i].Mean);

                if (context.Log.IsInfoEnabled)
                {
                    context.Log.Info(
                        $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} " +
                        $" and model {context.EntityAnalysisModel.Instance.Id} evaluating Exhaustive Search Instance Id {exhaustive.Id}." +
                        $" {cleanName} fall back value {data[i]}.");
                }
            }
        }

        private static void ExtractFromSanction(Context context, ExhaustiveSearchInstance exhaustive, int i, double[] data, string cleanName)
        {
            if (context.Log.IsInfoEnabled)
            {
                context.Log.Info(
                    $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} " +
                    $" and model {context.EntityAnalysisModel.Instance.Id} evaluating Exhaustive Search Instance Id {exhaustive.Id}." +
                    $" will look up {cleanName} for Ttl Counter.");
            }

            if (context.EntityAnalysisModelInstanceEntryPayload
                .Sanction.TryGetValue(cleanName, out var valueSanction))
            {
                data[i] = exhaustive.NetworkVariablesInOrder[i].ZScore(
                    valueSanction);

                if (context.Log.IsInfoEnabled)
                {
                    context.Log.Info(
                        $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} " +
                        $" and model {context.EntityAnalysisModel.Instance.Id} evaluating Exhaustive Search Instance Id {exhaustive.Id}." +
                        $" {cleanName} found value {data[i]}.");
                }
            }
            else
            {
                data[i] = exhaustive.NetworkVariablesInOrder[i].ZScore(
                    exhaustive.NetworkVariablesInOrder[i].Mean);

                if (context.Log.IsInfoEnabled)
                {
                    context.Log.Info(
                        $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} " +
                        $" and model {context.EntityAnalysisModel.Instance.Id} evaluating Exhaustive Search Instance Id {exhaustive.Id}." +
                        $" {cleanName} fall back value {data[i]}.");
                }
            }
        }

        private static void ExtractFromTtlCounter(Context context, ExhaustiveSearchInstance exhaustive, int i, double[] data, string cleanName)
        {
            if (context.Log.IsInfoEnabled)
            {
                context.Log.Info(
                    $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} " +
                    $" and model {context.EntityAnalysisModel.Instance.Id} evaluating Exhaustive Search Instance Id {exhaustive.Id}." +
                    $" will look up {cleanName} for Ttl Counter.");
            }

            if (context.EntityAnalysisModelInstanceEntryPayload
                .TtlCounter.TryGetValue(cleanName, out var valueTtl))
            {
                data[i] = exhaustive.NetworkVariablesInOrder[i].ZScore(
                    valueTtl);

                if (context.Log.IsInfoEnabled)
                {
                    context.Log.Info(
                        $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} " +
                        $" and model {context.EntityAnalysisModel.Instance.Id} evaluating Exhaustive Search Instance Id {exhaustive.Id}." +
                        $" {cleanName} found value {data[i]}.");
                }
            }
            else
            {
                data[i] = exhaustive.NetworkVariablesInOrder[i].ZScore(
                    exhaustive.NetworkVariablesInOrder[i].Mean);

                if (context.Log.IsInfoEnabled)
                {
                    context.Log.Info(
                        $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} " +
                        $" and model {context.EntityAnalysisModel.Instance.Id} evaluating Exhaustive Search Instance Id {exhaustive.Id}." +
                        $" {cleanName} fall back value {data[i]}.");
                }
            }
        }

        private static void ExtractFromDictionary(Context context, ExhaustiveSearchInstance exhaustive, int i, double[] data, string cleanName)
        {
            if (context.Log.IsInfoEnabled)
            {
                context.Log.Info(
                    $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} " +
                    $" and model {context.EntityAnalysisModel.Instance.Id} evaluating Exhaustive Search Instance Id {exhaustive.Id}." +
                    $" will look up {cleanName} for KVP.");
            }

            if (context.EntityAnalysisModelInstanceEntryPayload
                .Dictionary.TryGetValue(cleanName, out var valueKvp))
            {
                data[i] = exhaustive.NetworkVariablesInOrder[i].ZScore(
                    valueKvp);

                if (context.Log.IsInfoEnabled)
                {
                    context.Log.Info(
                        $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} " +
                        $" and model {context.EntityAnalysisModel.Instance.Id} evaluating Exhaustive Search Instance Id {exhaustive.Id}." +
                        $" {cleanName} found value {data[i]}.");
                }
            }
            else
            {
                data[i] = exhaustive.NetworkVariablesInOrder[i].ZScore(
                    exhaustive.NetworkVariablesInOrder[i].Mean);

                if (context.Log.IsInfoEnabled)
                {
                    context.Log.Info(
                        $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} " +
                        $" and model {context.EntityAnalysisModel.Instance.Id} evaluating Exhaustive Search Instance Id {exhaustive.Id}." +
                        $" {cleanName} fall back value {data[i]}.");
                }
            }
        }

        private static void ExtractFromPayload(Context context, ExhaustiveSearchInstance exhaustive, int i, double[] data, string cleanName)
        {
            if (context.Log.IsInfoEnabled)
            {
                context.Log.Info(
                    $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} " +
                    $" and model {context.EntityAnalysisModel.Instance.Id} evaluating Exhaustive Search Instance Id {exhaustive.Id}." +
                    $" will look up {cleanName} for payload.");
            }

            if (context.EntityAnalysisModelInstanceEntryPayload
                .Payload.ContainsKey(cleanName))
            {
                data[i] = exhaustive.NetworkVariablesInOrder[i].ZScore(
                    context.EntityAnalysisModelInstanceEntryPayload.Payload[cleanName].AsDouble());

                if (context.Log.IsInfoEnabled)
                {
                    context.Log.Info(
                        $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} " +
                        $" and model {context.EntityAnalysisModel.Instance.Id} evaluating Exhaustive Search Instance Id {exhaustive.Id}." +
                        $" {cleanName} found value {data[i]}.");
                }
            }
            else
            {
                data[i] = exhaustive.NetworkVariablesInOrder[i].ZScore(
                    exhaustive.NetworkVariablesInOrder[i].Mean);

                if (context.Log.IsInfoEnabled)
                {
                    context.Log.Info(
                        $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} " +
                        $" and model {context.EntityAnalysisModel.Instance.Id} evaluating Exhaustive Search Instance Id {exhaustive.Id}." +
                        $" {cleanName} fall back value {data[i]}.");
                }
            }
        }
    }
}
