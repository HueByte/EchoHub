using System.Globalization;
using System.Text;

namespace EchoHub.Client.UI.Helpers;

/// <summary>
/// Converts emoji grapheme clusters to text shortcodes for safe TUI rendering.
/// Terminal width calculations for emoji are unreliable across different terminals,
/// so we replace them with fixed-width ASCII shortcodes for display only.
/// </summary>
public static class EmojiHelper
{
    /// <summary>
    /// Replace emoji graphemes in the text with :shortcode: equivalents.
    /// Non-emoji text passes through unchanged.
    /// </summary>
    public static string ReplaceEmoji(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        // Quick check: if no characters above BMP or supplementary emoji ranges, skip processing
        bool hasEmoji = false;
        foreach (var rune in text.EnumerateRunes())
        {
            if (IsEmojiRune(rune))
            {
                hasEmoji = true;
                break;
            }
        }

        if (!hasEmoji)
            return text;

        var sb = new StringBuilder(text.Length);
        var enumerator = StringInfo.GetTextElementEnumerator(text);

        while (enumerator.MoveNext())
        {
            var grapheme = enumerator.GetTextElement();

            // Check if this grapheme contains emoji runes
            bool graphemeHasEmoji = false;
            foreach (var rune in grapheme.EnumerateRunes())
            {
                if (IsEmojiRune(rune))
                {
                    graphemeHasEmoji = true;
                    break;
                }
            }

            if (graphemeHasEmoji)
            {
                // Try to find a shortcode for the whole grapheme first
                if (EmojiShortcodes.TryGetValue(grapheme, out var shortcode))
                {
                    sb.Append(shortcode);
                }
                else
                {
                    // Try the base emoji (first rune only, stripping modifiers/ZWJ)
                    var baseRune = GetBaseEmoji(grapheme);
                    if (baseRune is not null && EmojiShortcodes.TryGetValue(baseRune, out shortcode))
                    {
                        sb.Append(shortcode);
                    }
                    else
                    {
                        // Unknown emoji — use generic placeholder
                        sb.Append("[emoji]");
                    }
                }
            }
            else
            {
                sb.Append(grapheme);
            }
        }

        return sb.ToString();
    }

    private static bool IsEmojiRune(Rune rune)
    {
        var value = rune.Value;

        // Common emoji ranges
        if (value >= 0x1F600 && value <= 0x1F64F) return true; // Emoticons
        if (value >= 0x1F300 && value <= 0x1F5FF) return true; // Misc Symbols & Pictographs
        if (value >= 0x1F680 && value <= 0x1F6FF) return true; // Transport & Map
        if (value >= 0x1F900 && value <= 0x1F9FF) return true; // Supplemental Symbols
        if (value >= 0x1FA00 && value <= 0x1FA6F) return true; // Chess Symbols
        if (value >= 0x1FA70 && value <= 0x1FAFF) return true; // Symbols Extended-A
        if (value >= 0x2600 && value <= 0x26FF) return true;   // Misc Symbols
        if (value >= 0x2700 && value <= 0x27BF) return true;   // Dingbats
        if (value >= 0xFE00 && value <= 0xFE0F) return true;   // Variation Selectors
        if (value >= 0x200D && value <= 0x200D) return true;   // ZWJ
        if (value >= 0x1F1E0 && value <= 0x1F1FF) return true; // Regional Indicators (flags)
        if (value >= 0x231A && value <= 0x23F3) return true;   // Misc Technical (watch, hourglass)
        if (value >= 0x2934 && value <= 0x2935) return true;   // Arrows
        if (value >= 0x25AA && value <= 0x25FE) return true;   // Geometric Shapes
        if (value >= 0x2B05 && value <= 0x2B55) return true;   // Misc Symbols & Arrows
        if (value >= 0x3030 && value <= 0x303D) return true;   // CJK Symbols
        if (value == 0x00A9 || value == 0x00AE) return true;   // © ®
        if (value == 0x2122) return true;                       // ™
        if (value >= 0x1F000 && value <= 0x1F02F) return true;  // Mahjong & Dominos

        return false;
    }

    /// <summary>
    /// Extract the base emoji string (first non-modifier, non-ZWJ rune) for lookup.
    /// </summary>
    private static string? GetBaseEmoji(string grapheme)
    {
        foreach (var rune in grapheme.EnumerateRunes())
        {
            // Skip ZWJ, variation selectors, skin tone modifiers
            if (rune.Value == 0x200D) continue;
            if (rune.Value >= 0xFE00 && rune.Value <= 0xFE0F) continue;
            if (rune.Value >= 0x1F3FB && rune.Value <= 0x1F3FF) continue;

            if (IsEmojiRune(rune))
                return rune.ToString();
        }

        return null;
    }

