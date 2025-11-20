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

namespace Jube.Engine.EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntry
{
    using System.IO;
    using AsyncInvocationCallbackToken;
    using Newtonsoft.Json;

    public static class BuildJsonResponses
    {
        public static MemoryStream BuildJson(AsyncInvocationCallbackToken payload,
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

        public static MemoryStream BuildJson(EntityAnalysisModelInstanceEntryPayload payload,
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
    }
}
