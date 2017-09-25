Oddball proof-of-concept pieces to be polished and used later.

### GC

A simple (simplistic?) garbage collector.  It has a circularly linked list called the clothesline where objects to be destroyed are hung.  

I've seen a language or two where an object's destructor ("dtor") takes care of shutting down resources it used, when those resources are wrapped in another kind of object.  Why can't the object that most directly wraps the resource take care of itself?  Because the containing object, once destroyed, means its member objects are also destroyed and hence can no longer take dtor actions? 

This GC takes the following approach. Objects "decompose" on the clothesline. When the GC processes an object X on the clothesline, for each reference object Y & Z that X owns (as in "strong reference"), the ownership is transferred to the clothesline.  Then the memory for X can be freed because nothing remains in X except value properties and null pointers.  Repeating the process, Y and Z will decompose in their own time as well. 

Basically, since objects are constructed in a smaller-to-larger manner, this GC deconstructs in reverse order, like popping a stack. 


### GrammarLines

Preliminary version of complish.

### OrdinalWordsToNumber

"First" becomes 1.   "One thousand four hundred eighty fifth" becomes 1485. 


### Contradictions

Given many statements like "A precedes B", sort them in order or find and report, readably, why the collection can't be sorted -- because there's a contradiction. 

