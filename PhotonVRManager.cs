using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

using UnityEngine;
using UnityEngine.SceneManagement;

using Photon.VR.Player;
using Photon.VR.Saving;
using Photon.Pun;
using Photon.Realtime;
using Photon;

using TMPro;

using ExitGames.Client.Photon;

namespace Photon.VR
{
    public class PhotonVRManager : MonoBehaviourPunCallbacks
    {
        public static PhotonVRManager Manager { get; private set; }

        [Header("Photon")]
        public string AppId;
        public string VoiceAppId;
        [Tooltip("Please read https://doc.photonengine.com/en-us/pun/current/connection-and-authentication/regions for more information")]
        public string Region = "eu";
        [Tooltip("The region that people connect to if the apk is a development build (check your build settings).")]
        public string DevRegion = "";

        [Header("Player")]
        public Transform Head;
        public Transform LeftHand;
        public Transform RightHand;
        public Color Colour;

        public Dictionary<string, string> Cosmetics { get; private set; } = new Dictionary<string, string>();

        [Header("Networking")]
        public string DefaultQueue = "Default";
        public int DefaultRoomLimit = 10;

        [Header("Other")]

        [Tooltip("If the user shall connect when this object has awoken")]
        public bool ConnectOnAwake = true;
        [Tooltip("If the user shall join a room when they connect")]
        public bool JoinRoomOnConnect = true;
        [Tooltip("Simulates an online connection.\nPUN can be used as usual.")]
        public bool StartInOfflineMode = false;

        [Tooltip("Optional. A TextMeshPro that displays what PhotonVRManager is doing.")]
        public TextMeshPro LogText;

        [NonSerialized]
        public PhotonVRPlayer LocalPlayer;

        private RoomOptions options;

        private ConnectionState State = ConnectionState.Disconnected;

        private const string PublicRoomPrefix = "pub-";
        private const string PrivateRoomPrefix = "priv-";

        private const string RoomCodeAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        private const int RoomCodeLength = 6;

        private const int MaxUsernameLength = 16;
        private static readonly Regex RichTextTag = new Regex("<[^>]*>", RegexOptions.Compiled);

        private void Start()
        {

            if (Manager != null && Manager != this)
            {
                Debug.LogError("There can't be multiple PhotonVRManagers in a scene");
                Destroy(gameObject);
                return;
            }

            Manager = this;
            State = ConnectionState.Setting_Up_Settings;
            Status("Setting Up Settings...");

            PhotonNetwork.PhotonServerSettings.DevRegion = DevRegion;
            PhotonNetwork.PhotonServerSettings.StartInOfflineMode = StartInOfflineMode;

            DontDestroyOnLoad(gameObject);

            LoadSavedValues();

            if (ConnectOnAwake)
                Connect();
        }

        private void LoadSavedValues()
        {

            try
            {
                if (!string.IsNullOrEmpty(PlayerPrefs.GetString("Colour")))
                    Colour = ClampColour(JsonUtility.FromJson<Color>(PlayerPrefs.GetString("Colour")));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Saved colour could not be read, using the default: {e.Message}");
            }

            try
            {
                if (!string.IsNullOrEmpty(PlayerPrefs.GetString("Cosmetics")))
                    Cosmetics = PhotonVRValueSaver.GetDictionary("Cosmetics") ?? new Dictionary<string, string>();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Saved cosmetics could not be read, using none: {e.Message}");
                Cosmetics = new Dictionary<string, string>();
            }
        }

        private static void Status(string message)
        {
            Debug.Log(message);
            if (Manager != null && Manager.LogText != null)
                Manager.LogText.text = message;
        }

        private static bool ManagerReady()
        {
            if (Manager != null)
                return true;
            Debug.LogError("There is no PhotonVRManager in the scene yet");
            return false;
        }

        private static Color ClampColour(Color colour)
        {

            return new Color(
                Mathf.Clamp01(colour.r),
                Mathf.Clamp01(colour.g),
                Mathf.Clamp01(colour.b),
                Mathf.Clamp01(colour.a));
        }

#if UNITY_EDITOR
        public void CheckDefaultValues()
        {
            bool b = CheckForRig(this);
            if (b)
            {
                if (string.IsNullOrEmpty(AppId))
                    AppId = PhotonNetwork.PhotonServerSettings.AppSettings.AppIdRealtime;

                if (string.IsNullOrEmpty(VoiceAppId))
                    VoiceAppId = PhotonNetwork.PhotonServerSettings.AppSettings.AppIdVoice;

                Debug.Log("Attempted to set default values");
            }
        }

