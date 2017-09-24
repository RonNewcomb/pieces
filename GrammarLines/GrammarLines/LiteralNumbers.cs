using System;
using System.Collections.Generic;
using System.Linq;

static partial class Program
{
    /// <summary> We can add new suffixes to numbers by using this event systems so new code can "subscribe" to the event of finishing
    /// parsing a number. </summary>
    public static void InitNumberUnitSystem()
    {
        UnitsOfMeasureParsers.Add(IsPercent);
        UnitsOfMeasureParsers.Add(IsOrdinal);
        /*for(long i = 0; i < 23; i++)
            Console.WriteLine("{0}: {1}",i, NumberToSpelledOutOrdinal(i));
        Console.WriteLine("{0}: {1}", 100, NumberToSpelledOutOrdinal(100));
        Console.WriteLine("{0}: {1}", 569, NumberToSpelledOutOrdinal(569));
        Console.WriteLine("{0}: {1}", 1000, NumberToSpelledOutOrdinal(1000));
        Console.WriteLine("{0}: {1}", 2435, NumberToSpelledOutOrdinal(2435));
        Console.WriteLine("{0}: {1}", 200456, NumberToSpelledOutOrdinal(200456));
        Console.WriteLine("{0}: {1}", 213456, NumberToSpelledOutOrdinal(213456));
        Console.WriteLine("{0}: {1}", 654213456, NumberToSpelledOutOrdinal(654213456));
        Console.WriteLine("{0}: {1}", 700654213456, NumberToSpelledOutOrdinal(700654213456));
        Console.WriteLine("{0}: {1}", 103700654213456, NumberToSpelledOutOrdinal(103700654213456));
        Console.WriteLine("{0}: {1}", 999103700654213412, NumberToSpelledOutOrdinal(999103700654213412));
        Console.ReadLine();
        return;*/

    }

    /// <summary>Complish has both "number/numeric" type as well as a distinct "ordinal"</summary>
    public enum NumberSubtype { Cardinal, TentativelyOrdinal, Ordinal, Percent /* other units of measure? */};
    public static int NumberOfNumericSubtypes = 4;
    public static NumberSubtype numtype;
    public static StandardType AsStandardType(this NumberSubtype t)
    {
        if (t == NumberSubtype.Percent) return StandardType.percent;
        if (t == NumberSubtype.Ordinal) return StandardType.position;
        return StandardType.number;
    }

    /// <summary>
    /// Although "one hundred and two" is bad grammar, it appears frequently. But if an And just happens to trail a
    /// number without being part of it, this flags it.
    /// </summary>
    public static bool AteTheWordAnd = false;

    /// <summary>Parses input.  Will return the int representation of the number or ordinal passed in:  34, 81st, 
    /// "one hundred twenty-two", "thirty-seventh", etc. </summary>
    /// <returns>Null if there isn't some sort of number or ordinal at the beginning of the input.</returns>
    public static long? ParseNumberOrdinalPercent(List<string> input)
    {
        numtype = NumberSubtype.Cardinal; // assume Cardinal number until proven otherwise
        AteTheWordAnd = false;
        //input = input.TrimStart(new[] { ' ', '\t', '\n', '-' });
        long? retval = Number(input);
        if (retval != null) return retval;
        return SpelledOutOrdinalToNumber(input);
    }

    static readonly List<string> spelledOutNumbers = new List<string> { "zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine", "ten", "eleven", "twelve", "thirteen", "fourteen", "fifteen", "sixteen", "seventeen", "eighteen", "nineteen" };
    static readonly List<string> spelledOutTens = new List<string> { "zeroes", "ten", "twenty", "thirty", "forty", "fifty", "sixty", "seventy", "eighty", "ninety" };
    static readonly List<string> spelledOutPowersOfTen = new List<string> { "a", "thousand", "million", "billion", "trillion", "quadrillion", "quintillion" };
    static readonly List<string> ordinals = new List<string> { "zeroth", "first", "second", "third", "fourth", "fifth", "sixth", "seventh", "eighth", "ninth", "tenth", "eleventh", "twelfth" };

    /// <summary>Output. Turns a number into a string, with the ordinal suffix -th attached if needed.</summary>
    public static string GetSuffix(long? num)
    {
        switch (numtype)
        {
            case NumberSubtype.Cardinal: return "";
            case NumberSubtype.Ordinal: return th(num ?? 0);
            case NumberSubtype.Percent: return "%";
        }
        return "";
    }

