﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;

#pragma warning disable CS1591

namespace MintyCore.Lib.VeldridUtils;

/// <summary>
///     A parser for Wavefront MTL files.
/// </summary>
public class MtlParser
{
    private readonly ParseContext _pc = new();

    /// <summary>
    ///     Parses a <see cref="MtlFile" /> from the given array of text lines.
    /// </summary>
    /// <param name="lines">The raw text lines of the MTL file.</param>
    /// <returns>A new <see cref="MtlFile" />.</returns>
    public MtlFile Parse(string[] lines)
    {
        foreach (var line in lines) _pc.Process(line);
        _pc.EndOfFileReached();

        return _pc.FinalizeFile();
    }

    /// <summary>
    ///     Parses a <see cref="MtlFile" /> from the given stream
    /// </summary>
    /// <param name="s">The stream to parse from.</param>
    /// <returns>A new <see cref="MtlFile" />.</returns>
    public MtlFile Parse(Stream s)
    {
        string text;
        using (var sr = new StreamReader(s))
        {
            text = sr.ReadToEnd();
        }

        var lineStart = 0;
        int lineEnd;
        while ((lineEnd = text.IndexOf('\n', lineStart)) != -1)
        {
            string line;

            if (lineEnd != 0 && text[lineEnd - 1] == '\r')
                line = text.Substring(lineStart, lineEnd - lineStart - 1);
            else
                line = text.Substring(lineStart, lineEnd - lineStart);

            _pc.Process(line);
            lineStart = lineEnd + 1;
        }

        _pc.EndOfFileReached();
        return _pc.FinalizeFile();
    }

    private class ParseContext
    {
        private static readonly char[] _whitespaceChars = {' '};

        private readonly List<MaterialDefinition> _definitions = new();
        private MaterialDefinition? _currentDefinition;

        private int _currentLine;
        private string _currentLineText = string.Empty;

        public void Process(string line)
        {
            _currentLine++;
            _currentLineText = line;

            var pieces = line.Split(_whitespaceChars, StringSplitOptions.RemoveEmptyEntries);
            if (pieces.Length == 0 || pieces[0].StartsWith("#")) return;
            switch (pieces[0].ToLowerInvariant().Trim())
            {
                case "newmtl":
                    ExpectExactly(pieces, 1, "newmtl");
                    FinalizeCurrentMaterial();
                    _currentDefinition = new MaterialDefinition(pieces[1]);
                    break;
                case "ka":
                    ExpectExactly(pieces, 3, "Ka");
                    Debug.Assert(_currentDefinition != null, nameof(_currentDefinition) + " != null");
                    _currentDefinition.AmbientReflectivity = ParseVector3(pieces[1], pieces[2], pieces[3], "Ka");
                    break;
                case "kd":
                    ExpectExactly(pieces, 3, "Kd");
                    Debug.Assert(_currentDefinition != null, nameof(_currentDefinition) + " != null");
                    _currentDefinition.DiffuseReflectivity = ParseVector3(pieces[1], pieces[2], pieces[3], "Kd");
                    break;
                case "ks":
                    ExpectExactly(pieces, 3, "Ks");
                    Debug.Assert(_currentDefinition != null, nameof(_currentDefinition) + " != null");
                    _currentDefinition.SpecularReflectivity = ParseVector3(pieces[1], pieces[2], pieces[3], "Ks");
                    break;
                case "ke": // Non-standard?
                    ExpectExactly(pieces, 3, "Ke");
                    Debug.Assert(_currentDefinition != null, nameof(_currentDefinition) + " != null");
                    _currentDefinition.EmissiveCoefficient = ParseVector3(pieces[1], pieces[2], pieces[3], "Ks");
                    break;
                case "tf":
                    ExpectExactly(pieces, 3, "Tf");
                    Debug.Assert(_currentDefinition != null, nameof(_currentDefinition) + " != null");
                    _currentDefinition.TransmissionFilter = ParseVector3(pieces[1], pieces[2], pieces[3], "Tf");
                    break;
                case "illum":
                    ExpectExactly(pieces, 1, "illum");
                    Debug.Assert(_currentDefinition != null, nameof(_currentDefinition) + " != null");
                    _currentDefinition.IlluminationModel = ParseInt(pieces[1], "illum");
                    break;
                case "d": // "Dissolve", or opacity
                    ExpectExactly(pieces, 1, "d");
                    Debug.Assert(_currentDefinition != null, nameof(_currentDefinition) + " != null");
                    _currentDefinition.Opacity = ParseFloat(pieces[1], "d");
                    break;
                case "tr": // Transparency
                    ExpectExactly(pieces, 1, "Tr");
                    Debug.Assert(_currentDefinition != null, nameof(_currentDefinition) + " != null");
                    _currentDefinition.Opacity = 1 - ParseFloat(pieces[1], "Tr");
                    break;
                case "ns":
                    ExpectExactly(pieces, 1, "Ns");
                    Debug.Assert(_currentDefinition != null, nameof(_currentDefinition) + " != null");
                    _currentDefinition.SpecularExponent = ParseFloat(pieces[1], "Ns");
                    break;
                case "sharpness":
                    ExpectExactly(pieces, 1, "sharpness");
                    Debug.Assert(_currentDefinition != null, nameof(_currentDefinition) + " != null");
                    _currentDefinition.Sharpness = ParseFloat(pieces[1], "sharpness");
                    break;
                case "ni": // "Index of refraction"
                    ExpectExactly(pieces, 1, "Ni");
                    Debug.Assert(_currentDefinition != null, nameof(_currentDefinition) + " != null");
                    _currentDefinition.OpticalDensity = ParseFloat(pieces[1], "Ni");
                    break;
                case "map_ka":
                    ExpectExactly(pieces, 1, "map_ka");
                    Debug.Assert(_currentDefinition != null, nameof(_currentDefinition) + " != null");
                    _currentDefinition.AmbientTexture = pieces[1];
                    break;
                case "map_kd":
                    ExpectExactly(pieces, 1, "map_kd");
                    Debug.Assert(_currentDefinition != null, nameof(_currentDefinition) + " != null");
                    _currentDefinition.DiffuseTexture = pieces[1];
                    break;
                case "map_ks":
                    ExpectExactly(pieces, 1, "map_ks");
                    Debug.Assert(_currentDefinition != null, nameof(_currentDefinition) + " != null");
                    _currentDefinition.SpecularColorTexture = pieces[1];
                    break;
                case "map_bump":
                case "bump":
                    ExpectExactly(pieces, 1, "map_bump");
                    Debug.Assert(_currentDefinition != null, nameof(_currentDefinition) + " != null");
                    _currentDefinition.BumpMap = pieces[1];
                    break;
                case "map_d":
                    ExpectExactly(pieces, 1, "map_d");
                    Debug.Assert(_currentDefinition != null, nameof(_currentDefinition) + " != null");
                    _currentDefinition.AlphaMap = pieces[1];
                    break;
                case "map_ns":
                    ExpectExactly(pieces, 1, "map_ns");
                    Debug.Assert(_currentDefinition != null, nameof(_currentDefinition) + " != null");
                    _currentDefinition.SpecularHighlightTexture = pieces[1];
                    break;


                default:
                    throw new ObjParseException(
                        $"An unsupported line-type specifier, '{pieces[0]}', was used on line {_currentLine}, \"{_currentLineText}\"");
            }
        }

