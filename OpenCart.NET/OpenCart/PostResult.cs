using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenCartNET.OpenCart
{
    public class Data
    {
        public int id { get; set; }
    }

    public class PostResult
    {
        public int success { get; set; }
        public List<object> error { get; set; }
        public Data data { get; set; }
    }
}