    // Common emoji → shortcode mapping (display-only, covers most frequently used emoji)
    private static readonly Dictionary<string, string> EmojiShortcodes = new()
    {
        // Smileys & Emotion
        ["\U0001F600"] = ":grinning:",
        ["\U0001F601"] = ":grin:",
        ["\U0001F602"] = ":joy:",
        ["\U0001F603"] = ":smiley:",
        ["\U0001F604"] = ":smile:",
        ["\U0001F605"] = ":sweat_smile:",
        ["\U0001F606"] = ":laughing:",
        ["\U0001F607"] = ":innocent:",
        ["\U0001F608"] = ":smiling_imp:",
        ["\U0001F609"] = ":wink:",
        ["\U0001F60A"] = ":blush:",
        ["\U0001F60B"] = ":yum:",
        ["\U0001F60C"] = ":relieved:",
        ["\U0001F60D"] = ":heart_eyes:",
        ["\U0001F60E"] = ":sunglasses:",
        ["\U0001F60F"] = ":smirk:",
        ["\U0001F610"] = ":neutral_face:",
        ["\U0001F611"] = ":expressionless:",
        ["\U0001F612"] = ":unamused:",
        ["\U0001F613"] = ":sweat:",
        ["\U0001F614"] = ":pensive:",
        ["\U0001F615"] = ":confused:",
        ["\U0001F616"] = ":confounded:",
        ["\U0001F617"] = ":kissing:",
        ["\U0001F618"] = ":kissing_heart:",
        ["\U0001F619"] = ":kissing_smiling_eyes:",
        ["\U0001F61A"] = ":kissing_closed_eyes:",
        ["\U0001F61B"] = ":stuck_out_tongue:",
        ["\U0001F61C"] = ":stuck_out_tongue_winking_eye:",
        ["\U0001F61D"] = ":stuck_out_tongue_closed_eyes:",
        ["\U0001F61E"] = ":disappointed:",
        ["\U0001F61F"] = ":worried:",
        ["\U0001F620"] = ":angry:",
        ["\U0001F621"] = ":rage:",
        ["\U0001F622"] = ":cry:",
        ["\U0001F623"] = ":persevere:",
        ["\U0001F624"] = ":triumph:",
        ["\U0001F625"] = ":disappointed_relieved:",
        ["\U0001F626"] = ":frowning:",
        ["\U0001F627"] = ":anguished:",
        ["\U0001F628"] = ":fearful:",
        ["\U0001F629"] = ":weary:",
        ["\U0001F62A"] = ":sleepy:",
        ["\U0001F62B"] = ":tired_face:",
        ["\U0001F62C"] = ":grimacing:",
        ["\U0001F62D"] = ":sob:",
        ["\U0001F62E"] = ":open_mouth:",
        ["\U0001F62F"] = ":hushed:",
        ["\U0001F630"] = ":cold_sweat:",
        ["\U0001F631"] = ":scream:",
        ["\U0001F632"] = ":astonished:",
        ["\U0001F633"] = ":flushed:",
        ["\U0001F634"] = ":sleeping:",
        ["\U0001F635"] = ":dizzy_face:",
        ["\U0001F636"] = ":no_mouth:",
        ["\U0001F637"] = ":mask:",
        ["\U0001F641"] = ":slightly_frowning_face:",
        ["\U0001F642"] = ":slightly_smiling_face:",
        ["\U0001F643"] = ":upside_down_face:",
        ["\U0001F644"] = ":roll_eyes:",
        ["\U0001F910"] = ":zipper_mouth:",
        ["\U0001F911"] = ":money_mouth:",
        ["\U0001F912"] = ":thermometer_face:",
        ["\U0001F913"] = ":nerd:",
        ["\U0001F914"] = ":thinking:",
        ["\U0001F915"] = ":head_bandage:",
        ["\U0001F920"] = ":cowboy:",
        ["\U0001F921"] = ":clown:",
        ["\U0001F922"] = ":nauseated:",
        ["\U0001F923"] = ":rofl:",
        ["\U0001F924"] = ":drooling:",
        ["\U0001F925"] = ":lying:",
        ["\U0001F929"] = ":star_struck:",
        ["\U0001F92A"] = ":zany:",
        ["\U0001F92B"] = ":shushing:",
        ["\U0001F92C"] = ":cursing:",
        ["\U0001F92D"] = ":hand_over_mouth:",
        ["\U0001F92E"] = ":vomiting:",
        ["\U0001F92F"] = ":exploding_head:",
        ["\U0001F970"] = ":smiling_face_with_hearts:",
        ["\U0001F971"] = ":yawning:",
        ["\U0001F972"] = ":smiling_with_tear:",
        ["\U0001F973"] = ":partying:",
        ["\U0001F974"] = ":woozy:",
        ["\U0001F975"] = ":hot_face:",
        ["\U0001F976"] = ":cold_face:",
        ["\U0001F979"] = ":holding_back_tears:",
        ["\U0001F97A"] = ":pleading:",
        ["\U0001FAE0"] = ":melting:",
        ["\U0001FAE1"] = ":saluting:",
        ["\U0001FAE2"] = ":face_with_open_eyes_hand_over_mouth:",
        ["\U0001FAE3"] = ":face_with_peeking_eye:",
        ["\U0001FAE4"] = ":face_with_diagonal_mouth:",

        // Gestures
        ["\U0001F44D"] = ":+1:",
        ["\U0001F44E"] = ":-1:",
        ["\U0001F44B"] = ":wave:",
        ["\U0001F44C"] = ":ok_hand:",
        ["\U0001F44F"] = ":clap:",
        ["\U0001F44A"] = ":fist:",
        ["\U0001F91D"] = ":handshake:",
        ["\U0001F91E"] = ":crossed_fingers:",
        ["\U0001F91F"] = ":love_you:",
        ["\U0001F918"] = ":metal:",
        ["\U0001F919"] = ":call_me:",
        ["\U0001F590"] = ":raised_hand:",
        ["\U0001F4AA"] = ":muscle:",
        ["\U0001F926"] = ":facepalm:",
        ["\U0001F937"] = ":shrug:",
        ["\U0001F64F"] = ":pray:",
        ["\U0001F64C"] = ":raised_hands:",
        ["\U0001F64B"] = ":raising_hand:",

        // Hearts & Symbols
        ["\u2764"] = "<3",
        ["\U0001F494"] = "</3",
        ["\U0001F495"] = ":two_hearts:",
        ["\U0001F496"] = ":sparkling_heart:",
        ["\U0001F497"] = ":heartpulse:",
        ["\U0001F498"] = ":cupid:",
        ["\U0001F499"] = ":blue_heart:",
        ["\U0001F49A"] = ":green_heart:",
        ["\U0001F49B"] = ":yellow_heart:",
        ["\U0001F49C"] = ":purple_heart:",
        ["\U0001F49D"] = ":gift_heart:",
        ["\U0001F49E"] = ":revolving_hearts:",
        ["\U0001F49F"] = ":heart_decoration:",
        ["\U0001F90D"] = ":white_heart:",
        ["\U0001F90E"] = ":brown_heart:",
        ["\U0001F5A4"] = ":black_heart:",
        ["\U0001F9E1"] = ":orange_heart:",

        // Objects & Nature
        ["\U0001F525"] = ":fire:",
        ["\U0001F4A9"] = ":poop:",
        ["\U0001F480"] = ":skull:",
        ["\U0001F47B"] = ":ghost:",
        ["\U0001F47D"] = ":alien:",
        ["\U0001F916"] = ":robot:",
        ["\U0001F4AF"] = ":100:",
        ["\U0001F4A5"] = ":boom:",
        ["\U0001F4A4"] = ":zzz:",
        ["\U0001F4A2"] = ":anger:",
        ["\U0001F4AC"] = ":speech_balloon:",
        ["\U0001F440"] = ":eyes:",
        ["\U0001F3B5"] = ":musical_note:",
        ["\U0001F3B6"] = ":notes:",
        ["\U0001F389"] = ":tada:",
        ["\U0001F38A"] = ":confetti:",
        ["\U0001F381"] = ":gift:",
        ["\U0001F3C6"] = ":trophy:",
        ["\U0001F4B0"] = ":money_bag:",
        ["\U0001F4BB"] = ":computer:",
        ["\U0001F4F1"] = ":phone:",
        ["\U0001F4E7"] = ":email:",
        ["\U0001F511"] = ":key:",
        ["\U0001F512"] = ":lock:",
        ["\U0001F513"] = ":unlock:",
        ["\U0001F6A8"] = ":rotating_light:",
        ["\U0001F6AB"] = ":no_entry:",

        // Animals
        ["\U0001F436"] = ":dog:",
        ["\U0001F431"] = ":cat:",
        ["\U0001F42D"] = ":mouse:",
        ["\U0001F430"] = ":rabbit:",
        ["\U0001F43B"] = ":bear:",
        ["\U0001F427"] = ":penguin:",
        ["\U0001F41D"] = ":bee:",
        ["\U0001F40D"] = ":snake:",
        ["\U0001F422"] = ":turtle:",

        // Food & Drink
        ["\U0001F355"] = ":pizza:",
        ["\U0001F354"] = ":hamburger:",
        ["\U0001F37A"] = ":beer:",
        ["\U0001F377"] = ":wine:",
        ["\U0001F370"] = ":cake:",
        ["\u2615"] = ":coffee:",
        ["\U0001F382"] = ":birthday:",

        // Misc symbols (BMP)
        ["\u2705"] = ":white_check_mark:",
        ["\u274C"] = ":x:",
        ["\u274E"] = ":negative_squared_cross_mark:",
        ["\u2714"] = ":heavy_check_mark:",
        ["\u2716"] = ":heavy_multiplication_x:",
        ["\u26A0"] = ":warning:",
        ["\u2B50"] = ":star:",
        ["\u2728"] = ":sparkles:",
        ["\u267B"] = ":recycle:",
        ["\u2611"] = ":ballot_box_with_check:",
        ["\u23F0"] = ":alarm_clock:",
        ["\u231A"] = ":watch:",
        ["\u231B"] = ":hourglass:",
    };
}
