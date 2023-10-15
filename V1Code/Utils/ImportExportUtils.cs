﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using DiskCardGame;
using InscryptionAPI.Guid;
using InscryptionAPI.Helpers;
using InscryptionAPI.Localizing;
using JLPlugin;
using TinyJson;
using UnityEngine;

public static class ImportExportUtils
{
    private static string ID;
    private static string DebugPath;
    
    public static void SetID(string id)
    {
        ID = id;
    }

    public static void SetDebugPath(string path)
    {
        DebugPath = path;
    }
    
    public static T ParseEnum<T>(string value) where T : unmanaged, System.Enum
    {
        T result;
        if (Enum.TryParse<T>(value, out result))
            return result;

        int idx = Math.Max(value.LastIndexOf('_'), value.LastIndexOf('.'));

        if (idx < 0)
            throw new InvalidCastException($"Cannot parse {value} as {typeof(T).FullName}");

        string guid = value.Substring(0, idx);
        string name = value.Substring(idx + 1);
        return GuidManager.GetEnumValue<T>(guid, name);
    }

    public static void ApplyProperty<T,Y>(Func<T> getter, Action<T> setter, ref Y serializeInfoValue, bool toCardInfo, string category, string suffix)
    {
        if (toCardInfo)
        {
            T t = default;
            ApplyValue(ref t, ref serializeInfoValue, true, category, suffix);
            setter(t);
                
        }
        else
        {
            T t = getter();
            ApplyValue(ref t, ref serializeInfoValue, false, category, suffix);
        }
    }

    public static void ApplyProperty<T,Y>(ref T serializeInfoValue, Func<Y> getter, Action<Y> setter, bool toCardInfo, string category, string suffix)
    {
        if (toCardInfo)
        {
            Y y = getter();
            ApplyValue(ref serializeInfoValue, ref y, false, category, suffix);
        }
        else
        {
            Y y = default;
            ApplyValue(ref serializeInfoValue, ref y, true, category, suffix);
            setter(y);
        }
    }

    public static void ApplyValue<T, Y>(ref T a, ref Y b, bool toA, string category, string suffix)
    {
        if (toA)
        {
            ConvertValue(ref b, ref a, category, suffix);
        }
        else
        {
            ConvertValue(ref a, ref b, category, suffix);
        }
    }
    
