using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UnityStandardAssets.CrossPlatformInput
{
    [RequireComponent(typeof(Image))]
    public class TouchPad : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        public enum AxisOption
        {
            Both,
            OnlyHorizontal,
            OnlyVertical
        }

        public enum ControlStyle
        {
            Absolute,
            Relative,
            Swipe,
        }

        public AxisOption axesToUse = AxisOption.Both;
        public ControlStyle controlStyle = ControlStyle.Swipe;
        public string horizontalAxisName = "Mouse X";
        public string verticalAxisName = "Mouse Y";
        public float Xsensitivity = 1f;
        public float Ysensitivity = 1f;
        [Tooltip("Smoothing factor for touch swipe")]
        public float smoothing = 30f;
        public bool invertY = false;

        private CrossPlatformInputManager.VirtualAxis m_HorizontalVirtualAxis;
        private CrossPlatformInputManager.VirtualAxis m_VerticalVirtualAxis;
        private bool m_UseX;
        private bool m_UseY;
        private bool m_Dragging;
        private int m_PointerId = -1;

        private Vector2 m_CurrentFrameDelta;
        private Vector2 m_SmoothVelocity;

        // Base sensitivity scale calibrated for PlayerController
        private const float SENSITIVITY_SCALE = 0.0022f;

        void OnEnable()
        {
            CreateVirtualAxes();
        }

        void CreateVirtualAxes()
        {
            m_UseX = (axesToUse == AxisOption.Both || axesToUse == AxisOption.OnlyHorizontal);
            m_UseY = (axesToUse == AxisOption.Both || axesToUse == AxisOption.OnlyVertical);

            if (m_UseX)
            {
                if (CrossPlatformInputManager.AxisExists(horizontalAxisName))
                {
                    m_HorizontalVirtualAxis = CrossPlatformInputManager.VirtualAxisReference(horizontalAxisName);
                }
                else
                {
                    m_HorizontalVirtualAxis = new CrossPlatformInputManager.VirtualAxis(horizontalAxisName);
                    CrossPlatformInputManager.RegisterVirtualAxis(m_HorizontalVirtualAxis);
                }
            }

            if (m_UseY)
            {
                if (CrossPlatformInputManager.AxisExists(verticalAxisName))
                {
                    m_VerticalVirtualAxis = CrossPlatformInputManager.VirtualAxisReference(verticalAxisName);
                }
                else
                {
                    m_VerticalVirtualAxis = new CrossPlatformInputManager.VirtualAxis(verticalAxisName);
                    CrossPlatformInputManager.RegisterVirtualAxis(m_VerticalVirtualAxis);
                }
            }
        }

        public void OnPointerDown(PointerEventData data)
        {
            if (!m_Dragging)
            {
                m_Dragging = true;
                m_PointerId = data.pointerId;
                m_CurrentFrameDelta = Vector2.zero;
                m_SmoothVelocity = Vector2.zero;
            }
        }

        public void OnDrag(PointerEventData data)
        {
            if (m_Dragging && data.pointerId == m_PointerId)
            {
                m_CurrentFrameDelta += data.delta;
            }
        }

        public void OnPointerUp(PointerEventData data)
        {
            if (data.pointerId == m_PointerId)
            {
                m_Dragging = false;
                m_PointerId = -1;
                m_CurrentFrameDelta = Vector2.zero;
            }
        }

        void Update()
        {
            float dt = Mathf.Max(Time.deltaTime, 0.0001f);

            // Convert frame delta into pixels per second velocity
            Vector2 targetVelocity = m_Dragging ? (m_CurrentFrameDelta / dt) : Vector2.zero;
            m_CurrentFrameDelta = Vector2.zero;

            if (smoothing > 0f)
            {
                float t = Mathf.Clamp01(dt * smoothing);
                m_SmoothVelocity = Vector2.Lerp(m_SmoothVelocity, targetVelocity, t);
            }
            else
            {
                m_SmoothVelocity = targetVelocity;
            }

            if (!m_Dragging && m_SmoothVelocity.sqrMagnitude < 1f)
            {
                m_SmoothVelocity = Vector2.zero;
            }

            float outX = m_SmoothVelocity.x * SENSITIVITY_SCALE * Xsensitivity;
            float outY = m_SmoothVelocity.y * SENSITIVITY_SCALE * Ysensitivity * (invertY ? -1f : 1f);

            if (m_UseX && m_HorizontalVirtualAxis != null)
            {
                m_HorizontalVirtualAxis.Update(outX);
            }

            if (m_UseY && m_VerticalVirtualAxis != null)
            {
                m_VerticalVirtualAxis.Update(outY);
            }
        }

        void OnDisable()
        {
            if (m_HorizontalVirtualAxis != null)
            {
                m_HorizontalVirtualAxis.Update(0f);
            }
            if (m_VerticalVirtualAxis != null)
            {
                m_VerticalVirtualAxis.Update(0f);
            }
            m_Dragging = false;
            m_PointerId = -1;
            m_CurrentFrameDelta = Vector2.zero;
            m_SmoothVelocity = Vector2.zero;
        }
    }
}
