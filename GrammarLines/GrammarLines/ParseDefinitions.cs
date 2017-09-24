using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

static partial class Program
{
    /// <summary> Uses ParseNounPhraseForParameter. </summary>
    public static parameter ParseSubject(string[] words)
    {
        parameter subject = ParseNounPhraseForParameter(words, true);
        if (subject != null)
        {
            Console.WriteLine("  parsed the subject of a sentence, '{0}'", subject.name);
            if (preposition != "") method_being_constructed.signature.Add(new term(preposition));
            method_being_constructed.signature.Add(new term(subject, preposition));
            subjectAt = method_being_constructed.signature.Count - 1;
            preposition = "";
        }
        return subject;
    }

    /// <summary> Parses "To give (parameter list)". Returns the defined verb and adds a new item to the global multimethods list.
    /// Uses FindVerb and PredicateDefinition2.</summary>
    static fiveforms PredicateDefinition(string[] words)
    {
        incarnations = FindMainVerb(words[index]);
        if (incarnations == null) return null;
        var mm = PredicateDefinition2(words, incarnations);
        if (mm != null) multimethods.Add(mm);
        return incarnations;
    }

    /// <summary> Once a string has been converted to a verb (fiveforms), this parses the parameter list via multiple calls to
    /// ParseNounPhraseForParameter and its own list delimiter parsing. Returns the defined verb as a convenience, but the real
    /// result is added to the global multimethods collection. </summary>
    static multimethod PredicateDefinition2(string[] words, fiveforms incarnations, bool isSubject = false)
    {
        method_being_constructed.signature.Add(new term(incarnations));
        definedVerb = words[index];
        string preposition = "";
        bool found_official_preposition = false;
        TheGiveSomebodyItException = DirectObjectPlacement.not_there_yet;
        do
        {
            index++;
            while (!stringnums.EndsInfinitiveDefinition.Contains(words[index]))
            {
                if (stringnums.EOL.Contains(words[index]))
                    goto doublebreak;
                parameter noun_phrase = ParseNounPhraseForParameter(words);
                if (noun_phrase == null)
                {
                    if (!stringnums.ListSeparators.Contains(words[index]))
                    {
                        if (!found_official_preposition) // after a preposition, there shouldn't be any words related to the verb.
                        {
                            if (isSubject && FindMainVerb(words[index]).HasValue())
                                goto doublebreak;
                            if (preposition != "") preposition += " ";
                            preposition += words[index]; // so, "the person given to", not, "the person given to just"
                        }
                        if (stringnums.prepositions.Contains(words[index]))
                            found_official_preposition = true;
                    }
                    index++;
                }
                else
                {
                    if (preposition != "")
                        method_being_constructed.signature.Add(new term(preposition));
                    method_being_constructed.signature.Add(new term(noun_phrase, preposition));
                    preposition = "";
                    found_official_preposition = false;
                }
            }
            // if the next word is "a/an/another", then we are probably in a list. 
            // if it were a function body starting, it would likely be "the"
        } while (stringnums.IndefiniteArticles.Contains(words[index + 1]) || stringnums.ListDeterminers.Contains(words[index + 1]) || stringnums.ListSeparators.Contains(words[index + 1]));
    doublebreak:
        if (preposition != "")
            method_being_constructed.signature.Add(new term(preposition));
        return method_being_constructed;
    }

