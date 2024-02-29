
namespace OS2syncAD
{
    public class Filter
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private ADUtils ADUtils;

        public Filter(ADUtils adUtils)
        {
            ADUtils = adUtils;
        }

        public bool ShouldWeSynchronize(ADEvent anEvent)
        {
            if (anEvent.AffectedObjectType.Equals(ObjectType.OU))
            {
                return ShouldWeSynchronizeOU(anEvent);
            }

            return ShouldWeSynchronizeUser(anEvent);
        }
       
        private bool ShouldWeSynchronizeOU(ADEvent ou)
        {
            bool isFictive = ADUtils.IsFictive(ou);
            bool isBlocked = ADUtils.IsBlocked(ou);

            if (isFictive || isBlocked)
            {
                if (AppConfiguration.CleanupOUJobDryRun)
                {
                    log.Info("Filtering " + ou.ADAttributes.DistinguishedName + " : fictive=" + isFictive + ", blocked=" + isBlocked);
                }

                return false;
            }

            return true;
        }

        private bool ShouldWeSynchronizeUser(ADEvent user)
        {
            bool isBlocked = ADUtils.IsUserBlocked(user);

            if (isBlocked)
            {
                return false;
            }

            return true;
        }
    }
}
