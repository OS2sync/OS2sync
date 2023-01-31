using Quartz;
using Organisation.IntegrationLayer;
using Organisation.BusinessLayer;
using System;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;
using System.Text.Json;

namespace Organisation.SchedulingLayer
{
    [DisallowConcurrentExecution]
    public class SyncJob : IJob
    {
        private static log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private static DateTime nextRun = DateTime.MinValue;
        private static long errorCount = 0;

        public Task Execute(IJobExecutionContext context)
        {
            try
            {
                long userCount = 0, ouCount = 0;

                if (DateTime.Compare(DateTime.Now, nextRun) < 0)
                {
                    return Task.CompletedTask;
                }

                try
                {
                    log.Debug("Scheduler started synchronizing objects from queue");

                    HandleOUs(out ouCount);
                    HandleUsers(out userCount);
                }
                catch (Exception ex)
                {
                    errorCount++;

                    switch (errorCount)
                    {
                        case 1:
                            nextRun = DateTime.Now.AddMinutes(1);
                            break;
                        case 2:
                            nextRun = DateTime.Now.AddMinutes(3);
                            break;
                        default:
                            nextRun = DateTime.Now.AddMinutes(5);
                            break;
                    }

                    if (errorCount < 10)
                    {
                        log.Warn("Failed to run scheduler, sleeping until: " + nextRun.ToString("MM/dd/yyyy HH:mm"), ex);
                    }
                    else
                    {
                        log.Error("Failed to run scheduler, sleeping until: " + nextRun.ToString("MM/dd/yyyy HH:mm"), ex);
                    }
                }

                if (userCount > 0 || ouCount > 0)
                {
                    log.Info("Scheduler completed " + userCount + " user(s) and " + ouCount + " ou(s)");
                }
            }
            catch (Exception ex)
            {
                log.Error("Something unexpected happened during execution", ex);
            }

            return Task.CompletedTask;
        }

        public static void HandleUsers(out long count)
        {
            UserService service = new UserService();
            UserDao dao = new UserDao();
            count = 0;

            var users = dao.Get4OldestEntries();

            // remove duplicates
            users = users.GroupBy(u => u.Uuid)
                .Select(duplicateUsers => {
                    // order users by Id desc
                    var users = duplicateUsers.OrderByDescending(user => user.Id);
                    
                    // remove all but first
                    users.Skip(1).ToList().ForEach(u => dao.Delete(u.Id));
                    
                    // return first
                    return users.First();
                }).ToList();

            do
            {
                if (users.Count > 0)
                {
                    int subCounter = 0;

                    Parallel.ForEach(users, (user) => {
                        using (var cancelTokenSource = new CancellationTokenSource())
                        {
                            var cancelToken = cancelTokenSource.Token;
                            var task = Task.Run(() => HandleUser(user, service, dao), cancelToken);
                            if (task.Wait(TimeSpan.FromSeconds(300)))
                            {
                                int result = task.Result;

                                switch (result)
                                {
                                    case -1:
                                        // we are not increment subcounter here, as this is a temporary failure, and we should sleep
                                        break;
                                    case -2:
                                        // bad data, it was logged and then throw away, nothing to see here, move along
                                        lock (users)
                                        {
                                            subCounter++;
                                        }
                                        break;
                                    default:
                                        lock (users)
                                        {
                                            subCounter++;
                                        }
                                        errorCount = 0;
                                        break;
                                }
                            }
                            else
                            {
                                log.Warn("Timeout happended while waiting for synchronization of user: " + user.Uuid);

                                cancelTokenSource.Cancel();
                            }
                        }
                    });

                    count += subCounter;
                    if (subCounter != users.Count) {
                        throw new TemporaryFailureException();
                    }

                    users = dao.Get4OldestEntries();
                }
            } while (users.Count > 0);
        }

        public static int HandleUser(UserRegistrationExtended user, UserService service, UserDao dao)
        {
            try
            {
                OrganisationRegistryProperties.SetCurrentMunicipality(user.Cvr);

                if (user.Operation.Equals(OperationType.DELETE))
                {
                    service.Delete(user.Uuid, user.Timestamp);
                }
                else
                {
                    bool identicalToLastSuccess = false;
                    if (user.Operation.Equals(OperationType.UPDATE))
                    {

                        // check for no changes
                        var succesUsers = dao.GetSuccessEntries(user.Uuid);
                        var userToCompare = succesUsers.Where(user => user.Operation.Equals(OperationType.UPDATE)).OrderBy(user => user.Id).LastOrDefault();
                        if (userToCompare != null)
                        {
                            string jsonUserToCompare = JsonSerializer.Serialize(userToCompare);
                            string jsonUser = JsonSerializer.Serialize(user);
                            identicalToLastSuccess = jsonUserToCompare.Equals(jsonUser);
                        }
                    }

                    if (identicalToLastSuccess)
                    {
                        log.Debug("Skipping user update for '" + user.Uuid + "' because it was identical to last known successful update");
                    }
                    else
                    {
                        service.Update(user);
                    }
                }
                dao.OnSuccess(user.Id);
                dao.Delete(user.Id);

                return 0;
            }
            catch (TemporaryFailureException ex)
            {
                log.Warn("Could not handle user '" + user.Uuid + "' at the moment, will try later", ex);

                return -1;
            }
            catch (Exception ex)
            {
                log.Error("Could not handle user '" + user.Uuid + "'", ex);
                dao.OnFailure(user.Id, ex.Message);
                dao.Delete(user.Id);

                return -2;
            }
        }

