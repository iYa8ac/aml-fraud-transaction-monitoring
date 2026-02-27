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

namespace Jube.Engine.EntityAnalysisModelInvoke.Extraction
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using Context;
    using Dictionary;
    using DynamicEnvironment;
    using Helpers;
    using log4net;
    using Models.Payload.EntityAnalysisModelInstanceEntryPayload;
    using EntityAnalysisModel=EntityAnalysisModelManager.EntityAnalysisModel.EntityAnalysisModel;

    public class EntityAnalysisModelDictionaryNoBoxingExtractor(
        EntityAnalysisModel entityAnalysisModel,
        Dictionary<int, EntityAnalysisModel> availableModels,
        DynamicEnvironment environment,
        ILog log)
    {
        public Context CreateContext(
            DictionaryNoBoxing<string> payload, int entityAnalysisModelReprocessingRuleInstanceId)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var entityAnalysisModelInstanceEntryPayload = ExtractModelFieldsForInvocation(payload, entityAnalysisModelReprocessingRuleInstanceId);

            ExtractRequestXPathForInvocation(payload, entityAnalysisModelInstanceEntryPayload);

            return new Context
            {
                StartBytesUsed = GC.GetAllocatedBytesForCurrentThread(),
                EntityAnalysisModel = entityAnalysisModel,
                AvailableEntityAnalysisModels = availableModels,
                EntityAnalysisModelInstanceEntryPayload = entityAnalysisModelInstanceEntryPayload,
                Stopwatch = stopwatch,
                Log = log,
                Random = new Random(Environment.TickCount ^ Guid.NewGuid().GetHashCode()),
                Environment = environment,
                Async = false
            };
        }

        private EntityAnalysisModelInstanceEntryPayload ExtractModelFieldsForInvocation(
            DictionaryNoBoxing<string> entry, int entityAnalysisModelReprocessingRuleInstanceId)
        {
            var entityAnalysisModelInstanceEntryPayload = EntityAnalysisModelInstanceEntryPayloadHelpers.Create(entityAnalysisModel, entry["EntityAnalysisModelInstanceEntryGuid"]);

            var modelEntryValue = String.Empty;
            DateTime referenceDateValue = default;

            try
            {
                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Dictionary No Boxing to Context Extractor: EntityAnalysisModelInstanceEntryGUID is {entityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid}.");
                }

                modelEntryValue = entry[entityAnalysisModel.References.EntryName].ToString();

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Dictionary No Boxing to Context Extractor: EntityAnalysisModelInstanceEntryGUID is {entityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} ModelEntryValue is {modelEntryValue}.");
                }

                referenceDateValue = entry[entityAnalysisModel.References.ReferenceDateName];

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Dictionary No Boxing to Context Extractor: EntityAnalysisModelInstanceEntryGUID is {entityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} ReferenceDateValue is {referenceDateValue}. Has created invoke instance.  Will now add the XPath values by looping through the XPath values configured for this model");
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                log.Error($"Dictionary No Boxing to Context Extractor: EntityAnalysisModelInstanceEntryGUID is {entityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} ExtractModelFieldsForInvocation: has produced an error {ex}");
            }

            entityAnalysisModelInstanceEntryPayload.EntityInstanceEntryId = modelEntryValue;
            entityAnalysisModelInstanceEntryPayload.ReferenceDate = referenceDateValue;
            entityAnalysisModelInstanceEntryPayload.Payload = entry;
            entityAnalysisModelInstanceEntryPayload.EntityAnalysisModelReprocessingRuleInstanceId = entityAnalysisModelReprocessingRuleInstanceId;

            return entityAnalysisModelInstanceEntryPayload;
        }

        private void ExtractRequestXPathForInvocation(
            DictionaryNoBoxing<string> entry,
            EntityAnalysisModelInstanceEntryPayload entityAnalysisModelInstanceEntryPayload)
        {
            try
            {
                foreach (var xPath in
                         from xPathLinq in entityAnalysisModel.Collections.EntityAnalysisModelRequestXPaths
                         where !entityAnalysisModelInstanceEntryPayload.Payload.ContainsKey(xPathLinq.Name)
                         select xPathLinq)
                {
                    if (!entry.ContainsKey(xPath.Name))
                    {
                        if (log.IsInfoEnabled)
                        {
                            log.Info(
                                $"Dictionary No Boxing to Context Extractor: EntityAnalysisModelInstanceEntryGUID is {entityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} XPath {xPath.Name} was not in the original payload.");
                        }
                    }
                    else
                    {
                        switch (xPath.DataTypeId)
                        {
                            case 1:
                                entityAnalysisModelInstanceEntryPayload.Payload.TryAdd(xPath.Name, entry[xPath.Name].AsString());
                                break;
                            case 2:
                                entityAnalysisModelInstanceEntryPayload.Payload.TryAdd(xPath.Name, entry[xPath.Name].AsInt());
                                break;
                            case 3:
                                entityAnalysisModelInstanceEntryPayload.Payload.TryAdd(xPath.Name, entry[xPath.Name].AsDouble());
                                break;
                            case 4:
                                entityAnalysisModelInstanceEntryPayload.Payload.TryAdd(xPath.Name, entry[xPath.Name].AsDateTime());
                                break;
                            case 5:
                                entityAnalysisModelInstanceEntryPayload.Payload.TryAdd(xPath.Name, entry[xPath.Name].AsBool());
                                break;
                            case 6:
                            case 7:
                                entityAnalysisModelInstanceEntryPayload.Payload.TryAdd(xPath.Name, entry[xPath.Name].AsDouble());
                                break;
                        }

                        if (log.IsInfoEnabled)
                        {
                            log.Info(
                                $"Dictionary No Boxing to Context Extractor: EntityAnalysisModelInstanceEntryGUID is {entityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} added {xPath.Name} with value {entry[xPath.Name]} as a report column.");
                        }
                    }
                }

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Dictionary No Boxing to Context Extractor: EntityAnalysisModelInstanceEntryGUID is {entityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} has added all request XPath fields,  will now start the invoke.");
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                log.Error($"Dictionary No Boxing to Context Extractor: EntityAnalysisModelInstanceEntryGUID is {entityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} has produced an error {ex}");
            }
        }
    }
}
