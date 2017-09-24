using System;
using System.Collections.Generic;
using System.Linq;

namespace OrdinalWordsToNumber
{
    class Program
    {
        public static string input;

        static void Main(string[] args)
        {
            InitNumberUnitSystem();
            while ((input = Console.ReadLine()) != null)
            {
                input = input + " ENDLINE";
                long? compiled = OrdinalToNumber(input);
                if (compiled == null)
                    Console.WriteLine("NaN");
                else
                    Console.WriteLine("{0}{1}", compiled, GetSuffix(compiled));
            }
        }

        /// <summary> We can add new suffixes to numbers by using this event systems so new code can "subscribe" to the event of finishing
        /// parsing a number. </summary>
        public static void InitNumberUnitSystem()
        {
            ParseAnyUnitsOfMeasure += IsPercent;
            ParseAnyUnitsOfMeasure += IsOrdinal;
        }

        /// <summary>Complish has both "number/numeric" type as well as a distinct "ordinal"</summary>
        public enum NumberSubtype { Cardinal, TentativelyOrdinal, Ordinal, Percent /* other units of measure? */};
        public static uint NumberOfNumericSubtypes = 4;
        public static NumberSubtype numtype;

        /// <summary>
        /// Although "one hundred and two" is bad grammar, it appears frequently. But if an And just happens to trail a
        /// number without being part of it, this flags it.
        /// </summary>
        public static bool AteTheWordAnd = false;

        /// <summary>Parses input.  Will return the int representation of the number or ordinal passed in:  34, 81st, 
        /// "one hundred twenty-two", "thirty-seventh", etc. </summary>
        /// <returns>Null if there isn't some sort of number or ordinal at the beginning of the input.</returns>
        public static long? OrdinalToNumber(string input)
        {
            numtype = NumberSubtype.Cardinal; // assume Cardinal number until proven otherwise
            AteTheWordAnd = false;
            input = input.TrimStart(new[] { ' ', '\t', '\n', '-' });
            long? retval = Number(input);
            if (retval != null) return retval;
            return SpelledOutOrdinalToNumber(input);
        }

        static List<string> spelledOutNumbers = new List<string> { "zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine", "ten", "eleven", "twelve", "thirteen", "fourteen", "fifteen", "sixteen","seventeen", "eighteen", "nineteen" };
        static List<string> spelledOutTens = new List<string> { "zeroes", "ten", "twenty", "thirty", "forty", "fifty", "sixty", "seventy", "eighty", "ninety" };
        static List<string> spelledOutPowersOfTen = new List<string> { "a", "thousand", "million", "billion", "trillion", "quadrillion", "quintillion" };
        static List<string> ordinals = new List<string> { "zeroth", "first", "second", "third", "fourth", "fifth", "sixth", "seventh", "eighth", "ninth", "tenth", "eleventh", "twelfth" };

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
        public static int? Number(string input)
        {
            int index = -1, retval = 0;
            char ch = '0';
            do
            {
                index++;
                retval = (retval * 10) + (ch - '0');
                ch = input.ElementAt(index);
            } while (ch >= '0' && ch <= '9');
            if (index < 1) return null;
            
            index += ParseAnyUnitsOfMeasure(input.Substring(index));

            // if not whitespace at the index, throw error. Or, a unit of measure is attached, like 50% or 50hz
            return retval;
        }

        /// <summary> This is the type for delegates that handle the event ParseAnyUnitsOfMeasure </summary>
        /// <param name="input">The input text to parse</param>
        /// <returns>Return zero if no match, or the new position within the input to begin parsing. Be sure to set the 
        /// numtype variable to your type.</returns>
        public delegate int IsUnitOfMeasureSuffix(string input);

        /// <summary>After a number is read in, this is called to see if there's a suffix after the number identifying it as
        /// a unit of measure. If so, set the variable numtype to your type.</summary>
        /// <param name="input">The input text to parse</param>
        /// <returns>Return zero if no match, or the new position within the input to begin parsing. Be sure to set the 
        /// numtype variable to your type.</returns>
        public static event IsUnitOfMeasureSuffix ParseAnyUnitsOfMeasure;

        /// <summary>Parses input. One of the unit-of-measure parsers. Of type IsUnitOfMeasureSuffix</summary>
        public static int IsPercent(string input)
        {
            string suffix = input.Substring(0, 2);
            if (suffix == "st" || suffix == "nd" || suffix == "rd" || suffix == "th")
            {
                numtype = NumberSubtype.Ordinal;
                return 2;
            }
            return 0;
        }

        /// <summary>Parses input. One of the unit-of-measure parsers. Of type IsUnitOfMeasureSuffix</summary>
        public static int IsOrdinal(string input)
        {
            input = input.Trim();
            if (input.StartsWith("%") || input.StartsWith("percent"))
            {
                numtype = NumberSubtype.Percent;
                return 1; // TODO this is wrong!
            }
            return 0;
        }


        /// <summary>Parses spelled-out numbers and ordinals like "thirty two thousand four hundred fifty-first"</summary>
        public static long? SpelledOutOrdinalToNumber(string input)
        {
            long? total = null;
            string word = "";
            int index = 0;
            char ch;

            getNewWord:  // I tried a recursive version but it didn't work out.
            AteTheWordAnd = (word == "and"); // initialize & reset
            if (total != null) input = input.Substring(index); 
            if (numtype == NumberSubtype.TentativelyOrdinal) numtype = NumberSubtype.Ordinal;

            // begin
            input = input.TrimStart(new[]{' ','\t','\n','-'});
            index = -1;
            do
            {
                index++;
                ch = input.ElementAt(index);
            } while (ch >= 'a' && ch <= 'z');
            if (index < 1) 
                return total;
            // if we read it something that wasn't a word, stop already and return what, if anything, we already got.

            word = input.Substring(0, index);
            
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
                // TODO oops!  backtrack the index over this word!!
            }

            // Maybe it's a unit of measure word? If so, this ends it anyway.
            index += ParseAnyUnitsOfMeasure(input);

            if (numtype == NumberSubtype.TentativelyOrdinal) numtype = NumberSubtype.Cardinal;
            return total;
        }

    }
}
