using System;
using System.DirectoryServices;
using System.Text;

namespace OS2syncAD
{
    public class ADAttributeLoader
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public ADAttributes Load(string cn)
        {
            bool inHierarchy = false;

            String rootOU = AppConfiguration.RootOU;
            if (cn.ToLower().Contains(rootOU.ToLower()))
            {
                log.Debug("cn=" + cn + " is considered in the hierarchy of root=" + rootOU);
                inHierarchy = true;
            }

            // if the CN is not a child of any of the roots (or one of the roots), we block it)
            if (!inHierarchy)
            {
                return Blocked(cn);
            }

            using (DirectoryEntry searchRoot = new DirectoryEntry())
            {
                using (DirectorySearcher deSearch = new DirectorySearcher(searchRoot))
                {
                    deSearch.Filter = "(&(distinguishedName=" + cn + "))";
                    deSearch.SearchScope = SearchScope.Subtree;
                    SearchResult result = deSearch.FindOne();

                    // if we cannot find it in AD, log it and return a blocked entry
                    if (result == null)
                    {
                        log.Warn("Unable to find object in Active Directory: " + cn);
                        return Blocked(cn);
                    }

                    bool isUser = IsUser(result);

                    return AttributesBuilder.BuildAttributes(result.Properties);
                }
            }
        }

        private static bool IsUser(SearchResult searchResult)
        {
            if (searchResult.Properties.Contains("objectclass"))
            {
                return searchResult.Properties["objectclass"].Contains("user");
            }

            using (DirectorySearcher deSearch = new DirectorySearcher())
            {
                Guid uuid = new Guid((byte[])searchResult.Properties["objectguid"][0]);
                deSearch.Filter = "(&(objectguid=" + GetBinaryStringFromGuid(uuid.ToString()) + "))";
                deSearch.Tombstone = true;

                SearchResult result = deSearch.FindOne();
                return result.Properties["objectclass"].Contains("user");
            }
        }

        private static string GetBinaryStringFromGuid(string guidstring)
        {
            Guid guid = new Guid(guidstring);

            byte[] bytes = guid.ToByteArray();

            StringBuilder sb = new StringBuilder();

            foreach (byte b in bytes)
            {
                sb.Append(string.Format(@"\{0}", b.ToString("X")));
            }

            return sb.ToString();
        }

        private ADAttributes Blocked(String cn)
        {
            ADAttributes attr = new ADAttributes();
            attr.DistinguishedName = cn;
            attr.Attributes[AppConfiguration.OUAttributeFiltered] = new ADSingleValueAttribute("OUAttributeFiltered", Constants.BLOCKED);

            return attr;
        }
    }
}
