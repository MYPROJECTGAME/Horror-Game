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
        [Tooltip("Smoothing factor for camera rotation. Higher = sharper, lower = smoother")]
        public float smoothing = 28f;
        public bool invertY = false;

        private CrossPlatformInputManager.VirtualAxis m_HorizontalVirtualAxis;
        private CrossPlatformInputManager.VirtualAxis m_VerticalVirtualAxis;
        private bool m_UseX;
        private bool m_UseY;
        private bool m_Dragging;
        private int m_PointerId = -1;

        private Vector2 m_AccumulatedDelta;
        private Vector2 m_SmoothDelta;

        // Base sensitivity scale calibrated for PlayerController's (mouseSensetivity * 2) * Time.deltaTime
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
                m_AccumulatedDelta = Vector2.zero;
            }
        }

        public void OnDrag(PointerEventData data)
        {
            if (m_Dragging && data.pointerId == m_PointerId)
            {
                m_AccumulatedDelta += data.delta;
            }
        }

        public void OnPointerUp(PointerEventData data)
        {
            if (data.pointerId == m_PointerId)
            {
                m_Dragging = false;
                m_PointerId = -1;
                m_AccumulatedDelta = Vector2.zero;
            }
        }

        void Update()
        {
            float dt = Mathf.Max(Time.deltaTime, 0.0001f);

            Vector2 target = m_Dragging ? m_AccumulatedDelta : Vector2.zero;
            m_AccumulatedDelta = Vector2.zero;

            if (smoothing > 0f)
            {
                m_SmoothDelta = Vector2.Lerp(m_SmoothDelta, target, dt * smoothing);
            }
            else
            {
                m_SmoothDelta = target;
            }

            if (!m_Dragging && m_SmoothDelta.sqrMagnitude < 0.0001f)
            {
                m_SmoothDelta = Vector2.zero;
            }

            // Output to virtual axis (velocity = delta / dt, so (velocity * dt) in PlayerController cancels dt)
            float outX = (m_SmoothDelta.x / dt) * SENSITIVITY_SCALE * Xsensitivity;
            float outY = (m_SmoothDelta.y / dt) * SENSITIVITY_SCALE * Ysensitivity * (invertY ? -1f : 1f);

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
            m_AccumulatedDelta = Vector2.zero;
            m_SmoothDelta = Vector2.zero;
        }
    }
}
