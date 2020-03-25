using System;
using System.DirectoryServices;
using System.Text;

namespace OS2syncAD
{
    public class ADEventBuilder
    {
        private const string IS_DELETED_ATTRIBUTE = "isdeleted";

        public static ADEvent Build(SearchResult searchResult)
        {
            bool isUser = IsUser(searchResult);

            ADAttributes attributes = AttributesBuilder.BuildAttributes(searchResult.Properties);

            ADEvent deletedObject = EventOnObjectDeleted(attributes, isUser);
            if (deletedObject != null)
            {
                return deletedObject;
            }

            ADEvent createdObject = EventOnObjectCreated(attributes, isUser);
            if (createdObject != null)
            {
                return createdObject;
            }

            return EventOnObjectUpdate(attributes, isUser);
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

        private static ADEvent EventOnObjectCreated(ADAttributes attributes, bool isUser)
        {
            if (attributes.Contains("whencreated") && (!attributes.Contains("useraccountcontrol")))
            {
                return new ADEvent(0, OperationType.Create, ObjectType.OU, attributes, DateTime.Now, null);
            }

            else if (attributes.Contains("whencreated") && attributes.Contains("objectclass") && isUser)
            {
                return new ADEvent(0, OperationType.Create, ObjectType.User, attributes, DateTime.Now, null);
            }

            return null;
        }

        private static ADEvent EventOnObjectDeleted(ADAttributes attributes, bool isUser)
        {
            if (!attributes.Contains(IS_DELETED_ATTRIBUTE))
            {
                return null;
            }

            ADSingleValueAttribute isDeleted = (ADSingleValueAttribute)attributes.GetField(IS_DELETED_ATTRIBUTE);

            if (isDeleted.Value != null && "true".Equals((string)isDeleted.Value.ToLower()))
            {
                if (isUser)
                {
                    return new ADEvent(0, OperationType.Remove, ObjectType.User, attributes, DateTime.Now, null);
                }
                else
                {
                    return new ADEvent(0, OperationType.Remove, ObjectType.OU, attributes, DateTime.Now, null);
                }
            }

            return null;
        }

        private static ADEvent EventOnObjectUpdate(ADAttributes attributes, bool isUser)
        {
            if (!isUser)
            {
                return new ADEvent(0, OperationType.Update, ObjectType.OU, attributes, DateTime.Now, null);
            }

            return new ADEvent(0, OperationType.Update, ObjectType.User, attributes, DateTime.Now, null);
        }
    }
}
