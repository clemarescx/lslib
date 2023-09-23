using LSLib.LS.Story.Compiler;
using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using QUT.Gppg;

namespace LSLib.LS.Story.GoalParser;

public class CodeLocation : IMerge<CodeLocation>
{
    private string fileName;
    private int startLine;   // start line
    private int startColumn; // start column
    private int endLine;     // end line
    private int endColumn;   // end column

    /// <summary>
    /// The line at which the text span starts.
    /// </summary>
    public string FileName => fileName;

    /// <summary>
    /// The line at which the text span starts.
    /// </summary>
    public int StartLine => startLine;

    /// <summary>
    /// The column at which the text span starts.
    /// </summary>
    public int StartColumn => startColumn;

    /// <summary>
    /// The line on which the text span ends.
    /// </summary>
    public int EndLine => endLine;

    /// <summary>
    /// The column of the first character
    /// beyond the end of the text span.
    /// </summary>
    public int EndColumn => endColumn;

    /// <summary>
    /// Default no-arg constructor.
    /// </summary>
    public CodeLocation() { }

    /// <summary>
    /// Constructor for text-span with given start and end.
    /// </summary>
    /// <param name="sl">start line</param>
    /// <param name="sc">start column</param>
    /// <param name="el">end line </param>
    /// <param name="ec">end column</param>
    public CodeLocation(string fl, int sl, int sc, int el, int ec)
    {
        fileName = fl;
        startLine = sl;
        startColumn = sc;
        endLine = el;
        endColumn = ec;
    }

    /// <summary>
    /// Create a text location which spans from the
    /// start of "this" to the end of the argument "last"
    /// </summary>
    /// <param name="last">The last location in the result span</param>
    /// <returns>The merged span</returns>
    public CodeLocation Merge(CodeLocation last)
    {
        return new(fileName, startLine, startColumn, last.endLine, last.endColumn);
    }
}

public abstract class GoalScanBase : AbstractScanner<object, CodeLocation>
{
    protected string fileName;

    public override CodeLocation yylloc { get; set; }

    protected virtual bool yywrap() { return true; }

    protected string MakeLiteral(string lit) => lit;

    protected string MakeString(string lit)
    {
        return MakeLiteral(Regex.Unescape(lit.Substring(1, lit.Length - 2)));
    }
}

