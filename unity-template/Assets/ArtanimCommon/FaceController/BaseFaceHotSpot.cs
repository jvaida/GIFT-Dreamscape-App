using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Artanim
{
    public abstract class BaseFaceHotSpot : MonoBehaviour
    {
        [Tooltip("The priority of the hotspot if more than one hotspot is valid for the avatar. 0=Highes priority")]
        public int Priority = 0;
        [Tooltip("Minimal horizontal viewing angle")]
        public float MinHorizontalAngle = -10.0f;
        [Tooltip("Maximal horizontal viewing angle")]
        public float MaxHorizontalAngle = 10.0f;
        [Tooltip("Minimal vertical viewing angle")]
        public float MinVerticalAngle = -5.0f;
        [Tooltip("Maximal vertical viewing angle")]
        public float MaxVerticalAngle = 5.0f;

        private void OnTriggerEnter(Collider other)
        {
            var eyeController = GetTriggerEyeController(other);
            if (eyeController)
            {
                eyeController.RegisterHotSpot(this);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            var eyeController = GetTriggerEyeController(other);
            if (eyeController)
            {
                eyeController.UnRegisterHotSpot(this);
            }
        }

        private AvatarEyeController GetTriggerEyeController(Collider other)
        {
            var bodyPart = other.GetComponent<AvatarBodyPart>();
            if (bodyPart && bodyPart.BodyPart == Location.Messages.EAvatarBodyPart.Head)
            {
                return bodyPart.GetComponentInParent<AvatarEyeController>();
            }
            return null;
        }

        public override string ToString()
        {
            return string.Format("{0}: name={1}, priority={2}", GetType().Name, name, Priority);
        }

        //[Range(0f, 180f)]
        //public float TestHorizontal = 30f;

        //[Range(0f, 180f)]
        //public float TestVertical = 30f;

        //private void OnDrawGizmosSelected()
        //{
        //    //Forward
        //    Gizmos.color = Color.blue;
        //    Gizmos.DrawLine(transform.position, transform.position + transform.forward.normalized * GetComponent<SphereCollider>().radius);

        //    //Angles
        //    DrawAngleGizmo(transform.forward, transform.up, TestHorizontal, Color.green);
        //    DrawAngleGizmo(transform.forward, transform.up, -TestHorizontal, Color.green);
        //    DrawAngleGizmo(transform.forward, transform.right, TestVertical, Color.red);
        //    DrawAngleGizmo(transform.forward, transform.right, -TestVertical, Color.red);
        //}

        //private void DrawAngleGizmo(Vector3 direction, Vector3 axis, float angle, Color color)
        //{
        //    Gizmos.color = color;
        //    //Vector3 target = transform.position + Quaternion.AngleAxis(angle, axis) * direction.normalized * 2f;
        //    Vector3 target = transform.position + Quaternion.AngleAxis(angle, axis) * direction.normalized * GetComponent<SphereCollider>().radius;
        //    Gizmos.DrawLine(transform.position, target);
        //}
    }
}