    /// <summary> Creates a parameter name for use in the function body like "the thing given" or "the person given to" </summary>
    static parameter NamedParameter(parameter noun_phrase_being_constructed, fiveforms verb, bool isSubject = false)
    {
        //parameter noun_phrase = noun_phrase_being_constructed;
        if (noun_phrase_being_constructed.preposition == "" && !isSubject)
            HandleSwappedDirectandIndirectObjects(method_being_constructed, "to", verb, isSubject);

        foreach (term part_of_speech in method_being_constructed.signature)
        {
            if (part_of_speech.which != PartsOfSpeech.noun) continue;
            if (part_of_speech.noun == noun_phrase_being_constructed) break;
            if (part_of_speech.noun.type == noun_phrase_being_constructed.type) // is the noun a Integer? Percentage? Ranking?
                noun_phrase_being_constructed.position++; // then it shall be the first/second/third integer/task/customer
        }
        if (noun_phrase_being_constructed.position == 2) // then find the first "the TYPE" and change it to "the first TYPE"
            foreach (term np in method_being_constructed.signature)
            {
                if (np.which != PartsOfSpeech.noun) continue;
                if (np.noun.type == noun_phrase_being_constructed.type)
                {
                    np.noun.fullname2 = "first " + np.noun.fullname2;
                    break;
                }
            }

        CreateFullNames(noun_phrase_being_constructed, verb, isSubject);

        return noun_phrase_being_constructed;
    }

    /// <summary>Creates the names by which the given parameter will be invoked:  the thing given, the person given to, etc.</summary>
    static void CreateFullNames(parameter noun_phrase, fiveforms verb, bool isSubject = false)
    {
        if (noun_phrase.isAggregate) // it's a gerund, or participial phrase, etc.
            return;

        noun_phrase.fullname1 = noun_phrase.name + ((verb == TheVerbIs) ? "" : " " + ((isSubject) ? verb._ing : verb._en));
        if (noun_phrase.preposition != "") noun_phrase.fullname1 += " " + noun_phrase.preposition;

        noun_phrase.fullname2 = noun_phrase.name; // given name.  But, if the name is just the type, then..
        if (noun_phrase.position > 1)
            if (Contains<StandardType>(noun_phrase.name) || Contains<StandardTypePlural>(noun_phrase.name))
                noun_phrase.fullname2 = NumberToSpelledOutOrdinal(noun_phrase.position) + " " + noun_phrase.name; // TODO translate int to ordinal
    }


    /// <summary>For cases of "give someone the thing", we must back up and add "to" to the "someone"</summary>
    static void HandleSwappedDirectandIndirectObjects(multimethod method_being_constructed, string prep, fiveforms verb, bool isSubject)
    {
        if (isSubject) return;
        if (listType != null) return;
        switch (TheGiveSomebodyItException)
        {
            case DirectObjectPlacement.not_there_yet:
                TheGiveSomebodyItException = DirectObjectPlacement.normal;
                break;
            case DirectObjectPlacement.normal:
                // then the previous parameter is the indirect, not direct, object, and needs "to" prepended
                for (int i = 0; method_being_constructed.signature.Count > i; i++)
                {
                    if (method_being_constructed.signature[i].which == PartsOfSpeech.noun)
                    {
                        term newterm = method_being_constructed.signature[i];
                        newterm.noun.preposition = prep;
                        method_being_constructed.signature[i] = newterm;
                        CreateFullNames(method_being_constructed.signature[i].noun, verb);//not sure why this is still needed
                        break;
                    }
                }
                TheGiveSomebodyItException = DirectObjectPlacement.after_the_indirect_object;
                break;
            case DirectObjectPlacement.after_the_indirect_object:
            case DirectObjectPlacement.error:
                TheGiveSomebodyItException = DirectObjectPlacement.error;
                Console.WriteLine("  ERROR: I require those parameters to be separated by prepositions or commas.");
                method_being_constructed.problems++;
                break;
        }
    }
    static DirectObjectPlacement TheGiveSomebodyItException = DirectObjectPlacement.not_there_yet;


