using Organisation.BusinessLayer;
using Organisation.SchedulingLayer;
using System;
using Microsoft.AspNetCore.Mvc;
using Organisation.BusinessLayer.DTO.Registration;
using System.Collections.Generic;
using Organisation.BusinessLayer.DTO.Read;
using System.Linq;

namespace Organisation.ServiceLayer
{
    [Route("api/[controller]")]
    public class UserController : BaseController
    {
        private static log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private UserDao userDao;
        private UserService userService;

        public UserController()
        {
            userDao = new UserDao();
            userService = new UserService();
        }

        [HttpPost]
        public IActionResult Update([FromBody] UserRegistration user, [FromHeader] string cvr, [FromHeader] string apiKey, [FromQuery] bool bypassCache = false, [FromQuery] int priority = 10)
        {
            if ((cvr = AuthorizeAndFetchCvr(cvr, apiKey)) == null)
            {
                log.Warn("Rejected access to User Update operation");

                return Unauthorized();
            }

            string error;
            if ((error = ValidateUser(user)) == null)
            {
                try
                {
                    userDao.Save(user, OperationType.UPDATE, bypassCache, priority, cvr);
                }
                catch (Exception ex)
                {
                    log.Error("Failed to save User", ex);

                    return BadRequest(ex.Message);
                }
            }
            else
            {
                return BadRequest(error);
            }

            return Ok(user);
        }

        [HttpDelete("{uuid}")]
        public IActionResult Delete(string uuid, [FromHeader] string cvr, [FromHeader] string apiKey)
        {
            if ((cvr = AuthorizeAndFetchCvr(cvr, apiKey)) == null)
            {
                log.Warn("Rejected access to User Delete operation");

                return Unauthorized();
            }

            try
            {
                UserRegistrationExtended user = new UserRegistrationExtended()
                {
                    Uuid = uuid
                };

                userDao.Save(user, OperationType.DELETE, false, 10, cvr);
            }
            catch (Exception ex)
            {
                log.Error("Failed to save User", ex);

                return BadRequest(ex.Message);
            }

            return Ok();
        }

        [HttpPost("passiver/{uuid}")]
        public IActionResult Passiver(string uuid, [FromHeader] string cvr, [FromHeader] string apiKey)
        {
            if ((cvr = AuthorizeAndFetchCvr(cvr, apiKey)) == null)
            {
                log.Warn("Rejected access to User Passiver operation");

                return Unauthorized();
            }

            try
            {
                UserRegistrationExtended user = new UserRegistrationExtended()
                {
                    Uuid = uuid
                };

                userDao.Save(user, OperationType.PASSIVER, false, 10, cvr);
            }
            catch (Exception ex)
            {
                log.Error("Failed to save User", ex);

                return BadRequest(ex.Message);
            }

            return Ok();
        }

        [HttpPost("cleanup")]
        public IActionResult Cleanup([FromBody] string[] existingUsers, [FromHeader] string cvr, [FromHeader] string apiKey, [FromQuery] bool dryrun = false)
        {
            if ((cvr = AuthorizeAndFetchCvr(cvr, apiKey)) == null)
            {
                log.Warn("Rejected access to User Cleanup operation");

                return Unauthorized();
            }

            try
            {
                log.Info("Starting user cleanup for " + cvr);

                log.Info("Got payload with " + existingUsers.Length + " existing users from source system");

                var result = userService.List();

                log.Info("Found " + result.Count + " users in FK Organisation");

                int count = 0;
                foreach (string uuidInFk in result)
                {
                    if (!existingUsers.Contains(uuidInFk))
                    {
                        UserRegistrationExtended reg = new UserRegistrationExtended()
                        {
                            Uuid = uuidInFk
                        };

                        if (dryrun)
                        {
                            log.Info("Would have deleted " +  uuidInFk + " but did not, becuse dryrun=true was supplied as a query parameter");
                        }
                        else
                        {
                            log.Info("Queueing delete on " + uuidInFk);
                            userDao.Save(reg, OperationType.DELETE, false, 12, cvr);
                        }

                        count++;
                    }
                }

                log.Info("Found " + count + " users in FK Organisation that needed to be deleted");
                count = 0;

                List<string> usersNotInFK = new List<string>();
                foreach (string uuidInLocal in existingUsers)
                {
                    if (!result.Contains(uuidInLocal))
                    {
                        usersNotInFK.Add(uuidInLocal);
                        count++;
                    }
                }

                log.Info("Found " + count + " users in source system that does not exist in FK Organisation");

                log.Info("Completed user cleanup for " + cvr);

                return Ok(usersNotInFK);
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
                log.Warn("Rejected access to User CleanAll operation");

                return Unauthorized();
            }

            try
            {
                var result = userService.List();

                return Ok(result);
            }
            catch (Exception ex)
            {
                log.Error("Failed to bulkread users", ex);

                return BadRequest(ex.Message);
            }
        }

        [HttpGet("{uuid}")]
        public IActionResult Read(string uuid, [FromHeader] string cvr, [FromHeader] string apiKey)
        {
            if ((cvr = AuthorizeAndFetchCvr(cvr, apiKey)) == null)
            {
                log.Warn("Rejected access to User Read operation for " + uuid);

                return Unauthorized();
            }

            if (string.IsNullOrEmpty(uuid))
            {
                return BadRequest("uuid is null or empty");
            }

            try
            {
                UserRegistration registration = userService.Read(uuid);
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
                log.Error("Failed to read User", ex);

                return BadRequest(ex.Message);
            }

            return NotFound();
        }

        private string ValidateUser(UserRegistration user)
        {
            if (string.IsNullOrEmpty(user.Person.Name))
            {
                return "Person.Name is null or empty";
            }
            else if (string.IsNullOrEmpty(user.UserId))
            {
                return "UserId is null or empty";
            }
            else if (string.IsNullOrEmpty(user.Uuid))
            {
                return "Uuid is null empty";
            }
            else if (user.Positions == null ||user.Positions.Count == 0)
            {
                return "Positions is null or empty";
            }

            foreach (var position in user.Positions)
            {
                if (string.IsNullOrEmpty(position.Name) ||string.IsNullOrEmpty(position.OrgUnitUuid))
                {
                    return "Position Name or OrgUnitUUID is null";
                }
            }

            return null;
        }
    }
}
