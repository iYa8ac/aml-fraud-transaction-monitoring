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

namespace Jube.Engine.EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload
{
    using System.IO;
    using System.Linq;
    using AsyncInvocationCallbackToken;
    using Context;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    public static class BuildJsonResponses
    {
        public static MemoryStream BuildFullJson(AsyncInvocationCallbackToken payload,
            JsonSerializer serializer)
        {
            var stream = new MemoryStream();
            var streamWriter = new StreamWriter(stream);
            var jsonWriter = new JsonTextWriter(streamWriter);

            serializer.Serialize(jsonWriter, payload);
            jsonWriter.Flush();
            streamWriter.Flush();
            stream.Seek(0, SeekOrigin.Begin);
            return stream;
        }

        public static MemoryStream BuildFullJson(EntityAnalysisModelInstanceEntryPayload payload,
            JsonSerializer serializer)
        {
            var stream = new MemoryStream();
            var streamWriter = new StreamWriter(stream);
            var jsonWriter = new JsonTextWriter(streamWriter);

            serializer.Serialize(jsonWriter, payload);
            jsonWriter.Flush();
            streamWriter.Flush();
            stream.Seek(0, SeekOrigin.Begin);
            return stream;
        }

        public static MemoryStream BuildPartialResponsePayloadJson(Context context)
        {
            var jObject = CreateJObject(context);
            AddCreateCaseToJObject(context, jObject);
            AddDictionaryToJObject(context, jObject);
            AddTtlCounterToJObject(context, jObject);
            AddSanctionToJObject(context, jObject);
            AddAbstractionToJObject(context, jObject);
            AddAbstractionCalculationToJObject(context, jObject);
            AddHttpAdaptationToJObject(context, jObject);
            AddExhaustiveAdaptationToJObject(context, jObject);
            AddActivationToJObject(context, jObject);
            AddTagToJObject(context, jObject);

            var stream = new MemoryStream();
            var streamWriter = new StreamWriter(stream);
            var jsonWriter = new JsonTextWriter(streamWriter);

            jObject.WriteTo(jsonWriter);
            jsonWriter.Flush();
            streamWriter.Flush();
            stream.Seek(0, SeekOrigin.Begin);
            return stream;
        }

        private static void AddTagToJObject(Context context, JObject jObject)
        {

            var kvpEntityAnalysisModelTags = context.EntityAnalysisModel.Collections.EntityAnalysisModelTags.Where(w => w.ResponsePayload).ToArray();
            if (!kvpEntityAnalysisModelTags.Any())
            {
                return;
            }

            var kvpEntityAnalysisModelTagsArray = new JArray();
            jObject.Add("Tag", kvpEntityAnalysisModelTagsArray);
            foreach (var kvpEntityAnalysisModelTag in kvpEntityAnalysisModelTags)
            {
                var value = context.EntityAnalysisModelInstanceEntryPayload.Activation.FirstOrDefault(f => f.Key == kvpEntityAnalysisModelTag.Name);
                if (value.Key != null)
                {
                    kvpEntityAnalysisModelTagsArray.Add(JToken.FromObject(value));
                }
            }
        }

        private static void AddActivationToJObject(Context context, JObject jObject)
        {

            var kvpModelActivationRules = context.EntityAnalysisModel.Collections.ModelActivationRules.Where(w => w.ResponsePayload).ToArray();
            if (!kvpModelActivationRules.Any())
            {
                return;
            }

            var kvpModelActivationRulesArray = new JArray();
            jObject.Add("Activation", kvpModelActivationRulesArray);
            foreach (var kvpModelActivationRule in kvpModelActivationRules)
            {
                var value = context.EntityAnalysisModelInstanceEntryPayload.Activation.FirstOrDefault(f => f.Key == kvpModelActivationRule.Name);
                if (value.Key != null)
                {
                    kvpModelActivationRulesArray.Add(JToken.FromObject(value));
                }
            }
        }

        private static void AddExhaustiveAdaptationToJObject(Context context, JObject jObject)
        {

            var kvpExhaustiveModels = context.EntityAnalysisModel.Collections.ExhaustiveModels.Where(w => w.ResponsePayload).ToArray();
            if (!kvpExhaustiveModels.Any())
            {
                return;
            }

            var kvpExhaustiveModelsArray = new JArray();
            jObject.Add("ExhaustiveAdaptation", kvpExhaustiveModelsArray);
            foreach (var kvpExhaustiveModel in kvpExhaustiveModels)
            {
                var value = context.EntityAnalysisModelInstanceEntryPayload.ExhaustiveAdaptation.FirstOrDefault(f => f.Key == kvpExhaustiveModel.Name);
                if (value.Key != null)
                {
                    kvpExhaustiveModelsArray.Add(JToken.FromObject(value));
                }
            }
        }

        private static void AddHttpAdaptationToJObject(Context context, JObject jObject)
        {

            var kvpEntityAnalysisModelAdaptations = context.EntityAnalysisModel.Collections.EntityAnalysisModelAdaptations.Where(w => w.Value.ResponsePayload).ToArray();
            if (!kvpEntityAnalysisModelAdaptations.Any())
            {
                return;
            }

            var kvpEntityAnalysisModelAdaptationsArray = new JArray();
            jObject.Add("HttpAdaptation", kvpEntityAnalysisModelAdaptationsArray);
            foreach (var kvpEntityAnalysisModelAdaptation in kvpEntityAnalysisModelAdaptations)
            {
                var value = context.EntityAnalysisModelInstanceEntryPayload.ExhaustiveAdaptation.FirstOrDefault(f => f.Key == kvpEntityAnalysisModelAdaptation.Value.Name);
                if (value.Key != null)
                {
                    kvpEntityAnalysisModelAdaptationsArray.Add(JToken.FromObject(value));
                }
            }
        }

        private static void AddAbstractionCalculationToJObject(Context context, JObject jObject)
        {

            var kvpEntityAnalysisModelAbstractionCalculations = context.EntityAnalysisModel.Collections.EntityAnalysisModelAbstractionCalculations.Where(w => w.ResponsePayload).ToArray();
            if (!kvpEntityAnalysisModelAbstractionCalculations.Any())
            {
                return;
            }

            var kvpEntityAnalysisModelAbstractionCalculationsArray = new JArray();
            jObject.Add("AbstractionCalculation", kvpEntityAnalysisModelAbstractionCalculationsArray);
            foreach (var kvpEntityAnalysisModelAbstractionCalculation in kvpEntityAnalysisModelAbstractionCalculations)
            {
                var value = context.EntityAnalysisModelInstanceEntryPayload.AbstractionCalculation.FirstOrDefault(f => f.Key == kvpEntityAnalysisModelAbstractionCalculation.Name);
                if (value.Key != null)
                {
                    kvpEntityAnalysisModelAbstractionCalculationsArray.Add(JToken.FromObject(value));
                }
            }
        }

        private static void AddAbstractionToJObject(Context context, JObject jObject)
        {

            var kvpAbstractions = context.EntityAnalysisModel.Collections.ModelAbstractionRules.Where(w => w.ResponsePayload).ToArray();
            if (!kvpAbstractions.Any())
            {
                return;
            }

            var kvpAbstractionsJArray = new JArray();
            jObject.Add("Abstraction", kvpAbstractionsJArray);
            foreach (var kvpAbstraction in kvpAbstractions)
            {
                var value = context.EntityAnalysisModelInstanceEntryPayload.Abstraction.FirstOrDefault(f => f.Key == kvpAbstraction.Name);
                if (value.Key != null)
                {
                    kvpAbstractionsJArray.Add(JToken.FromObject(value));
                }
            }
        }

        private static void AddSanctionToJObject(Context context, JObject jObject)
        {

            var kvpEntityAnalysisModelSanctions = context.EntityAnalysisModel.Collections.EntityAnalysisModelSanctions.Where(w => w.ResponsePayload).ToArray();
            if (!kvpEntityAnalysisModelSanctions.Any())
            {
                return;
            }

            var kvpEntityAnalysisModelSanctionsArray = new JArray();
            jObject.Add("Sanction", kvpEntityAnalysisModelSanctionsArray);
            foreach (var kvpEntityAnalysisModelSanction in kvpEntityAnalysisModelSanctions)
            {
                var value = context.EntityAnalysisModelInstanceEntryPayload.Sanction.FirstOrDefault(f => f.Key == kvpEntityAnalysisModelSanction.Name);
                if (value.Key != null)
                {
                    kvpEntityAnalysisModelSanctionsArray.Add(JToken.FromObject(value));
                }
            }
        }

        private static void AddTtlCounterToJObject(Context context, JObject jObject)
        {

            var kvpModelTtlCounters = context.EntityAnalysisModel.Collections.ModelTtlCounters.Where(w => w.ResponsePayload).ToArray();
            if (!kvpModelTtlCounters.Any())
            {
                return;
            }

            var kvpModelTtlCountersArray = new JArray();
            jObject.Add("TtlCounter", kvpModelTtlCountersArray);
            foreach (var kvpModelTtlCounter in kvpModelTtlCounters)
            {
                var value = context.EntityAnalysisModelInstanceEntryPayload.TtlCounter.FirstOrDefault(f => f.Key == kvpModelTtlCounter.Name);
                if (value.Key != null)
                {
                    kvpModelTtlCountersArray.Add(JToken.FromObject(value));
                }
            }
        }

        private static void AddDictionaryToJObject(Context context, JObject jObject)
        {

            var kvpDictionaries = context.EntityAnalysisModel.Dependencies.KvpDictionaries.Where(w => w.Value.ResponsePayload).ToArray();
            if (kvpDictionaries.Length <= 0)
            {
                return;
            }

            var kvpDictionariesJArray = new JArray();
            jObject.Add("Dictionary", kvpDictionariesJArray);
            foreach (var kvpDictionary in kvpDictionaries)
            {
                var value = context.EntityAnalysisModelInstanceEntryPayload.Dictionary.FirstOrDefault(f => f.Key == kvpDictionary.Value.Name);
                if (value.Key != null)
                {
                    kvpDictionariesJArray.Add(JToken.FromObject(value));
                }
            }
        }

        private static void AddCreateCaseToJObject(Context context, JObject jObject)
        {

            if (context.EntityAnalysisModelInstanceEntryPayload.CreateCase != null)
            {
                jObject.Add("CreateCase", JToken.FromObject(context.EntityAnalysisModelInstanceEntryPayload.CreateCase));
            }
        }

        private static JObject CreateJObject(Context context)
        {

            var jObject = new JObject
            {
                {
                    "CreatedDate", context.EntityAnalysisModelInstanceEntryPayload.CreatedDate
                },
                {
                    "EntityAnalysisModelInstanceEntryGuid", context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid
                },
                {
                    "EntityInstanceEntryId", context.EntityAnalysisModelInstanceEntryPayload.EntityInstanceEntryId
                },
                {
                    "ReferenceDate", context.EntityAnalysisModelInstanceEntryPayload.ReferenceDate
                },
                {
                    "ResponseElevation", JToken.FromObject(context.EntityAnalysisModelInstanceEntryPayload.ResponseElevation)
                }
            };
            return jObject;
        }
    }
}