    /// <summary>A somewhat high-level function to parse (only) a single indeterminate noun phrase like "a numeric entry", "a number", 
    /// "a car", "a vertical percentage", "several cars", "anything", "the manager" (if the NP is the subject of its 
    /// sentence), "many gadgets" (when gadget itself hasn't been established yet), or a number of specifically invalid 
    /// combinations, that describes a typed parameter in the definition of a multimethod's signature. 
    /// Will soon need ability to parse -ing phrases and sub-predicates as in "To learn (repairing a car with a tool)".
    /// Uses ParseArticle, ParseNewIdentifier, ParseAdjectiveType, Parse enum, ParseAnyType, and multiple NameOfType.
    /// Finally calls CreateParameterObject after all but the worst parses.
    /// </summary>
    static parameter ParseNounPhraseForParameter(string[] words, bool isSubject = false)
    {
        StandardType? type = null;
        string ident = "";
        int savedIndex = index;
        bool identIsAdjectiveTypeIsNoun = false; 
        // usually if the type is the noun, there is no adjective: "the task"
        // and usually if both adj & noun are present, the type is the adjective: "the numeric virtue" 
        // this is for constructions like "the horizontal percent, the vertical percent"

        // step 1: try parsing "something", "an adjective-type noun" "nountype", etc., or as a last resort, just a new identifier
        Article? mode = ParseArticle(words) ?? ((isQuestion && isSubject) ? Article.a : (Article?)null);
        OtherParameterTypes? opt = Parse<OtherParameterTypes>(words[index]);
        StandardTypeAdjective? adjtype = ParseAdjectiveType(words);
        if (adjtype != null)
        {
            ident = ParseNewIdentifier(words, ref type);
            if (type != null && type != (StandardType)adjtype)
            {
                Console.WriteLine("ERROR: I'm unsure whether '{0} {1}' is a {1} nicknamed {0} or is something {0} nicknamed {1}.", adjtype, type);
                method_being_constructed.problems++;
                return null;
            }
            type = (StandardType)adjtype;
            if (ident == "")
            {
                Console.WriteLine("ERROR: must provide a name after the adjective '{0}' or use its noun form instead, '{1}'.", adjtype, (StandardType)adjtype);
                method_being_constructed.problems++;
                return null;
            }
        }
        else if (opt != null)
            type = (StandardType)opt;
        else
        {
            fiveforms ing = PresentParticiple(words[index]);
            if (ing != null)
                return CreateFunctionParameterObject(words, ing, isSubject);
            type = ParseAnyType(words); 
            ident = (type == null) ? "" : NameOfType(type.Value);
        }
        if (ident == "" && type == null /*&& isSubject*/) // then we have no type info whatsoever. Just get a new ident
        {
            ident = ParseNewIdentifier(words, ref type);
            identIsAdjectiveTypeIsNoun = (type != null);
        }

        if (type == null && mode == null)
        {   // if we know nothing, abort
            index = savedIndex;
            return null;
        }
        return CreateParameterObject(words, mode, type, adjtype, ident, identIsAdjectiveTypeIsNoun, isSubject);
    }

