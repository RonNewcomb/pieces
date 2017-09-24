using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

enum StandardType { anything = 1, nothing, something, valueType, referenceType, number, percent, money, discrete, boole, position, structure, time, reference, value, homogene, text, list, heterogene, sequence, Object,   /* and always leave as last */ Count };
enum StandardTypeAdjective { any = 1, no, something, valueType, referenceType, numeric, percentage, monetary, distinct, boolean, ordinal, structured, temporal, referenced, valued, homogeneous, textual, many, heterogeneous, sequential, Objective,/* and always leave as last */ Count };
enum StandardTypePlural { anythings = 1, nothings, somethings, valueType, referenceType, numbers, percentages, monies, discretion, booles, positions, structures, times, references, values, homogenes, texts, lists, heterogenes, sequences, Objects,  /* and always leave as last */ Count };
enum OtherParameterTypes { anything = 1, nothing, something }; // "nothing blue" == "something not-blue"; "Nothing's better than world peace" == "world peace is better than everything else"
enum Article { the, a, many };
enum Inflections { singular, plural, past, present_participal, past_participal, root, infinitive }; // "is" has 2 extra forms, "am" plus singular/plural past: "was/were"
enum ListType { bare /*unknown, just commas used*/, things /*and*/, alternatives /*or*/, sequence /*mixed*/, either };
/// <summary>In a multimethod's Signature like "give something to someone" there are two parameters but four terms: verb, noun, preposition, noun. Nouns are parameters, but the others are not. </summary>
enum PartsOfSpeech { preposition, verb, noun }; // parts of speech of a method signature
/// <summary>"give me liberty" vs. "give liberty to me": the first has 2 parameters without any preposition since the direct object and indirect object swapped places.</summary>
enum DirectObjectPlacement { not_there_yet, normal, after_the_indirect_object, error }; 
enum RelativePronouns { that, which, who, whom };

class stringnums // enum + string = stringnum
{
    public static string[] EndsInfinitiveDefinition = { ",", ":", "means", "?", "when", /* or any imperative verb, as it begins a clause */};
    public static string[] ListSeparators = { ",", "and", "or", "either", "then" };
    public static string[] IndefiniteArticles = { "a", "an", "another" };
    public static string[] ListDeterminers = { "multiple", "many", "several" };
    public static string[] DefiniteArticles = { "the" };
    public static string[] prepositions = { "to", "for", "as", "at", "by", "of", "off", "on" };
    public static string[] InflectionsOfIs = { "is", "are", "was", "being", "been", "am", "be", "were" };
    public static string[] EOL = { ".", "!" }; // but not ? because an answer is expected immediately afterward
    public static string[] QuestionWords = { "what", "when", "which", "where", "would", "could", "should", "can", "will", "shall" };
    public static string[] RelativePronouns = { "that", "which", "who", "whom" };
}

/// <summary>Holds all verbs with all five unique verb forms.  Mostly for testing.  A real implementation would optimize for thousands of verbs.</summary>
class fiveforms : IComparable
{
    public string singular;
    public string plural;
    public string past;
    public string _ing;
    public string _en;
    public fiveforms(string s, string p, string pst, string ing, string en)
    { singular = s; plural = p; past = pst; _ing = ing; _en = en; }
    int IComparable.CompareTo(object f2) { return singular.CompareTo((f2 as fiveforms).singular); }
    public bool Contains(string verb)
    {
        if (verb == singular) return true;
        if (verb == plural) return true;
        if (verb == past) return true;
        if (verb == _ing) return true;
        if (verb == _en) return true;
        return false;
    }
}

/// <summary>
/// This class remembers what a type's name and parent class are. New types defined in a source are appended here.
/// "Subtypes" is, for aggregate types like List, a name of the subtype -- or subtypes, semicolon-separated, as
/// appropriate.  A "list of XXX" is an aggregate, but an object like "car" is not, because the subtypes inside
/// Car aren't listed by its name.</summary>
class InheritedType
{
    public string name;
    public string parent;
    public string subtypes;
    public StandardType typeid;
}

/// <summary>This little construction marks the beginning of a relative clause. "..the Number to which..."</summary>
class theTypeWhich
{
    //public Article art;               // "the"
    public InheritedType type;          // "number"
    public string preposition;          // "to"
    //public string relativePronoun;    // "which.."
    public string say() { return "the " + type.name + ((preposition != "") ? " " + preposition : "") + " which"; }
}

