
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Reflection;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ReplaceDll
{
    private static readonly string uiguid = "f70555f144d8491a825f0804e09c671c";
    private static readonly string uiguid2 = "f5f67c52d1564df4a8936ccd202a3bd8";

    [MenuItem("Xiyou/Replace UGUI DLL")]
    static void Replace()
    {
        List<UnityReference> r = new List<UnityReference>();
        foreach (var type in Assembly.GetAssembly(typeof(Text)).GetTypes())
        {
            if (type.IsSubclassOf(typeof(UIBehaviour)) && type.IsAbstract == false)
            {
                UnityEngine.Debug.Log(type.FullName);
                var go = new GameObject();
                var comp = go.AddComponent(type) as UIBehaviour;
                var script = MonoScript.FromMonoBehaviour(comp);
                var path = AssetDatabase.GetAssetPath(script);
                r.Add(new UnityReference(
                    uiguid,
                    FileIDUtil.Compute(type).ToString(),
                    AssetDatabase.AssetPathToGUID(path),
                    "11500000"
                ));
                r.Add(new UnityReference(
                    uiguid2,
                    FileIDUtil.Compute(type).ToString(),
                    AssetDatabase.AssetPathToGUID(path),
                    "11500000"
                ));
                GameObject.DestroyImmediate(go);
            }
        }
        
        ReplaceReferences(Application.dataPath, r, uiguid, uiguid2);
    }

    static void ReplaceReferences(string assetFolder, List<UnityReference> r, string guid, string guid2)
    {
        var regex1 = new Regex(@"fileID: -?[0-9]+, guid: " + guid);
        var regex2 = new Regex(@"fileID: -?[0-9]+, guid: " + guid2);
        string[] files = Directory.GetFiles(assetFolder, "*", SearchOption.AllDirectories);
        for (int i = 0; i < files.Length; i++)
        {
            string file = files[i];
 
            if (EditorUtility.DisplayCancelableProgressBar("Replace DLL", file, i/(float)files.Length))
            {
                EditorUtility.ClearProgressBar();
                return;
            }
 
            if (file.EndsWith(".asset") || file.EndsWith(".prefab") || file.EndsWith(".unity"))
            {
                ReplaceFile(file, r);
                FindNotReplacedFiles(file, guid, regex1);
                FindNotReplacedFiles(file, guid2, regex2);
            }
        }
 
        EditorUtility.ClearProgressBar();
    }
 
    static void ReplaceFile(string filePath, List<UnityReference> references)
    {
        var fileContents = System.IO.File.ReadAllText(filePath);
         
        bool match = false;
         
        foreach(UnityReference r in references)
        {
            if (r.regex == null)
                r.regex = new Regex(@"fileID: " + r.srcFileId + ", guid: " + r.srcGuid);
            if (r.regex.IsMatch(fileContents))
            {
                fileContents = r.regex.Replace(fileContents, "fileID: " + r.dstFileId + ", guid: " + r.dstGuid);
                match = true;
                Debug.Log("Replaced: " + filePath);
            }
        }
         
        if (match)
        {
            System.IO.File.WriteAllText(filePath, fileContents); 
        }
    }
 
    /// <summary>
    /// Just to make sure that all references are replaced.
    /// </summary>
    static void FindNotReplacedFiles(string filePath, string ignore, Regex regex)
    {
        var fileContents = System.IO.File.ReadAllText(filePath);

        // -?        number can be negative
        // [0-9]+    1-n numbers
        regex.Replace(fileContents, match=>
        {
            Debug.LogWarning("NotReplaced: " + filePath);
            return match.Value;
        });
    }
 
    class UnityReference
    {
        public UnityReference(string srcGuid, string srcFileId, string dstGuid, string dstFileId)
        {
            this.srcGuid = srcGuid;
            this.srcFileId = srcFileId;
            this.dstGuid = dstGuid;
            this.dstFileId = dstFileId;
        }
         
        public string srcGuid;
        public string srcFileId;
        public string dstGuid;
        public string dstFileId;

        public Regex regex;
    }
}
