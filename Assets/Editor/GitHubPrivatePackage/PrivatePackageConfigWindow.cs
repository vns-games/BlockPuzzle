using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
public class PrivatePackageConfigWindow : EditorWindow
{
    [MenuItem("Window/GitHub Private Packages/Config")]
    public static void ShowWindow()
    {
        var wnd = GetWindow<PrivatePackageConfigWindow>(true, "Package Config", true);
        wnd.minSize = wnd.maxSize = new Vector2(500, 110);
    }

    private string authToken;
    private string apiUser;
    private string scope;
    private string registryUrl = "https://npm.pkg.github.com";

    private void OnEnable()
    {
        authToken = EditorPrefs.GetString("PPB_AuthToken", "");
        apiUser = EditorPrefs.GetString("PPB_ApiUser", "");
        scope = EditorPrefs.GetString("PPB_Scope", "");
        registryUrl = EditorPrefs.GetString("PPB_RegistryUrl", "https://npm.pkg.github.com");
    }

    private void OnGUI()
    {
        GUILayout.Space(5);
        authToken = EditorGUILayout.TextField("Auth Token (PAT):", authToken);
        apiUser = EditorGUILayout.TextField("GitHub User/Org:", apiUser);
        scope = EditorGUILayout.TextField("Scope:", scope);
        registryUrl = EditorGUILayout.TextField("Registry URL:", registryUrl);

        if (GUILayout.Button("Save Configuration"))
        {
            EditorPrefs.SetString("PPB_AuthToken", authToken);
            EditorPrefs.SetString("PPB_ApiUser", apiUser);
            EditorPrefs.SetString("PPB_Scope", scope);
            EditorPrefs.SetString("PPB_RegistryUrl", registryUrl);
            EnsureScopeRegistry();
            Debug.Log("Private Package Config Saved!");
        }
    }

    private void EnsureScopeRegistry()
    {
        string manifestPath = Path.Combine(Application.dataPath, "../Packages/manifest.json");
        if (!File.Exists(manifestPath)) return;

        string json = File.ReadAllText(manifestPath);

        bool registryExists = false;

        if (json.Contains("\"scopedRegistries\""))
        {
            // Var olan scopedRegistries içinde kontrol
            var regex = new Regex(@"\{\s*""name""\s*:\s*""[^""]+"",\s*""url""\s*:\s*""([^""]+)"".*?""scopes""\s*:\s*\[\s*""([^""]+)""", RegexOptions.Singleline);
            var matches = regex.Matches(json);
            foreach (Match m in matches)
            {
                string url = m.Groups[1].Value;
                string scopeInJson = m.Groups[2].Value;
                if (url == registryUrl + "/@" + apiUser && scopeInJson == scope)
                {
                    registryExists = true;
                    break;
                }
            }

            if (!registryExists)
            {
                // Mevcut scopedRegistries array'ının sonuna ekle (önce virgül ekle)
                int endIndex = json.LastIndexOf("]");
                string newEntry = @",
      {
        ""name"": ""GitHub"",
        ""url"": """ + registryUrl + "/@" + apiUser + @""",
        ""scopes"": [ """ + scope + @""" ]
      }";
                json = json.Insert(endIndex, newEntry);
            }
        }
        else
        {
            // scopedRegistries yok, yeni ekle (başına virgül koyma)
            string regInsert = @"
    ""scopedRegistries"": [
      {
        ""name"": ""GitHub"",
        ""url"": """ + registryUrl + "/@" + apiUser + @""",
        ""scopes"": [ """ + scope + @""" ]
      }
    ],";
            int depIndex = json.IndexOf("\"dependencies\"");
            if (depIndex >= 0) json = json.Insert(depIndex, regInsert + "\n");
        }

        File.WriteAllText(manifestPath, json);
    }

}