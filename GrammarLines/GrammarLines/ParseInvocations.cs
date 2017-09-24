using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

static partial class Program
{
    /// <summary>
    /// This does a quick 'n shallow look through all the signatures to see which ones match the opening word of the
    /// invocation-to-be-parsed.  Academically speaking, it's unnecessary.  Practically speaking, it's better than nothing.
    /// </summary>
    public static List<multimethod> ParseInvocationByFirstWord(List<string> words)
    {
        var likely = new List<multimethod>();
        string firstWord = words[index];
        if (stringnums.DefiniteArticles.Contains(firstWord) || stringnums.IndefiniteArticles.Contains(firstWord) || stringnums.ListDeterminers.Contains(firstWord))
            firstWord = words[++index];
        int savedIndex = index;
        bool startsThe = (words[index] == "the");
        while (words[index] == "the") index++;
        theTypeWhich theTypeWhich = IsNameOfTypeWhich(words);
        index = savedIndex;
        long? number = ParseNumberOrdinalPercent(words);
        index = savedIndex;
        bool TextLit = words[index].StartsWith("__litstring");
        index = savedIndex;
        fiveforms aVerb = PresentParticiple(words[index]) ?? FindMainVerb(firstWord);
        index = savedIndex;
        bool IsPrep = stringnums.prepositions.Contains(firstWord);
        bool IsVerb = aVerb != null;
        bool IsNoun = startsThe || theTypeWhich != null || number != null || TextLit;
        bool CanBeNoun = !IsVerb && !IsPrep;
        foreach (var mm in multimethods)
        {
            switch (mm.signature[0].which)
            {
                case PartsOfSpeech.verb:
                    if (IsVerb && mm.signature[0].verb.Contains(firstWord))
                        likely.Add(mm);
                    break;
                case PartsOfSpeech.noun:
                    if (mm.signature[0].noun.isAggregate)
                        likely.Add(mm);
                    if (CanBeNoun)
                        likely.Add(mm);
                    break;
                case PartsOfSpeech.preposition:
                    if (IsPrep && firstWord == mm.signature[0].preposition)
                        likely.Add(mm);
                    break;
            }
        }
        return likely;
    }

    /// <summary>High-level function to parse the body of a function. Turns parseMe.body (a string) into parseMe.parsed_body
    /// (a List of Invocations) via multiple calls to ParseAnInvocation() and ParseListSeparator().</summary>
    public static void ParseInvocations(multimethod parseMe)
    {
        Console.WriteLine("#{0}: parsing body", parseMe.prompt);
        index = 0;
        subprompt = 0;
        parseMe.parsed_body = new List<invocation>();
        invocation invoke;
        do
        {
            ++subprompt;
            subsubprompt = 0;
            invoke = ParseAnInvocation(parseMe.body);
            if (invoke != null)
            {
                parseMe.parsed_body.Add(invoke);
                invoke.definition.called++;
            }
            if (index < parseMe.body.Count - 1)
                ParseListSeparator(parseMe.body.ToArray());
        } while (invoke != null && index < parseMe.body.Count - 1);
        Console.WriteLine("  {0} statements in that function.", parseMe.parsed_body.Count);
        Console.WriteLine();
    }

    /// <summary>In parseMe.body, starting at global var 'index', this loops through all multimethods
    /// in the user's program and tries to match the current words to one of them. 
    /// Binds the arguments found in those words to the Parameters in each matching multimethods' Signature.
    /// On success, exactly one multimethod matches, so returns an Invocation with the List of Instances
    /// in the invocation.boundParameters. Uses ParseInvocationByFirstWord, ParseInvokedNoun (but handling 
    /// verbs and prepositions itself), ParseListSeparator, and Distance, with a lot of error reporting.</summary>
    public static invocation ParseAnInvocation(List<string> words)
    {
        List<multimethod> likely = ParseInvocationByFirstWord(words);
        if (likely == null || likely.Count == 0)
        {
            Console.WriteLine("Cannot find any multimethod which fits this function body: '{0}'", words[0]);
            return null;
        }

        int savedIndex = index;
        List<invocation> matching = ParseAnInvocation2(words, likely);

        return InvocationProblems(words, matching, likely, savedIndex);
    }

