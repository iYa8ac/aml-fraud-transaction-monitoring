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

// ReSharper disable CollectionNeverUpdated.Global

namespace Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel
{
    using Jube.Engine.Helpers;
    using Models;

    public class EntityAnalysisModel
    {
        public bool Started { get; set; }

        public JsonSerializationHelper JsonSerializationHelper { get; init; }

        public Instance Instance
        {
            get;
        } = new Instance();

        public Services Services
        {
            get;
        } = new Services();

        public Flags Flags
        {
            get;
        } = new Flags();

        public Collections Collections
        {
            get;
        } = new Collections();

        public Dependencies Dependencies
        {
            get;
        } = new Dependencies();

        public ConcurrentQueues ConcurrentQueues
        {
            get;
        } = new ConcurrentQueues();

        public Counters Counters
        {
            get;
        } = new Counters();

        public Cache Cache
        {
            get;
        } = new Cache();

        public References References
        {
            get;
        } = new References();
    }
}