    private static void ConvertValue<FromType, ToType>(ref FromType from, ref ToType to, string category, string suffix)
    {
        //Plugin.Log.LogInfo($"ConvertValue {typeof(FromType)} to {typeof(ToType)}");
        Type fromType = typeof(FromType);
        Type toType = typeof(ToType);
        try
        {
            if (fromType == toType)
            {
                to = (ToType)(object)from;
                return;
            }
            else if (AreNullableTypesEqual(from, to, out object fromValue, out object _, out bool fromHasValue,
                         out bool _))
            {
                //Debug.Log($"Same types and someone is nullable");
                if (fromHasValue)
                {
                    to = (ToType)fromValue;
                }
                return;
            }
            else if (fromType.IsGenericType && fromType.GetGenericTypeDefinition() == typeof(List<>) &&
                     toType.IsGenericType && toType.GetGenericTypeDefinition() == typeof(List<>))
            {
                // List to List
                IList toList = (IList)Activator.CreateInstance(toType);
                to = (ToType)toList;
                if (from != null)
                {
                    IList fromList = (IList)from;
                    for (int i = 0; i < fromList.Count; i++)
                    {
                        var o1 = fromList[i];
                        var o2 = GetDefault(toType.GetGenericArguments().Single());

                        object[] parameters = { o1, o2, category, $"{suffix}_{i+1}" };
                        var m = typeof(ImportExportUtils).GetMethod(nameof(ConvertValue), BindingFlags.NonPublic | BindingFlags.Static)
                            .MakeGenericMethod(fromType.GetGenericArguments().Single(), toType.GetGenericArguments().Single());
                        
                        m.Invoke(null, parameters);
                        toList.Add(parameters[1]);
                    }
                }
                return;
            }
            else if (fromType.IsGenericType && fromType.GetGenericTypeDefinition() == typeof(List<>) && toType.IsArray)
            {
                // List to Array
                //Plugin.Log.LogInfo($"List to Array {from} {to}");
                IList fromList = (IList)from;
                int size = from == null ? 0 : fromList.Count;
                Array toArray = Array.CreateInstance(toType.GetElementType(), size);
                to = (ToType)(object)toArray;
                if (from != null)
                {
                    for (int i = 0; i < fromList.Count; i++)
                    {
                        var o1 = fromList[i];
                        var o2 = GetDefault(toType.GetElementType());

                        object[] parameters = { o1, o2, category, $"{suffix}_{i+1}" };
                        var m = typeof(ImportExportUtils).GetMethod(nameof(ConvertValue), BindingFlags.NonPublic | BindingFlags.Static)
                            .MakeGenericMethod(fromType.GetGenericArguments().Single(), toType.GetElementType());
                        
                        m.Invoke(null, parameters);
                        
                        toArray.SetValue(parameters[1], i);
                    }
                }
                
                //Plugin.Log.LogInfo($"List to Array Done {from} {to}");
                return;
            }
            else if (fromType.IsArray && toType.IsGenericType && toType.GetGenericTypeDefinition() == typeof(List<>))
            {
                // Array to List
                IList toList = (IList)Activator.CreateInstance(toType);
                to = (ToType)toList;
                if (from != null)
                {
                    Array fromArray = (Array)(object)from;
                    for (int i = 0; i < fromArray.Length; i++)
                    {
                        var o1 = fromArray.GetValue(i);
                        var o2 = GetDefault(toType.GetGenericArguments().Single());

                        object[] parameters = { o1, o2, category, $"{suffix}_{i+1}" };
                        var m = typeof(ImportExportUtils).GetMethod(nameof(ConvertValue), BindingFlags.NonPublic | BindingFlags.Static)
                            .MakeGenericMethod(fromType.GetElementType(), toType.GetGenericArguments().Single());
                        
                        m.Invoke(null, parameters);
                        
                        toList.Add(parameters[1]);
                    }
                }
                
                return;
            }
            else if (fromType.IsEnum && toType == typeof(string))
            {
                string oType = from.ToString();
                if (int.TryParse(oType, out int value))
                {
                    // Custom type
                    object[] parameters = { value, "guid", "name" };
                    var m = typeof(GuidManager).GetMethod(nameof(GuidManager.TryGetGuidAndKeyEnumValue), BindingFlags.Public | BindingFlags.Static)
                        .MakeGenericMethod(fromType);
                    var result = (bool)m.Invoke(null, parameters);
                    
                    if (result)
                    {
                        string guid = (string)parameters[1];
                        string key = (string)parameters[2];
                        to = (ToType)(object)(guid + "_" + key);
                    }
                    else
                    {
                        Error($"Failed to convert enum to string! '{from}'");
                        to = (ToType)(object)oType;
                    }
                }
                else
                {
                    to = (ToType)(object)oType;
                }

                return;
            }
            else if (fromType == typeof(string) && toType.IsEnum)
            {
                if (!string.IsNullOrEmpty((string)(object)from))
                {
                    object o = typeof(ImportExportUtils)
                        .GetMethod(nameof(ParseEnum), BindingFlags.Public | BindingFlags.Static)
                        .MakeGenericMethod(toType)
                        .Invoke(null, new object[] { from });
                    to = (ToType)o;
                }

                return;
            }
            else if (fromType == typeof(CardInfo) && toType == typeof(string))
            {
                if (from != null)
                    to = (ToType)(object)((from as CardInfo).name);
                return;
            }
            else if (fromType == typeof(string) && toType == typeof(CardInfo))
            {
                string s = (string)(object)from;
                if (!string.IsNullOrEmpty(s))
                    to = (ToType)(object)CardLoader.GetCardByName(s);
                return;
            }
            else if (fromType == typeof(string) && (toType == typeof(Texture) || toType.IsSubclassOf(typeof(Texture))))
            {
                string path = (string)(object)from;
                if (!string.IsNullOrEmpty(path))
                {
                    try
                    {
                        to = (ToType)(object)TextureHelper.GetImageAsTexture(path);
                    }
                    catch (FileNotFoundException)
                    {
                        Error($"Failed to find texture {path}!");
                    }
                }
            
                return;
            }
            else if ((fromType == typeof(Texture) || fromType.IsSubclassOf(typeof(Texture))) && toType == typeof(string))
            {
                Texture texture = (Texture)(object)from;
                if (texture != null)
                {
                    string path = Path.Combine(Plugin.ExportDirectory, category, "Assets", $"{ID}_{suffix}.png");
                    to = (ToType)(object)ExportTexture(texture, path);
                }

                return;
            }
            else if (fromType == typeof(string) && toType == typeof(Sprite))
            {
                string path = (string)(object)from;
                if (!string.IsNullOrEmpty(path))
                {
                    Texture2D imageAsTexture = TextureHelper.GetImageAsTexture(path);
                    if (imageAsTexture != null)
                    {
                        to = (ToType)(object)imageAsTexture.ConvertTexture();
                    }
                }
            
                return;
            }
            else if (fromType == typeof(Sprite) && toType == typeof(string))
            {
                Sprite texture = (Sprite)(object)from;
                if (texture != null)
                {
                    string path = Path.Combine(Plugin.ExportDirectory, category, "Assets", $"{ID}_{suffix}.png");
                    to = (ToType)(object)ExportTexture(texture.texture, path);
                }

                return;
            }
            else if(fromType.GetInterfaces().Contains(typeof(IConvertible)) && toType.GetInterfaces().Contains(typeof(IConvertible)))
            {
                IConvertible a = from as IConvertible;
                IConvertible b = to as IConvertible;
                if (a != null && b != null)
                {
                    to = (ToType)Convert.ChangeType(a, toType);
                }
                return;
            }
            else if (fromType == typeof(JSONParser.LocalizableField) && toType == typeof(string))
            {
                Error("Use ApplyLocaleField when converted from LocalizableField to string!");
            }
            else if (fromType == typeof(string) && toType == typeof(JSONParser.LocalizableField))
            {
                Error("Use ApplyLocaleField when converted from string to LocalizableField!");
            }
        }
        catch (Exception e)
        {
            Error($"Failed to convert: {fromType} to {toType}");
            Exception(e);
            return;
        }

        Error($"Unsupported conversion type: {fromType} to {toType}\n{Environment.StackTrace}");
    }

