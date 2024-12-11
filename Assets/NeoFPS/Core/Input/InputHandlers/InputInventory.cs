using UnityEngine;
using NeoFPS.Constants;

namespace NeoFPS
{
    [HelpURL("https://docs.neofps.com/manual/inputref-mb-inputinventory.html")]
    [RequireComponent(typeof(ICharacter))]
    public class InputInventory : CharacterInputBase
    {
        [Header("Input Properties")]

        [SerializeField, Range(0.1f, 1f), Tooltip("The delay between repeating input when holding the next or previous weapon buttons.")]
        private float m_RepeatDelay = 0.25f;

        [SerializeField, Tooltip("Should the mouse scroll wheel switch between weapons.")]
        private bool m_ScrollSelect = true;

        [SerializeField, Range(0.01f, 1f), Tooltip("The delay between repeating input when rolling the mouse scroll wheel.")]
        private float m_ScrollDelay = 0.1f;

        [Header("Inputs")]

        [SerializeField, Tooltip("The input buttons corresponding to each slot. If you have quick-melee / thrown inputs then you can map them to specific slots here.")]
        private FpsInputButton[] m_SlotButtons = {
            FpsInputButton.Quickslot1,
            FpsInputButton.Quickslot2,
            FpsInputButton.Quickslot3,
            FpsInputButton.Quickslot4,
            FpsInputButton.Quickslot5,
            FpsInputButton.Quickslot6,
            FpsInputButton.Quickslot7,
            FpsInputButton.Quickslot8,
            FpsInputButton.Quickslot9,
            FpsInputButton.Quickslot10,
        };

        private float m_WeaponCycleTimeout = 0f;
        private float m_ScrollTimer = 0f;

        protected override void UpdateInput()
        {
            if (!allowWeaponInput)
                return;

            // Switch weapons			
            if (m_Character.quickSlots != null)
            {
                int weaponCycle = 0;
                if (m_WeaponCycleTimeout == 0f)
                {
                    // Get cycle direction
                    if (GetButton(FpsInputButton.PrevWeapon))
                        weaponCycle -= 1;
                    if (GetButton(FpsInputButton.NextWeapon))
                        weaponCycle += 1;

                    // Cycle weapon
                    switch (weaponCycle)
                    {
                        case 1:
                            {
                                m_Character.quickSlots.SelectNextSlot();
                                m_WeaponCycleTimeout = m_RepeatDelay;
                                break;
                            }
                        case -1:
                            {
                                m_Character.quickSlots.SelectPreviousSlot();
                                m_WeaponCycleTimeout = m_RepeatDelay;
                                break;
                            }
                    }
                }
                else
                {
                    // Get cycle direction
                    if (GetButtonUp(FpsInputButton.PrevWeapon) || GetButtonUp(FpsInputButton.NextWeapon))
                        m_WeaponCycleTimeout = 0f;
                    else
                    {
                        // Reduce repeat timeout
                        m_WeaponCycleTimeout -= Time.deltaTime;
                        if (m_WeaponCycleTimeout < 0f)
                            m_WeaponCycleTimeout = 0f;
                    }
                }

                // Quick-switch
                if (GetButtonDown(FpsInputButton.SwitchWeapon))
                    m_Character.quickSlots.SwitchSelection();

                // Quickslots
                for (int i = 0; i < m_SlotButtons.Length; ++i)
                {
                    if (m_SlotButtons[i] != FpsInputButton.None && GetButtonDown(m_SlotButtons[i]))
                        m_Character.quickSlots.SelectSlot(i);
                }

                // Holster
                if (GetButtonDown(FpsInputButton.Holster))
                    m_Character.quickSlots.SelectSlot(-1);

                // Drop selected weapon
                if (GetButtonDown(FpsInputButton.DropWeapon))
                    m_Character.quickSlots.DropSelected();

                // Mouse scroll
                if (m_ScrollSelect)
                {
                    if (m_ScrollTimer == 0f)
                    {
                        float scroll = GetAxis(FpsInputAxis.MouseScroll);
                        if (scroll > 0.075f)
                        {
                            m_Character.quickSlots.SelectNextSlot();
                            m_ScrollTimer = m_ScrollDelay;
                        }
                        if (scroll < -0.075f)
                        {
                            m_Character.quickSlots.SelectPreviousSlot();
                            m_ScrollTimer = m_ScrollDelay;
                        }
                    }
                    else
                    {
                        m_ScrollTimer -= Time.unscaledDeltaTime;
                        if (m_ScrollTimer < 0f)
                            m_ScrollTimer = 0f;
                    }
                }
            }
        }
    }
}