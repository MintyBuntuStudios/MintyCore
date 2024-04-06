﻿using Avalonia;
using JetBrains.Annotations;
using MintyCore;
using MintyCore.AvaloniaIntegration;
using TestMod;

namespace AvaloniaDesigner;

internal static class Program
{
    private static void Main(string[] args)
    {
        throw new NotSupportedException("This project isn't meant to be run: it's only for Avalonia designer support.");
    }

    [UsedImplicitly]
    public static AppBuilder BuildAvaloniaApp()
    {
        var configPath = Path.Combine(AppContext.BaseDirectory, "ProjectConfig.txt");
        var actualProjectPath = File.ReadAllText(configPath).Trim();
        
        return AppBuilder.Configure<App>().UseMintyCoreIdePreview(actualProjectPath);
    }
}