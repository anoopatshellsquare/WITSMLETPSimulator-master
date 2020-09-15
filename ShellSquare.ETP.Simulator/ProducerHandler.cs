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
using Energistics.Etp.v12.Protocol.Store;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Animation;
using IStoreStore = Energistics.Etp.v12.Protocol.Store.IStoreStore;

namespace ShellSquare.ETP.Simulator
{
    public class ProducerHandler
    {
        private List<ChannelStreamingInfo> m_ChannelStreamingInfo = new List<ChannelStreamingInfo>();
        public Action<ChannelMetadata> ChannelInfoReceived;
        public Action<string, double, TraceLevel> Message;
        public Action<IList<DataItem>> ChannelDataReceived;
        const string SUBPROTOCOL = "energistics-tp";
        IEtpClient m_client;
        private DateTime m_Time;
        string m_ApplicationName = "ShellSquare ETP Simulator";
        string m_ApplicationVersion = "1.4.1.1";

        private readonly DateTime m_Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public string UserName = "";
        public string Password = "";
        public string URL = "";


        TimeSpan  startTimeSpan = TimeSpan.Zero;
        TimeSpan periodTimeSpan = TimeSpan.FromSeconds(10);
        long index = 0;
        public ChannelMetadata Metadata;
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
               
              

                m_client.Handler<ICoreClient>().OnOpenSession += HandleOpenSession;

              //  m_client.Handler<IStoreStore>().OnPutDataObjects += ProducerHandler_OnPutDataObjects;

                m_client.Register<IChannelStreamingConsumer, ChannelStreamingConsumerHandler >();
                m_client.Register<IChannelStreamingConsumer, ChannelStreamingConsumerHandler>();


                m_client.Register<IChannelStreamingConsumer, ChannelStreamingConsumerHandler>();
                m_client.Handler<IChannelStreamingConsumer>().OnChannelMetadata += HandleChannelMetadata;
                m_client.Handler<IChannelStreamingConsumer>().OnProtocolException += HandleProtocolException;
                m_client.Handler<IChannelStreamingConsumer>().OnChannelData += HandleChannelData;

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




        protected void HandleChannelData(object sender, ProtocolEventArgs<ChannelData> e)
        {
            ChannelDataReceived?.Invoke(e.Message.Data);
        }
        protected void HandleProtocolException(object sender, ProtocolEventArgs<IProtocolException> e)
        {
            var receivedTime = DateTime.UtcNow;
            if (e.Header.MessageType == 1000)
            {
                var timediff = receivedTime - m_Time;
                var bodyrecord = Activator.CreateInstance<ProtocolException>();
                string message = $"Error Received ({e.Message.ErrorCode}): {e.Message.ErrorMessage}";

                message = $"\nResponse : [Protocol {e.Header.Protocol} MessageType {e.Header.MessageType}]\n{message}";

                Message?.Invoke(message, timediff.TotalMilliseconds, TraceLevel.Error);
               // HasDescribing = false;
            }
        }


