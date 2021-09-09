using Organisation.BusinessLayer;
using Organisation.SchedulingLayer;
using System;
using Microsoft.AspNetCore.Mvc;
using Organisation.BusinessLayer.DTO.Registration;

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
        public IActionResult Update([FromBody] UserRegistration user, [FromHeader] string cvr, [FromHeader] string apiKey)
        {
            if ((cvr = AuthorizeAndFetchCvr(cvr, apiKey)) == null)
            {
                return Unauthorized();
            }

            string error;
            if ((error = ValidateUser(user)) == null)
            {
                try
                {
                    userDao.Save(user, OperationType.UPDATE, cvr);
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
                return Unauthorized();
            }

            try
            {
                UserRegistrationExtended user = new UserRegistrationExtended()
                {
                    Uuid = uuid
                };

                userDao.Save(user, OperationType.DELETE, cvr);
            }
            catch (Exception ex)
            {
                log.Error("Failed to save User", ex);

                return BadRequest(ex.Message);
            }

            return Ok();
        }

        [HttpGet("{uuid}")]
        public IActionResult Read(string uuid, [FromHeader] string cvr, [FromHeader] string apiKey)
        {
            if ((cvr = AuthorizeAndFetchCvr(cvr, apiKey)) == null)
            {
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

            foreach (Position position in user.Positions)
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
