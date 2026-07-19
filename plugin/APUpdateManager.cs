using BepInEx.Unity.IL2CPP.Utils.Collections;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
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

        // Shift + F7 hold-to-repeat: -1 while not held. Set to Time.time on initial
        // press so the first repeat can be timed relative to it.
        private float _f7HoldStartTime = -1f;
        // Time.time of the next allowed repeat shot while held past the threshold.
        private float _f7NextRepeatTime = 0f;
        private const float F7_REPEAT_HOLD_THRESHOLD = 1f; // seconds held before repeating starts
        private const float F7_REPEAT_INTERVAL = 0.1f;     // 10 shots/sec
        private const double F7_REPEAT_START_MULTIPLIER = 100.0; // multiplier once repeating starts
        private const float F7_REPEAT_ESCALATION_INTERVAL = 3f;  // seconds between escalations
        private const double F7_REPEAT_ESCALATION_FACTOR = 20.0; // multiplier grows by this factor each escalation

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
                // The actual per-playthrough save is loaded later, by
                // TrackingManager.ReconcileSaveForSeed inside ArchipelagoManager.TryConnect,
                // once the AP seed name is known. This just marks tracking as ready.
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
                    InstantCashOutCurrentTicket("F5");
                }

                // F6: wipe the current playthrough's save data and reset all in-memory
                // tracking (testing shortcut). Only touches the active per-playthrough
                // save file, other playthroughs' saved progress is untouched. After
                // wiping, force a full disconnect and reconnect so the server replays
                // all previously received items into the freshly cleared ReceivedItemCounts.
                // Without the reconnect, the server won't re-send items.
                if (kb.f6Key.wasPressedThisFrame)
                {
                    Plugin.Log.LogInfo("AP: F6 - wiping save and resetting state.");
                    TrackingManager.WipeSave();
                    _trackingInitialized = true; // WipeSave already gives us a fresh in-memory state
                    ItemApplicator.ResetAppliedLevels();
                    StartCoroutine(ReconnectCoroutine(
                        Plugin.ConfigHost.Value, Plugin.ConfigPort.Value,
                        Plugin.ConfigSlotName.Value, Plugin.ConfigPassword.Value).WrapToIl2Cpp());
                }

                // F7: instantly grant the same amount a real "Small Cash Injection" item
                // would (testing shortcut). Shift + F7 grants a "Large Cash Injection" instead.
                // Holding Shift + F7 for over a second starts auto-firing 100x a Large Cash
                // Injection, 10 times/sec, escalating by a further 20x every 3 seconds
                // held, until either key is released.
                if (kb.f7Key.wasPressedThisFrame)
                {
                    bool shift = kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed;
                    _f7HoldStartTime = Time.time;
                    _f7NextRepeatTime = Time.time + F7_REPEAT_HOLD_THRESHOLD;
                    try
                    {
                        if (shift)
                            ItemApplicator.DebugGrantLargeCashInjection();
                        else
                            ItemApplicator.DebugGrantSmallCashInjection();
                    }
                    catch (Exception ex)
                    {
                        Plugin.Log.LogError($"AP: F7 instant money failed: {ex.Message}");
                    }
                }
                else if (kb.f7Key.isPressed)
                {
                    bool shift = kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed;
                    if (shift && _f7HoldStartTime >= 0f && Time.time >= _f7NextRepeatTime)
                    {
                        _f7NextRepeatTime = Time.time + F7_REPEAT_INTERVAL;
                        double heldSeconds = Time.time - _f7HoldStartTime;
                        int escalations = (int)(heldSeconds / F7_REPEAT_ESCALATION_INTERVAL);
                        double multiplier = F7_REPEAT_START_MULTIPLIER * Math.Pow(F7_REPEAT_ESCALATION_FACTOR, escalations);
                        try
                        {
                            ItemApplicator.DebugGrantMegaCashInjection(multiplier);
                        }
                        catch (Exception ex)
                        {
                            Plugin.Log.LogError($"AP: F7 repeat instant money failed: {ex.Message}");
                        }
                    }
                }
                else
                {
                    _f7HoldStartTime = -1f;
                }

            }

            // Right-clicking a ticket on the table simulates a left-click that normally
            // picks the ticket up and opens it, then after a short delay do the same
            // instant cash-out F5 does. Lets players open and cash out a ticket with
            // a single right-click instead of left-click then F5.
            var mouse = Mouse.current;
            if (mouse != null && mouse.rightButton.wasPressedThisFrame)
            {
                StartCoroutine(RightClickOpenAndCashOut().WrapToIl2Cpp());
            }

            _shopApplyTimer -= Time.deltaTime;
            if (_shopApplyTimer <= 0f)
            {
                _shopApplyTimer = 3f;
                ItemApplicator.ApplyAll();
                ItemApplicator.LockUnapplied();
            }
        }

        private static void InstantCashOutCurrentTicket(string source)
        {
            try
            {
                var scratching = UnityEngine.Object.FindObjectOfType<PlayerScratching>(true);
                var ticket = scratching?.CurrentTicket;
                if (ticket == null)
                {
                    Plugin.Log.LogWarning($"AP: {source} - no active ticket.");
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
                    Plugin.Log.LogInfo($"AP: {source} - instant cash-out triggered.");
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"AP: {source} instant scratch failed: {ex.Message}");
            }
        }

        private IEnumerator RightClickOpenAndCashOut()
        {
            // Spacing mouse events a frame apart makes it read as a
            // press then release, same as real left-clicking.
            mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
            yield return null;
            mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);

            yield return new WaitForSeconds(0.1f);
            InstantCashOutCurrentTicket("Right-click");
        }

        [DllImport("user32.dll")]
        private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);

        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP = 0x0004;

        private IEnumerator ReconnectCoroutine(string host, int port, string slotName, string password)
        {
            // Explicit disconnect bypasses TryConnect's "already connected" early return
            // guard, ensuring a fresh login and a full server replay of received items.
            ArchipelagoManager.TryDisconnect();
            yield return new WaitForSeconds(0.5f);
            string result = ArchipelagoManager.TryConnect(host, port, slotName, password);
            if (result == "")
            {
                Plugin.Log.LogInfo("AP: Reconnect successful - waiting for server replay...");
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