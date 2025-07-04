﻿using System;
using System.Numerics;

namespace DevelopersHub.RealtimeNetworking.Server
{
    class Terminal
    {

        #region Update
        public const int updatesPerSecond = 30;
        public static void Update()
        {
            
        }
        #endregion

        #region Connection
        public const int maxPlayers = 100000;
        public const int port = 5555;
        public static void OnClientConnected(int id, string ip)
        {
            
        }

        public static void OnClientDisconnected(int id, string ip)
        {
            
        }
        #endregion

        #region Data
        public enum RequestsID
        {
            AUTH = 1, SYNC = 2, BUILD = 3
        }
        public static void ReceivedPacket(int clientID, Packet packet)
        {
            // For test, remove it ->
            int id = packet.ReadInt();
            string device = "";
            
            switch((RequestsID)id) {
                case RequestsID.AUTH:
                    device = packet.ReadString();
                    Database.AuthenticatePlayer(clientID, device);
                    break;
                case RequestsID.SYNC:
                    device = packet.ReadString();
                    Database.SyncPlayerData(clientID, device);
                    break;
                case RequestsID.BUILD:
                    device = packet.ReadString();
                    string building = packet.ReadString();
                    int x = packet.ReadInt();
                    int y = packet.ReadInt();
                    Database.PlaceBuilding(clientID, device, building, x, y);
                    break;
            }
        }

        public static void ReceivedBytes(int clientID, int packetID, byte[] data)
        {
            
        }

        public static void ReceivedString(int clientID, int packetID, string data)
        {
            
        }

        public static void ReceivedInteger(int clientID, int packetID, int data)
        {
            
        }

        public static void ReceivedFloat(int clientID, int packetID, float data)
        {

        }

        public static void ReceivedBoolean(int clientID, int packetID, bool data)
        {

        }

        public static void ReceivedVector3(int clientID, int packetID, Vector3 data)
        {

        }

        public static void ReceivedQuaternion(int clientID, int packetID, Quaternion data)
        {

        }

        public static void ReceivedLong(int clientID, int packetID, long data)
        {

        }

        public static void ReceivedShort(int clientID, int packetID, short data)
        {

        }

        public static void ReceivedByte(int clientID, int packetID, byte data)
        {

        }

        public static void ReceivedEvent(int clientID, int packetID)
        {

        }
        #endregion

    }
}