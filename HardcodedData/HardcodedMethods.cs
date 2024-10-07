using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DB2StructGenerator.HardcodedData
{
    public class HardcodedMethods
    {
        public string StorageName { get; protected set; } = string.Empty;
        public List<string> Methods { get; protected set; } = [];
    }
}
