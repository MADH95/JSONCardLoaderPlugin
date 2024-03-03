﻿using DiskCardGame;
using JLPlugin;
using JLPlugin.V2.Data;
using System;
using System.Collections.Generic;
using TinyJson;

namespace JSONLoader.API
{
    public static class JSONLoaderAPI
    {
        public static List<string> customFileExtensionExceptions = new List<string>();

        public class ConfigilAction
        {
            public string actionName { get; set; }
            public List<string> fields { get; set; }
            public Action<Dictionary<string, string>> functionToCall { get; set; }
        }

        public static List<ConfigilAction> customActionList = new List<ConfigilAction>();
        public static void AddAction(ConfigilAction action)
        {
            customActionList.Add(action);
        }

        public static void AddActions(List<ConfigilAction> actions)
        {
            customActionList.AddRange(actions);
        }

        public static event Func<Dictionary<string, string>, Dictionary<string, string>> ModifyVariableList;

        public static Dictionary<string, string> GetModifiedVariableList(Dictionary<string, string> variables)
        {
            if (ModifyVariableList == null) return variables;
            return ModifyVariableList(variables);
        }

        public static void AddCard(string json) { AddCards(json); }

        public static void AddCards(params string[] json)
        {
            foreach (string card in json)
            {
                try
                {
                    CardSerializeInfo cardInfo = JSONParser.FromJson<CardSerializeInfo>(card);
                    ImportExportUtils.SetDebugPath(Environment.StackTrace);
                    cardInfo.Apply();
                    Plugin.Log.LogDebug($"Added card {cardInfo.name} using JSONLoader API");
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogError($"Failed to add card using JSONLoader API: {ex.Message}");
                    Plugin.Log.LogError(ex);
                }
            }
        }

        public static void RemoveCard(string json) { RemoveCards(json); }

        public static void RemoveCards(params string[] json)
        {
            foreach (string card in json)
            {
                try
                {
                    CardSerializeInfo cardInfo = JSONParser.FromJson<CardSerializeInfo>(card);
                    cardInfo.Remove();
                    Plugin.Log.LogDebug($"Removed card {cardInfo.name} using JSONLoader API");
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogError($"Failed to remove card using JSONLoader API: {ex.Message}");
                    Plugin.Log.LogError(ex);
                }
            }
        }

        public static CardInfo ParseCard(string json)
        {
            try
            {
                ImportExportUtils.SetDebugPath(Environment.StackTrace);
                CardSerializeInfo cardInfo = JSONParser.FromJson<CardSerializeInfo>(json);
                return cardInfo.ToCardInfo();
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"Failed to parse card using JSONLoader API: {ex.Message}");
                Plugin.Log.LogError(ex);
            }
            return null;
        }

        public static List<CardInfo> ParseCards(params string[] json)
        {
            List<CardInfo> cards = new List<CardInfo>();
            foreach (string card in json)
            {
                cards.Add(ParseCard(card));
            }
            return cards;
        }
    }
}
