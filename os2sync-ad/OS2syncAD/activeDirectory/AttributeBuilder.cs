using System;
using System.Collections;
using System.Collections.Generic;
using System.DirectoryServices;

namespace OS2syncAD
{
    public static class AttributesBuilder
    {
        private const string OBJECT_GUID_ATTRIBUTE = "objectguid";
        private const string OBJECT_DN_ATTRIBUTE = "distinguishedname";

        public static ADAttributes BuildAttributes(ResultPropertyCollection properties)
        {
            ADAttributes attributes = new ADAttributes();
            IDictionaryEnumerator enumerator = properties.GetEnumerator();
            attributes.Uuid = new Guid((byte[])properties[OBJECT_GUID_ATTRIBUTE][0]).ToString();

            if (properties.Contains(OBJECT_DN_ATTRIBUTE) && properties[OBJECT_DN_ATTRIBUTE].Count > 0 && !String.IsNullOrEmpty(properties[OBJECT_DN_ATTRIBUTE][0].ToString()))
            {
                attributes.DistinguishedName = properties[OBJECT_DN_ATTRIBUTE][0].ToString();
            }

            while (enumerator.MoveNext())
            {
                int nrOfAttributes = ((ResultPropertyValueCollection)enumerator.Value).Count;
                IADAttribute attribute = createIADAttribute(nrOfAttributes, (DictionaryEntry)enumerator.Current, OBJECT_GUID_ATTRIBUTE);

                if (attribute != null)
                {
                    attributes.Add(attribute);
                }
            }

            return attributes;
        }

        private static IADAttribute createIADAttribute(int nrOfAttributes, DictionaryEntry current, string uuidProperty)
        {
            ResultPropertyValueCollection values = (ResultPropertyValueCollection)current.Value;

            if (nrOfAttributes == 0)
            {
                return new ADNullValueAttribute((string)current.Key);
            }
            else if (nrOfAttributes == 1)
            {
                IEnumerator valuesEnumerator = values.GetEnumerator();
                valuesEnumerator.MoveNext();

                return createSingleValueAttribute(current, valuesEnumerator, uuidProperty);
            }
            else
            {
                IEnumerator valuesEnumerator = values.GetEnumerator();
                valuesEnumerator.MoveNext();

                return createMultiValueAttribute(current);
            }
        }

        private static IADAttribute createSingleValueAttribute(DictionaryEntry current, IEnumerator valuesEnumerator, string uuidProperty)
        {
            if (current.Key.Equals(OBJECT_GUID_ATTRIBUTE) ||current.Key.Equals(uuidProperty))
            {
                return null;
            }
            else
            {
                return new ADSingleValueAttribute((string)current.Key, valuesEnumerator.Current.ToString());
            }
        }

        private static IADAttribute createMultiValueAttribute(DictionaryEntry current)
        {
            ADMultiValueAttribute multivalueAttribute = new ADMultiValueAttribute((string)current.Key);
            List<string> values = new List<string>();

            ResultPropertyValueCollection innerValues = (ResultPropertyValueCollection)current.Value;

            foreach (object innerValue in innerValues)
            {
                // Sanity test to ensure the multiValue is of string type
                if (innerValue is string)
                {
                    values.Add((string)innerValue);
                }
                else
                {
                    return new ADNullValueAttribute((string)current.Key);
                }
            }

            multivalueAttribute.Name = (string)current.Key;
            multivalueAttribute.Add(values);

            return multivalueAttribute;
        }
    }
}