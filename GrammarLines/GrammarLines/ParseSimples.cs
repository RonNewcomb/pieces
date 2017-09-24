using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

static partial class Program
{
    // single words, single punctuation mark, similar bottom-level stuff.

    /// <summary>
    /// Works well, as far as it goes.  It can't do nested lists/sequences, and even if it could, how would such a 
    /// list appear in the source anyhow?  It returns null when not in a list, or Bare if in a list of unknown type.
    /// Also, the either-and catch might need some work in the case of an either-or being used within a sequence.
    /// </summary>
    static ListType? ParseListSeparator(string[] words)
    {
        ListType? thisSeparator = null;
        string word = words[index];
        for (index++; stringnums.ListSeparators.Contains(word); index++)
        {
            switch (word)
            {
                case "then":
                case ",": thisSeparator = ListType.bare; break;
                case "and": thisSeparator = ListType.things; break;
                case "either": thisSeparator = ListType.either; break;
                case "or": thisSeparator = ListType.alternatives; break;
                default: Console.WriteLine("ERROR IN COMPILER ParseListSeparator() #1"); break;
            }
            switch (listType)
            {
                case null: listType = thisSeparator; break;
                case ListType.bare: listType = thisSeparator; break;
                case ListType.alternatives: if (thisSeparator == ListType.things) listType = ListType.sequence; break;
                case ListType.either: if (thisSeparator == ListType.things) listType = ListType.sequence; break;
                case ListType.sequence: listType = thisSeparator; break;
                case ListType.things:
                    if (thisSeparator == ListType.alternatives)
                        listType = ListType.sequence;
                    if (thisSeparator == ListType.either)
                    {
                        Console.WriteLine("Error: An 'either-and' list? Please change 'and' to 'or', or remove the 'either'");
                        method_being_constructed.problems++;
                    }
                    break;
                default: Console.WriteLine("ERROR IN COMPILER ParseListSeparator() #2"); break;
            }
            word = words[index];
            thisSeparator = listType;
        }
        index--;
        return thisSeparator;
    }
    static ListType? listType = null;

    /// <summary>Parses a/an, the, many/multiple/several. Will eat "the many" as "many".</summary>
    static Article? ParseArticle(string[] words)
    {
        Article? found = null;
        string word = words[index];
        index++;
        if (stringnums.IndefiniteArticles.Contains(word)) found = Article.a;
        if (stringnums.DefiniteArticles.Contains(word)) found = Article.the;
        if (stringnums.ListDeterminers.Contains(word)) found = Article.many;
        if (found != null)
        {
            var found2nd = ParseArticle(words);
            return found2nd ?? found; // eat "the many", returning "many"; otherwise just return whatever one word we found
        }
        index--;
        return null;
    }

    /// <summary>Do these words point to any of the types known so far?</summary>
    static StandardType? ParseAnyType(string[] words)
    {
        char[] theSpace = new[] { ' ' };
        foreach (var newType in InheritanceTree)
        {
            string[] terms = newType.name.Split(theSpace, StringSplitOptions.RemoveEmptyEntries);
            int savedIndex = index;
            foreach (string word in terms)
            {
                if (word == words[index++]) continue;
                index = savedIndex;
                break;
            }
            if (index != savedIndex) return newType.typeid;
        }
        return ParseBasicType(words);
    }

    /// <summary>The base types are always a single word, but have two forms, adjective and noun. This parses both.</summary>
    static StandardType? ParseBasicType(string[] words)
    {
        StandardTypeAdjective? TypeAsAdj = Parse<StandardTypeAdjective>(words[index]);
        if (TypeAsAdj != null)
            return (StandardType)TypeAsAdj;
        return Parse<StandardType>(words[index]);
    }

    /// <summary>The base types are always a single word, but have two forms, adjective and noun. This parses the adjective only. See also ParseBasicType.</summary>
    static StandardTypeAdjective? ParseAdjectiveType(string[] words)
    {
        if (stringnums.ListDeterminers.Contains(words[index]))
        {   // to handle all the synonyms for "many"
            index++;
            return StandardTypeAdjective.many;
        }
        return Parse<StandardTypeAdjective>(words[index]);
    }

    /// <summary>Parses for the given Enum, and returns NULL or a value of that enum.</summary>
    /// <typeparam name="T">an Enum</typeparam>
    /// <param name="word">Input in which to look for the value</param>
    /// <returns>A value from the Enum, or null.</returns>
    public static TEnum? Parse<TEnum>(string word) where TEnum : struct, IConvertible, IComparable, IFormattable  /* Enum actually */
    {
        TEnum theEnum;
        if (true != Enum.TryParse<TEnum>(word, true, out theEnum))
            return null;
        index++;
        return theEnum;
    }

    /// <summary>Decides whether the passed-in word is one of the enum values.</summary>
    public static bool Contains<TEnum>(string word) where TEnum : struct, IConvertible, IComparable, IFormattable  /* Enum actually */
    {
        TEnum theEnum;
        return Enum.TryParse<TEnum>(word, true, out theEnum);
    }

    /// <summary>Determines if the multi-word identifer is at the beginning of the words array </summary>
    public static bool Match(List<string> words, string ident)
    {
        int savedIndex = index;
        string[] identWords = ident.Split(' ');
        foreach (var w in identWords)
        {
            if (index >= words.Count || w != words[index])
            {
                index = savedIndex;
                MatchCount = 0;
                return false;
            }
            index++;
        }
        MatchCount = index - savedIndex;
        return true;
    }
    /// <summary>A second output value from Program.Match(), detailing the number of words matched.</summary>
    public static int MatchCount;

    /// <summary>String extension method, for consistency with nullable enums.
    /// If I glance the word "null" in an if-condition, I tend to think that the body handles the null case. It's easy
    /// to miss the != operator.  This method removes the word null from the if-condition that handles the non-null case.</summary>
    public static bool HasValue(this object str)
    {
        return str != null;
    }

    /// <summary>Used to determine if two multimethod signatures are the same. 
    /// Doesn't come out as good English or good code because of the parenthesis.
    /// Intended for signature-to-signature equality comparisons.</summary>
    public static string ToString(this List<term> signature, bool asGerund)
    {
        string retval = asGerund ? "(" : "";
        foreach (term term in signature)
        {
            switch (term.which)
            {
                case PartsOfSpeech.preposition:
                    retval += " " + term.preposition;
                    break;
                case PartsOfSpeech.noun:
                    retval += term.noun.isAggregate ? " " + term.noun.relation.ToString(true) : " " + term.noun.name;
                    break;
                case PartsOfSpeech.verb:
                    retval += " " + (asGerund ? term.verb._ing : term.verb.plural);
                    break;
            }
        }
        return (retval + (asGerund ? ")" : "")).Trim();
    }

}
