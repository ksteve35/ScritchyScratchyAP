using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Helpers;
using System;
using System.Collections.Generic;

namespace ScritchyScratchyAP
{
    public static class ArchipelagoManager
    {
        public static ArchipelagoSession Session { get; private set; }
        public static bool Connected { get; private set; } = false;

        public static string TryConnect(string host, int port, string slotName, string password = "")
        {
            // If already connected as the same player, do nothing
            if (Connected &&
                Session.Players.GetPlayerName(Session.ConnectionInfo.Slot) == slotName &&
                Session.Socket.Uri.Host == host &&
                Session.Socket.Uri.Port.ToString() == port.ToString())
            {
                return "";
            }

            TryDisconnect();

            Plugin.Log.LogInfo($"AP: Connecting to {host}:{port} as {slotName}...");

            LoginResult loginResult;

            try
            {
                Session = ArchipelagoSessionFactory.CreateSession(host, port);
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"AP: Failed to create session: {e.Message}");
                return $"Failed to create session: {e.Message}";
            }

            // Hook up error listener BEFORE attempting login
            // so we can see the exception
            Session.Socket.ErrorReceived += (exception, message) =>
            {
                Plugin.Log.LogError($"AP: Socket error received!");
                Plugin.Log.LogError($"AP: Message: {message}");
                Plugin.Log.LogError($"AP: Exception: {exception}");
            };

            Session.Socket.SocketClosed += (reason) =>
            {
                Plugin.Log.LogError($"AP: Socket closed! Reason: {reason}");
            };

            // Subscribe BEFORE logging in. With ItemsHandlingFlags.AllItems, the server
            // replays the entire item history as part of the login, and the
            // client library can raise ItemReceived for that burst before
            // TryConnectAndLogin() even returns. Subscribing after login is a race.
            TrackingManager.ResetReceivedItems();
            ItemApplicator.ResetAppliedLevels();
            Session.Items.ItemReceived += OnItemReceived;

            try
            {
                loginResult = Session.TryConnectAndLogin(
                    "Scritchy Scratchy",
                    slotName,
                    ItemsHandlingFlags.AllItems,
                    password: password == "" ? null : password
                );
            }
            catch (Exception e)
            {
                loginResult = new LoginFailure(e.GetBaseException().Message);
            }

            if (loginResult is LoginSuccessful loginSuccess)
            {
                Connected = true;
                Plugin.Log.LogInfo($"AP: Successfully connected as {slotName}!");
                return "";
            }
            else
            {
                var failure = (LoginFailure)loginResult;
                string errorMsg = "";
                Plugin.Log.LogError("AP: Connection failed!");
                foreach (var err in failure.Errors)
                {
                    Plugin.Log.LogError($"  - {err}");
                    errorMsg = err;
                }
                foreach (var code in failure.ErrorCodes)
                {
                    Plugin.Log.LogError($"  - ErrorCode: {code}");
                }
                TryDisconnect();
                return errorMsg;
            }
        }

        public static void TryDisconnect()
        {
            if (Session != null)
            {
                try { Session.Socket.DisconnectAsync(); } catch { }
            }
            Connected = false;
            Session = null;
        }

        public static void SendCheck(long locationId)
        {
            if (!Connected) return;
            Session.Locations.CompleteLocationChecksAsync(locationId);
            Plugin.Log.LogInfo($"AP: Sent check for location ID {locationId}");
        }

        private static void OnItemReceived(ReceivedItemsHelper helper)
        {
            var item = helper.DequeueItem();
            Plugin.Log.LogInfo($"AP: Received item '{item.ItemName}'");
            // IncrementReceivedItem is safe to call here (no IL2CPP game objects).
            // ApplyItem touches Unity objects, so enqueue it to run on the main thread.
            int count = TrackingManager.IncrementReceivedItem(item.ItemName);
            string name = item.ItemName;
            APUpdateManager.Enqueue(() => ItemApplicator.ApplyItem(name, count));
        }
    }
}