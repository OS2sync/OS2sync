namespace Organisation.IntegrationLayer
{
    internal enum UpdateIndicator
    {
        NONE,       // do not do anything with this field
        ADD,        // only add the supplied references, do not remove anything
        REMOVE,     // only remove the supplied references, do not add anything
        COMPARE     // compare the supplied references with the existing and add/remove the difference
    }
}
