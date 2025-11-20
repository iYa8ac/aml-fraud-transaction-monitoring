namespace Jube.Data.Query
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using log4net;
    using Npgsql;

    public class GetArchiveDistinctEntryKeyValue(ILog log, string connectionString)
    {
        public async Task<List<string>> ExecuteAsync(Guid entityAnalysisModelGuid,
            string key, DateTime dateFrom, DateTime dateTo, CancellationToken token = default)
        {
            var connection = new NpgsqlConnection(connectionString);
            var value = new List<string>();
            try
            {
                await connection.OpenAsync(token).ConfigureAwait(false);

                const string sql = "select distinct \"Json\" -> 'payload' ->> (@key)" +
                                   " from \"Archive\" a inner join \"EntityAnalysisModel\" e on a.\"EntityAnalysisModelId\" = e.\"Id\"" +
                                   " where e.\"Guid\" = (@entityAnalysisModelGuid)" +
                                   " and \"Json\" -> 'payload' ->> (@key) = (@value)" +
                                   " and \"CreatedDate\" > (@dateFrom) and \"CreatedDate\" < (@dateTo)";

                var command = new NpgsqlCommand(sql);
                command.Connection = connection;
                command.Parameters.AddWithValue("key", key);
                command.Parameters.AddWithValue("dateFrom", dateFrom);
                command.Parameters.AddWithValue("dateTo", dateTo);
                command.Parameters.AddWithValue("entityAnalysisModelGuid", entityAnalysisModelGuid);
                await command.PrepareAsync(token).ConfigureAwait(false);

                var reader = await command.ExecuteReaderAsync(token).ConfigureAwait(false);
                while (await reader.ReadAsync(token).ConfigureAwait(false))
                {
                    if (!await reader.IsDBNullAsync(0, token))
                    {
                        value.Add(reader.GetValue(0).ToString());
                    }
                }

                await reader.CloseAsync().ConfigureAwait(false);
                await reader.DisposeAsync().ConfigureAwait(false);
                await command.DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                log.Error($"Archive SQL: Has created an exception as {ex}.");
            }
            finally
            {
                await connection.CloseAsync().ConfigureAwait(false);
                await connection.DisposeAsync().ConfigureAwait(false);
            }

            return value;
        }

        public async Task<List<string>> ExecuteAsync(Guid entityAnalysisModelGuid,
            string key, CancellationToken token = default)
        {
            var connection = new NpgsqlConnection(connectionString);
            var value = new List<string>();
            try
            {
                await connection.OpenAsync(token).ConfigureAwait(false);

                const string sql = "select distinct \"Json\" -> 'payload' ->> (@key)" +
                                   " from \"Archive\" a inner join \"EntityAnalysisModel\" e on a.\"EntityAnalysisModelId\" = e.\"Id\"" +
                                   " where e.\"Guid\" = (@entityAnalysisModelGuid)";

                var command = new NpgsqlCommand(sql);
                command.Connection = connection;
                command.Parameters.AddWithValue("entityAnalysisModelGuid", entityAnalysisModelGuid);
                command.Parameters.AddWithValue("key", key);
                await command.PrepareAsync(token).ConfigureAwait(false);

                var reader = await command.ExecuteReaderAsync(token).ConfigureAwait(false);
                while (await reader.ReadAsync(token).ConfigureAwait(false))
                {
                    if (!await reader.IsDBNullAsync(0, token))
                    {
                        value.Add(reader.GetValue(0).ToString());
                    }
                }

                await reader.CloseAsync().ConfigureAwait(false);
                await reader.DisposeAsync().ConfigureAwait(false);
                await command.DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                log.Error($"Archive SQL: Has created an exception as {ex}.");
            }
            finally
            {
                await connection.CloseAsync().ConfigureAwait(false);
                await connection.DisposeAsync().ConfigureAwait(false);
            }

            return value;
        }
    }
}
