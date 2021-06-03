using System;
using System.DirectoryServices;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using Quartz;

namespace OS2syncAD
{
    [DisallowConcurrentExecution]
    [PersistJobDataAfterExecution]
    public class EventListenerJob : IJob
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private static readonly string DIR_SYNC_COOKIE_KEY = "DirSyncCookie";
        private static readonly string JOB_DATA_COOKIE = "COOKIE";
        private static readonly string JOB_DATA_DIRECTORY_SEARCHER = "DIRECTORY_SEARCHER";
        private static readonly string JOB_DATA_EVENT_LISTENER_PROCESSER = "EVENT_LISTENER_PROCESSER";

        private DirectorySearcher directorySearcher;
        private EventListenerProcessor eventListenerProcessor;
        private List<byte> cookie;

        private void LoadState(IJobExecutionContext context)
        {
            InitializeDirectorySearcher(context);
            InitializeEventListenerProcesser(context);
            LoadCookieIfNeeded(context);
        }

        public Task Execute(IJobExecutionContext context)
        {
            try
            {
                LoadState(context);

                // perform a sync, fetching latest changes
                using (SearchResultCollection searchResults = directorySearcher.FindAll())
                {
                    int i = 0, failures = 0, ou_updates = 0, ou_deletes = 0, user_updates = 0, user_deletes = 0;

                    foreach (SearchResult searchResult in searchResults)
                    {
                        i++;
                        if (i % 250 == 0)
                        {
                            log.Info("Synchronization from Active Diretory in progress - processed " + i + " modifications");
                        }

                        try
                        {
                            switch (eventListenerProcessor.Process(searchResult))
                            {
                                case EventListenerProcessor.ProcessingResult.FAILURE:
                                    failures++;
                                    break;
                                case EventListenerProcessor.ProcessingResult.OU_DELETE:
                                    ou_deletes++;
                                    break;
                                case EventListenerProcessor.ProcessingResult.OU_UPDATE:
                                    log.Debug("OU retrieved from AD for update/create: " + searchResult.Path);
                                    ou_updates++;
                                    break;
                                case EventListenerProcessor.ProcessingResult.USER_DELETE:
                                    user_deletes++;
                                    break;
                                case EventListenerProcessor.ProcessingResult.USER_UPDATE:
                                    user_updates++;
                                    break;
                            }
                        }
                        catch (Exception ex)
                        {
                            log.Warn("Failed to parse object from AD with path: " + searchResult.Path + " and error: " + ex.Message, ex);
                        }
                    }

                    if (i > 0)
                    {
                        log.Info("Read " + i + " modifications from Active Directory (" + ou_updates + " ou-updates, " + ou_deletes + " ou-deletes, " + user_updates + " user-updates, " + user_deletes + " user-deletes and " + failures + " unknown records)");
                    }

                    // store the updated cookie
                    SetCookie(directorySearcher.DirectorySynchronization.GetDirectorySynchronizationCookie());

                    RecordDao.Save(DIR_SYNC_COOKIE_KEY, Convert.ToBase64String((directorySearcher.DirectorySynchronization.GetDirectorySynchronizationCookie())));
                }
            }
            catch (Exception ex)
            {
                log.Error("Directory Synchronization Failed.", ex);
            }

            return Task.CompletedTask;
        }

        private void InitializeEventListenerProcesser(IJobExecutionContext context)
        {
            if (eventListenerProcessor == null)
            {
                eventListenerProcessor = (EventListenerProcessor)context.JobDetail.JobDataMap[JOB_DATA_EVENT_LISTENER_PROCESSER];
            }

            if (eventListenerProcessor == null)
            {
                var attributeLoader = new ADAttributeLoader();
                var adUtils = new ADUtils(attributeLoader);
                var eventEnricher = new EventEnricher(attributeLoader, adUtils);
                var eventMapper = new EventMapper();
                var eventBuilder = new ADEventBuilder();
                var orgUnitDao = new Organisation.SchedulingLayer.OrgUnitDao();
                var userDao = new Organisation.SchedulingLayer.UserDao();
                var filter = new Filter(adUtils);

                eventListenerProcessor = new EventListenerProcessor(eventEnricher, eventMapper, eventBuilder, orgUnitDao, userDao, filter, adUtils);

                context.JobDetail.JobDataMap.Put(JOB_DATA_EVENT_LISTENER_PROCESSER, eventListenerProcessor);
            }
        }

