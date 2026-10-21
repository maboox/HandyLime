namespace HandyLime.Core;

public sealed record AdjustmentParameter(string Key, string Label, string Group,
    double Minimum, double Maximum, double Default, double Step);

public static class ColorAdjustments
{
    public static IReadOnlyList<AdjustmentParameter> Parameters { get; } = Array.AsReadOnly<AdjustmentParameter>([
        new("exposure", "Exposure (EV)", "Light", -3, 3, 0, 0.1),
        new("brightness", "Brightness", "Light", 0, 200, 100, 1),
        new("contrast", "Contrast", "Light", -100, 100, 0, 1),
        new("highlights", "Highlights", "Light", -100, 100, 0, 1),
        new("shadows", "Shadows", "Light", -100, 100, 0, 1),
        new("whites", "Whites", "Light", -100, 100, 0, 1),
        new("blacks", "Blacks", "Light", -100, 100, 0, 1),
        new("gamma", "Gamma", "Light", 0.2, 3, 1, 0.05),
        new("temperature", "Warmth", "Color", -100, 100, 0, 1),
        new("tint", "Tint", "Color", -100, 100, 0, 1),
        new("saturation", "Saturation", "Color", 0, 200, 100, 1),
        new("vibrance", "Vibrance", "Color", -100, 100, 0, 1),
        new("hue", "Hue", "Color", -180, 180, 0, 1),
        new("sharpen", "Sharpen", "Detail", 0, 5, 0, 0.1)
    ]);
    public static Field[] Fields => Parameters.Select(p => new Field(p.Key, p.Label,
        Values.Invariant(p.Default), $"{Values.Invariant(p.Minimum)}–{Values.Invariant(p.Maximum)}")).ToArray();
    public static void Validate(Dictionary<string, string> options)
    {
        foreach (var p in Parameters) Values.Number(options, p.Key, p.Default, p.Minimum, p.Maximum);
    }
}