    private static bool AreNullableTypesEqual<T, Y>(T t, Y y, out object a, out object b, out bool aHasValue, out bool bHasValue)
    {
        //Debug.Log($"AreNullableTypesEqual: {typeof(T)} to {typeof(Y)}");
        aHasValue = false;
        bHasValue = false;
        a = null;
        b = null;
        
        bool tIsNullable = typeof(T).IsGenericType && typeof(T).GetGenericTypeDefinition() == typeof(Nullable<>);
        bool yIsNullable = typeof(Y).IsGenericType && typeof(Y).GetGenericTypeDefinition() == typeof(Nullable<>);
        if (!tIsNullable && !yIsNullable)
        {
            //Debug.Log($"\t Neither are nullable");
            return false;
        }

        Type tInnerType = tIsNullable ? Nullable.GetUnderlyingType(typeof(T)) : typeof(T);
        Type yInnerType = yIsNullable ? Nullable.GetUnderlyingType(typeof(Y)) : typeof(Y);
        if (tInnerType == yInnerType)
        {
            //Debug.Log($"\t Same Inner types: {t}({tInnerType}) {y}({yInnerType})");
            if (tIsNullable)
            {
                a = GetValueFromNullable(t, out aHasValue);
            }
            else
            {
                a = t;
                aHasValue = true;
            }
            
            if (yIsNullable)
            {
                b = GetValueFromNullable(y, out bHasValue);
            }
            else
            {
                b = y;
                bHasValue = true;
            }

            return true;
        }

        Error($"Not same types {typeof(T)} {typeof(Y)}");
        return false;
    }

    private static string ExportTexture(Texture texture, string path)
    {
        if (texture is Texture2D texture2D)
        {
            return ExportTexture(texture2D, path);
        }
        
        Texture2D converted = Texture2D.CreateExternalTexture(
            texture.width,
            texture.height,
            TextureFormat.RGBA32,
            false, false,
            texture.GetNativeTexturePtr());
        return ExportTexture(converted, path);
    }
    
    private static string ExportTexture(Texture2D texture, string path)
    {
        if (!texture.isReadable)
        {
            RenderTexture renderTex = RenderTexture.GetTemporary(
                texture.width,
                texture.height,
                0,
                RenderTextureFormat.Default,
                RenderTextureReadWrite.Linear);

            Graphics.Blit(texture, renderTex);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = renderTex;
            Texture2D readableText = new Texture2D(texture.width, texture.height);
            readableText.ReadPixels(new Rect(0, 0, renderTex.width, renderTex.height), 0, 0);
            readableText.Apply();
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(renderTex);
            texture = readableText;
        }

        byte[] bytes = texture.EncodeToPNG();
        if (bytes == null)
        {
            Error("Failed to turn into bytes??");
        }

        if (string.IsNullOrEmpty(path))
        {
            Error("path is empty????");
        }
        
        var dirPath = Path.GetDirectoryName(path);
        if (!Directory.Exists(dirPath))
        {
            Directory.CreateDirectory(dirPath);
        }

        File.WriteAllBytes(path, bytes);
        return Path.GetFileName(path);
    }

    public static string[] ExportTextures(IEnumerable<Texture2D> texture, string type, string fileName)
    {
        int i = 0;
        List<string> paths = new List<string>();
        foreach (Texture2D texture2D in texture)
        {
            i++;

            string path = Path.Combine(Plugin.ExportDirectory, type, "Assets", $"{fileName}_{i}.png");
            paths.Add(ExportTexture(texture2D, path));
        }

        return paths.ToArray();
    }

