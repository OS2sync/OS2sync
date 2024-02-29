using System;
using System.DirectoryServices;
using System.Threading.Tasks;
using System.Collections.Generic;
using Quartz;
using Organisation.BusinessLayer;
using IntegrationLayer.OrganisationFunktion;
using Organisation.BusinessLayer.DTO.Read;
using Organisation.BusinessLayer.DTO.Registration;
using Organisation.SchedulingLayer;

namespace OS2syncAD
{
    [DisallowConcurrentExecution]
    public class CleanupOrgUnitJob : IJob
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private InspectorService inspectorService;

        private EventEnricher EventEnricher { get; set; }

        private Filter Filter { get; set; }

        private OrgUnitDao OrgUnitDao { get; set; }

        public CleanupOrgUnitJob()
        {
            var attributeLoader = new ADAttributeLoader();
            var adUtils = new ADUtils(attributeLoader);

            this.EventEnricher = new EventEnricher(attributeLoader, adUtils);
            this.inspectorService = new InspectorService();
            this.Filter = new Filter(adUtils);
            this.OrgUnitDao = new Organisation.SchedulingLayer.OrgUnitDao();
        }

        public Task Execute(IJobExecutionContext context)
        {
            List<FiltreretOejebliksbilledeType> allUnitRoles = new List<FiltreretOejebliksbilledeType>();
            List<ADEvent> adOUS = ReadAllOrgUnits();

            List<OU> ous = inspectorService.ReadOUHierarchy(AppConfiguration.Cvr, out allUnitRoles, null, ReadTasks.NO, ReadManager.NO, ReadAddresses.NO, ReadPayoutUnit.NO, ReadContactPlaces.NO, ReadPositions.NO, ReadContactForTasks.NO);
            foreach (var ou in ous) // OU's in FK Organisation
            {
                if (adOUS.Find(o => o.ADAttributes.Uuid.Equals(ou.Uuid)) == null)
                {
                    // Delete
                    OrgUnitRegistration orgUnit = new OrgUnitRegistration();
                    orgUnit.Uuid = ou.Uuid;

                    if (!(AppConfiguration.CleanupOUJobDryRun))
                    {
                        OrgUnitDao.Save(orgUnit, global::Organisation.SchedulingLayer.OperationType.DELETE, false, 10, AppConfiguration.Cvr);
                        log.Debug($"CleanupTask Remove: OrgUnit:{ou.Name} UUID:{orgUnit.Uuid}");
                    }
                    else
                    {
                        log.Info($"CleanupTask DryRun Remove: OrgUnit:{ou.Name} UUID:{orgUnit.Uuid}");
                    }
                }
            }

            return Task.CompletedTask;
        }

        private DirectorySearcher InitializeDirectorySearcher()
        {
            var attributes = new HashSet<string>();
            attributes.Add("objectGuid");
            attributes.Add("distinguishedName");
            attributes.Add("ou");

            attributes.Add(AppConfiguration.OUAttributeFiltered);
            attributes.Add(AppConfiguration.OUAttributeName);

            attributes.Remove(null);
            attributes.Remove("");

            var attributesArray = new string[attributes.Count];
            attributes.CopyTo(attributesArray);

            DirectorySearcher directorySearcher = new DirectorySearcher("(&((objectClass=organizationalUnit))(!(objectClass=computer)))", attributesArray);
            directorySearcher.PageSize = 500;

            return directorySearcher;
        }

        private List<ADEvent> ReadAllOrgUnits()
        {
            var result = new List<ADEvent>();

            try
            {
                using (var directorySearcher = InitializeDirectorySearcher())
                {
                    using (SearchResultCollection searchResults = directorySearcher.FindAll())
                    {
                        foreach (SearchResult searchResult in searchResults)
                        {
                            try
                            {
                                ADEvent adEvent = ADEventBuilder.Build(searchResult);
                                ADEvent enrichedEvent = EventEnricher.Enrich(adEvent);

                                if (!Filter.ShouldWeSynchronize(enrichedEvent))
                                {
                                    continue;
                                }

                                result.Add(enrichedEvent);
                            }
                            catch (Exception ex)
                            {
                                log.Warn("Failed to parse object from AD with path: " + searchResult.Path + " and error: " + ex.Message, ex);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error("Directory Read All OrgUnits Failed.", ex);
            }

            return result;
        }
    }
}