    /// <summary>This does the real work for parsing an invocation. It loops through all "likely" multimethods, producing a 
    /// smaller set of "matching" multimethods.</summary>
    public static List<invocation> ParseAnInvocation2(List<string> words, List<multimethod> likely, bool allowTrailingWords = false, theTypeWhich theTypeWhich = null)
    {
        bool isRelativeClause = theTypeWhich.HasValue();
        allowTrailingWords |= isRelativeClause;
        bool allowUnboundArgument = allowTrailingWords && theTypeWhich == null; // gerund phrases only. for now.
        // unbound arguments means the signature we're trying to match -- which is itself a parameter of the outer function --
        // can "accept" extra arguments. but those arguments must be bound, so they don't Use Up the unbound args.
        // How does a higher-order function mention whether it wants a fully bound function -- which it can invoke --
        // or a unbound function -- which it invokes by giving it some arguments?
        // For 
        if (allowUnboundArgument && prompt == 37)
            Console.WriteLine();
        int invocationBeginsAt = index;
        scopeDepth++;
        var matching = new List<invocation>();
        foreach (multimethod fitting in likely)
        {
            foreach (term term in fitting.signature)
                if (term.which == PartsOfSpeech.noun)
                    AddToScope(term.noun);
            var bound = new List<instance>();
            bool nextNounMustBeUnbound = false;
            index = invocationBeginsAt;
            bool TheSearchFieldIsNext = false;
            fitting.matchingTerms = -1;
            bool ThisSigMatches = true; // assume yes, now try to disprove
            foreach (term term in fitting.signature)
            {
                fitting.matchingTerms++;
                switch (term.which)
                {
                    case PartsOfSpeech.preposition:
                        TheSearchFieldIsNext = (isRelativeClause && theTypeWhich.preposition == term.preposition);
                        if (TheSearchFieldIsNext)
                            continue; // then the preposition appeared before Which:  "...the number *to* which..."
                        if (index >= words.Count || term.preposition != words[index])
                        {
                            if (!allowUnboundArgument)
                                break;
                            nextNounMustBeUnbound = true;
                            continue;
                        }
                        index++;
                        continue;
                    case PartsOfSpeech.verb:
                        if (index >= words.Count || !term.verb.Contains(words[index])) 
                            break;
                        TheSearchFieldIsNext = (isRelativeClause && theTypeWhich.preposition == "");
                        index++;
                        continue;
                    case PartsOfSpeech.noun:
                        if (isRelativeClause && TheSearchFieldIsNext /* is NOW */)
                        {
                            TheSearchFieldIsNext = false;
                            if (!IsA(theTypeWhich.type.typeid, term.noun.type)) break;
                            bound.Add(new instance { toAccess = Indirectedness.nested_invocation, type = theTypeWhich.type.typeid, var = term.noun });
                        }
                        else
                        {
                            var boundNoun = ParseInvokedNoun(words, term.noun, allowUnboundArgument);
                            if (boundNoun == null) break;
                            if (nextNounMustBeUnbound)
                            {
                                if (boundNoun.toAccess != Indirectedness.unbound) break;
                                if (!IsA(boundNoun.var.type, term.noun.type))
                                {
                                    Console.WriteLine("    (type of function's parameter didn't match and wasn't contravariant)");
                                    break; // contravariant, not covariant
                                }
                                nextNounMustBeUnbound = false;
                                continue; // without saving the unbound; 
                            }
                            bound.Add(boundNoun);
                        }
                        continue;
                }
                // only hit here on a term not matching, so, the sig doesn't match
                bound.Clear();
                ThisSigMatches = false;
                break;
            } // end foreach term in sig
            if (ThisSigMatches)
            {
                // even if all words are used up, the main clause should be followed by a comma, THEN, end-of-line, or other list delimiter 
                if (allowTrailingWords || index >= words.Count || ParseListSeparator(words.ToArray()) != null)
                    matching.Add(new invocation(fitting, invocationBeginsAt, index - 1, bound));
            }
            foreach (term term in fitting.signature)
                if (term.which == PartsOfSpeech.noun)
                    RemoveFromScope(term.noun);
        }
        scopeDepth--;
        return matching;
    }

