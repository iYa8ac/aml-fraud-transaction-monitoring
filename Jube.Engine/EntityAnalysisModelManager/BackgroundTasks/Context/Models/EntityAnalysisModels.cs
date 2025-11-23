namespace Jube.Engine.EntityAnalysisModelManager.BackgroundTasks.Context.Models
{
    using System;
    using System.Collections.Generic;
    using EntityAnalysisModel;
    using EntityAnalysisModel.Models.Models;

    public class EntityAnalysisModels
    {
        public List<EntityAnalysisModelInlineScript> InlineScripts { get; } = [];
        public Dictionary<int, EntityAnalysisModel> ActiveEntityAnalysisModels { get; } = [];
        public bool EntityModelsHasLoadedForStartup { get; set; }
        public Guid EntityAnalysisInstanceGuid { get; set; }
    }
}
