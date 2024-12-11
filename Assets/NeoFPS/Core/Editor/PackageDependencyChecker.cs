#if UNITY_EDITOR

using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using System.Collections.Generic;
using UnityEngine.Rendering;

namespace NeoFPSEditor
{
    public class PackageDependencyChecker
    {
        private static Action m_OnComplete = null;
        private static AddRequest m_AddRequest = null;

        static void WaitForInstall()
        {
            // Wait
            if (m_AddRequest.Status == StatusCode.InProgress)
                return;

            // Clean up
            m_AddRequest = null;

            // Unsubscribe from application event
            EditorApplication.update -= WaitForInstall;

            // Fire completed callback
            m_OnComplete?.Invoke();
            m_OnComplete = null;
        }

        public static void InstallPackage(string packageName, string version, Action onComplete)
        {
            // Store completion callback
            m_OnComplete = onComplete;

            // Package not installed. Send add request
            if (string.IsNullOrWhiteSpace(version))
                m_AddRequest = Client.Add(packageName);
            else
                m_AddRequest = Client.Add(packageName + "@" + version);

            // Subscribe to install event
            EditorApplication.update += WaitForInstall;
        }

        [Serializable]
        private struct PackageInfo
        {
            public string name;
            public string version;
        }

        public static bool IsPackageInstalled(string packageName, string version)
        {
            var path = "Packages/" + packageName + "/package.json";

            var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);

            // Check if package is installed at all
            if (asset == null)
                return false;

            // Check if specific version required
            if (string.IsNullOrWhiteSpace(version))
                return true;

            // Check if specific version is installed
            var info = JsonUtility.FromJson<PackageInfo>(asset.text);

            if (info.name != packageName) return false;

            int dashIndex = info.version.IndexOf('-');

            if (dashIndex > -1)
            {
                info.version = info.version.Substring(0, dashIndex);
            }

            Version parsedVersion;
            Version targetVersion;

            try
            {
                parsedVersion = new Version(info.version);
                targetVersion = new Version(version);
            }
            catch
            {
                Debug.LogError(string.Format("Failed to parse package version strings (info:`{0}`, package:`{1}`) for package: {2}", info.version, version, packageName));
                return false;
            }

            return parsedVersion >= targetVersion;
        }
         
    }
}

#endif