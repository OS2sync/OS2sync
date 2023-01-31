using Organisation.BusinessLayer;
using Organisation.BusinessLayer.DTO.Read;
using System;
using System.Collections.Generic;
using System.Text;

namespace Organisation.ServiceLayer
{
    public class GuiUtil
    {
        private static log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private static InspectorService inspector = new InspectorService();

        // cache
        private static string orgUnitTreeCache;
        private static Dictionary<string, string> ouNameCache = new Dictionary<string, string>();
        private static Dictionary<string, OU> ouCache = new Dictionary<string, OU>();
        private static Dictionary<string, User> userCache = new Dictionary<string, User>();

        public static OU GetOrgUnit(string uuid)
        {
            if (!ouCache.ContainsKey(uuid))
            {
                var ou = inspector.ReadOUObject(uuid, ReadTasks.YES, ReadManager.YES, ReadAddresses.YES, ReadPayoutUnit.YES, ReadContactPlaces.YES, ReadPositions.YES, ReadContactForTasks.YES);

                ouCache.Add(uuid, ou);
            }

            return ouCache[uuid];
        }

        public static User GetUser(string uuid)
        {
            if (!userCache.ContainsKey(uuid))
            {
                var user = inspector.ReadUserObject(uuid, ReadAddresses.YES, ReadParentDetails.YES);

                userCache.Add(uuid, user);
            }

            return userCache[uuid];
        }

        public static string GetOrgUnitTree()
        {
            try
            {
                if (string.IsNullOrEmpty(orgUnitTreeCache))
                {
                    var ous = inspector.ReadOUHierarchy(null, out _, null, ReadTasks.NO, ReadManager.NO, ReadAddresses.NO, ReadPayoutUnit.NO, ReadContactPlaces.NO, ReadPositions.NO, ReadContactForTasks.NO);

                    StringBuilder builder = new StringBuilder();
                    builder.Append("[");
                    bool first = true;
                    foreach (var ou in ous)
                    {
                        if (!first)
                        {
                            builder.Append(",");
                        }

                        first = false;
                        builder.Append("{");
                        builder.Append("'id': '" + ou.Uuid + "',");
                        builder.Append("'parent': '" + (ou.ParentOU?.Uuid != null ? ou.ParentOU.Uuid : "#") + "',");
                        builder.Append("'text': '" + StripBadStuff(ou.Name) + "'");
                        builder.Append("}");

                        if (!ouNameCache.ContainsKey(ou.Uuid))
                        {
                            ouNameCache.Add(ou.Uuid, ou.Name);
                        }
                        else {
                            log.Warn("OrgUnit (" + ou.Uuid + " / " + ou.Name + ") already exists in cache - ignoring duplicate found in response from STS Organisation!");
                        }
                    }
                    builder.Append("]");

                    orgUnitTreeCache = builder.ToString();
                }

                return orgUnitTreeCache;
            }
            catch (Exception ex)
            {
                log.Error("Failed to fetch hierarchy", ex);
            }

            return "[]";
        }

        private static string StripBadStuff(string name) {
            return name.Replace('\'', ' ');
        }

        public static string GetOUName(string uuid)
        {
            if (ouNameCache.ContainsKey(uuid))
            {
                return (ouNameCache[uuid] + " ");
            }

            return "";
        }

        public static string GetUserName(string uuid)
        {
            if (userCache.ContainsKey(uuid))
            {
                return (userCache[uuid].Person.Name + ", ");
            }

            return "";
        }

        public static void ClearCache()
        {
            orgUnitTreeCache = null;
            ouCache = new Dictionary<string, OU>();
            userCache = new Dictionary<string, User>();
            ouNameCache = new Dictionary<string, string>();
        }
    }
}
