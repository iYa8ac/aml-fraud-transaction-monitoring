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

namespace Jube.Engine.BackgroundTasks.Context.Models
{
    using System;

    public class Counters
    {
        public int HttpCounterAllError { get; set; }
        public int HttpCounterAllRequests { get; set; }
        public int HttpCounterCallback { get; set; }
        public int HttpCounterExhaustive { get; set; }
        public int HttpCounterModel { get; set; }
        public int HttpCounterModelAsync { get; set; }
        public int HttpCounterSanction { get; set; }
        public int HttpCounterTag { get; set; }
        public int PendingCallbacksTimeoutCounter { get; set; }
        public DateTime LastBalanceCountersWritten { get; set; }
        public DateTime LastCallbackTimeout { get; set; }
        public DateTime LastHttpCountersWritten { get; set; }
    }
}
