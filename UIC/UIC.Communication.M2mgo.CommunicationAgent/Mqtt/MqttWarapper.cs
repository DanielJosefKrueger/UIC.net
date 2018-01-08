﻿using System;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using UIC.Communication.M2mgo.CommunicationAgent.Mqtt.messaging;
using UIC.Framework.Interfaces.Edm.Value;
using UIC.Util.Logging;

namespace UIC.Communication.M2mgo.CommunicationAgent.Mqtt {
    internal class MqttWarapper {
        private MqttClient _mqttClient;
        private readonly MqttConnectionWatchdog _mqttConnectionWatchdog;
        private readonly ILogger _logger;
        private Action<Command> _handler;

        public MqttWarapper(MqttConnectionWatchdog connectionWatchdog, ILogger logger)
        {
            _mqttConnectionWatchdog = connectionWatchdog;
            _logger = logger;
        }

        internal void Connect(M2mgoMqttParams param, Action<Command> handler)
        {
            _handler = handler;
            _mqttClient = new MqttClient(param.BrokerUrl, param.BrokerPort, !param.DeactivateSecureChannel, MqttSslProtocols.TLSv1_1, userCertificateSelectionCallback, userCertificateValidationCallback);

            _mqttClient.MqttMsgPublishReceived += MqttClientOnMqttMsgPublishReceived;
            _mqttClient.MqttMsgPublished += MqttClientOnMqttMsgPublished;
            //_mqttClient.ProtocolVersion = MqttProtocolVersion.Version_3_1;

            _logger.Information("Trying to connect to broker: " + param.BrokerUrl);
            _mqttConnectionWatchdog.Connect(_mqttClient);
        }

        private void MqttClientOnMqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        {
            string command = Encoding.UTF8.GetString(e.Message);
            _logger.Debug("Received Mesage: " + e.Topic + " - " + command);
            if (_handler != null)
            {
                _handler(command);
            }
        }

        private void MqttClientOnMqttMsgPublished(object sender, MqttMsgPublishedEventArgs e)
        {
            _logger.Debug("Published Event for MessageId " + e.MessageId + " - " + e.IsPublished);
        }

        private X509Certificate userCertificateValidationCallback(object sender, string targethost, X509CertificateCollection localcertificates, X509Certificate remotecertificate, string[] acceptableissuers)
        {
            return null;
        }

        private bool userCertificateSelectionCallback(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslpolicyerrors)
        {
            return true;
        }

        public void Dispose()
        {
            if (_mqttConnectionWatchdog != null)
            {
                _mqttConnectionWatchdog.Dispose();
            }
        }

        public void Pulish(M2MgoPublishMessage msg)
        {
            var topic = msg.GetTopic();
            var payloadString = msg.GetPayload();
            _logger.Information("Publish {0}: {1}", topic, payloadString);
            _mqttClient.Publish(topic, Encoding.UTF8.GetBytes(payloadString));
        }
    }
}
