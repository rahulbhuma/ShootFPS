using NeoFPS.Samples;
using System.Collections.Generic;
using UnityEngine;

namespace NeoFPS.SinglePlayer
{
    public class LoadoutBuilderTab : InstantSwitchTabBase
    {
        [SerializeField, RequiredObjectProperty, Tooltip("A prototype multi-choice UI widget that will be cloned for each of the available loadout slots")]
        private MultiInputMultiChoice m_MultiChoicePrototype = null;

        public override string tabName
        {
            get { return "Modify Loadout"; }
        }

        public ILoadoutBuilder loadoutBuilder
        {
            get;
            private set;
        }

        private List<MultiInputMultiChoice> m_SlotChoices = new List<MultiInputMultiChoice>();

        public override bool Initialise(FpsSoloGameCustomisable g)
        {
            base.Initialise(g);

            if (m_MultiChoicePrototype == null)
                return false;

            loadoutBuilder = g as ILoadoutBuilder;
            if (loadoutBuilder != null && loadoutBuilder.numLoadoutBuilderSlots > 0)
            {
                var options = new List<string>(10);

                for (int i = 0; i < loadoutBuilder.numLoadoutBuilderSlots; ++i)
                {
                    var slot = loadoutBuilder.GetLoadoutBuilderSlotInfo(i);

                    // Instantiate multi-choice
                    var slotChoice = Instantiate(m_MultiChoicePrototype, m_MultiChoicePrototype.transform.parent);

                    // Assign options
                    for (int j = 0; j < slot.numOptions; ++j)
                    {
                        var option = slot.GetOption(j);
                        if (option != null)
                            options.Add(NeoFpsInventoryDatabase.GetEntryName(option.itemIdentifier));
                        else
                            options.Add("Empty");
                    }
                    slotChoice.options = options.ToArray();
                    options.Clear();

                    // Initialise slot choice
                    slotChoice.onIndexChanged.AddListener((int index) => { slot.currentOption = index; });
                    slotChoice.label = slot.displayName;
                    slotChoice.index = slot.currentOption;

                    m_SlotChoices.Add(slotChoice);
                }

                m_MultiChoicePrototype.gameObject.SetActive(false);

                return true;
            }
            else
                return false;
        }
    }
}
