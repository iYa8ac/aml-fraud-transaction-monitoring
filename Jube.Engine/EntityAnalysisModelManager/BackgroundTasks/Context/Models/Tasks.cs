namespace Jube.Engine.EntityAnalysisModelManager.BackgroundTasks.Context.Models
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using TaskStarters;

    public class Tasks
    {
        public readonly List<Task> ArchiverTasks = [];
        public List<Task> PersistToActivationWatcherPollingTasks = [];
        public List<Task> ReprocessingAsyncTasks = [];
        public Task ModelSyncTask { get; set; }
        public Task AbstractionRuleCachingTask { get; set; }
        public Task TtlCounterAdministrationTask { get; set; }
        public Task CachePruneAsyncTask { get; set; }
    }
}
