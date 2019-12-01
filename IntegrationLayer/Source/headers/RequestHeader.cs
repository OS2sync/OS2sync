using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;
using System.Xml;

namespace Organisation.IntegrationLayer
{
    class RequestHeader : MessageHeader
    {
        private string uuid;
        public override string Name => "RequestHeader";
        public override string Namespace => "http://kombit.dk/xml/schemas/RequestHeader/1/";

        public RequestHeader(string uuid)
        {
            this.uuid = uuid;
        }

        protected override void OnWriteHeaderContents(XmlDictionaryWriter writer, MessageVersion messageVersion)
        {
            writer.WriteElementString("TransactionUUID", uuid);
        }
    }
}