        private void FinalizeCurrentMaterial()
        {
            if (_currentDefinition != null)
            {
                _definitions.Add(_currentDefinition);
                _currentDefinition = null;
            }
        }

        public void EndOfFileReached()
        {
            FinalizeCurrentMaterial();
        }

        public MtlFile FinalizeFile()
        {
            return new MtlFile(_definitions);
        }

        private Vector3 ParseVector3(string xStr, string yStr, string zStr, string location)
        {
            try
            {
                var x = float.Parse(xStr, CultureInfo.InvariantCulture);
                var y = float.Parse(yStr, CultureInfo.InvariantCulture);
                var z = float.Parse(zStr, CultureInfo.InvariantCulture);

                return new Vector3(x, y, z);
            }
            catch (FormatException fe)
            {
                throw CreateParseException(location, fe);
            }
        }

        private int ParseInt(string intStr, string location)
        {
            try
            {
                var i = int.Parse(intStr, CultureInfo.InvariantCulture);
                return i;
            }
            catch (FormatException fe)
            {
                throw CreateParseException(location, fe);
            }
        }

        private float ParseFloat(string intStr, string location)
        {
            try
            {
                var f = float.Parse(intStr, CultureInfo.InvariantCulture);
                return f;
            }
            catch (FormatException fe)
            {
                throw CreateParseException(location, fe);
            }
        }

        private void ExpectExactly(string[] pieces, int count, string name)
        {
            if (pieces.Length != count + 1)
            {
                var message = string.Format(
                    "Expected exactly {0} components to a line starting with {1}, on line {2}, \"{3}\".",
                    count,
                    name,
                    _currentLine,
                    _currentLineText);
                throw new MtlParseException(message);
            }
        }

        private MtlParseException CreateParseException(string location, Exception e)
        {
            var message = string.Format("An error ocurred while parsing {0} on line {1}, \"{2}\"", location,
                _currentLine, _currentLineText);
            return new MtlParseException(message, e);
        }
    }
}

public class MtlParseException : Exception
{
    public MtlParseException(string message) : base(message)
    {
    }

    public MtlParseException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

/// <summary>
///     Represents a parsed MTL definition file.
/// </summary>
public class MtlFile
{
    /// <summary>
    ///     Constructs a new <see cref="MtlFile" /> from pre-parsed material definitions.
    /// </summary>
    /// <param name="definitions">A collection of material definitions.</param>
    public MtlFile(IEnumerable<MaterialDefinition> definitions)
    {
        Definitions = definitions.ToDictionary(def => def.Name);
    }

    /// <summary>
    ///     Gets a mapping of all <see cref="MaterialDefinition" />s contained in this <see cref="MtlFile" />.
    /// </summary>
    public IReadOnlyDictionary<string, MaterialDefinition> Definitions { get; }
}

/// <summary>
///     An individual material definition from a Wavefront MTL file.
/// </summary>
public class MaterialDefinition
{
    internal MaterialDefinition(string name)
    {
        Name = name;
    }

    public string Name { get; }

    public Vector3 AmbientReflectivity { get; internal set; }
    public Vector3 DiffuseReflectivity { get; internal set; }
    public Vector3 SpecularReflectivity { get; internal set; }
    public Vector3 EmissiveCoefficient { get; internal set; }
    public Vector3 TransmissionFilter { get; internal set; }
    public int IlluminationModel { get; internal set; }
    public float Opacity { get; internal set; }
    public float Transparency => 1 - Opacity;
    public float SpecularExponent { get; internal set; }
    public float Sharpness { get; internal set; }
    public float OpticalDensity { get; internal set; }

    public string AmbientTexture { get; internal set; } = string.Empty;
    public string DiffuseTexture { get; internal set; } = string.Empty;
    public string SpecularColorTexture { get; internal set; } = string.Empty;
    public string SpecularHighlightTexture { get; internal set; } = string.Empty;
    public string AlphaMap { get; internal set; } = string.Empty;
    public string BumpMap { get; internal set; } = string.Empty;
    public string DisplacementMap { get; internal set; } = string.Empty;
    public string StencilDecalTexture { get; internal set; } = string.Empty;
}