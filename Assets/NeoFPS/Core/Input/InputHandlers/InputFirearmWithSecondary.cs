using NeoFPS.CharacterMotion;
using NeoFPS.CharacterMotion.Parameters;
using NeoFPS.ModularFirearms;
using NeoFPS.WieldableTools;
using System;
using UnityEngine;
using UnityEngine.Events;

namespace NeoFPS
{
    [HelpURL("https://docs.neofps.com/manual/inputref-mb-inputfirearmwithsecondary.html")]
    public class InputFirearmWithSecondary : FpsInput
    {
        [Header("Primary")]
        [SerializeField, Tooltip("The primary firearm.")]
        private ModularFirearm m_Firearm = null;

        [Header("Secondary")]
        [SerializeField, Tooltip("The weapon that is triggered via the secondary fire.")]
        private WeaponInfo m_Secondary = new WeaponInfo { type = WeaponType.Firearm };

        private enum WeaponType
        {
            Firearm,
            WieldableTool,
            Melee,
            Thrown
        }

        [Serializable]
        private struct WeaponInfo
        {
            public WeaponType type;
            public ModularFirearm firearm;
            public WieldableTool wieldableTool;
            public BaseMeleeWeapon melee;
            public BaseThrownWeapon thrown;
        }

        private bool m_IsPlayer = false;
        private bool m_IsAlive = false;
        private ICharacter m_Character = null;

        public override FpsInputContext inputContext
        {
            get { return FpsInputContext.Character; }
        }

        #region SETUP

        protected override void OnEnable()
        {
            base.OnEnable();
            OnWielderChanged(GetComponentInParent<ICharacter>());
            FpsSettings.keyBindings.onRebind += OnRebindKeys;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            OnWielderChanged(null);
            FpsSettings.keyBindings.onRebind -= OnRebindKeys;
        }

        void OnWielderChanged(ICharacter character)
        {
            if (m_Character != null)
            {
                m_Character.onControllerChanged -= OnControllerChanged;
                m_Character.onIsAliveChanged -= OnIsAliveChanged;
            }

            m_Character = character;

            if (m_Character != null)
            {
                m_Character.onControllerChanged += OnControllerChanged;
                m_Character.onIsAliveChanged += OnIsAliveChanged;
                OnControllerChanged(m_Character, m_Character.controller);
                OnIsAliveChanged(m_Character, m_Character.isAlive);
            }
            else
            {
                m_IsPlayer = false;
                m_IsAlive = false;
            }
        }

        void OnControllerChanged(ICharacter character, IController controller)
        {
            m_IsPlayer = (controller != null && controller.isPlayer);
            if (m_IsPlayer && m_IsAlive)
                PushContext();
            else
                PopContext();
        }

        void OnIsAliveChanged(ICharacter character, bool alive)
        {
            m_IsAlive = alive;
            if (m_IsPlayer && m_IsAlive)
                PushContext();
            else
            {
                PopContext();
                ResetWeapons();
            }
        }

        void OnRebindKeys(FpsInputButton button, bool primary, KeyCode to)
        {
            if (button == FpsInputButton.AimToggle || button == FpsInputButton.Aim)
            {
                ResetWeapons();
            }
        }

        protected override void OnLoseFocus()
        {
            ResetWeapons();
        }

        void ResetWeapons()
        {
            m_Firearm.trigger.Release();

            switch (m_Secondary.type)
            {
                case WeaponType.Firearm:
                    if (m_Secondary.firearm != null)
                    {
                        m_Secondary.firearm.trigger.Release();
                    }
                    break;
                case WeaponType.WieldableTool:
                    if (m_Secondary.wieldableTool != null)
                    {
                        m_Secondary.wieldableTool.PrimaryRelease();
                        m_Secondary.wieldableTool.SecondaryRelease();
                    }
                    break;
                case WeaponType.Melee:
                    if (m_Secondary.melee != null)
                    {
                        m_Secondary.melee.PrimaryRelease();
                        m_Secondary.melee.SecondaryRelease();
                    }
                    break;
            }
        }

        #endregion

        #region INPUT

