using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Poopin
{
    class Program
    {
        
        static void Main(string[] args)
        {
            Timer t = new Timer(StartParser, null, 0, 108000);
            Parser Parser = new Parser();
            Console.ReadLine();
        }
        private static void StartParser(Object o)
        {
            Console.WriteLine("Parser started: " + DateTime.Now);
            Parser Parser = new Parser();
            Console.WriteLine("Parsing comments from DELFI...");
            int NewCommentsFromDelfi = Parser.ParseComments();
            if (NewCommentsFromDelfi > 0)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Parsing comments from DELFI completed!\nTotal: " + NewCommentsFromDelfi + " comments parsed.");
                Console.ForegroundColor = ConsoleColor.White;
            }
            else
            {
                if (NewCommentsFromDelfi == 0)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("Attention! Found 0 comments. Source: DELFI");
                    Console.ForegroundColor = ConsoleColor.White;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Error: 1. Source: DELFI");
                    Console.ForegroundColor = ConsoleColor.White;
                }
            }

        }
    }
}