    public static void ApplyLocaleField(string field, ref JSONParser.LocalizableField rows, ref string cardInfoEnglishField, bool toCardInfo)
    {
        if (toCardInfo)
        {
            ApplyLocaleField(field, rows, out cardInfoEnglishField);
        }
        else
        {
            string s = cardInfoEnglishField;
            cardInfoEnglishField = s;
            ImportLocaleField(rows, cardInfoEnglishField);
        }
    }

    private static void ImportLocaleField(JSONParser.LocalizableField rows, string cardInfoEnglishField)
    {
        // From game to LocalizableField
        rows.rows.Clear();
        rows.Initialize(cardInfoEnglishField);
        
        var translation = Localization.Translations.Find((a) => a.englishStringFormatted == cardInfoEnglishField);
        if (translation != null)
        {
            foreach (KeyValuePair<Language, string> pair in translation.values)
            {
                string code = LocalizationManager.LanguageToCode(pair.Key);
                rows.SetValue($"{rows.englishFieldName}_{code}", pair.Value);
                VerboseLog($"Loaded {cardInfoEnglishField} translation for {code} => {pair.Key}");
            }
        }
        else
        {
            VerboseLog($"ApplyLocaleField could not find any translations from english '{cardInfoEnglishField}'");
        }
    }

    /// <summary>
    /// From SerializeCardInfo to cardInfo
    /// </summary>
    /// <param name="field"></param>
    /// <param name="rows"></param>
    /// <param name="cardInfoEnglishField"></param>
    /// <param name="toCardInfo"></param>
    private static void ApplyLocaleField(string field, JSONParser.LocalizableField rows, out string cardInfoEnglishField)
    {
        if (rows.rows.TryGetValue(rows.englishFieldName, out string english))
        {
            cardInfoEnglishField = english;
        }
        else if (rows.rows.Count > 0)
        {
            cardInfoEnglishField = rows.rows.First().Value;
        }
        else
        {
            cardInfoEnglishField = null;
            return;
        }

        VerboseLog($"ApplyLocaleField {field} english {cardInfoEnglishField}");
        foreach (KeyValuePair<string, string> pair in rows.rows)
        {
            if (pair.Key == rows.englishFieldName)
                continue;

            int indexOf = pair.Key.LastIndexOf("_", StringComparison.Ordinal);
            if (indexOf < 0)
            {
                VerboseError($"Could not find _ of key {pair.Key} in field {field}!");
                continue;
            }

            // Translations
            int length = pair.Key.Length - indexOf - 1;
            string code = pair.Key.Substring(indexOf + 1, length);
            Language language = LocalizationManager.CodeToLanguage(code);
            if (language != Language.NUM_LANGUAGES)
            {
                LocalizationManager.Translate(Plugin.PluginGuid, null, cardInfoEnglishField, pair.Value, language);
                VerboseLog($"Translation {cardInfoEnglishField} to {code} = {pair.Value}");
            }
            else
            {
                Error($"Unknown language code {code} for card {cardInfoEnglishField} in field {field}");
            }
        }
    }

    private static object GetValueFromNullable<U>(U u, out bool hasValue)
    {
        Type type = typeof(U);
        if (u != null)
        {
            bool v = (bool)type.GetProperty("HasValue", BindingFlags.Instance | BindingFlags.Public).GetValue(u);
            if (v)
            {
                hasValue = true;
                return type.GetProperty("Value", BindingFlags.Instance | BindingFlags.Public).GetValue(u);
            }
        }

        hasValue = false;
        Type underlyingType = Nullable.GetUnderlyingType(type);
        if(underlyingType.IsValueType)
        {
            return Activator.CreateInstance(underlyingType);
        }
        return null;
    }

    private static object GetDefault(Type type)
    {
        if(type.IsValueType)
        {
            return Activator.CreateInstance(type);
        }
        return null;
    }
    
    private static void VerboseLog(string message)
    {
        Plugin.VerboseLog($"[{DebugPath}][{ID}] {message}");
    }
    
    private static void VerboseWarning(string message)
    {
        if (Plugin.verboseLogging.Value)
            Plugin.VerboseWarning($"[{DebugPath}][{ID}] {message}");
    }
    
    private static void VerboseError(string message)
    {
        if (Plugin.verboseLogging.Value)
            Plugin.VerboseError($"[{DebugPath}][{ID}] {message}");
    }
    
    private static void Error(string message)
    {
        if (Plugin.verboseLogging.Value)
            Plugin.Log.LogError($"[{DebugPath}][{ID}] {message}");
    }
    
    private static void Exception(Exception e)
    {
        if (Plugin.verboseLogging.Value)
            Plugin.Log.LogError($"[{DebugPath}][{ID}] {e.Message}\n{e.StackTrace}");
    }
}