        private bool CheckForRig(PhotonVRManager manager)
        {
            GameObject[] objects = FindObjectsOfType<GameObject>();

            bool b = false;

            if (manager.Head == null)
            {
                b = true;
                foreach (GameObject obj in objects)
                {
                    if (obj.name.Contains("Camera") || obj.name.Contains("Head"))
                    {
                        manager.Head = obj.transform;
                        break;
                    }
                }
            }

            if (manager.LeftHand == null)
            {
                b = true;
                foreach (GameObject obj in objects)
                {
                    if (obj.name.Contains("Left") && (obj.name.Contains("Hand") || obj.name.Contains("Controller")))
                    {
                        manager.LeftHand = obj.transform;
                        break;
                    }
                }
            }

            if (manager.RightHand == null)
            {
                b = true;
                foreach (GameObject obj in objects)
                {
                    if (obj.name.Contains("Right") && (obj.name.Contains("Hand") || obj.name.Contains("Controller")))
                    {
                        manager.RightHand = obj.transform;
                        break;
                    }
                }
            }

            return b;
        }
#endif

        public static bool Connect()
        {
            if (!ManagerReady())
                return false;

            if (string.IsNullOrEmpty(Manager.AppId) || string.IsNullOrEmpty(Manager.VoiceAppId))
            {
                Debug.LogError("Please input an app id");
                Manager.State = ConnectionState.Error;
                Status("Please input an app id.");
                return false;
            }

            if (PhotonNetwork.AuthValues != null)
                Debug.LogWarning("Connect() is discarding existing AuthValues. Use ConnectAuthenticated to keep them.");
            PhotonNetwork.AuthValues = null;

            Manager.State = ConnectionState.Connecting;
            PhotonNetwork.PhotonServerSettings.AppSettings.AppIdRealtime = Manager.AppId;
            PhotonNetwork.PhotonServerSettings.AppSettings.AppIdVoice = Manager.VoiceAppId;
            PhotonNetwork.PhotonServerSettings.AppSettings.FixedRegion = Manager.Region;
            PhotonNetwork.ConnectUsingSettings();
            Status("Connecting...");
            return true;
        }

        public static bool ConnectAuthenticated(string username, string token)
        {
            if (!ManagerReady())
                return false;

            if (string.IsNullOrEmpty(Manager.AppId) || string.IsNullOrEmpty(Manager.VoiceAppId))
            {
                Debug.LogError("Please input an app id");
                Manager.State = ConnectionState.Error;
                Status("Please input an app id.");
                return false;
            }

            AuthenticationValues authentication = new AuthenticationValues { AuthType = CustomAuthenticationType.Custom };
            authentication.AddAuthParameter("username", username);
            authentication.AddAuthParameter("token", token);
            PhotonNetwork.AuthValues = authentication;

            Manager.State = ConnectionState.Connecting;
            PhotonNetwork.PhotonServerSettings.AppSettings.AppIdRealtime = Manager.AppId;
            PhotonNetwork.PhotonServerSettings.AppSettings.AppIdVoice = Manager.VoiceAppId;
            PhotonNetwork.PhotonServerSettings.AppSettings.FixedRegion = Manager.Region;
            PhotonNetwork.ConnectUsingSettings();
            Status("Connecting...");
            return true;
        }

        public void Disconnect()
        {
            PhotonNetwork.Disconnect();
        }

        public static void ChangeServers(string Id, string VoiceId)
        {
            if (!ManagerReady())
                return;

            PhotonNetwork.Disconnect();
            Manager.AppId = Id;
            Manager.VoiceAppId = VoiceId;
            Connect();
        }

        public static void ChangeServersAuthenticated(string Id, string VoiceId, string username, string token)
        {
            if (!ManagerReady())
                return;

            PhotonNetwork.Disconnect();
            Manager.AppId = Id;
            Manager.VoiceAppId = VoiceId;
            ConnectAuthenticated(username, token);
        }

