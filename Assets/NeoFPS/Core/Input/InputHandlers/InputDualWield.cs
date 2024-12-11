using NeoFPS.CharacterMotion;
using NeoFPS.CharacterMotion.Parameters;
using NeoFPS.ModularFirearms;
using NeoFPS.WieldableTools;
using System;
using UnityEngine;
using UnityEngine.Events;

namespace NeoFPS
{
    [HelpURL("https://docs.neofps.com/manual/inputref-mb-inputdualwield.html")]
    public class InputDualWield : FpsInput
    {
        [Header ("Left")]
        [SerializeField, Tooltip("The weapon for the left hand side")]
        private WeaponInfo m_Left = new WeaponInfo { type = WeaponType.Firearm };

        [Header("Right")]
        [SerializeField, Tooltip("The weapon for the right hand side")]
        private WeaponInfo m_Right = new WeaponInfo { type = WeaponType.Firearm };

        [Header("Dual Wield Style")]
        [SerializeField, Tooltip("How should the dual wielding work. PrimarySecondary fires one weapon with primary fire and the other with secondary. TogetherPlusAim fires both weapons at the same time and can be aimed (see the docs for tips).")]
        private DualWieldStyle m_Style = DualWieldStyle.PrimarySecondary;
        [SerializeField, Tooltip("The property key for the character motion graph (switch)")]
        private string m_AimingKey = "aiming";
        [SerializeField, Tooltip("Either weapon blocks the other. For example, when one weapon reloads, the other won't be able to do anything.")]
        private bool m_StrictBlocking = false;

        private enum WeaponType
        {
            Firearm,
            WieldableTool,
            Melee,
            Thrown
        }

        private enum DualWieldStyle
        {
            PrimarySecondary,
            PrimarySecondaryFlipped,
            TogetherPlusAim
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
        protected int m_AimingKeyHash = -1;
        protected SwitchParameter m_AimProperty = null;

        public override FpsInputContext inputContext
        {
            get { return FpsInputContext.Character; }
        }

        #region SETUP

        protected override void OnAwake()
        {
            m_AimingKeyHash = Animator.StringToHash(m_AimingKey);
        }

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
                m_AimProperty = null;
            }

            m_Character = character;

            if (m_Character != null)
            {
                if (m_Style == DualWieldStyle.TogetherPlusAim && m_Character.motionController != null)
                {
                    MotionGraphContainer motionGraph = m_Character.motionController.motionGraph;
                    m_AimProperty = motionGraph.GetSwitchProperty(m_AimingKeyHash);
                }

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

                ResetWeapon(m_Left);
                ResetWeapon(m_Right);

                if (m_AimProperty != null)
                    m_AimProperty.on = false;
            }
        }

        void OnRebindKeys(FpsInputButton button, bool primary, KeyCode to)
        {
            if (button == FpsInputButton.AimToggle || button == FpsInputButton.Aim)
            {
                ResetWeapon(m_Left);
                ResetWeapon(m_Right);
            }
        }

        protected override void OnLoseFocus()
        {
            ResetWeapon(m_Left);
            ResetWeapon(m_Right);
        }

