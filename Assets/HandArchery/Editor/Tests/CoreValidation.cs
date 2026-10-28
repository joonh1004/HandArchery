using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace HandArchery.EditorTests
{
    public static class CoreValidation
    {
        [MenuItem("HandArchery/Validate Core")]
        public static void Run()
        {
            CoreTestResult result = CoreTestSuite.RunAll();
            string directory = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Validation");
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, "core-results.json"), result.ToJson());
            foreach (CoreTestCase item in result.Cases)
                if (!item.Passed) Debug.LogError("CORE FAIL " + item.Name + ": " + item.Error);
            Debug.Log("CORE TOTAL " + result.Cases.Count + " PASS " + result.Passed + " FAIL " + result.Failed);
            if (Application.isBatchMode) EditorApplication.Exit(result.Failed == 0 ? 0 : 1);
            else if (result.Failed > 0) throw new InvalidOperationException("Core validation failed. See Validation/core-results.json");
        }
    }
}
