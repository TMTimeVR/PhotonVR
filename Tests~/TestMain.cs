// Behavioural tests for the PhotonVR fixes. Exercises the real source files
// against functional Unity stubs and asserts the properties the hardening relies
// on. Private statics are reached by reflection, which is fine for a test.
#if TESTPASS
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Photon.VR;
using Photon.VR.Saving;
using UnityEngine;

public static class TestMain
{
    static int pass = 0, fail = 0;

    static void Check(string name, Func<bool> cond, string detail = null)
    {
        bool ok;
        string err = null;
        try { ok = cond(); }
        catch (Exception e) { ok = false; err = e.GetType().Name + ": " + e.Message; }
        if (ok) { pass++; Console.WriteLine("  PASS  " + name); }
        else { fail++; Console.WriteLine("  FAIL  " + name + (err != null ? "  -> threw: " + err : (detail != null ? "  -> " + detail : ""))); }
    }

    static object InvokePrivate(Type t, string method, params object[] args)
    {
        MethodInfo m = t.GetMethod(method, BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public | BindingFlags.Instance);
        if (m == null) throw new MissingMethodException(t.Name + "." + method);
        return m.Invoke(null, args);
    }

    public static int Main()
    {
        Console.WriteLine("\n== 1. Room codes: space, alphabet, collision resistance ==");
        {
            var mgr = new PhotonVRManager();
            var codes = new List<string>();
            for (int i = 0; i < 20000; i++) codes.Add(mgr.CreateRoomCode());

            Check("every code is 6 characters", () => codes.All(c => c.Length == 6));
            Check("alphabet excludes I, O, 0 and 1 (misread when spoken)",
                  () => codes.All(c => c.All(ch => "ABCDEFGHJKLMNPQRSTUVWXYZ23456789".Contains(ch))));
            // 32^6 is about 1.07e9, so across 20000 draws the birthday expectation
            // is roughly 0.19 collisions. Asserting zero would be asserting luck;
            // assert the rate is negligible instead.
            Check("20000 draws are >99.9% distinct", () => codes.Distinct().Count() >= 19980,
                  "distinct=" + codes.Distinct().Count() + "/20000");

            // The original drew from Next(99999), about 1e5 codes. Compare collision
            // rates over the same number of draws, giving the old scheme the best
            // case of a single well-seeded Random instance.
            //
            // Not tested here: `new System.Random()` per call is seeded from
            // Environment.TickCount on Unity's Mono runtime, so two clients failing
            // matchmaking in the same tick got identical codes. .NET 8 seeds
            // randomly instead, so this harness cannot show that. The narrow space
            // below is a problem on every runtime.
            var rng = new System.Random(12345);
            var legacy = new List<string>();
            for (int i = 0; i < 2000; i++) legacy.Add(rng.Next(99999).ToString());
            int legacyDupes = legacy.Count - legacy.Distinct().Count();

            var modern = new List<string>();
            for (int i = 0; i < 2000; i++) modern.Add(mgr.CreateRoomCode());
            int modernDupes = modern.Count - modern.Distinct().Count();

            Check("ORIGINAL 1e5 code space collides over 2000 draws", () => legacyDupes > 0,
                  "dupes=" + legacyDupes);
            Check("new code space does not", () => modernDupes == 0, "dupes=" + modernDupes);
        }

        Console.WriteLine("\n== 2. Display names cannot carry TMP rich text or unbounded length ==");
        {
            Func<string, string> San = s => (string)InvokePrivate(typeof(PhotonVRManager), "SanitiseDisplayName", s);

            Check("size tag stripped", () => !San("<size=5000>huge").Contains("<"), San("<size=5000>huge"));
            Check("sprite tag stripped", () => !San("<sprite=1>").Contains("<"));
            Check("colour tag stripped", () => San("<color=red>bob</color>") == "bob", San("<color=red>bob</color>"));
            Check("length capped at 16", () => San(new string('A', 500)).Length == 16);
            Check("null becomes a usable default", () => San(null) == "Player");
            Check("empty becomes a usable default", () => San("") == "Player");
            Check("tag-only name becomes a usable default", () => San("<b></b>") == "Player");
            Check("ordinary name survives unchanged", () => San("TMTime") == "TMTime");
        }

        Console.WriteLine("\n== 3. Colours are clamped before display ==");
        {
            Func<Color, Color> Clamp = c => (Color)InvokePrivate(typeof(PhotonVRManager), "ClampColour", c);

            Check("HDR whiteout clamped to 1", () => { var c = Clamp(new Color(99999, 99999, 99999, 1)); return c.r == 1 && c.g == 1 && c.b == 1; });
            Check("negatives clamped to 0", () => { var c = Clamp(new Color(-50, -50, -50, 1)); return c.r == 0 && c.g == 0 && c.b == 0; });
            Check("in-range colour untouched", () => { var c = Clamp(new Color(0.25f, 0.5f, 0.75f, 1f)); return c.r == 0.25f && c.g == 0.5f && c.b == 0.75f; });
        }

        Console.WriteLine("\n== 4. Room options clamp MaxPlayers (0 means unlimited to PUN) ==");
        {
            Func<int, byte> Max = n =>
            {
                MethodInfo m = typeof(PhotonVRManager).GetMethod("BuildRoomOptions", BindingFlags.NonPublic | BindingFlags.Static);
                var ro = (Photon.Realtime.RoomOptions)m.Invoke(null, new object[] { n, true, null });
                return ro.MaxPlayers;
            };

            Check("256 does not wrap to 0 (unlimited)", () => Max(256) == 255, "got " + Max(256));
            Check("300 does not wrap to 44", () => Max(300) == 255, "got " + Max(300));
            Check("0 becomes at least 1", () => Max(0) == 1, "got " + Max(0));
            Check("negative becomes at least 1", () => Max(-5) == 1, "got " + Max(-5));
            Check("normal value preserved", () => Max(10) == 10);
            // Demonstrate the original unchecked cast for contrast.
            Check("ORIGINAL unchecked cast wrapped 256 to 0", () => unchecked((byte)256) == 0);
        }

        Console.WriteLine("\n== 5. Saved cosmetics round-trip without corrupting keys ==");
        {
            PlayerPrefs.Store.Clear();

            var d = new Dictionary<string, string> {
                { "Head", "TopHat" },
                { "Face", "Sunglasses" },
                { "Weird,Key", "WithComma" },
                { "Empty", "" }
            };
            PhotonVRValueSaver.SaveDictionary("Cosmetics", d);
            var back = PhotonVRValueSaver.GetDictionary("Cosmetics");

            Check("all four entries survive", () => back.Count == 4, "count=" + back.Count);
            Check("values match", () => back["Head"] == "TopHat" && back["Face"] == "Sunglasses");
            Check("a key containing a comma is not corrupted", () => back.ContainsKey("Weird,Key") && back["Weird,Key"] == "WithComma");
            Check("empty value preserved", () => back["Empty"] == "");

            PlayerPrefs.Store.Clear();
            Check("absent key returns an empty dictionary, not one bogus entry",
                  () => PhotonVRValueSaver.GetDictionary("Cosmetics").Count == 0,
                  "count=" + PhotonVRValueSaver.GetDictionary("Cosmetics").Count);

            PlayerPrefs.Store["Cosmetics"] = "not json at all {{{";
            Check("corrupt saved data does not throw", () => PhotonVRValueSaver.GetDictionary("Cosmetics").Count == 0);

            PlayerPrefs.Store.Clear();
            PhotonVRValueSaver.SaveDictionary("Cosmetics", null);
            Check("saving null does not throw and reads back empty",
                  () => PhotonVRValueSaver.GetDictionary("Cosmetics").Count == 0);

            // The prefix-collision the old scheme had: ("Cosmetic","s") vs ("Cosmetics","").
            PlayerPrefs.Store.Clear();
            PhotonVRValueSaver.SaveDictionary("Cosmetic", new Dictionary<string, string> { { "s", "A" } });
            PhotonVRValueSaver.SaveDictionary("Cosmetics", new Dictionary<string, string> { { "", "B" } });
            Check("two saves with colliding prefixes stay separate",
                  () => PhotonVRValueSaver.GetDictionary("Cosmetic")["s"] == "A");
        }

        Console.WriteLine("\n== 6. Room name namespaces are separate ==");
        {
            string pub = (string)typeof(PhotonVRManager).GetField("PublicRoomPrefix", BindingFlags.NonPublic | BindingFlags.Static).GetRawConstantValue();
            string priv = (string)typeof(PhotonVRManager).GetField("PrivateRoomPrefix", BindingFlags.NonPublic | BindingFlags.Static).GetRawConstantValue();
            Check("public and private prefixes differ", () => pub != priv, pub + " vs " + priv);
            Check("neither prefix is empty", () => pub.Length > 0 && priv.Length > 0);
            Check("a private code cannot name a public room",
                  () => !(priv + "ABC123").StartsWith(pub));
        }

        Console.WriteLine("\n----------------------------------------");
        Console.WriteLine($"{pass} passed, {fail} failed");
        return fail == 0 ? 0 : 1;
    }
}
#endif