/// <summary> Contains the information for a function parameter, and the names the parameter goes by within the function. 
/// The "StandardType" type acts like a pointer to an InheritedType. This also accepts relationships of types, such as
/// functions, relations, structs, etc., if the parameter is used within the definition of a higher-order function.</summary>
class parameter
{
    public string preposition;
    public string name;
    public Article art;
    public StandardType type;
    public string fullname1;
    public string fullname2;
    public int position;
    public bool identIsAdjectiveTypeIsNoun;
    public parameter(string p, string n, Article a, StandardType t, bool identIsAdj)
    {
        preposition = p; art = a; type = t; fullname1 = null; fullname2 = null; position = 1; identIsAdjectiveTypeIsNoun = identIsAdj;
        name = (identIsAdj) ? (n + " " + t) : (n);
    }

    // the below is for higher-order functions, whose nouns (parameters) are verbs
    public List<term> relation = null;
    public bool isAggregate { get { return relation != null; } }
    /// <summary>Invalid except for isAggregate == true</summary>
    public int prompt;
    public parameter(multimethod subfunction)
    {
        prompt = subfunction.prompt;
        relation = subfunction.signature;
        foreach (var term in relation)
            if (term.which == PartsOfSpeech.verb)
                name = term.verb._ing;
        fullname1 = name;
        fullname2 = name;
    }
}

/// <summary> This informs the assembly language with the distinction of where a value is coming from: 
/// literal which is inlined in the assembly code, 
/// local variable which is on the stack,
/// global which is in the PermanentDataSegment,
/// unbound, meaning we're currying a function and this will be a parameter in the new function,
/// or is the intermediate result returned from an invocation, so may be in a register or whereever.</summary>
enum Indirectedness { unbound, literal, global, local_parameter, nested_invocation }

/// <summary> An instance is a value of a particular type. Usually the term is reserved for objects -- an instance of a class -- but is 
/// used here to refer to values of any type.  5 is an instance of Number, for example. The toAccess property is of type 
/// Indirectedness, an enum explaining if this instance is a literal, like 5 or "hi world", or is in a local variable, etc. </summary>
class instance
{
    public parameter var;
    public Indirectedness toAccess; // literal? get it from a storage location? instanced?
    public long literalValue; // Indirectness.literal, when var.type = number.  Not necessarily a long.
    public string literalString; // Indirectness.literal, when var.type = string
    public invocation inner; // Indirectness.nested_invocation, when var.type = ..invocation
    public StandardType type; // of the provided value -- not necessarily what the signature expects
    /*public int creates;
    public int reads;
    public int updates;
    public int deletes;*/
}

/// <summary>The body of any function/multimethod is a simple list of Invocations. An invocation binds arguments to parameters.</summary>
class invocation
{
    public theTypeWhich which;
    public multimethod definition;
    public List<instance> boundParameters;
    public int startWord;
    public int endWord;
    public invocation(multimethod d, int from, int to, List<instance> bound)
    { definition = d; startWord = from; endWord = to; boundParameters = bound; }
}

/// <summary>The function signature "give something to someone" contains four terms -- verb, noun, preposition, noun -- two of which
/// are also Parameters (the nouns). The property "which" is an enum stating which other propery -- noun, verb, preposition -- is valid.
/// So, a list of terms define a multimethod's Signature.</summary>
class term
{
    public PartsOfSpeech which;
    public string preposition;
    public fiveforms verb;
    public parameter noun;
    public term(string prep) { which = PartsOfSpeech.preposition; preposition = prep; }
    public term(fiveforms v) { which = PartsOfSpeech.verb; verb = v; }
    public term(parameter p, string prepositions) { which = PartsOfSpeech.noun; noun = p; noun.preposition = prepositions; }
    public override string ToString()
    {
        switch (which)
        {
            case PartsOfSpeech.preposition: return preposition;
            case PartsOfSpeech.verb: return verb.singular;
            case PartsOfSpeech.noun: 
                return noun.isAggregate ? noun.relation.ToString(true).Replace("(","").Replace(")","") : "a " + noun.name;
        }
        return "???";
    }
}

/// <summary> This remembers a single method's Signature (List of Terms), and the method's Body as, first, an unparsed String, then
/// later as parsed_body (List of Invocations). </summary>
class multimethod
{
    /// <summary>For a function defined as "Someone gives something to someone", there are three parameters (noun phrases)
    /// but five terms (when you include the verb 'gives' and the preposition 'to').</summary>
    public List<term> signature = new List<term>();
    public List<string> body;
    public List<invocation> parsed_body;
    public int prompt = 0;
    public int problems = 0;
    public int todo = 0;
    public bool fully_parsed = false;
    public long distance = 0;
    public int matchingTerms = 0;
    public int called = 0;
    //public string returning; // question word(s)
    public string question;
}


