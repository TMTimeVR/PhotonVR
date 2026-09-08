using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using TMPro;

namespace Photon.VR.Player
{
    public class PhotonVRPlayerName : MonoBehaviour
    {
        [Tooltip("How high the text should be above the players head")]
        public float Offset = 0.17f;
        public Transform Head;

        private void Update()
        {

            if (Head == null || PhotonVRManager.Manager == null || PhotonVRManager.Manager.Head == null)
                return;

            transform.position = Head.position + new Vector3(0, Offset, 0);

            Vector3 direction = PhotonVRManager.Manager.Head.position - transform.position;

            if (direction.sqrMagnitude < 0.0001f)
                return;

            Quaternion look = Quaternion.LookRotation(direction);
            Quaternion quaternion = Quaternion.Euler(0, look.eulerAngles.y, 0);
            transform.rotation = Quaternion.Slerp(transform.rotation, quaternion, 10 * Time.deltaTime);
        }
    }

}
