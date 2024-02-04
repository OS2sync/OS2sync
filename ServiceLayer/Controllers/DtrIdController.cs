using Organisation.IntegrationLayer;
using Organisation.BusinessLayer;
using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Organisation.BusinessLayer.DTO.Read;

namespace Organisation.ServiceLayer
{
    [Route("api/[controller]")]
    public class DtrIdController : Controller
    {
        private static log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private InspectorService service = new InspectorService();

        [HttpGet]
        public IActionResult Read([FromHeader] string cvr, [FromHeader] string apiKey)
        {
            List<UserDTO> result = new List<UserDTO>();

            try {
                // set cvr on thread if supplied as header (will revert to default if null)
                OrganisationRegistryProperties.SetCurrentMunicipality(cvr);

                log.Info("Fetching users in OrgUnits with a DTR-ID for " + OrganisationRegistryProperties.GetCurrentMunicipality());

                // read OUs
                List<global::IntegrationLayer.OrganisationFunktion.FiltreretOejebliksbilledeType> allUnitRoles;
                List<OU> ous = service.ReadOUHierarchy(cvr, out allUnitRoles, null, ReadTasks.NO, ReadManager.YES, ReadAddresses.YES, ReadPayoutUnit.NO, ReadContactPlaces.NO, ReadPositions.NO, ReadContactForTasks.NO);

                log.Info("Found " + ous.Count() + " orgUnits in total");

                // filter OUs so we only get those with a DTRID registered on them
                ous = ous.Where(ou => ou.Addresses.Where(a => a is DtrId).Count() > 0).ToList();

                log.Info("Filtered to " + ous.Count() + " orgUnits with a DTR ID assigned");

                // TODO: could optimize this with some parallel lookup
                // read positions from OrgUnits
                service.LoadPositions(ous, allUnitRoles);

                // read users
                var userUuids = service.FindAllUsers(ous).Distinct().ToList();

                log.Info("Identified " + userUuids.Count + " users - reading details");

                var users = service.ReadUsers(cvr, userUuids, allUnitRoles, null, ReadAddresses.YES, ReadParentDetails.NO);
                log.Info("Found " + users.Count + " users");

                foreach (var ou in ous)
                {
                    var dtrIdAddress = ou.Addresses.Where(a => a is DtrId).FirstOrDefault();
                    if (dtrIdAddress == null)
                    {
                        continue;
                    }

                    string dtrId = dtrIdAddress.Value;

                    // load manager if available
                    if (!string.IsNullOrEmpty(ou.Manager?.Uuid))
                    {
                        log.Info("Reading manager for " + ou.Name);

                        try
                        {
                            var manager = service.ReadUserObject(ou.Manager.Uuid, ReadAddresses.YES, ReadParentDetails.NO);

                            var emailAddress = manager.Addresses.Where(a => a is Email).FirstOrDefault();
                            var phoneAddress = manager.Addresses.Where(a => a is Phone).FirstOrDefault();

                            var email = (emailAddress != null) ? emailAddress.Value : null;
                            var phone = (phoneAddress != null) ? phoneAddress.Value : null;

                            UserDTO userDTO = new UserDTO();
                            userDTO.dtrId = dtrId;
                            userDTO.email = email;
                            userDTO.phone = phone;
                            userDTO.ssn = manager.Person?.Cpr;
                            userDTO.userId = manager.UserId;
                            userDTO.uuid = manager.Uuid.ToLower();
                            userDTO.manager = true;
                            result.Add(userDTO);
                        }
                        catch (Exception ex)
                        {
                            log.Warn("Manager did not exist: " + ou.Manager.Uuid + " - " + ex.Message);
                        }
                    }

                    foreach (var user in users)
                    {
                        if (user.Positions.Where(p => string.Compare(p.OU?.Uuid, ou.Uuid) == 0).Count() > 0)
                        {
                            var emailAddress = user.Addresses.Where(a => a is Email).FirstOrDefault();
                            var phoneAddress = user.Addresses.Where(a => a is Phone).FirstOrDefault();

                            var email = (emailAddress != null) ? emailAddress.Value : null;
                            var phone = (phoneAddress != null) ? phoneAddress.Value : null;

                            UserDTO userDTO = new UserDTO();
                            userDTO.dtrId = dtrId;
                            userDTO.email = email;
                            userDTO.phone = phone;
                            userDTO.ssn = user.Person?.Cpr;
                            userDTO.userId = user.UserId;
                            userDTO.uuid = user.Uuid.ToLower();
                            userDTO.manager = false;
                            result.Add(userDTO);
                        }
                    }
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                log.Error("Failed to build Hierarchy for " + OrganisationRegistryProperties.GetCurrentMunicipality(), ex);
                return BadRequest("Error - se logs for details");
            }
        }

        [Serializable]
        class UserDTO
        {
            public string uuid { get; set; }
            public string ssn { get; set; }
            public string userId { get; set; }
            public string phone { get; set; }
            public string email { get; set; }
            public string dtrId { get; set; }
            public bool manager { get; set; }
        }
    }
}