    /// <summary>Output. Chooses and returns the correct suffix for its ordinal parameter.</summary>
    public static string th(long num)
    {   
        string[] ordinalSuffixes = { "th", "st", "nd", "rd" };
        if (num >= 11 && num <= 13) return "th"; // annoying exception
        return (num % 10 <= 3) ? ordinalSuffixes[num % 10] : "th";
    }

    /// <summary>Parses digits in input. Much like Int.Parse except is suffix-aware for ordinals.</summary>
    public static int? Number(List<string> words)
    {
        string input = words[index];
        if (input.Length == 0) return null;
        char ch = input.ElementAt(0);
        if (ch < '0' || ch > '9') return null;
        int i = 0,  retval = ch - '0';
        while (++i < input.Length)
        {
            ch = input.ElementAt(i);
            if (ch < '0' || ch > '9') break;
            retval = (retval * 10) + (ch - '0');
        }
        var unitFound = ParseAnyUnitsOfMeasure(words, i);
        index += (unitFound > 0) ? unitFound : 1; // a plain number has no unit, so we must index++ in that case
        return retval;
    }

    /// <summary> This is the type for delegates that handle the event ParseAnyUnitsOfMeasure </summary>
    /// <param name="input">The input text to parse</param>
    /// <returns>Return zero if no match, or the new position within the input to begin parsing. Be sure to set the 
    /// numtype variable to your type.</returns>
    public delegate int IsUnitOfMeasureSuffix(List<string> input, int letterPosition);

    /// <summary>After a number is read in, this is called to see if there's a suffix after the number identifying it as
    /// a unit of measure. If so, set the variable numtype to your type.</summary>
    /// <param name="input">The input text to parse</param>
    /// <returns>Return zero if no match, or the new position within the input to begin parsing. Be sure to set the 
    /// numtype variable to your type.</returns>
    public static List<IsUnitOfMeasureSuffix> UnitsOfMeasureParsers = new List<IsUnitOfMeasureSuffix>();
    //public static event IsUnitOfMeasureSuffix ParseAnyUnitsOfMeasure;

    /// <summary>Uses every unit-parser in the list UnitsOfMeasureParsers looking for a match to the current input.</summary>
    /// <returns>the new position within the input to begin parsing</returns>
    public static int ParseAnyUnitsOfMeasure(List<string> input, int letterPosition)
    {
        foreach (IsUnitOfMeasureSuffix parser in UnitsOfMeasureParsers)
        {
            int indexMovedBy = parser(input, letterPosition);
            if (indexMovedBy > 0) return indexMovedBy;
        }
        return 0;
    }

    /// <summary>Parses input. One of the unit-of-measure parsers. Of type IsUnitOfMeasureSuffix</summary>
    public static int IsOrdinal(List<string> input, int letterPosition)
    {
        if (input[index].Length >= letterPosition) return 0;
        string suffix = input[index].Substring(letterPosition, 2);
        if (suffix == "st" || suffix == "nd" || suffix == "rd" || suffix == "th")
        {
            numtype = NumberSubtype.Ordinal;
            return 2;
        }
        return 0;
    }

    /// <summary>Parses input. One of the unit-of-measure parsers. Of type IsUnitOfMeasureSuffix</summary>
    public static int IsPercent(List<string> input, int letterPosition)
    {
        if (input[index].Length > letterPosition)
        {
            if (input[index].Substring(letterPosition,1) != "%") 
                return 0;
        }
        else if (index+1 >= input.Count)
            return 0;
        else if (!input[index + 1].StartsWith("percent"))
            return 0;
        numtype = NumberSubtype.Percent;
        return 1; // TODO this is wrong!
    }

    /// <summary> Output. Creates the ordinal words for a passed-in number.</summary>
    public static string NumberToSpelledOutOrdinal(long Nth)
    {
        string answer = NumberToSpelledOutWords(Nth);
        if (answer == "") return "zeroth";
        int i = answer.LastIndexOfAny(new[] { ' ', '-' });
        string lastWord = answer.Substring(i + 1);
        if (spelledOutNumbers.Contains(lastWord))
        {
            int j = spelledOutNumbers.IndexOf(lastWord);
            if (j < 13) return answer.Substring(0, i+1) + ordinals[j];
        }
        else if (spelledOutTens.Contains(lastWord))
        {
            int j = spelledOutTens.IndexOf(lastWord);
            return answer.Substring(0, answer.Length - 2) + "ieth";
        }
        return answer + "th";
    }