        void ResetWeapon(WeaponInfo weaponInfo)
        {
            switch(weaponInfo.type)
            {
                case WeaponType.Firearm:
                    if (weaponInfo.firearm != null)
                    {
                        weaponInfo.firearm.trigger.Release();
                        weaponInfo.firearm.aimToggleHold.on = false;
                    }
                    break;
                case WeaponType.WieldableTool:
                    if (weaponInfo.wieldableTool != null)
                    {
                        weaponInfo.wieldableTool.PrimaryRelease();
                        weaponInfo.wieldableTool.SecondaryRelease();
                    }
                    break;
                case WeaponType.Melee:
                    if (weaponInfo.melee != null)
                    {
                        weaponInfo.melee.PrimaryRelease();
                        weaponInfo.melee.SecondaryRelease();
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

            // Aim
            if (m_Style == DualWieldStyle.TogetherPlusAim)
            {
                firearm.aimToggleHold.SetInput(
                    GetButtonDown(FpsInputButton.AimToggle),
                    GetButton(FpsInputButton.Aim)
                    );
                if (m_AimProperty != null)
                    m_AimProperty.on = firearm.aimToggleHold.on;
            }
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

            ApplyInput(m_Left, true, reload, switchModes);
            ApplyInput(m_Right, false, reload, switchModes);

            // Flashlight
            if (GetButtonDown(FpsInputButton.Flashlight))
            {
                var flashlight = GetComponentInChildren<IWieldableFlashlight>(false);
                if (flashlight != null)
                    flashlight.Toggle();
            }
        }

        void ApplyInput (WeaponInfo weaponInfo, bool left, bool reload, bool switchModes)
        {
            // Get the correct fire button for this style
            FpsInputButton fireButton = FpsInputButton.PrimaryFire;
            switch (m_Style)
            {
                case DualWieldStyle.PrimarySecondary:
                    if (!left)
                        fireButton = FpsInputButton.SecondaryFire;
                    break;
                case DualWieldStyle.PrimarySecondaryFlipped:
                    if (left)
                        fireButton = FpsInputButton.SecondaryFire;
                    break;
                case DualWieldStyle.TogetherPlusAim:
                    fireButton = FpsInputButton.PrimaryFire;
                    break;
            }

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

        private bool m_LeftBlocking = false;
        private bool m_LeftReloading = false;
        private bool m_RightBlocking = false;
        private bool m_RightReloading = false;

        protected override void Start()
        {
            base.Start();

            if (m_StrictBlocking)
            {
                AttachBlockChangeHandler(m_Left, OnLeftBlockedChanged, OnLeftReloadStart, OnLeftReloadComplete);
                AttachBlockChangeHandler(m_Right, OnRightBlockedChanged, OnRightReloadStart, OnRightReloadComplete);
            }
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

        private void OnLeftBlockedChanged(bool blocked)
        {
            if (m_RightBlocking || m_RightReloading)
                return;

            if (blocked)
            {
                m_LeftBlocking = true;
                if (!m_LeftReloading)
                    SetWeaponBlockedState(m_Right, true);
            }
            else
            {
                if (!m_LeftReloading)
                    SetWeaponBlockedState(m_Right, false);
                m_LeftBlocking = false;
            }
        }

        private void OnRightBlockedChanged(bool blocked)
        {
            if (m_LeftBlocking || m_LeftReloading)
                return;

            if (blocked)
            {
                m_RightBlocking = true;
                if (!m_RightReloading)
                    SetWeaponBlockedState(m_Left, true);
            }
            else
            {
                if (!m_RightReloading)
                    SetWeaponBlockedState(m_Left, false);
                m_RightBlocking = false;
            }
        }

        private void OnLeftReloadStart(IModularFirearm arg)
        {
            if (m_RightBlocking || m_RightReloading)
                return;

            m_LeftReloading = true;
            if (!m_LeftBlocking)
                SetWeaponBlockedState(m_Right, true);
        }

        private void OnLeftReloadComplete(IModularFirearm arg)
        {
            if (m_RightBlocking || m_RightReloading)
                return;

            if (!m_LeftBlocking)
                SetWeaponBlockedState(m_Right, false);
            m_LeftReloading = false;
        }

        private void OnRightReloadStart(IModularFirearm arg)
        {
            if (m_LeftBlocking || m_LeftReloading)
                return;

            m_RightReloading = true;
            if (!m_RightBlocking)
                SetWeaponBlockedState(m_Left, true);
        }

        private void OnRightReloadComplete(IModularFirearm arg)
        {
            if (m_LeftBlocking || m_LeftReloading)
                return;

            if (!m_RightBlocking)
                SetWeaponBlockedState(m_Left, false);
            m_RightReloading = false;
        }

        #endregion
    }
}