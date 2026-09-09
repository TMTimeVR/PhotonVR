# ![](Visuals/SmallerTextepicepic.png)
A Unity Package containing all the necessary components to do VR networking with [Photon](https://photonengine.com), plus more customisation.

# Notes by TMTime

This fork is basically Photon VR 0.0.6.

There might be some bugs in the code. Please have mercy with me.

**Do not copy just PhotonVRManager.cs any more.** It used to be the only file I had
changed. The September 2026 review changed seven, and the manager now depends on the
others. Take the whole package.

The short version of that review. This fork did not compile, and the manager never
finished starting up, because of a null reference four lines into `Start()`. Both are
fixed. So are two bugs that let any player in a room crash everyone else, and room
codes that collided often enough to leave people stuck. Full write-up in
[reports/security-review.md](reports/security-review.md), tests in [Tests~](Tests~).


# I DID NOT MAKE THIS. I JUST MODIFIED THE CODE!

I added a dev region choice. Meaning if your apk is a development build, it's going to connect to the dev region.
```cs
        [Tooltip("The region that people connect to if the apk is a development build (check your build settings).")]
        public string DevRegion = "";
```
I also added a Offline mode bool. Meaning that it simulates an online connection. PUN can be used as if it was in an online connection.
```cs
        [Tooltip("Simulates an online connection.\nPUN can be used as usual.")]
        public bool StartInOfflineMode = false;
```
This did nothing before the September 2026 review. The code set it inside a branch
that only a *duplicate* manager could reach, so the real manager never read it.
`Start()` applies it now.
I also also added more connection states for more debugging.
```cs
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
```

I also also ALSO added a TMP that you can assign and it will say what's going on.
```cs
        [Tooltip("Optional. A TextMeshPro that displays what PhotonVRManager is doing.")]
        public TextMeshPro LogText;
```
Assign it in the Inspector, or leave it empty and the status only goes to the log.

It was `public static` until the September 2026 review. Unity does not serialize
static fields, so the Inspector could never assign it and it was null in every build.
`Start()` then wrote to it before doing anything else. That one line stopped the
manager connecting at all. Writes go through a helper that tolerates it being unset:
```cs
        private static void Status(string message)
        {
            Debug.Log(message);
            if (Manager != null && Manager.LogText != null)
                Manager.LogText.text = message;
        }
```

I just basically made it more easy so you don't have to go manually into the Photon server settings and just be able to control Photon from one script.

# Credits to [fchb1239](https://github.com/fchb1239/PhotonVR) for making Photon VR.

[![Download](https://img.shields.io/badge/Download-blue.svg)](https://github.com/fchb1239/PhotonVR/releases)
[![Discord](https://img.shields.io/badge/Discord-blue.svg)](https://discord.gg/rRvnU846Bf)

# Documentation
By default everything is set up for a super simple system, obviously you can code some stuff yourself to make everything work for your application.

You need to have [PUN 2](https://assetstore.unity.com/packages/tools/network/pun-2-free-119922) and [Photon Voice 2](https://assetstore.unity.com/packages/tools/audio/photon-voice-2-130518) in your project before importing this
You also need TextMeshPro - that's built into unity, you just gotta go into the Player prefab and press Import TMP Essentials.

![](https://user-images.githubusercontent.com/29258204/178261709-e87f2177-d4bc-4878-91ae-5d2d52d5081c.png)


Start off by going in Resources/PhotonVR/Prefabs and dragging everything in there into the scene.

![](https://user-images.githubusercontent.com/29258204/178261831-ee9e4744-5b80-443f-9dcc-5913dcaaca49.png)


Then, put your Photon AppId into AppId and Photon Voice AppId into VoiceAppId, then put in a region - the default is "eu". Click [here](https://doc.photonengine.com/en-us/pun/current/connection-and-authentication/regions) to see a list of regions

Then drag in your controllers and headset into Head, Left Hand and Right Hand - set the colour to the default colour.

Set Default Queue to the queue you want to automatically load into when the game starts and set the Default Room Limit to the maximum amount of players you want in a default room.

Connect On Awake makes it so when the game loads you instantly try to connect, Join Room On Connect makes it so it instantly joins the Default Queue with the Default Room Limit when you connect to the server.

![](https://user-images.githubusercontent.com/29258204/178260207-79da9ffe-efbb-44cc-a648-1cd40900c82d.png)

Including Photon.VR
```cs
using Photon.VR;
```

Connecting to the servers
```cs
PhotonVRManager.Connect();
```

Switching Photon servers
```cs
PhotonVRManager.ChangeServers("AppId", "VoiceAppId");
```

Connecting to the servers authenticated
```cs
// These will not actually work, you need to set this up with PlayFab or something else
string username = "MYID";
string token = "MYTOKEN";
PhotonVRManager.ConnectAuthenticated(username, token);
```

Switching Photon servers authenticated
```cs
// These will not actually work, you need to set this up with PlayFab or something else
string username = "MYID";
string token = "MYTOKEN";
PhotonVRManager.ChangeServersAuthenticated("AppId", "VoiceAppId", username, token);
```

[PlayFab Photon authentication documentation](https://docs.microsoft.com/en-us/gaming/playfab/sdks/photon/quickstart)

Joining rooms
```cs
// It will only join people on the same queue but the room codes themselves are random
string queue = "Space";
// Optional
int maxPlayers = 8;
PhotonVRManager.JoinRandomRoom(queue, maxPlayers);
```

Joining private rooms
```cs
string roomCode = "1234";
// Optional
int maxPlayers = 8;
PhotonVRManager.JoinPrivateRoom(roomCode, maxPlayers);
```

Codes are cleaned up before use. Case is raised, spaces and punctuation are dropped,
and anything past 16 characters is cut. So `ab-c 12` and `AB-C12` reach the same
room, which matters when two people are reading a code to each other. Dashes and
underscores survive, so a name like `MY-ROOM` works as well as a generated code.

```cs
PhotonVRManager.NormaliseRoomCode(" ab-c 12 ");  // "AB-C12"
PhotonVRManager.IsValidRoomCode("!!!");          // false
```

<b>Calling from a button</b>

The methods above are static, and Unity cannot bind a UnityEvent to a static method
from the Inspector. So the manager also has three instance methods that take a single
string, which a button or a UnityEvent can call directly.

```cs
public void JoinPrivate(string code)
public void JoinQueue(string queue)
public void Leave()
```

Drag the PhotonVRManager object into the event slot, pick one of these, and type the
code or queue name in the field underneath. Feed `JoinPrivate` from whatever keyboard
or input field you already have.

<b>Jumping between queues</b>

You can now switch queue while already in a room. PUN refuses a join in that state,
so the manager leaves the current room first and joins once it is out. Nothing extra
to call:

```cs
PhotonVRManager.JoinRandomRoom("Space");
```

To read your current state:

```cs
PhotonVRManager.CurrentQueue;    // "Space", or "" in a private room
PhotonVRManager.InPrivateRoom;   // true or false
PhotonVRManager.RoomCode;        // the code, without the internal prefix
PhotonVRManager.LeaveRoom();     // leave without joining anything else
```

Switching scenes
```cs
int sceneIndex = 1;
// Optional
int maxPlayers = 8;
PhotonVRManager.SwitchScenes(SceneIndex, maxPlayers);
```

Setting name
```cs
PhotonVRManager.SetUsername("fchb1239");
```


Setting colour
```cs
Color myColour = new Color(0, 0, 1);
PhotonVRManager.SetColour(myColour);
```

<b>Cosmetics</b>

To put on cosmetics you can use two functions to do the job. You can put on an entire set like so
```cs
PhotonVRManager.SetCosmetics(new Dictionary<string, string>
{
    { "Head", "VRTopHat" },
    { "Face", "VRSunglasses" }
    // And so on
});
```

Or if you want to do one at a time (like if you have a button with a specefic cosmetic) then do like so
```cs
PhotonVRManager.SetCosmetic("Head", "VRTopHat");
```

If you set a cosmetic part to a cosmetic that doesn't exit, it won't equip anything. So if you want to clear the head of cosmetics do like so
```cs
PhotonVRManager.SetCosmetic("Head", "");
```
It's the same story with SetCosmetics.

Every body part on the player has a child named something with "Cosmetics", under those you put the models of the cosmetics you want.
You have to rename the object to the ID of the cosmetic, let's say you put on a hat with the ID "VRTopHat" then under the Cosmetics child of the head you put your model and name it "VRTopHat", like this:

![](https://user-images.githubusercontent.com/29258204/178257224-254c10c5-e68a-4fd9-97f4-308896e62bf7.png)


