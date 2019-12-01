using Organisation.IntegrationLayer;
using Organisation.BusinessLayer;
using System;
using System.Linq;
using System.Threading;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Organisation.BusinessLayer.DTO.Read;

namespace Organisation.ServiceLayer
{
    [Route("api/[controller]")]
    public class HierarchyController : Controller
    {
        private static log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private InspectorService service = new InspectorService();
        private static Dictionary<string, HierarchyWrapper> cache = new Dictionary<string, HierarchyWrapper>();

        public static void Cleanup()
        {
            log.Debug("Running cleanup");
            var toRemove = new List<string>();

            foreach (KeyValuePair<string, HierarchyWrapper> entry in cache)
            {
                if (entry.Value.Created.CompareTo(DateTime.Now.AddHours(-1)) < 0)
                {
                    toRemove.Add(entry.Key);
                }
            }

            foreach (string key in toRemove)
            {
                log.Info("Removing " + key + " from cache");
                cache.Remove(key);
            }
        }

        [HttpGet]
        public IActionResult Read([FromHeader] string cvr, [FromHeader] string apiKey)
        {
            if (!ApiKeyFilter.ValidApiKey(apiKey))
            {
                return Unauthorized();
            }

            string uuid = Guid.NewGuid().ToString().ToLower();

            new Thread(() => {
                try {
                    // set cvr on thread if supplied as header (will revert to default if null)
                    OrganisationRegistryProperties.SetCurrentMunicipality(cvr);

                    log.Info("Fetching hierarchy for " + OrganisationRegistryProperties.GetCurrentMunicipality());

                    // read OUs
                    List<global::IntegrationLayer.OrganisationFunktion.FiltreretOejebliksbilledeType> allUnitRoles;
                    var ous = service.ReadOUHierarchy(cvr, out allUnitRoles, null, ReadTasks.NO, ReadManager.NO, ReadAddresses.NO, ReadPayoutUnit.NO, ReadPositions.YES, ReadContactForTasks.NO);

                    // read users
                    var userUuids = service.FindAllUsers(ous).Distinct().ToList();
                    var users = service.ReadUsers(cvr, userUuids, allUnitRoles, null, ReadAddresses.YES, ReadParentDetails.NO);
                    log.Info("Found " + users.Count + " users");

                    // construct result
                    var res = new Hierarchy();
                    
                    // ous can be mapped in a simple manner
                    res.OUs = ous.Select(ou => new BasicOU() {
                        Name = ou.Name,
                        ParentOU = ou.ParentOU?.Uuid,
                        Uuid = ou.Uuid
                    }).ToList();

                    // users has a slightly more complex structure
                    foreach (var user in users) {
                        if (string.IsNullOrEmpty(user.Person?.Name))
                        {
                            log.Warn("User with uuid " + user.Uuid + " does not have a Person.Name for CVR: " + cvr);
                            continue;
                        }

                        BasicUser basicUser = new BasicUser();
                        basicUser.Name = user.Person.Name;
                        basicUser.UserId = user.UserId;
                        basicUser.Uuid = user.Uuid;

                        if (user.Addresses != null)
                        {
                            foreach (var address in user.Addresses)
                            {
                                if (address is Email)
                                {
                                    basicUser.Email = address.Value;
                                }
                                else if (address is Phone)
                                {
                                    basicUser.Telephone = address.Value;
                                }
                            }
                        }

                        if (user.Positions != null)
                        {
                            foreach (var position in user.Positions)
                            {
                                basicUser.Positions.Add(new BasicPosition() {
                                    Name = position.Name,
                                    Uuid = position.OU.Uuid
                                });
                            }
                        }
                        
                        res.Users.Add(basicUser);
                    }

                    log.Info("Hierarchy build for " + OrganisationRegistryProperties.GetCurrentMunicipality() + ". Adding to cache with uuid: " + uuid);

                    cache.Add(uuid, new HierarchyWrapper()
                    {
                        Created = DateTime.Now,
                        Result = res,
                        Status = Status.SUCCESS
                    });
                }
                catch (Exception ex)
                {
                    log.Error("Failed to build Hierarchy for " + OrganisationRegistryProperties.GetCurrentMunicipality(), ex);

                    cache.Add(uuid, new HierarchyWrapper()
                    {
                        Created = DateTime.Now,
                        Result = null,
                        Status = Status.FAILURE
                    });
                }
            }).Start();

            return Ok(uuid);
        }

        [HttpGet("{uuid}")]
        public IActionResult ReadResult(string uuid, [FromHeader] string apiKey)
        {
            if (!ApiKeyFilter.ValidApiKey(apiKey))
            {
                return Unauthorized();
            }

            try
            {
                if (cache.ContainsKey(uuid))
                {
                    var result = cache[uuid];
                    cache.Remove(uuid);

                    return Ok(result);
                }

                return NotFound();
            }
            catch (Exception ex)
            {
                log.Error("Failed to load hierarchy", ex);

                return BadRequest();
            }
        }
    }

    enum Status { SUCCESS, FAILURE };

    [Serializable]
    class HierarchyWrapper
    {
        public DateTime Created { get; set; }
        public Hierarchy Result { get; set; }
        public Status Status { get; set; }
    }
}
