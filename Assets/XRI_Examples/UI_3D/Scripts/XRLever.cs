using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;

namespace UnityEngine.XR.Content.Interaction
{
    /// <summary>
    /// An interactable lever that snaps into an on or off position by a direct interactor
    /// </summary>
    public class XRLever : XRBaseInteractable
    {
        const float k_LeverDeadZone = 0.1f; // Prevents rapid switching between on and off states when right in the middle

        [SerializeField]
        [Tooltip("The object that is visually grabbed and manipulated")]
        Transform m_Handle = null;

        [SerializeField]
        [Tooltip("The value of the lever")]
        int m_Value = 0;

        [SerializeField]
        [Tooltip("If enabled, the lever will snap to the value position when released")]
        bool m_LockToValue;
        
        [SerializeField]
        [Tooltip("The number of steps the lever can have, distance is equal for all")]
        [Range(2, 99)]
        int m_Steps = 2;

        [SerializeField]
        [Tooltip("Angle of the lever in the 'on' position")]
        float m_MaxAngle = 90.0f;

        [SerializeField]
        [Tooltip("Angle of the lever in the 'off' position")]
        float m_MinAngle = -90.0f;

        [SerializeField]
        Vector3 m_RotationAxis = Vector3.right;

        [SerializeField]
        Vector3 m_upAxis = Vector3.up;

        [Tooltip("Events to trigger when the lever deactivates")]
        List<UnityEvent> m_OnLeverActivate = new List<UnityEvent>(); 

        IXRSelectInteractor m_Interactor;

        private List<float> values = new List<float>();

        /// <summary>
        /// The object that is visually grabbed and manipulated
        /// </summary>
        public Transform handle
        {
            get => m_Handle;
            set => m_Handle = value;
        }

        /// <summary>
        /// The value of the lever
        /// </summary>
        public int value
        {
            get => m_Value;
            set => SetValue(value, true);
        }

        /// <summary>
        /// If enabled, the lever will snap to the value position when released
        /// </summary>
        public bool lockToValue
        {
            get => m_LockToValue;
            set => m_LockToValue = value;
        }
        
        /// <summary>
        /// The number of steps the lever can have, distance is equal for all
        /// </summary>
        public int steps
        {
            get => m_Steps;
            set
            {
                if (value == m_Steps)
                    return;
                m_Steps = Mathf.Clamp(value, 2, 99);
                OnStepsUpdated();
            } 
        }

        /// <summary>
        /// Angle of the lever in the 'on' position
        /// </summary>
        public float maxAngle
        {
            get => m_MaxAngle;
            set
            {
                m_MaxAngle = value;
                for (var i = 0; i < m_Steps; i++)
                {
                    values.Add(m_MinAngle + i * (m_MaxAngle - m_MinAngle) / (steps - 1));
                }
            }
        }

        /// <summary>
        /// Angle of the lever in the 'off' position
        /// </summary>
        public float minAngle
        {
            get => m_MinAngle;
            set
            {
                m_MinAngle = value;
                for (var i = 0; i < m_Steps; i++)
                {
                    values.Add(m_MinAngle + i * (m_MaxAngle - m_MinAngle) / (steps - 1));
                }
            }
        }

        public Vector3 rotatingAxis
        {
            get => m_RotationAxis;
            set => m_RotationAxis = value;
        }


        public Vector3 upAxis
        {
            get => m_upAxis;
            set => m_upAxis = value;
        }


        /// <summary>
        /// Events to trigger when the lever activates
        /// </summary>
        public List<UnityEvent> onLeverActivate => m_OnLeverActivate;
        
        /// <summary>
        /// Events to trigger when the lever activates
        /// </summary>
        public UnityEvent onLeverActivateByIndex(int index) => m_OnLeverActivate[index];

        protected override void Awake()
        {
            base.Awake();
            OnStepsUpdated();
        }

        void Start()
        {
            SetValue(m_Value, true);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            selectEntered.AddListener(StartGrab);
            selectExited.AddListener(EndGrab);
        }

        protected override void OnDisable()
        {
            selectEntered.RemoveListener(StartGrab);
            selectExited.RemoveListener(EndGrab);
            base.OnDisable();
        }