    public static string NumberToSpelledOutWords(long Nth)
    {
        if (Nth == 0) return "zero";
        string answer = (Nth < 0) ? " negative" : "";
        int logBase1000 = 6;
        for (long power = 1000000000000000000; power > 0; power /= 1000, logBase1000--)
            if (Nth >= power)
            {
                int smallN = (int)(Nth / power); // should be a number < 1,000
                if (smallN >= 100)
                    answer += " " + spelledOutNumbers[(smallN / 100)] + " hundred";
                smallN %= 100;
                if (smallN >= 20)
                {
                    answer += " " + spelledOutTens[(smallN / 10)];
                    smallN %= 10;
                }
                else
                    smallN %= 20;
                if (smallN > 0)
                    answer += " " + spelledOutNumbers[smallN];
                if (logBase1000 > 0)
                    answer += " " + spelledOutPowersOfTen[logBase1000];
                Nth %= power;
            }
        return answer.Substring(1);
    }

    /// <summary>Parses spelled-out numbers and ordinals like "thirty two thousand four hundred fifty-first"</summary>
    public static long? SpelledOutOrdinalToNumber(List<string> words)
    {
        long? total = null;
        string word = "";
        int i = 0;
        char ch;
        string input = words[index];

        getNewWord:  // I tried a recursive version but it didn't work out.
        AteTheWordAnd = (word == "and"); // initialize & reset
        if (total != null) input = words[index++]; 
        if (numtype == NumberSubtype.TentativelyOrdinal) numtype = NumberSubtype.Ordinal;

        // begin
        i = -1;
        do
        {
            i++;
            ch = input.ElementAt(i);
        } while (ch >= 'a' && ch <= 'z' && i < input.Length - 1);
        if (i < 1)
            return total;
        // if we read it something that wasn't a word, stop already and return what, if anything, we already got.

        word = input.Substring(0, i);
            
        retry: // if none of the defined words match, we'll modify the word suffix and retry. There's 2 strategies.
            
        if (ordinals.Contains(word))
        {
            if (total == null) total = 0;
            total += ordinals.IndexOf(word);
            numtype = NumberSubtype.Ordinal;
            goto getNewWord;
        }
        if (spelledOutNumbers.Contains(word))
        {
            if (total == null) total = 0;
            total += spelledOutNumbers.IndexOf(word);
            goto getNewWord;
        }
        if (spelledOutTens.Contains(word))
        {
            if (total == null) total = 0;
            total += spelledOutTens.IndexOf(word) * 10;
            goto getNewWord;
        }
        if (word == "hundred" || spelledOutPowersOfTen.Contains(word))
        {
            if (total == null) total = 1;
            long power = (word == "hundred") ? 100 : (long)Math.Pow(10, 3 * spelledOutPowersOfTen.IndexOf(word));
            if (total < power)
                total *= power;
            else
            {
                long subhundred = total.Value % 100;
                total = total - subhundred + subhundred * power;
            }
            goto getNewWord;
        }

        // If we reach here, we don't know the word.  
        // Or do we? Perhaps if we played suffix games, we might recognize it after all.
        if (word.EndsWith("tieth"))
        {
            numtype = NumberSubtype.TentativelyOrdinal;
            word = word.Replace("tieth", "ty");
            goto retry;
        }
        if (word.EndsWith("th"))
        {
            numtype = NumberSubtype.TentativelyOrdinal;
            word = word.Substring(0, word.Length - 2);
            goto retry;
        }
        if (word == "and")
            goto getNewWord;

        // If we found at least one numeric word, we keep GOTOing until we eventually finish all the words in the phrase.

        if (AteTheWordAnd)
        {
            index--;            // TODO oops!  backtrack the index over this word!!
        }

        // Maybe it's a unit of measure word? If so, this ends it anyway.
        index += ParseAnyUnitsOfMeasure(words,i);

        if (numtype == NumberSubtype.TentativelyOrdinal) numtype = NumberSubtype.Cardinal;
        return total;
    }

}