    /// <summary>When defining a parameter of a new function's signature, and once parsing of that parameter is done, this converts
    /// the information into a parameter object. Uses CreateListParameterObject since that is a tome in itself.</summary>
    static parameter CreateParameterObject(string[] words, Article? mode, StandardType? type, StandardTypeAdjective? adjtype, string ident, bool identIsAdjectiveTypeIsNoun = false, bool isSubject = false)
    {
        // something/nothing/anything aren't preceded by an article. Treat as "a thing"
        if (type.HasValue && type <= StandardType.something) 
        {
            Console.WriteLine("  declaring a parameter which {0}", type == StandardType.nothing ? "must be nothing" : type == StandardType.anything ? "could be anything" : "mustn't be nothing");
            return new parameter("", type.Value.ToString(), Article.a, type.Value, identIsAdjectiveTypeIsNoun);
        }

        switch (mode)
        {
            case Article.the:
                if (type.HasValue)
                {
                    if (adjtype == null)
                        Console.WriteLine("ERROR: we say 'the {0}' to use it later in the sentence. Here we should say 'a {0}'.", NameOfType(type.Value));
                    else
                        Console.WriteLine("ERROR: we say 'the {0}' to use it later in the sentence. Here we should say 'a {0} {1}'.", adjtype, ident == "" ? "name-of-it" : ident);
                    method_being_constructed.problems++;
                    return null;
                }
                if (isSubject || isQuestion)
                {
                    just_declared_new_type = isSubject; // and a global instance, as well
                    return new parameter("", ident, mode ?? Article.the, type ?? StandardType.number, identIsAdjectiveTypeIsNoun);
                }
                Console.WriteLine("ERROR: we should use 'a' not 'the' in this part of the sentence.");
                method_being_constructed.problems++;
                return null;

            case Article.many:
                return CreateListParameterObject(ref type, ident, identIsAdjectiveTypeIsNoun);
            
            case Article.a:
                if (type.HasValue) // also, should we disallow it when the ident is also a type? "numeric car"
                {
                    Console.WriteLine("  declaring a parameter of type {0} called '{1}'", NameOfType(type.Value), ident);
                    InheritedType it = InheritanceTree.Find(item => item.name == ident);
                    if (it != null && it.typeid != type.Value)
                        Console.WriteLine("  WARNING: '{0}' is declared elsewhere as a '{1}', not a '{2}'", it.name, NameOfType(it.typeid), NameOfType(type.Value));
                }
                else if (isQuestion && incarnations == TheVerbIs)
                {
                    for (IdentEndsAt = index; IdentEndsAt < words.Length && words[IdentEndsAt] != "?"; IdentEndsAt++) ;
                    if (IdentEndsAt >= words.Length)
                    {
                        Console.WriteLine("  ERROR: I thought #{0} was a question because it began with '{1}', but I couldn't find a question mark afterward.", method_being_constructed.prompt, method_being_constructed.question);
                        method_being_constructed.problems++;
                        return null;
                    }
                    Console.WriteLine("  defining a {0} by Q&A.", ident);
                    ident = ParseNewIdentifier(words, ref type);
                }
                else if (!isSubject)
                {
                    if (ident == "") // then we have no type info whatsoever. Just get a new ident
                        ident = ParseNewIdentifier(words, ref type);
                    Console.WriteLine("  TODO: is '{0}' textual? Numeric? Or something more complex?", ident);
                    method_being_constructed.todo++;
                }
                // defining a new type
                else if (InheritanceTree.Find(item => item.name == ident) != null)
                {
                    Console.WriteLine("  ERROR: re-defining type '{0}'.", ident);
                    method_being_constructed.problems++;
                    //return null; // let it parse for now
                    return new parameter("", ident, mode ?? Article.a, type ?? (StandardType)0, identIsAdjectiveTypeIsNoun);
                }
                else
                {
                    // object until proven otherwise. heck, it may not even be a type, it might be an instance or variable.
                    var newType = CreateNewType(ident);
                    Console.WriteLine("  assuming '{0}' is a new type", newType.name, newType.typeid);
                }
                break;

            case null:
                if (type == null)
                    return null;
                if (ident == null)
                    Console.WriteLine("  plural(?) parameter of type {0}", type);
                else
                    Console.WriteLine("  plural(?) parameter of type {1} called the {0}", ident, NameOfType(type.Value));
                break;
        }
        return new parameter("", ident, mode ?? Article.a, type ?? (StandardType)0, identIsAdjectiveTypeIsNoun);
    }

