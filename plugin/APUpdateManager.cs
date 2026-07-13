using BepInEx.Unity.IL2CPP.Utils.Collections;
using System;
using System.Collections;
using System.Collections.Concurrent;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ScritchyScratchyAP
{
    public class APUpdateManager : MonoBehaviour
    {
        // AP callbacks fire on a background thread. Any call into IL2CPP game
        // objects from that thread causes an AccessViolationException. This queues
        // those calls here and drains them each frame on the Unity main thread.
        private static readonly ConcurrentQueue<Action> _mainThreadQueue = new();

        // Occasionally retry ApplyAll so items received before the shop was
        // active, or before PopulateShop fired, eventually get applied.
        private float _shopApplyTimer = 3f;

        // Set in Awake so ConnectionGUI can trigger a reconnect
        // with new connection details via RequestReconnect below.
        private static APUpdateManager _instance;

        // Opening the F1 connection panel and clicking Connect connects the player to
        // the AP server with the parameters they type in. This guards the one-time save
        // load so it still happens exactly once, on whichever connect attempt is first.
        private static bool _trackingInitialized = false;

        public static void Enqueue(Action action) => _mainThreadQueue.Enqueue(action);

        // Called by ConnectionGUI when the player submits new connection details.
        // Runs the same disconnect then reconnect flow as F6, just with supplied
        // host/port/slot/password params instead of hardcoded values.
        public static void RequestReconnect(string host, int port, string slotName, string password)
        {
            if (_instance == null)
            {
                Plugin.Log.LogWarning("AP: RequestReconnect called before APUpdateManager was ready.");
                return;
            }
            if (!_trackingInitialized)
            {
                // Load save BEFORE connecting. TryConnect calls ResetReceivedItems() on
                // login. By loading first, _data.SentLocations is already populated so that the
                // in-memory-only clear in ResetReceivedItems() doesn't lose it.
                TrackingManager.Initialize();
                _trackingInitialized = true;
            }
            _instance.StartCoroutine(_instance.ReconnectCoroutine(host, port, slotName, password).WrapToIl2Cpp());
        }

        void Awake()
        {
            _instance = this;
        }

        void Update()
        {
            while (_mainThreadQueue.TryDequeue(out Action action))
            {
                try { action(); }
                catch (Exception ex)
                {
                    Plugin.Log.LogError($"AP: Main-thread action threw: {ex.Message}\n{ex.StackTrace}");
                }
            }

            var kb = Keyboard.current;
            if (kb != null)
            {
                // F5: instantly cash out the current ticket (testing shortcut)
                if (kb.f5Key.wasPressedThisFrame)
                {
                    try
                    {
                        var scratching = UnityEngine.Object.FindObjectOfType<PlayerScratching>(true);
                        var ticket = scratching?.CurrentTicket;
                        if (ticket == null)
                        {
                            Plugin.Log.LogWarning("AP: F5 — no active ticket.");
                        }
                        else
                        {
                            ticket.AllScratched = true;
                            ticket.AutoScratched = true;
                            ticket.ShowCashOutButton(true);
                            // Try button click first, fall back to invoking the OnCashedOut action directly
                            if (ticket.cashOutButton != null)
                                ticket.cashOutButton.onClick.Invoke();
                            else
                                ticket.OnCashedOut?.Invoke();
                            Plugin.Log.LogInfo("AP: F5 — instant cash-out triggered.");
                        }
                    }
                    catch (Exception ex)
                    {
                        Plugin.Log.LogError($"AP: F5 instant scratch failed: {ex.Message}");
                    }
                }

                // F6: wipe AP save.json and reset all in-memory tracking (testing shortcut).
                // After wiping, force a full disconnect and reconnect so the server replays
                // all previously received items into the freshly cleared ReceivedItemCounts.
                // Without the reconnect, the server won't re-send items.
                if (kb.f6Key.wasPressedThisFrame)
                {
                    Plugin.Log.LogInfo("AP: F6 — wiping save and resetting state.");
                    TrackingManager.WipeSave();
                    _trackingInitialized = true; // WipeSave already gives us a fresh in-memory state
                    ItemApplicator.ResetAppliedLevels();
                    StartCoroutine(ReconnectCoroutine(
                        Plugin.ConfigHost.Value, Plugin.ConfigPort.Value,
                        Plugin.ConfigSlotName.Value, Plugin.ConfigPassword.Value).WrapToIl2Cpp());
                }

                // F7: instantly grant the same amount a real "Small Cash Injection" item would (testing shortcut).
                if (kb.f7Key.wasPressedThisFrame)
                {
                    try
                    {
                        ItemApplicator.DebugGrantSmallCashInjection();
                    }
                    catch (Exception ex)
                    {
                        Plugin.Log.LogError($"AP: F7 instant money failed: {ex.Message}");
                    }
                }

            }

            // When AP progressive items are applied the shop panel level counters
            // won't update until the shop refreshes. UpdatePanelsVisibility() is safe
            // to call externally, unlike PopulateShop which rebuilds shopPanelDict
            // and crashes with a duplicate-key exception if called twice.
            if (ItemApplicator.ShopNeedsVisibilityRefresh)
            {
                ItemApplicator.ShopNeedsVisibilityRefresh = false;
                var shop = UnityEngine.Object.FindObjectOfType<UpgradeShop>(true);
                shop?.UpdatePanelsVisibility();
            }

            _shopApplyTimer -= Time.deltaTime;
            if (_shopApplyTimer <= 0f)
            {
                _shopApplyTimer = 3f;
                ItemApplicator.ApplyAll();
                ItemApplicator.LockUnapplied();
            }
        }

        private IEnumerator ReconnectCoroutine(string host, int port, string slotName, string password)
        {
            // Explicit disconnect bypasses TryConnect's "already connected" early return
            // guard, ensuring a fresh login and a full server replay of received items.
            ArchipelagoManager.TryDisconnect();
            yield return new WaitForSeconds(0.5f);
            string result = ArchipelagoManager.TryConnect(host, port, slotName, password);
            if (result == "")
            {
                Plugin.Log.LogInfo("AP: Reconnect successful — waiting for server replay...");
                yield return new WaitForSeconds(1.5f);
                ItemApplicator.ApplyAll();
                ItemApplicator.LockUnapplied();
                TrackingManager.CatchUpCoinAutoCompletes();
                Plugin.Log.LogInfo("AP: Post-reconnect ApplyAll complete.");
            }
            else
            {
                Plugin.Log.LogError($"AP: Reconnect failed: {result}");
            }
        }
    }
}