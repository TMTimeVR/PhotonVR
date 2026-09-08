using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

using Photon.Pun;

using TMPro;

namespace Photon.VR.Player
{
    public class PhotonVRPlayer : MonoBehaviourPun
    {
        [Header("Objects")]
        public Transform Head;
        public Transform Body;
        public Transform LeftHand;
        public Transform RightHand;
        [Tooltip("The objects that will get the colour of the player applied to them")]
        public List<MeshRenderer> ColourObjects;

        [Space]
        [Tooltip("Feel free to add as many slots as you feel necessary")]
        public List<CosmeticSlot> CosmeticSlots = new List<CosmeticSlot>();

        [Header("Other")]
        public TextMeshPro NameText;
        public bool HideLocalPlayer = true;

        private void Awake()
        {
            if (photonView.IsMine)
            {
                if (PhotonVRManager.Manager != null)
                    PhotonVRManager.Manager.LocalPlayer = this;

                if (HideLocalPlayer)
                {

                    Hide(Head);
                    Hide(Body);
                    Hide(RightHand);
                    Hide(LeftHand);
                    if (NameText != null)
                        NameText.gameObject.SetActive(false);
                }
            }

            if (NameText != null)
                NameText.richText = false;

            DontDestroyOnLoad(gameObject);

            _RefreshPlayerValues();
        }

        private static void Hide(Transform target)
        {
            if (target != null)
                target.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (!photonView.IsMine)
                return;

            PhotonVRManager manager = PhotonVRManager.Manager;
            if (manager == null)
                return;

            Follow(Head, manager.Head);
            Follow(RightHand, manager.RightHand);
            Follow(LeftHand, manager.LeftHand);
        }

        private static void Follow(Transform target, Transform source)
        {
            if (target == null || source == null)
                return;

            target.position = source.position;
            target.rotation = source.rotation;
        }

        public void RefreshPlayerValues()
        {

            _RefreshPlayerValues();
            photonView.RPC("RPCRefreshPlayerValues", RpcTarget.Others);
        }

        private float lastRemoteRefresh = -RemoteRefreshCooldown;
        private const float RemoteRefreshCooldown = 0.5f;

        [PunRPC]
        private void RPCRefreshPlayerValues(PhotonMessageInfo info)
        {
            if (info.Sender == null || info.Sender != photonView.Owner)
                return;

            if (Time.time - lastRemoteRefresh < RemoteRefreshCooldown)
                return;
            lastRemoteRefresh = Time.time;

            _RefreshPlayerValues();
        }

        private void _RefreshPlayerValues()
        {

            Photon.Realtime.Player owner = photonView.Owner;
            if (owner == null)
                return;

            ExitGames.Client.Photon.Hashtable properties = owner.CustomProperties;

            if (NameText != null)
                NameText.text = SanitiseName(owner.NickName);

            Color colour = ReadColour(properties);
            foreach (MeshRenderer renderer in ColourObjects)
            {
                if (renderer != null)
                    renderer.material.color = colour;
            }

            ApplyCosmetics(ReadCosmetics(properties));
        }

        private static string SanitiseName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return string.Empty;

            name = RichTextTag.Replace(name, string.Empty).Trim();
            return name.Length > MaxNameLength ? name.Substring(0, MaxNameLength) : name;
        }

        private const int MaxNameLength = 16;
        private static readonly Regex RichTextTag = new Regex("<[^>]*>", RegexOptions.Compiled);

        private static Color ReadColour(ExitGames.Client.Photon.Hashtable properties)
        {
            if (properties == null || !properties.TryGetValue("Colour", out object raw) || !(raw is string json))
                return Color.white;

            try
            {
                Color colour = JsonUtility.FromJson<Color>(json);

                return new Color(
                    Mathf.Clamp01(colour.r),
                    Mathf.Clamp01(colour.g),
                    Mathf.Clamp01(colour.b),
                    Mathf.Clamp01(colour.a));
            }
            catch (Exception)
            {
                return Color.white;
            }
        }

        private static Dictionary<string, string> ReadCosmetics(ExitGames.Client.Photon.Hashtable properties)
        {
            if (properties == null || !properties.TryGetValue("Cosmetics", out object raw))
                return null;

            return raw as Dictionary<string, string>;
        }

        private void ApplyCosmetics(Dictionary<string, string> cosmetics)
        {
            if (cosmetics == null)
                return;

            foreach (KeyValuePair<string, string> cosmetic in cosmetics)
            {
                if (cosmetic.Key == null)
                    continue;

                foreach (CosmeticSlot slot in CosmeticSlots)
                {
                    if (slot == null || slot.Object == null || slot.SlotName != cosmetic.Key)
                        continue;

                    foreach (Transform cos in slot.Object)
                        cos.gameObject.SetActive(cos.name == cosmetic.Value);
                }
            }
        }

        [Serializable]
        public class CosmeticSlot
        {
            public string SlotName;
            public Transform Object;
        }
    }
}
