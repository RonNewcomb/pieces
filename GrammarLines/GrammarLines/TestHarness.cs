using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.IO;

/// <summary>Spoon-feeds lines of input to the compiler before turning things over to Console.ReadLine().
/// Also grabs output and compares it to known good output.</summary>
public static class TestHarness
{
    public static int NextTestLine = 0; // the next element of testlines which ReadTestLine() will feed to the consuming compiler 
    public delegate string FetchLine(); // the type for the function Console.ReadLine()
    public static string ReadTestLine() // a function of the same type which gets input lines from the string array testlines
    {
        if (NextTestLine >= testlines.Count())
        {
            LineFeeder = Console.ReadLine; // then turn itself off
            LineExaminer = stdout;
            return LineFeeder();
        }
        Console.WriteLine(testlines[NextTestLine].Trim());
        return testlines[NextTestLine].Trim();
    }
    public static FetchLine LineFeeder = ReadTestLine; // a function variable holding the function the compiler calls for input
    private static TextWriter LineExaminer = new CompareWriter();  // this is our new Console.WriteLine() class 
    private readonly static TextWriter stdout = Console.Out;      // save this for when we turn off TestHarness
    private class CompareWriter : StringWriter // there's many overloads of Write() and WriteLine()
    {
        public override void WriteLine() { thisTurn += "\r\n"; }
        public override void Write(string value) { thisTurn += value; }
        public override void WriteLine(string value) { thisTurn += value + "\r\n"; }
    }
    private static string thisTurn;
    public static void Reset() { NextTestLine = 0; LineExaminer = new CompareWriter(); }
    public static void BeginTurn() { Console.SetOut(LineExaminer); thisTurn = ""; }
    public static void EndTurn(int responseSet)
    {
        if (NextTestLine + 3 >= testlines.Count()) LineExaminer = stdout; // turn itself off when its time
        if (Console.Out == stdout) return; // if off, do nothing
        NextTestLine += 3;
        Console.SetOut(TestHarness.stdout);
        if (testlines[NextTestLine - responseSet].Trim() != thisTurn.Trim())
            Console.WriteLine("{0}: {1}\r\n{2}\r\n******** EXPECTED **************************\n{3}\r\n****************************************", NextTestLine / 3, testlines[(NextTestLine / 3 - 1) * 3].Trim(), thisTurn.Trim(), testlines[NextTestLine - responseSet].Trim());
    }
    // The test input and expected output
    public static string[] testlines = new string[0];
    public static void loadTestScript(string filename)
    {
        if (!File.Exists(filename + ".txt"))
        {
            Console.WriteLine(filename + ".txt doesn't exist.");
            return;
        }
        testlines = File.ReadAllText(filename+".txt").Split(new string[]{"<SPLIT>"},StringSplitOptions.None);
        LineFeeder = ReadTestLine;
        Reset();
    }
}