        void ApplyFirearmInput(ModularFirearm firearm, FpsInputButton fireButton, bool reload, bool switchModes)
        {
            // Check for fire press
            if (GetButtonDown(fireButton))
            {
                if (firearm.trigger.blocked && firearm.reloader.interruptable)
                    firearm.reloader.Interrupt();
                else
                    firearm.trigger.Press();
            }

            // Check for fire release
            if (GetButtonUp(fireButton))
                firearm.trigger.Release();

            // Reload
            if (reload)
                firearm.Reload();

            // Switch modes
            if (switchModes)
                firearm.SwitchMode();
        }

        void ApplyWieldableToolInput(WieldableTool tool, FpsInputButton fireButton, bool reload)
        {
            if (GetButtonDown(fireButton))
                tool.PrimaryPress();
            if (GetButtonUp(fireButton))
                tool.PrimaryRelease();

            if (reload)
                tool.Interrupt();
        }

        void ApplyMeleeInput(BaseMeleeWeapon melee, FpsInputButton fireButton)
        {
            if (GetButtonDown(fireButton))
                melee.PrimaryPress();
            if (GetButtonUp(fireButton))
                melee.PrimaryRelease();
        }

        void ApplyThrownInput(BaseThrownWeapon thrownWeapon, FpsInputButton fireButton)
        {
            if (GetButtonDown(fireButton))
                thrownWeapon.ThrowHeavy();
        }

        protected override void UpdateInput()
        {
            if (m_Character != null && !m_Character.allowWeaponInput)
                return;

            bool reload = GetButtonDown(FpsInputButton.Reload);
            bool switchModes = GetButtonDown(FpsInputButton.SwitchWeaponModes);

            ApplyFirearmInput(m_Firearm, FpsInputButton.PrimaryFire, reload, switchModes);
            ApplyInput(m_Secondary, FpsInputButton.SecondaryFire, reload, switchModes);

            // Flashlight
            if (GetButtonDown(FpsInputButton.Flashlight))
            {
                var flashlight = GetComponentInChildren<IWieldableFlashlight>(false);
                if (flashlight != null)
                    flashlight.Toggle();
            }
        }

        void ApplyInput(WeaponInfo weaponInfo, FpsInputButton fireButton, bool reload, bool switchModes)
        {
            switch (weaponInfo.type)
            {
                case WeaponType.Firearm:
                    if (weaponInfo.firearm != null && weaponInfo.firearm.enabled)
                        ApplyFirearmInput(weaponInfo.firearm, fireButton, reload, switchModes);
                    break;
                case WeaponType.WieldableTool:
                    if (weaponInfo.wieldableTool != null && weaponInfo.wieldableTool.enabled)
                        ApplyWieldableToolInput(weaponInfo.wieldableTool, fireButton, reload);
                    break;
                case WeaponType.Melee:
                    if (weaponInfo.melee != null && weaponInfo.melee.enabled)
                        ApplyMeleeInput(weaponInfo.melee, fireButton);
                    break;
                case WeaponType.Thrown:
                    if (weaponInfo.thrown != null && weaponInfo.thrown.enabled)
                        ApplyThrownInput(weaponInfo.thrown, fireButton);
                    break;
            }
        }

        #endregion

        #region BLOCKING

        private bool m_PrimaryBlocking = false;
        private bool m_PrimaryReloading = false;
        private bool m_SecondaryBlocking = false;
        private bool m_SecondaryReloading = false;

        protected override void Start()
        {
            base.Start();

            m_Firearm.onBlockedChanged += OnPrimaryBlockedChanged;
            m_Firearm.reloader.onReloadStart += OnPrimaryReloadStart;
            m_Firearm.reloader.onReloadComplete += OnPrimaryReloadComplete;
            AttachBlockChangeHandler(m_Secondary, OnSecondaryBlockedChanged, OnSecondaryReloadStart, OnSecondaryReloadComplete);
        }

