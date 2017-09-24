using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.IO;

static partial class Program
{
    /// <summary>This marks where we are in the user's source code. It points to the next UNrecognized word, NOT to the last recognized word.</summary>
    static int index = 0; 
    /// <summary>All your functions are belong to this. Or will, once the first pass of the user's source is complete. 
    /// The second pass doesn't alter this, but instead parses the bodies of the mms in here.
    /// Eventually, this becomes an abstract syntax tree.</summary>
    static List<multimethod> multimethods = new List<multimethod>();
    static multimethod method_being_constructed = null;
    static string definedVerb = "";
    static fiveforms incarnations;
    static bool isQuestion;
    static bool just_declared_new_type; // to catch some user mistakes
    static string preposition = "";
    static int subjectAt = -1;

    /// <summary>Handy for quick-reference. There's several variations of the verb to be.
    /// 1) A-is-a defines object inheritance, which is a child-to-parent relationship.
    /// 2) The-is-a defines a global instance or variable. which is an example-to-definition relationship.
    /// 3) A-is-a-and-a defines a object (struct), which is a whole-to-parts relationship; the has-a relationship of OOP.
    /// 4) A-is-a-or-a defines an enum, which is a peer-to-peer relationship.</summary>
    static fiveforms TheVerbIs;

    /// <summary>This is the string table. It'll be outputted to the runtime almost as-is.
    /// The runtime puts the length of the string in front of it.</summary>
    static List<string> literalStrings = new List<string>();

    /// <summary>This holds the inheritance tree and other details of all types.</summary>
    static List<InheritedType> InheritanceTree = new List<InheritedType>();

    /// <summary>This holds all the defined verbs, and links to all the multimethods each verb can indicate.</summary>
    static SortedDictionary<fiveforms, List<multimethod>> mms = new SortedDictionary<fiveforms, List<multimethod>>();

    /// <summary> Most parse routines return something different -- the type of whatever they parse.</summary>
    delegate T ParseRoutine<T>(string[] words);
    delegate dynamic ParseSomething(string[] words);

