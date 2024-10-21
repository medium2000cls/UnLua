using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
#if UE_5_0_OR_LATER
using EpicGames.Core;
#else
using Tools.DotNETCommon;
#endif
using UnrealBuildTool;

public class Cffilua : ModuleRules
{
    public Cffilua(ReadOnlyTargetRules Target) : base(Target)
    {
        Type = ModuleType.External;
        bEnableUndefinedIdentifierWarnings = false;
        ShadowVariableWarningLevel = WarningLevel.Off;
        
        m_LuaVersion = GetLuaVersion();
        m_LibName = GetLibraryName();
        
        Dictionary<string, Action> actions = new Dictionary<string, Action>();
        actions["Win64"] = BuildForWin64;
		actions[Target.Platform.ToString()].Invoke();
    }

    private void BuildForWin64()
    {
        var dllPath = GetLibraryPath();
        PublicDelayLoadDLLs.Add(m_LibName);
        SetupForRuntimeDependency(dllPath, "Win64");
    }
    
    private string GetLuaVersion()
    {
        var projectDir = Target.ProjectFile.Directory;
        var configFilePath = projectDir + "/Config/DefaultUnLuaEditor.ini";
        var configFileReference = new FileReference(configFilePath);
        var configFile = FileReference.Exists(configFileReference)
            ? new ConfigFile(configFileReference)
            : new ConfigFile();
        var config = new ConfigHierarchy(new[] { configFile });
        const string section = "/Script/UnLuaEditor.UnLuaEditorSettings";
        string version;
        if (config.GetString(section, "LuaVersion", out version))
            return version;
        return "lua-5.4.3";
    }
    
    private static void CopyDirectory(string srcDir, string dstDir)
    {
        var dir = new DirectoryInfo(srcDir);
        if (!dir.Exists)
            throw new DirectoryNotFoundException(dir.FullName);
    
        var dirs = dir.GetDirectories();
        Directory.CreateDirectory(dstDir);
    
        foreach (var file in dir.GetFiles())
        {
            var targetFilePath = Path.Combine(dstDir, file.Name);
            file.CopyTo(targetFilePath, true);
        }
    
        foreach (var subDir in dirs)
        {
            var newDstDir = Path.Combine(dstDir, subDir.Name);
            CopyDirectory(subDir.FullName, newDstDir);
        }
    }
    private string GetLibraryPath(string architecture = null)
    {
        if (architecture == null)
            architecture = string.Empty;
        return Path.Combine(ModuleDirectory, m_LuaVersion, "lib", Target.Platform.ToString(), architecture, m_LibName);
    }
    
    private string GetLibraryName()
    {
        if (Target.Platform == UnrealTargetPlatform.Win64)
            return "cffi.dll";
        if (Target.Platform == UnrealTargetPlatform.Mac)
            return "cffi.dylib";
        return "cffi.a";
    }
    
    private void SetupForRuntimeDependency(string fullPath, string platform)
    {
        if (!File.Exists(fullPath))
        {
            return;
        }
        var fileName = Path.GetFileName(fullPath);
        var dstPath = Path.Combine("$(ProjectDir)", "Binaries", platform, fileName);
        RuntimeDependencies.Add(dstPath, fullPath);
    }

    private readonly string m_LuaVersion;
    private readonly string m_LibName;

}