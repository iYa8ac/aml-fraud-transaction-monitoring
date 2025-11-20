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
    using System.Threading.Tasks;
    using CompilerUtilities;
    using Data.Repository;
    using Dictionary;
    using Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models.Models;
    using Jube.Engine.EntityAnalysisModelManager.Helpers;
    using log4net;

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

                    var inlineScript = context.EntityAnalysisModels.InlineScripts.Find(x => x.InlineScriptId == record.Id);
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
                    inlineScript.InlineScriptId = record.Id;
                    inlineScript.InlineScriptCode = record.Code;

                    if (context.Services.Log.IsDebugEnabled)
                    {
                        context.Services.Log.Debug(
                            $"Entity Start: Inline Script {record.Id} has rule script of {inlineScript.InlineScriptCode}.");
                    }

                    inlineScript.MethodName = record.MethodName;

                    if (context.Services.Log.IsDebugEnabled)
                    {
                        context.Services.Log.Debug(
                            $"Entity Start: Inline Script {record.Id} has method specification of {inlineScript.MethodName}.");
                    }

                    inlineScript.ClassName = record.ClassName;

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

                    var dependencyArray = new string[2];
                    dependencyArray[0] = Path.Combine(context.Paths.BinaryPath, "log4net.dll");
                    dependencyArray[1] = Path.Combine(context.Paths.BinaryPath, "Jube.Dictionary.dll");

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
                            inlineScript.InlineScriptType.GetMethod(inlineScript.MethodName,
                                [typeof(DictionaryNoBoxing), typeof(ILog)]);

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

                        var compile = new CompileUtility();
                        compile.CompileCode(inlineScript.InlineScriptCode, context.Services.Log, dependencyArray);

                        if (context.Services.Log.IsDebugEnabled)
                        {
                            context.Services.Log.Debug(
                                $"Entity Start: Inline Script {record.Id} has been compiled with {compile.Errors} errors.");
                        }

                        if (compile.Errors == 0)
                        {
                            if (context.Services.Log.IsDebugEnabled)
                            {
                                context.Services.Log.Debug(
                                    $"Entity Start: Inline Script {record.Id} has been compiled with no errors and will now be allocated to a delegate.");
                            }

                            inlineScript.InlineScriptCompile = compile.CompiledAssembly;
                            inlineScript.InlineScriptType =
                                inlineScript.InlineScriptCompile.GetType(inlineScript.ClassName);
                            inlineScript.PreProcessingMethodInfo =
                                inlineScript.InlineScriptType.GetMethod(inlineScript.MethodName,
                                    [typeof(DictionaryNoBoxing), typeof(ILog)]);

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
                            if (context.Services.Log.IsDebugEnabled)
                            {
                                context.Services.Log.Debug(
                                    $"Entity Start: Could not compile inline script: {inlineScript.InlineScriptCode}.");
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

                        var searchKeyAttributesPropertyInfo = inlineScript.InlineScriptCompile.GetTypes()
                            .SelectMany(t => t.GetProperties()).ToArray();
                        foreach (var propertyInfoWithinLoop in searchKeyAttributesPropertyInfo)
                        {
                            if (context.Services.Log.IsDebugEnabled)
                            {
                                context.Services.Log.Debug(
                                    $"Entity Start: Inline Script {record.Id} is inspecting property {propertyInfoWithinLoop.Name} and looking for custom attributes.");
                            }

                            foreach (var customAttributeDataWithinLoop in propertyInfoWithinLoop
                                         .CustomAttributes)
                            {
                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Inline Script {record.Id} is inspecting property {propertyInfoWithinLoop.Name} and is inspecting custom attribute {customAttributeDataWithinLoop.AttributeType.Name}.");
                                }

                                switch (customAttributeDataWithinLoop.AttributeType.Name)
                                {
                                    case "SearchKey":
                                    {
                                        if (context.Services.Log.IsDebugEnabled)
                                        {
                                            context.Services.Log.Debug(
                                                $"Entity Start: Inline Script {record.Id} is inspecting property {propertyInfoWithinLoop.Name} and has found a Search Key.");
                                        }

                                        var groupingKey = new DistinctSearchKey
                                        {
                                            SearchKey = propertyInfoWithinLoop.Name
                                        };

                                        foreach (var customAttributeNamedArgument in
                                                 customAttributeDataWithinLoop
                                                     .NamedArguments)
                                        {
                                            var customAttributeTypedArgument =
                                                customAttributeNamedArgument.TypedValue;
                                            switch (customAttributeNamedArgument.MemberName)
                                            {
                                                case "CacheKey":
                                                    if (context.Services.Log.IsDebugEnabled)
                                                    {
                                                        context.Services.Log.Debug(
                                                            $"Entity Start: Inline Script {record.Id} is inspecting property {propertyInfoWithinLoop.Name} and has found a Search Key, CacheKey with value of {customAttributeTypedArgument.Value}.");
                                                    }

                                                    groupingKey.SearchKeyCache =
                                                        Convert.ToBoolean(customAttributeTypedArgument.Value);
                                                    break;
                                                case "CacheKeyIntervalType":
                                                    if (context.Services.Log.IsDebugEnabled)
                                                    {
                                                        context.Services.Log.Debug(
                                                            $"Entity Start: Inline Script {record.Id} is inspecting property {propertyInfoWithinLoop.Name} and has found a Search Key, CacheKeyIntervalType with value of {customAttributeTypedArgument.Value}.");
                                                    }

                                                    groupingKey.SearchKeyCacheIntervalType =
                                                        customAttributeTypedArgument.Value.ToString();
                                                    break;
                                                case "CacheKeyIntervalValue":
                                                    if (context.Services.Log.IsDebugEnabled)
                                                    {
                                                        context.Services.Log.Debug(
                                                            $"Entity Start: Inline Script {record.Id} is inspecting property {propertyInfoWithinLoop.Name} and has found a Search Key, CacheKeyIntervalValue with value of {customAttributeTypedArgument.Value}.");
                                                    }

                                                    groupingKey.SearchKeyCacheIntervalValue =
                                                        Convert.ToInt32(customAttributeTypedArgument.Value);
                                                    break;
                                            }
                                        }

                                        inlineScript.GroupingKeys.Add(groupingKey);

                                        if (context.Services.Log.IsDebugEnabled)
                                        {
                                            context.Services.Log.Debug(
                                                $"Entity Start: Inline Script {record.Id} is inspecting property {propertyInfoWithinLoop.Name} and has found a Search Key and is adding the grouping key to the model.");
                                        }

                                        break;
                                    }
                                    case "ReportTable":
                                    {
                                        var columnName = propertyInfoWithinLoop.Name;
                                        int columnType;

                                        if (propertyInfoWithinLoop.PropertyType == typeof(string))
                                        {
                                            columnType = 1;
                                        }

                                        else if (propertyInfoWithinLoop.PropertyType == typeof(int))
                                        {
                                            columnType = 2;
                                        }

                                        else if (propertyInfoWithinLoop.PropertyType == typeof(double))
                                        {
                                            columnType = 3;
                                        }

                                        else if (propertyInfoWithinLoop.PropertyType ==
                                                 typeof(DateTime))
                                        {
                                            columnType = 4;
                                        }

                                        else if (propertyInfoWithinLoop.PropertyType == typeof(bool))
                                        {
                                            columnType = 5;
                                        }

                                        else
                                        {
                                            columnType = 1;
                                        }

                                        if (!inlineScript.PromoteReportTableColumns.ContainsKey(
                                                "ColumnName"))
                                        {
                                            inlineScript.PromoteReportTableColumns.Add(columnName,
                                                columnType);
                                        }

                                        break;
                                    }
                                }
                            }
                        }

                        inlineScript.ActivatedObject =
                            Activator.CreateInstance(inlineScript.InlineScriptType, context.Services.Log);

                        if (context.Services.Log.IsDebugEnabled)
                        {
                            context.Services.Log.Debug(
                                $"Entity Start: Inline Script {record.Id} has been created and the method referenced.");
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
