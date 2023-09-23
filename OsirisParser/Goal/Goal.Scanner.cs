using System;
using System.Collections.Generic;
using System.Text;
using LSLib.LS.Story.GoalParser;

namespace OsirisParser.Goal
{
    public partial class GoalScanner
    {
        public override void yyerror(string format, params object[] args)
        {
            base.yyerror(format, args);
            Console.WriteLine(format, args);
            Console.WriteLine();
        }

        public GoalScanner(string fileName)
        {
            this.fileName = fileName;
        }

        public CodeLocation LastLocation()
        {
            return new(null, tokLin, tokCol, tokELin, tokECol);
        }
    }
}