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
    using System.Threading.Tasks;
    using ActivationRules;

    public static class ActivationRulesExtensions
    {
        public static async Task<Context> ExecuteActivationsAsync(this Context context)
        {
            if (context.Log.IsInfoEnabled)
            {
                context.Log.Info(
                    $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} will now process {context.EntityAnalysisModel.Collections.ModelActivationRules.Count} Activation Rules.");
            }

            var (activationRuleCount, createCase, prevailingActivationRuleId)
                = await context.IterateAndProcessAsync(context.EntityAnalysisModel.Services.CacheService, context.AvailableEntityAnalysisModels, context.EntityAnalysisModel.Services.RabbitMqChannel).ConfigureAwait(false);

            context.ActivationRuleFinishResponseElevation(context.EntityAnalysisModelInstanceEntryPayload.ResponseElevation.Value);
            context.ActivationRuleResponseElevationAddToCounters();
            context.UpdateContextStateWithActivationRulesOutcome(activationRuleCount, prevailingActivationRuleId, createCase);

            StorePerformanceFromStopwatch(context);

            return context;
        }

        private static void StorePerformanceFromStopwatch(Context context)
        {
            context.EntityAnalysisModelInstanceEntryPayload.InvokeTaskPerformance.ComputeTimes.ExecuteActivation = (int)(context.Stopwatch.ElapsedTicks * 1000000 / Stopwatch.Frequency);

            if (context.Log.IsInfoEnabled)
            {
                context.Log.Info(
                    $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} has added the response elevation for use in bidding against other models if called by model inheritance.");
            }
        }
    }
}
