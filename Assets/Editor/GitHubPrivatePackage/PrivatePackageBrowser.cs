using UnityEngine;
using UnityEditor;
using System.IO;
using System.Net.Http;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor.Compilation;

// -------------------------------
// PRIVATE PACKAGE BROWSER
// -------------------------------
public class PrivatePackageBrowser : EditorWindow
{
    private Vector2 scrollPos;
    private List<PackageInfo> packageList = new();
    private Dictionary<string, string> installedPackages = new();   // name -> version
    private Dictionary<string, List<string>> dependencyMap = new(); // package -> dependencies

    private string authToken;
    private string apiUser;
    private string scope;
    private string registryUrl;

    [MenuItem("Window/GitHub Private Packages/Package Manager")]
    public static void ShowWindow()
    {
        var wnd = GetWindow<PrivatePackageBrowser>();
        wnd.minSize = wnd.maxSize = new Vector2(800, 400);
        
        Texture2D icon = EditorGUIUtility.IconContent("Package Manager").image as Texture2D;
        wnd.titleContent = new GUIContent("GitHub Private Packages", icon);
    }

    private async void OnEnable()
    {
        LoadConfig();
        LoadInstalledPackages();
        LoadDependencyMap();

        if (IsConfigured())
            await LoadPackages();
    }

    private void LoadConfig()
    {
        authToken = EditorPrefs.GetString("PPB_AuthToken", "");
        apiUser = EditorPrefs.GetString("PPB_ApiUser", "");
        scope = EditorPrefs.GetString("PPB_Scope", "com.vns");
        registryUrl = EditorPrefs.GetString("PPB_RegistryUrl", "https://npm.pkg.github.com");
    }

    // ----------------------------------------
    // Manifest.json'daki paketleri ve sürümlerini al
    // ----------------------------------------
    private void LoadInstalledPackages()
    {
        installedPackages.Clear();
        string manifestPath = Path.Combine(Application.dataPath, "../Packages/manifest.json");
        if (!File.Exists(manifestPath)) return;

        string json = File.ReadAllText(manifestPath);
        var matches = Regex.Matches(json, "\"([^\"]+)\"\\s*:\\s*\"([^\"]+)\"");

        foreach (Match m in matches)
            installedPackages[m.Groups[1].Value] = m.Groups[2].Value;
    }

    // ----------------------------------------
    // PackageCache'teki paketlerin dependency haritasını hazırla
    // ----------------------------------------
    private void LoadDependencyMap()
    {
        dependencyMap.Clear();
        string cachePath = Path.Combine(Application.dataPath, "../Library/PackageCache");
        if (!Directory.Exists(cachePath)) return;

        foreach (var dir in Directory.GetDirectories(cachePath))
        {
            string packageJson = Path.Combine(dir, "package.json");
            if (!File.Exists(packageJson)) continue;

            string json = File.ReadAllText(packageJson);
            Match nameMatch = Regex.Match(json, "\"name\"\\s*:\\s*\"([^\"]+)\"");
            if (!nameMatch.Success) continue;
            string pkgName = nameMatch.Groups[1].Value;

            List<string> deps = new();
            MatchCollection depMatches = Regex.Matches(json, "\"([^\"]+)\"\\s*:\\s*\"([^\"]+)\"");
            foreach (Match m in depMatches)
            {
                string depName = m.Groups[1].Value;
                if (depName != pkgName) deps.Add(depName);
            }

            dependencyMap[pkgName] = deps;
        }
    }

    private bool IsConfigured()
    {
        if (string.IsNullOrEmpty(authToken) || string.IsNullOrEmpty(apiUser))
        {
            EditorGUILayout.HelpBox("Lütfen önce Config penceresinden PAT ve GitHub kullanıcı adını girin.", MessageType.Warning);
            return false;
        }
        return true;
    }

    private async System.Threading.Tasks.Task LoadPackages()
    {
        packageList.Clear();

        using (var client = new HttpClient())
        {
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {authToken}");
            client.DefaultRequestHeaders.Add("User-Agent", "UnityEditor");
            client.DefaultRequestHeaders.Add("Accept", "application/vnd.github+json");

            try
            {
                string url = $"https://api.github.com/users/{apiUser}/packages?package_type=npm";
                string response = await client.GetStringAsync(url);
                string wrapped = "{\"items\":" + response + "}";
                PackageInfoArray data = JsonUtility.FromJson<PackageInfoArray>(wrapped);

                foreach (var pkg in data.items)
                {
                    if (!pkg.name.StartsWith(scope)) continue; // sadece scope paketleri

                    string displayName = pkg.name.Contains("/") ? pkg.name.Split('/')[1] : pkg.name;
                    string verUrl = $"https://api.github.com/users/{apiUser}/packages/npm/{pkg.name}/versions";
                    string verResp = await client.GetStringAsync(verUrl);
                    string verWrapped = "{\"items\":" + verResp + "}";
                    VersionInfoArray verData = JsonUtility.FromJson<VersionInfoArray>(verWrapped);
                    string latestVersion = verData.items.Length > 0 ? verData.items[0].name : "unknown";

                    installedPackages.TryGetValue(pkg.name, out string installedVer);

                    // Dependency kontrolü: başka paketin bağımlılığı mı
                    bool isDep = false;
                    foreach (var deps in dependencyMap.Values)
                    {
                        if (deps.Contains(pkg.name))
                        {
                            isDep = true;
                            break;
                        }
                    }

                    string status;
                    if (isDep)
                        status = "Installed As Dependency";
                    else if (!string.IsNullOrEmpty(installedVer))
                        status = installedVer == latestVersion ? "Installed" : "Outdated";
                    else
                        status = "Not Installed";

                    packageList.Add(new PackageInfo
                    {
                        name = pkg.name,
                        displayName = displayName,
                        latestVersion = latestVersion,
                        installedVersion = installedVer,
                        status = status,
                        isDependency = isDep
                    });
                }
            }
            catch(System.Exception ex)
            {
                Debug.LogError("Paketleri çekerken hata: " + ex.Message);
            }
        }
    }

