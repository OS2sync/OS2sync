using System;
using IntegrationLayer.Organisation;

namespace Organisation.IntegrationLayer
{
    internal class OrganisationStubHelper
    {
        internal const string SERVICE = "organisation/6";

        internal void AddOverordnetEnhed(string overordnetEnhedUUID, VirkningType virkning, RegistreringType1 registration)
        {
            UnikIdType orgUnitReference = StubUtil.GetReference<UnikIdType>(overordnetEnhedUUID, ItemChoiceType.UUIDIdentifikator);

            OrganisationEnhedRelationType organisationEnhedRelationType = new OrganisationEnhedRelationType();
            organisationEnhedRelationType.Virkning = virkning;
            organisationEnhedRelationType.ReferenceID = orgUnitReference;

            registration.RelationListe.Overordnet = organisationEnhedRelationType;
        }

        internal VirkningType GetVirkning(DateTime timestamp)
        {
            TidspunktType beginTime = new TidspunktType();
            beginTime.Item = timestamp.Date + new TimeSpan(0, 0, 0);

            VirkningType virkning = new VirkningType();
            virkning.AktoerRef = GetOrganisationReference();
            virkning.AktoerTypeKode = AktoerTypeKodeType.Organisation;
            virkning.AktoerTypeKodeSpecified = true;
            virkning.FraTidspunkt = beginTime;

            return virkning;
        }

        private UnikIdType GetOrganisationReference()
        {
            return StubUtil.GetReference<UnikIdType>(StubUtil.GetMunicipalityOrganisationUUID(), ItemChoiceType.UUIDIdentifikator);
        }
    }
}
