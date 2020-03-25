namespace OS2syncAD
{
    public class ADUtils
    {
        private ADAttributeLoader AttributeLoader { get; set; }

        public ADUtils(ADAttributeLoader attributeLoader)
        {
            AttributeLoader = attributeLoader;
        }

        public ADEvent GetOUParent(ADEvent anOU)
        {
            if (IsRootOU(anOU))
            {
                return null;
            }

            string parentCandidateDN = GetParentString(anOU);
            ADAttributes parentCandidateAttributes = AttributeLoader.Load(parentCandidateDN); // lookup direct parent in Active Directory;
            ADEvent parentCandidate = WrapInEvent(parentCandidateAttributes);

            // we add this check before the “fictive” check to avoid fictive flags on roots
            if (IsRootOU(parentCandidate))
            {
                return parentCandidate;
            }

            // we skip fictive parents
            if (IsFictive(parentCandidate))
            {
                return GetOUParent(parentCandidate);
            }

            return parentCandidate;
        }

        private string GetParentString(ADEvent anOU)
        {
            int idx = anOU.ADAttributes.DistinguishedName.IndexOf(",OU=");
            if (idx < 0)
            {
                idx = anOU.ADAttributes.DistinguishedName.IndexOf(",DC=");
            }

            return anOU.ADAttributes.DistinguishedName.Substring(idx + 1);
        }

        public ADEvent GetImmediateParent(ADEvent anOU)
        {
            if (IsRootOU(anOU))
            {
                return null;
            }

            string parentCandidateDN = GetParentString(anOU);
            ADAttributes parentCandidateAttributes = AttributeLoader.Load(parentCandidateDN); // lookup direct parent in Active Directory;
            ADEvent parentCandidate = WrapInEvent(parentCandidateAttributes);

            return parentCandidate;
        }

        private static ADEvent WrapInEvent(ADAttributes attributes)
        {
            return new ADEvent(0, OperationType.Update, ObjectType.OU, attributes, System.DateTime.Now, null);
        }

        public bool IsRootOU(ADEvent anOU)
        {
            string dn = anOU.ADAttributes.DistinguishedName;

            string rootOU = AppConfiguration.RootOU;
            if (rootOU.ToLower().Equals(dn.ToLower()))
            {
                return true;
            }

            return false;
        }

        public bool IsBlocked(ADEvent anOU)
        {
            if (IsRootOU(anOU))
            {
                return false; // this is how this recursive method stops and returns false
            }
            else if (anOU.ADAttributes.Contains(AppConfiguration.OUAttributeFiltered) && !(anOU.ADAttributes.Attributes[AppConfiguration.OUAttributeFiltered] is ADNullValueAttribute))
            {
                ADSingleValueAttribute filtering = (ADSingleValueAttribute)anOU.ADAttributes.Attributes[AppConfiguration.OUAttributeFiltered];

                return filtering.Value.Equals(Constants.BLOCKED);
            }

            ADEvent parent = GetOUParent(anOU);

            return IsBlocked(parent);
        }

        public bool IsFictive(ADEvent anOU)
        {
            if (anOU.ADAttributes.Contains(AppConfiguration.OUAttributeFiltered) && !(anOU.ADAttributes.Attributes[AppConfiguration.OUAttributeFiltered] is ADNullValueAttribute))
            {
                ADSingleValueAttribute filtering = (ADSingleValueAttribute)anOU.ADAttributes.Attributes[AppConfiguration.OUAttributeFiltered];
                return filtering.Value.Equals(Constants.FICTIVE);
            }

            return false;
        }

        private ADEvent GetUserParent(ADEvent user)
        {
            string parentCandidateDN = GetParentString(user);

            ADAttributes parentCandidateAttributes = AttributeLoader.Load(parentCandidateDN); // lookup direct parent in Active Directory;
            ADEvent parentCandidate = WrapInEvent(parentCandidateAttributes);

            // we add this check before the “fictive” check to avoid fictive flags on roots
            if (IsRootOU(parentCandidate))
            {
                return parentCandidate;
            }
            
            // we skip fictive parents
            if (IsFictive(parentCandidate))
            {
                return GetOUParent(parentCandidate);
            }

            return parentCandidate;
        }

        public bool IsUserBlocked(ADEvent userEvent)
        {
            ADEvent parentOU = GetUserParent(userEvent);

            return IsBlocked(parentOU);
        }

        public bool IsUserFictive(ADEvent userEvent)
        {
            ADEvent parentOU = GetImmediateParent(userEvent);

            return IsFictive(parentOU);
        }
    }
}