    /// <summary>Once the magic words that begin a relative clause are found ("the TYPE which..") by ParseInvokedNoun, 
    /// this parses the rest of the invocation.  Relative clauses invoke phrases or data structures and play a matching game with
    /// them. This is straightforward with data, but with imperative code we have to "go Prolog" for it to make sense 
    /// even conceptually. </summary>
    static invocation ParseARelativeInvocation(theTypeWhich theTypeWhich, List<string> words)
    {
        subsubprompt++;

        // Handle passive voice constructions like "the type1 to which the type2 WAS GIVEN".
        string theIsUsed = "";
        fiveforms relativeVerb = null;
        bool passiveVoice = (words[index] == "was" || words[index] == "were" || words[index] == "is" || words[index] == "are");
        if (passiveVoice)
        {
            theIsUsed = words[index++];
            relativeVerb = mms.SingleOrDefault(item => item.Key._en == words[index]).Key;
            if (relativeVerb == null)
            {
                passiveVoice = false;
                relativeVerb = TheVerbIs;
                index--;
            }
        }
        else
            relativeVerb = mms.SingleOrDefault(item => item.Key.singular == words[index] || item.Key.plural == words[index] || item.Key.past == words[index]).Key;
        /*if (relativeVerb != null)
        {
            bool pastTense = (words[index++] == relativeVerb.past);

            // now search all possible keywords & prepositions for all possible methods to see if the next word matches one of them
            List<multimethod> submatches = new List<multimethod>();
            int invocationBeginsAt = index;
            foreach (multimethod mm in mms[relativeVerb])
            {
                index = invocationBeginsAt;
                foreach (term t in mm.signature)
                    if (t.which == PartsOfSpeech.noun && Match(words, t.noun.preposition))
                        if (IsA(theTypeWhich.type.typeid, t.noun.type))
                            // "which gave" (imperative)  vs.  "which knows" (relation)
                            if ((pastTense && !mm.fully_parsed) || (!pastTense && mm.fully_parsed))
                            {
                                submatches.Add(mm);
                                break;
                            }
            }
            if (submatches.Count == 0)
            {
                Console.WriteLine("  ERROR: In the relative clause 'which {0}{1}' I couldn't find any defined method for {2} which matched it", passiveVoice ? theIsUsed + " " : "", passiveVoice ? relativeVerb._en : pastTense ? relativeVerb.past : relativeVerb.singular, relativeVerb.singular);
                index = savedIndex;
                return null;
            }
            if (submatches.Count > 1)
            {
                Console.WriteLine("  ERROR: In the relative clause 'which {0}{1}' I found too many defined methods for {2}", passiveVoice ? theIsUsed + " " : "", passiveVoice ? relativeVerb._en : pastTense ? relativeVerb.past : relativeVerb.singular, relativeVerb.singular);
                index = savedIndex;
                return null;
            }
            Console.WriteLine("  relative clause matches #{0}", submatches[0].prompt);
        }*/

        
        // now parse the argument(s) being fed to it
        int savedIndex = index;
        List<multimethod> likely = passiveVoice ? mms[relativeVerb] : ParseInvocationByFirstWord(words);
        if (likely == null || likely.Count == 0)
        {
            Console.WriteLine("Cannot find any multimethod which fits this part of the relative clause: '{0}...'", words[index]);
            return null;
        }

        List<invocation> matching = ParseAnInvocation2(words, likely, true, theTypeWhich);

        return InvocationProblems(words, matching, likely, savedIndex, theTypeWhich);
    }

    /// <summary>Called from ParseAnInvocation2. 
    /// Tries to match the current words in the source to exactly one in-scope thing which satisfies the given parameter's type.
    /// On success returns the Instance (literal value, variable which holds the value, etc.) 
    /// Uses IsNameOfTypeWhich, ParseRelativeInvocation, ParseNumberOrdinalPercent, MatchToNamedStorageLocation.</summary>
    static instance ParseInvokedNoun(List<string> words, parameter satisfyMe, bool allowUnboundVariables = false)
    {
        if (index >= words.Count) return null;
        int savedIndex = index;
        while (words[index] == "the") index++;
        theTypeWhich theTypeWhich = null;
        invocation inv = null;
        long? number = null;
        Article? art = null;

        if (words[index].StartsWith("__litstring"))
            return (IsA(StandardType.text, satisfyMe.type)) ? null : new instance() { var = satisfyMe, literalString = Say(words[index++]), type = StandardType.text, toAccess = Indirectedness.literal };
        if ((theTypeWhich = IsNameOfTypeWhich(words)) != null && ((inv = ParseARelativeInvocation(theTypeWhich, words)) != null))
            return new instance() { var = satisfyMe, type = theTypeWhich.type.typeid, inner = inv, toAccess = Indirectedness.nested_invocation };
        if (satisfyMe.isAggregate && words[index].EndsWith("ing") && (inv = ParseAGerundInvocation(satisfyMe, words)) != null)
            return new instance() { var = satisfyMe, type = satisfyMe.type, inner = inv, toAccess = Indirectedness.nested_invocation };
        if ((number = ParseNumberOrdinalPercent(words)) != null)
            return (!IsA(numtype.AsStandardType(), satisfyMe.type)) ? null : new instance() { var = satisfyMe, literalValue = number.Value, type = numtype.AsStandardType(), toAccess = Indirectedness.literal };
        if (allowUnboundVariables && (art = ParseArticle(words.ToArray())) != null)
        {
            index = savedIndex; // back up over the article(s) for the following function to eat
            parameter unbound = ParseNounPhraseForParameter(words.ToArray(), false);
            if (unbound == null) return null;
            return new instance() { var = unbound, toAccess = Indirectedness.unbound };
        }
        return MatchToNamedStackLocation(words);
    }

