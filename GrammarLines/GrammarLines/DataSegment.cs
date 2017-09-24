using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

static partial class Program
{
    /// <summary> Data lives in one of a few places.  The runtime heap, the runtime stack, or here, 
    /// the permanent data segment which holds the global vars and static vars.  The string table 
    /// is also technically within this segment.</summary>
    public static List<parameter> PermanentDataSegment = new List<parameter>();

    /// <summary>Contains what locals are in scope. Keyed on the First Word of the name of each storage location for quick look-up.</summary>
    static Dictionary<string, List<StackLocation>> scope = new Dictionary<string, List<StackLocation>>();

    internal class StackLocation
    {
        public string name;
        public int depth;
        public parameter inst;
        public StackLocation(string n, int d, parameter i)
        { name = n; depth = d; inst = i; }
    }

    public static int scopeDepth = 0;

    /// <summary>Adds the parameter to scope, for when entering a function.
    /// Handles the case when locals with same-name as globals mask the globals.</summary>
    public static void AddToScope(parameter inst)
    {
        AddSynonymsToScope(inst.name, inst);
        if (inst.name != inst.fullname1)
            AddSynonymsToScope(inst.fullname1, inst);
        if (inst.name != inst.fullname2 && inst.fullname1 != inst.fullname2)
            AddSynonymsToScope(inst.fullname2, inst);
    }

    private static void AddSynonymsToScope(string name, parameter inst)
    {
        var sl = new StackLocation(name, scopeDepth, inst);
        var keyname = name.FirstWord();
        if (scope.ContainsKey(keyname))
            scope[keyname].Add(sl);
        else
            scope.Add(keyname, new List<StackLocation>() { sl });
    }

    /// <summary>Adds the instance to scope. Handles the case when locals with same-name as globals mask the globals.</summary>
    public static void RemoveFromScope(parameter inst)
    {
        RemoveSynonymsFromScope(inst.name, inst);
        if (inst.name != inst.fullname1)
            RemoveSynonymsFromScope(inst.fullname1, inst);
        if (inst.name != inst.fullname2 && inst.fullname1 != inst.fullname2)
            RemoveSynonymsFromScope(inst.fullname2, inst);
    }

    private static void RemoveSynonymsFromScope(string name, parameter inst)
    {
        string word = name.FirstWord();
        if (string.IsNullOrEmpty(word) || !scope.ContainsKey(word))
            return;
        scope[word].RemoveAll(l => l.depth == scopeDepth && l.inst == inst);
        if (!scope[word].Any())
            scope.Remove(word);
    }
    
    // 0) er, traditionally, upon a func call, the previous func's vars temporarily disappear from scope....
    // 1) But didn't an early doc of complish allow referencing anything up the call stack? Meaning, the "current past"?
    // 2) And doesn't the current doc say we can "take" return values from already-returned calls?  The "recent past"?
    // Well, if a func is called from multiple places, the "inherited locals" will be different.

    /// <summary> When parsing the (invocation of) a simple noun, like a variable name or a local parameter name.</summary>
    public static instance MatchToNamedStackLocation(List<string> words)
    {
        string firstWord = words[index];
        if (!scope.ContainsKey(firstWord))
            return null;
        var possibles = scope[firstWord];
        StackLocation bestmatch = null;
        int MostWordsMatched = 0;
        bool conflict = false;
        foreach (StackLocation sl in possibles)
        {
            if (!Program.Match(words, sl.name))
                continue;
            if (Program.MatchCount < MostWordsMatched)
                continue;
            // Accept an unconditional match if 1) it matches the most words, or 2) it matches the same number but is nested deeper (more local)
            if (bestmatch != null) conflict = (MostWordsMatched == Program.MatchCount && bestmatch.depth == sl.depth); // avoid null deref
            if (!conflict)
            {
                MostWordsMatched = Program.MatchCount;
                bestmatch = sl;
            }
        }
        if (conflict)
        {
            Console.WriteLine("  multiple interpretations of the {1} words '{0}'", Program.Say(words, 0, MostWordsMatched), MostWordsMatched);
            return null;
        }
        index += MostWordsMatched;
        return new instance() { var = bestmatch.inst, toAccess = Indirectedness.local_parameter, type = bestmatch.inst.type };
    }

    // Can the local parameters' names be exported to the caller for use after the invocation?  
    //   ... give it to her;
    //   ... examine the thing given;
    // This would be the difference between passing as ref and by value; using the new name points to the changed value if it
    // was changed in the callee.  Of course, this only makes sense with assigned vars, not with bound vars.

    static string FirstWord(this string words)
    {
        if (string.IsNullOrWhiteSpace(words))
            return "";
        string[] ws = words.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        return ws[0];
    }

}
