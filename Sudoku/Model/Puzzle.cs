using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sudoku
{
    public class Puzzle
    {
        public Puzzle()
        {
        }

        public string Initial  { get => ".....9.5.1.....2...2.7..8.6.45.1....89..4.51............8..2..9.5.4.8.....1..7..."; }
        public string Solution { get => "784629351136584297529731846645913728897246513312875964478152639953468172261397485"; }
    }
}
