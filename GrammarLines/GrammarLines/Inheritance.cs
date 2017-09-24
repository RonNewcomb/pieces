using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

static partial class Program
{
    /// <summary>Creates a new type defined by the user, typically of type 'object'.</summary>
    public static InheritedType CreateNewType(string ident, StandardType parentClass = StandardType.heterogene)
    {
        return CreateNewType(ident, parentClass.ToString());
    }

    /// <summary>Creates a new type defined by the user, as a subclass of another user-defined type.</summary>
    public static InheritedType CreateNewType(string ident, string parentClassName)
    {
        if (string.IsNullOrWhiteSpace(parentClassName)) return CreateNewType(ident);
        var newType = new InheritedType() { name = ident, parent = parentClassName, typeid = (StandardType)(InheritanceTree.Count + 2) };
        InheritanceTree.Add(newType);
        just_declared_new_type = true;
        return newType;
    }

    /// <summary> Creates the lookup table for what types inherit from what. This would be hard-coded if C# would allow. </summary>
    public static void InitInheritanceTree()
    {
        for (StandardType st = StandardType.anything; st < StandardType.Count; st++)
        {
            var it = new InheritedType() { name = st.ToString(), subtypes = null, typeid = st };
            switch (st)
            {
                case StandardType.anything: it.parent = null; break;
                case StandardType.something:
                case StandardType.nothing: it.parent = StandardType.anything.ToString(); break;
                case StandardType.valueType:
                case StandardType.referenceType: it.parent = StandardType.something.ToString(); break;
                case StandardType.number:
                case StandardType.discrete:
                case StandardType.reference:
                case StandardType.structure: it.parent = StandardType.valueType.ToString(); break;
                case StandardType.homogene:
                case StandardType.heterogene:
                case StandardType.value: it.parent = StandardType.referenceType.ToString(); break;
                case StandardType.percent:
                case StandardType.money: it.parent = StandardType.number.ToString(); break;
                case StandardType.position:
                case StandardType.boole: it.parent = StandardType.discrete.ToString(); break;
                case StandardType.time: it.parent = StandardType.structure.ToString(); break;
                case StandardType.text:
                case StandardType.list: it.parent = StandardType.homogene.ToString(); break;
                case StandardType.sequence:
                case StandardType.Object: it.parent = StandardType.heterogene.ToString(); break;
                default: Console.WriteLine("ERROR IN COMPILER: unknown base type {0}", st); break;
            }
            InheritanceTree.Add(it);
        }
        //TestInheritanceTree();
    }

    /*public static void TestInheritanceTree()
    {
        IsA(StandardType.number, StandardType.number);
        Console.WriteLine("Is {0} a {1}? {2}.", StandardType.number, StandardType.number, (distance > -1));
        IsA(StandardType.number, StandardType.valueType);
        Console.WriteLine("Is {0} a {1}? {2}.", StandardType.number, StandardType.valueType, (distance > -1));
        IsA(StandardType.Object, StandardType.anything);
        Console.WriteLine("Is {0} a {1}? {2}.", StandardType.Object, StandardType.anything, (distance > -1));
        IsA(StandardType.anything, StandardType.anything);
        Console.WriteLine("Is {0} a {1}? {2}.", StandardType.anything, StandardType.anything, (distance > -1));
        IsA(StandardType.text, StandardType.valueType);
        Console.WriteLine("Is {0} a {1}? {2}.", StandardType.text, StandardType.valueType, (distance > -1));
        IsA(StandardType.text, StandardType.reference);
        Console.WriteLine("Is {0} a {1}? {2}.", StandardType.text, StandardType.reference, (distance > -1));
        IsA(StandardType.number, StandardType.percent);
        Console.WriteLine("Is {0} a {1}? {2}.", "number", "percent", (distance > -1));
        IsA(StandardType.percent, StandardType.number);
        Console.WriteLine("Is {0} a {1}? {2}.", "percent", "number", (distance > -1));
        IsA(StandardType.number, StandardType.anything);
        Console.WriteLine("Is {0} a {1}? {2}.", "number", "anything", (distance > -1));
        IsA(StandardType.number, StandardType.something);
        Console.WriteLine("Is {0} a {1}? {2}.", "number", "something", (distance > -1));
        /*IsA(StandardType.money, StandardType.car);
        Console.WriteLine("Is {0} a {1}? {2}.", "money", "car", (distance > -1));
        IsA("car", "object");
        Console.WriteLine("Is {0} a {1}? {2}.", "car", "object", (distance > -1));
        IsA("car", "something");
        Console.WriteLine("Is {0} a {1}? {2}.", "car", "something", (distance > -1));
    }*/

