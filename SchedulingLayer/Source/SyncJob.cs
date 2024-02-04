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
        private enum SyncResult { Processed, TemporaryFailure, SkippedDueToCache, PermanentFailed }

        private static log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private static DateTime nextRun = DateTime.MinValue;
        private static long errorCount = 0;

        public Task Execute(IJobExecutionContext context)
        {
            try
            {
                if (DateTime.Compare(DateTime.Now, nextRun) < 0)
                {
                    return Task.CompletedTask;
                }

                try
                {
                    log.Debug("Scheduler started synchronizing objects from queue");

                    // empties the entire queue before moving to users
                    HandleOUs();

                    // handles 100 users max, before looping (so it logs progress periodically, but also so we ensure that ous are handled,
                    // even when we are synchronising 10.000+ users
                    HandleUsers();
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
            }
            catch (Exception ex)
            {
                log.Error("Something unexpected happened during execution", ex);
            }

            return Task.CompletedTask;
        }

        public static void HandleUsers()
        {
            UserService service = new UserService();
            UserDao dao = new UserDao();
            int transferCount = 0, failureCount = 0, cacheCount = 0;
            bool temporaryFailure = false;
            int totalCount = 0;

            var users = dao.GetOldestEntries();
            while (users.Count > 0)
            {
                totalCount += users.Count;

                Parallel.ForEach(users, (user) => {
                    using (var cancelTokenSource = new CancellationTokenSource())
                    {
                        var cancelToken = cancelTokenSource.Token;

                        var task = Task.Run(() => HandleUser(user, service, dao), cancelToken);

                        if (task.Wait(TimeSpan.FromSeconds(300)))
                        {
                            switch (task.Result)
                            {
                                case SyncResult.TemporaryFailure:
                                    temporaryFailure = true;
                                    break;
                                case SyncResult.PermanentFailed:
                                    // bad data, it was logged and then throw away, nothing to see here, move along
                                    lock (users)
                                    {
                                        failureCount++;
                                    }
                                    break;
                                case SyncResult.SkippedDueToCache:
                                    lock (users)
                                    {
                                        cacheCount++;
                                    }
                                    // note we do not zero errorCount, as we did not actually call FK Organisation
                                    break;
                                case SyncResult.Processed:
                                    lock (users)
                                    {
                                        transferCount++;
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

                if (temporaryFailure)
                {
                    break;
                }

                // handle 100 users at a time, then restart global loop (so we log periodically, and can skip to OU's)
                if (totalCount >= 100)
                {
                    break;
                }

                users = dao.GetOldestEntries();
            }

            if (transferCount > 0 || failureCount > 0 || cacheCount > 0)
            {
                int total = transferCount + failureCount + cacheCount;

                log.Info("Processed " + total + " user(s): success=" + transferCount + ", failure=" + failureCount + ", cacheSkip=" + cacheCount);
            }

            // indicate to main control that we should sleep a short while before trying again
            if (temporaryFailure)
            {
                throw new TemporaryFailureException();
            }
        }

        private static SyncResult HandleUser(UserRegistrationExtended user, UserService service, UserDao dao)
        {
            try
            {
                bool identicalToLastSuccess = false;
                OrganisationRegistryProperties.SetCurrentMunicipality(user.Cvr);

                if (user.Operation.Equals(OperationType.DELETE))
                {
                    service.Delete(user.Uuid, user.Timestamp);
                }
                else
                {
                    if (user.Operation.Equals(OperationType.UPDATE))
                    {
                        // TODO: verify this works before we enable
                        /*
                        // check for no changes
                        if (!user.BypassCache)
                        {
                            var succesUsers = dao.GetSuccessEntries(user.Uuid);
                            var userToCompare = succesUsers.Where(user => user.Operation.Equals(OperationType.UPDATE)).OrderBy(user => user.Id).LastOrDefault();
                            if (userToCompare != null)
                            {
                                string jsonUserToCompare = JsonSerializer.Serialize(userToCompare);
                                string jsonUser = JsonSerializer.Serialize(user);
                                identicalToLastSuccess = jsonUserToCompare.Equals(jsonUser);
                            }
                        }
                        */
                    }

                    if (!identicalToLastSuccess)
                    {
                        service.Update(user);
                    }
                }

                dao.OnSuccess(user.Id, identicalToLastSuccess);
                dao.Delete(user.Id);

                if (identicalToLastSuccess)
                {
                    log.Debug("Skipping user update for '" + user.Uuid + "' because it was identical to last known successful update");
                    return SyncResult.SkippedDueToCache;
                }

                return SyncResult.Processed;
            }
            catch (TemporaryFailureException ex)
            {
                log.Warn("Could not handle user '" + user.Uuid + "' at the moment, will try later", ex);
                return SyncResult.TemporaryFailure;
            }
            catch (Exception ex)
            {
                log.Error("Could not handle user '" + user.Uuid + "'", ex);
                dao.OnFailure(user.Id, ex.Message);
                dao.Delete(user.Id);

                return SyncResult.PermanentFailed;
            }
        }

        public static void HandleOUs()
        {
            OrgUnitService service = new OrgUnitService();
            OrgUnitDao dao = new OrgUnitDao();
            int transferCount = 0, failureCount = 0, cacheCount = 0;
            bool temporaryFailure = false;

            var orgUnits = dao.GetOldestEntries();
            while (orgUnits.Count > 0)
            {
                Parallel.ForEach(orgUnits, (orgUnit) =>
                {
                    using (var cancelTokenSource = new CancellationTokenSource())
                    {
                        var cancelToken = cancelTokenSource.Token;

                        var task = Task.Run(() => HandleOU(orgUnit, service, dao), cancelToken);

                        if (task.Wait(TimeSpan.FromSeconds(300)))
                        {
                            switch (task.Result)
                            {
                                case SyncResult.TemporaryFailure:
                                    temporaryFailure = true;
                                    break;
                                case SyncResult.PermanentFailed:
                                    // bad data, it was logged and then throw away, nothing to see here, move along
                                    lock (orgUnits)
                                    {
                                        failureCount++;
                                    }
                                    break;
                                case SyncResult.SkippedDueToCache:
                                    lock (orgUnits)
                                    {
                                        cacheCount++;
                                    }
                                    // note we do not zero errorCount, as we did not actually call FK Organisation
                                    break;
                                case SyncResult.Processed:
                                    lock (orgUnits)
                                    {
                                        transferCount++;
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

                if (temporaryFailure) {
                    break;
                }

                orgUnits = dao.GetOldestEntries();
            }

            if (transferCount > 0 || failureCount > 0 || cacheCount > 0)
            {
                int total = transferCount + failureCount + cacheCount;

                log.Info("Processed " + total + " orgUnit(s): success=" + transferCount + ", failure=" + failureCount + ", cacheSkip=" + cacheCount);
            }

            // indicate to main control that we should sleep a short while before trying again
            if (temporaryFailure)
            {
                throw new TemporaryFailureException();
            }
        }

        private static SyncResult HandleOU(OrgUnitRegistrationExtended ou, OrgUnitService service, OrgUnitDao dao)
        {
            try
            {
                bool identicalToLastSuccess = false;
                OrganisationRegistryProperties.SetCurrentMunicipality(ou.Cvr);

                if (ou.Operation.Equals(OperationType.DELETE))
                {
                    service.Delete(ou.Uuid, ou.Timestamp);
                }
                else
                {
                    if (ou.Operation.Equals(OperationType.UPDATE))
                    {
                        // TODO: verify this works before we enable
                        /*
                        // check for no changes
                        if (!ou.BypassCache)
                        {
                            var succesOus = dao.GetSuccessEntries(ou.Uuid);
                            var ouToCompare = succesOus.Where(ou => ou.Operation.Equals(OperationType.UPDATE)).OrderBy(ou => ou.Id).LastOrDefault();
                            if (ouToCompare != null)
                            {
                                string jsonOUToCompare = JsonSerializer.Serialize(ouToCompare);
                                string jsonOU = JsonSerializer.Serialize(ou);
                                identicalToLastSuccess = jsonOUToCompare.Equals(jsonOU);
                            }
                        }
                        */
                    }

                    if (!identicalToLastSuccess)
                    {
                        service.Update(ou);
                    }
                }

                dao.OnSuccess(ou.Id, identicalToLastSuccess);
                dao.Delete(ou.Id);

                if (identicalToLastSuccess)
                {
                    log.Debug("Skipping ou update for '" + ou.Uuid + "' because it was identical to last known successful update");
                    return SyncResult.SkippedDueToCache;
                }

                return SyncResult.Processed;
            }
            catch (TemporaryFailureException ex)
            {
                log.Warn("Could not handle ou '" + ou.Uuid + "' at the moment, will try later", ex);
                return SyncResult.TemporaryFailure;
            }
            catch (Exception ex)
            {
                log.Error("Could not handle ou '" + ou.Uuid + "'", ex);
                dao.OnFailure(ou.Id, ex.Message);
                dao.Delete(ou.Id);

                return SyncResult.PermanentFailed;
            }
        }
    }
}
