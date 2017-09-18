using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using OpenCartNET.Base;

namespace OpenCartNET.OpenCart
{
    public class Categories
    {
        public int success { get; set; }
        public object[] error { get; set; }
        public List<ProductCategory> data { get; set; }
    }

    public class PostProductCategories
    {
        public List<ProductCategory> category_description { get; set; }
        public string sort_order { get; set; }
        public int[] category_store { get; set; }
        public int parent_id { get; set; }
        public string status { get; set; }
        public int[] category_filter { get; set; }
        public string column { get; set; }
        public int top { get; set; }
        public string keyword { get; set; }
    }

}

