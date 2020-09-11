using Energistics.Etp;
using Energistics.Etp.Common;
using Energistics.Etp.Common.Datatypes;
using Energistics.Etp.Common.Protocol.Core;
using Energistics.Etp.Security;
using Energistics.Etp.v11;
using Energistics.Etp.v11.Datatypes;
using Energistics.Etp.v11.Datatypes.ChannelData;
using Energistics.Etp.v11.Protocol.ChannelStreaming;
using Energistics.Etp.v11.Protocol.Core;
using Energistics.Etp.v11.Protocol.Discovery;
using Energistics.Etp.v11.Protocol.Store;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Animation;

namespace ShellSquare.ETP.Simulator
{
    public class ProducerHandler
    {
        public Action<ChannelMetadata> ChannelInfoReceived;
        public Action<string, double, TraceLevel> Message;
        const string SUBPROTOCOL = "energistics-tp";
        IEtpClient m_client;
        private DateTime m_Time;
        string m_ApplicationName = "ShellSquare ETP Simulator";
        string m_ApplicationVersion = "1.4.1.1";

        public string UserNameS = "";
        public string Password = "";
        public string URL = "";
        public async Task Connect(string url, string username, string password, CancellationToken token)
        {
            try
            {
                m_Time = DateTime.UtcNow;

                var protocols = new List<SupportedProtocol>();
                SupportedProtocol p;
                p = EtpHelper.ToSupportedProtocol(Protocols.ChannelStreaming, "consumer");
                protocols.Add(p);

                p = EtpHelper.ToSupportedProtocol(Protocols.Discovery, "store");
                protocols.Add(p);


                var auth = Authorization.Basic(username, password);
                m_client = EtpFactory.CreateClient(WebSocketType.WebSocket4Net, url, m_ApplicationName, m_ApplicationVersion, SUBPROTOCOL, auth);

                //m_client.Register<IChannelStreamingConsumer, ChannelStreamingConsumerHandler>();
                m_client.Register<IChannelStreamingProducer, ChannelStreamingProducerHandler>();

                m_client.Register<IDiscoveryCustomer, DiscoveryCustomerHandler>();
                m_client.Register<IStoreCustomer, StoreCustomerHandler>();
                m_client.Handler<ICoreClient>().OnOpenSession += HandleOpenSession;


                m_client.Register<IChannelStreamingConsumer, ChannelStreamingConsumerHandler >();
                m_client.Register<IChannelStreamingConsumer, ChannelStreamingConsumerHandler>();
                m_client.Handler<IChannelStreamingConsumer>().OnChannelMetadata += ProducerHandler_OnChannelMetadata;
                m_client.Handler<IChannelStreamingConsumer>().OnProtocolException += ProducerHandler_OnProtocolException; ;
                m_client.Handler<IChannelStreamingConsumer>().OnChannelData += ProducerHandler_OnChannelData; 



                CreateSession(protocols);
                await m_client.OpenAsync();

            }
            catch (Exception ex)
            {

                if (ex.InnerException != null)
                {
                    throw new Exception($"{ex.Message} {ex.InnerException.Message}");
                }
                throw;
            }
        }

        private void ProducerHandler_OnChannelData(object sender, ProtocolEventArgs<ChannelData> e)
        {
            string a = "";
            throw new NotImplementedException();
        }

        private void ProducerHandler_OnProtocolException(object sender, ProtocolEventArgs<IProtocolException> e)
        {


            string a = "";
           // throw new NotImplementedException();
        }

        private void ProducerHandler_OnChannelStreamingStop(object sender, ProtocolEventArgs<ChannelStreamingStop> e)
        {

            
            //throw new NotImplementedException();
        }

        private void ProducerHandler_OnAcknowledge1(object sender, ProtocolEventArgs<IAcknowledge> e)
        {
            throw new NotImplementedException();
        }

        private void ProducerHandler_OnChannelMetadata(object sender, ProtocolEventArgs<ChannelMetadata> e)
        {
          
            //throw new NotImplementedException();
        }

        private void ProducerHandler_OnChannelDescribe(object sender, ProtocolEventArgs<ChannelDescribe, IList<ChannelMetadataRecord>> e)
        {
           
            //throw new NotImplementedException();
        }

        private void ProducerHandler_OnStart(object sender, ProtocolEventArgs<Start> e)
        {
            String a = "";
            string message = $"\nRequest: [Protocol {e.Header .Protocol } MessageType {e.Header.MessageType}]";
            message= e.Message.ToString();
            Message?.Invoke(message, 123456, TraceLevel.Info);
        }

