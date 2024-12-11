using System;
using UnityEngine;

namespace NeoFPS
{
    public interface IMonoBehaviour
    {
        bool enabled { get; set; }
        string name { get; }
        GameObject gameObject { get; }
        Transform transform { get; }
        T GetComponent<T>();
        T GetComponentInChildren<T>();
        T GetComponentInParent<T>();
        T[] GetComponents<T>();
        T[] GetComponentsInChildren<T>(bool includeInactive = false);
        T[] GetComponentsInParent<T>(bool includeInactive = false);
        Component GetComponent(Type t);
        Component GetComponentInChildren(Type t);
        Component GetComponentInParent(Type t);
        Component[] GetComponents(Type t);
        Component[] GetComponentsInChildren(Type t, bool includeInactive = false);
        Component[] GetComponentsInParent(Type t, bool includeInactive = false);
    }
}
