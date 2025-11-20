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

namespace Jube.App.Code
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;
    using Newtonsoft.Json;

    public class SendHttpEndpoint
    {
        public async Task SendAsync(string httpEndpoint, byte httpEndpointTypeId, Dictionary<string, string> values)
        {
            if (String.IsNullOrEmpty(httpEndpoint))
            {
                return;
            }

            var tokenization = new Tokenisation();
            var urlTokens = tokenization.ReturnTokens(httpEndpoint);

            var replacedUrl = httpEndpoint;
            foreach (var token in urlTokens)
            {
                if (values.TryGetValue(token, out var replacement))
                {
                    replacedUrl = replacedUrl.Replace($"[@{token}@]", replacement);
                }
            }

            using var client = new HttpClient();

            if (httpEndpointTypeId == 1)
            {
                var json = JsonConvert.SerializeObject(values);
                using var content = new StringContent(json, Encoding.UTF8, "application/json");

                using var response = await client.PostAsync(replacedUrl, content).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();

                await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            }
            else
            {
                using var response = await client.GetAsync(replacedUrl).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();

                await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            }
        }
    }
}
