
namespace Organisation.IntegrationLayer
{
    internal class ReadSettings
    {
        private int _hierarchyGrouping = 500;
        private int _orgUnitGrouping = 7;
        private int _userGrouping = 50;

        public int UserGrouping
        {
            get
            {
                return _userGrouping;
            }
            set
            {
                if (value > 100)
                {
                    _userGrouping = 100;
                }
                else if (value < 10)
                {
                    _userGrouping = 10;
                }
                else
                {
                    _userGrouping = value;
                }
            }
        }

        public int OrgUnitGrouping
        {
            get
            {
                return _orgUnitGrouping;
            }
            set
            {
                if (value > 10)
                {
                    _orgUnitGrouping = 10;
                }
                else if (value < 3)
                {
                    _orgUnitGrouping = 3;
                }
                else
                {
                    _orgUnitGrouping = value;
                }
            }
        }

        public int HierarchyGrouping
        {
            get
            {
                return _hierarchyGrouping;
            }
            set
            {
                if (value > 500)
                {
                    _hierarchyGrouping = 500;
                }
                else if (value < 50)
                {
                    _hierarchyGrouping = 50;
                }
                else
                {
                    _hierarchyGrouping = value;
                }
            }
        }
    }
}