        protected void HandleChannelMetadata(object sender, ProtocolEventArgs<ChannelMetadata> e)
        {
            var receivedTime = DateTime.UtcNow;
            if (e.Header.Protocol == 1 && e.Header.MessageType == 2)
            {
                var timediff = receivedTime - m_Time;

                string message = "Channels received: [";
                ChannelMetadata metadata = new ChannelMetadata();
                metadata.Channels = new List<ChannelMetadataRecord>();
                lock (m_ChannelStreamingInfo)
                {
                    foreach (var channel in e.Message.Channels)
                    {

                        //if (m_LogCurveEml.Contains(channel.ChannelUri, StringComparer.InvariantCultureIgnoreCase))
                        {
                            metadata.Channels.Add(channel);
                            ChannelStreamingInfo channelStreamingInfo = new ChannelStreamingInfo()
                            {
                                ChannelId = channel.ChannelId,
                                StartIndex = new StreamingStartIndex()
                                {
                                    Item = null
                                },
                                ReceiveChangeNotification = true
                            };

                            m_ChannelStreamingInfo.Add(channelStreamingInfo);
                            message = message + $"\n{channel.ChannelId} {channel.ChannelName} {channel.ChannelUri}";

                            //ChannelMetaDataVM channelMetaData_VM = ETPMapper.Instance().Map<ChannelMetaDataVM>(channel); 
                            //string json = JsonConvert.SerializeObject(channelMetaData_VM, Formatting.Indented);
                            //Message?.Invoke(json, timediff.TotalMilliseconds, TraceLevel.Info);
                        }
                    }

                    Metadata = metadata;

                    ChannelInfoReceived?.Invoke(metadata);
                }

                message = message + "\n]";

                message = $"\nResponse : [Protocol {e.Header.Protocol} MessageType {e.Header.MessageType}]\n{message}";
                Message?.Invoke(message, timediff.TotalMilliseconds, TraceLevel.Info);

               // HasDescribing = false;
            }
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
            var receivedTime = DateTime.UtcNow;
            string message = e.Message.ToMessage();
            message = $"\nResponse : [Protocol {e.Header.Protocol} MessageType {e.Header.MessageType}]\n{message}";
            var timediff = receivedTime - m_Time;
            Message?.Invoke(message, timediff.TotalMilliseconds, TraceLevel.Info);
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



        //public async Task SendChannelMetadata(ChannelMetadata metadata)
        //{
        //    MessageHeader header = new MessageHeader();
        //    header.Protocol = (int)Protocols.ChannelStreaming;
        //    header.MessageType = 2;
        //    header.MessageId = EtpHelper.NextMessageId;
        //    header.MessageFlags = 0;
        //    header.CorrelationId = 0;

        //    var result = m_client.Handler<IChannelStreamingProducer>().ChannelMetadata(header, metadata.Channels);
        //}

       
        private async Task SendChannelDataActual(List<ChannelStreamingInfo> lstChannels)
        {
            var handler = m_client.Handler<IChannelStreamingProducer>();
            //index = index + 1;
            await Task.Run(async () =>
            {
                var receivedTime = DateTime.UtcNow;
                var timediff = receivedTime - m_Time;
                MessageHeader header = new MessageHeader();
                header.Protocol = (int)Protocols.ChannelStreaming  ;
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
                    //d.ChannelId = item.ChannelId;
                    TimeSpan t = (receivedTime - m_Epoch);
                    //DataItem d = new DataItem();
                    d.ChannelId = item.ChannelId;
                    index = (long)t.TotalMilliseconds;

                    d.Indexes = new List<long>();
                    d.Indexes.Add(index);
                    d.Value = new DataValue();
                    d.Value.Item = random.Next();
                    d.ValueAttributes = new List<DataAttribute>();
                    recordData.Data.Add(d);


                    // TimeSpan t = (receivedTime - m_Epoch);
                    //// index = (long)t.TotalMilliseconds;
                    // DataItem d = new DataItem();
                    // d.ChannelId = item.ChannelId;
                    // d.Indexes = new List<long>();
                    // d.Indexes.Add(index);
                    // d.Value = new DataValue();
                    // d.Value.Item = random.Next();//(long)t.TotalMilliseconds;  //Math.Round(random.NextDouble() * 1000, 2);//random.Next();
                    // d.ValueAttributes = new List<DataAttribute>();
                    // recordData.Data.Add(d);
                    // //m_client.Handler<IChannelStreamingProducer>().ChannelData(header, recordData.Data,MessageFlags.None);
                }
                var a = handler.ChannelData(header, recordData.Data);
                //var  a= m_client.Handler<IChannelStreamingProducer>().ChannelData(header, recordData.Data);
                string message = $"\nRequest: [Protocol {header.Protocol} MessageType {header.MessageType}]";
                Message?.Invoke(message + "\nChannel Data processed " + (receivedTime - m_Epoch).ToString(), 0, TraceLevel.Info);
            });
        }

      
        public async Task SendChannelData(List<ChannelStreamingInfo> lstChannels)
        {
            while (true)
            {

                var handler = m_client.Handler<IChannelStreamingProducer>();
                //index = index + 1;
                await Task.Run(async () =>
                {
                    MessageHeader header = new MessageHeader();
                    header.Protocol = (int)Protocols.ChannelStreaming;
                    header.MessageType = 3;
                    header.MessageId = EtpHelper.NextMessageId;
                    header.MessageFlags = 0;
                    header.CorrelationId = 0;

                    var recordData = Activator.CreateInstance<ChannelData>();
                    recordData.Data = new List<DataItem>();

                    Random random = new Random();
                    var receivedTime = DateTime.UtcNow;
                    foreach (var item in lstChannels)
                    {
                        DataItem d = new DataItem();
                        //d.ChannelId = item.ChannelId;
                        TimeSpan t = (receivedTime - m_Epoch);
                        //DataItem d = new DataItem();
                        d.ChannelId = item.ChannelId;
                        index = (long)t.TotalMilliseconds;

                        d.Indexes = new List<long>();
                        d.Indexes.Add(index);
                        d.Value = new DataValue();
                        d.Value.Item = random.Next();
                        d.ValueAttributes = new List<DataAttribute>();
                        recordData.Data.Add(d);
                    }
                    var a = handler.ChannelData(header, recordData.Data);
                    //string message = $"\nRequest: [Protocol {header.Protocol} MessageType {header.MessageType}]";
                    //Message?.Invoke(message + "\nChannel Data processed " + (receivedTime - m_Epoch).ToString(), 0, TraceLevel.Info);
                });
                await Task.Delay(TimeSpan.FromSeconds(10));
            }


            //while (true)
            //{

            //    await SendChannelDataActual(lstChannels);
            //    await Task.Delay(TimeSpan.FromSeconds(10));

            //    ////New Code ..Need to check is this required or not
            //    //var handler = m_client.Handler<IChannelStreamingProducer>();
            //    ////index = index + 1;
            //    ////var timer = new System.Threading.Timer((e) =>
            //    ////{
            //    //    await Task.Run(async () =>
            //    //    {
            //    //        var receivedTime = DateTime.UtcNow;
            //    //        var timediff = receivedTime - m_Time;

            //    //        MessageHeader header = new MessageHeader();
            //    //        header.Protocol = (int)Protocols.ChannelStreaming;
            //    //        header.MessageType = 3;
            //    //        header.MessageId = EtpHelper.NextMessageId;
            //    //        header.MessageFlags = 0;
            //    //        header.CorrelationId = 0;
            //    //        var recordData = Activator.CreateInstance<ChannelData>();
            //    //        recordData.Data = new List<DataItem>();
            //    //        Random random = new Random();
            //    //        foreach (var item in lstChannels)
            //    //        {
            //    //            TimeSpan t = (receivedTime - m_Epoch);
            //    //            index = (long)t.TotalMilliseconds;
            //    //            DataItem d = new DataItem();
            //    //            d.ChannelId = item.ChannelId;
            //    //            d.Indexes = new List<long>();
            //    //            d.Indexes.Add(index);
            //    //            d.Value = new DataValue();
            //    //            d.Value.Item = Math.Round(random.NextDouble() * 1000, 2);//random.Next();
            //    //            d.ValueAttributes = new List<DataAttribute>();
            //    //            recordData.Data.Add(d);
            //    //        }
            //    //        m_client.Handler<IChannelStreamingProducer>().ChannelData(header, recordData.Data);
            //    //        string message = $"\nRequest: [Protocol {header.Protocol} MessageType {header.MessageType}]";
            //    //        Message?.Invoke(message + "\nChannel Data processed " + (receivedTime - m_Epoch).ToString(), 0, TraceLevel.Info);
            //    //    });
            //    //    await Task.Delay(TimeSpan.FromSeconds(10));
            //    ////}, null, startTimeSpan, periodTimeSpan);

            //}
        }
    }
}