        public static void SetUsername(string Name)
        {

            Name = SanitiseDisplayName(Name);

            PhotonNetwork.LocalPlayer.NickName = Name;
            PlayerPrefs.SetString("Username", Name);

            if (PhotonNetwork.InRoom)
                if (Manager != null && Manager.LocalPlayer != null)
                    Manager.LocalPlayer.RefreshPlayerValues();
        }

        private static string SanitiseDisplayName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return "Player";

            name = RichTextTag.Replace(name, string.Empty).Trim();
            if (name.Length > MaxUsernameLength)
                name = name.Substring(0, MaxUsernameLength);

            return string.IsNullOrEmpty(name) ? "Player" : name;
        }

        public static void SetColour(Color PlayerColour)
        {
            if (!ManagerReady())
                return;

            PlayerColour = ClampColour(PlayerColour);
            Manager.Colour = PlayerColour;
            PublishProperty("Colour", JsonUtility.ToJson(PlayerColour));
            PlayerPrefs.SetString("Colour", JsonUtility.ToJson(PlayerColour));

            if (PhotonNetwork.InRoom)
                if (Manager.LocalPlayer != null)
                    Manager.LocalPlayer.RefreshPlayerValues();
        }

        public static void SetCosmetics(Dictionary<string, string> PlayerCosmetics)
        {
            if (!ManagerReady())
                return;

            Manager.Cosmetics = PlayerCosmetics ?? new Dictionary<string, string>();
            PublishProperty("Cosmetics", Manager.Cosmetics);
            PhotonVRValueSaver.SaveDictionary("Cosmetics", Manager.Cosmetics);

            if (PhotonNetwork.InRoom)
                if (Manager.LocalPlayer != null)
                    Manager.LocalPlayer.RefreshPlayerValues();
        }

        public static void SetCosmetic(string Type, string CosmeticId)
        {
            if (!ManagerReady())
                return;

            if (string.IsNullOrEmpty(Type))
                return;

            Manager.Cosmetics[Type] = CosmeticId;
            PublishProperty("Cosmetics", Manager.Cosmetics);
            PhotonVRValueSaver.SaveDictionary("Cosmetics", Manager.Cosmetics);

            if (PhotonNetwork.InRoom)
                if (Manager.LocalPlayer != null)
                    Manager.LocalPlayer.RefreshPlayerValues();
        }

        private static void PublishProperty(string key, object value)
        {
            if (PhotonNetwork.LocalPlayer == null)
                return;

            ExitGames.Client.Photon.Hashtable hash = new ExitGames.Client.Photon.Hashtable();
            hash[key] = value;
            PhotonNetwork.LocalPlayer.SetCustomProperties(hash);
        }

        public override void OnConnectedToMaster()
        {
            State = ConnectionState.Connected;
            Status("Connected!");

            PhotonNetwork.LocalPlayer.NickName = SanitiseDisplayName(PlayerPrefs.GetString("Username"));

            ExitGames.Client.Photon.Hashtable hash = new ExitGames.Client.Photon.Hashtable();
            hash["Colour"] = JsonUtility.ToJson(Colour);
            hash["Cosmetics"] = Cosmetics;
            PhotonNetwork.LocalPlayer.SetCustomProperties(hash);

            if (JoinRoomOnConnect)
                JoinRandomRoom(DefaultQueue, DefaultRoomLimit);
        }

        public static ConnectionState GetConnectionState()
        {
            return Manager != null ? Manager.State : ConnectionState.Disconnected;
        }

        public static void SwitchScenes(int SceneIndex, int MaxPlayers)
        {
            if (!ManagerReady())
                return;

            Manager.State = ConnectionState.Switching_Scenes;
            SceneManager.LoadScene(SceneIndex);
            JoinRandomRoom(SceneIndex.ToString(), MaxPlayers);
        }

        public static void SwitchScenes(int SceneIndex)
        {
            if (!ManagerReady())
                return;

            SwitchScenes(SceneIndex, Manager.DefaultRoomLimit);
        }

        private enum PendingKind { None, Queue, Private }

