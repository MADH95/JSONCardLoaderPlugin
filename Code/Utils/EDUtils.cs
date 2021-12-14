﻿
using System.Linq;
using System.Collections.Generic;

using DiskCardGame;

using APIPlugin;


namespace JLPlugin.Utils
{
    using Data;

    using static DiskCardGame.EncounterBlueprintData;

    public static class EDUtils
    {
        public static List<CardInfo> GetCardInfos( List<string> cards )
        {
            if ( cards == null )
            {
                return null;
            }
            List<CardInfo> cardInfos = new();
            foreach ( string card in cards )
            {
                CardInfo cardInfo = GetCardByName( card );
                if ( cardInfo == null )
                {
                    Plugin.Log.LogError( $"{ ErrorUtil.Identifier } - card { card } does not exist" );
                }
                else
                {
                    cardInfos.Add( cardInfo );
                }
            }
            return cardInfos;
        }

        public static T Assign<T>( string data, string field, Dictionary<string, T> dict )
        {
            ErrorUtil.Field = field;

            if ( string.IsNullOrEmpty( data ) )
                return default;

            if ( !dict.ContainsKey( data ) )
            {
                ErrorUtil.Log( data );
                return default;
            }

            return dict[data];
        }

        public static List<T> Assign<T>( List<string> list, string field, Dictionary<string, T> dict )
        {
            ErrorUtil.Field = field;

            if ( list is null || list.Count == 0 )
                return null;

            List<T> output = new();

            foreach ( string value in list )
            {
                if ( !dict.ContainsKey( value ) )
                {
                    ErrorUtil.Log( value );
                    continue;
                }

                output.Add( dict[value] );
            }

            if ( output.Count == 0 )
                return null;

            return output;
        }

        internal static List<TurnModBlueprint> GetTurnMods( List<TurnModData> mods )
        {
            if ( mods == null )
            {
                return null;
            }
            List<TurnModBlueprint> blueprints = new();
            foreach ( TurnModData mod in mods )
            {
                blueprints.Add( new TurnModBlueprint()
                {
                    turn = mod.turn,
                    applyAtDifficulty = mod.applyAtDifficulty,
                    overlockCards = mod.overclockCards
                } );
            }
            return blueprints;
        }

        public static List<List<CardBlueprint>> GetCardBlueprints( List<List<CardBlueprintData>> cards )
        {
            if ( cards == null )
            {
                return null;
            }
            List<List<CardBlueprint>> cardBlueprints = new List<List<CardBlueprint>>();
            foreach ( List<CardBlueprintData> list in cards )
            {
                cardBlueprints.Add( new List<CardBlueprint>() );
                foreach ( CardBlueprintData blueprint in list )
                {
                    string card = blueprint.card;
                    bool hasReplacement = blueprint.replacement != null;
                    CardInfo info1 = GetCardByName( card );
                    if ( info1 == null )
                    {
                        Plugin.Log.LogError( $"{ ErrorUtil.Identifier } - card { card } does not exist" );
                        continue;
                    }
                    CardInfo info2 = null;
                    if ( hasReplacement && blueprint.replacement.card != null )
                    {
                        info2 = GetCardByName( blueprint.replacement.card );
                        if ( info2 == null )
                        {
                            Plugin.Log.LogError( $"{ ErrorUtil.Identifier } - card { blueprint.replacement.card } does not exist" );
                            continue;
                        }
                    }
                    cardBlueprints.Last().Add( new CardBlueprint()
                    {
                        card = info1,
                        difficultyReplace = hasReplacement,
                        replacement = info2,
                        difficultyReq = ( hasReplacement ? blueprint.replacement.difficultyReq : 0 ),
                        randomReplaceChance = ( hasReplacement ? blueprint.randomReplaceChance : 0 )
                    } );
                }
            }
            return cardBlueprints;
        }

        public static CardInfo GetCardByName( string name )
        {
            // Modded cards are not in AllData until a save is loaded
            // Not cloned because modded cards do not have evolution parameters set until a save is loaded.
            CardInfo info = NewCard.cards.Find( ( CardInfo x ) => x.name == name );
            if ( info != null )
            {
                return info;
            }
            info = ScriptableObjectLoader<CardInfo>.AllData.Find( ( CardInfo x ) => x.name == name );
            if ( info != null )
            {
                return info;
            }
            else
            {
                return null;
            }
        }
    }
}
