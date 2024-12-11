using System;
using System.Collections.Generic;
using UnityEngine;
using NeoFPS.Constants;

namespace NeoFPS
{
    [HelpURL("https://docs.neofps.com/manual/inventoryref-so-fpsinventorykeydbtable.html")]
    public class FpsInventoryKeyDbTable : FpsInventoryDbTableBase
    {
        private FpsInventoryDatabaseEntry[] m_Entries = { };

        public override string tableName
        {
            get { return "Scripted Constants"; }
        }

        public override FpsInventoryDatabaseEntry[] entries
        {
            get
            {
                RefreshEntries();
                return m_Entries;
            }
        }

        public override int count
        {
            get { return FpsInventoryKey.count - 1; }
        }

        protected void Awake()
        {
            RefreshEntries();
            hideFlags = HideFlags.HideInHierarchy;
        }

        void RefreshEntries()
        {
            int targetCount = count;
            if (m_Entries == null || m_Entries.Length != targetCount)
            {
                m_Entries = new FpsInventoryDatabaseEntry[targetCount];
                for (int i = 0; i < targetCount; ++i)
                {
                    int key = i + 1;
                    string n = FpsInventoryKey.names[key];
                    m_Entries[i] = new FpsInventoryDatabaseEntry(key, n);
                }
            }
        }
    }
}