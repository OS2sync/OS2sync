using Quartz;
using System.Threading.Tasks;

namespace Organisation.ServiceLayer
{
    [DisallowConcurrentExecution]
    public class CleanupJob : IJob
    {
        private static log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public async Task Execute(IJobExecutionContext context) => await Task.Run(() => 
        {
            HierarchyController.Cleanup();
        });
    }
}