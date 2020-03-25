
namespace OS2syncAD
{
    public class Filter
    {
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
