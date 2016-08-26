﻿using System.Collections.Generic;
using System.Net.Mqtt.Diagnostics;
using System.Net.Mqtt.Packets;
using System.Net.Mqtt.Server;
using System.Net.Mqtt.Storage;

namespace System.Net.Mqtt.Flows
{
    internal class ServerProtocolFlowProvider : ProtocolFlowProvider
	{
		readonly IMqttAuthenticationProvider authenticationProvider;
		readonly IConnectionProvider connectionProvider;
		readonly IPacketIdProvider packetIdProvider;
		readonly IEventStream eventStream;

		public ServerProtocolFlowProvider (IMqttAuthenticationProvider authenticationProvider,
			IConnectionProvider connectionProvider,
			IMqttTopicEvaluator topicEvaluator,
			IRepositoryProvider repositoryProvider,
			IPacketIdProvider packetIdProvider,
			IEventStream eventStream,
			ITracerManager tracerManager,
			MqttConfiguration configuration)
			: base (topicEvaluator, repositoryProvider, tracerManager, configuration)
		{
			this.authenticationProvider = authenticationProvider;
			this.connectionProvider = connectionProvider;
			this.packetIdProvider = packetIdProvider;
			this.eventStream = eventStream;
		}

		protected override IDictionary<ProtocolFlowType, IProtocolFlow> InitializeFlows ()
		{
			var flows = new Dictionary<ProtocolFlowType, IProtocolFlow>();

			var sessionRepository = repositoryProvider.GetRepository<ClientSession>();
			var willRepository = repositoryProvider.GetRepository<ConnectionWill> ();
			var retainedRepository = repositoryProvider.GetRepository<RetainedMessage> ();
			var senderFlow = new PublishSenderFlow (sessionRepository, tracerManager, configuration);

			flows.Add (ProtocolFlowType.Connect, new ServerConnectFlow (authenticationProvider, sessionRepository, willRepository, senderFlow, tracerManager));
			flows.Add (ProtocolFlowType.PublishSender, senderFlow);
			flows.Add (ProtocolFlowType.PublishReceiver, new ServerPublishReceiverFlow (topicEvaluator, connectionProvider,
				senderFlow, retainedRepository, sessionRepository, willRepository, packetIdProvider, eventStream, tracerManager, configuration));
			flows.Add (ProtocolFlowType.Subscribe, new ServerSubscribeFlow (topicEvaluator, sessionRepository,
				retainedRepository, packetIdProvider, senderFlow, tracerManager, configuration));
			flows.Add (ProtocolFlowType.Unsubscribe, new ServerUnsubscribeFlow (sessionRepository));
			flows.Add (ProtocolFlowType.Ping, new PingFlow ());
			flows.Add (ProtocolFlowType.Disconnect, new DisconnectFlow (connectionProvider, sessionRepository, willRepository, tracerManager));

			return flows;
		}

		protected override bool IsValidPacketType (MqttPacketType packetType)
		{
			return packetType == MqttPacketType.Connect ||
				packetType == MqttPacketType.Subscribe ||
				packetType == MqttPacketType.Unsubscribe ||
				packetType == MqttPacketType.Publish ||
				packetType == MqttPacketType.PublishAck ||
				packetType == MqttPacketType.PublishComplete ||
				packetType == MqttPacketType.PublishReceived ||
				packetType == MqttPacketType.PublishRelease ||
				packetType == MqttPacketType.PingRequest ||
				packetType == MqttPacketType.Disconnect;
		}
	}
}
