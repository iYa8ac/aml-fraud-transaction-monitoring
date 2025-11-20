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
    using System.Linq;
    using System.Threading.Tasks;
    using Data.Repository;
    using Parser;

    public static class ConfigureTokenParserForSecurityExtensions
    {
        public static async Task<Context> ConfigureTokenParserForSecurityAsync(this Context context)
        {
            try
            {
                var repository = new RuleScriptTokenRepository(context.Services.DbContext);
                var tokens = (await repository.GetAsync(context.Services.CancellationToken).ConfigureAwait(false)).Select(s => s.Token).ToList();

                if (context.Services.Log.IsInfoEnabled)
                {
                    context.Services.Log.Info($"Entity Start: Has fetched {tokens.Count} tokens.  Will construct and return the parser.");
                }

                context.Services.Parser = new Parser(context.Services.Log, tokens);

                if (context.Services.Log.IsInfoEnabled)
                {
                    context.Services.Log.Info("Entity Start: Starting soft code parser.");
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                context.Services.Log.Error($"ConfigureTokenParserForSecurityAsync: has produced an error {ex}");
            }

            return context;
        }
    }
}
