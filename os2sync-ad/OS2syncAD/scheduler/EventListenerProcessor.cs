using System.DirectoryServices;
using Organisation.SchedulingLayer;
using Organisation.BusinessLayer.DTO.Registration;

namespace OS2syncAD
{
    public class EventListenerProcessor
    {
        public enum ProcessingResult { OU_UPDATE, OU_DELETE, USER_UPDATE, USER_DELETE, FAILURE };

        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private ADEventBuilder ADEventBuilder { get; set; }
        private EventEnricher EventEnricher { get; set; }
        private EventMapper EventMapper { get; set; }
        private OrgUnitDao OrgUnitDao { get; set; }
        private UserDao UserDao { get; set; }
        private ADUtils ADUtils { get; set; }
        private Filter Filter { get; set; }

        public EventListenerProcessor(
            EventEnricher eventEnricher,
            EventMapper mapper,
            ADEventBuilder adEventBuilder,
            OrgUnitDao orgUnitDao,
            UserDao userDao,
            Filter filter,
            ADUtils adUtils)
        {
            this.EventEnricher = eventEnricher;
            this.ADEventBuilder = adEventBuilder;
            this.EventMapper = mapper;
            this.OrgUnitDao = orgUnitDao;
            this.UserDao = userDao;
            this.Filter = filter;
            this.ADUtils = adUtils;
        }

        public ProcessingResult Process(SearchResult searchResult)
        {
            ADEvent adEvent = ADEventBuilder.Build(searchResult);
            ADEvent enrichedEvent = EventEnricher.Enrich(adEvent);

            if (!Filter.ShouldWeSynchronize(enrichedEvent))
            {
                log.Debug("Setting operation to Remove due to filtering on: " + enrichedEvent.ADAttributes?.DistinguishedName);
                enrichedEvent.OperationType = OperationType.Remove;
            }

            return InvokeOperation(enrichedEvent);
        }

        public ProcessingResult InvokeOperation(ADEvent adEvent)
        {
            if (ObjectType.User.Equals(adEvent.AffectedObjectType))
            {
                UserRegistration user = EventMapper.MapUser(adEvent);

                UserDao.Save(user, EventMapper.Map(adEvent.OperationType), false, 10, AppConfiguration.Cvr);
                log.Debug("User (" + user.Person.Name + ", " + user.Uuid + ") " + adEvent.OperationType);

                return (adEvent.OperationType == OperationType.Remove) ? ProcessingResult.USER_DELETE : ProcessingResult.USER_UPDATE;
            }
            else if (ObjectType.OU.Equals(adEvent.AffectedObjectType))
            {
                OrgUnitRegistration orgUnit = EventMapper.MapOU(adEvent);

                OrgUnitDao.Save(orgUnit, EventMapper.Map(adEvent.OperationType), false, 10, AppConfiguration.Cvr);
                log.Debug("OrgUnit (" + orgUnit.Name + ", " + orgUnit.Uuid + ") " + adEvent.OperationType + ". Parent = '" + orgUnit.ParentOrgUnitUuid + "'");

                return (adEvent.OperationType == OperationType.Remove) ? ProcessingResult.OU_DELETE : ProcessingResult.OU_UPDATE;
            }

            return ProcessingResult.FAILURE;
        }
    }
}