        private void AttachBlockChangeHandler(WeaponInfo weaponInfo, UnityAction<bool> blockedHandler, UnityAction<IModularFirearm> reloadStartHandler, UnityAction<IModularFirearm> reloadCompleteHandler)
        {
            switch (weaponInfo.type)
            {
                case WeaponType.Firearm:
                    if (weaponInfo.firearm != null)
                    {
                        weaponInfo.firearm.onBlockedChanged += blockedHandler;
                        weaponInfo.firearm.reloader.onReloadStart += reloadStartHandler;
                        weaponInfo.firearm.reloader.onReloadComplete += reloadCompleteHandler; 
                    }
                    break;
                case WeaponType.WieldableTool:
                    if (weaponInfo.wieldableTool != null)
                        weaponInfo.wieldableTool.onBlockedChanged += blockedHandler;
                    break;
                case WeaponType.Melee:
                    if (weaponInfo.melee != null)
                        weaponInfo.melee.onBlockedChanged += blockedHandler;
                    break;
                case WeaponType.Thrown:
                    if (weaponInfo.thrown != null)
                        weaponInfo.thrown.onBlockedChanged += blockedHandler;
                    break;
            }
        }

        void SetWeaponBlockedState(WeaponInfo weaponInfo, bool block)
        {
            switch (weaponInfo.type)
            {
                case WeaponType.Firearm:
                    if (block)
                        weaponInfo.firearm?.AddBlocker(this);
                    else
                        weaponInfo.firearm?.RemoveBlocker(this);
                    break;
                case WeaponType.WieldableTool:
                        if (block)
                            weaponInfo.wieldableTool?.AddBlocker(this);
                        else
                            weaponInfo.wieldableTool?.RemoveBlocker(this);
                    break;
                case WeaponType.Melee:
                    if (block)
                        weaponInfo.melee?.AddBlocker(this);
                    else
                        weaponInfo.melee?.RemoveBlocker(this);
                    break;
                case WeaponType.Thrown:
                    if (block)
                        weaponInfo.thrown?.AddBlocker(this);
                    else
                        weaponInfo.thrown?.RemoveBlocker(this);
                    break;
            }
        }

        private void OnPrimaryBlockedChanged(bool blocked)
        {
            if (m_SecondaryBlocking || m_SecondaryReloading)
                return;

            if (blocked)
            {
                m_PrimaryBlocking = true;
                if (!m_PrimaryReloading)
                    SetWeaponBlockedState(m_Secondary, true);
            }
            else
            {
                if (!m_PrimaryReloading)
                    SetWeaponBlockedState(m_Secondary, false);
                m_PrimaryBlocking = false;
            }
        }

        private void OnSecondaryBlockedChanged(bool blocked)
        {
            if (m_PrimaryBlocking || m_PrimaryReloading)
                return;

            if (blocked)
            {
                m_SecondaryBlocking = true;
                if (!m_SecondaryReloading)
                    m_Firearm?.AddBlocker(this);
            }
            else
            {
                if (!m_SecondaryReloading)
                    m_Firearm?.RemoveBlocker(this);
                m_SecondaryBlocking = false;
            }
        }

        private void OnPrimaryReloadStart(IModularFirearm arg)
        {
            if (m_SecondaryBlocking || m_SecondaryReloading)
                return;

            m_PrimaryReloading = true;
            if (!m_PrimaryBlocking)
                SetWeaponBlockedState(m_Secondary, true);
        }

        private void OnPrimaryReloadComplete(IModularFirearm arg)
        {
            if (m_SecondaryBlocking || m_SecondaryReloading)
                return;

            if (!m_PrimaryBlocking)
                SetWeaponBlockedState(m_Secondary, false);
            m_PrimaryReloading = false;
        }

        private void OnSecondaryReloadStart(IModularFirearm arg)
        {
            if (m_PrimaryBlocking || m_PrimaryReloading)
                return;

            m_SecondaryReloading = true;
            if (!m_SecondaryBlocking)
                m_Firearm?.AddBlocker(this);
        }

        private void OnSecondaryReloadComplete(IModularFirearm arg)
        {
            if (m_PrimaryBlocking || m_PrimaryReloading)
                return;

            if (!m_SecondaryBlocking)
                m_Firearm?.RemoveBlocker(this);
            m_SecondaryReloading = false;
        }

        #endregion
    }
}