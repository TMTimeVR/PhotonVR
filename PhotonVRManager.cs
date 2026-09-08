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

            Manager.pendingQueue = SceneIndex.ToString();
            Manager.pendingLimit = MaxPlayers;

            SceneManager.LoadScene(SceneIndex);

            if (PhotonNetwork.InRoom)
                PhotonNetwork.LeaveRoom();
            else
                JoinRandomRoom(SceneIndex.ToString(), MaxPlayers);
        }

        public static void SwitchScenes(int SceneIndex)
        {
            if (!ManagerReady())
                return;

            SwitchScenes(SceneIndex, Manager.DefaultRoomLimit);
        }

        private string pendingQueue;
        private int pendingLimit;

        public override void OnLeftRoom()
        {
            State = ConnectionState.Connected;

            if (!string.IsNullOrEmpty(pendingQueue))
            {
                string queue = pendingQueue;
                int limit = pendingLimit;
                pendingQueue = null;
                JoinRandomRoom(queue, limit);
            }
        }

        public static void JoinRandomRoom(string Queue, int MaxPlayers) => _JoinRandomRoom(Queue, MaxPlayers);

        public static void JoinRandomRoom(string Queue) => _JoinRandomRoom(Queue, Manager != null ? Manager.DefaultRoomLimit : 10);

        private static void _JoinRandomRoom(string Queue, int MaxPlayers)
        {
            if (!ManagerReady())
                return;

            Manager.State = ConnectionState.JoiningRoom;
            ExitGames.Client.Photon.Hashtable hastable = new ExitGames.Client.Photon.Hashtable();
            hastable.Add("queue", Queue);
            hastable.Add("version", Application.version);

            RoomOptions roomOptions = BuildRoomOptions(MaxPlayers, true, hastable);
            Manager.options = roomOptions;

            PhotonNetwork.JoinRandomRoom(hastable, roomOptions.MaxPlayers, MatchmakingMode.RandomMatching, null, null, null);
            Debug.Log($"Joining random with type {hastable["queue"]}");
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

        public static void JoinPrivateRoom(string RoomId, int MaxPlayers) => _JoinPrivateRoom(RoomId, MaxPlayers);

        public static void JoinPrivateRoom(string RoomId) => _JoinPrivateRoom(RoomId, Manager != null ? Manager.DefaultRoomLimit : 10);

        public static void _JoinPrivateRoom(string RoomId, int MaxPlayers)
        {
            if (!ManagerReady())
                return;

            if (string.IsNullOrEmpty(RoomId))
            {
                Debug.LogError("A private room needs a room code");
                return;
            }

            PhotonNetwork.JoinOrCreateRoom(PrivateRoomPrefix + RoomId,
                BuildRoomOptions(MaxPlayers, false, null), null, null);
            Status($"Joining a private room: {RoomId}");
            Manager.State = ConnectionState.JoiningRoom;
        }

        public override void OnJoinedRoom()
        {
            Debug.Log("Joined a room");
            State = ConnectionState.InRoom;
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
            Debug.LogWarning($"Room creation failed ({returnCode}: {message}), retrying with a new code");
            HandleJoinError();
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
