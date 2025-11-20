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

namespace Jube.Test.Load
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    public class Load
    {
        private readonly Stopwatch swTotal = new Stopwatch();
        private int requests;

        [Theory]
        [InlineData("http://localhost:5001/api/invoke/EntityAnalysisModel/90c425fd-101a-420b-91d1-cb7a24a969cc",
            10000, 100000000, 10, 1,12)]
        public Task LoadTestAsync(string uriString, int httpTimeout, long iteration,
            int maxConnectionsPerServer, int timeDriftMs, int taskCount)
        {
            var txnId = 0;
            var random = new Random();
            var referenceDate = DateTime.Now.AddYears(-10);
            var uri = new Uri(uriString);
            var stringTemplate = Helpers.ReadFileContents("Load/Mock.json");
            
            var baseIterationsPerClient = iteration / taskCount;
            var remainder = iteration % taskCount;

            _ = Task.Run(() =>
            {
                _ = WriteTpsEstimatesAsync();
            });

            swTotal.Start();

            var tasks = new List<Task>();
            for (var clientIndex = 0; clientIndex < taskCount; clientIndex++)
            {
                var iterationsForThisClient = baseIterationsPerClient + (clientIndex == taskCount - 1 ? remainder : 0);

                tasks.Add(Task.Run(async () =>
                {
                    var clientHandler = new HttpClientHandler
                    {
                        MaxConnectionsPerServer = maxConnectionsPerServer,
                        ServerCertificateCustomValidationCallback = (_, _, _, _) => true
                    };

                    using var client = new HttpClient(clientHandler);
                    client.Timeout = TimeSpan.FromMilliseconds(httpTimeout);

                    for (var i = 0; i < iterationsForThisClient; i++)
                    {
                        var replacements = new Dictionary<string, string>
                        {
                            ["[@AccountId@]"] = random.NextInt64(1, 10000000).ToString(),
                            ["[@TxnId@]"] = Interlocked.Increment(ref txnId).ToString(),
                            ["[@TxnDateTime@]"] = referenceDate.AddMilliseconds(timeDriftMs).ToString("o")
                        };

                        var payload = replacements.Aggregate(stringTemplate, (current, kvp) => current.Replace(kvp.Key, kvp.Value));

                        await SendToJubeAndAwaitResponseAsync(payload, uri, client);

                        Interlocked.Increment(ref requests);
                    }
                }));
            }
            
            swTotal.Stop();

            return Task.WhenAll(tasks);
        }

        private static async Task SendToJubeAndAwaitResponseAsync(string stringReplaced, Uri uri, HttpClient client)
        {
            var stringContent = new StringContent(
                stringReplaced,
                Encoding.UTF8,
                "application/json");

            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = uri,
                Content = stringContent
            };

            var sw = new Stopwatch();
            sw.Start();
            await client.SendAsync(request, HttpCompletionOption.ResponseContentRead);
            sw.Stop();
        }

        private async Task WriteTpsEstimatesAsync(CancellationToken token = default)
        {
            try
            {
                var docPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                var outputFileTpsSnapshot = new StreamWriter(Path.Combine(docPath, "WriteLinesTpsSnapshot.txt"));
                outputFileTpsSnapshot.AutoFlush = true;

                while (!token.IsCancellationRequested)
                {
                    await Task.Delay(1000, token);

                    await outputFileTpsSnapshot.WriteLineAsync(
                        $"{Math.Round(swTotal.Elapsed.TotalSeconds)},{requests}");
                    requests = 0;
                }
            }
            catch (Exception)
            {
                //Not implemented
            }
        }
    }
}
