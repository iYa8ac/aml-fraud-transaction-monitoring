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
    using System.Diagnostics;
    using System.Linq;
    using Data.Poco;
    using ReflectionHelpers;

    public static class ExecuteAbstractionRulesWithoutSearchKeysExtensions
    {
        public static Context ExecuteAbstractionRulesWithoutSearchKeys(this Context context)
        {
            foreach (var evaluateAbstractionRule in
                     from evaluateAbstractionRuleLinq in context.EntityAnalysisModel.Collections.ModelAbstractionRules
                     where !evaluateAbstractionRuleLinq.Search
                     select evaluateAbstractionRuleLinq)
            {
                if (context.Log.IsInfoEnabled)
                {
                    context.Log.Info(
                        $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} abstraction rule {evaluateAbstractionRule.Id} is being processed as a basic rule.");
                }

                double abstractionValue;

                if (ReflectRuleHelper.Execute(evaluateAbstractionRule, context.EntityAnalysisModel, context.EntityAnalysisModelInstanceEntryPayload.Payload,
                        context.EntityAnalysisModelInstanceEntryPayload.Dictionary, context.Log))
                {
                    abstractionValue = 1;

                    if (context.Log.IsInfoEnabled)
                    {
                        context.Log.Info(
                            $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} abstraction rule {evaluateAbstractionRule.Id} has returned true and set abstraction value to {abstractionValue}.");
                    }
                }
                else
                {
                    abstractionValue = 0;

                    if (context.Log.IsInfoEnabled)
                    {
                        context.Log.Info(
                            $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} abstraction rule {evaluateAbstractionRule.Id} has returned false and set abstraction value to {abstractionValue}.");
                    }
                }

                context.EntityAnalysisModelInstanceEntryPayload.Abstraction.Add(evaluateAbstractionRule.Name,
                    abstractionValue);

                if (context.Log.IsInfoEnabled)
                {
                    context.Log.Info(
                        $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} is a basic abstraction rule {evaluateAbstractionRule.Id} added value {abstractionValue} to processing.");
                }

                if (evaluateAbstractionRule.ReportTable)
                {
                    context.EntityAnalysisModelInstanceEntryPayload.ArchiveKeys.Add(new ArchiveKey
                    {
                        ProcessingTypeId = 5,
                        Key = evaluateAbstractionRule.Name,
                        KeyValueFloat = abstractionValue,
                        EntityAnalysisModelInstanceEntryGuid =
                            context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid
                    });

                    if (context.Log.IsInfoEnabled)
                    {
                        context.Log.Info(
                            $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} is a basic abstraction rule {evaluateAbstractionRule.Id} added value {abstractionValue} to report payload with a column name of {evaluateAbstractionRule.Name}.");
                    }
                }

                if (context.Log.IsInfoEnabled)
                {
                    context.Log.Info(
                        $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} finished basic abstraction rule {evaluateAbstractionRule.Id}.");
                }
            }

            context.EntityAnalysisModelInstanceEntryPayload.InvokeTaskPerformance.ComputeTimes.ExecuteAbstractionRulesWithoutSearchKey = (int)(context.Stopwatch.ElapsedTicks * 1000000 / Stopwatch.Frequency);

            if (context.Log.IsInfoEnabled)
            {
                context.Log.Info(
                    $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} Abstraction has concluded in {context.Stopwatch.ElapsedTicks * 1000000 / Stopwatch.Frequency} ns.");
            }

            return context;
        }
    }
}
