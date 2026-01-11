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

namespace Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Context.Extensions
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using Attributes;
    using Data.Repository;
    using Interfaces;
    using Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models.Models;
    using Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models.Models.EntityAnalysisModelInlineScript;
    using Jube.Engine.EntityAnalysisModelManager.Helpers;
    using Parser.Compiler;

    public static class SyncEntityAnalysisInlineScriptsExtensions
    {
        public static async Task<Context> SyncEntityAnalysisInlineScriptAsync(this Context context)
        {
            if (context.Services.Log.IsDebugEnabled)
            {
                context.Services.Log.Debug("Entity Start: Getting all Inline Scripts from Database.");
            }

            var repository = new EntityAnalysisInlineScriptRepository(context.Services.DbContext);

            if (context.Services.Log.IsDebugEnabled)
            {
                context.Services.Log.Debug(
                    "Entity Start: Executing EntityAnalysisInlineScriptRepository.Get.");
            }

            var records = await repository.GetAsync();

            foreach (var record in records)
            {
                try
                {
                    if (context.Services.Log.IsDebugEnabled)
                    {
                        context.Services.Log.Debug(
                            $"Entity Start: Found an inline script with the id of {record.Id} and will proceed to check if already have this inline script available.");
                    }

                    var inlineScript = context.EntityAnalysisModels.InlineScripts.Find(x => x.Id == record.Id);
                    if (inlineScript == null)
                    {
                        inlineScript = new EntityAnalysisModelInlineScript();

                        if (context.Services.Log.IsDebugEnabled)
                        {
                            context.Services.Log.Debug(
                                $"Entity Start: Have not found an inline script in the available inline scripts, with the id of {record.Id} hence a new one will be created.");
                        }
                    }
                    else
                    {
                        if (context.Services.Log.IsDebugEnabled)
                        {
                            context.Services.Log.Debug(
                                $"Entity Start: Found an inline script in the available inline scripts, with the id of {record.Id} and this will be used.");
                        }
                    }

                    if (context.Services.Log.IsDebugEnabled)
                    {
                        context.Services.Log.Debug(
                            $"Entity Start: Found an inline script {record.Id} has Created Date: {inlineScript.CreatedDate.HasValue} of {inlineScript.CreatedDate}. A check will be made to see if it has changed recently");
                    }

                    if ((!inlineScript.CreatedDate.HasValue ||
                         !(Convert.ToDateTime(record.CreatedDate) > inlineScript.CreatedDate)) &&
                        inlineScript.CreatedDate.HasValue)
                    {
                        continue;
                    }

                    if (context.Services.Log.IsDebugEnabled)
                    {
                        context.Services.Log.Debug(
                            $"Entity Start: Inline Script {record.Id} has changed recently or is new,  setting created date.");
                    }

                    inlineScript.CreatedDate = record.CreatedDate;
                    inlineScript.Id = record.Id;
                    inlineScript.InlineScriptCode = record.Code;
                    inlineScript.LanguageId = record.LanguageId ?? 1;

                    if (context.Services.Log.IsDebugEnabled)
                    {
                        context.Services.Log.Debug(
                            $"Entity Start: Inline Script {record.Id} has rule script of {inlineScript.InlineScriptCode}.");
                    }

                    if (context.Services.Log.IsDebugEnabled)
                    {
                        context.Services.Log.Debug(
                            $"Entity Start: Inline Script {record.Id} has method specification of {inlineScript.ClassName}.");
                    }

                    inlineScript.Name = record.Name;

                    if (context.Services.Log.IsDebugEnabled)
                    {
                        context.Services.Log.Debug($"Entity Start: Inline Script {record.Id} has name of {inlineScript.Name}.");
                    }

                    var dependencyArray = AppDomain.CurrentDomain.GetAssemblies()
                        .Where(a => !a.IsDynamic && !String.IsNullOrEmpty(a.Location))
                        .Select(a => a.Location)
                        .ToArray();

                    if (context.Services.Log.IsDebugEnabled)
                    {
                        context.Services.Log.Debug(
                            $"Entity Start: Inline Script {record.Id} is being checked for dll dependencies.");
                    }

                    if (!String.IsNullOrEmpty(record.Dependency))
                    {
                        if (context.Services.Log.IsDebugEnabled)
                        {
                            context.Services.Log.Debug(
                                $"Entity Start: Inline Script {record.Id} has dll dependency specification of {record.Dependency}.");
                        }

                        inlineScript.Dependencies = record.Dependency;

                        foreach (var file in inlineScript.Dependencies.Split(",".ToCharArray()))
                        {
                            Array.Resize(ref dependencyArray, dependencyArray.Length + 1);
                            if (File.Exists(Path.Combine(context.Paths.BinaryPath, file)))
                            {
                                dependencyArray[^1] = Path.Combine(context.Paths.BinaryPath, file);
                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Added Inline Script Dependency at binary level {dependencyArray[^1]} for inline script {record.Id}.");
                                }
                            }
                            else
                            {
                                dependencyArray[^1] = Path.Combine(context.Paths.FrameworkPath, file);
                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Added Inline Script Dependency at framework level {dependencyArray[^1]} for inline script {record.Id}.");
                                }
                            }
                        }
                    }

                    var inlineScriptHash = HashHelper.GetHash(inlineScript.InlineScriptCode);

                    if (context.Services.Log.IsDebugEnabled)
                    {
                        context.Services.Log.Debug(
                            $"Entity Start: Inline Script {record.Id} has been hashed to {inlineScriptHash} and the hash cache will now be checked.");
                    }

                    if (context.Caching.HashCacheAssembly.TryGetValue(inlineScriptHash, out var value))
                    {
                        if (context.Services.Log.IsDebugEnabled)
                        {
                            context.Services.Log.Debug(
                                $"Entity Start: Inline Script {record.Id} has been hashed to {inlineScriptHash} and has been located in the hash cache,  this will be used.  Creating a delegate.");
                        }

                        inlineScript.InlineScriptCompile = value;
                        inlineScript.InlineScriptType =
                            inlineScript.InlineScriptCompile.GetType(inlineScript.ClassName);
                        inlineScript.PreProcessingMethodInfo =
                            inlineScript.InlineScriptType.GetMethod("ExecuteAsync",
                                [typeof(EntityAnalysisModelInvoke.Context.Context)]);

                        if (context.Services.Log.IsDebugEnabled)
                        {
                            context.Services.Log.Debug(
                                $"Entity Start: Inline Script {record.Id} has been hashed to {inlineScriptHash} and has been located in the hash cache and allocated to a delegate.");
                        }
                    }
                    else
                    {
                        bool compiled;

                        if (context.Services.Log.IsDebugEnabled)
                        {
                            context.Services.Log.Debug(
                                $"Entity Start: Inline Script {record.Id} has been hashed to {inlineScriptHash} but has not been located in the hash cache.  The inline script will now be compiled.");
                        }

                        var compile = new Compile();
                        compile.CompileCode(inlineScript.InlineScriptCode, context.Services.Log,
                            dependencyArray,
                            inlineScript.LanguageId == 2 ? Compile.Language.CSharp : Compile.Language.Vb);

                        if (context.Services.Log.IsDebugEnabled)
                        {
                            context.Services.Log.Debug(
                                $"Entity Start: Inline Script {record.Id} has been compiled with {compile.Errors} errors.");
                        }

                        if (compile.Errors == null)
                        {
                            if (context.Services.Log.IsDebugEnabled)
                            {
                                context.Services.Log.Debug(
                                    $"Entity Start: Inline Script {record.Id} has been compiled with no errors and will now be allocated to a delegate.");
                            }

                            inlineScript.InlineScriptCompile = compile.CompiledAssembly;

                            var interfaceType = typeof(IInlineScript);
                            var implementations = inlineScript.InlineScriptCompile.GetExportedTypes()
                                .Where(t => interfaceType.IsAssignableFrom(t) && t.IsClass && !t.IsAbstract);

                            foreach (var type in implementations)
                            {
                                inlineScript.ClassName = type.FullName;
                                break;
                            }

                            inlineScript.InlineScriptType =
                                inlineScript.InlineScriptCompile.GetType(inlineScript.ClassName);

                            if (inlineScript.InlineScriptType == null)
                            {
                                context.Services.Log.Error(
                                    $"Entity Start: Could not compile inline script: {inlineScript.Id} did not fine class entry point for {inlineScript.ClassName}.");

                                continue;
                            }

                            inlineScript.PreProcessingMethodInfo =
                                inlineScript.InlineScriptType.GetMethod("ExecuteAsync",
                                    [typeof(EntityAnalysisModelInvoke.Context.Context)]);

                            if (inlineScript.PreProcessingMethodInfo == null)
                            {
                                context.Services.Log.Error(
                                    $"Entity Start: Could not compile inline script: {inlineScript.Id} did not find method entry point for ExecuteAsync.");

                                continue;
                            }

                            context.EntityAnalysisModels.InlineScripts.Add(inlineScript);
                            context.Caching.HashCacheAssembly.Add(inlineScriptHash, compile.CompiledAssembly);
                            compiled = true;

                            if (context.Services.Log.IsDebugEnabled)
                            {
                                context.Services.Log.Debug(
                                    $"Entity Start: Inline Script {record.Id} has been compiled and allocated to a delegate.");
                            }
                        }
                        else
                        {
                            foreach (var error in compile.Errors)
                            {
                                context.Services.Log.Error(
                                    $"Entity Start: Could not compile inline script: {inlineScript.Id} with error: {error.ToString()}.");
                            }

                            compiled = false;
                        }

                        if (!compiled)
                        {
                            continue;
                        }

                        if (context.Services.Log.IsDebugEnabled)
                        {
                            context.Services.Log.Debug(
                                $"Entity Start: Inline Script {record.Id} has been compiled will now proceed to inspect the properties exposed by the class.");
                        }

                        foreach (var p in inlineScript.InlineScriptType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
                        {
                            var entityAnalysisModelInlineScriptProperty = new EntityAnalysisModelInlineScriptPropertyAttribute
                            {
                                Name = p.Name,
                                ReportTable = p.GetCustomAttribute<ReportTable>() != null,
                                Latitude = p.GetCustomAttribute<Latitude>() != null,
                                Longitude = p.GetCustomAttribute<Longitude>() != null,
                                ResponsePayload = p.GetCustomAttribute<ResponsePayload>() != null
                            };

                            var propertyType = Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType;

                            entityAnalysisModelInlineScriptProperty.DataTypeId = propertyType switch
                            {
                                not null when propertyType == typeof(string) => 1,
                                not null when propertyType == typeof(int) => 2,
                                not null when propertyType == typeof(byte) => 3,
                                not null when propertyType == typeof(double) => 4,
                                not null when propertyType == typeof(DateTime) => 5,
                                not null when propertyType == typeof(bool) => 3,
                                _ => entityAnalysisModelInlineScriptProperty.DataTypeId
                            };

                            if (context.Services.Log.IsDebugEnabled)
                            {
                                context.Services.Log.Debug(
                                    $"Entity Start: Inline Script {record.Id} is inspecting property {p.Name} and looking for custom attributes.");
                            }

                            var searchKeyAttribute = p.GetCustomAttribute<SearchKey>();

                            if (searchKeyAttribute != null)
                            {
                                var distinctSearchKey = new DistinctSearchKey
                                {
                                    SearchKey = p.Name,
                                    SearchKeyTtlInterval = searchKeyAttribute.SearchKeyTtlInterval,
                                    SearchKeyTtlIntervalValue = searchKeyAttribute.SearchKeyTtlIntervalValue,
                                    SearchKeyFetchLimit = searchKeyAttribute.SearchKeyFetchLimit,
                                    SearchKeyCache = searchKeyAttribute.SearchKeyCache,
                                    SearchKeyCacheInterval = searchKeyAttribute.SearchKeyCacheInterval,
                                    SearchKeyCacheValue = searchKeyAttribute.SearchKeyCacheValue,
                                    SearchKeyCacheSample = searchKeyAttribute.SearchKeyCacheSample,
                                    SearchKeyCacheFetchLimit = searchKeyAttribute.SearchKeyFetchLimit,
                                    SearchKeyCacheTtlInterval = searchKeyAttribute.SearchKeyCacheTtlInterval,
                                    SearchKeyCacheTtlValue = searchKeyAttribute.SearchKeyCacheTtlValue
                                };

                                inlineScript.GroupingKeys.Add(distinctSearchKey);
                                entityAnalysisModelInlineScriptProperty.SearchKey = distinctSearchKey;
                            }

                            inlineScript.EntityAnalysisModelInlineScriptPropertyAttributes.Add(p.Name, entityAnalysisModelInlineScriptProperty);

                            if (context.Services.Log.IsDebugEnabled)
                            {
                                context.Services.Log.Debug(
                                    $"Entity Start: Inline Script {record.Id} is inspecting property {p.Name} and has found a Search Key and is adding the grouping key to the model.");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    context.Services.Log.Error(
                        $"Entity Start: Inline script with the id of {record.Id} has created an error {ex}.");
                }
            }

            if (context.Services.Log.IsDebugEnabled)
            {
                context.Services.Log.Debug("Entity Start:  Completed creating Inline Scripts and closed the reader.");
            }

            return context;
        }
    }
}
