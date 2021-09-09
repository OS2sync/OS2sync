namespace Organisation.IntegrationLayer
{
    internal enum UpdateIndicator
    {
        NONE,       // do not do anything with this field
        COMPARE     // compare the supplied references with the existing and add/remove the difference
    }
}