   private void OnGUI()
{
    GUILayout.Space(10);

    if (!IsConfigured()) return;

    if (GUILayout.Button("Reload Packages", GUILayout.Height(25)))
        _ = LoadPackages();

    GUILayout.Space(10);

    float totalWidth = position.width - 40; // sağ & sol margin için
    float colName = totalWidth * 0.30f;
    float colLatest = totalWidth * 0.15f;
    float colInstalled = totalWidth * 0.15f;
    float colStatus = totalWidth * 0.20f;
    float colAction = totalWidth * 0.20f;

    // Tablo başlığı
    EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
    GUILayout.Label("Package Name", GUILayout.Width(colName));
    GUILayout.Label("Latest Version", GUILayout.Width(colLatest));
    GUILayout.Label("Installed Version", GUILayout.Width(colInstalled));
    GUILayout.Label("Status", GUILayout.Width(colStatus));
    GUILayout.Label("Action", GUILayout.Width(colAction));
    EditorGUILayout.EndHorizontal();

    scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

    foreach (var pkg in packageList)
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

        GUILayout.Label(pkg.displayName, GUILayout.Width(colName));
        GUILayout.Label(pkg.latestVersion, GUILayout.Width(colLatest));
        GUILayout.Label(string.IsNullOrEmpty(pkg.installedVersion) ? "-" : pkg.installedVersion, GUILayout.Width(colInstalled));
        GUILayout.Label(pkg.status, GUILayout.Width(colStatus));

        if (pkg.status == "Not Installed")
        {
            if (GUILayout.Button("Install", GUILayout.Width(colAction - 10)))
                AddToManifest(pkg.name, pkg.latestVersion);
        }
        else if (pkg.status == "Outdated")
        {
            if (GUILayout.Button("Update", GUILayout.Width(colAction - 10)))
                UpdatePackage(pkg.name, pkg.latestVersion);
        }
        else if (pkg.status == "Installed")
        {
            if (GUILayout.Button("Remove", GUILayout.Width(colAction - 10)))
                RemoveFromManifest(pkg.name);
        }
        else if (pkg.status == "Installed As Dependency")
        {
            GUILayout.Label("-", GUILayout.Width(colAction));
        }
        else
        {
            GUILayout.Label("-", GUILayout.Width(colAction));
        }

        EditorGUILayout.EndHorizontal();
    }

    EditorGUILayout.EndScrollView();
}



    // -------------------------------
    // Manifest işlemleri
    // -------------------------------
    private void AddToManifest(string packageName, string version)
    {
        string manifestPath = Path.Combine(Application.dataPath, "../Packages/manifest.json");
        if (!File.Exists(manifestPath))
        {
            Debug.LogError("manifest.json bulunamadı!");
            return;
        }

        string json = File.ReadAllText(manifestPath);

        int depStart = json.IndexOf("\"dependencies\"");
        int braceStart = json.IndexOf("{", depStart);
        int braceEnd = json.IndexOf("}", braceStart);
        string deps = json.Substring(braceStart + 1, braceEnd - braceStart - 1);

        string pattern = $"\"{packageName}\"\\s*:\\s*\"[^\"]+\"";
        if (Regex.IsMatch(deps, pattern))
            deps = Regex.Replace(deps, pattern, $"\"{packageName}\": \"{version}\"");
        else
            deps += (string.IsNullOrEmpty(deps) ? "" : ",\n    ") + $"\"{packageName}\": \"{version}\"";

        json = json.Substring(0, braceStart + 1) + "\n    " + deps + "\n" + json.Substring(braceEnd);
        
        File.WriteAllText(manifestPath, json);
        UnityEditor.PackageManager.Client.Resolve();
        CompilationPipeline.RequestScriptCompilation();
        LoadInstalledPackages();
        LoadDependencyMap();

        Debug.Log($"{packageName}@{version} manifest.json'a eklendi!");
    }

    private void RemoveFromManifest(string packageName)
    {
        string manifestPath = Path.Combine(Application.dataPath, "../Packages/manifest.json");
        if (!File.Exists(manifestPath)) return;

        string json = File.ReadAllText(manifestPath);
        string pattern = $"[,\\s]*\"{packageName}\"\\s*:\\s*\"[^\"]+\"";
        json = Regex.Replace(json, pattern, "");

        File.WriteAllText(manifestPath, json);
        UnityEditor.PackageManager.Client.Resolve();
        CompilationPipeline.RequestScriptCompilation();
        LoadInstalledPackages();
        LoadDependencyMap();

        Debug.Log($"{packageName} manifest.json'dan kaldırıldı!");
    }

    private void UpdatePackage(string packageName, string version)
    {
        RemoveFromManifest(packageName);
        AddToManifest(packageName, version);
        Debug.Log($"{packageName} güncellendi → {version}");
    }

    // ------------------------------------
    // DATA CLASSES
    // ------------------------------------
    [System.Serializable] private class PackageInfo
    {
        public string name;
        public string displayName;
        public string latestVersion;
        public string installedVersion;
        public string status;
        public bool isDependency;
    }
    [System.Serializable] private class PackageInfoArray
    {
        public PackageInfo[] items;
    }
    [System.Serializable] private class VersionInfo
    {
        public string name;
    }
    [System.Serializable] private class VersionInfoArray
    {
        public VersionInfo[] items;
    }
}