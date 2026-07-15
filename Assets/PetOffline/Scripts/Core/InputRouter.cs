using UnityEngine;
using UnityEngine.InputSystem;

namespace PetOffline.Core
{
    public sealed class InputRouter : MonoBehaviour
    {
        [SerializeField, RequiredReference] InputActionAsset actions;

        public InputActionAsset Actions => actions;
        public InputActionMap Gameplay => actions?.FindActionMap("Gameplay");
        public InputActionMap UI => actions?.FindActionMap("UI");

        public void Configure(InputActionAsset value) => actions = value;

        public void SetGameplayMode(bool enabled)
        {
            if (actions == null)
                return;

            if (enabled)
            {
                UI?.Disable();
                Gameplay?.Enable();
            }
            else
            {
                Gameplay?.Disable();
                UI?.Enable();
            }
        }

        void OnDisable() => actions?.Disable();
    }
}