    /// <summary>Part of CreateParameterObject, which does the work for ParseNounPhraseForParameter. 
    /// When declaring the parameters of a function's signature, this helper digests the "many..." parameter, which isn't
    /// a trivial operation, considering that lists have subtypes, some of which may not be known yet.</summary>
    static parameter CreateListParameterObject(ref StandardType? type, string ident, bool adjnounswap)
    {
        if (ident == null && type == null)
        {
            Console.WriteLine("  ERROR: many whats?");
            method_being_constructed.problems++;
            return new parameter("", ident, Article.many, (StandardType)0, adjnounswap);
        }

        //either ident has value, type has value, or both have value
        // if basetype has value, then ident must not be a type. i.e., warn of "numeric car" or "many numeric cars" when car is of type object
        if (ident.HasValue() && type.HasValue)
        {
            InheritedType it = InheritanceTree.Find(item => item.name == ident);
            if (it != null && it.typeid != type.Value)
                Console.WriteLine("  WARNING: '{0}' is declared elsewhere as a '{1}', not a '{2}'", it.name, NameOfType(it.typeid), NameOfType(type.Value));
        }

        // for the term "several/many/multiple numbers", set ident to the whole term "many numbers" -- a generated name
        if (string.IsNullOrEmpty(ident)) ident = string.Format("many {0}", type);

        // for the term "many gadgets", set type to "gadgets" by finding the type by name, or making a new one
        if (type == null)
        {
            InheritedType subtype = InheritanceTree.Find(item => item.name == ident);
            if (subtype == null)
            {   // make a new type, "gadgets", whose parent (and subtype, if applicable) to be filled out later?
                subtype = new InheritedType() { name = ident, typeid = (StandardType)(InheritanceTree.Count + 2) };
                InheritanceTree.Add(subtype);
            }
            type = subtype.typeid;
        }

        // now all three are populated:  mode, type, ident
        // now, does our composite type already exist? Else make a new typeid
        string typeAsString = type.Value.ToString();
        InheritedType aggregate = InheritanceTree.Find(item => item.name == ident && item.parent == StandardType.list.ToString() && item.subtypes == typeAsString);

        if (aggregate != null)
        {
            Console.WriteLine("  another '{0}' (a list of {1})", ident, NameOfType(type.Value));
            return new parameter("", ident, Article.many, type.Value, adjnounswap);
        }
        aggregate = new InheritedType() { name = ident, parent = StandardType.list.ToString(), subtypes = type.Value.ToString(), typeid = (StandardType)(InheritanceTree.Count + 2) };
        InheritanceTree.Add(aggregate);
        just_declared_new_type = true;

        string typename = NameOfType(type.Value);
        if (ident != typename)
            Console.WriteLine("  a list of {0}, called {1}", typename, ident);
        else
            Console.WriteLine("  a list of {0}", typename);

        return new parameter("", ident, Article.many, type.Value, adjnounswap);
    }

    /// <summary>This is used while defining a higher-order function, tipped off by the -ing verb form that leads.</summary>
    static parameter CreateFunctionParameterObject(string[] words, fiveforms ing, bool isSubject = false)
    {
        long savedIndex = index;
        multimethod outerMethod = method_being_constructed;
        string savedDefinedVerb = definedVerb;
        method_being_constructed = new multimethod() { prompt = -outerMethod.prompt, question = "" };
        multimethod innerMethod = PredicateDefinition2(words, ing, isSubject); // returns method_being_constructed
        method_being_constructed = outerMethod;
        definedVerb = savedDefinedVerb;
        return new parameter(innerMethod);
    }

    /// <summary>A hint, not usually present</summary>
    static int IdentEndsAt;
    /// <summary> Parses input. Stub-like.  This is complex because how to know when to stop eating words is complex.</summary>
    static string ParseNewIdentifier(string[] words, ref StandardType? nounIsType)
    {
        // for "a horizontal percent" where the ident is an adjective
        string ident = "";
        int saved = index;
        for (; IdentEndsAt > index; index++)
        {
            saved = index;
            if (ParseArticle(words) != null) break;
            if (words[index] == "is" || words[index] == "are") break;
            if (ParseAdjectiveType(words) != null) break; // but allow the noun types, for a numeric number
            if (ParseListSeparator(words) != null) break;
            if (stringnums.EndsInfinitiveDefinition.Contains(words[index])) break;
            if (stringnums.EOL.Contains(words[index])) break;
            if (stringnums.prepositions.Contains(words[index])) break; // names stop at a preposition.
            if (Contains<OtherParameterTypes>(words[index])) break;
            if (ident != "")
            {   // if we've eaten at least one ident words, maybe the next is a type: "the foo bar percentage"
                var identIsAdjective = ParseAnyType(words);
                if (identIsAdjective != null)
                {
                    nounIsType = identIsAdjective;
                    saved = index;
                    break;
                }
            }
            if (ident != "") ident += " ";
            ident += words[index];
        }
        index = saved;
        return ident;
    }

}
