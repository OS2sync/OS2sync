using Organisation.IntegrationLayer;
﻿using Organisation.BusinessLayer;
using Organisation.SchedulingLayer;
using System;
using Microsoft.AspNetCore.Mvc;
using Organisation.BusinessLayer.DTO.Registration;

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
        public IActionResult Update([FromBody] OrgUnitRegistration ou, [FromHeader] string cvr, [FromHeader] string apiKey)
        {
            if ((cvr = AuthorizeAndFetchCvr(cvr, apiKey)) == null)
            {
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
                    orgUnitDao.Save(ou, OperationType.UPDATE, cvr);
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
                return Unauthorized();
            }

            try
            {
                OrgUnitRegistrationExtended toRemove = new OrgUnitRegistrationExtended()
                {
                    Uuid = uuid
                };

                orgUnitDao.Save(toRemove, OperationType.DELETE, cvr);
            }
            catch (Exception ex)
            {
                log.Error("Failed to save OrgUnit", ex);

                return BadRequest(ex.Message);
            }

            return Ok();
        }

        [HttpGet("{uuid}")]
        public IActionResult Read(string uuid, [FromHeader] string cvr, [FromHeader] string apiKey)
        {
            if (AuthorizeAndFetchCvr(cvr, apiKey) == null)
            {
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
