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

namespace Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models
{
    using System;

    public class Counters
    {
        public double BillingResponseElevationBalance { get; set; }
        public double BillingResponseElevationCount { get; set; }
        public char MaxResponseElevationInterval { get; set; }
        public int MaxResponseElevationValue { get; set; }
        public int MaxResponseElevationThreshold { get; set; }
        public double MaxResponseElevation { get; set; }
        public int ModelInvokeCounter { get; set; }
        public int ModelInvokeGatewayCounter { get; set; }
        public int ModelResponseElevationCounter { get; set; }
        public double ModelResponseElevationSum { get; set; }
        public int BalanceLimitCounter { get; set; }
        public int ResponseElevationValueLimitCounter { get; set; }
        public int ResponseElevationFrequencyLimitCounter { get; set; }
        public int ResponseElevationValueGatewayLimitCounter { get; set; }
        public int ResponseElevationBillingSumLimitCounter { get; set; }
        public int ParentResponseElevationValueLimitCounter { get; set; }
        public int ParentBalanceLimitCounter { get; set; }
        public DateTime LastCountersChecked { get; set; }
        public DateTime LastCountersWritten { get; set; }
        public DateTime LastModelInvokeCountersWritten { get; set; }
        public int ActivationWatcherCount { get; set; }
        public int MaxActivationWatcherValue { get; set; }
        public double MaxActivationWatcherThreshold { get; set; }
        public char MaxActivationWatcherInterval { get; set; }
        public double ActivationWatcherSample { get; set; }
    }
}