        public static void HandleOUs(out long count)
        {
            OrgUnitService service = new OrgUnitService();
            OrgUnitDao dao = new OrgUnitDao();
            count = 0;

            var orgUnits = dao.Get4OldestEntries();

            // remove duplicates
            orgUnits = orgUnits.GroupBy(u => u.Uuid)
                .Select(duplicateOUs => {
                    // order orgUnits by Id desc
                    var ous = duplicateOUs.OrderByDescending(user => user.Id);
                    
                    // remove all but first
                    ous.Skip(1).ToList().ForEach(ou => dao.Delete(ou.Id));
                    
                    // return first
                    return ous.First();
                }).ToList();

            do {
                if (orgUnits.Count > 0)
                {
                    int subCounter = 0;
                        
                    Parallel.ForEach(orgUnits, (orgUnit) =>
                    {
                        using (var cancelTokenSource = new CancellationTokenSource())
                        {
                            var cancelToken = cancelTokenSource.Token;
                            var task = Task.Run(() => HandleOU(orgUnit, service, dao), cancelToken);

                            if (task.Wait(TimeSpan.FromSeconds(300)))
                            {
                                int result = task.Result;

                                switch (result)
                                {
                                    case -1:
                                        // we are not increment subcounter here, as this is a temporary failure, and we should sleep
                                        break;
                                    case -2:
                                        // bad data, it was logged and then throw away, nothing to see here, move along
                                        lock (orgUnits)
                                        {
                                            subCounter++;
                                        }
                                        break;
                                    default:
                                        lock (orgUnits)
                                        {
                                            subCounter++;
                                        }
                                        errorCount = 0;
                                        break;
                                }
                            }
                            else
                            {
                                log.Warn("Timeout happended while waiting for synchronization of OrgUnit: " + orgUnit.Uuid);

                                cancelTokenSource.Cancel();
                            }
                        }
                    });

                    count += subCounter;
                    if (subCounter != orgUnits.Count) {
                        throw new TemporaryFailureException();
                    }

                    orgUnits = dao.Get4OldestEntries();
                }
            } while (orgUnits.Count > 0);
        }

        private static int HandleOU(OrgUnitRegistrationExtended ou, OrgUnitService service, OrgUnitDao dao)
        {
            try
            {
                OrganisationRegistryProperties.SetCurrentMunicipality(ou.Cvr);

                if (ou.Operation.Equals(OperationType.DELETE))
                {
                    service.Delete(ou.Uuid, ou.Timestamp);
                }
                else
                {
                    bool identicalToLastSuccess = false;
                    if (ou.Operation.Equals(OperationType.UPDATE))
                    {
                        // check for no changes
                        var succesOus = dao.GetSuccessEntries(ou.Uuid);
                        var ouToCompare = succesOus.Where(ou => ou.Operation.Equals(OperationType.UPDATE)).OrderBy(ou => ou.Id).LastOrDefault();
                        if (ouToCompare != null)
                        {
                            string jsonOUToCompare = JsonSerializer.Serialize(ouToCompare);
                            string jsonOU = JsonSerializer.Serialize(ou);
                            identicalToLastSuccess = jsonOUToCompare.Equals(jsonOU);
                        }

                    }
                    if (identicalToLastSuccess)
                    {
                        log.Debug("Skipping ou update for '" + ou.Uuid + "' because it was identical to last known successful update");
                    }
                    else
                    {
                        service.Update(ou);
                    }
                    
                }

                dao.OnSuccess(ou.Id);
                dao.Delete(ou.Id);

                return 0;
            }
            catch (TemporaryFailureException ex)
            {
                log.Warn("Could not handle ou '" + ou.Uuid + "' at the moment, will try later", ex);
                return -1;
            }
            catch (Exception ex)
            {
                log.Error("Could not handle ou '" + ou.Uuid + "'", ex);
                dao.OnFailure(ou.Id, ex.Message);
                dao.Delete(ou.Id);

                return -2;
            }
        }
    }
}