        private PendingKind pendingKind = PendingKind.None;
        private string pendingTarget;
        private int pendingLimit;

        private bool joiningPrivate;

        public static string CurrentQueue
        {
            get
            {
                if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null)
                    return string.Empty;

                ExitGames.Client.Photon.Hashtable props = PhotonNetwork.CurrentRoom.CustomProperties;
                if (props == null || !props.TryGetValue("queue", out object q))
                    return string.Empty;

                return q as string ?? string.Empty;
            }
        }

        public static bool InPrivateRoom
        {
            get
            {
                if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null)
                    return false;

                string name = PhotonNetwork.CurrentRoom.Name;
                return name != null && name.StartsWith(PrivateRoomPrefix);
            }
        }

        public static void LeaveRoom()
        {
            if (!ManagerReady())
                return;

            Manager.pendingKind = PendingKind.None;
            Manager.pendingTarget = null;

            if (PhotonNetwork.InRoom)
                PhotonNetwork.LeaveRoom();
        }

        private static bool DeferUntilOutOfRoom(PendingKind kind, string target, int maxPlayers)
        {
            if (!PhotonNetwork.InRoom)
                return false;

            Manager.pendingKind = kind;
            Manager.pendingTarget = target;
            Manager.pendingLimit = maxPlayers;
            PhotonNetwork.LeaveRoom();
            return true;
        }

        public override void OnLeftRoom()
        {
            State = ConnectionState.Connected;

            if (pendingKind == PendingKind.None)
                return;

            PendingKind kind = pendingKind;
            string target = pendingTarget;
            int limit = pendingLimit;
            pendingKind = PendingKind.None;
            pendingTarget = null;

            if (kind == PendingKind.Queue)
                JoinRandomRoom(target, limit);
            else
                JoinPrivateRoom(target, limit);
        }

        public static void JoinRandomRoom(string Queue, int MaxPlayers) => _JoinRandomRoom(Queue, MaxPlayers);

        public static void JoinRandomRoom(string Queue) => _JoinRandomRoom(Queue, Manager != null ? Manager.DefaultRoomLimit : 10);

        private static void _JoinRandomRoom(string Queue, int MaxPlayers)
        {
            if (!ManagerReady())
                return;

            if (string.IsNullOrEmpty(Queue))
            {
                Debug.LogError("A queue needs a name");
                return;
            }

            if (DeferUntilOutOfRoom(PendingKind.Queue, Queue, MaxPlayers))
                return;

            Manager.joiningPrivate = false;
            Manager.State = ConnectionState.JoiningRoom;

            ExitGames.Client.Photon.Hashtable hastable = new ExitGames.Client.Photon.Hashtable();
            hastable.Add("queue", Queue);
            hastable.Add("version", Application.version);

            RoomOptions roomOptions = BuildRoomOptions(MaxPlayers, true, hastable);
            Manager.options = roomOptions;

            PhotonNetwork.JoinRandomRoom(hastable, roomOptions.MaxPlayers, MatchmakingMode.RandomMatching, null, null, null);
            Status($"Joining {Queue}");
        }

        private static RoomOptions BuildRoomOptions(int MaxPlayers, bool visible, ExitGames.Client.Photon.Hashtable properties)
        {
            RoomOptions roomOptions = new RoomOptions();

            roomOptions.MaxPlayers = (byte)Mathf.Clamp(MaxPlayers, 1, 255);
            roomOptions.IsVisible = visible;
            roomOptions.IsOpen = true;

            if (properties != null)
            {
                roomOptions.CustomRoomProperties = properties;
                roomOptions.CustomRoomPropertiesForLobby = new string[] { "queue", "version" };
            }

            return roomOptions;
        }

        public const int MaxTypedRoomCodeLength = 16;

        public static string NormaliseRoomCode(string code)
        {
            if (string.IsNullOrEmpty(code))
                return string.Empty;

            System.Text.StringBuilder clean = new System.Text.StringBuilder(code.Length);
            foreach (char c in code)
            {
                if (clean.Length >= MaxTypedRoomCodeLength)
                    break;

                if (char.IsLetterOrDigit(c))
                    clean.Append(char.ToUpperInvariant(c));
                else if (c == '-' || c == '_')
                    clean.Append(c);
            }

            return clean.ToString();
        }

        public static bool IsValidRoomCode(string code)
        {
            return NormaliseRoomCode(code).Length > 0;
        }

        public static void JoinPrivateRoom(string RoomId, int MaxPlayers) => _JoinPrivateRoom(RoomId, MaxPlayers);

        public static void JoinPrivateRoom(string RoomId) => _JoinPrivateRoom(RoomId, Manager != null ? Manager.DefaultRoomLimit : 10);

        public void JoinPrivate(string code) => JoinPrivateRoom(code);

        public void JoinQueue(string queue) => JoinRandomRoom(queue);

        public void Leave() => LeaveRoom();

        public static void _JoinPrivateRoom(string RoomId, int MaxPlayers)
        {
            if (!ManagerReady())
                return;

            string code = NormaliseRoomCode(RoomId);
            if (code.Length == 0)
            {
                Debug.LogError("A private room needs a room code");
                Status("That room code is not usable.");
                return;
            }

            if (DeferUntilOutOfRoom(PendingKind.Private, code, MaxPlayers))
                return;

            Manager.joiningPrivate = true;
            Manager.State = ConnectionState.JoiningRoom;

            PhotonNetwork.JoinOrCreateRoom(PrivateRoomPrefix + code,
                BuildRoomOptions(MaxPlayers, false, null), null, null);
            Status($"Joining a private room: {code}");
        }

        public override void OnJoinedRoom()
        {
            joiningPrivate = false;
            State = ConnectionState.InRoom;

            if (InPrivateRoom)
                Status($"In private room {RoomCode}");
            else
                Status($"In queue {CurrentQueue}");
        }

        public static string RoomCode
        {
            get
            {
                if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null)
                    return string.Empty;

                string name = PhotonNetwork.CurrentRoom.Name ?? string.Empty;
                if (name.StartsWith(PrivateRoomPrefix))
                    return name.Substring(PrivateRoomPrefix.Length);
                if (name.StartsWith(PublicRoomPrefix))
                    return name.Substring(PublicRoomPrefix.Length);
                return name;
            }
        }

        public override void OnDisconnected(DisconnectCause cause)
        {
            base.OnDisconnected(cause);
            State = ConnectionState.Disconnected;
            Status($"Disconnected. Cause {cause}");
        }

        public override void OnJoinRandomFailed(short returnCode, string message) => HandleJoinError();

        public override void OnCreateRoomFailed(short returnCode, string message)
        {

            if (joiningPrivate)
            {
                joiningPrivate = false;
                State = ConnectionState.Error;
                Debug.LogWarning($"Could not join that private room ({returnCode}: {message})");
                Status("Could not join that room.");
                return;
            }

            Debug.LogWarning($"Room creation failed ({returnCode}: {message}), retrying with a new code");
            HandleJoinError();
        }

        public override void OnJoinRoomFailed(short returnCode, string message)
        {
            joiningPrivate = false;
            State = ConnectionState.Error;
            Debug.LogWarning($"Could not join that room ({returnCode}: {message})");
            Status("Could not join that room.");
        }

        private void HandleJoinError()
        {
            Debug.Log("Failed to join room - creating a new one");
            State = ConnectionState.Error;

            if (options == null)
                options = BuildRoomOptions(DefaultRoomLimit, true, null);

            string roomCode = CreateRoomCode();
            Status($"Joining {roomCode}");
            PhotonNetwork.CreateRoom(PublicRoomPrefix + roomCode, options, null, null);
        }

        public string CreateRoomCode()
        {

            State = ConnectionState.Generating_Roomcode;

            byte[] bytes = new byte[RoomCodeLength];
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
                rng.GetBytes(bytes);

            char[] code = new char[RoomCodeLength];
            for (int i = 0; i < RoomCodeLength; i++)
                code[i] = RoomCodeAlphabet[bytes[i] % RoomCodeAlphabet.Length];

            return new string(code);
        }
    }

    public enum ConnectionState
    {
        Disconnected,
        Connecting,
        Connected,
        JoiningRoom,
        InRoom,
        Error,
        Generating_Roomcode,
        Switching_Scenes,
        Setting_Up_Settings
    }
}
