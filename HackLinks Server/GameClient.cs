﻿using HackLinks_Server.Computers;
using HackLinks_Server.Computers.Permissions;
using HackLinks_Server.Computers.Processes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HackLinks_Server.Computers.DataObjects;
using HackLinks_Server.Util;
using static HackLinksCommon.NetUtil;

namespace HackLinks_Server
{
    public class GameClient
    {

        public Socket client;
        public Server server;

        public ServerAccount account;

        public Session activeSession;
        public List<Permissions> permissions => account.permissions;
        public Node homeComputer;

        public string buffer = "";

        public enum PlayerStatus
        {
            ONLINE,
            DISCONNECTING,
            TERMINATED
        }

        public PlayerStatus status = PlayerStatus.ONLINE;

        public int UserId { get; internal set; }

        public GameClient(Socket client, Server server)
        {
            this.client = client;
            this.server = server;
        }

        public void ConnectTo(Node node)
        {
            Send(PacketType.KERNL, "connect", "succ", node.ip, "3");
            Login(node, new Credentials(node.GetUserId("guest"), Group.GUEST));
        }

        public void Login(Node node, Credentials credentials)
        {
            Send(PacketType.KERNL, "login", ((int)credentials.Group).ToString(), account.username);

            // TODO query passwd for shell
            Process process = CreateProcess(node, "HASH", credentials, (Process.Printer)(input => Send(PacketType.MESSG, input)));
            activeSession = new Session(this, node, process);
        }

        public Process CreateProcess(Node node, string type, Process parent)
        {
            return CreateProcess(node, type, parent.Credentials, parent.Print);
        }

        private Process CreateProcess(Node node, string type, Credentials credentials, Process.Printer printer)
        {
            return CreateProcess(node, Type.GetType($"HackLinks_Server.Computers.Processes.{type}"), credentials, printer);
        }

        private Process CreateProcess(Node node, Type type, Credentials credentials, Process.Printer printer)
        {
            Logger.Debug(type);
            object[] args;
            if( type == typeof(ServerAdmin))
            {
                args = new object[] { node.NextPID, printer, node, credentials, this };
            } else
            {
                args = new object[] { node.NextPID, printer, node, credentials };
            }
            return (Process)Activator.CreateInstance(type, args);
        }

        public void Disconnect()
        {
            if(activeSession != null)
            {
                activeSession.DisconnectSession();
                activeSession = null;
                Send(PacketType.KERNL, "disconnect");
            }
        }

        public void Start()
        {
            try
            {
                StateObject state = new StateObject();

                client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                    new AsyncCallback(ReadCallback), state);
            }
            catch (Exception e)
            {
                Logger.Exception(e);
                netDisconnect();
            }
        }

        public void ReadCallback(IAsyncResult ar)
        {
            try
            {
                String content = String.Empty;

                StateObject state = (StateObject)ar.AsyncState;

                int bytesRead = client.EndReceive(ar);

                if (bytesRead > 0)
                {
                    state.sb.Append(Encoding.ASCII.GetString(
                        state.buffer, 0, bytesRead));

                    content = state.sb.ToString();

                    Logger.Debug($"Received Data: \"{content.Replace("{","{{").Replace("}","}}")}\"");

                    List<Packet> packets = ParsePackets(content);

                    foreach (Packet packet in packets)
                    {
                        server.TreatMessage(this, packet.Type, packet.Data);
                    }

                    state.sb.Clear();
                    
                    client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                    new AsyncCallback(ReadCallback), state);
                }
            }
            catch(Exception ex)
            {
                Logger.Exception(ex);
                netDisconnect();
            }
        }

        public void netDisconnect()
        {
            this.status = PlayerStatus.DISCONNECTING;
            //client.Disconnect(false);
            server.RemoveClient(this);
            client.Dispose();
        }

        public void Send(PacketType type, params string[] data)
        {
            try
            {
                JObject packet = new JObject
                {
                    {"type", type.ToString()},
                    {"data", new JArray(data)},
                };

                // Convert the string data to byte data using ASCII encoding.
                byte[] byteData = Encoding.ASCII.GetBytes(packet.ToString());

                // Begin sending the data to the remote device.
                client.BeginSend(byteData, 0, byteData.Length, 0,
                    new AsyncCallback(SendCallback), client);
            }
            catch(Exception ex)
            {
                Logger.Exception(ex);
                netDisconnect();
            }
        }

        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                int bytesSent = client.EndSend(ar);
            }
            catch (Exception e)
            {
                Logger.Exception(e);
                netDisconnect();
            }
        }

        public void TraceTermination()
        {
            if(this.activeSession != null)
            {
                activeSession.traceSpd = 0;
                activeSession.trace = 0;
            }
            

            Send(PacketType.FX, "traceOver");
            Disconnect();
            status = PlayerStatus.TERMINATED;
        }
    }
}