        private void InitializeDirectorySearcher(IJobExecutionContext context)
        {
            if (directorySearcher == null)
            {
                directorySearcher = (DirectorySearcher)context.JobDetail.JobDataMap[JOB_DATA_DIRECTORY_SEARCHER];
            }

            if (directorySearcher == null)
            {
                var attributes = new HashSet<string>();
                attributes.Add("objectGuid");
                attributes.Add("distinguishedName");
                attributes.Add("ou");
                attributes.Add("sAMAccountName");

                attributes.Add(AppConfiguration.OUAttributeFiltered);
                attributes.Add(AppConfiguration.OUAttributeEan);
                attributes.Add(AppConfiguration.OUAttributeDtrId);
                attributes.Add(AppConfiguration.OUAttributeEmail);
                attributes.Add(AppConfiguration.OUAttributeLocation);
                attributes.Add(AppConfiguration.OUAttributeLOSShortName);
                attributes.Add(AppConfiguration.OUAttributeName);
                attributes.Add(AppConfiguration.OUAttributePayoutUnitUUID);
                attributes.Add(AppConfiguration.OUAttributePhone);
                attributes.Add(AppConfiguration.OUAttributeLOSId);
                attributes.Add(AppConfiguration.OUAttributePost);
                attributes.Add(AppConfiguration.UserAttributeLocation);
                attributes.Add(AppConfiguration.UserAttributeMail);
                attributes.Add(AppConfiguration.UserAttributePersonCpr);
                attributes.Add(AppConfiguration.UserAttributePersonName);
                attributes.Add(AppConfiguration.UserAttributePhone);
                attributes.Add(AppConfiguration.UserAttributePositionName);
                attributes.Add(AppConfiguration.UserAttributeRacfID);

                attributes.Remove(null);
                attributes.Remove("");

                var attributesArray = new string[attributes.Count];
                attributes.CopyTo(attributesArray);

                DirectorySynchronization dirSync = new DirectorySynchronization();
                dirSync.Option = DirectorySynchronizationOptions.ObjectSecurity;

                directorySearcher = new DirectorySearcher("(&(|(objectClass=user)(objectClass=organizationalUnit))(!(objectClass=computer)))", attributesArray);
                directorySearcher.SizeLimit = 500;
                directorySearcher.DirectorySynchronization = dirSync;

                context.JobDetail.JobDataMap.Put(JOB_DATA_DIRECTORY_SEARCHER, directorySearcher);
            }
        }

        private void LoadCookieIfNeeded(IJobExecutionContext context)
        {
            if (cookie == null)
            {
                cookie = (List<byte>) context.JobDetail.JobDataMap[JOB_DATA_COOKIE];

                if (cookie == null)
                {
                    cookie = new List<byte>();
                    context.JobDetail.JobDataMap.Put(JOB_DATA_COOKIE, cookie);
                }
            }

            if (cookie.Count != 0) // Cookie is already set, no need to load it
            {
                return;
            }
            else if (IsCookieInDB())
            {
                string b64cookie = RecordDao.FindByKey(DIR_SYNC_COOKIE_KEY).Value;
                SetCookie(Convert.FromBase64String(b64cookie));
                directorySearcher.DirectorySynchronization.ResetDirectorySynchronizationCookie(Convert.FromBase64String(b64cookie));

                log.Info("Loaded DirSyncCookie from the database.");
            }
            else
            {
                log.Info("No DirSyncCookie, performing a full sync!");
            }
        }

        private void SetCookie(byte[] val)
        {
            cookie.Clear();
            cookie.AddRange(val.ToList());
        }

        private static bool IsCookieInDB()
        {
            return RecordDao.FindByKey(DIR_SYNC_COOKIE_KEY) == null ? false : true;
        }
    }
}

