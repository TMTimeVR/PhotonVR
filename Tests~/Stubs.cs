// Stand-ins for the Unity, PUN2, Photon Voice and TextMeshPro surface that the
// PhotonVR runtime scripts touch. Enough for Roslyn to type-check them.
using System;
using System.Collections;
using System.Collections.Generic;

namespace UnityEngine {
 public class Object { public string name; public static void Destroy(Object o) {} public static void DontDestroyOnLoad(Object o) {} public static T[] FindObjectsOfType<T>() => new T[0]; public static T FindObjectOfType<T>() => default; }
 public class Component : Object { public GameObject gameObject; public Transform transform; public T GetComponent<T>() => default; public T GetComponentInChildren<T>() => default; public T[] GetComponentsInChildren<T>() => new T[0]; }
 public class Behaviour : Component { public bool enabled { get; set; } public bool isActiveAndEnabled => true; }
 public class MonoBehaviour : Behaviour {
   public Coroutine StartCoroutine(IEnumerator r) => null;
   public void StopAllCoroutines() {}
   public void Invoke(string m, float t) {}
   public void InvokeRepeating(string m, float t, float r) {} }
 public class Coroutine {}
 public class Transform : Component {
   public Vector3 position { get; set; }
   public Quaternion rotation { get; set; }
   public Vector3 localPosition { get; set; }
   public Quaternion localRotation { get; set; }
   public Vector3 localScale { get; set; }
   public Vector3 eulerAngles { get; set; }
   public Vector3 localEulerAngles { get; set; }
   public Transform parent { get; set; }
   public int childCount => 0;
   public Transform GetChild(int i) => null;
   public Transform Find(string n) => null;
   public void SetParent(Transform p) {}
   public void SetParent(Transform p, bool w) {}
   public IEnumerator GetEnumerator() => null; }
 public class GameObject : Object {
   public GameObject() {} public GameObject(string n) {}
   public Transform transform => null;
   public bool activeSelf => true; public bool activeInHierarchy => true;
   public void SetActive(bool b) {}
   public T GetComponent<T>() => default;
   public T AddComponent<T>() => default;
   public static GameObject Find(string n) => null; }
 public struct Vector3 {
   public float x, y, z;
   public Vector3(float a, float b, float c) { x=a; y=b; z=c; }
   public static Vector3 zero => default; public static Vector3 one => default;
   public static Vector3 up => default; public static Vector3 forward => default;
   public static Vector3 Lerp(Vector3 a, Vector3 b, float t) => default;
   public static float Distance(Vector3 a, Vector3 b) => 0f;
   public float sqrMagnitude => 0f;
   public float magnitude => 0f;
   public Vector3 normalized => default;
   public static Vector3 operator +(Vector3 a, Vector3 b) => default;
   public static Vector3 operator -(Vector3 a, Vector3 b) => default;
   public static Vector3 operator *(Vector3 a, float b) => default; }
 public struct Quaternion {
   public float x, y, z, w;
   public Quaternion(float a, float b, float c, float d) { x=a; y=b; z=c; w=d; }
   public static Quaternion LookRotation(Vector3 f) => default;
   public static Quaternion LookRotation(Vector3 f, Vector3 u) => default;
   public static Quaternion identity => default;
   public Vector3 eulerAngles => default;
   public static Quaternion Euler(float a, float b, float c) => default;
   public static Quaternion Lerp(Quaternion a, Quaternion b, float t) => default;
   public static Quaternion Slerp(Quaternion a, Quaternion b, float t) => default; }
 public struct Color {
   public float r, g, b, a;
   public Color(float x, float y, float z) { r=x; g=y; b=z; a=1f; }
   public Color(float x, float y, float z, float w) { r=x; g=y; b=z; a=w; }
   public static Color white => default; public static Color black => default;
   public static Color red => default; public static Color green => default; public static Color blue => default; }
 public class Material : Object { public Color color { get; set; } public void SetColor(string n, Color c) {} }
 public class Renderer : Component { public Material material { get; set; } public Material[] materials { get; set; } }
 public class MeshRenderer : Renderer {}
 public class SkinnedMeshRenderer : Renderer {}
 public class Application { public static string identifier => ""; public static string version => ""; public static void Quit() {} public static bool isEditor => false; }
 public class Debug {
   public static bool isDebugBuild => true;
   public static void Log(object o) {} public static void LogError(object o) {}
   public static void LogWarning(object o) {} public static void LogException(Exception e) {} }
 public class Time { public static float time => 0f; public static float deltaTime => 0f; public static float fixedDeltaTime => 0f; }
 public class SystemInfo { public static string deviceUniqueIdentifier => ""; public static string deviceName => ""; }
 public class WaitForSeconds { public WaitForSeconds(float s) {} }
 public class WaitForSecondsRealtime { public WaitForSecondsRealtime(float s) {} }
 public class Random { public static int Range(int a, int b) => 0; public static float Range(float a, float b) => 0f; public static float value => 0f; }
 public class Mathf {
   public static float Clamp(float v, float a, float b) => v < a ? a : (v > b ? b : v);
   public static int Clamp(int v, int a, int b) => v < a ? a : (v > b ? b : v);
   public static float Clamp01(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);
   public static float Lerp(float a, float b, float t) => a + (b - a) * t;
   public static float Abs(float f) => f < 0 ? -f : f; }
 public class PlayerPrefs {
   public static readonly System.Collections.Generic.Dictionary<string,string> Store = new System.Collections.Generic.Dictionary<string,string>();
   public static void SetString(string k, string v) { Store[k] = v; }
   public static string GetString(string k) => Store.TryGetValue(k, out var v) ? v : "";
   public static string GetString(string k, string d) => Store.TryGetValue(k, out var v) ? v : d;
   public static void SetInt(string k, int v) {}
   public static int GetInt(string k) => 0;
   public static int GetInt(string k, int d) => d;
   public static void SetFloat(string k, float v) {}
   public static float GetFloat(string k) => 0f;
   public static float GetFloat(string k, float d) => d;
   public static bool HasKey(string k) => Store.ContainsKey(k);
   public static void DeleteKey(string k) { Store.Remove(k); }
   public static void Save() {} }
 public class JsonUtility {
   private static readonly System.Text.Json.JsonSerializerOptions Opts =
     new System.Text.Json.JsonSerializerOptions { IncludeFields = true };
   public static string ToJson(object o) => System.Text.Json.JsonSerializer.Serialize(o, Opts);
   public static T FromJson<T>(string s) => System.Text.Json.JsonSerializer.Deserialize<T>(s, Opts);
   public static object FromJson(string s, Type t) => System.Text.Json.JsonSerializer.Deserialize(s, t, Opts);
   public static void FromJsonOverwrite(string s, object o) {} }
 public class SerializeField : Attribute {}
 public class HideInInspector : Attribute {}
 public class SerializableAttribute2 : Attribute {}
 public class HeaderAttribute : Attribute { public HeaderAttribute(string s) {} }
 public class TooltipAttribute : Attribute { public TooltipAttribute(string s) {} }
 public class SpaceAttribute : Attribute { public SpaceAttribute() {} public SpaceAttribute(float f) {} }
 public class RangeAttribute : Attribute { public RangeAttribute(float a, float b) {} }
 public class TextAreaAttribute : Attribute { public TextAreaAttribute() {} public TextAreaAttribute(int a, int b) {} }
 public class RuntimeInitializeOnLoadMethodAttribute : Attribute { public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType t) {} }
 public enum RuntimeInitializeLoadType { SubsystemRegistration, AfterSceneLoad }
 public class AndroidJavaClass : IDisposable { public AndroidJavaClass(string s) {} public T GetStatic<T>(string n) => default; public void Dispose() {} }
 public class AndroidJavaObject { public T Call<T>(string n, params object[] a) => default; public T Get<T>(string n) => default; public T GetStatic<T>(string n) => default; }
}
namespace UnityEngine.SceneManagement {
 public struct Scene { public string name => ""; public int buildIndex => 0; }
 public class SceneManager {
   public static Scene GetActiveScene() => default;
   public static void LoadScene(string n) {}
   public static void LoadScene(int i) {} }
}
namespace UnityEngine.Networking {
 public class DownloadHandler { public byte[] data => null; public string text => ""; }
 public class UnityWebRequestAsyncOperation {}
 public class UnityWebRequest : IDisposable {
   public enum Result { InProgress, Success, ConnectionError, ProtocolError, DataProcessingError }
   public Result result => Result.Success; public string error => ""; public int timeout { get; set; }
   public DownloadHandler downloadHandler => null;
   public static UnityWebRequest Get(string u) => null;
   public UnityWebRequestAsyncOperation SendWebRequest() => null;
   public void Dispose() {} }
}
namespace TMPro {
 public class TMP_Text : UnityEngine.Component { public string text { get; set; } public UnityEngine.Color color { get; set; } public bool richText { get; set; } public float fontSize { get; set; } }
 public class TextMeshPro : TMP_Text {}
 public class TextMeshProUGUI : TMP_Text {}
}
namespace ExitGames.Client.Photon {
 public class Hashtable : Dictionary<object, object> {}
}
namespace Photon.Realtime {
 public class AppSettings { public string AppIdRealtime, AppIdVoice, AppIdChat, FixedRegion; public bool UseNameServer = true; }
 public class AuthenticationValues {
   public CustomAuthenticationType AuthType { get; set; }
   public string UserId { get; set; }
   public void AddAuthParameter(string k, string v) {} }
 public enum CustomAuthenticationType { Custom, None }
 public class Player {
   public string NickName { get; set; }
   public string UserId { get; set; }
   public int ActorNumber => 0;
   public bool IsLocal => true;
   public bool IsMasterClient => true;
   public ExitGames.Client.Photon.Hashtable CustomProperties { get; set; }
   public void SetCustomProperties(ExitGames.Client.Photon.Hashtable h) {} }
 public class Room {
   public string Name { get; set; }
   public int PlayerCount => 0;
   public byte MaxPlayers { get; set; }
   public bool IsOpen { get; set; }
   public bool IsVisible { get; set; }
   public Dictionary<int, Player> Players => null;
   public ExitGames.Client.Photon.Hashtable CustomProperties { get; set; }
   public void SetCustomProperties(ExitGames.Client.Photon.Hashtable h) {} }
 public class RoomOptions {
   public byte MaxPlayers { get; set; }
   public bool IsOpen { get; set; }
   public bool IsVisible { get; set; }
   public bool CleanupCacheOnLeave { get; set; }
   public ExitGames.Client.Photon.Hashtable CustomRoomProperties { get; set; }
   public string[] CustomRoomPropertiesForLobby { get; set; } }
 public class TypedLobby { public static TypedLobby Default => null; }
 public class RoomInfo { public string Name => ""; public int PlayerCount => 0; }
 public enum DisconnectCause { None, ExceptionOnConnect, Exception, DisconnectByServerLogic, DisconnectByClientLogic, MaxCcuReached, InvalidRegion, CustomAuthenticationFailed, AuthenticationTicketExpired, OperationNotAllowedInCurrentState }
 public enum ClientState { PeerCreated, ConnectedToMasterServer, Joined, Disconnected }
 public enum MatchmakingMode { FillRoom, SerialMatching, RandomMatching }
 public interface IConnectionCallbacks {}
 public interface IMatchmakingCallbacks {}
 public interface IInRoomCallbacks {}
}
namespace Photon.Pun {
 public class PhotonView : UnityEngine.Component {
   public bool IsMine => true;
   public int ViewID { get; set; }
   public Photon.Realtime.Player Owner => null;
   public Photon.Realtime.Player Controller => null;
   public void RPC(string name, RpcTarget target, params object[] args) {}
   public void RPC(string name, Photon.Realtime.Player target, params object[] args) {} }
 public enum RpcTarget { All, Others, MasterClient, AllBuffered, OthersBuffered, AllViaServer, AllBufferedViaServer }
 public class PunRPCAttribute : Attribute {}
 public struct PhotonMessageInfo {
   public Photon.Realtime.Player Sender => null;
   public double SentServerTime => 0d;
   public PhotonView photonView => null; }
 public class MonoBehaviourPun : UnityEngine.MonoBehaviour { public PhotonView photonView => null; }
 public class MonoBehaviourPunCallbacks : MonoBehaviourPun {
   public virtual void OnConnectedToMaster() {}
   public virtual void OnConnected() {}
   public virtual void OnDisconnected(Photon.Realtime.DisconnectCause cause) {}
   public virtual void OnJoinedRoom() {}
   public virtual void OnLeftRoom() {}
   public virtual void OnJoinRandomFailed(short returnCode, string message) {}
   public virtual void OnJoinRoomFailed(short returnCode, string message) {}
   public virtual void OnCreateRoomFailed(short returnCode, string message) {}
   public virtual void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer) {}
   public virtual void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer) {}
   public virtual void OnPlayerPropertiesUpdate(Photon.Realtime.Player target, ExitGames.Client.Photon.Hashtable changedProps) {}
   public virtual void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged) {}
   public virtual void OnMasterClientSwitched(Photon.Realtime.Player newMasterClient) {}
   public virtual void OnEnable() {}
   public virtual void OnDisable() {} }
 public class ServerSettings { public Photon.Realtime.AppSettings AppSettings; public string DevRegion; public bool StartInOfflineMode; }
 public class PhotonNetwork {
   // Stateful for the tests: room membership is settable and every call that
   // matters is recorded, so the leave-then-join handoff can be asserted.
   public static readonly System.Collections.Generic.List<string> Calls = new System.Collections.Generic.List<string>();
   public static bool InRoomFlag = false;
   public static void Reset() { Calls.Clear(); InRoomFlag = false; CurrentRoomRef = null; }

   public static string NickName { get; set; }
   public static bool IsConnected => true;
   public static bool InRoom => InRoomFlag;
   public static bool IsMasterClient => true;
   public static bool AutomaticallySyncScene { get; set; }
   public static Photon.Realtime.AuthenticationValues AuthValues { get; set; }
   public static ServerSettings PhotonServerSettings { get; set; } = new ServerSettings { AppSettings = new Photon.Realtime.AppSettings() };
   public static Photon.Realtime.Player LocalPlayer { get; set; } = new Photon.Realtime.Player();
   public static Photon.Realtime.Room CurrentRoomRef;
   public static Photon.Realtime.Room CurrentRoom => CurrentRoomRef;
   public static Photon.Realtime.ClientState NetworkClientState => default;
   public static int GetPing() => 0;
   public static void Disconnect() {}
   public static bool ConnectUsingSettings() => true;
   public static bool ConnectUsingSettings(Photon.Realtime.AppSettings a) => true;
   public static bool CreateRoom(string name) => CreateRoom(name, null, null, null);
   public static bool CreateRoom(string name, Photon.Realtime.RoomOptions o) => CreateRoom(name, o, null, null);
   public static bool CreateRoom(string name, Photon.Realtime.RoomOptions o, Photon.Realtime.TypedLobby l) => CreateRoom(name, o, l, null);
   public static bool CreateRoom(string name, Photon.Realtime.RoomOptions o, Photon.Realtime.TypedLobby l, string[] e) { Calls.Add("CreateRoom:" + name); return true; }
   public static bool JoinRoom(string name) { Calls.Add("JoinRoom:" + name); return true; }
   public static bool JoinOrCreateRoom(string name, Photon.Realtime.RoomOptions o, Photon.Realtime.TypedLobby l) => JoinOrCreateRoom(name, o, l, null);
   public static bool JoinOrCreateRoom(string name, Photon.Realtime.RoomOptions o, Photon.Realtime.TypedLobby l, string[] e) { Calls.Add("JoinOrCreateRoom:" + name); return true; }
   public static bool JoinRandomRoom() => true;
   public static bool JoinRandomRoom(ExitGames.Client.Photon.Hashtable f, byte m) => true;
   public static bool JoinRandomRoom(ExitGames.Client.Photon.Hashtable f, byte m, Photon.Realtime.MatchmakingMode mm, Photon.Realtime.TypedLobby l, string sql) => true;
   public static bool JoinRandomRoom(ExitGames.Client.Photon.Hashtable f, byte m, Photon.Realtime.MatchmakingMode mm, Photon.Realtime.TypedLobby l, string sql, string[] e) { Calls.Add("JoinRandomRoom:" + (f != null && f.ContainsKey("queue") ? f["queue"] : "?")); return true; }
   public static bool LeaveRoom() { Calls.Add("LeaveRoom"); InRoomFlag = false; return true; }
   public static UnityEngine.GameObject Instantiate(string prefabName, UnityEngine.Vector3 pos, UnityEngine.Quaternion rot) => null;
   public static UnityEngine.GameObject Instantiate(string prefabName, UnityEngine.Vector3 pos, UnityEngine.Quaternion rot, byte group) => null;
   public static void Destroy(UnityEngine.GameObject go) {}
   public static void Destroy(UnityEngine.Component c) {} }
}
namespace Photon.Voice {
 public class Recorder : UnityEngine.MonoBehaviour { public bool TransmitEnabled { get; set; } }
 public class Speaker : UnityEngine.MonoBehaviour {}
}
namespace Photon.Voice.Unity {
 public class Recorder : UnityEngine.MonoBehaviour { public bool TransmitEnabled { get; set; } }
 public class VoiceConnection : UnityEngine.MonoBehaviour {}
}
namespace Photon.Voice.PUN {
 public class PunVoiceClient : UnityEngine.MonoBehaviour { public static PunVoiceClient Instance => null; }
}

namespace UnityEngine.Events {
 public class UnityEventBase { }
 public class UnityEvent : UnityEventBase { public void Invoke() {} public void AddListener(System.Action a) {} }
 public class UnityEvent<T0> : UnityEventBase { public void Invoke(T0 a) {} public void AddListener(System.Action<T0> a) {} }
}

namespace UnityEngine {
 // Small shim so tests can reset the Photon stub without reaching into Photon.Pun.
 public static class PhotonNetworkTestAccess {
   public static void Reset() { Photon.Pun.PhotonNetwork.Reset(); }
 }
}
