using System.IO;
using System.Xml.Serialization;

namespace Organisation.BusinessLayer
{
    internal class XmlUtil
    {
        internal static string SerializeObject<T>(T toSerialize)
        {
            XmlSerializer xmlSerializer = new XmlSerializer(toSerialize.GetType());

            using (StringWriter textWriter = new StringWriter())
            {
                xmlSerializer.Serialize(textWriter, toSerialize);
                return textWriter.ToString();
            }
        }
    }
}
