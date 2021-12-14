﻿
using System;
using System.Linq;
using System.Collections.Generic;

using DiskCardGame;

namespace JLPlugin.Utils
{
    using Data;
    using JSONLoader.DynamicClasses;
    using static JSONLoader.DynamicClasses.TalkingCards;

    public static class Dicts
    {

        // Cards

        public static readonly Dictionary<string, CardMetaCategory> MetaCategory
            = Enum.GetValues(typeof(CardMetaCategory))
                    .Cast<CardMetaCategory>()
                    .ToDictionary(t => t.ToString(), t => t);

        public static readonly Dictionary<string, CardComplexity> Complexity
            = Enum.GetValues(typeof(CardComplexity))
                    .Cast<CardComplexity>()
                    .ToDictionary(t => t.ToString(), t => t);

        public static readonly Dictionary<string, CardTemple> Temple
            = Enum.GetValues(typeof(CardTemple))
                    .Cast<CardTemple>()
                    .ToDictionary(t => t.ToString(), t => t);

        public static readonly Dictionary<string, GemType> GemColour
            = Enum.GetValues(typeof(GemType))
                    .Cast<GemType>()
                    .ToDictionary(t => t.ToString(), t => t);

        public static readonly Dictionary<string, SpecialStatIcon> StatIcon
            = Enum.GetValues(typeof(SpecialStatIcon))
                    .Cast<SpecialStatIcon>()
                    .ToDictionary(t => t.ToString(), t => t);

        public static readonly Dictionary<string, Tribe> Tribes
            = Enum.GetValues(typeof(Tribe))
                    .Cast<Tribe>()
                    .ToDictionary(t => t.ToString(), t => t);

        public static readonly Dictionary<string, Trait> Traits
            = Enum.GetValues(typeof(Trait))
                    .Cast<Trait>()
                    .ToDictionary(t => t.ToString(), t => t);

        public static readonly Dictionary<string, SpecialTriggeredAbility> SpecialAbilities
            = Enum.GetValues(typeof(SpecialTriggeredAbility))
                    .Cast<SpecialTriggeredAbility>()
                    .ToDictionary(t => t.ToString(), t => t);

        public static readonly Dictionary<string, Ability> Abilities
            = Enum.GetValues(typeof(Ability))
                    .Cast<Ability>()
                    .ToDictionary(t => t.ToString(), t => t);

        public static readonly Dictionary<string, CardAppearanceBehaviour.Appearance> AppearanceBehaviour
            = Enum.GetValues(typeof(CardAppearanceBehaviour.Appearance))
                    .Cast<CardAppearanceBehaviour.Appearance>()
                    .ToDictionary(t => t.ToString(), t => t);

       public static readonly Dictionary<string, Emotion> Emotions
            = Enum.GetValues( typeof( Emotion ) )
                    .Cast<Emotion>()
                    .ToDictionary( t => t.ToString(), t => t );

       public static readonly Dictionary<string, StoryEvent> StoryEvents
            = Enum.GetValues( typeof( StoryEvent ) )
                    .Cast<StoryEvent>()
                    .ToDictionary( t => t.ToString(), t => t );

        public static readonly Dictionary<string, DialogueID> DialogueIDs
            = Enum.GetValues( typeof( DialogueID ) )
                    .Cast<DialogueID>()
                    .ToDictionary( t => t.ToString(), t => t );

        public static readonly List<string> CardDataFields
            = typeof(CardData).GetFields()
                                .Select(elem => elem.Name)
                                .ToList();


        // Encounters

        public static readonly List<string> EncounterDataFields
            = typeof(EncounterData).GetFields()
                                .Select(elem => elem.Name)
                                .ToList();
    }
}