        private void ProducerHandler_OnAcknowledge(object sender, ProtocolEventArgs<IAcknowledge> e)
        {
            string a = "";
           // throw new NotImplementedException();
        }

        private void ProducerHandler_OnChannelStreamingStart(object sender, ProtocolEventArgs<ChannelStreamingStart> e)
        {
            string a = "";
            Message?.Invoke("response from Simulate just Test", 0, TraceLevel.Info);
            //  throw new NotImplementedException();
        }


        private void CreateSession(List<SupportedProtocol> protocols)
        {
            MessageHeader header = new MessageHeader();
            header.Protocol = (int)Protocols.Core;
            header.MessageType = 1;
            header.MessageId = EtpHelper.NextMessageId;
            header.MessageFlags = 0;
            header.CorrelationId = 0;

            List<ISupportedProtocol> requestedProtocols = new List<ISupportedProtocol>();
            requestedProtocols.AddRange(protocols);

            var requestSession = new RequestSession()
            {
                ApplicationName = m_ApplicationName,
                ApplicationVersion = m_ApplicationVersion,
                RequestedProtocols = requestedProtocols.Cast<SupportedProtocol>().ToList(),
                SupportedObjects = new List<string>()
            };

            string message = $"\nRequest: [Protocol {header.Protocol} MessageType {header.MessageType}]";
            Message?.Invoke(message, 0, TraceLevel.Info);
            m_client.Handler<ICoreClient>().RequestSession(m_ApplicationName, m_ApplicationVersion, requestedProtocols);
        }


        protected void HandleOpenSession(object sender, ProtocolEventArgs<OpenSession> e)
        {

            String a = "";
            //var receivedTime = DateTime.UtcNow;
            //lock (m_ConnectionLock)
            //{
            //    m_HasConnected = true;
            //}
            //string message = ToString(e.Message);
            //message = $"\nResponse : [Protocol {e.Header.Protocol} MessageType {e.Header.MessageType}]\n{message}";
            //var timediff = receivedTime - m_Time;
            //Message?.Invoke(message, timediff.TotalMilliseconds, TraceLevel.Info);
        }


        public async Task SendChannelMetadata(ChannelMetadata metadata)
        {
            MessageHeader header = new MessageHeader();
            header.Protocol = (int)Protocols.ChannelStreaming;
            header.MessageType = 2;
            header.MessageId = EtpHelper.NextMessageId;
            header.MessageFlags = 0;
            header.CorrelationId = 0;

            var result = m_client.Handler<IChannelStreamingProducer>().ChannelMetadata(header, metadata.Channels);
        }

        int index = 0;
        public async Task SendChannelData(List<ChannelStreamingInfo> lstChannels)
        {

            while (true)
            {

                //New Code ..Need to check is this required or not
                var auth = Authorization.Basic(UserNameS, Password);
                m_client = EtpFactory.CreateClient(WebSocketType.WebSocket4Net, URL, m_ApplicationName, m_ApplicationVersion, SUBPROTOCOL, auth);
                m_client.Register<IChannelStreamingProducer, ChannelStreamingProducerHandler>();

                var handler = m_client.Handler<IChannelStreamingProducer>();

                index = index + 1;
                await Task.Run(async () =>
                {
                    var receivedTime = DateTime.UtcNow;
                    var timediff = receivedTime - m_Time;

                    MessageHeader header = new MessageHeader();
                    header.Protocol = (int)Protocols.ChannelStreaming;
                    header.MessageType = 3;
                    header.MessageId = EtpHelper.NextMessageId;
                    header.MessageFlags = 0;
                    header.CorrelationId = 0;

                    var recordData = Activator.CreateInstance<ChannelData>();
                    recordData.Data = new List<DataItem>();

                    Random random = new Random();
                    
                    foreach (var item in lstChannels)
                    {
                        DataItem d = new DataItem();
                        d.ChannelId = item.ChannelId;

                        d.Indexes = new List<long>();
                        d.Indexes.Add(index);
                        d.Value = new DataValue();
                        d.Value.Item = random.Next();

                        d.ValueAttributes = new List<DataAttribute>();
                        recordData.Data.Add(d);
                    }

                    //handler.ChannelData(header, recordData.Data);
                    
                    m_client.Handler<IChannelStreamingProducer>().ChannelData(header, recordData.Data);
                    string message = $"\nRequest: [Protocol {header.Protocol} MessageType {header.MessageType}]";
                    Message?.Invoke(message+ "\nChannel Data processed" +index , 0, TraceLevel.Info);

                });
                
                await Task.Delay(TimeSpan.FromSeconds(10));
               
            }
        }

    }
}
