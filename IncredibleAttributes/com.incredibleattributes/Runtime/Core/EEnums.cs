using UnityEngine;

namespace IncredibleAttributes
{
    /// <summary>Preset colors for use with IncredibleAttributes that need a color.</summary>
    public enum EColor
    {
        Clear, White, Black, Gray, Red, Pink, Orange, Yellow,
        Green, Teal, Blue, Indigo, Violet, Purple
    }

    /// <summary>Logical operator used when combining multiple conditions.</summary>
    public enum EConditionOperator
    {
        And,
        Or
    }

    /// <summary>Visual type of an InfoBox message.</summary>
    public enum EInfoBoxType
    {
        Normal,
        Warning,
        Error
    }

    /// <summary>When a [Button] method is interactable in the inspector.</summary>
    public enum EButtonEnableMode
    {
        /// <summary>Always clickable.</summary>
        Always,
        /// <summary>Only clickable when NOT in Play Mode.</summary>
        Editor,
        /// <summary>Only clickable in Play Mode.</summary>
        Playmode
    }

    /// <summary>Utility to convert EColor to a UnityEngine.Color.</summary>
    public static class EColorExtensions
    {
        public static Color ToColor(this EColor color) => color switch
        {
            EColor.Clear   => new Color(0, 0, 0, 0),
            EColor.White   => Color.white,
            EColor.Black   => Color.black,
            EColor.Gray    => Color.gray,
            EColor.Red     => new Color(1f, 0.2f, 0.2f),
            EColor.Pink    => new Color(1f, 0.5f, 0.7f),
            EColor.Orange  => new Color(1f, 0.6f, 0.1f),
            EColor.Yellow  => new Color(1f, 0.92f, 0.2f),
            EColor.Green   => new Color(0.2f, 0.9f, 0.2f),
            EColor.Teal    => new Color(0.1f, 0.9f, 0.8f),
            EColor.Blue    => new Color(0.2f, 0.5f, 1f),
            EColor.Indigo  => new Color(0.3f, 0.1f, 0.9f),
            EColor.Violet  => new Color(0.6f, 0.2f, 1f),
            EColor.Purple  => new Color(0.7f, 0.1f, 0.7f),
            _              => Color.white
        };
    }
}
