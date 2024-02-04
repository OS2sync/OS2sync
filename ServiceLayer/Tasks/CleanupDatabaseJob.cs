using Organisation.SchedulingLayer;
using Quartz;
using System.Threading.Tasks;

namespace Organisation.ServiceLayer
{
    [DisallowConcurrentExecution]
    public class CleanupDatabaseJob : IJob
    {
        private static log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public async Task Execute(IJobExecutionContext context) => await Task.Run(() => 
        {
            log.Info("Executing Cleanup Database Job");

            var orgUnitDao = new OrgUnitDao();
            orgUnitDao.Cleanup();

            var userDao = new UserDao();
            userDao.Cleanup();

            log.Info("Finished executing Cleanup Database Job");
        });
    }
}