        private void OnStepsUpdated()
        {
            values.Clear();
            for (var i = 0; i < m_Steps; i++)
            {
                values.Add(m_MinAngle + i * (m_MaxAngle - m_MinAngle) / (steps - 1));
            }
            m_OnLeverActivate.Clear();
            for (var i = 0; i < m_Steps; i++)
            {
                m_OnLeverActivate.Add(new UnityEvent());
            }
        }

        void StartGrab(SelectEnterEventArgs args)
        {
            m_Interactor = args.interactorObject;
        }

        void EndGrab(SelectExitEventArgs args)
        {
            SetValue(m_Value, true);
            m_Interactor = null;
        }

        public override void ProcessInteractable(XRInteractionUpdateOrder.UpdatePhase updatePhase)
        {
            base.ProcessInteractable(updatePhase);

            if (updatePhase == XRInteractionUpdateOrder.UpdatePhase.Dynamic)
            {
                if (isSelected)
                {
                    UpdateValue();
                }
            }
        }

        Vector3 GetLookDirection()
        {
            Vector3 direction = m_Interactor.GetAttachTransform(this).position - m_Handle.position;
            direction = transform.InverseTransformDirection(direction);

            return direction.normalized;
        }

        void UpdateValue()
        {
            var lookDirection = GetLookDirection();

            var rightAxis = Vector3.Cross(m_RotationAxis, m_upAxis);
            var rightValue= Vector3.Dot(rightAxis, lookDirection);
            var upValue = Vector3.Dot(m_upAxis, lookDirection);

            var lookAngle = Mathf.Atan2(rightValue, upValue) * Mathf.Rad2Deg;

            if (m_MinAngle < m_MaxAngle)
                lookAngle = Mathf.Clamp(lookAngle, m_MinAngle, m_MaxAngle);
            else
                lookAngle = Mathf.Clamp(lookAngle, m_MaxAngle, m_MinAngle);

            // TODO: include deadzone in steps
            
            // var maxAngleDistance = Mathf.Abs(m_MaxAngle - lookAngle);
            // var minAngleDistance = Mathf.Abs(m_MinAngle - lookAngle);
            //
            // if (m_Value)
            //     maxAngleDistance *= (1.0f - k_LeverDeadZone);
            // else
            //     minAngleDistance *= (1.0f - k_LeverDeadZone);
            //
            // var newValue = (maxAngleDistance < minAngleDistance);
            
            var newValue = values.IndexOf(values.OrderBy(a => Math.Abs(lookAngle - a)).First());

            SetHandleAngle(lookAngle);

            SetValue(newValue);
        }

        void SetValue(int unClampedVal, bool forceRotation = false)
        {
            var val = Mathf.Clamp(unClampedVal, 0, steps - 1);
            
            if (m_Value == val)
            {
                if (forceRotation)
                    SetHandleAngle(m_MinAngle + val * (m_MaxAngle - m_MinAngle) / (steps - 1));

                return;
            }

            m_Value = val;

            // if (m_Value)
            // {
            //     m_OnLeverActivate.Invoke();
            // }
            // else
            // {
            //     m_OnLeverDeactivate.Invoke();
            // }
            onLeverActivateByIndex(val).Invoke();

            if (!isSelected && (m_LockToValue || forceRotation))
                SetHandleAngle(m_MinAngle + val * (m_MaxAngle - m_MinAngle) / (steps - 1));
        }

        void SetHandleAngle(float angle)
        {
            if (m_Handle != null)
                m_Handle.localRotation = Quaternion.Euler(m_RotationAxis * angle);
        }

        void OnDrawGizmosSelected()
        {
            var angleStartPoint = transform.position;

            if (m_Handle != null)
                angleStartPoint = m_Handle.position;

            const float k_AngleLength = 0.25f;

            var angleMaxPoint = angleStartPoint + transform.TransformDirection(Quaternion.Euler(m_RotationAxis * m_MaxAngle) * Vector3.up) * k_AngleLength;
            var angleMinPoint = angleStartPoint + transform.TransformDirection(Quaternion.Euler(m_RotationAxis * m_MinAngle) * Vector3.up) * k_AngleLength;

            Gizmos.color = Color.green;
            Gizmos.DrawLine(angleStartPoint, angleMaxPoint);

            Gizmos.color = Color.red;
            Gizmos.DrawLine(angleStartPoint, angleMinPoint);
        }

        void OnValidate()
        {
            SetHandleAngle(m_MinAngle + m_Value * (m_MaxAngle - m_MinAngle) / (steps - 1));
        }
    }
}