    static int prompt = 1, subprompt, subsubprompt; // the sentence#, invocation#, and relative clause #

    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    static void Main(string[] args)
    {
        ReadForms();
        TheVerbIs = FindVerb("is");
        InitNumberUnitSystem();
        InitInheritanceTree();

        string input = null;
        string[] words = null;
        int maxWords;
        TestHarness.loadTestScript("me");
        for (Console.Write("{0}: ", prompt); (input = TestHarness.LineFeeder()) != null; TestHarness.EndTurn(2), Console.Write("{0}: ", ++prompt))
        {
            TestHarness.BeginTurn();

            // Move string literals into the string table, leaving placeholders behind.
            while (input.Contains('"'))
            {
                int from = input.IndexOf('"');
                int endAt = from + 1;
            loopPastDquoteLiteral:
                endAt += input.Substring(endAt).IndexOf('"');
                if (input.Length > endAt + 1 && input.ElementAt(endAt + 1) == '"')
                {
                    endAt += 2;
                    goto loopPastDquoteLiteral;
                }
                int len = endAt - (from + 1);
                string literal = input.Substring(from + 1, len);
                input = input.Substring(0, from - 1) + String.Format(" __litstring{0}__ ", literalStrings.Count) + input.Substring(endAt+1);
                literalStrings.Add(literal.Replace("\"\"", "\""));
            }

            // cleanse the input
            input = input.ToLowerInvariant() + " ";
            input = input.Replace(", ", " , ").Replace(". ", " . ").Replace(": ", " : ").Replace("? ", " ? ");
            input = input + " " + stringnums.EOL[0];
            words = input.Split(new[] { ' ', '\t', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            maxWords = (int)words.Count();
            if (maxWords < 2) break; // EOL is always appended

            // debugging / testharness feature: input a line number 
            if (maxWords == 2 && Int32.TryParse(words[0], out subjectAt))
            {
                Console.WriteLine(TestHarness.testlines[--subjectAt * 3]);
                Console.WriteLine(TestHarness.testlines[subjectAt * 3 + 1]);
                Console.WriteLine(TestHarness.testlines[subjectAt * 3 + 2]);
                continue;
            }

            // meta / macro command: tell the compiler to run another file. 
            if (maxWords == 3 && words[0] == "regard" && words[1].StartsWith("__litstring"))
            {
                TestHarness.loadTestScript(Say(words[1]));
                continue;
            }

            // initialize vars
            index = 0;
            definedVerb = "";
            preposition = "";
            method_being_constructed = new multimethod() { prompt = prompt, question = "" };
            //method_being_constructed.prompt = prompt;
            subjectAt = -1;
            isQuestion = false;
            incarnations = null;
            listType = null;
            IdentEndsAt = maxWords;
            just_declared_new_type = false;

            // Absorb any prepositions at the beginning, as in "To give..." or "To boldly go..." or "To a person give...", etc.
            while (stringnums.prepositions.Contains(words[index]))
            {
                if (preposition != "") preposition += " ";
                preposition += words[index++];
            }

            // if the first word(s) indicate a question, read it.
            //method_being_constructed.question = "";
            /*if (stringnums.QuestionWords.Contains(words[index]))
            {
                string question = "";
                while (stringnums.QuestionWords.Contains(words[index]))
                {
                    if (question != "") question += " ";
                    question += words[index++];
                }
                incarnations = FindVerb(words[index]); // Catches "What is.." but not "Which car is..."
                method_being_constructed.question = question;
                isQuestion = true;
            }*/

            // if first word(s) indicated a subject, read it
            if (stringnums.DefiniteArticles.Contains(words[index]) || stringnums.IndefiniteArticles.Contains(words[index]) || stringnums.ListDeterminers.Contains(words[index]) || words[index].EndsWith("ing") || (method_being_constructed.question != "" && incarnations == null))
                ParseSubject(words);

            if (prompt >= 37)
                Console.WriteLine();

            // Regardless, parse the predicate:  a verb and the nouns that go with.
            incarnations = PredicateDefinition(words);

            // If we haven't found a verb yet, but we do have a subject, look for the verb in the subject's name.
            if (incarnations == null && subjectAt >= 0)
            {
                string[] subnames = method_being_constructed.signature[subjectAt].noun.name.Split(' ');
                for (int i = subnames.Count() - 1; i >= 0; i--)
                {
                    index--;
                    incarnations = FindMainVerb(subnames[i]);
                    if (incarnations == null) continue;

                    // Ah, there's the verb. Shorten the subject's name, and try parsing the predicate again from this index position.
                    definedVerb = subnames[i];
                    string newname = subnames[0];
                    for (int j = 1; j < i; j++)
                        newname += " " + subnames[j];
                    method_being_constructed.signature[subjectAt].noun.name = newname;
                    Console.WriteLine("  oops; verb is '{1}' and subject is '{0}'", newname, definedVerb);
                    incarnations = PredicateDefinition(words);
                    break;
                }
            }

            // If we still don't have a verb, we're going to have to search for it the long way.
            if (incarnations == null)
            {
                int savedIndex = index;
                while (incarnations == null && index < maxWords-1 && !stringnums.EndsInfinitiveDefinition.Contains(words[index]))
                    incarnations = FindMainVerb(words[++index]); // this is an expensive loop
                if (incarnations != null)
                {
                    // ah, found it. There must be a subject in front of it...
                    definedVerb = words[index];
                    IdentEndsAt = index;
                    index = 0; // back to first word of sentence
                    if (ParseSubject(words) == null)
                    {
                        method_being_constructed.signature.Add(new term(incarnations));
                        Console.Write("ERROR: verb is '{0}' but I don't know what to do with the subject: ", definedVerb);
                        for (int i = savedIndex; i < IdentEndsAt; i++)
                            Console.Write("{0} ",words[i]);
                        Console.WriteLine();
                        method_being_constructed.problems++;
                        multimethods.Add(method_being_constructed);
                        continue;
                    }
                    index++;    // advance past verb
                    IdentEndsAt = (int)words.Count(); // un-set the hint
                    var mm = PredicateDefinition2(words, incarnations); // and finish the predicate as normal
                    multimethods.Add(mm);
                }
            }

            // If we STILL don't have a verb, abort this sentence.
            if (incarnations == null)
            {
                method_being_constructed.signature.Add(new term("..."));
                Console.Write("ERROR: I couldn't find a verb in: ");
                for (int i = 0; i < index; i++)
                    Console.Write("{0} ", words[i]);
                Console.WriteLine();
                method_being_constructed.problems++;
                multimethods.Add(method_being_constructed);
                continue;
            }

            //if (prompt == 32)
            //    Console.WriteLine("debug");

            // OK, we have a verb, and processed the rest of the definition with it.  We should be at a marker separating 
            // the function head from the function body.  Or if it's a type/struct/object definition, we're at the end of 
            // the sentence entirely.
            method_being_constructed.fully_parsed = stringnums.EOL.Contains(words[index]);
            if (stringnums.EndsInfinitiveDefinition.Contains(words[index]))
                index++; // advance past the comma, colon, BY, VIA, MEANS, or whatever separates head from body
            else if (words[index] != ".")
            {
                Console.WriteLine("I didn't expect this definition of '{1}' end with a '{0}'", words[index], definedVerb);
                method_being_constructed.problems++;
                continue;
            }

            // If we had a valid definition, save away its body. We can't parse the bodies until we read all the heads.
            // That's because what names, esp. parameter names and other function invocations, that appear in the bodies
            // aren't even known yet. 
            if (!method_being_constructed.fully_parsed)
            {
                method_being_constructed.body = new List<string>();
                while (index < maxWords - 1 && !stringnums.EOL.Contains(words[index]))
                    method_being_constructed.body.Add(words[index++]);  //ParseParameterInvocation(words);
            }


            // for case of "X is Y..."
            // if the subject is already a type, and is now being used with "IS" again,
            // Ensure we don't use a known type with "is" unless
            // 1) there's a "when", or
            // 2) the subject is "the" instance of the type, or
            // 3) the subject is a gerund phrase (function type), so the predicate is a categorical name, a "class" or "name of enum" for signatures
            if (subjectAt != -1 && incarnations._en == "been" && method_being_constructed.fully_parsed)
            {
                parameter theSubject = method_being_constructed.signature[subjectAt].noun;
                if (just_declared_new_type && theSubject.art == Article.the)
                {
                    Console.WriteLine("  global variable or singleton instance '{0}' created", theSubject.name);
                    PermanentDataSegment.Add(theSubject);
                }
                else if (theSubject.isAggregate && method_being_constructed.signature.Count == 3) 
                {   // "Xing... is Y." so define Y as a category for verbs
                    var ident = method_being_constructed.signature[2].which == PartsOfSpeech.noun ? method_being_constructed.signature[2].noun.name : method_being_constructed.signature[2].preposition;
                    var newType = CreateNewType(ident, StandardType.homogene);
                    Console.WriteLine("  assuming '{0}' categorizes verbs", ident);
                }
                else if (!just_declared_new_type)
                {
                    if (theSubject.type == StandardType.anything)
                        Console.WriteLine("  ERROR: I have no idea what a '{0}' is. It could be anything.", theSubject.name);
                    else
                        Console.WriteLine("  ERROR: trying to re-define the type '{0}'", NameOfType(theSubject.type));
                    method_being_constructed.problems++;
                }

            }

            // Create the various names for each parameter in the new method.
            // This isn't perfect because we don't know where a noun phrase ends and where adverbs and verbs begin.
            // We'll keep it all as the possible noun phrase, but when we start parsing the body, we'll look at
            // what the parameters are actually called in there. Then we can shorten our parameter names to match
            // sensibly.
            TheGiveSomebodyItException = DirectObjectPlacement.not_there_yet;
            for (int i = 0; i < method_being_constructed.signature.Count; i++)
            {
                if (method_being_constructed.signature[i].which == PartsOfSpeech.noun)
                {
                    term newterm = method_being_constructed.signature[i];
                    parameter oldparam = newterm.noun;
                    newterm.noun = NamedParameter(oldparam, incarnations, (i == subjectAt));
                    method_being_constructed.signature[i] = newterm;
                }
                else if (method_being_constructed.signature[i].which == PartsOfSpeech.verb)
                    TheGiveSomebodyItException = DirectObjectPlacement.not_there_yet;
            }

            // Now we should be at the end of line: a period, question mark, or exclamation mark
            // If not, throw a helpful error.
            method_being_constructed.problems++;
            if (index >= words.Count())
                Console.WriteLine("COMPILER ERROR: EOL eaten: index too high");
            else if (words[index] != "ENDOFINPUT" && words[index] != ".")
                if (index < words.Count())
                    Console.WriteLine("  ERROR: I wasn't expecting '{0}' there, but rather, the end of the sentence.", words[index]);
                else
                    Console.WriteLine("COMPILER ERROR: EOL eaten");
            else
                method_being_constructed.problems--;


            // Sentence processed. Get next sentence
        }

        TestSignatures();
        Pass2ResolveImplicitTypesAndCreateMultimethodDictionary();
        Pass3MethodBodies();
        VerifyAbstractSyntaxTree();
        CodeGeneration();
    }

    static void TestSignatures()
    {
        //// And now, all the input sentences have been read in, if not processed.

        // test output only
        TestHarness.Reset();
        foreach (multimethod func in multimethods)
        {
            TestHarness.BeginTurn();
            Console.WriteLine("{0}{1}", (TestHarness.NextTestLine < TestHarness.testlines.Count()) ? TestHarness.testlines[TestHarness.NextTestLine].Trim() : func.prompt.ToString(), (func.problems > 0) ? " (problematic)" : "");
            foreach (term term in func.signature)
            {
                switch (term.which)
                {
                    case PartsOfSpeech.noun:        Console.WriteLine("  parameter '{1}{0}'", term.noun.fullname1, term.noun.isAggregate ? "" : "the "); break;
                    case PartsOfSpeech.verb:        Console.WriteLine("  verb '{0}'", term.verb.plural); break;
                    case PartsOfSpeech.preposition: Console.WriteLine("  keywords '{0}'", term.preposition); break;
                    default:                        Console.WriteLine("ERROR IN COMPILER: unknown term"); break;
                }
            }
            TestHarness.EndTurn(1);
            if (func.fully_parsed) Console.WriteLine("({0} fully parsed{1})", func.prompt, func.problems + func.todo > 0 ? ", but with problems/to-dos" : "");
        }

    }

    static void Pass2ResolveImplicitTypesAndCreateMultimethodDictionary()
    {
        // Next up, ensure we've resolved the types of all parameters.
        // (and to speed up later processing, create a sorted dictionary of all the multimethods)
        // (and to speed up later processing, create a sorted dictionary of all the multimethods which don't begin with a parameter)
        List<term> badterms = new List<term>();
        foreach (multimethod function in multimethods)
        {
            foreach (term term in function.signature)
            {
                switch (term.which)
                {
                case PartsOfSpeech.verb: // create a sorted dictionary of all multimethods
                    if (!mms.ContainsKey(term.verb))
                        mms.Add(term.verb, new List<multimethod>());
                    mms[term.verb].Add(function);
                    continue;
                case PartsOfSpeech.noun: // find the type of any unknown-type parameters
                    if (term.noun.type != (StandardType)0) 
                        break;
                    if (term.noun.isAggregate)
                    {
                        foreach (term innerTerm in term.noun.relation)
                        {
                            if (innerTerm.which != PartsOfSpeech.noun) continue;
                            InheritedType it = InheritanceTree.Find(x => x.name == innerTerm.noun.name);
                            if (it != null)
                                innerTerm.noun.type = it.typeid;
                            else
                                badterms.Add(innerTerm);
                        }
                        break;
                    }
                    foreach (var t in InheritanceTree)
                    {
                        if (t.name != term.noun.name) continue;
                        term.noun.type = t.typeid;
                        goto breakOverElseBecauseFoundIt;
                    }
                    badterms.Add(term);
//                    Console.WriteLine("  ERROR: is '{0}' a number? A text? Something else? You haven't said.", term.noun.name);
                    function.problems++;
                    break;
                }
                breakOverElseBecauseFoundIt:
                ;
            }
        }
        if (badterms.Count > 0)
            Console.WriteLine("  ERROR: What are these? A number? A text? Something else? You haven't said.");
        foreach (var term in badterms)
            Console.Write("{0}, ", term.noun.name);
        Console.WriteLine();

        // When creating aggregate types like List Of X, we might create X as a placeholder. But if the real X is never defined...
        List<InheritedType> badtypes = new List<InheritedType>();
        foreach (InheritedType t in InheritanceTree)
            if (t.parent == null && t.typeid != StandardType.anything)
                badtypes.Add(t);
        if (badtypes.Count > 0)
            Console.WriteLine("  ERROR: Are these the plural form of a word I don't know? Or is it a number? A text? Something else? We have 'many' of them but I need to know what just one of them is.");
        foreach (var t in badtypes)
            Console.Write("{0}, ", t.name);
        Console.WriteLine();

        // Now that all types are resolved, ensure we haven't re-declared the same verb with the same signature.
        Console.WriteLine("{0} different verbs: ", mms.Count);
        foreach (KeyValuePair<fiveforms,List<multimethod>> entry in mms)
        {
            Console.WriteLine(entry.Key.plural);
            if (entry.Value.Count < 2) continue;
            var sigs = new Dictionary<string, multimethod>();
            foreach (multimethod method in entry.Value)
            {
                string newsig = method.signature.ToString(false);//"";
                /*foreach (term t in method.signature)
                {
                    switch (t.which)
                    {
                        case PartsOfSpeech.noun:
                            newsig += (t.noun.isAggregate) ? t.noun.signatureToString() : t.noun.name; 
                            break;
                        case PartsOfSpeech.preposition: newsig += t.preposition; break;
                        case PartsOfSpeech.verb: 
                            newsig += t.verb.plural;
                            break;
                    }
                    newsig += " ";
                }*/
                var preexisting = (sigs.ContainsKey(newsig)) ? sigs[newsig] : null;
                if (preexisting != null)
                {
                    Console.WriteLine("  ERROR: we have more than one method for {0}:", entry.Key._ing);
                    Console.WriteLine("#{0} '{1}'", preexisting.prompt, newsig);
                    Console.WriteLine("#{0} '{1}'", method.prompt, newsig);
                }
                else
                    sigs.Add(newsig, method);
            }
        }
    }

    static void Pass3MethodBodies()
    {
        // And now, we're finally ready to try parsing any function bodies. 
        foreach (multimethod method in multimethods)
        {
            if (method.fully_parsed) continue;
            if (method.body == null) continue;
            if (method.body.Count == 0) continue;
            prompt = method.prompt; // useful for debugging
            ParseInvocations(method);
        }
    }

    static void VerifyAbstractSyntaxTree()
    {
        // now check the whole abstract syntax tree

        // First, is there one and only one root?  "To do:"
        fiveforms rootverb = FindVerb("do");
        multimethod main = null;
        if (mms.ContainsKey(rootverb))
            foreach (multimethod possibleRoot in mms[rootverb])
                if (possibleRoot.signature.Count == 1)
                    if (main == null)
                        main = possibleRoot;
                    else
                        Console.WriteLine("  ERROR:  I have too many definitions for \"To do:\", like #{0} and #{1}", main.prompt, possibleRoot.prompt);
        if (main == null)
            Console.WriteLine("  ERROR:  Where does the whole thing start?  I need a sentence which begins, \"To do:\"");
        else
            main.called++;

        // dead code elimination?  unused structs elimination?
    }

    static void CodeGeneration()
    {
        // create file
        CodeGen codegen = new Flash();
        codegen.Begin("complish");
        codegen.WriteStringTable(literalStrings);

        foreach (multimethod method in multimethods)
        {
            if (method.parsed_body == null || method.parsed_body.Count == 0)
            {   // it's data not code
                parameter subject = null;
                fiveforms verb = null;
                foreach (term term in method.signature)
                    if (term.which == PartsOfSpeech.verb)
                    {
                        verb = term.verb;
                        break;
                    }
                    else if (term.which == PartsOfSpeech.noun)
                        subject = term.noun;
                if (verb == null) continue;
                bool subjectNamesStruct = (verb == TheVerbIs);
                if (subjectNamesStruct && subject == null) 
                    continue;//compiler error? named instance? global var?
                //Console.WriteLine("struct {0} {1}  // #{2}{3}", subjectNamesStruct ? subject.fullname1.Replace(' ', '_') : verb.singular + "_relation", "{", method.prompt, method.problems > 0 ? " ?" : "");
                foreach (term term in method.signature)
                {
                    if (term.which != PartsOfSpeech.noun) continue;
                    if (subjectNamesStruct && term.noun == subject) continue;
                    /*if (term.noun.art != Article.many)
                        Console.WriteLine("\t{0} {1};", NameOfType(term.noun.type), term.noun.fullname1.Replace(' ', '_'));
                    else
                        Console.WriteLine("\tList<{0}> {1};", NameOfType(term.noun.type), term.noun.fullname1.Replace(' ', '_'));*/
                }
                //Console.WriteLine("}");
                //Console.WriteLine();
            }
            else // it's code not data
            {
                ;// Console.WriteLine("#{0} called from {1} places", method.prompt, method.called);
            }
        }

        codegen.WriteInt(10);
        codegen.Finish();

        Console.Write("---- press enter ----");
        Console.ReadLine();
    }

    /// <summary> Output.  Returns the words in the list, space-separated. Prepends theTypeWhich for relative clauses</summary>
    public static string Say(List<string> words, int from, int to, theTypeWhich preamble = null)
    {
        string retval = "";
        if (preamble != null)
            retval += preamble.say() + " ";
        if (to >= words.Count) to = words.Count - 1;
        for (; from <= to; from++)
        {
            retval += (words[from].StartsWith("__litstring")) ? '"' + Say(words[from]) + '"' : words[from];
            if (from != to) retval += " ";
        }
        return retval;
    }

    /// <summary>Given the __litstring token that sits in words[], returns the string literal from the string table.</summary>
    public static string Say(string litStringToken)
    {
        if (!litStringToken.StartsWith("__litstring")) return litStringToken;
        string marker = litStringToken.Substring(11);
        marker = marker.Substring(0, marker.Length - 2);
        int Nth = Int32.Parse(marker);
        return literalStrings[Nth];
    }

}

