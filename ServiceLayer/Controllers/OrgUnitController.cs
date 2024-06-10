using Organisation.IntegrationLayer;
﻿using Organisation.BusinessLayer;
using Organisation.SchedulingLayer;
using System;
using Microsoft.AspNetCore.Mvc;
using Organisation.BusinessLayer.DTO.Registration;
using System.Collections.Generic;
using System.Linq;

namespace Organisation.ServiceLayer
{
    [Route("api/[controller]")]
    public class OrgUnitController : BaseController
    {
        private static log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private OrgUnitDao orgUnitDao;
        private OrgUnitService orgUnitService;

        public OrgUnitController()
        {
            orgUnitDao = new OrgUnitDao();
            orgUnitService = new OrgUnitService();
        }

        [HttpPost]
        public IActionResult Update([FromBody] OrgUnitRegistration ou, [FromHeader] string cvr, [FromHeader] string apiKey, [FromQuery] bool bypassCache = false, [FromQuery] int priority = 10)
        {
            if ((cvr = AuthorizeAndFetchCvr(cvr, apiKey)) == null)
            {
                log.Warn("Rejected access to OrgUnit Update operation");
                return Unauthorized();
            }

            // setting it will revert to default if no value is supplied, so we can read a valid value afterwards
            OrganisationRegistryProperties.SetCurrentMunicipality(cvr);
            cvr = OrganisationRegistryProperties.GetCurrentMunicipality();

            string error;
            if ((error = ValidateOU(ou)) == null)
            {
                try
                {
                    orgUnitDao.Save(ou, OperationType.UPDATE, bypassCache, priority, cvr);
                }
                catch (Exception ex)
                {
                    log.Error("Failed to save OrgUnit", ex);

                    return BadRequest(ex.Message);
                }
            }
            else
            {
                return BadRequest(error);
            }

            return Ok(ou);
        }


        [HttpDelete("{uuid}")]
        public IActionResult Delete(string uuid, [FromHeader] string cvr, [FromHeader] string apiKey)
        {
            if ((cvr = AuthorizeAndFetchCvr(cvr, apiKey)) == null)
            {
                log.Warn("Rejected access to OrgUnit Delete operation");

                return Unauthorized();
            }

            try
            {
                OrgUnitRegistrationExtended toRemove = new OrgUnitRegistrationExtended()
                {
                    Uuid = uuid
                };

                orgUnitDao.Save(toRemove, OperationType.DELETE, false, 10, cvr);
            }
            catch (Exception ex)
            {
                log.Error("Failed to save OrgUnit", ex);

                return BadRequest(ex.Message);
            }

            return Ok();
        }

        [HttpPost("cleanup")]
        public IActionResult Cleanup([FromBody] string[] existingOrgUnits, [FromHeader] string cvr, [FromHeader] string apiKey, [FromQuery] bool dryrun = false)
        {
            if ((cvr = AuthorizeAndFetchCvr(cvr, apiKey)) == null)
            {
                log.Warn("Rejected access to OrgUnit Cleanup operation");

                return Unauthorized();
            }

            try
            {
                log.Info("Starting orgUnit cleanup for " + cvr);

                log.Info("Got payload with " + existingOrgUnits.Length + " existing orgUnits from source system");

                var result = orgUnitService.List();

                log.Info("Found " + result.Count + " orgUnits in FK Organisation");

                int count = 0;
                foreach (string uuidInFk in result)
                {
                    if (!existingOrgUnits.Contains(uuidInFk))
                    {
                        OrgUnitRegistrationExtended reg = new OrgUnitRegistrationExtended()
                        {
                            Uuid = uuidInFk
                        };

                        if (dryrun)
                        {
                            log.Info("Would have deleted " + uuidInFk + " but did not, becuse dryrun=true was supplied as a query parameter");
                        }
                        else
                        {
                            log.Info("Queueing delete on " + uuidInFk);
                            orgUnitDao.Save(reg, OperationType.DELETE, false, 12, cvr);
                        }

                        count++;
                    }
                }

                log.Info("Found " + count + " orgUnits in FK Organisation that needed to be deleted");
                count = 0;

                List<string> ousNotInFK = new List<string>();
                foreach (string uuidInLocal in existingOrgUnits)
                {
                    if (!result.Contains(uuidInLocal))
                    {
                        ousNotInFK.Add(uuidInLocal);
                        count++;
                    }
                }

                log.Info("Found " + count + " orgUnits in source system that does not exist in FK Organisation");

                log.Info("Completed orgUnit cleanup for " + cvr);

                return Ok(ousNotInFK);
            }
            catch (Exception ex)
            {
                log.Error("Failed to perform cleanup", ex);

                return BadRequest(ex.Message);
            }
        }

        [HttpGet("all")]
        public IActionResult ReadAll([FromHeader] string cvr, [FromHeader] string apiKey)
        {
            if ((cvr = AuthorizeAndFetchCvr(cvr, apiKey)) == null)
            {
                log.Warn("Rejected access to OrgUnit ReadAll operation");

                return Unauthorized();
            }

            try
            {
                var result = orgUnitService.List();

                return Ok(result);
            }
            catch (Exception ex)
            {
                log.Error("Failed to bulkread orgUnits", ex);

                return BadRequest(ex.Message);
            }
        }

        [HttpGet("{uuid}")]
        public IActionResult Read(string uuid, [FromHeader] string cvr, [FromHeader] string apiKey)
        {
            if (AuthorizeAndFetchCvr(cvr, apiKey) == null)
            {
                log.Warn("Rejected access to OrgUnit Read operation on " + uuid);

                return Unauthorized();
            }

            if (string.IsNullOrEmpty(uuid))
            {
                return BadRequest("uuid is null");
            }

            try
            {
                OrgUnitRegistration registration = orgUnitService.Read(uuid);
                if (registration != null)
                {
                    return Ok(registration);
                }
            }
            catch (RegistrationNotFoundException)
            {
                return NotFound();
            }
            catch (Exception ex)
            {
                log.Error("Failed to read OrgUnit", ex);

                return BadRequest(ex.Message);
            }

            return NotFound();
        }

        private string ValidateOU(OrgUnitRegistration ou)
        {
            if (string.IsNullOrEmpty(ou.Name))
            {
                return "name is null";
            }
            else if (string.IsNullOrEmpty(ou.Uuid))
            {
                return "uuid is null";
            }

            return null;
        }
    }
}