    /// <summary> Higher-order functions will have a parameter of type "function". We invoke the higher-order function by passing
    /// it a gerund phrase. So, "To give a number" becomes "giving 5" when passed into a higher-order function. Here we try
    /// to match the subsignature to the source text.</summary>
    static invocation ParseAGerundInvocation(parameter funcSignatureToSatisfy, List<string> words)
    {
        subsubprompt++;
        var likely = new List<multimethod> { new multimethod() { signature = funcSignatureToSatisfy.relation, prompt = funcSignatureToSatisfy.prompt } };
        int savedIndex2 = index;
        List<invocation> matching = ParseAnInvocation2(words, likely, true);
        return InvocationProblems(words, matching, likely, savedIndex2, null, true);
    }

    /// <summary>After whittling down the list of multimethods to just "likely" methods, then to just "matching" methods,
    /// how many of each are left?  Ideally one "matching" method.  This applies stricter signature matching, then
    /// either reports errors or returns the sole "matching" method. Agnostic to relative clause or main clause.
    /// Used by ParseAnInvocation, ParseARelativeInvocation. </summary>
    public static invocation InvocationProblems(List<string> words, List<invocation> matching, List<multimethod> likely, int invocationBeginsAt, theTypeWhich theTypeWhich = null, bool isGerund = false)
    {
        string sourceLocation = "#" + prompt + "." + subprompt + (theTypeWhich == null && !isGerund ? "" : "." + subsubprompt);
        if (matching.Count == 1)
        {
            Console.WriteLine("  {2}: '{0}' matches definition {1}{3}", Say(words, matching[0].startWord, matching[0].endWord, theTypeWhich), matching[0].definition.prompt, sourceLocation, (matching[0].definition.prompt == prompt) ? " (recursive)" : "");
            index = matching[0].endWord + 1;
            matching[0].which = theTypeWhich;
            return matching[0];
        }
        if (matching.Count > 1)
        {
            Console.WriteLine("  {1}: Wow, {2} methods match '{0}'!", words[invocationBeginsAt], sourceLocation, matching.Count);
            double closestDistance = 99999;
            var stricterMatches = new List<invocation>();
            foreach (invocation match in matching)
            {
                long dist = Distance(match.definition, match.boundParameters);
                Console.WriteLine("   #{1} matched {0} words, with a distance of {2} ({3})", 1 + match.endWord - match.startWord, match.definition.prompt, Math.Sqrt(dist), dist);
                if (dist == closestDistance)
                    stricterMatches.Add(match);
                else if (dist < closestDistance)
                {
                    stricterMatches.Clear();
                    stricterMatches.Add(match);
                    closestDistance = dist;
                }
            }
            if (stricterMatches.Count == 1)
            {
                Console.WriteLine("   closest is #{0} at {1}", stricterMatches[0].definition.prompt, Math.Sqrt(closestDistance));
                index = stricterMatches[0].endWord + 1;
                stricterMatches[0].which = theTypeWhich;
                return stricterMatches[0];
            }
            Console.WriteLine("   distance critera still leaves {0} matches", stricterMatches.Count);
            return null;
        }
        // else matching.Count == 0
        switch (likely.Count)
        {
            case 0:
                Console.WriteLine("  {1}: No defined methods match all terms of '{0}'", words[invocationBeginsAt], sourceLocation);
                break;
            case 1:
                Console.WriteLine("  {0}: For '{1}...', I tried to match definition #{2} but '{3}...' didn't match the {4} term, {5}", sourceLocation,
                    words[invocationBeginsAt], likely[0].prompt, words[index], NumberToSpelledOutOrdinal(likely[0].matchingTerms + 1), likely[0].signature[likely[0].matchingTerms].ToString());
                break;
            default:
                Console.WriteLine("  {2}: No defined methods match all terms of '{0}' (but {1} likely)", words[invocationBeginsAt], likely.Count, sourceLocation);
                foreach (var item in likely)
                {
                    Console.Write("   #{1} matched {0} of its {2} terms", item.matchingTerms, item.prompt, item.signature.Count);
                    if (item.matchingTerms == item.signature.Count)
                        Console.Write(" but there were more words to match starting at '{0}...'", words[index]);// Say(words, index, parseMe.body.Count, theTypeWhich));
                    Console.WriteLine();
                }
                break;
        }
        return null;
    }
}
