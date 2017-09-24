using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

static partial class Program
{
    /// <summary> big list! </summary>
    static List<fiveforms> verbForms = new List<fiveforms>() 
        { 
            new fiveforms("is", "are", "was", "being", "been"), 
            new fiveforms("am", "be", "were", "being", "been"), // hack!!
        };

    /// <summary>Given a string containing a verb, finds the verb struct for it which has all forms.
    /// Only accepts singular and plural forms.</summary>
    static fiveforms FindMainVerb(string verb)
    {
        if (verb.EndsWith("ing")) // all _ing forms end in -ing, no exceptions.
            return null;
        foreach (fiveforms forms in verbForms) // could sort verbForms for performance
        {
            if (verb == forms.singular) return forms;
            if (verb == forms.plural) return forms;
        }
        return null;
    }

    /// <summary>Given a string containing a verb, finds the verb struct for it which has all forms.
    /// Only accepts singular and plural forms.</summary>
    static fiveforms PresentParticiple(string verb)
    {
        if (verb.EndsWith("ing")) // all _ing forms end in -ing, no exceptions.
        {
            foreach (fiveforms forms in verbForms)
                if (verb == forms._ing) 
                    return forms;
        }
        return null;
    }

    /// <summary>Given a string containing a verb, finds the verb struct for it which has all forms.
    /// Can pass any form into it.</summary>
    static fiveforms FindVerb(string verb)
    {
        if (verb.EndsWith("ing")) // all _ing forms end in -ing, no exceptions.
        {
            foreach (fiveforms forms in verbForms)
                if (verb == forms._ing) return forms;
        }
        foreach (fiveforms forms in verbForms) // could sort verbForms for performance
        {
            if (verb == forms.singular) return forms;
            if (verb == forms.plural) return forms;
            if (verb == forms.past) return forms;
            if (verb == forms._en) return forms;
        }
        return null;
    }

    /// <summary>Stub. Get the proper inflection for the passed-in verb, which is presumably in its root form.</summary>
    static string FormOf(Inflections inflection, string verb)
    {
        // first of all, for the verb "is", handle it separately
        // (note that "is" does exist in the FindVerb list as the first element, but it's obviously missing a few forms) 
        if (stringnums.InflectionsOfIs.Contains(verb))
        {
            switch (inflection)
            {
                case Inflections.root: return "were";         // whatever
                case Inflections.infinitive: return "to be";
                case Inflections.singular: return "is";
                case Inflections.plural: return "are";
                case Inflections.past: return "was"; // one of them, anyway
                case Inflections.present_participal: return "being";
                case Inflections.past_participal: return "been";
                default: return "am";
            }
        }

        // second, is it one of the irregular ones that have all five forms?
        fiveforms f5 = FindVerb(verb);
        if (f5 != null)
            switch (inflection)
            {
                case Inflections.singular: return f5.singular;
                case Inflections.plural: return f5.plural;
                case Inflections.past: return f5.past;
                case Inflections.present_participal: return f5._ing;
                case Inflections.past_participal: return f5._en;
            }

        // for now, make stuff up
        switch (inflection)
        {
            case Inflections.root: return verb;                 // used for the imperative invocation
            case Inflections.infinitive: return "to " + verb;   // used for the infinitive definition
            case Inflections.singular: return verb + "s";       // used for relations
            case Inflections.plural: return verb;               // used for relations
            case Inflections.past: return verb + "ed";
            case Inflections.present_participal: return verb + "ing";// used for continuous tense & for gerund (noun phrase)
            case Inflections.past_participal: return verb + "n";        // used for perfect tense & for adjective
        }
        return verb;
    }

    /// <summary>Initializes data.</summary>
    static void ReadForms()
    {
        string homefolder = @"../../";
        foreach (string line in File.ReadLines(homefolder + "5forms.txt"))
        {
            string[] forms = line.Split(' ');
            verbForms.Add(new fiveforms(forms[0], forms[1], forms[2], forms[3], forms[4]));
        }
    }

}
