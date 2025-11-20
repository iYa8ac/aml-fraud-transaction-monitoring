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

namespace Jube.Engine.EntityAnalysisModelInvoke.Context.Extensions.ActivationRules
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Cache;
    using Data.Poco;
    using Models.CaseManagement;
    using Models.Payload.EntityAnalysisModelInstanceEntry;
    using RabbitMQ.Client;
    using ReflectionHelpers;
    using EntityAnalysisModel=EntityAnalysisModelManager.EntityAnalysisModel.EntityAnalysisModel;

    public static class IterateActivationRulesExtensions
    {
        public static (int activationRuleCount, CreateCase createCase, int? prevailingActivationRuleId) IterateAndProcess(this Context context,
            CacheService cacheService, Dictionary<int, EntityAnalysisModel> availableModels,
            IModel rabbitMqChannel)
        {
            var rulesCount = context.EntityAnalysisModel.Collections.ModelActivationRules.Count;
            var prevailingActivationRuleName = String.Empty;
            var responseElevationHighWaterMark = 0d;
            var activationLock = new object();
            var archiveKeysLock = new object();
            var countLock = new object();
            var createCaseSet = 0;
            var suppressedActivationRules = new List<string>(rulesCount);
            var activationRuleCount = 0;
            CreateCase createCase = null;
            int? prevailingActivationRuleId = null;

            Parallel.For(0, rulesCount, iActivationRule =>
            {
                var evaluateActivationRule = context.EntityAnalysisModel.Collections.ModelActivationRules[iActivationRule];
                try
                {
                    var suppressed = false;
                    if (context.ActivationRuleGetSuppressedModel(ref suppressedActivationRules) || context.CheckSuppressedResponseElevation())
                    {
                        suppressed = true;

                        if (context.Log.IsInfoEnabled)
                        {
                            context.Log.Info(
                                $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} activation rule {evaluateActivationRule.Id} is suppressed at the model level or has exceeded response elevation counter at {context.EntityAnalysisModel.ConcurrentQueues.BillingResponseElevationBalanceEntries.Count} or {context.EntityAnalysisModel.Counters.BillingResponseElevationBalance}.");
                        }
                    }
                    else
                    {
                        if (context.Log.IsInfoEnabled)
                        {
                            context.Log.Info(
                                $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} activation rule {evaluateActivationRule.Id} is not suppressed at the model level, will test at rule level.");
                        }

                        if (!evaluateActivationRule.EnableReprocessing && context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelReprocessingRuleInstanceId.HasValue)
                        {
                            suppressed = true;

                            if (context.Log.IsInfoEnabled)
                            {
                                context.Log.Info(
                                    $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} activation rule {evaluateActivationRule.Id} is suppressed at the activation rule level because of reprocessing.");
                            }
                        }
                        else if (suppressedActivationRules is { Count: > 0 })
                        {
                            suppressed = suppressedActivationRules.Contains(evaluateActivationRule.Name);

                            if (context.Log.IsInfoEnabled)
                            {
                                context.Log.Info(
                                    suppressed
                                        ? $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} activation rule {evaluateActivationRule.Id} is suppressed at the activation rule level."
                                        : $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} activation rule {evaluateActivationRule.Id} is not suppressed at the activation rule level.");
                            }
                        }
                    }

                    var activationSample = evaluateActivationRule.ActivationSample >= context.Random.NextDouble();
                    if (!activationSample)
                    {
                        if (context.Log.IsInfoEnabled)
                        {
                            context.Log.Info(
                                $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id}  has failed in sampling so certain activations will not take place even if there is a match on the activation rule.");
                        }

                        return;
                    }

                    if (context.Log.IsInfoEnabled)
                    {
                        context.Log.Info(
                            $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id}  has passed sampling and is eligible for activation.");
                    }

                    var matched = ReflectRuleHelper.Execute(
                        evaluateActivationRule,
                        context.EntityAnalysisModel,
                        context.EntityAnalysisModelInstanceEntryPayload,
                        context.EntityAnalysisModelInstanceEntryPayload.Dictionary,
                        context.Log);

                    if (context.Log.IsInfoEnabled)
                    {
                        context.Log.Info(
                            $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id}  has finished testing the activation rule and it has a matched status of {matched}.");
                    }

                    if (!matched)
                    {
                        return;
                    }

                    lock (activationLock)
                    {
                        if (context.EntityAnalysisModelInstanceEntryPayload.Activation.ContainsKey(evaluateActivationRule.Name))
                        {
                            if (context.Log.IsInfoEnabled)
                            {
                                context.Log.Info(
                                    $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} and has already added the activation rule {evaluateActivationRule.Id} on {evaluateActivationRule.Name} for processing.");
                            }

                            return;
                        }

                        context.EntityAnalysisModelInstanceEntryPayload.Activation.Add(
                            evaluateActivationRule.Name,
                            new EntityModelActivationRulePayload
                            {
                                Visible = evaluateActivationRule.Visible
                            });

                        if (context.Log.IsInfoEnabled)
                        {
                            context.Log.Info(
                                $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} and has added the activation rule {evaluateActivationRule.Id} flag on {evaluateActivationRule.Name} to the activation buffer for processing.");
                        }
                    }

                    if (evaluateActivationRule.ReportTable)
                    {
                        lock (archiveKeysLock)
                        {
                            context.EntityAnalysisModelInstanceEntryPayload.ArchiveKeys.Add(new ArchiveKey
                            {
                                ProcessingTypeId = 11,
                                Key = evaluateActivationRule.Name,
                                KeyValueBoolean = 1,
                                EntityAnalysisModelInstanceEntryGuid =
                                    context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid
                            });
                        }

                        if (context.Log.IsInfoEnabled)
                        {
                            context.Log.Info(
                                $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} and has added the activation rule {evaluateActivationRule.Id} flag to the response payload also.");
                        }
                    }

                    lock (countLock)
                    {
                        context.ProcessResponseElevation(evaluateActivationRule, ref responseElevationHighWaterMark, suppressed);
                        context.ActivationRuleNotification(evaluateActivationRule, suppressed, rabbitMqChannel);

                        context.ActivationRuleCountsAndArchiveHighWatermark(evaluateActivationRule, suppressed, ref activationRuleCount,
                            ref prevailingActivationRuleId, ref prevailingActivationRuleName);

                        context.ActivationRuleActivationWatcher(evaluateActivationRule, suppressed, rabbitMqChannel);

                        if (createCase == null)
                        {
                            if (Interlocked.CompareExchange(ref createCaseSet, 1, 0) == 0)
                            {
                                createCase = context.ActivationRuleCreateCaseObject(evaluateActivationRule, suppressed);
                            }
                        }

                        context.ActivationRuleTtlCounter(evaluateActivationRule, availableModels, cacheService);
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    context.Log.Error(
                        $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} error in TTL Counter processing as {ex} .");
                }
            });

            return (activationRuleCount, createCase, prevailingActivationRuleId);
        }
    }
}
