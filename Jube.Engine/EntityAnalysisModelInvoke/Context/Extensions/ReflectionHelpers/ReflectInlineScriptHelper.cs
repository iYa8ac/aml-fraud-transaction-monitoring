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

namespace Jube.Engine.EntityAnalysisModelInvoke.Context.Extensions.ReflectionHelpers
{
    using System;
    using System.Threading.Tasks;
    using Data.Poco;
    using EntityAnalysisModelInlineScript=EntityAnalysisModelManager.EntityAnalysisModel.Models.Models.EntityAnalysisModelInlineScript;

    public static class ReflectInlineScriptHelper
    {
        public static async Task ExecuteAsync(EntityAnalysisModelInlineScript.EntityAnalysisModelInlineScript entityAnalysisModelInlineScript, Context context)
        {
            var activatedObject = Activator.CreateInstance(entityAnalysisModelInlineScript.InlineScriptType);

            object[] args = [context];
            var result = entityAnalysisModelInlineScript.PreProcessingMethodInfo.Invoke(activatedObject, args);

            if (result is Task task)
            {
                try
                {
                    await task;
                }
                catch (Exception ex)
                {
                    context.Log.Info($"Error executing Inline Script {entityAnalysisModelInlineScript.Id}:", ex);
                }
            }

            foreach (var entityAnalysisModelInlineScriptPropertyAttribute in entityAnalysisModelInlineScript.EntityAnalysisModelInlineScriptPropertyAttributes)
            {
                try
                {
                    if (activatedObject == null)
                    {
                        continue;
                    }

                    var property = entityAnalysisModelInlineScript.InlineScriptType.GetProperty(entityAnalysisModelInlineScriptPropertyAttribute.Key);
                    var propertyType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

                    if (propertyType != typeof(string)
                        && propertyType != typeof(int)
                        && propertyType != typeof(bool)
                        && propertyType != typeof(DateTime)
                        && propertyType != typeof(double))
                    {
                        continue;
                    }

                    var value = property.GetValue(activatedObject);

                    switch (value)
                    {
                        case string s:
                            context.EntityAnalysisModelInstanceEntryPayload.Payload.TryAdd(entityAnalysisModelInlineScriptPropertyAttribute.Key, s);

                            if (entityAnalysisModelInlineScriptPropertyAttribute.Value.ReportTable)
                            {
                                context.EntityAnalysisModelInstanceEntryPayload.ArchiveKeys.Add(new ArchiveKey
                                {
                                    ProcessingTypeId = 1,
                                    Key = property.Name,
                                    KeyValueString = value == null ? null : Convert.ToString(value),
                                    EntityAnalysisModelInstanceEntryGuid =
                                        context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid
                                });
                            }

                            break;

                        case int i:
                            context.EntityAnalysisModelInstanceEntryPayload.Payload.TryAdd(entityAnalysisModelInlineScriptPropertyAttribute.Key, i);

                            if (entityAnalysisModelInlineScriptPropertyAttribute.Value.ReportTable)
                            {
                                context.EntityAnalysisModelInstanceEntryPayload.ArchiveKeys.Add(new ArchiveKey
                                {
                                    ProcessingTypeId = 1,
                                    Key = property.Name,
                                    KeyValueInteger = i,
                                    EntityAnalysisModelInstanceEntryGuid =
                                        context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid
                                });
                            }

                            break;

                        case byte b:
                            context.EntityAnalysisModelInstanceEntryPayload.Payload.TryAdd(entityAnalysisModelInlineScriptPropertyAttribute.Key, b);

                            if (entityAnalysisModelInlineScriptPropertyAttribute.Value.ReportTable)
                            {
                                context.EntityAnalysisModelInstanceEntryPayload.ArchiveKeys.Add(new ArchiveKey
                                {
                                    ProcessingTypeId = 1,
                                    Key = property.Name,
                                    KeyValueBoolean = b,
                                    EntityAnalysisModelInstanceEntryGuid =
                                        context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid
                                });
                            }

                            break;

                        case double d:
                            context.EntityAnalysisModelInstanceEntryPayload.Payload.TryAdd(entityAnalysisModelInlineScriptPropertyAttribute.Key, d);

                            if (entityAnalysisModelInlineScriptPropertyAttribute.Value.ReportTable)
                            {
                                context.EntityAnalysisModelInstanceEntryPayload.ArchiveKeys.Add(new ArchiveKey
                                {
                                    ProcessingTypeId = 1,
                                    Key = property.Name,
                                    KeyValueFloat = d,
                                    EntityAnalysisModelInstanceEntryGuid =
                                        context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid
                                });
                            }

                            break;

                        case DateTime dt:
                            context.EntityAnalysisModelInstanceEntryPayload.Payload.TryAdd(entityAnalysisModelInlineScriptPropertyAttribute.Key, dt);

                            if (entityAnalysisModelInlineScriptPropertyAttribute.Value.ReportTable)
                            {
                                context.EntityAnalysisModelInstanceEntryPayload.ArchiveKeys.Add(new ArchiveKey
                                {
                                    ProcessingTypeId = 1,
                                    Key = property.Name,
                                    KeyValueDate = dt,
                                    EntityAnalysisModelInstanceEntryGuid =
                                        context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid
                                });
                            }

                            break;

                        default:
                            context.EntityAnalysisModelInstanceEntryPayload.Payload.TryAdd(entityAnalysisModelInlineScriptPropertyAttribute.Key, value.ToString());

                            if (entityAnalysisModelInlineScriptPropertyAttribute.Value.ReportTable)
                            {
                                context.EntityAnalysisModelInstanceEntryPayload.ArchiveKeys.Add(new ArchiveKey
                                {
                                    ProcessingTypeId = 1,
                                    Key = property.Name,
                                    KeyValueString = value.ToString(),
                                    EntityAnalysisModelInstanceEntryGuid =
                                        context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid
                                });
                            }

                            break;
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    context.Log.Error(ex.ToString());
                }
            }
        }
    }
}