    /// <summary>Returns the name of the given StandardType, including newly-defined types in the source. </summary>
    public static string NameOfType(StandardType? s)
    {
        if (s == null) return "whatever";
        var st = s.Value;
        if (st <= StandardType.anything) return StandardType.anything.ToString();
        if (st < StandardType.Count) return st.ToString();
        InheritedType it = InheritanceTree.Find(item => item.typeid == st);
        return it.name;
    }

    /// <summary>Searches for constructions of the form "the TYPE which", as these indicate the beginning of a relative clause.
    /// Use only when parsing invocations, not definitions. </summary>
    public static theTypeWhich IsNameOfTypeWhich(List<string> words)
    {
        int savedIndex = index;
        theTypeWhich theType = new theTypeWhich { preposition = "" };
        theType.type = InheritanceTree.SingleOrDefault(itype => Match(words, itype.name));
        if (theType.type == null || index >= words.Count)
        {
            index = savedIndex;
            return null;
        }
        if (Contains<RelativePronouns>(words[index]))
        {
            index++;
            return theType;
        }
        if (stringnums.prepositions.Contains(words[index]) && Contains<RelativePronouns>(words[index + 1]))
        {
            theType.preposition = words[index];
            index += 2;
            return theType;
        }
        index = savedIndex;
        return null;
    }

    /// <summary>Returns the parent of the passed-in type. Returns Anything for unrecognized types as well as Anything
    /// itself.  Returns Object (which is assumed) for types which claim to have no parent.  (Those were implicitly 
    /// created by a "list of" construction, and will presumably be filled in later.) </summary>
    public static StandardType ParentOf(StandardType st)
    {
        if (st == StandardType.anything) return StandardType.anything; // anything is anything, the root type
        InheritedType it = InheritanceTree.Find(item => item.typeid == st);
        if (it == null) return StandardType.anything;
        if (it.parent == null) return StandardType.Object; // only happens with used but undefined types
        return InheritanceTree.Find(item => item.name == it.parent).typeid;

    }

    /// <summary>Answers whether the first instance, variable, or type is-a the second type.  Single-inheritance. </summary>
    static bool IsA(StandardType instanceOrType, StandardType type)
    {
        distance = DistanceFrom(instanceOrType, type);
        return (distance > -1); // Distance of 0 means same exact type. Distance of 1 means one's a direct parent, etc.
    }

    /// <summary>Asking if Type X is-a X is a distance of 0, is-a parent-of-X is a distance of 1, etc.  -1 means X isn't a Y</summary>
    static int distance = -1;

    /// <summary>Given a method signature and the types of the values being fed to it, this calculates how close of a match the values are for
    /// that signature.  It's used during multi-method dispatch when multiple methods can take that set of inputs to choose the 
    /// best match for those inputs.</summary>
    public static long Distance(multimethod method, List<instance> inputs)
    {
        if (inputs.Count == 0) return 0; // hopefully the signature's verb and the used verb match. If not, why call this function?
        long summation = 0;
        int i = 0;
        int squareme;
        foreach (term term in method.signature)
        {
            if (term.which != PartsOfSpeech.noun) continue;
            if (i >= inputs.Count) return 999998;
            squareme = DistanceFrom(inputs[i].type, term.noun.type);
            if (squareme < 0) return 1000000; // the inputs don't even match the signature.  
            summation += squareme * squareme;
            i++;
        }
        return summation;
    }

    /// <summary>Calculates if the child is the same type as parent (zero), or how many levels of inheritance it's 
    /// removed from.  Returns -1 if the child isn't a subclass of (or equal to) the parent.</summary>
    public static int DistanceFrom(StandardType child, StandardType parent = StandardType.anything)
    {
        return DistanceFrom(NameOfType(child), NameOfType(parent));
    }

    /// <summary>Calculates if the child is the same type as parent (zero), or how many levels of inheritance it's 
    /// removed from.  Returns -1 if the child isn't a subclass of (or equal to) the parent.</summary>
    public static int DistanceFrom(string child, string parent = "anything")// StandardType st, StandardType from = StandardType.anything)
    {
        int distance;
        for (distance = 0; child != null && child != parent; distance++)
            child = InheritanceTree.Find(item => item.name == child).parent;
        if (child != parent) distance = -1;
        return distance; // Distance of 0 means same exact type. Distance of 1 means one's a direct parent, etc.
    }

}