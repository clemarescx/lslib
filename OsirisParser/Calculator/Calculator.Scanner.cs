using System;
using System.Collections.Generic;
using System.Text;

namespace OsirisParser.Calculator
{
    internal partial class CalculatorScanner
    {

        void GetNumber()
        {
            yylval.s = yytext;
            yylval.n = int.Parse(yytext);
        }

		public override void yyerror(string format, params object[] args)
		{
			base.yyerror(format, args);
			Console.WriteLine(format, args);
			Console.WriteLine();
		}
    }
}
