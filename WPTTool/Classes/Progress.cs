﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPTTool.Classes
{
    public class Progress
    {
        public int Total { get; set; }
        public int Parsed { get; set; }
        public int NotParsed { get; set; }
        public int Measured { get; set; }
        public int NotMeasured { get; set; }
        public string CurrentlyParsing { get; set; }
        public string CurrentlyMeasuring { get; set; }

    }
}